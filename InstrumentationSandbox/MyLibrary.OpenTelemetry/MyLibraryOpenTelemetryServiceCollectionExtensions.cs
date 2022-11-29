using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MyLibrary;
using MyLibrary.Telemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

namespace Microsoft.Extensions.DependencyInjection;

public static class MyLibraryOpenTelemetryServiceCollectionExtensions
{
    public static MyLibraryBuilder WithOpenTelemetry(this MyLibraryBuilder myLibraryBuilder)
        => WithOpenTelemetry(myLibraryBuilder, name: null, configure: null);

    public static MyLibraryBuilder WithOpenTelemetry(this MyLibraryBuilder myLibraryBuilder, Action<MyLibraryTelemetryOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return WithOpenTelemetry(myLibraryBuilder, name: null, configure);
    }

    public static MyLibraryBuilder WithOpenTelemetry(
        this MyLibraryBuilder myLibraryBuilder,
        string? name,
        Action<MyLibraryTelemetryOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(myLibraryBuilder);

        name ??= Options.Options.DefaultName;

        myLibraryBuilder.Services.AddOptions();

        if (configure != null)
        {
            myLibraryBuilder.Services.Configure(name, configure);
        }

        MyLibraryTelemetryOptions? options = null;

        myLibraryBuilder.Services.ConfigureOpenTelemetryTracing(builder => builder.ConfigureBuilder((sp, builder) =>
        {
            options ??= GetOptions(sp, name);

            if (options.TracingEnabled)
            {
                builder.AddSource(MyService.TelemetryName);
            }
        }));

        myLibraryBuilder.Services.ConfigureOpenTelemetryMetrics(builder => builder.ConfigureBuilder((sp, builder) =>
        {
            options ??= GetOptions(sp, name);

            if (options.MetricsEnabled)
            {
                builder.AddMeter(MyService.TelemetryName);
            }
        }));

        myLibraryBuilder.Services.TryAdd(ServiceDescriptor.Singleton<IMyServiceTelemetry, OpenTelemetryMyServiceTelemetry>(sp =>
        {
            options ??= GetOptions(sp, name);

            return new OpenTelemetryMyServiceTelemetry(options);
        }));

        return myLibraryBuilder;

        static MyLibraryTelemetryOptions GetOptions(IServiceProvider serviceProvider, string name)
        {
            return serviceProvider.GetRequiredService<IOptionsMonitor<MyLibraryTelemetryOptions>>().Get(name);
        }
    }
}
