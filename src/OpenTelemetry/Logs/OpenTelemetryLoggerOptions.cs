// <copyright file="OpenTelemetryLoggerOptions.cs" company="OpenTelemetry Authors">
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
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Internal;
using OpenTelemetry.Resources;

namespace OpenTelemetry.Logs
{
    /// <summary>
    /// Contains OpenTelemetry logging options.
    /// </summary>
    public class OpenTelemetryLoggerOptions
    {
        internal readonly List<BaseProcessor<LogRecord>> Processors = new();
        internal ResourceBuilder ResourceBuilder = ResourceBuilder.CreateDefault();
        internal List<Action<IServiceProvider, OpenTelemetryLoggerProvider>>? ConfigurationActions = new();

        private const bool DefaultIncludeScopes = false;
        private const bool DefaultIncludeFormattedMessage = false;
        private const bool DefaultParseStateValues = false;

        private bool? includeScopes;
        private bool? includeFormattedMessage;
        private bool? parseStateValues;

        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> where Logging services are
        /// configured.
        /// </summary>
        /// <remarks>
        /// Note: <see cref="Services"/> are only available during the
        /// application configuration phase. When using "Options" pattern via
        /// <see cref="OptionsServiceCollectionExtensions"/> or interfaces such
        /// as <see cref="IConfigureOptions{T}"/> <see cref="Services"/> will be
        /// unavailable because "Options" are built after application services
        /// have been configured.
        /// </remarks>
        public IServiceCollection? Services { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not log scopes should be
        /// included on generated <see cref="LogRecord"/>s. Default value:
        /// False.
        /// </summary>
        public bool IncludeScopes
        {
            get => this.includeScopes ?? DefaultIncludeScopes;
            set => this.includeScopes = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not formatted log message
        /// should be included on generated <see cref="LogRecord"/>s. Default
        /// value: False.
        /// </summary>
        public bool IncludeFormattedMessage
        {
            get => this.includeFormattedMessage ?? DefaultIncludeFormattedMessage;
            set => this.includeFormattedMessage = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not log state should be
        /// parsed into <see cref="LogRecord.StateValues"/> on generated <see
        /// cref="LogRecord"/>s. Default value: False.
        /// </summary>
        /// <remarks>
        /// Note: When <see cref="ParseStateValues"/> is set to <see
        /// langword="true"/> <see cref="LogRecord.State"/> will always be <see
        /// langword="null"/>.
        /// </remarks>
        public bool ParseStateValues
        {
            get => this.parseStateValues ?? DefaultParseStateValues;
            set => this.parseStateValues = value;
        }

        /// <summary>
        /// Adds processor to the options.
        /// </summary>
        /// <remarks>
        /// Note: The supplied <paramref name="processor"/> will be
        /// automatically disposed when then the final <see
        /// cref="OpenTelemetryLoggerProvider"/> built from the options is
        /// disposed.
        /// </remarks>
        /// <param name="processor">Log processor to add.</param>
        /// <returns>Returns <see cref="OpenTelemetryLoggerOptions"/> for chaining.</returns>
        public OpenTelemetryLoggerOptions AddProcessor(BaseProcessor<LogRecord> processor)
        {
            Guard.ThrowIfNull(processor);

            this.Processors.Add(processor);

            return this;
        }

        /// <summary>
        /// Adds a processor to the options which will be retrieved using dependency injection.
        /// </summary>
        /// <typeparam name="T">Processor type.</typeparam>
        /// <returns>The supplied <see cref="OpenTelemetryLoggerOptions"/> for chaining.</returns>
        public OpenTelemetryLoggerOptions AddProcessor<T>()
            where T : BaseProcessor<LogRecord>
        {
            return this.Configure((sp, provider) =>
            {
                provider.AddProcessor(sp.GetRequiredService<T>());
            });
        }

        /// <summary>
        /// Sets the <see cref="Resources.ResourceBuilder"/> from which the Resource associated with
        /// this provider is built from. Overwrites currently set ResourceBuilder.
        /// You should usually use <see cref="ConfigureResource(Action{ResourceBuilder})"/> instead
        /// (call <see cref="ResourceBuilder.Clear"/> if desired).
        /// </summary>
        /// <param name="resourceBuilder"><see cref="Resources.ResourceBuilder"/> from which Resource will be built.</param>
        /// <returns>Returns <see cref="OpenTelemetryLoggerOptions"/> for chaining.</returns>
        public OpenTelemetryLoggerOptions SetResourceBuilder(ResourceBuilder resourceBuilder)
        {
            Guard.ThrowIfNull(resourceBuilder);
            this.ResourceBuilder = resourceBuilder;
            return this;
        }

        /// <summary>
        /// Modify the <see cref="Resources.ResourceBuilder"/> from which the Resource associated with
        /// this provider is built from in-place.
        /// </summary>
        /// <param name="configure">An action which modifies the provided <see cref="Resources.ResourceBuilder"/> in-place.</param>
        /// <returns>Returns <see cref="OpenTelemetryLoggerOptions"/> for chaining.</returns>
        public OpenTelemetryLoggerOptions ConfigureResource(Action<ResourceBuilder> configure)
        {
            Guard.ThrowIfNull(configure, nameof(configure));
            configure(this.ResourceBuilder);
            return this;
        }

        /// <summary>
        /// Register a callback action to configure the <see
        /// cref="OpenTelemetryLoggerProvider"/> once the application <see
        /// cref="IServiceProvider"/> is available.
        /// </summary>
        /// <param name="configure">Configuration callback.</param>
        /// <returns>The supplied <see cref="OpenTelemetryLoggerOptions"/> for chaining.</returns>
        public OpenTelemetryLoggerOptions Configure(Action<IServiceProvider, OpenTelemetryLoggerProvider> configure)
        {
            Guard.ThrowIfNull(configure);

            var configurationActions = this.ConfigurationActions;
            if (configurationActions == null)
            {
                throw new InvalidOperationException("Configuration actions cannot be registered on options after OpenTelemetryLoggerProvider has been created.");
            }

            configurationActions.Add(configure);

            return this;
        }

        internal void ApplyTo(OpenTelemetryLoggerOptions other)
        {
            Debug.Assert(other != null, "other instance was null");

            other!.ResourceBuilder = this.ResourceBuilder;

            if (this.includeFormattedMessage.HasValue)
            {
                other.includeFormattedMessage = this.includeFormattedMessage;
            }

            if (this.includeScopes.HasValue)
            {
                other.includeScopes = this.includeScopes;
            }

            if (this.parseStateValues.HasValue)
            {
                other.parseStateValues = this.parseStateValues;
            }

            Debug.Assert(this.Processors != null && other.Processors != null, "Processors was null");

            foreach (var processor in this.Processors!)
            {
                other.Processors!.Add(processor);
            }

            Debug.Assert(this.ConfigurationActions != null && other.ConfigurationActions != null, "ConfigurationActions was null");

            foreach (var configurationAction in this.ConfigurationActions!)
            {
                other.ConfigurationActions!.Add(configurationAction);
            }
        }
    }
}
