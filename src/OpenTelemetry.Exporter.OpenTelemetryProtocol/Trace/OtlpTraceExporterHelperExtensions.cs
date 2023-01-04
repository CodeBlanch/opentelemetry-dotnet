// <copyright file="OtlpTraceExporterHelperExtensions.cs" company="OpenTelemetry Authors">
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation;
using OpenTelemetry.Internal;

namespace OpenTelemetry.Trace
{
    /// <summary>
    /// Extension methods to simplify registering of the OpenTelemetry Protocol (OTLP) exporter.
    /// </summary>
    public static class OtlpTraceExporterHelperExtensions
    {
        /// <summary>
        /// Adds OpenTelemetry Protocol (OTLP) exporter to the TracerProvider.
        /// </summary>
        /// <param name="builder"><see cref="TracerProviderBuilder"/> builder to use.</param>
        /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
        [Obsolete("Use AddOtlpTraceExporter instead this method will be removed in a future version.")]
        public static TracerProviderBuilder AddOtlpExporter(this TracerProviderBuilder builder)
            => AddOtlpExporter(builder, name: null, configure: null);

        /// <summary>
        /// Adds OpenTelemetry Protocol (OTLP) exporter to the TracerProvider.
        /// </summary>
        /// <param name="builder"><see cref="TracerProviderBuilder"/> builder to use.</param>
        /// <param name="configure">Callback action for configuring <see cref="OtlpExporterOptions"/>.</param>
        /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
        [Obsolete("Use AddOtlpTraceExporter instead this method will be removed in a future version.")]
        public static TracerProviderBuilder AddOtlpExporter(this TracerProviderBuilder builder, Action<OtlpExporterOptions> configure)
            => AddOtlpExporter(builder, name: null, configure);

        /// <summary>
        /// Adds OpenTelemetry Protocol (OTLP) exporter to the TracerProvider.
        /// </summary>
        /// <param name="builder"><see cref="TracerProviderBuilder"/> builder to use.</param>
        /// <param name="name">Name which is used when retrieving options.</param>
        /// <param name="configure">Callback action for configuring <see cref="OtlpExporterOptions"/>.</param>
        /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
        [Obsolete("Use AddOtlpTraceExporter instead this method will be removed in a future version.")]
        public static TracerProviderBuilder AddOtlpExporter(
            this TracerProviderBuilder builder,
            string? name,
            Action<OtlpExporterOptions>? configure)
        {
            Guard.ThrowIfNull(builder);

            name ??= Options.DefaultName;

            builder.ConfigureServices(services =>
            {
                services.RegisterOptionsFactory(configuration => new SdkLimitOptions(configuration));
            });

            return builder.ConfigureBuilder((sp, builder) =>
            {
                var exporterOptions = new OtlpExporterOptions(
                    sp.GetRequiredService<IConfiguration>(),
                    sp.GetRequiredService<IOptionsMonitor<BatchExportActivityProcessorOptions>>().Get(name));

                configure?.Invoke(exporterOptions);

                // Note: Not using name here for SdkLimitOptions. There should
                // only be one provider for a given service collection so
                // SdkLimitOptions is treated as a single default instance.
                var sdkOptionsManager = sp.GetRequiredService<IOptionsMonitor<SdkLimitOptions>>().CurrentValue;

                AddOtlpExporter(builder, new(exporterOptions), sdkOptionsManager, sp);
            });
        }

        /// <summary>
        /// Adds <see cref="OtlpTraceExporter"/> to the <see cref="TracerProviderBuilder"/> using default options.
        /// </summary>
        /// <param name="builder"><see cref="TracerProviderBuilder"/> builder to use.</param>
        /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
        public static TracerProviderBuilder AddOtlpTraceExporter(this TracerProviderBuilder builder)
            => AddOtlpTraceExporter(builder, name: null, configureExporterOptions: null);

