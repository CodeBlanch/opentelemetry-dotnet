using Microsoft.Extensions.DependencyInjection.Extensions;
using MyLibrary;

namespace Microsoft.Extensions.DependencyInjection;

public static class MyLibraryServiceCollectionExtensions
{
    public static MyLibraryBuilder AddMyLibrary(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IMyService, MyService>();

        return new()
        {
            Services = services
        };
    }
}
