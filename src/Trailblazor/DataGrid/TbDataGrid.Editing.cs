using Microsoft.JSInterop;
using Trailblazor.Internal.Events;

namespace Trailblazor;

public partial class TbDataGrid<TItem>
{
    private GridEdit? _edit;
    // The key of the row the user is adding, which no item's key equals.
    private static readonly object NewRowKey = new();
    private TbButton? _addButton;
    private int _editVersion;
    // A batch grid's changes by row key and column, in the order first made, then why the app last turned them down,
    // and the save in flight with the cells it sent.
    private readonly OrderedDictionary<(object Key, string Column), GridCellEdit<TItem>> _changes = [];
    // The rows a batch grid holds as deleted until the changes are saved, by key, in the order they were deleted.
    private readonly OrderedDictionary<object, TItem> _deleted = [];
    // The rows the user added to a batch grid, each under a key of its own, in the order they were added.
    private readonly OrderedDictionary<object, TItem> _added = [];
    private string? _batchError;
    private Task? _batchSave;
    private HashSet<(object Key, string Column)> _sending = [];
    private HashSet<object> _sendingAdded = [];

    /// <summary>Starts an edit in this item's row from code, such as an app's own Edit button, as a double-click on one
    /// of its cells does. <paramref name="columnId"/> names the cell to start in by its column's <c>Id</c> or field
    /// path, and without one the first editable column is used. An edit already open is saved first, and stays open
    /// when it can't be. An item the grid isn't showing, such as one filtered out, isn't edited.</summary>
    /// <exception cref="ArgumentException">No shown, editable column has this id.</exception>
    /// <exception cref="InvalidOperationException">No shown column is editable.</exception>
    public async Task StartEditAsync(TItem item, string? columnId = null)
    {
        var editable = _shownColumns.Where(CanEdit).ToList();
        if (editable.Count == 0)
        {
            throw new InvalidOperationException("The grid shows no editable column.");
        }

        var column = columnId is null ? editable[0] : editable.FirstOrDefault(shown => shown.Key == columnId)
            ?? throw new ArgumentException($"No shown, editable column has the id \"{columnId}\".", nameof(columnId));
        await StartEditAsync(KeyOf(item), column);
        StateHasChanged();
    }

    // An edit already open is saved before another starts, and stays open when it can't be. In row mode an edit covers
    // every editable cell of the row, starting in the one asked for.
    private async Task StartEditAsync(object key, TbColumn<TItem> column)
    {
        if (IsDeleted(key))
        {
            return;
        }

        if (_edit is { } open && (Equals(open.Key, key) && (EditsRows || open.Start == column)
                || !await SaveEditAsync(refocus: false)))
        {
            return;
        }

        // The save just made may have replaced the row's item, or filtered or sorted the row out of view.
        var at = _rows.FindIndex(row => Equals(KeyOf(row), key));
        if (at < 0 && !_added.ContainsKey(key))
        {
            return;
        }

        var item = at >= 0 ? _rows[at] : _added[key];
        var columns = EditsRows ? _shownColumns.Where(CanEdit) : [column];
        _edit = new(item, key, column, [.. columns.Select(editable => new CellEdit(editable,
            ChangeOf(key, editable) is { } change ? change.Value : editable.ValueOf(item)))]);
        _editVersion++;
    }

    // A new row opens at the top, editing every cell with an editor, once an edit already open is saved.
    private async Task AddRowAsync()
    {
        if (!await SaveEditAsync(refocus: false))
        {
            return;
        }

        var item = NewItem!();
        var columns = _shownColumns.Where(column => column.Editor != ColumnEditor.None).ToList();
        // A batch grid keeps the new row as a change, and its first cell opens for editing.
        if (EditMode == GridEditMode.Batch)
        {
            var key = new object();
            _added[key] = item;
            _rowVersion++;
            Telemetry.Used(TelemetryName, "add");
            await StartEditAsync(key, columns[0]);
            return;
        }

        _edit = new(item, NewRowKey, columns[0], [.. columns.Select(column => new CellEdit(column,
            column.ValueOf(item)))]);
        _editVersion++;
    }

