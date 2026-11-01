using Bunit;

namespace Trailblazor.Testing;

/// <summary>A dialog a test opened with <see cref="TrailblazorBunitExtensions.ShowDialogAsync{TDialog, TResult}"/>.
/// </summary>
/// <typeparam name="TDialog">The dialog's content.</typeparam>
/// <typeparam name="TResult">What the dialog closes with.</typeparam>
public sealed class TestDialog<TDialog, TResult> where TDialog : TbDialogContent<TResult>
{
    internal TestDialog(IRenderedComponent<TbRoot> root, DialogReference<TResult> reference)
    {
        Root = root;
        Reference = reference;
    }

    /// <summary>The <see cref="TbRoot"/> the dialog shows in, with its title and buttons; find its elements here.
    /// </summary>
    public IRenderedComponent<TbRoot> Root { get; }

    /// <summary>The open dialog, to close it, change it or await its <see cref="DialogReference{TResult}.Result"/>.
    /// </summary>
    public DialogReference<TResult> Reference { get; }

    /// <summary>The dialog's content component, for its instance, or to render it again.</summary>
    public IRenderedComponent<TDialog> Content => Root.FindComponents<DialogContent>()
        .Single(content => content.Instance.Dialog == Reference).FindComponent<TDialog>();
}
