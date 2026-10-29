using Microsoft.Extensions.DependencyInjection.Extensions;
using Trailblazor;
using Trailblazor.Primitives;

namespace Microsoft.Extensions.DependencyInjection;

public static class TrailblazorServiceCollectionExtensions
{
    public static IServiceCollection AddTrailblazor(this IServiceCollection services)
    {
        services.TryAddScoped<JsModuleLoader>();
        services.TryAddScoped<DialogService>();
        services.TryAddScoped<IDialogService>(provider => provider.GetRequiredService<DialogService>());
        return services;
    }
}
