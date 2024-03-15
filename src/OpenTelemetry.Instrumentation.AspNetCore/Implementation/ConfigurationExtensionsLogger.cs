// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Instrumentation.AspNetCore.Implementation;

namespace Microsoft.Extensions.Configuration;

internal static class ConfigurationExtensionsLogger
{
    public static void LogInvalidEnvironmentVariable(string key, string value)
    {
        AspNetCoreInstrumentationEventSource.Log.InvalidEnvironmentVariable(key, value);
    }
}
