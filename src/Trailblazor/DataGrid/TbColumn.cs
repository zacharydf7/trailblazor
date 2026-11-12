using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A column of a <see cref="TbDataGrid{TItem}"/>: a <see cref="Field"/> it shows and can sort by, or a
/// <see cref="CellTemplate"/> of its own, or both. Columns show in the order they first render, so one that shows
/// later, such as inside an <c>@if</c>, goes last.</summary>
public sealed partial class TbColumn<TItem> : ComponentBase, IDisposable
{
    private Expression<Func<TItem, object?>>? _field;
    private Func<TItem, object?>? _value;
    private object?[] _captured = [];
    private int _compiled;
    private object? _shown;
    private bool _averageNeedsFallback;
    private Action<TItem, object?>? _setter;

    [CascadingParameter]
    private TbDataGrid<TItem> Grid { get; set; } = default!;

    [CascadingParameter]
    private TbColumnGroup<TItem>? Band { get; set; }

    // The groups the column sits in, outermost first; their headings fill the rows above its header.
    internal IReadOnlyList<TbColumnGroup<TItem>> Bands => Band?.Chain ?? [];

    /// <summary>The value the column shows, such as <c>job => job.Name</c>. It also names the column, sorts it and
    /// gives its title, unless <see cref="Id"/>, <see cref="SortBy"/> or <see cref="Title"/> say otherwise. Values it
    /// reads from outside, such as a loop's variable, are compared to tell when it changed, so they should compare
    /// equal when they mean the same, as strings and records do.</summary>
    [Parameter]
    public Expression<Func<TItem, object?>>? Field { get; set; }

    /// <summary>The header's text. Defaults to the <see cref="Field"/>'s name in words, so <c>CreatedAt</c> becomes
    /// "Created at".</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Names the column in a <see cref="GridSort"/> and in its cells' <c>data-column</c>. Defaults to the
    /// <see cref="Field"/>'s path from the item, such as <c>Shipping.City</c>, or else the <see cref="Title"/>. No two
    /// columns of a grid may share one.</summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>How the <see cref="Field"/>'s value is written, such as <c>"d"</c> for a short date or <c>"N2"</c>
    /// for a number with two decimals, in the current culture.</summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>Lets the user sort the rows by this column.</summary>
    [Parameter]
    public bool Sortable { get; set; }

    /// <summary>The value rows sort by, when it isn't the <see cref="Field"/>'s, such as a name's last word. Call the
    /// grid's <see cref="TbDataGrid{TItem}.RefreshAsync"/> when what it reads from outside the item changes.</summary>
    [Parameter]
    public Func<TItem, object?>? SortBy { get; set; }

    /// <summary>Lets the user filter the rows by this column, from the grid's filter row or filter menu (see
    /// <see cref="TbDataGrid{TItem}.FilterMode"/>). The filter suits the
    /// <see cref="Field"/>'s type: text the cell contains, a number it equals, a range of dates, one of an enum's
    /// values, or yes or no for a bool.</summary>
    [Parameter]
    public bool Filterable { get; set; }

    /// <summary>The value rows filter by, when it isn't the <see cref="Field"/>'s, such as a name without accents.
    /// The filter still suits the <see cref="Field"/>'s type. Call the grid's
    /// <see cref="TbDataGrid{TItem}.RefreshAsync"/> when what it reads from outside the item changes.</summary>
    [Parameter]
    public Func<TItem, object?>? FilterBy { get; set; }

    /// <summary>The values the filter offers, such as a column's regions, shown as the cells show them. The filter
    /// becomes a list the user picks any of: a multiple select in the filter row, or checkboxes in the filter menu. In
    /// the menu, an enum or bool column gets checkboxes of its values without this.</summary>
    [Parameter]
    public IEnumerable<object?>? FilterChoices { get; set; }

    /// <summary>Draws the column's filter instead of the built-in one, in the filter row or the filter menu. Its
    /// context holds the column's filters and replaces them; in memory, rows are matched against the
    /// <see cref="Field"/>'s or <see cref="FilterBy"/>'s value.</summary>
    [Parameter]
    public RenderFragment<GridFilterContext>? FilterTemplate { get; set; }

