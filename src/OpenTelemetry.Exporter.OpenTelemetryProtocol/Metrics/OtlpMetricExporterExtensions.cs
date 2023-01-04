// <copyright file="OtlpMetricExporterExtensions.cs" company="OpenTelemetry Authors">
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Internal;

namespace OpenTelemetry.Metrics
{
    /// <summary>
    /// Extension methods to simplify registering of the OpenTelemetry Protocol (OTLP) exporter.
    /// </summary>
    public static class OtlpMetricExporterExtensions
    {
        /// <summary>
        /// Adds <see cref="OtlpMetricExporter"/> to the <see cref="MeterProviderBuilder"/> using default options.
        /// </summary>
        /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
        /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
        [Obsolete("Use AddOtlpMetricExporter instead this method will be removed in a future version.")]
        public static MeterProviderBuilder AddOtlpExporter(this MeterProviderBuilder builder)
            => AddOtlpExporter(builder, name: null, configureExporter: null);

        /// <summary>
        /// Adds <see cref="OtlpMetricExporter"/> to the <see cref="MeterProviderBuilder"/>.
        /// </summary>
        /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
        /// <param name="configureExporter">Callback action for configuring <see cref="OtlpExporterOptions"/>.</param>
        /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
        [Obsolete("Use AddOtlpMetricExporter instead this method will be removed in a future version.")]
        public static MeterProviderBuilder AddOtlpExporter(this MeterProviderBuilder builder, Action<OtlpExporterOptions> configureExporter)
            => AddOtlpExporter(builder, name: null, configureExporter);

        /// <summary>
        /// Adds <see cref="OtlpMetricExporter"/> to the <see cref="MeterProviderBuilder"/>.
        /// </summary>
        /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
        /// <param name="name">Name which is used when retrieving options.</param>
        /// <param name="configureExporter">Callback action for configuring <see cref="OtlpExporterOptions"/>.</param>
        /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
        [Obsolete("Use AddOtlpMetricExporter instead this method will be removed in a future version.")]
        public static MeterProviderBuilder AddOtlpExporter(
            this MeterProviderBuilder builder,
            string? name,
            Action<OtlpExporterOptions>? configureExporter)
        {
            Guard.ThrowIfNull(builder);

            name ??= Options.DefaultName;

            return builder.ConfigureBuilder((sp, builder) =>
            {
                var options = new OtlpExporterOptions(
                    sp.GetRequiredService<IConfiguration>(),
                    defaultBatchOptions: null);

                configureExporter?.Invoke(options);

                AddOtlpExporter(
                    builder,
                    new(options),
                    sp.GetRequiredService<IOptionsMonitor<MetricReaderOptions>>().Get(name),
                    sp);
            });
        }

        /// <summary>
        /// Adds <see cref="OtlpMetricExporter"/> to the <see cref="MeterProviderBuilder"/>.
        /// </summary>
        /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
        /// <param name="configureExporterAndMetricReader">Callback action for
        /// configuring <see cref="OtlpExporterOptions"/> and <see
        /// cref="MetricReaderOptions"/>.</param>
        /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
        [Obsolete("Use AddOtlpMetricExporter instead this method will be removed in a future version.")]
        public static MeterProviderBuilder AddOtlpExporter(
            this MeterProviderBuilder builder,
            Action<OtlpExporterOptions, MetricReaderOptions> configureExporterAndMetricReader)
            => AddOtlpExporter(builder, name: null, configureExporterAndMetricReader);

        /// <summary>
        /// Adds <see cref="OtlpMetricExporter"/> to the <see cref="MeterProviderBuilder"/>.
        /// </summary>
        /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
        /// <param name="name">Name which is used when retrieving options.</param>
        /// <param name="configureExporterAndMetricReader">Callback action for
        /// configuring <see cref="OtlpExporterOptions"/> and <see
        /// cref="MetricReaderOptions"/>.</param>
        /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
        [Obsolete("Use AddOtlpMetricExporter instead this method will be removed in a future version.")]
        public static MeterProviderBuilder AddOtlpExporter(
            this MeterProviderBuilder builder,
            string? name,
            Action<OtlpExporterOptions, MetricReaderOptions>? configureExporterAndMetricReader)
        {
            Guard.ThrowIfNull(builder);

            name ??= Options.DefaultName;

            return builder.ConfigureBuilder((sp, builder) =>
            {
                var exporterOptions = new OtlpExporterOptions(
                    sp.GetRequiredService<IConfiguration>(),
                    defaultBatchOptions: null);

                var metricReaderOptions = sp.GetRequiredService<IOptionsMonitor<MetricReaderOptions>>().Get(name);

                configureExporterAndMetricReader?.Invoke(exporterOptions, metricReaderOptions);

                AddOtlpExporter(builder, new(exporterOptions), metricReaderOptions, sp);
            });
        }

