// <copyright file="OtlpMetricExporterOptions.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.Metrics;

/// <summary>
/// OpenTelemetry Protocol (OTLP) metric exporter options.
/// OTEL_EXPORTER_OTLP_METRICS_ENDPOINT, OTEL_EXPORTER_OTLP_METRICS_HEADERS,
/// OTEL_EXPORTER_OTLP_METRICS_TIMEOUT, OTEL_EXPORTER_OTLP_METRICS_PROTOCOL
/// environment variables are parsed during object construction in addition to
/// the keys defined by <see cref="OtlpExporterBaseOptions"/>.
/// </summary>
/// <remarks>
/// The constructor throws <see cref="FormatException"/> if it fails to parse
/// any of the supported environment variables.
/// </remarks>
public class OtlpMetricExporterOptions : OtlpExporterBaseOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OtlpMetricExporterOptions"/> class.
    /// </summary>
    public OtlpMetricExporterOptions()
        : this(new ConfigurationBuilder().AddEnvironmentVariables().Build())
    {
    }

    internal OtlpMetricExporterOptions(IConfiguration configuration)
        : base(configuration)
    {
    }

    [Obsolete]
    internal OtlpMetricExporterOptions(OtlpExporterOptions options)
        : base(options)
    {
    }

    internal override string? SignalEndpointEnvVarName => "OTEL_EXPORTER_OTLP_METRICS_ENDPOINT";

    internal override string? SignalHeadersEnvVarName => "OTEL_EXPORTER_OTLP_METRICS_HEADERS";

    internal override string? SignalTimeoutEnvVarName => "OTEL_EXPORTER_OTLP_METRICS_TIMEOUT";

    internal override string? SignalProtocolEnvVarName => "OTEL_EXPORTER_OTLP_METRICS_PROTOCOL";
}
