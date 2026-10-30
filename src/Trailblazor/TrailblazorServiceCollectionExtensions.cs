using Microsoft.Extensions.DependencyInjection.Extensions;
using Trailblazor;
using Trailblazor.Primitives;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers Trailblazor's services.</summary>
public static class TrailblazorServiceCollectionExtensions
{
    /// <summary>Registers Trailblazor's dialog, toast, announcer and browser services, and applies
    /// <paramref name="configure"/> to the app-wide <see cref="TrailblazorOptions"/>.</summary>
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
        services.TryAddScoped<IBrowserService, BrowserService>();
        return services;
    }
}