    /// <summary>Lets the rows be grouped by this column, by the user from the grid's "Group by" list or from code
    /// through the grid's <see cref="TbDataGrid{TItem}.Groups"/>. With a <see cref="Format"/> and no
    /// <see cref="GroupBy"/>, rows group by the text their cells show, so times shown as dates group by day.</summary>
    [Parameter]
    public bool Groupable { get; set; }

    /// <summary>The value rows group by, when it isn't the <see cref="Field"/>'s, such as a date's month. A group's
    /// heading shows it as the cells show their values. Call the grid's <see cref="TbDataGrid{TItem}.RefreshAsync"/>
    /// when what it reads from outside the item changes.</summary>
    [Parameter]
    public Func<TItem, object?>? GroupBy { get; set; }

    /// <summary>Lets the user edit the column's cells: double-click a cell, press Enter or F2 on it, or start typing.
    /// Enter then saves and Escape cancels; Tab saves and opens the next editable cell, and leaving the cell, or
    /// choosing an enum's value, saves too. A text, number, enum or bool <see cref="Field"/> gets a field to edit it
    /// with; any other type needs an <see cref="EditTemplate"/>. Each value is checked by the field's
    /// data annotations, such as <c>[Range]</c>, and by <see cref="Validate"/>, and a failed check keeps the edit open
    /// with its error. A saved value goes to the grid's <see cref="TbDataGrid{TItem}.OnCellEdit"/>, or, without one,
    /// is set on the item when the field is a property or field the grid can set. An editable column makes the grid an
    /// ARIA grid with cell navigation, as <see cref="TbDataGrid{TItem}.CellNavigation"/> does. The second click of the
    /// double-click that opens an edit, and any click in an edit, don't raise
    /// <see cref="TbDataGrid{TItem}.OnRowClick"/> or toggle the row's selection.</summary>
    [Parameter]
    public bool Editable { get; set; }

    /// <summary>Draws the field a cell is edited with, in place of the built-in one. Its context holds the value and
    /// changes it, saves or cancels the edit, and has the error from the last attempt to save.</summary>
    [Parameter]
    public RenderFragment<GridCellEditContext<TItem>>? EditTemplate { get; set; }

    /// <summary>Checks a value before it is saved, returning why it can't be, such as <c>"Pick a later date."</c>, or
    /// null when it can. Give it as a lambda, such as <c>(hike, value) =&gt; ...</c>, so the column still takes its
    /// item type from the grid.</summary>
    [Parameter]
    public Func<TItem, object?, string?>? Validate { get; set; }

    /// <summary>Draws each cell instead of the <see cref="Field"/>'s text. Its context is the row's item. Clicks on
    /// buttons, links and fields in it don't select the row. A row draws again when its item, its selection, its class,
    /// its label or the columns change; call the grid's <see cref="TbDataGrid{TItem}.RenderRows"/> when the template
    /// shows anything else that changed.</summary>
    [Parameter]
    public RenderFragment<TItem>? CellTemplate { get; set; }

    /// <summary>Draws the header instead of the <see cref="Title"/>, such as a title with a filter beside it. A
    /// sortable column keeps its sort button, with this content after it.</summary>
    [Parameter]
    public RenderFragment? Header { get; set; }

    /// <summary>The total the column shows in the grid's totals row, worked out over every row that meets the filters,
    /// not only the page shown. With <see cref="TbDataGrid{TItem}.GroupTotals"/>, each open group shows its own. A
    /// grid with a <see cref="TbDataGrid{TItem}.DataProvider"/> asks for it in <see cref="GridRequest.Aggregates"/>,
    /// so one rendered without interactivity, such as on a static page, shows no totals.</summary>
    [Parameter]
    public ColumnAggregate Aggregate { get; set; }

