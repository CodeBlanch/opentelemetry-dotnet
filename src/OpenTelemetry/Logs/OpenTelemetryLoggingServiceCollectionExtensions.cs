using System;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Internal;

namespace OpenTelemetry.Logs
{
    public static class OpenTelemetryLoggingServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureOpenTelemetryLoggerProvider(
            this IServiceCollection services,
            Action<OpenTelemetryLoggerProvider> configure)
        {
            Guard.ThrowIfNull(configure);

            return ConfigureOpenTelemetryLoggerProvider(services, (sp, provider) => configure(provider));
        }

        public static IServiceCollection ConfigureOpenTelemetryLoggerProvider(
            this IServiceCollection services,
            Action<IServiceProvider, OpenTelemetryLoggerProvider> configure)
        {
            Guard.ThrowIfNull(services);
            Guard.ThrowIfNull(configure);

            return services.AddSingleton(new LoggerProviderConfigureRegistration(configure));
        }

        internal sealed class LoggerProviderConfigureRegistration
        {
            public LoggerProviderConfigureRegistration(Action<IServiceProvider, OpenTelemetryLoggerProvider> configure)
            {
                this.Configure = configure;
            }

            public Action<IServiceProvider, OpenTelemetryLoggerProvider> Configure { get; }
        }
    }
}
