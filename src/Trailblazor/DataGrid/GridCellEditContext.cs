namespace Trailblazor;

/// <summary>What a <see cref="TbColumn{TItem}.EditTemplate"/> edits: the row's item and the value being edited, which
/// the template changes with <see cref="SetValue"/>, and the error from the last attempt to save it.</summary>
public sealed class GridCellEditContext<TItem>
{
    private readonly Action<object?> _setValue;
    private readonly Func<Task> _save;
    private readonly Func<Task> _cancel;

    internal GridCellEditContext(TItem item, object? value, string? error, Action<object?> setValue, Func<Task> save,
        Func<Task> cancel)
    {
        Item = item;
        Value = value;
        Error = error;
        _setValue = setValue;
        _save = save;
        _cancel = cancel;
    }

    /// <summary>The row's item.</summary>
    public TItem Item { get; }

    /// <summary>The value being edited, which starts as the cell's.</summary>
    public object? Value { get; }

    /// <summary>Why the last attempt to save failed, such as a value out of range, for the template to show.
    /// </summary>
    public string? Error { get; }

    /// <summary>Changes the value being edited.</summary>
    public void SetValue(object? value) => _setValue(value);

    /// <summary>Checks and saves the value, as Enter does, ending the edit unless a check fails.</summary>
    public Task SaveAsync() => _save();

    /// <summary>Ends the edit without saving, as Escape does.</summary>
    public Task CancelAsync() => _cancel();
}