    /// <summary>Works out the column's total from the rows that meet the filters, for a total
    /// <see cref="Aggregate"/> doesn't offer, such as the share of rows that are done. Its value shows in place of the
    /// <see cref="Aggregate"/>, written with the column's <see cref="Format"/>. Only a grid with
    /// <see cref="TbDataGrid{TItem}.Items"/> calls it; a <see cref="TbDataGrid{TItem}.DataProvider"/> gives the
    /// column's total in <see cref="GridResult{TItem}.Totals"/> itself. With
    /// <see cref="TbDataGrid{TItem}.GroupTotals"/>, it's called again with each open group's rows.</summary>
    [Parameter]
    public Func<IReadOnlyList<TItem>, object?>? AggregateBy { get; set; }

    /// <summary>Draws the column's cell in the totals row. Its context is the column's total, or null when it has
    /// none. With <see cref="TbDataGrid{TItem}.GroupTotals"/>, it draws each open group's cell too, with that group's
    /// total as its context.</summary>
    [Parameter]
    public RenderFragment<object?>? FooterTemplate { get; set; }

    /// <summary>The column's width, as CSS, such as <c>"8rem"</c> or <c>"20%"</c>. The browser treats it as the width
    /// it prefers, so a column can grow or shrink to fit the grid; once the user resizes a column of a grid with
    /// <see cref="TbDataGrid{TItem}.ResizableColumns"/>, every column keeps the width it has.</summary>
    [Parameter]
    public string? Width { get; set; }

    /// <summary>Whether the user can resize the column, when the grid has
    /// <see cref="TbDataGrid{TItem}.ResizableColumns"/>. On by default.</summary>
    [Parameter]
    public bool Resizable { get; set; } = true;

    /// <summary>Whether the user can hide the column, when the grid has
    /// <see cref="TbDataGrid{TItem}.HideableColumns"/>. On by default; a column that must stay, such as the one that
    /// names each row, turns it off.</summary>
    [Parameter]
    public bool Hideable { get; set; } = true;

    /// <summary>The narrowest the user can make the column, as CSS, such as <c>"8rem"</c>. Defaults to 8rem, room for
    /// its header's buttons and the start of its title.</summary>
    [Parameter]
    public string? MinWidth { get; set; }

    /// <summary>Keeps the column in view at the start or end of the row while the rows scroll sideways. A pinned
    /// column moves to its side, after any earlier column pinned to the same side. A column pinned at the start pins
    /// the selection and order columns with it.</summary>
    [Parameter]
    public ColumnPin Pinned { get; set; }

    /// <summary>Where the header and cells sit. Defaults to the end for numbers and the start for everything else.
    /// </summary>
    [Parameter]
    public ColumnAlign? Align { get; set; }

    /// <summary>Extra CSS classes for the column's cells.</summary>
    [Parameter]
    public string? Class { get; set; }

    internal string Key => Id ?? MemberPath ?? MemberName ?? Title ?? throw new InvalidOperationException(
        $"A {nameof(TbColumn<TItem>)} needs a {nameof(Field)}, an {nameof(Id)} or a {nameof(Title)}.");

    internal string Heading => Title ?? (MemberName is { } name ? Words(name) : "");

    internal ColumnAlign Alignment => Align ?? (IsNumber(FieldType) ? ColumnAlign.End : ColumnAlign.Start);

    internal bool CanSort => Sortable && (SortBy is not null || _value is not null);

    internal bool CanFilter =>
        Filterable && (FilterBy is not null || _value is not null || FilterTemplate is not null);

    internal bool CanGroup => Groupable && (GroupBy is not null || _value is not null);

    internal ColumnEditor Editor => !Editable ? ColumnEditor.None : EditTemplate is not null ? ColumnEditor.Custom
        : ValueType switch
        {
            null => ColumnEditor.None,
            { IsEnum: true } => ColumnEditor.Choice,
            var type when type == typeof(bool) => ColumnEditor.YesNo,
            var type when type == typeof(string) => ColumnEditor.Text,
            var type when IsNumber(type) => ColumnEditor.Number,
            _ => ColumnEditor.None,
        };

    internal bool CanSet => _setter is not null;

    internal bool AcceptsNull =>
        FieldType is not { IsValueType: true } || Nullable.GetUnderlyingType(FieldType) is not null;

