// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Exporter.Zipkin.Implementation;

namespace Microsoft.Extensions.Configuration;

internal static class ConfigurationExtensionsLogger
{
    public static void LogInvalidEnvironmentVariable(string key, string value)
    {
        ZipkinExporterEventSource.Log.InvalidEnvironmentVariable(key, value);
    }
}
