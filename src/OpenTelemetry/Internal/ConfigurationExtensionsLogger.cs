// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Internal;

namespace Microsoft.Extensions.Configuration;

internal static class ConfigurationExtensionsLogger
{
    public static void LogInvalidEnvironmentVariable(string key, string value)
    {
        OpenTelemetrySdkEventSource.Log.InvalidEnvironmentVariable(key, value);
    }
}
