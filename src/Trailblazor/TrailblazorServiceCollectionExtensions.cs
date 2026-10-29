using Microsoft.Extensions.DependencyInjection.Extensions;
using Trailblazor;
using Trailblazor.Primitives;

namespace Microsoft.Extensions.DependencyInjection;

public static class TrailblazorServiceCollectionExtensions
{
    public static IServiceCollection AddTrailblazor(this IServiceCollection services,
        Action<TrailblazorOptions>? configure = null)
    {
        services.AddOptions<TrailblazorOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<JsModuleLoader>();
        services.TryAddScoped<DialogService>();
        services.TryAddScoped<IDialogService>(provider => provider.GetRequiredService<DialogService>());
        services.TryAddScoped<Announcer>();
        services.TryAddScoped<IAnnouncer>(provider => provider.GetRequiredService<Announcer>());
        services.TryAddScoped<ToastService>();
        services.TryAddScoped<IToastService>(provider => provider.GetRequiredService<ToastService>());
        return services;
    }
}
