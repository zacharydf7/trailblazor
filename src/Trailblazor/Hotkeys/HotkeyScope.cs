namespace Trailblazor;

/// <summary>Where a <see cref="TbHotkey"/> fires.</summary>
public enum HotkeyScope
{
    /// <summary>Anywhere on the page. While a modal dialog is open, only the hotkeys inside it fire.</summary>
    Page,

    /// <summary>Only while focus is inside the element the hotkey sits in, such as an editor or a list.</summary>
    Parent,
}
