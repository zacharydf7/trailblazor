using Microsoft.Extensions.DependencyInjection.Extensions;
using Trailblazor.Primitives;

namespace Microsoft.Extensions.DependencyInjection;

public static class TrailblazorServiceCollectionExtensions
{
    public static IServiceCollection AddTrailblazor(this IServiceCollection services)
    {
        services.TryAddScoped<JsModuleLoader>();
        return services;
    }
}
