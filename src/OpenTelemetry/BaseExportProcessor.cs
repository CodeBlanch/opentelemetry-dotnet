// <copyright file="BaseExportProcessor.cs" company="OpenTelemetry Authors">
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
using System.Threading;
using OpenTelemetry.Internal;

namespace OpenTelemetry
{
    /// <summary>
    /// Type of Export Processor to be used.
    /// </summary>
    public enum ExportProcessorType
    {
        /// <summary>
        /// Use SimpleExportProcessor.
        /// Refer to the <a href="https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/sdk.md#simple-processor">
        /// specification</a> for more information.
        /// </summary>
        Simple,

        /// <summary>
        /// Use BatchExportProcessor.
        /// Refer to <a href="https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/sdk.md#batching-processor">
        /// specification</a> for more information.
        /// </summary>
        Batch,
    }

    /// <summary>
    /// Contains the defined export processor filter decisions.
    /// </summary>
    public enum ExportFilterDecision
    {
        /// <summary>
        /// Item will be exported.
        /// </summary>
        Export,

        /// <summary>
        /// Item will ignored.
        /// </summary>
        Ignore,
    }

    /// <summary>
    /// Implements processor that exports telemetry objects.
    /// </summary>
    /// <typeparam name="T">The type of telemetry object to be exported.</typeparam>
    public abstract class BaseExportProcessor<T> : BaseProcessor<T>
        where T : class
    {
        protected readonly BaseExporter<T> exporter;
        private long filteredCount;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseExportProcessor{T}"/> class.
        /// </summary>
        /// <param name="exporter">Exporter instance.</param>
        protected BaseExportProcessor(BaseExporter<T> exporter)
        {
            Guard.ThrowIfNull(exporter);

            this.exporter = exporter;
        }

        /// <summary>
        /// Gets or sets a filter function that is run OnEnd to determine if a
        /// <typeparamref name="T"/> instance should be exported or not.
        /// </summary>
        /// <remarks>
        /// Note: An instance of <typeparamref name="T"/> will be exported if
        /// any of the following conditions are <see langword="true"/>...
        /// <list type="bullet"><item><see cref="ExportFilter"/> is <see
        /// langword="null"/>.</item>
        /// <item><see cref="ExportFilter"/> returns <see
        /// cref="ExportFilterDecision.Export"/>.</item>
        /// <item><see cref="ExportFilter"/> throws an exception.</item></list>
        /// </remarks>
        public Func<T, ExportFilterDecision>? ExportFilter { get; set; }

        internal BaseExporter<T> Exporter => this.exporter;

        internal long FilteredCount => this.filteredCount;

        /// <inheritdoc />
        public sealed override void OnStart(T data)
        {
        }

        public override void OnEnd(T data)
        {
            var filter = this.ExportFilter;
            if (filter != null)
            {
                try
                {
                    var result = filter(data);
                    if (result == ExportFilterDecision.Ignore)
                    {
                        Interlocked.Increment(ref this.filteredCount);
                        return;
                    }
                }
                catch
                {
                    // TODO: Log
                }
            }

            this.OnExport(data);
        }

        internal override void SetParentProvider(BaseProvider parentProvider)
        {
            base.SetParentProvider(parentProvider);

            this.exporter.ParentProvider = parentProvider;
        }

        protected abstract void OnExport(T data);

        /// <inheritdoc />
        protected override bool OnForceFlush(int timeoutMilliseconds)
        {
            return this.exporter.ForceFlush(timeoutMilliseconds);
        }

        /// <inheritdoc />
        protected override bool OnShutdown(int timeoutMilliseconds)
        {
            return this.exporter.Shutdown(timeoutMilliseconds);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    try
                    {
                        this.exporter.Dispose();
                    }
                    catch (Exception ex)
                    {
                        OpenTelemetrySdkEventSource.Log.SpanProcessorException(nameof(this.Dispose), ex);
                    }
                }

                this.disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
