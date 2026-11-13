using System.ComponentModel;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Internal.Events;

/// <summary>A number input's change, with whether the browser could read what was typed. Raised by
/// <see cref="TbNumberField{TValue}"/>'s input; consumers don't normally handle it.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class NumberChangeEventArgs : EventArgs
{
    /// <summary>The input's value, which the browser leaves empty when it can't read the text.</summary>
    public string? Value { get; set; }

    /// <summary>Whether the input holds text the browser can't read as a number, such as "4-".</summary>
    public bool BadInput { get; set; }
}

/// <summary>A step a number field's script asks for: a large one for Page Up or Page Down, or a repeat or the release
/// of a held step button. Raised by <see cref="TbNumberField{TValue}"/>'s input; consumers don't normally handle it.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class NumberStepEventArgs : EventArgs
{
    /// <summary>1 to step up, -1 to step down, or 0 when a held step button is let go.</summary>
    public int Direction { get; set; }

    /// <summary>Whether the step is a large one, for Page Up or Page Down.</summary>
    public bool Large { get; set; }

    /// <summary>Whether a step button is held, so the step shows without changing the value yet.</summary>
    public bool Held { get; set; }
}

/// <summary>Focus moving into or out of a field's root, with whether it crossed the root's edge or only moved between
/// the field's own parts. Raised for a field's <c>OnFocus</c> and <c>OnBlur</c>; consumers don't normally handle it.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class FieldFocusEventArgs : EventArgs
{
    /// <summary>Whether focus came from, or went to, somewhere outside the field.</summary>
    public bool CrossesField { get; set; }
}

/// <summary>HTML a rich text editor's script sent: its new value, or what was pasted for the app's paste transform.
/// Raised by <see cref="TbRichTextEditor"/>'s editing area; consumers don't normally handle it.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RichTextChangeEventArgs : EventArgs
{
    /// <summary>The HTML the editor's script sent, before the editor parses it: all of it, or one part when it's
    /// long.</summary>
    public string Html { get; set; } = "";

    /// <summary>Whether more of the HTML follows in the next event.</summary>
    public bool Continues { get; set; }
}

/// <summary>A rich text editor's request for its link dialog, sent by its script on Ctrl+K or the link button. Raised
/// by <see cref="TbRichTextEditor"/>'s editing area; consumers don't normally handle it.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RichTextLinkEventArgs : EventArgs
{
    /// <summary>The link the selected text has, or null when it has none or more than one.</summary>
    public string? Link { get; set; }

    /// <summary>Whether nothing is selected, so the dialog inserts new linked text.</summary>
    public bool Insert { get; set; }
}

/// <summary>Registers Trailblazor's custom browser events with Razor, which only reads them from a public class with
/// this name. The browser side is in Trailblazor.lib.module.js.</summary>
[EventHandler("ontbnumberchange", typeof(NumberChangeEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontbnumberleave", typeof(NumberChangeEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontbnumberinput", typeof(NumberChangeEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontbnumberstep", typeof(NumberStepEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontbfocusenter", typeof(FieldFocusEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontbfocusleave", typeof(FieldFocusEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontbrowclick", typeof(RowClickEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontbrowextend", typeof(RowExtendEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontbselectall", typeof(EventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontbhotkey", typeof(EventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontbtreefilterdown", typeof(EventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontbeditstart", typeof(EventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontbeditkey", typeof(GridEditKeyEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontbcolumnwidths", typeof(ColumnWidthsEventArgs), enableStopPropagation: true,
    enablePreventDefault: true)]
[EventHandler("ontbcolumnorder", typeof(ColumnOrderEventArgs), enableStopPropagation: true,
    enablePreventDefault: true)]
[EventHandler("ontbrichtextchange", typeof(RichTextChangeEventArgs), enableStopPropagation: true,
    enablePreventDefault: true)]
[EventHandler("ontbrichtextpaste", typeof(RichTextChangeEventArgs), enableStopPropagation: true,
    enablePreventDefault: true)]
[EventHandler("ontbrichtextlink", typeof(RichTextLinkEventArgs), enableStopPropagation: true,
    enablePreventDefault: true)]
[EventHandler("ontbtagadd", typeof(TagAddEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontbtagremove", typeof(TagRemoveEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontbtreedrop", typeof(TreeDropEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontbcontextmenuopen", typeof(ContextMenuOpenEventArgs), enableStopPropagation: true,
    enablePreventDefault: true)]
[EventHandler("ontbcellrange", typeof(CellRangeEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontbcellscopy", typeof(EventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontbsignaturestroke", typeof(SignatureStrokeEventArgs), enableStopPropagation: true,
    enablePreventDefault: true)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class EventHandlers
{
}