        /// <summary>
        /// Adds <see cref="OtlpMetricExporter"/> to the <see cref="MeterProviderBuilder"/> using default options.
        /// </summary>
        /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
        /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
        public static MeterProviderBuilder AddOtlpMetricExporter(this MeterProviderBuilder builder)
            => AddOtlpMetricExporter(builder, name: null, configureExporterOptions: null, configureReaderOptions: null);

        /// <summary>
        /// Adds <see cref="OtlpMetricExporter"/> to the <see cref="MeterProviderBuilder"/>.
        /// </summary>
        /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
        /// <param name="configureExporterOptions">Callback action for configuring <see cref="OtlpMetricExporterOptions"/>.</param>
        /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
        public static MeterProviderBuilder AddOtlpMetricExporter(
            this MeterProviderBuilder builder,
            Action<OtlpMetricExporterOptions> configureExporterOptions)
        {
            Guard.ThrowIfNull(configureExporterOptions);

            return AddOtlpMetricExporter(builder, name: null, configureExporterOptions, configureReaderOptions: null);
        }

        /// <summary>
        /// Adds <see cref="OtlpMetricExporter"/> to the <see cref="MeterProviderBuilder"/>.
        /// </summary>
        /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
        /// <param name="configureExporterOptions">Callback action for configuring <see cref="OtlpMetricExporterOptions"/>.</param>
        /// <param name="configureReaderOptions">Callback action for configuring <see cref="MetricReaderOptions"/>.</param>
        /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
        public static MeterProviderBuilder AddOtlpMetricExporter(
            this MeterProviderBuilder builder,
            Action<OtlpMetricExporterOptions> configureExporterOptions,
            Action<MetricReaderOptions> configureReaderOptions)
        {
            Guard.ThrowIfNull(configureExporterOptions);
            Guard.ThrowIfNull(configureReaderOptions);

            return AddOtlpMetricExporter(builder, name: null, configureExporterOptions, configureReaderOptions);
        }

        /// <summary>
        /// Adds <see cref="OtlpMetricExporter"/> to the <see cref="MeterProviderBuilder"/>.
        /// </summary>
        /// <param name="builder"><see cref="MeterProviderBuilder"/> builder to use.</param>
        /// <param name="name">Optional name which is used when retrieving options.</param>
        /// <param name="configureExporterOptions">Optional callback action for configuring <see cref="OtlpMetricExporterOptions"/>.</param>
        /// <param name="configureReaderOptions">Optional callback action for configuring <see cref="MetricReaderOptions"/>.</param>
        /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
        public static MeterProviderBuilder AddOtlpMetricExporter(
            this MeterProviderBuilder builder,
            string? name,
            Action<OtlpMetricExporterOptions>? configureExporterOptions,
            Action<MetricReaderOptions>? configureReaderOptions)
        {
            Guard.ThrowIfNull(builder);

            name ??= Options.DefaultName;

            builder.ConfigureServices(services =>
            {
                if (configureExporterOptions != null)
                {
                    services.Configure(name, configureExporterOptions);
                }

                if (configureReaderOptions != null)
                {
                    services.Configure(name, configureReaderOptions);
                }

                services.RegisterOptionsFactory(configuration
                    => new OtlpMetricExporterOptions(configuration));
            });

            return builder.ConfigureBuilder((sp, builder) =>
            {
                var exporterOptions = sp.GetRequiredService<IOptionsMonitor<OtlpMetricExporterOptions>>().Get(name);
                var metricReaderOptions = sp.GetRequiredService<IOptionsMonitor<MetricReaderOptions>>().Get(name);

                AddOtlpExporter(builder, exporterOptions, metricReaderOptions, sp);
            });
        }

        internal static MeterProviderBuilder AddOtlpExporter(
            MeterProviderBuilder builder,
            OtlpMetricExporterOptions exporterOptions,
            MetricReaderOptions metricReaderOptions,
            IServiceProvider serviceProvider,
            Func<BaseExporter<Metric>, BaseExporter<Metric>>? configureExporterInstance = null)
        {
            exporterOptions.TryEnableIHttpClientFactoryIntegration(serviceProvider, "OtlpMetricExporter");

            BaseExporter<Metric> metricExporter = new OtlpMetricExporter(exporterOptions);

            if (configureExporterInstance != null)
            {
                metricExporter = configureExporterInstance(metricExporter);
            }

            var metricReader = PeriodicExportingMetricReaderHelper.CreatePeriodicExportingMetricReader(
                metricExporter,
                metricReaderOptions);

            return builder.AddReader(metricReader);
        }
    }
}