    // A select takes Enter to open its list, so choosing from it saves a cell's edit; a row's edit is saved whole.
    private Task ChooseAsync(CellEdit cell, string? choice)
    {
        cell.Value = choice;
        return !EditsRows ? SaveEditAsync(refocus: true) : Task.CompletedTask;
    }

    // A key the field used, such as Enter opening a select's list or Escape closing it, leaves the edit open.
    private async Task OnEditKeyAsync(GridEditKeyEventArgs args)
    {
        if (args.Handled)
        {
            return;
        }

        if (args is { Key: "Enter", IsComposing: false })
        {
            await SaveEditAsync(refocus: true);
        }
        else if (args is { Key: "Escape", IsComposing: false })
        {
            await CancelEditAsync();
        }
    }

    // Enter on a row's Save or Cancel button clicks it, so only Escape is handled here.
    private Task OnRowActionKeyAsync(GridEditKeyEventArgs args) =>
        args is { Key: "Escape", Handled: false } ? CancelEditAsync() : Task.CompletedTask;

    // A field removed as the next edit opens reports focus leaving too, and that mustn't save the new edit.
    private Func<FieldFocusEventArgs, Task> LeftHandler(GridEdit edit) => args => OnEditLeftAsync(edit, args);

    // A new row left untouched is dropped, so a click elsewhere doesn't add a row the user never filled in.
    private async Task OnEditLeftAsync(GridEdit edit, FieldFocusEventArgs args)
    {
        if (args.CrossesEdit && _edit == edit && IsNew(edit.Key) && edit.Untouched)
        {
            _edit = null;
            _editVersion++;
        }
        else if (args.CrossesEdit && _edit == edit)
        {
            await SaveEditAsync(refocus: false);
        }
    }

    // A value that fails a check keeps the edit open, showing why; one that passes goes to the app, or onto the item,
    // and the rows are shown again, sorted and filtered with it. The edit stays open until the app has taken it, so a
    // handler that sets an error loses nothing typed. A save already on its way, such as to a server, is waited for
    // rather than started again.
    private Task<bool> SaveEditAsync(bool refocus)
    {
        if (_edit is not { } edit)
        {
            return Task.FromResult(true);
        }

        if (edit.Saving is { IsCompleted: false } saving)
        {
            return saving;
        }

        return edit.Saving = SaveAsync(edit, refocus);
    }

    // A row is saved only when every one of its cells passes its checks.
    private async Task<bool> SaveAsync(GridEdit edit, bool refocus)
    {
        edit.Error = null;
        Dictionary<CellEdit, object?> values = [];
        foreach (var cell in edit.Cells)
        {
            cell.Error = CheckEdit(edit.Item, cell, out var value);
            values[cell] = value;
        }

        if (edit.Cells.Any(cell => cell.Error is not null))
        {
            ShowEditErrors();
            return false;
        }

        if (EditMode == GridEditMode.Batch)
        {
            KeepChanges(edit, values);
            Telemetry.Used(TelemetryName, "edit");
            if (refocus)
            {
                await FocusCellAsync(edit);
            }

            return true;
        }

        if (await RejectionOfAsync(edit, values) is { } rejected)
        {
            if (EditsRows)
            {
                edit.Error = rejected;
                Announcer.Assertive(rejected);
            }
            else
            {
                edit.StartCell.Error = rejected;
            }

            ShowEditErrors();
            return false;
        }

        var adding = IsNew(edit.Key);
        if (!HasEditHandler && !adding)
        {
            foreach (var (cell, value) in values)
            {
                cell.Column.SetValue(edit.Item, value);
            }
        }

        _edit = null;
        _editVersion++;
        Telemetry.Used(TelemetryName, adding ? "add" : "edit");
        if (adding)
        {
            Announcer.Polite(Text(Labels.RowAdded, TbStrings.DataGridRowAdded));
        }

        await RefreshAsync();
        if (refocus)
        {
            await FocusCellAsync(edit);
        }

        return true;
    }

