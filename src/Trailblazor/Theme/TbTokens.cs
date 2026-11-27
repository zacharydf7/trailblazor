namespace Trailblazor;

/// <summary>The CSS custom properties an app may set or read. Every other property the library uses starts with
/// <c>--_tb-</c>: it belongs to a component's own workings and may change in any release.</summary>
public static class TbTokens
{
    /// <summary>The theme's tokens, which the root, a palette or a density sets: colors and their tonal ramps, type,
    /// spacing, radii, shadows, motion, density and breakpoints. Set any of them on the root or an element to restyle
    /// everything under it.</summary>
    public static IReadOnlyList<string> Theme { get; } =
    [
        "--tb-gray-50", "--tb-gray-100", "--tb-gray-200", "--tb-gray-300", "--tb-gray-400", "--tb-gray-500",
        "--tb-gray-600", "--tb-gray-700", "--tb-gray-800", "--tb-gray-900", "--tb-gray-925", "--tb-gray-950",
        "--tb-gray-960", "--tb-gray-975", "--tb-gray-985", "--tb-graphite-50", "--tb-graphite-100", "--tb-graphite-200",
        "--tb-graphite-300", "--tb-graphite-400", "--tb-graphite-500", "--tb-graphite-600", "--tb-graphite-700",
        "--tb-graphite-800", "--tb-graphite-900", "--tb-graphite-925", "--tb-graphite-950", "--tb-graphite-960",
        "--tb-graphite-975", "--tb-graphite-985", "--tb-blue-50", "--tb-blue-100", "--tb-blue-200", "--tb-blue-300",
        "--tb-blue-400", "--tb-blue-500", "--tb-blue-600", "--tb-blue-700", "--tb-blue-800", "--tb-blue-900",
        "--tb-blue-950", "--tb-green-50", "--tb-green-100", "--tb-green-200", "--tb-green-300", "--tb-green-400",
        "--tb-green-500", "--tb-green-600", "--tb-green-700", "--tb-green-800", "--tb-green-900", "--tb-green-950",
        "--tb-amber-50", "--tb-amber-100", "--tb-amber-200", "--tb-amber-300", "--tb-amber-400", "--tb-amber-500",
        "--tb-amber-600", "--tb-amber-700", "--tb-amber-800", "--tb-amber-900", "--tb-amber-950", "--tb-red-50",
        "--tb-red-100", "--tb-red-200", "--tb-red-300", "--tb-red-400", "--tb-red-500", "--tb-red-600", "--tb-red-700",
        "--tb-red-800", "--tb-red-900", "--tb-red-950", "--tb-sky-50", "--tb-sky-100", "--tb-sky-200", "--tb-sky-300",
        "--tb-sky-400", "--tb-sky-500", "--tb-sky-600", "--tb-sky-700", "--tb-sky-800", "--tb-sky-900", "--tb-sky-950",
        "--tb-stone-50", "--tb-stone-100", "--tb-stone-200", "--tb-stone-300", "--tb-stone-400", "--tb-stone-500",
        "--tb-stone-600", "--tb-stone-700", "--tb-stone-800", "--tb-stone-900", "--tb-stone-925", "--tb-stone-950",
        "--tb-stone-960", "--tb-stone-975", "--tb-stone-985", "--tb-mauve-50", "--tb-mauve-100", "--tb-mauve-200",
        "--tb-mauve-300", "--tb-mauve-400", "--tb-mauve-500", "--tb-mauve-600", "--tb-mauve-700", "--tb-mauve-800",
        "--tb-mauve-900", "--tb-mauve-925", "--tb-mauve-950", "--tb-mauve-960", "--tb-mauve-975", "--tb-mauve-985",
        "--tb-sage-50", "--tb-sage-100", "--tb-sage-200", "--tb-sage-300", "--tb-sage-400", "--tb-sage-500",
        "--tb-sage-600", "--tb-sage-700", "--tb-sage-800", "--tb-sage-900", "--tb-sage-925", "--tb-sage-950",
        "--tb-sage-960", "--tb-sage-975", "--tb-sage-985", "--tb-clay-50", "--tb-clay-100", "--tb-clay-200",
        "--tb-clay-300", "--tb-clay-400", "--tb-clay-500", "--tb-clay-600", "--tb-clay-700", "--tb-clay-800",
        "--tb-clay-900", "--tb-clay-950", "--tb-indigo-50", "--tb-indigo-100", "--tb-indigo-200", "--tb-indigo-300",
        "--tb-indigo-400", "--tb-indigo-500", "--tb-indigo-600", "--tb-indigo-700", "--tb-indigo-800",
        "--tb-indigo-900", "--tb-indigo-950", "--tb-violet-50", "--tb-violet-100", "--tb-violet-200", "--tb-violet-300",
        "--tb-violet-400", "--tb-violet-500", "--tb-violet-600", "--tb-violet-700", "--tb-violet-800",
        "--tb-violet-900", "--tb-violet-950", "--tb-plum-50", "--tb-plum-100", "--tb-plum-200", "--tb-plum-300",
        "--tb-plum-400", "--tb-plum-500", "--tb-plum-600", "--tb-plum-700", "--tb-plum-800", "--tb-plum-900",
        "--tb-plum-950", "--tb-fuchsia-50", "--tb-fuchsia-100", "--tb-fuchsia-200", "--tb-fuchsia-300",
        "--tb-fuchsia-400", "--tb-fuchsia-500", "--tb-fuchsia-600", "--tb-fuchsia-700", "--tb-fuchsia-800",
        "--tb-fuchsia-900", "--tb-fuchsia-950", "--tb-rose-50", "--tb-rose-100", "--tb-rose-200", "--tb-rose-300",
        "--tb-rose-400", "--tb-rose-500", "--tb-rose-600", "--tb-rose-700", "--tb-rose-800", "--tb-rose-900",
        "--tb-rose-950", "--tb-moss-50", "--tb-moss-100", "--tb-moss-200", "--tb-moss-300", "--tb-moss-400",
        "--tb-moss-500", "--tb-moss-600", "--tb-moss-700", "--tb-moss-800", "--tb-moss-900", "--tb-moss-950",
        "--tb-teal-50", "--tb-teal-100", "--tb-teal-200", "--tb-teal-300", "--tb-teal-400", "--tb-teal-500",
        "--tb-teal-600", "--tb-teal-700", "--tb-teal-800", "--tb-teal-900", "--tb-teal-950", "--tb-color-success",
        "--tb-color-success-hover", "--tb-color-success-active", "--tb-color-on-success", "--tb-color-success-subtle",
        "--tb-color-success-text", "--tb-color-warning", "--tb-color-warning-hover", "--tb-color-warning-active",
        "--tb-color-on-warning", "--tb-color-warning-subtle", "--tb-color-warning-text", "--tb-color-danger",
        "--tb-color-danger-hover", "--tb-color-danger-active", "--tb-color-on-danger", "--tb-color-danger-subtle",
        "--tb-color-danger-text", "--tb-color-info", "--tb-color-info-hover", "--tb-color-info-active",
        "--tb-color-on-info", "--tb-color-info-subtle", "--tb-color-info-text", "--tb-focus-ring-width",
        "--tb-focus-ring-offset", "--tb-disabled-opacity", "--tb-color-scrim", "--tb-font-family",
        "--tb-font-family-mono", "--tb-font-size-xs", "--tb-font-size-sm", "--tb-font-size-md", "--tb-font-size-lg",
        "--tb-font-size-xl", "--tb-font-size-2xl", "--tb-font-size-3xl", "--tb-font-weight-regular",
        "--tb-font-weight-medium", "--tb-font-weight-semibold", "--tb-font-weight-bold", "--tb-line-height-tight",
        "--tb-line-height-normal", "--tb-space-0", "--tb-space-1", "--tb-space-2", "--tb-space-3", "--tb-space-4",
        "--tb-space-5", "--tb-space-6", "--tb-space-8", "--tb-space-10", "--tb-space-12", "--tb-space-16",
        "--tb-radius-sm", "--tb-radius-md", "--tb-radius-lg", "--tb-radius-xl", "--tb-radius-full", "--tb-shadow-sm",
        "--tb-shadow-md", "--tb-shadow-lg", "--tb-chart-series-1", "--tb-chart-series-2", "--tb-chart-series-3",
        "--tb-chart-series-4", "--tb-chart-series-5", "--tb-chart-series-6", "--tb-chart-series-7",
        "--tb-chart-series-8", "--tb-motion-duration-fast", "--tb-motion-duration-normal",
        "--tb-motion-duration-slow", "--tb-motion-easing-standard", "--tb-motion-easing-emphasized",
        "--tb-breakpoint-sm", "--tb-breakpoint-md", "--tb-breakpoint-lg", "--tb-breakpoint-xl", "--tb-control-height",
        "--tb-control-padding-block", "--tb-control-padding-inline", "--tb-item-height", "--tb-row-height",
        "--tb-control-height-sm", "--tb-control-height-lg", "--tb-font-family-heading", "--tb-color-background",
        "--tb-color-surface", "--tb-color-surface-raised", "--tb-color-surface-sunken", "--tb-color-text",
        "--tb-color-text-muted", "--tb-color-text-disabled", "--tb-border-subtle", "--tb-border-strong",
        "--tb-color-primary", "--tb-color-on-primary", "--tb-color-secondary", "--tb-color-on-secondary",
        "--tb-color-neutral", "--tb-color-neutral-hover", "--tb-color-neutral-active", "--tb-color-on-neutral",
        "--tb-color-neutral-subtle", "--tb-color-neutral-text", "--tb-color-primary-hover", "--tb-color-primary-active",
        "--tb-color-primary-subtle", "--tb-color-primary-text", "--tb-focus-ring-color", "--tb-color-secondary-hover",
        "--tb-color-secondary-active", "--tb-color-secondary-subtle", "--tb-color-secondary-text", "--tb-color-hover",
        "--tb-color-pressed",
    ];

