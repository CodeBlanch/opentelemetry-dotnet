// <copyright file="OtlpMetricExporter.cs" company="OpenTelemetry Authors">
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

using System.Diagnostics;
using OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation;
using OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation.ExportClient;
using OpenTelemetry.Metrics;
using OtlpCollector = OpenTelemetry.Proto.Collector.Metrics.V1;
using OtlpResource = OpenTelemetry.Proto.Resource.V1;

namespace OpenTelemetry.Exporter
{
    /// <summary>
    /// Exporter consuming <see cref="Metric"/> and exporting the data using
    /// the OpenTelemetry protocol (OTLP).
    /// </summary>
    public class OtlpMetricExporter : BaseExporter<Metric>
    {
        private readonly IExportClient<OtlpCollector.ExportMetricsServiceRequest> exportClient;

        private OtlpResource.Resource processResource;

        /// <summary>
        /// Initializes a new instance of the <see cref="OtlpMetricExporter"/> class.
        /// </summary>
        /// <param name="options">Configuration options for the exporter.</param>
        [Obsolete("Use the ctor accepting OtlpMetricExporterOptions instead this method will be removed in a future version.")]
        public OtlpMetricExporter(OtlpExporterOptions options)
            : this(options?.GetMetricsExportClient() ?? throw new ArgumentNullException(nameof(options)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OtlpMetricExporter"/> class.
        /// </summary>
        /// <param name="options">Configuration options for the exporter.</param>
        public OtlpMetricExporter(OtlpMetricExporterOptions options)
            : this(options?.GetMetricsExportClient() ?? throw new ArgumentNullException(nameof(options)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OtlpMetricExporter"/> class.
        /// </summary>
        /// <param name="exportClient">Client used for sending export request.</param>
        internal OtlpMetricExporter(IExportClient<OtlpCollector.ExportMetricsServiceRequest> exportClient)
        {
            Debug.Assert(exportClient != null, "exportClient was null");

            this.exportClient = exportClient;
        }

        internal OtlpResource.Resource ProcessResource => this.processResource ??= this.ParentProvider.GetResource().ToOtlpResource();

        /// <inheritdoc />
        public override ExportResult Export(in Batch<Metric> metrics)
        {
            // Prevents the exporter's gRPC and HTTP operations from being instrumented.
            using var scope = SuppressInstrumentationScope.Begin();

            var request = new OtlpCollector.ExportMetricsServiceRequest();

            try
            {
                request.AddMetrics(this.ProcessResource, metrics);

                if (!this.exportClient.SendExportRequest(request))
                {
                    return ExportResult.Failure;
                }
            }
            catch (Exception ex)
            {
                OpenTelemetryProtocolExporterEventSource.Log.ExportMethodException(ex);
                return ExportResult.Failure;
            }
            finally
            {
                request.Return();
            }

            return ExportResult.Success;
        }

        /// <inheritdoc />
        protected override bool OnShutdown(int timeoutMilliseconds)
        {
            return this.exportClient?.Shutdown(timeoutMilliseconds) ?? true;
        }
    }
}
