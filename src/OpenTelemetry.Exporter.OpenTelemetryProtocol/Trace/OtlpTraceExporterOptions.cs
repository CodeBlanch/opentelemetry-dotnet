// <copyright file="OtlpTraceExporterOptions.cs" company="OpenTelemetry Authors">
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

using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Exporter;

namespace OpenTelemetry.Trace;

/// <summary>
/// OpenTelemetry Protocol (OTLP) trace exporter options.
/// OTEL_EXPORTER_OTLP_TRACES_ENDPOINT, OTEL_EXPORTER_OTLP_TRACES_HEADERS,
/// OTEL_EXPORTER_OTLP_TRACES_TIMEOUT, OTEL_EXPORTER_OTLP_TRACES_PROTOCOL
/// environment variables are parsed during object construction in addition to
/// the keys defined by <see cref="OtlpExporterBaseOptions"/>.
/// </summary>
/// <remarks>
/// The constructor throws <see cref="FormatException"/> if it fails to parse
/// any of the supported environment variables.
/// </remarks>
public class OtlpTraceExporterOptions : OtlpExporterBaseOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OtlpTraceExporterOptions"/> class.
    /// </summary>
    public OtlpTraceExporterOptions()
        : this(new ConfigurationBuilder().AddEnvironmentVariables().Build(), new())
    {
    }

    internal OtlpTraceExporterOptions(
        IConfiguration configuration,
        BatchExportActivityProcessorOptions defaultBatchOptions)
        : base(configuration)
    {
        this.BatchExportProcessorOptions = defaultBatchOptions;
    }

    [Obsolete]
    internal OtlpTraceExporterOptions(OtlpExporterOptions options)
        : base(options)
    {
        this.ExportProcessorType = options.ExportProcessorType;
        this.BatchExportProcessorOptions = options.BatchExportProcessorOptions ?? new BatchExportActivityProcessorOptions();
    }

    internal override string? SignalEndpointEnvVarName => "OTEL_EXPORTER_OTLP_TRACES_ENDPOINT";

    internal override string? SignalHeadersEnvVarName => "OTEL_EXPORTER_OTLP_TRACES_HEADERS";

    internal override string? SignalTimeoutEnvVarName => "OTEL_EXPORTER_OTLP_TRACES_TIMEOUT";

    internal override string? SignalProtocolEnvVarName => "OTEL_EXPORTER_OTLP_TRACES_PROTOCOL";

    /// <summary>
    /// Gets or sets the export processor type to be used with the OpenTelemetry Protocol Exporter. The default value is <see cref="ExportProcessorType.Batch"/>.
    /// </summary>
    public ExportProcessorType ExportProcessorType { get; set; } = ExportProcessorType.Batch;

    /// <summary>
    /// Gets or sets the BatchExportProcessor options. Ignored unless ExportProcessorType is Batch.
    /// </summary>
    public BatchExportProcessorOptions<Activity> BatchExportProcessorOptions { get; set; }
}
