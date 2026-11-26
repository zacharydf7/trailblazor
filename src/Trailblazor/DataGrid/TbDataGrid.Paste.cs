using System.Globalization;
using Trailblazor.Internal.Events;

namespace Trailblazor;

public partial class TbDataGrid<TItem>
{
    // A batch grid keeps pasted cells as changes, saved with the rest.
    private bool CanPaste => EditMode == GridEditMode.Batch && _shownColumns.Any(CanPasteInto);

    private bool CanPasteInto(TbColumn<TItem> column) => CanEdit(column) && column.Editor != ColumnEditor.Custom;

    private Task OnCellsPastedAsync(CellsPasteEventArgs args) => AnsweredAsync(() =>
    {
        Paste(args);
        return Task.CompletedTask;
    });

    // Nothing is kept unless every pasted cell reads as its column's value and passes the column's checks. A paste that
    // lands on no cell it can change, such as on a read-only column, does nothing.
    private void Paste(CellsPasteEventArgs args)
    {
        if (!CanPaste || EndOf(args.Row, args.Column) is not { } focused || string.IsNullOrEmpty(args.Text)
            || PasteTargets(TabSeparated.Read(args.Text), focused) is not { Count: > 0 } targets)
        {
            return;
        }

        List<(TItem Row, TbColumn<TItem> Column, object? Value)> pasted = [];
        foreach (var (row, column, text) in targets)
        {
            object? value = null;
            var error = !TryReadPasted(column, text, out var read)
                ? Format(Labels.EditInvalid, TbStrings.DataGridEditInvalid, column.Heading)
                : CheckEdit(row, new CellEdit(column, read), out value);
            if (error is not null)
            {
                Announcer.Assertive(Format(Labels.PasteRejected, TbStrings.DataGridPasteRejected, error));
                return;
            }

            pasted.Add((row, column, value));
        }

        foreach (var (row, column, value) in pasted)
        {
            KeepChange(KeyOf(row), row, column, value);
        }

        _editVersion++;
        _rowVersion++;
        Telemetry.Used(TelemetryName, "paste");
        Announcer.Polite(Format(Labels.Pasted, TbStrings.DataGridPasted, pasted.Count));
    }

    // Each pasted value with the cell it lands in. One value pasted over a range fills it; otherwise the values run from
    // the range's first cell, or the cell in focus, and stop at the last row and column shown. A cell that can't be
    // edited, or that edits with a template, keeps its value.
    private List<(TItem Row, TbColumn<TItem> Column, string Text)> PasteTargets(List<List<string>> values,
        (object Row, string Column) focused)
    {
        if (HasRange && values is [[var only]])
        {
            return [.. _rows.Where(row => _rangeRows.Contains(KeyOf(row))).SelectMany(row => _shownColumns
                .Where(column => _rangeColumns.Contains(column.Key) && CanPasteInto(column))
                .Select(column => (row, column, only)))];
        }

        var top = HasRange ? _rows.FindIndex(row => _rangeRows.Contains(KeyOf(row)))
            : _rows.FindIndex(row => Equals(KeyOf(row), focused.Row));
        var left = HasRange ? _shownColumns.FindIndex(column => _rangeColumns.Contains(column.Key))
            : _shownColumns.FindIndex(column => column.Key == focused.Column);
        List<(TItem, TbColumn<TItem>, string)> targets = [];
        for (var down = 0; top >= 0 && down < values.Count && top + down < _rows.Count; down++)
        {
            for (var across = 0; left >= 0 && across < values[down].Count && left + across < _shownColumns.Count;
                across++)
            {
                if (_shownColumns[left + across] is var column && CanPasteInto(column))
                {
                    targets.Add((_rows[top + down], column, values[down][across]));
                }
            }
        }

        return targets;
    }

    // Pasted text read as the column's editor reads it, in the current culture and with the column's Format, so text
    // copied from the grid pastes back; a moment with no offset is in the zone the grid shows. A blank cell clears the
    // value.
    private bool TryReadPasted(TbColumn<TItem> column, string text, out object? value)
    {
        value = null;
        if (column.Editor == ColumnEditor.Text)
        {
            value = text;
            return true;
        }

        text = text.Trim();
        if (text.Length == 0)
        {
            return true;
        }

        var culture = CultureInfo.CurrentCulture;
        var format = column.Format is { Length: > 1 } custom ? custom : null;
        switch (column.Editor)
        {
            case ColumnEditor.Number when decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                culture, out var number):
                value = number;
                return true;
            case ColumnEditor.Choice when column.Choices.FirstOrDefault(choice =>
                string.Equals(choice.Label, text, StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(choice.Value.ToString(), text, StringComparison.OrdinalIgnoreCase)) is
                { Value: { } choice }:
                value = choice;
                return true;
            case ColumnEditor.YesNo:
                value = string.Equals(text, column.TextOf(true), StringComparison.CurrentCultureIgnoreCase) ? true
                    : string.Equals(text, column.TextOf(false), StringComparison.CurrentCultureIgnoreCase) ? false
                    : bool.TryParse(text, out var yes) ? yes : null;
                return value is not null;
            case ColumnEditor.Date when CalendarDates.TryReadDate(text,
                format ?? culture.DateTimeFormat.ShortDatePattern, culture, out var day):
                value = day;
                return true;
            case ColumnEditor.Time when TimeList.TryRead(text, culture, out var time):
                value = time;
                return true;
            case ColumnEditor.DateAndTime when TryReadMoment(text, format, culture, out var moment):
                value = moment;
                return true;
            case ColumnEditor.DateAndTimeWithOffset when TryReadMoment(text, format, culture, out var moment):
                value = moment.Kind == DateTimeKind.Unspecified
                    ? new DateTimeOffset(moment, ShownTimeZone.GetUtcOffset(moment))
                    : DateTimeOffset.Parse(text, culture);
                return true;
            default:
                return false;
        }
    }

    private static bool TryReadMoment(string text, string? format, CultureInfo culture, out DateTime moment) =>
        format is not null && DateTime.TryParseExact(text, format, culture, DateTimeStyles.None, out moment)
        || DateTime.TryParse(text, culture, DateTimeStyles.None, out moment);
}
