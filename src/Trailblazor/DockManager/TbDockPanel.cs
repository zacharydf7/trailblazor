using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>One panel of a <see cref="TbDockManager"/>: a tab and the content it shows. The dock's
/// <see cref="DockLayout"/> says where the panel is, by its <see cref="Id"/>, and the content keeps its state as the
/// panel moves, hides and shows. Its <c>Class</c>, <c>Style</c> and other attributes go on the panel's element,
/// except <c>id</c>, which the dock sets.</summary>
public sealed class TbDockPanel : TbComponentBase, IDisposable
{
    private string? _id;

    /// <summary>Names the panel in the dock's <see cref="DockLayout"/> and in any layout saved from it, so keep it
    /// the same between versions of the app. It can't be empty or have white space in it, and can't change.</summary>
    [Parameter, EditorRequired]
    public string Id { get; set; } = "";

    /// <summary>The text on the panel's tab, which also names the panel to screen readers.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    /// <summary>An icon before the tab's text, hidden from screen readers.</summary>
    [Parameter]
    public Icon? Icon { get; set; }

    /// <summary>Content in the panel's tab after its title, such as a count or a status dot. Screen readers hear its
    /// text after the tab's name, as its description, and the tab and the panel keep <see cref="Title"/> as their
    /// name, so the menus, the testing helpers and the announcements as the panel moves still name it by its title.
    /// Give it text a screen reader can say, such as a badge's <c>Label</c>, keep it short, and leave controls out of
    /// it: the tab is a button.</summary>
    [Parameter]
    public RenderFragment? TabEnd { get; set; }

    /// <summary>Whether the user can close the panel, from the mark on its tab, with Delete or Backspace on the tab,
    /// with a middle click, or from Close in its group's menu. Defaults to true. <see cref="DockLayout.Open"/> shows
    /// a closed panel again.</summary>
    [Parameter]
    public bool Closable { get; set; } = true;

    /// <summary>Asked before the user closes the panel, by any route: the mark on its tab, Delete or Backspace, a
    /// middle click, or Close in a menu. Return false to keep the panel open, with its content and the focus as
    /// they are, such as after asking whether to discard unsaved changes. While it is being answered, another
    /// request to close the panel is dropped. It isn't asked when the app closes the panel itself, with
    /// <see cref="DockLayout.Close"/>.</summary>
    [Parameter]
    public Func<Task<bool>>? CanClose { get; set; }

    /// <summary>Marks the panel as holding unsaved changes. Its tab shows a dot, which gives way to the close mark
    /// while the pointer or the keyboard focus is on the tab, and screen readers hear "Unsaved changes" with the tab's
    /// name. The button of a panel put away on a strip shows and says the same, and so do the panel's button in the
    /// away row and its window's return button while it is in a window of its own.</summary>
    [Parameter]
    public bool Modified { get; set; }

    /// <summary>Whether the panel's menus offer "Open in new window" when the dock's
    /// <see cref="TbDockManager.AllowPopOut"/> is on. Defaults to true. Set it to false for a panel that has to stay
    /// on the page, such as one that holds a script that finds its elements through the page's document, or an
    /// iframe, which loads again when it moves. A panel that is in a window of its own comes back when this turns
    /// false.</summary>
    [Parameter]
    public bool AllowPopOut { get; set; } = true;

    /// <summary>How long the panel's own browser window stays open after the app takes the panel away while it shows
    /// there, for a panel that comes and goes, such as a form that is there only while a task runs. The window says
    /// that the panel isn't open, and shows the panel again when the app makes it again, so the user needn't open
    /// the window each time. <see cref="Timeout.InfiniteTimeSpan"/>, or a time longer than a browser's timer can wait
    /// (about 24 days), keeps the window until the user or the page closes it. Null (the default) or a time of zero
    /// or less closes the window with the panel, which then shows where it was docked when it is made again.
    /// </summary>
    [Parameter]
    public TimeSpan? KeepWindowFor { get; set; }

    /// <summary>Whether the content stays rendered, hidden, while another tab of its group shows, so its state
    /// survives. Defaults to true. Set it to false for content that is cheap to build again and costly to keep.
    /// Either way the content first renders when the panel first shows, and a closed panel's content is disposed.
    /// A panel put away on a strip counts as hidden: its content is kept or disposed the same way.
    /// </summary>
    [Parameter]
    public bool KeepAlive { get; set; } = true;

    /// <summary>The panel's content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [CascadingParameter]
    private TbDockManager? Dock { get; set; }

    // Changes each time the parent renders the panel, so the dock renders its content again only then.
    internal int Version { get; private set; }

    internal IReadOnlyDictionary<string, object>? HostAttributes => AttributesWithoutId;

    // The id the panel registered under, which the dock knows it by even while a changed Id is being refused.
    internal string Name => _id ?? Id;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Dock is null)
        {
            throw new InvalidOperationException($"{nameof(TbDockPanel)} must be inside a {nameof(TbDockManager)}.");
        }

        if (string.IsNullOrEmpty(Id) || Id.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(
                $"A {nameof(TbDockPanel)} needs an {nameof(Id)} that isn't empty and has no white space in it.");
        }

        if (_id is null)
        {
            _id = Id;
            Dock.Add(this);
        }
        else if (_id != Id)
        {
            throw new InvalidOperationException($"A {nameof(TbDockPanel)}'s {nameof(Id)} can't change.");
        }

        Version = Dock.Stamp();
        Dock.Refresh();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_id is not null)
        {
            Dock?.Remove(this);
        }
    }
}
