using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A number input's change, with whether the browser could read what was typed. Raised by
/// <see cref="TbNumberField{TValue}"/>'s input; consumers don't normally handle it.</summary>
public sealed class NumberChangeEventArgs : EventArgs
{
    /// <summary>The input's value, which the browser leaves empty when it can't read the text.</summary>
    public string? Value { get; set; }

    /// <summary>Whether the input holds text the browser can't read as a number, such as "4-".</summary>
    public bool BadInput { get; set; }
}

/// <summary>Registers Trailblazor's custom browser events with Razor, which only reads them from a public class with
/// this name. The browser side is in Trailblazor.lib.module.js.</summary>
[EventHandler("ontbnumberchange", typeof(NumberChangeEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontbnumberleave", typeof(NumberChangeEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
public static class EventHandlers
{
}
