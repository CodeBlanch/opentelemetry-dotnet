// <copyright file="OpenTelemetryLoggerProvider.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

#nullable enable

using System;
using System.Collections;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Internal;
using OpenTelemetry.Resources;

namespace OpenTelemetry.Logs
{
    /// <summary>
    /// An <see cref="ILoggerProvider"/> implementation for exporting logs using OpenTelemetry.
    /// </summary>
    [ProviderAlias("OpenTelemetry")]
    public class OpenTelemetryLoggerProvider : BaseProvider, ILoggerProvider, ISupportExternalScope
    {
        internal readonly bool IncludeScopes;
        internal readonly bool IncludeFormattedMessage;
        internal readonly bool ParseStateValues;
        internal BaseProcessor<LogRecord>? Processor;
        internal Resource Resource;
        private readonly ServiceProvider? ownedServiceProvider;
        private readonly Hashtable loggers = new();
        private ILogRecordPool? threadStaticPool = LogRecordThreadStaticPool.Instance;
        private bool disposed;

        static OpenTelemetryLoggerProvider()
        {
            // Accessing Sdk class is just to trigger its static ctor,
            // which sets default Propagators and default Activity Id format
            _ = Sdk.SuppressInstrumentation;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenTelemetryLoggerProvider"/> class.
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
        public OpenTelemetryLoggerProvider(IServiceProvider serviceProvider)
            : this(
                  serviceProvider?.GetRequiredService<IOptions<OpenTelemetryLoggerOptions>>().Value ?? throw new ArgumentNullException(nameof(serviceProvider)),
                  serviceProvider,
                  ownsServiceProvider: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenTelemetryLoggerProvider"/> class.
        /// </summary>
        /// <param name="options"><see cref="OpenTelemetryLoggerOptions"/>.</param>
        [Obsolete("Call the OpenTelemetryLoggerProvider constructor which accepts IServiceProvider")]
        public OpenTelemetryLoggerProvider(IOptionsMonitor<OpenTelemetryLoggerOptions> options)
            : this(options?.CurrentValue ?? throw new ArgumentNullException(nameof(options)), serviceProvider: null, ownsServiceProvider: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenTelemetryLoggerProvider"/> class.
        /// </summary>
        public OpenTelemetryLoggerProvider()
            : this(new(), serviceProvider: null, ownsServiceProvider: false)
        {
        }

        // Note: This is only for tests. Options will be missing ServiceCollection & ServiceProvider features will be unavailable.
        internal OpenTelemetryLoggerProvider(OpenTelemetryLoggerOptions options)
            : this(options, serviceProvider: null, ownsServiceProvider: false)
        {
        }

        private OpenTelemetryLoggerProvider(OpenTelemetryLoggerOptions options, IServiceProvider? serviceProvider, bool ownsServiceProvider)
        {
            Guard.ThrowIfNull(options);

            this.IncludeScopes = options.IncludeScopes;
            this.IncludeFormattedMessage = options.IncludeFormattedMessage;
            this.ParseStateValues = options.ParseStateValues;

            if (ownsServiceProvider)
            {
                this.ownedServiceProvider = serviceProvider as ServiceProvider;
            }

            this.Resource = options.ResourceBuilder.Build();

            // Step 1: Add any processors added to options.

            foreach (var processor in options.Processors)
            {
                this.AddProcessor(processor);
            }

            var configurationActions = options.ConfigurationActions;
            if (configurationActions?.Count > 0)
            {
                // Step 2: Execute any configuration actions.

                if (serviceProvider == null)
                {
                    throw new InvalidOperationException("Configuration actions were registered on options but no service provider was supplied.");
                }

                // Note: Not using a foreach loop because additional actions can be
                // added during each call.
                for (int i = 0; i < configurationActions.Count; i++)
                {
                    configurationActions[i](serviceProvider, this);
                }

                options.ConfigurationActions = null;
            }

            if (serviceProvider != null)
            {
                // Step 3: Look for any processors registered directly with the service provider.

                var registeredProcessors = serviceProvider.GetServices<BaseProcessor<LogRecord>>();
                foreach (BaseProcessor<LogRecord> processor in registeredProcessors)
                {
                    this.AddProcessor(processor);
                }
            }
        }

        internal IExternalScopeProvider? ScopeProvider { get; private set; }

        internal ILogRecordPool LogRecordPool => this.threadStaticPool ?? LogRecordSharedPool.Current;

        /// <summary>
        /// Create a <see cref="OpenTelemetryLoggerProvider"/> instance.
        /// </summary>
        /// <param name="configure">Configuration callback.</param>
        /// <returns><see cref="OpenTelemetryLoggerProvider"/>.</returns>
        public static OpenTelemetryLoggerProvider Create(Action<OpenTelemetryLoggerOptions>? configure = null)
        {
            OpenTelemetryLoggerOptions options = new();

            if (configure != null)
            {
                ServiceCollection services = new ServiceCollection();

                options.Services = services;

                configure.Invoke(options);

                IServiceProvider serviceProvider = services.BuildServiceProvider();

                return new OpenTelemetryLoggerProvider(options, serviceProvider, ownsServiceProvider: true);
            }

            return new OpenTelemetryLoggerProvider(options, serviceProvider: null, ownsServiceProvider: false);
        }

        /// <inheritdoc/>
        void ISupportExternalScope.SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            this.ScopeProvider = scopeProvider;

            lock (this.loggers)
            {
                foreach (DictionaryEntry entry in this.loggers)
                {
                    if (entry.Value is OpenTelemetryLogger logger)
                    {
                        logger.ScopeProvider = scopeProvider;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public ILogger CreateLogger(string categoryName)
        {
            if (this.loggers[categoryName] is not OpenTelemetryLogger logger)
            {
                lock (this.loggers)
                {
                    logger = (this.loggers[categoryName] as OpenTelemetryLogger)!;
                    if (logger == null)
                    {
                        logger = new OpenTelemetryLogger(categoryName, this)
                        {
                            ScopeProvider = this.ScopeProvider,
                        };

                        this.loggers[categoryName] = logger;
                    }
                }
            }

            return logger;
        }

        /// <summary>
        /// Flushes all the processors registered under <see
        /// cref="OpenTelemetryLoggerProvider"/>, blocks the current thread
        /// until flush completed, shutdown signaled or timed out.
        /// </summary>
        /// <param name="timeoutMilliseconds">
        /// The number (non-negative) of milliseconds to wait, or
        /// <c>Timeout.Infinite</c> to wait indefinitely.
        /// </param>
        /// <returns>
        /// Returns <c>true</c> when force flush succeeded; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the <c>timeoutMilliseconds</c> is smaller than -1.
        /// </exception>
        /// <remarks>
        /// This function guarantees thread-safety.
        /// </remarks>
        public bool ForceFlush(int timeoutMilliseconds = Timeout.Infinite)
        {
            return this.Processor?.ForceFlush(timeoutMilliseconds) ?? true;
        }

        /// <summary>
        /// Add a processor to the <see cref="OpenTelemetryLoggerProvider"/>.
        /// </summary>
        /// <remarks>
        /// Note: The supplied <paramref name="processor"/> will be
        /// automatically disposed when then the <see
        /// cref="OpenTelemetryLoggerProvider"/> is disposed.
        /// </remarks>
        /// <param name="processor">Log processor to add.</param>
        /// <returns>The supplied <see cref="OpenTelemetryLoggerOptions"/> for chaining.</returns>
        public OpenTelemetryLoggerProvider AddProcessor(BaseProcessor<LogRecord> processor)
        {
            Guard.ThrowIfNull(processor);

            processor.SetParentProvider(this);

            if (this.threadStaticPool != null && this.ContainsBatchProcessor(processor))
            {
                this.threadStaticPool = null;
            }

            if (this.Processor == null)
            {
                this.Processor = processor;
            }
            else if (this.Processor is CompositeProcessor<LogRecord> compositeProcessor)
            {
                compositeProcessor.AddProcessor(processor);
            }
            else
            {
                var newCompositeProcessor = new CompositeProcessor<LogRecord>(new[]
                {
                    this.Processor,
                });
                newCompositeProcessor.SetParentProvider(this);
                newCompositeProcessor.AddProcessor(processor);
                this.Processor = newCompositeProcessor;
            }

            return this;
        }

        /// <summary>
        /// Create a <see cref="LogEmitter"/>.
        /// </summary>
        /// <returns><see cref="LogEmitter"/>.</returns>
        internal LogEmitter CreateEmitter() => new(this);

        internal bool ContainsBatchProcessor(BaseProcessor<LogRecord> processor)
        {
            if (processor is BatchExportProcessor<LogRecord>)
            {
                return true;
            }
            else if (processor is CompositeProcessor<LogRecord> compositeProcessor)
            {
                var current = compositeProcessor.Head;
                while (current != null)
                {
                    if (this.ContainsBatchProcessor(current.Value))
                    {
                        return true;
                    }

                    current = current.Next;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    // Wait for up to 5 seconds grace period
                    this.Processor?.Shutdown(5000);
                    this.Processor?.Dispose();

                    this.ownedServiceProvider?.Dispose();
                }

                this.disposed = true;
                OpenTelemetrySdkEventSource.Log.ProviderDisposed(nameof(OpenTelemetryLoggerProvider));
            }

            base.Dispose(disposing);
        }
    }
}
