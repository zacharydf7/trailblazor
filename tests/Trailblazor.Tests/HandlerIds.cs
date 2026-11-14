using System.Globalization;
using AngleSharp.Dom;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Trailblazor.Tests;

// Raises an event on the handler id a test read earlier, as the browser does, to show the id survives a render.
internal static class HandlerIds
{
    public static ulong Read(IElement element, string eventName) =>
        ulong.Parse(element.GetAttribute($"blazor:{eventName}")!, CultureInfo.InvariantCulture);

#pragma warning disable BL0006 // Dispatching by handler id, as the browser does, needs RenderTree's EventFieldInfo.
    public static Task DispatchAsync(BunitRenderer renderer, ulong handler, int componentId, string value,
        EventArgs args) =>
        renderer.DispatchEventAsync(handler, new EventFieldInfo { ComponentId = componentId, FieldValue = value },
            args);
#pragma warning restore BL0006
}
