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

    /// <summary>Whether the user can close the panel, from the mark on its tab, with Delete or Backspace on the tab,
    /// with a middle click, or from Close in its group's menu. Defaults to true. <see cref="DockLayout.Open"/> shows
    /// a closed panel again.</summary>
    [Parameter]
    public bool Closable { get; set; } = true;

    /// <summary>Whether the content stays rendered, hidden, while another tab of its group shows, so its state
    /// survives. Defaults to true. Set it to false for content that is cheap to build again and costly to keep.
    /// Either way the content first renders when the panel first shows, and a closed panel's content is disposed.
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