    // A batch grid keeps each checked value as a change.
    private void KeepChanges(GridEdit edit, Dictionary<CellEdit, object?> values)
    {
        foreach (var (cell, value) in values)
        {
            KeepChange(edit.Key, _rows.FirstOrDefault(row => Equals(KeyOf(row), edit.Key)) ?? edit.Item, cell.Column,
                value);
        }

        _edit = null;
        _editVersion++;
        _rowVersion++;
    }

    // A value edited back to the item's own drops the change, unless the cell is still being saved, since the save is
    // about to change the item.
    private void KeepChange(object rowKey, TItem item, TbColumn<TItem> column, object? value)
    {
        var key = (rowKey, column.Key);
        if (Equals(value, column.ValueOf(item)) && !_sending.Contains(key))
        {
            _changes.Remove(key);
        }
        else
        {
            _changes[key] = new(item, column.Key, value);
        }
    }

    private GridCellEdit<TItem>? ChangeOf(object key, TbColumn<TItem> column) =>
        _changes.GetValueOrDefault((key, column.Key));

    /// <summary>Saves the changes of a grid whose <see cref="EditMode"/> is <see cref="GridEditMode.Batch"/>, as its
    /// Save changes button does: an edit still open is saved into them first, then <see cref="OnBatchSave"/> gets them
    /// all (without a handler, each value is set on its item), and the rows are shown again. If that edit fails its
    /// check, nothing is saved. A handler that sets <see cref="GridBatchEdit{TItem}.Error"/> keeps the changes and
    /// shows why. A save already on its way is waited for rather than started again.</summary>
    public Task SaveChangesAsync() => _batchSave is { IsCompleted: false } saving ? saving : _batchSave = SaveBatchAsync();

    private Task SaveBatchAsync() => AnsweredAsync(SaveBatchCoreAsync);

    private async Task SaveBatchCoreAsync()
    {
        if (!await SaveEditAsync(refocus: false) || !HasChanges)
        {
            return;
        }

        // An added row's cells the user never opened are checked too, as a row edit checks its new row.
        foreach (var (key, item) in _added)
        {
            foreach (var column in _shownColumns.Where(column => CanEdit(column) && ChangeOf(key, column) is null))
            {
                if (CheckEdit(item, new CellEdit(column, column.ValueOf(item)), out _) is { } invalid)
                {
                    _batchError = invalid;
                    Announcer.Assertive(invalid);
                    return;
                }
            }
        }

        var sent = _changes.Where(pair => !IsDeleted(pair.Key.Key)).ToList();
        var deleted = _deleted.ToList();
        var added = _added.ToList();
        // As with a row edit's new row, each value typed into an added row is set on its item where the grid can.
        foreach (var (key, change) in sent.Where(pair => IsAdded(pair.Key.Key)))
        {
            if (_columns.FirstOrDefault(column => column.Key == key.Column) is { CanSet: true } column)
            {
                column.SetValue(change.Item, change.Value);
            }
        }

        var batch = new GridBatchEdit<TItem>([.. sent.Select(pair => pair.Value)])
        {
            Deleted = [.. deleted.Select(pair => pair.Value)],
            Added = [.. added.Select(pair => pair.Value)],
        };
        _batchError = null;
        _sending = [.. sent.Select(pair => pair.Key)];
        _sendingAdded = [.. added.Select(pair => pair.Key)];
        try
        {
            if (OnBatchSave.HasDelegate)
            {
                await OnBatchSave.InvokeAsync(batch);
            }
            else
            {
                foreach (var change in batch.Changes)
                {
                    _columns.First(column => column.Key == change.Column).SetValue(change.Item, change.Value);
                }
            }
        }
        finally
        {
            _sending = [];
            _sendingAdded = [];
        }

        if (batch.Error is { } error)
        {
            // The refused changes stay, so swap in any items reloaded while the save handler had them.
            TakeReloadedItems(_rows);
            _batchError = error;
            Announcer.Assertive(error);
            return;
        }

        // Changes made while the batch was on its way stay for the next save.
        foreach (var (key, change) in sent)
        {
            if (ReferenceEquals(_changes.GetValueOrDefault(key), change))
            {
                _changes.Remove(key);
            }
        }

        foreach (var (key, _) in deleted)
        {
            _deleted.Remove(key);
            foreach (var change in _changes.Keys.Where(change => Equals(change.Key, key)).ToList())
            {
                _changes.Remove(change);
            }
        }

        // An added row's changes made while its save was on its way move onto the saved row, for the next save.
        foreach (var (key, item) in added)
        {
            _added.Remove(key);
            foreach (var (change, edit) in _changes.Where(pair => Equals(pair.Key.Key, key)).ToList())
            {
                _changes.Remove(change);
                _changes[(KeyOf(item), change.Column)] = edit;
            }
        }

        Announcer.Polite(Text(Labels.ChangesSaved, TbStrings.DataGridChangesSaved));
        await RefreshAsync();
    }

