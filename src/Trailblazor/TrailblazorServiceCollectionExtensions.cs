using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Trailblazor;
using Trailblazor.Internal.Events;
using Trailblazor.Primitives;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers Trailblazor's services.</summary>
public static class TrailblazorServiceCollectionExtensions
{
    private const DynamicallyAccessedMemberTypes Kept =
        DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties;

    // Blazor uses reflection to read custom event arguments and viewport changes from JSON and to write move checks
    // back to the tree's and sortable list's scripts, so trimming must keep their constructors and properties.
    /// <summary>Registers Trailblazor's dialog, toast, announcer, browser, localizer, theme and telemetry services,
    /// and applies <paramref name="configure"/> to the app-wide <see cref="TrailblazorOptions"/>.</summary>
    [DynamicDependency(Kept, typeof(NumberChangeEventArgs))]
    [DynamicDependency(Kept, typeof(NumberStepEventArgs))]
    [DynamicDependency(Kept, typeof(FieldFocusEventArgs))]
    [DynamicDependency(Kept, typeof(RowClickEventArgs))]
    [DynamicDependency(Kept, typeof(RowExtendEventArgs))]
    [DynamicDependency(Kept, typeof(GridEditKeyEventArgs))]
    [DynamicDependency(Kept, typeof(ColumnWidthsEventArgs))]
    [DynamicDependency(Kept, typeof(ColumnOrderEventArgs))]
    [DynamicDependency(Kept, typeof(RichTextChangeEventArgs))]
    [DynamicDependency(Kept, typeof(RichTextLinkEventArgs))]
    [DynamicDependency(Kept, typeof(TagAddEventArgs))]
    [DynamicDependency(Kept, typeof(TagRemoveEventArgs))]
    [DynamicDependency(Kept, typeof(TreeDropEventArgs))]
    [DynamicDependency(Kept, typeof(MoveCheck))]
    [DynamicDependency(Kept, typeof(Viewport))]
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
        services.TryAddScoped<ITbLocalizer, TbLocalizer>();
        services.TryAddScoped<IThemeService, ThemeService>();
        services.AddMetrics();
        services.TryAddSingleton<TelemetryInstruments>();
        services.TryAddSingleton<SeededPalettes>();
        services.TryAddScoped<Telemetry>();
        return services;
    }
}
