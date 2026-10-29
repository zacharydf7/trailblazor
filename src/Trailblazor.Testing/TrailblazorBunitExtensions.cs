using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Testing;

/// <summary>bUnit setup for testing components built with Trailblazor.</summary>
public static class TrailblazorBunitExtensions
{
    /// <summary>Registers Trailblazor's services and answers its JavaScript modules, so components render and respond
    /// to clicks in bUnit with no JSInterop setup. Calls into the modules return defaults, which Trailblazor's
    /// components take to mean the browser part isn't there. A module set up by the test itself still wins. Call it
    /// before rendering or <c>SetRendererInfo</c>, since bUnit takes no new services once one has been resolved.
    /// </summary>
    public static BunitContext AddTrailblazor(this BunitContext context, Action<TrailblazorOptions>? configure = null)
    {
        context.Services.AddTrailblazor(configure);
        var modules = context.JSInterop.SetupModule(
            invocation => invocation.Arguments.FirstOrDefault() is string path
                && (path.StartsWith("./_content/Trailblazor/", StringComparison.Ordinal)
                    || path.StartsWith("./_content/Trailblazor.Primitives/", StringComparison.Ordinal)),
            isCatchAllHandler: true);
        modules.Mode = JSRuntimeMode.Loose;
        return context;
    }
}