    /// <summary>Drops the changes of a grid whose <see cref="EditMode"/> is <see cref="GridEditMode.Batch"/>, and any
    /// edit still open, as its Discard changes button does, so every cell shows its item's value again and every row
    /// marked deleted is restored.</summary>
    public Task DiscardChangesAsync()
    {
        _edit = null;
        _editVersion++;
        _changes.Clear();
        _deleted.Clear();
        _added.Clear();
        _batchError = null;
        RenderRows();
        return Task.CompletedTask;
    }

    private async Task SaveChangesFromBarAsync()
    {
        Telemetry.Used(TelemetryName, "save_changes");
        await SaveChangesAsync();
    }

    private Task DiscardChangesFromBarAsync()
    {
        Telemetry.Used(TelemetryName, "discard_changes");
        return DiscardChangesAsync();
    }

    private void ShowEditErrors()
    {
        _editVersion++;
        StateHasChanged();
    }

    // The app's handler takes the edit, or turns it down with an error.
    private async Task<string?> RejectionOfAsync(GridEdit edit, Dictionary<CellEdit, object?> values)
    {
        var adding = IsNew(edit.Key);
        if (adding)
        {
            foreach (var (cell, value) in values.Where(pair => pair.Key.Column.CanSet))
            {
                cell.Column.SetValue(edit.Item, value);
            }
        }
        else if (!HasEditHandler)
        {
            return null;
        }

        if (EditsRows)
        {
            var row = new GridRowEdit<TItem>(edit.Item,
                values.ToDictionary(pair => pair.Key.Column.Key, pair => pair.Value));
            await (adding ? OnRowAdd : OnRowEdit).InvokeAsync(row);
            return row.Error;
        }

        var change = new GridCellEdit<TItem>(edit.Item, edit.Start.Key, values[edit.StartCell]);
        await OnCellEdit.InvokeAsync(change);
        return change.Error;
    }

    /// <summary>Cancels the open edit, as Escape does: the values stay as they were, and focus goes back to the cell. A
    /// new row the user is adding is dropped, and focus goes to the Add row button. In <see cref="GridEditMode.Cell"/>,
    /// focus leaving the cell saves the edit, so a control outside the cell that takes focus saves the edit before this
    /// method can cancel it; call this from code that leaves focus where it is, or use <see cref="GridEditMode.Row"/>,
    /// whose rows have their own Cancel button.</summary>
    public async Task CancelEditAsync()
    {
        if (_edit is not { } edit)
        {
            return;
        }

        _edit = null;
        _editVersion++;
        if (!IsNew(edit.Key))
        {
            await FocusCellAsync(edit);
        }
        else if (_addButton is not null)
        {
            await NextRenderAsync();
            await _addButton.FocusAsync();
        }
    }