    internal ColumnFilterKind FilterKind => ValueType switch
    {
        { IsEnum: true } => ColumnFilterKind.Choice,
        var type when type == typeof(bool) => ColumnFilterKind.YesNo,
        var type when type == typeof(DateOnly) || type == typeof(DateTime) || type == typeof(DateTimeOffset) =>
            ColumnFilterKind.Date,
        var type when IsNumber(type) => ColumnFilterKind.Number,
        _ => ColumnFilterKind.Text,
    };

    // An enum's values, each named in words, for a choice filter.
    internal IEnumerable<(object Value, string Label)> Choices => ValueType is { IsEnum: true } type
        ? Enum.GetNames(type).Select(name => (Enum.Parse(type, name), Words(name)))
        : [];

    internal string CellClass { get; private set; } = "";

    private string? MemberName => (Body(_field) as MemberExpression)?.Member.Name;

    // The field's members from the item down, such as Shipping.City, so two fields ending in City don't clash.
    private string? MemberPath => Body(_field) is MemberExpression member ? PathOf(member) : null;

    private Type? FieldType => Body(_field)?.Type;

    private Type? ValueType => FieldType is null ? null : Nullable.GetUnderlyingType(FieldType) ?? FieldType;

    /// <inheritdoc />
    protected override void OnInitialized() => Grid.AddMember(this, Band);

    /// <inheritdoc />
    // Markup makes a new field expression and new templates on every render, so the column compares what they say and
    // tells the grid only when something it shows has changed; a template reads the page's state when it runs anyway.
    // A field's text doesn't show the values it reads from outside, such as a loop's variable, so those are compared.
    protected override void OnParametersSet()
    {
        var captured = Captured.Of(Field);
        if (Field?.ToString() != _field?.ToString() || !captured.SequenceEqual(_captured))
        {
            _field = Field;
            _captured = captured;
            _value = Field?.Compile();
            _setter = SetterOf(Field);
            _compiled++;
        }

        object shown = (_compiled, Title, Id, Format, Sortable, SortBy?.Method, Filterable, FilterBy?.Method,
            FilterChoices is null, FilterTemplate is null, Groupable, GroupBy?.Method, Width, Pinned, Align, Class,
            CellTemplate is null, Header is null, Editable, EditTemplate is null, Validate?.Method, Aggregate,
            AggregateBy?.Method, FooterTemplate is null);
        if (!shown.Equals(_shown))
        {
            _shown = shown;
            CellClass = ClassNames.Join("tb-data-grid__cell", ClassNames.Modifier("tb-data-grid__cell", Alignment),
                Class);
            _averageNeedsFallback = Format is null || !WritesDecimals(Format);
            Grid.ColumnChanged();
        }
    }

    internal object? ValueOf(TItem item) => _value?.Invoke(item);

    // A column drawn only by its CellTemplate has no value to export.
    internal bool HasField => _value is not null;

    internal bool HasTotal =>
        Aggregate != ColumnAggregate.None || AggregateBy is not null || FooterTemplate is not null;

    // Whole numbers and decimals add up exactly as decimal; doubles and floats as double, which holds their range.
    internal object? TotalOf(IReadOnlyList<TItem> rows)
    {
        if (AggregateBy is not null)
        {
            return AggregateBy(rows);
        }

        var values = rows.Select(ValueOf).Where(value => value is not null).ToList();
        if (Aggregate == ColumnAggregate.Count)
        {
            return values.Count;
        }

        if (Aggregate is ColumnAggregate.Min or ColumnAggregate.Max)
        {
            return values.Count == 0 ? null : Aggregate == ColumnAggregate.Min
                ? values.Min(Comparer<object?>.Default) : values.Max(Comparer<object?>.Default);
        }

        if (Aggregate is not (ColumnAggregate.Sum or ColumnAggregate.Average) || values.Count == 0)
        {
            return null;
        }

        if (values.All(value => value is TimeSpan))
        {
            var ticks = values.Sum(value => ((TimeSpan)value!).Ticks);
            return TimeSpan.FromTicks(Aggregate == ColumnAggregate.Sum ? ticks : ticks / values.Count);
        }

