// <copyright file="OpenTelemetryBuilderOtlpExporterExtensions.cs" company="OpenTelemetry Authors">
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

using Microsoft.Extensions.Configuration;
using OpenTelemetry.Exporter;
using OpenTelemetry.Internal;
using OpenTelemetry.Trace;

namespace OpenTelemetry;

public sealed class OtlpExporterBuilderOptions
{
    public OtlpExporterBuilderOptions()
        : this(new ConfigurationBuilder().AddEnvironmentVariables().Build(), new())
    {
    }

    internal OtlpExporterBuilderOptions(
        IConfiguration configuration,
        BatchExportActivityProcessorOptions defaultBatchOptions)
    {
        this.LoggingOptions = new OtlpExporterOptions(configuration, defaultBatchOptions);

        this.MetricsOptions = new OtlpExporterOptions(configuration, defaultBatchOptions);

        this.TracingOptions = new OtlpExporterOptions(configuration, defaultBatchOptions);

        if (configuration.TryGetUriValue("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", out var endpoint))
        {
            this.LoggingOptions.Endpoint = endpoint;
        }

        if (configuration.TryGetUriValue("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", out endpoint))
        {
            this.MetricsOptions.Endpoint = endpoint;
        }

        if (configuration.TryGetUriValue("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", out endpoint))
        {
            this.TracingOptions.Endpoint = endpoint;
        }
    }

    public bool EnableLogging { get; set; } = true;

    public bool EnableMetrics { get; set; } = true;

    public bool EnableTracing { get; set; } = true;

    public OtlpExporterOptions LoggingOptions { get; }

    public OtlpExporterOptions MetricsOptions { get; }

    public OtlpExporterOptions TracingOptions { get; }
}