    /// <summary>The tokens an app may set to restyle one kind of component, such as <c>--tb-button-height</c>. Each
    /// has a default until it is set.</summary>
    public static IReadOnlyList<string> Component { get; } =
    [
        "--tb-app-bar-height", "--tb-app-shell-height", "--tb-app-shell-navigation-width", "--tb-app-shell-rail-width",
        "--tb-barcode-background", "--tb-barcode-color", "--tb-blocked-opacity", "--tb-button-height",
        "--tb-button-radius", "--tb-carousel-duration", "--tb-chart-height", "--tb-chat-max-height",
        "--tb-data-grid-row-height", "--tb-data-grid-stripe-color", "--tb-dialog-radius", "--tb-dialog-z-index",
        "--tb-fab-offset",
        "--tb-fab-radius", "--tb-fab-z-index", "--tb-field-background", "--tb-field-border-color",
        "--tb-field-border-width", "--tb-field-height", "--tb-field-radius", "--tb-kanban-column-width",
        "--tb-lightbox-background", "--tb-menu-item-font-size", "--tb-menu-item-height", "--tb-menu-item-icon-size",
        "--tb-menu-radius", "--tb-pie-diameter", "--tb-popover-max-width", "--tb-popover-radius",
        "--tb-qr-code-background",
        "--tb-qr-code-color", "--tb-ribbon-context-color", "--tb-sparkline-height", "--tb-sparkline-width",
        "--tb-table-row-height", "--tb-table-sticky-top",
        "--tb-table-stripe-color", "--tb-tabs-indicator-color", "--tb-tabs-indicator-size", "--tb-tooltip-max-width",
        "--tb-tour-z-index", "--tb-tree-insert-band", "--tb-tree-insert-offset", "--tb-tree-item-height",
        "--tb-tree-item-radius", "--tb-tree-text-min-width", "--tb-watermark-color", "--tb-watermark-opacity",
    ];

    /// <summary>The values components set for an app to read, such as <c>--tb-scroll-lock-offset</c>, which an app's
    /// own fixed elements can add to their <c>right</c> so they hold still while a dialog locks the page's
    /// scrolling.</summary>
    public static IReadOnlyList<string> SetByComponents { get; } =
    [
        "--tb-dock-z", "--tb-scroll-lock-offset", "--tb-scroll-lock-offset-left",
    ];

    /// <summary>The classes an app may put on its own elements, such as <c>tb-visually-hidden</c>, which hides text
    /// from sight but not from screen readers.</summary>
    public static IReadOnlyList<string> Classes { get; } = ["tb-visually-hidden"];
}