        /// <summary>
        /// Adds <see cref="OtlpTraceExporter"/> to the <see cref="TracerProviderBuilder"/>.
        /// </summary>
        /// <param name="builder"><see cref="TracerProviderBuilder"/> builder to use.</param>
        /// <param name="configureExporterOptions">Callback action for configuring <see cref="OtlpTraceExporterOptions"/>.</param>
        /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
        public static TracerProviderBuilder AddOtlpTraceExporter(
            this TracerProviderBuilder builder,
            Action<OtlpTraceExporterOptions> configureExporterOptions)
        {
            Guard.ThrowIfNull(configureExporterOptions);

            return AddOtlpTraceExporter(builder, name: null, configureExporterOptions);
        }

        /// <summary>
        /// Adds <see cref="OtlpTraceExporter"/> to the <see cref="TracerProviderBuilder"/>.
        /// </summary>
        /// <param name="builder"><see cref="TracerProviderBuilder"/> builder to use.</param>
        /// <param name="name">Optional name which is used when retrieving options.</param>
        /// <param name="configureExporterOptions">Optional callback action for configuring <see cref="OtlpTraceExporterOptions"/>.</param>
        /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
        public static TracerProviderBuilder AddOtlpTraceExporter(
            this TracerProviderBuilder builder,
            string? name,
            Action<OtlpTraceExporterOptions>? configureExporterOptions)
        {
            Guard.ThrowIfNull(builder);

            name ??= Options.DefaultName;

            builder.ConfigureServices(services =>
            {
                if (configureExporterOptions != null)
                {
                    services.Configure(name, configureExporterOptions);
                }

                services.RegisterOptionsFactory(configuration => new SdkLimitOptions(configuration));
                services.RegisterOptionsFactory(
                    (sp, configuration) => new OtlpTraceExporterOptions(
                        configuration,
                        sp.GetRequiredService<IOptionsMonitor<BatchExportActivityProcessorOptions>>().Get(name)));
            });

            return builder.ConfigureBuilder((sp, builder) =>
            {
                var exporterOptions = sp.GetRequiredService<IOptionsMonitor<OtlpTraceExporterOptions>>().Get(name);

                // Note: Not using name here for SdkLimitOptions. There should
                // only be one provider for a given service collection so
                // SdkLimitOptions is treated as a single default instance.
                var sdkOptionsManager = sp.GetRequiredService<IOptionsMonitor<SdkLimitOptions>>().CurrentValue;

                AddOtlpExporter(builder, exporterOptions, sdkOptionsManager, sp);
            });
        }

        internal static TracerProviderBuilder AddOtlpExporter(
            TracerProviderBuilder builder,
            OtlpTraceExporterOptions exporterOptions,
            SdkLimitOptions sdkLimitOptions,
            IServiceProvider serviceProvider,
            Func<BaseExporter<Activity>, BaseExporter<Activity>>? configureExporterInstance = null)
        {
            exporterOptions.TryEnableIHttpClientFactoryIntegration(serviceProvider, "OtlpTraceExporter");

            BaseExporter<Activity> otlpExporter = new OtlpTraceExporter(exporterOptions, sdkLimitOptions);

            if (configureExporterInstance != null)
            {
                otlpExporter = configureExporterInstance(otlpExporter);
            }

            if (exporterOptions.ExportProcessorType == ExportProcessorType.Simple)
            {
                return builder.AddProcessor(new SimpleActivityExportProcessor(otlpExporter));
            }
            else
            {
                var batchOptions = exporterOptions.BatchExportProcessorOptions ?? new BatchExportActivityProcessorOptions();

                return builder.AddProcessor(new BatchActivityExportProcessor(
                    otlpExporter,
                    batchOptions.MaxQueueSize,
                    batchOptions.ScheduledDelayMilliseconds,
                    batchOptions.ExporterTimeoutMilliseconds,
                    batchOptions.MaxExportBatchSize));
            }
        }
    }
}
