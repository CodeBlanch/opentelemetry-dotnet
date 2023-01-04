// <copyright file="OtlpLogExporterWithOptions.cs" company="OpenTelemetry Authors">
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

using OpenTelemetry.Exporter;
using OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation;

namespace OpenTelemetry.Logs;

// TODO: When OtlpLogExporterOptions is moved into
// OpenTelemetry.Exporter.OpenTelemetryProtocol add these ctors to
// OtlpLogExporter and remove this class.
internal sealed class OtlpLogExporterWithOptions : OtlpLogExporter
{
    [Obsolete]
    public OtlpLogExporterWithOptions(OtlpExporterOptions options, SdkLimitOptions sdkLimitOptions)
        : base(sdkLimitOptions, new OtlpLogExporterOptions(options).GetLogExportClient())
    {
    }

    public OtlpLogExporterWithOptions(OtlpLogExporterOptions options, SdkLimitOptions sdkLimitOptions)
        : base(sdkLimitOptions, options?.GetLogExportClient() ?? throw new ArgumentNullException(nameof(options)))
    {
    }
}