    // Focus goes back to the cell once its field is gone, unless another edit has opened since.
    private async Task FocusCellAsync(GridEdit edit)
    {
        await NextRenderAsync();
        if (_handle is not null && _edit is null)
        {
            // A new row from a row edit is found by its item's key once saved; any other row keeps its own key.
            await _handle.InvokeVoidAsync("focusCell", IsNew(edit.Key) ? IdOf(edit.Item) : _ids.IdOf(edit.Key),
                edit.Start.Key);
        }
    }

    // A value the field can't hold, such as a fraction for a whole number, or no value for a field that can't be
    // null, fails before the column's own checks.
    private string? CheckEdit(TItem item, CellEdit cell, out object? value)
    {
        value = null;
        // A picker keeps its last value while it holds text it couldn't read, so the edit stays open until that text
        // is fixed.
        if (cell.Field is { HasParseError: true })
        {
            return Format(Labels.EditInvalid, TbStrings.DataGridEditInvalid, cell.Column.Heading);
        }

        if (cell.Column.HoldsWholeNumbers && cell.Value is decimal number && number != decimal.Truncate(number))
        {
            return Format(Labels.EditInvalid, TbStrings.DataGridEditInvalid, cell.Column.Heading);
        }

        try
        {
            value = cell.Column.ConvertForField(cell.Value);
        }
        catch (OverflowException)
        {
            return Format(Labels.EditInvalid, TbStrings.DataGridEditInvalid, cell.Column.Heading);
        }

        if (value is null && !cell.Column.AcceptsNull)
        {
            return Format(Labels.EditRequired, TbStrings.FieldRequired, cell.Column.Heading);
        }

        return cell.Column.AnnotationErrorOf(item, value) ?? cell.Column.Validate?.Invoke(item, value);
    }

    // A template that shows its value draws again as the value changes.
    private GridCellEditContext<TItem> ContextOf(GridEdit edit, CellEdit cell) => new(edit.Item, cell.Value,
        cell.Error, value =>
        {
            cell.Value = value;
            _editVersion++;
            StateHasChanged();
        }, () => SaveEditAsync(refocus: true), CancelEditAsync);

    private bool CanDelete => EditMode == GridEditMode.Batch ? Deletable && OnBatchSave.HasDelegate
        : OnRowDelete.HasDelegate;

    private bool IsDeleted(object key) => _deleted.ContainsKey(key);

    private bool CanAdd => NewItem is not null && !Virtualize
        && _shownColumns.Any(column => column.Editor != ColumnEditor.None)
        && (EditsRows ? OnRowAdd.HasDelegate
            : EditMode == GridEditMode.Batch && OnBatchSave.HasDelegate);

    private bool IsAdded(object key) => _added.ContainsKey(key);

    private GridEdit? Adding => _edit is { } edit && IsNew(edit.Key) ? edit : null;

    private GridEdit? AddingInRow => EditMode == GridEditMode.Row ? Adding : null;

    private bool EditsRows => EditMode is GridEditMode.Row or GridEditMode.Form;

    private string? FieldLabel(CellEdit cell) => EditMode == GridEditMode.Form ? cell.Column.Heading : null;

    private string? CellLabel(CellEdit cell) => EditMode == GridEditMode.Form ? null : cell.Column.Heading;

    private string FormTitle => _formEdit is not { } edit ? ""
        : IsNew(edit.Key) ? Text(Labels.AddRow, TbStrings.DataGridAddRow)
        : Format(Labels.EditRow, TbStrings.DataGridEditRow, LabelOf(edit.Item));

    // The form's last edit, which it shows until it has faded out.
    private GridEdit? _formEdit;

    private string FormLabelId(CellEdit cell) => $"{_changedId}-form-{cell.Column.Key}";

    // Closing the form, as with Escape or its close button, cancels the edit.
    private Task FormOpenChangedAsync(bool open) => open ? Task.CompletedTask : CancelEditAsync();

    private static bool IsNew(object key) => ReferenceEquals(key, NewRowKey);

    // What is being edited: one cell, or in row mode every editable cell of a row, starting in one.
    private sealed class GridEdit(TItem item, object key, TbColumn<TItem> start, IReadOnlyList<CellEdit> cells)
    {
        public TItem Item { get; } = item;