        if (values.FirstOrDefault(value => !NumberTypes.IsNumber(value!.GetType())) is { } other)
        {
            throw new InvalidOperationException($"The \"{Key}\" column can't show the {Aggregate} of its " +
                $"{other.GetType().Name} values; give it {nameof(AggregateBy)} to work out its total instead.");
        }

        if (values.Any(value => value is double or float))
        {
            var doubles = values.Select(value => Convert.ToDouble(value, CultureInfo.InvariantCulture)).ToList();
            return Aggregate == ColumnAggregate.Sum ? doubles.Sum() : doubles.Average();
        }

        var decimals = values.Select(value => Convert.ToDecimal(value, CultureInfo.InvariantCulture)).ToList();
        var sum = decimals.Sum();
        if (Aggregate == ColumnAggregate.Sum)
        {
            // A sum of whole numbers stays whole, so an integer format such as "D4" still applies.
            var whole = values.All(value => NumberTypes.IsWhole(value!.GetType()));
            return whole && sum is >= long.MinValue and <= long.MaxValue ? (long)sum : (object)sum;
        }

        return decimals.Average();
    }

    // A count is whole rows whatever the format; an unformatted whole-number or decimal average shows two decimals.
    internal string? TotalTextOf(object? total) => total switch
    {
        _ when AggregateBy is not null => TextOf(total),
        { } whole when Aggregate == ColumnAggregate.Count && NumberTypes.IsWhole(whole.GetType()) =>
            Convert.ToDecimal(total, CultureInfo.InvariantCulture).ToString("N0", CultureInfo.CurrentCulture),
        decimal average when Aggregate == ColumnAggregate.Average && _averageNeedsFallback =>
            average.ToString("#,0.##", CultureInfo.CurrentCulture),
        _ => TextOf(total),
    };

    // An integer-only format, such as "D4", can't write a decimal; checked when the format changes, not each render.
    private static bool WritesDecimals(string format)
    {
        try
        {
            _ = 1.5m.ToString(format, CultureInfo.InvariantCulture);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    internal object? SortValueOf(TItem item) => SortBy is not null ? SortBy(item) : ValueOf(item);

    internal object? FilterValueOf(TItem item) => FilterBy is not null ? FilterBy(item) : ValueOf(item);

    // A FilterBy value's type isn't known until it is read.
    internal Type? FilterType => FilterBy is null ? ValueType : null;

    internal bool HoldsWholeNumbers => ValueType is { } type && NumberTypes.IsWhole(type);

    // The edited value in the field's own type, such as a number or an enum parsed from its name; throws an
    // OverflowException when a number doesn't fit.
    internal object? ConvertForField(object? value)
    {
        if (value is null || ValueType is not { } type || value.GetType() == type || Editor == ColumnEditor.Custom)
        {
            return value;
        }

        return type.IsEnum && value is string name ? Enum.Parse(type, name)
            : Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
    }

    // The field's data annotations, such as [Range], when it is a property.
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The attributes come from the property the "
        + "column's expression reads, which trimming keeps; Getting Started tells a trimmed app to keep any other "
        + "properties an attribute reads, such as [Compare]'s.")]
    internal string? AnnotationErrorOf(TItem item, object? value)
    {
        if ((Body(_field) as MemberExpression)?.Member is not PropertyInfo property || item is null)
        {
            return null;
        }

        var attributes = property.GetCustomAttributes<ValidationAttribute>(inherit: true).ToList();
        List<ValidationResult> results = [];
        var context = new ValidationContext(item) { MemberName = property.Name, DisplayName = Heading };
        return attributes.Count == 0 || Validator.TryValidateValue(value, context, results, attributes) ? null
            : results.FirstOrDefault()?.ErrorMessage;
    }

    internal void SetValue(TItem item, object? value) => _setter!(item, value);

    internal object? GroupValueOf(TItem item) => GroupBy is not null ? GroupBy(item) : ValueOf(item);

