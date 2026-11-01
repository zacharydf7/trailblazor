using System.Runtime.CompilerServices;
using Bunit;
using Microsoft.AspNetCore.Components;
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
    /// <see cref="FakeBrowserService"/>, to check and steer from the test. A module or service set up by the test
    /// itself still wins. Call it before rendering or <c>SetRendererInfo</c>, since bUnit takes no new services once
    /// one has been resolved.</summary>
    public static BunitContext AddTrailblazor(this BunitContext context, Action<TrailblazorOptions>? configure = null)
    {
        context.Services.TryAddSingleton<FakeBrowserService>();
        context.Services.TryAddSingleton<IBrowserService>(provider => provider.GetRequiredService<FakeBrowserService>());
        context.Services.AddTrailblazor(configure);
        var modules = context.JSInterop.SetupModule(
            invocation => invocation.Arguments.FirstOrDefault() is string path
                && (path.StartsWith("./_content/Trailblazor/", StringComparison.Ordinal)
                    || path.StartsWith("./_content/Trailblazor.Primitives/", StringComparison.Ordinal)),
            isCatchAllHandler: true);
        modules.Mode = JSRuntimeMode.Loose;
        return context;
    }

    /// <summary>Opens a dialog for a test as a page would: renders a <see cref="TbRoot"/> to host it, opens
    /// <typeparamref name="TDialog"/> through <see cref="IDialogService"/>, and returns both. Find what the dialog
    /// shows through its <see cref="TestDialog{TDialog, TResult}.Root"/> or
    /// <see cref="TestDialog{TDialog, TResult}.Content"/>, and close it or await its result through its
    /// <see cref="TestDialog{TDialog, TResult}.Reference"/>. Dialogs open only in interactive sessions, so it sets an
    /// interactive Server renderer. Call <see cref="AddTrailblazor"/> first. Later calls in the same test use the same
    /// <see cref="TbRoot"/>; don't render another one yourself, or every dialog shows in both.</summary>
    public static async Task<TestDialog<TDialog, TResult>> ShowDialogAsync<TDialog, TResult>(this BunitContext context,
        string title, Action<DialogParameters<TDialog>>? parameters = null, DialogOptions? options = null)
        where TDialog : TbDialogContent<TResult>
    {
        if (!Roots.TryGetValue(context, out var root) || root.IsDisposed)
        {
            context.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
            root = context.Render<TbRoot>();
            Roots.AddOrUpdate(context, root);
        }

        var dialogs = context.Services.GetRequiredService<IDialogService>();
        var reference = await root.InvokeAsync(() => dialogs.Show<TDialog, TResult>(title, parameters, options));
        return new(root, reference);
    }
}
