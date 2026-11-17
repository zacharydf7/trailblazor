namespace Trailblazor;

/// <summary>What an in-place editor's <c>Editor</c> template edits: a draft of the value, which becomes the value when
/// it is saved.</summary>
/// <typeparam name="TValue">The value's type.</typeparam>
public sealed class InPlaceEditorContext<TValue>
{
    private readonly Func<Task> _save;
    private readonly Func<Task> _cancel;

    internal InPlaceEditorContext(TValue? draft, Func<Task> save, Func<Task> cancel) =>
        (Draft, _save, _cancel) = (draft, save, cancel);

    /// <summary>The value being edited, for the editor's field to bind to. It starts as the value itself, so for an
    /// object, set it to a changed copy: canceling can't undo a change made to the object.</summary>
    public TValue? Draft { get; set; }

    /// <summary>Saves the draft, as the save button and Enter do.</summary>
    public Task SaveAsync() => _save();

    /// <summary>Throws the draft away, as the cancel button and Escape do.</summary>
    public Task CancelAsync() => _cancel();
}
