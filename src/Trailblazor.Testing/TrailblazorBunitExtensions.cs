using System.Reflection;
using System.Runtime.CompilerServices;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Trailblazor.Testing;

/// <summary>bUnit setup for testing components built with Trailblazor.</summary>
public static class TrailblazorBunitExtensions
{
    // A page has one TbRoot, so a test does too, however many dialogs it opens.
    private static readonly ConditionalWeakTable<BunitContext, IRenderedComponent<TbRoot>> Roots = new();

    /// <summary>Registers Trailblazor's services and answers its JavaScript modules, so components render and respond
    /// to clicks in bUnit with no JSInterop setup. Calls into the modules return defaults, which Trailblazor's
    /// components take to mean the browser part isn't there. <see cref="IBrowserService"/> is a
    /// <see cref="FakeBrowserService"/>, to check and steer from the test, and <see cref="IDialogService"/> is a
    /// <see cref="FakeDialogService"/>, which records the dialogs opened. A module or service set up by the test
    /// itself still wins. Components render as on an interactive page: the renderer becomes an interactive Server one
    /// when the first component is created, unless the test has called <c>SetRendererInfo</c> itself, as it does to
    /// test what a static page shows; that call starts the services, so make it after the test registers its own
    /// services. The interactive default is a component factory, so clearing <c>ComponentFactories</c> removes it. Call
    /// this before rendering, since bUnit takes no new services once one has been resolved.</summary>
    public static BunitContext AddTrailblazor(this BunitContext context, Action<TrailblazorOptions>? configure = null)
    {
        context.Services.TryAddSingleton<FakeBrowserService>();
        context.Services.TryAddSingleton<IBrowserService>(provider => provider.GetRequiredService<FakeBrowserService>());
        context.Services.TryAddScoped(provider => new FakeDialogService(provider.GetRequiredService<DialogService>()));
        context.Services.TryAddScoped<IDialogService>(provider => provider.GetRequiredService<FakeDialogService>());
        context.Services.AddTrailblazor(configure);
        var modules = context.JSInterop.SetupModule(
            invocation => invocation.Arguments.FirstOrDefault() is string path
                && (path.StartsWith("./_content/Trailblazor/", StringComparison.Ordinal)
                    || path.StartsWith("./_content/Trailblazor.Primitives/", StringComparison.Ordinal)),
            isCatchAllHandler: true);
        modules.Mode = JSRuntimeMode.Loose;
        context.ComponentFactories.Add(new InteractiveByDefault(context));
        return context;
    }

    /// <summary>Opens a dialog for a test as a page would: renders a <see cref="TbRoot"/> to host it, opens
    /// <typeparamref name="TDialog"/> through <see cref="IDialogService"/>, and returns both. Find what the dialog
    /// shows through its <see cref="TestDialog{TDialog, TResult}.Root"/> or
    /// <see cref="TestDialog{TDialog, TResult}.Content"/>, and close it or await its result through its
    /// <see cref="TestDialog{TDialog, TResult}.Reference"/>. Call <see cref="AddTrailblazor"/> first, and leave the
    /// renderer interactive, since dialogs open only in interactive sessions. Later calls in the same test use the same
    /// <see cref="TbRoot"/>; don't render another one yourself, or every dialog shows in both.</summary>
    public static async Task<TestDialog<TDialog, TResult>> ShowDialogAsync<TDialog, TResult>(this BunitContext context,
        string title, Action<DialogParameters<TDialog>>? parameters = null, DialogOptions? options = null)
        where TDialog : TbDialogContent<TResult>
    {
        if (!Roots.TryGetValue(context, out var root) || root.IsDisposed)
        {
            root = context.Render<TbRoot>();
            Roots.AddOrUpdate(context, root);
        }

        var dialogs = context.Services.GetRequiredService<IDialogService>();
        var reference = await root.InvokeAsync(() => dialogs.Show<TDialog, TResult>(title, parameters, options));
        return new(root, reference);
    }

    // bUnit consults its factories as it creates each component, after the services are fixed; this one creates none.
    private sealed class InteractiveByDefault(BunitContext context) : IComponentFactory
    {
        // A renderer exposes its RendererInfo to subclasses only, so whether the test set one is read this way.
        private static readonly PropertyInfo RendererInfoProperty =
            typeof(Renderer).GetProperty(nameof(RendererInfo), BindingFlags.Instance | BindingFlags.NonPublic)!;

        private bool _checked;

        public bool CanCreate(Type componentType)
        {
            if (!_checked)
            {
                _checked = true;
                try
                {
                    RendererInfoProperty.GetValue(context.Renderer);
                }
                catch (TargetInvocationException exception)
                    when (exception.InnerException is MissingRendererInfoException)
                {
                    context.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
                }
            }

            return false;
        }

        public IComponent Create(Type componentType) => throw new NotSupportedException();
    }
}
