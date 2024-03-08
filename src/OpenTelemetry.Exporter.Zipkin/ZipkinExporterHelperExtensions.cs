// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#nullable enable

#if NETFRAMEWORK
using System.Net.Http;
#endif
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Internal;

namespace OpenTelemetry.Trace;

/// <summary>
/// Extension methods to simplify registering of Zipkin exporter.
/// </summary>
public static class ZipkinExporterHelperExtensions
{
    /// <summary>
    /// Adds Zipkin exporter to the TracerProvider.
    /// </summary>
    /// <param name="builder"><see cref="TracerProviderBuilder"/> builder to use.</param>
    /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
    public static TracerProviderBuilder AddZipkinExporter(this TracerProviderBuilder builder)
        => AddZipkinExporter(builder, name: null, configure: null);

    /// <summary>
    /// Adds Zipkin exporter to the TracerProvider.
    /// </summary>
    /// <param name="builder"><see cref="TracerProviderBuilder"/> builder to use.</param>
    /// <param name="configure">Callback action for configuring <see cref="ZipkinExporterOptions"/>.</param>
    /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
    public static TracerProviderBuilder AddZipkinExporter(this TracerProviderBuilder builder, Action<ZipkinExporterOptions> configure)
        => AddZipkinExporter(builder, name: null, configure);

    /// <summary>
    /// Adds Zipkin exporter to the TracerProvider.
    /// </summary>
    /// <param name="builder"><see cref="TracerProviderBuilder"/> builder to use.</param>
    /// <param name="name">Optional name which is used when retrieving
    /// options.</param>
    /// <param name="configure">Optional callback action for configuring <see
    /// cref="ZipkinExporterOptions"/>.</param>
    /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
    public static TracerProviderBuilder AddZipkinExporter(
        this TracerProviderBuilder builder,
        string? name,
        Action<ZipkinExporterOptions>? configure)
    {
        Guard.ThrowIfNull(builder);

        name ??= Options.DefaultName;

        builder.ConfigureServices(services =>
        {
            if (configure != null)
            {
                services.Configure(name, configure);
            }

            RegisterOptions(services);
        });

        return builder.AddProcessor(sp =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<ZipkinExporterOptions>>().Get(name);

#pragma warning disable CS0618 // Type or member is obsolete
            return BuildZipkinExporterProcessor(
                sp,
                options.ExportProcessorType,
                options.BatchExportProcessorOptions,
                options);
#pragma warning restore CS0618 // Type or member is obsolete
        });
    }

    /// <summary>
    /// Adds Zipkin exporter to the TracerProvider.
    /// </summary>
    /// <param name="builder"><see cref="TracerProviderBuilder"/> builder to use.</param>
    /// <param name="configureExporterAndProcessor">Callback action for configuring <see cref="ZipkinExporterOptions"/> and <see cref="ActivityExportProcessorOptions"/>.</param>
    /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
    public static TracerProviderBuilder AddZipkinExporter(
        this TracerProviderBuilder builder,
        Action<ZipkinExporterOptions, ActivityExportProcessorOptions> configureExporterAndProcessor)
        => AddZipkinExporter(builder, name: null, configureExporterAndProcessor);

    /// <summary>
    /// Adds Zipkin exporter to the TracerProvider.
    /// </summary>
    /// <param name="builder"><see cref="TracerProviderBuilder"/> builder to use.</param>
    /// <param name="name">Optional name which is used when retrieving
    /// options.</param>
    /// <param name="configureExporterAndProcessor">Optional callback action for
    /// configuring <see cref="ZipkinExporterOptions"/> and <see
    /// cref="ActivityExportProcessorOptions"/>.</param>
    /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
    public static TracerProviderBuilder AddZipkinExporter(
        this TracerProviderBuilder builder,
        string? name,
        Action<ZipkinExporterOptions, ActivityExportProcessorOptions>? configureExporterAndProcessor)
    {
        var finalOptionsName = name ?? Options.DefaultName;

        builder.ConfigureServices(RegisterOptions);

        return builder.AddProcessor(sp =>
        {
            var exporterOptions = sp.GetRequiredService<IOptionsMonitor<ZipkinExporterOptions>>().Get(finalOptionsName);

            var processorOptions = sp.GetRequiredService<IOptionsMonitor<ActivityExportProcessorOptions>>().Get(finalOptionsName);

            // Configuration delegate is executed inline.
            configureExporterAndProcessor?.Invoke(exporterOptions, processorOptions);

            return BuildZipkinExporterProcessor(
                sp,
                processorOptions.ExportProcessorType,
                processorOptions.BatchExportProcessorOptions,
                exporterOptions);
        });
    }

    private static void RegisterOptions(IServiceCollection services)
    {
        services.RegisterOptionsFactory(
            (sp, configuration, name) => new ZipkinExporterOptions(
                configuration,
                sp.GetRequiredService<IOptionsMonitor<ActivityExportProcessorOptions>>().Get(name)));
    }

    private static BaseProcessor<Activity> BuildZipkinExporterProcessor(
        IServiceProvider serviceProvider,
        ExportProcessorType processorType,
        BatchExportProcessorOptions<Activity> batchExportProcessorOptions,
        ZipkinExporterOptions exporterOptions)
    {
        if (exporterOptions.HttpClientFactory == ZipkinExporterOptions.DefaultHttpClientFactory)
        {
            exporterOptions.HttpClientFactory = () =>
            {
                var httpClientFactoryType = Type.GetType("System.Net.Http.IHttpClientFactory, Microsoft.Extensions.Http", throwOnError: false);
                if (httpClientFactoryType != null)
                {
                    var httpClientFactory = serviceProvider.GetService(httpClientFactoryType);
                    if (httpClientFactory != null)
                    {
                        var createClientMethod = httpClientFactoryType.GetMethod(
                            "CreateClient",
                            BindingFlags.Public | BindingFlags.Instance,
                            binder: null,
                            new Type[] { typeof(string) },
                            modifiers: null);
                        if (createClientMethod != null)
                        {
                            return (HttpClient)createClientMethod.Invoke(httpClientFactory, new object[] { "ZipkinExporter" })!;
                        }
                    }
                }

                return new HttpClient();
            };
        }

        var zipkinExporter = new ZipkinExporter(exporterOptions);

        if (processorType == ExportProcessorType.Simple)
        {
            return new SimpleActivityExportProcessor(zipkinExporter);
        }
        else
        {
            return new BatchActivityExportProcessor(
                zipkinExporter,
                batchExportProcessorOptions.MaxQueueSize,
                batchExportProcessorOptions.ScheduledDelayMilliseconds,
                batchExportProcessorOptions.ExporterTimeoutMilliseconds,
                batchExportProcessorOptions.MaxExportBatchSize);
        }
    }
}
