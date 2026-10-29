using System.Text.Json.Serialization;

namespace Trailblazor;

/// <summary>What gets focus when a dialog opens. An element in the dialog marked <c>autofocus</c>, or the first
/// field inside it, gets focus instead whenever it can take it.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<DialogFocus>))]
public enum DialogFocus
{
    /// <summary>The first field or button in the dialog's content, or the dialog itself when it has none.</summary>
    FirstField,

    /// <summary>The dialog itself, so a screen reader starts at its title.</summary>
    Dialog,
}