    internal object? GroupKeyOf(TItem item) =>
        GroupBy is null && Format is not null ? TextOf(ValueOf(item)) : GroupValueOf(item);

    internal string? TextOf(TItem item) => TextOf(ValueOf(item));

    internal string? TextOf(object? value) => value switch
    {
        null => null,
        IFormattable formattable when Format is not null => formattable.ToString(Format, CultureInfo.CurrentCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentCulture),
        _ => value.ToString(),
    };

    /// <inheritdoc />
    public void Dispose() => Grid.RemoveMember(this, Band);

    // The values a field reads from variables outside it, which C# keeps as fields of a closure object.
    private sealed class Captured : ExpressionVisitor
    {
        private readonly List<object?> _values = [];

        public static object?[] Of(Expression? field)
        {
            var visitor = new Captured();
            visitor.Visit(field);
            return [.. visitor._values];
        }

        // A chain such as column.Key, from a closure, is read to its end, so a key that stays the same compares equal
        // even when the column object around it is made anew.
        protected override Expression VisitMember(MemberExpression node)
        {
            if (!FromClosure(node))
            {
                return base.VisitMember(node);
            }

            _values.Add(Read(node));
            return node;
        }

        private static bool FromClosure(MemberExpression node) => node.Expression switch
        {
            ConstantExpression => true,
            MemberExpression inner => FromClosure(inner),
            _ => false,
        };

        // A link can be null where the field guards it, as in "currency == null ? ... : currency.Code", so the
        // chain stops there.
        private static object? Read(Expression node) => node switch
        {
            ConstantExpression constant => constant.Value,
            MemberExpression { Member: FieldInfo field } member when Read(member.Expression!) is { } target =>
                field.GetValue(target),
            MemberExpression { Member: PropertyInfo property } member when Read(member.Expression!) is { } target =>
                property.GetValue(target),
            _ => null,
        };
    }

    // A field the grid can set on an item held by reference, through a property with a public setter that isn't
    // init-only, or a field that isn't read-only.
    private static Action<TItem, object?>? SetterOf(Expression<Func<TItem, object?>>? field)
    {
        if (typeof(TItem).IsValueType || Body(field) is not MemberExpression member || member.Member switch
            {
                PropertyInfo property => property.SetMethod is not { IsPublic: true } setter || setter.ReturnParameter
                    .GetRequiredCustomModifiers().Contains(typeof(System.Runtime.CompilerServices.IsExternalInit)),
                FieldInfo info => info.IsInitOnly || !info.IsPublic,
                _ => true,
            })
        {
            return null;
        }

        var value = Expression.Parameter(typeof(object), "value");
        var assign = Expression.Assign(member, Expression.Convert(value, member.Type));
        return Expression.Lambda<Action<TItem, object?>>(assign, field!.Parameters[0], value).Compile();
    }

    private static string? PathOf(MemberExpression member) => member.Expression switch
    {
        ParameterExpression => member.Member.Name,
        MemberExpression parent when PathOf(parent) is { } path => $"{path}.{member.Member.Name}",
        _ => null,
    };

    // A field written as item => item.Count reaches here boxed, as a conversion around the member.
    private static Expression? Body(Expression<Func<TItem, object?>>? field) =>
        field?.Body is UnaryExpression { NodeType: ExpressionType.Convert } convert ? convert.Operand : field?.Body;

    private static bool IsNumber(Type? type) =>
        (Nullable.GetUnderlyingType(type ?? typeof(object)) ?? type) is { } underlying
        && (underlying.IsPrimitive && underlying != typeof(bool) && underlying != typeof(char)
            || underlying == typeof(decimal));

    private static string Words(string name)
    {
        var words = WordStart().Replace(name, " ");
        return words.Length == 0 ? words : char.ToUpper(words[0], CultureInfo.CurrentCulture)
            + words[1..].ToLower(CultureInfo.CurrentCulture);
    }

    // Where a new word starts: before a capital after a lowercase letter or digit, and before the last capital of a run
    // that a lowercase letter follows, so "HTMLPage" becomes "HTML page".
    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex WordStart();
}