        public object Key { get; } = key;

        public TbColumn<TItem> Start { get; } = start;

        public IReadOnlyList<CellEdit> Cells { get; } = cells;

        public CellEdit StartCell => Cells.First(cell => cell.Column == Start);

        public Task<bool>? Saving { get; set; }

        public string? Error { get; set; }

        public CellEdit? CellOf(TbColumn<TItem> column) => Cells.FirstOrDefault(cell => cell.Column == column);

        public bool Untouched => Cells.All(cell => Equals(cell.Value, cell.Initial));
    }

    // A cell being edited, and the value its field holds so far.
    private sealed class CellEdit(TbColumn<TItem> column, object? value)
    {
        public TbColumn<TItem> Column { get; } = column;

        public object? Value { get; set; } = value;

        public object? Initial { get; } = value;

        public string? Error { get; set; }

        // The field a date or time cell edits in, which says whether it holds text it couldn't read.
        public IFormField? Field { get; set; }
    }

    // A deleted row's own changes aren't saved, and an added row counts once, however many of its cells changed.
    private int ChangeCount => _changes.Keys.Count(key => !_deleted.ContainsKey(key.Key) && !IsAdded(key.Key))
        + _deleted.Count + _added.Count;

    private bool HasEditHandler => EditMode switch
    {
        GridEditMode.Row or GridEditMode.Form => OnRowEdit.HasDelegate,
        GridEditMode.Batch => OnBatchSave.HasDelegate,
        _ => OnCellEdit.HasDelegate,
    };

    // An added row that is deleted goes at once, with what was typed into it, since nothing has been saved. Focus goes
    // to the row in its place: the next added row, or the one before at the end, then the first row, then Add row.
    private async Task RemoveAddedAsync(object key)
    {
        // A row already on its way to the app is saved with the batch, so it stays.
        if (_sendingAdded.Contains(key))
        {
            return;
        }

        List<object> shown = [.. _added.Keys.Reverse()];
        var at = shown.IndexOf(key);
        shown.RemoveAt(at);
        _added.Remove(key);
        foreach (var change in _changes.Keys.Where(change => Equals(change.Key, key)).ToList())
        {
            _changes.Remove(change);
        }

        if (_edit is { } open && Equals(open.Key, key))
        {
            _edit = null;
            _editVersion++;
        }

        _rowVersion++;
        Telemetry.Used(TelemetryName, "delete");
        Announcer.Polite(Text(Labels.NewRowRemoved, TbStrings.DataGridNewRowRemoved));
        await NextRenderAsync();
        var column = _shownColumns.FirstOrDefault()?.Key;
        if (_handle is not null && shown.Count > 0)
        {
            await _handle.InvokeVoidAsync("focusCell", _ids.IdOf(shown[Math.Min(at, shown.Count - 1)]), column);
        }
        else if (_handle is not null && _rows.Count > 0)
        {
            await _handle.InvokeVoidAsync("focusCell", IdOf(_rows[0]), column);
        }
        else if (_addButton is not null)
        {
            await _addButton.FocusAsync();
        }
    }

    // A batch grid keeps a delete as a change until the changes are saved or discarded, and a second press restores
    // the row. An edit of the row is dropped, since its cells can't be edited while it's deleted.
    private Task ToggleDeletedAsync(TItem item)
    {
        var key = KeyOf(item);
        var label = LabelOf(item);
        if (_deleted.Remove(key))
        {
            Telemetry.Used(TelemetryName, "undo_delete");
            Announcer.Polite(Format(Labels.RowRestored, TbStrings.DataGridRowRestored, label));
        }
        else
        {
            if (_edit is { } open && Equals(open.Key, key))
            {
                _edit = null;
                _editVersion++;
            }

            _deleted[key] = item;
            Telemetry.Used(TelemetryName, "delete");
            Announcer.Polite(Format(Labels.RowMarkedDeleted, TbStrings.DataGridRowMarkedDeleted, label));
        }

        _rowVersion++;
        return Task.CompletedTask;
    }
}
