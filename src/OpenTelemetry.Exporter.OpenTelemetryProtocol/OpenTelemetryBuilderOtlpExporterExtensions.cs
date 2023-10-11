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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation;
using OpenTelemetry.Internal;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace OpenTelemetry;

public static class OpenTelemetryBuilderOtlpExporterExtensions
{
    public static T AddOtlpExporter<T>(
        this T builder)
        where T : IOpenTelemetryBuilder
        => AddOtlpExporter(builder, name: null, configure: null);

    public static T AddOtlpExporter<T>(
        this T builder,
        Action<OtlpExporterBuilderOptions>? configure)
        where T : IOpenTelemetryBuilder
        => AddOtlpExporter(builder, name: null, configure);

    public static T AddOtlpExporter<T>(
        this T builder,
        string? name,
        Action<OtlpExporterBuilderOptions>? configure)
        where T : IOpenTelemetryBuilder
    {
        Guard.ThrowIfNull(builder);

        builder.Services.RegisterOptionsFactory(configuration => new SdkLimitOptions(configuration));

        builder.Services.RegisterOptionsFactory(
            (sp, config, name) =>
            new OtlpExporterBuilderOptions(
                config,
                sp.GetRequiredService<IOptionsMonitor<BatchExportActivityProcessorOptions>>().Get(name)));

        name ??= Options.DefaultName;

        if (configure != null)
        {
            builder.Services.Configure(name, configure);
        }

        builder.Services.AddOptions<OpenTelemetryLoggerOptions>()
            .Configure<IServiceProvider>((logging, sp) =>
            {
                var builderOptions = GetOptions(sp, name);

                if (!builderOptions.EnableLogging)
                {
                    return;
                }

                logging.AddProcessor(
                    OtlpLogExporterHelperExtensions.BuildOtlpLogExporter(
                        OtlpExporterOptions.Merge(builderOptions.DefaultOptions, builderOptions.LoggingOptions),
                        sp.GetRequiredService<IOptionsMonitor<LogRecordExportProcessorOptions>>().Get(name)));
            });

        builder.Services.ConfigureOpenTelemetryMeterProvider(
            (sp, metrics) =>
            {
                var builderOptions = GetOptions(sp, name);

                if (!builderOptions.EnableMetrics)
                {
                    return;
                }

                metrics.AddReader(
                    OtlpMetricExporterExtensions.BuildOtlpExporterMetricReader(
                        OtlpExporterOptions.Merge(builderOptions.DefaultOptions, builderOptions.MetricsOptions),
                        sp.GetRequiredService<IOptionsMonitor<MetricReaderOptions>>().Get(name),
                        sp));
            });

        builder.Services.ConfigureOpenTelemetryTracerProvider(
            (sp, tracing) =>
            {
                var builderOptions = GetOptions(sp, name);

                if (!builderOptions.EnableTracing)
                {
                    return;
                }

                tracing.AddProcessor(
                    OtlpTraceExporterHelperExtensions.BuildOtlpExporterProcessor(
                        OtlpExporterOptions.Merge(builderOptions.DefaultOptions, builderOptions.TracingOptions),
                        sp.GetRequiredService<IOptionsMonitor<SdkLimitOptions>>().CurrentValue,
                        sp));
            });

        return builder;

        static OtlpExporterBuilderOptions GetOptions(IServiceProvider sp, string name)
        {
            return sp.GetRequiredService<IOptionsMonitor<OtlpExporterBuilderOptions>>().Get(name);
        }
    }
}
