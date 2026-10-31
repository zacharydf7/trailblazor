using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A column of a <see cref="TbDataGrid{TItem}"/>: a <see cref="Field"/> it shows and can sort by, or a
/// <see cref="Cell"/> template of its own, or both. Columns show in the order they first render, so one that shows
/// later, such as inside an <c>@if</c>, goes last.</summary>
public sealed partial class TbColumn<TItem> : ComponentBase, IDisposable
{
    private Expression<Func<TItem, object?>>? _field;
    private Func<TItem, object?>? _value;
    private object?[] _captured = [];
    private int _compiled;
    private object? _shown;

    [CascadingParameter]
    private TbDataGrid<TItem> Grid { get; set; } = default!;

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
    /// <see cref="Field"/>'s name, or else the <see cref="Title"/>.</summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>How the <see cref="Field"/>'s value is written, such as <c>"d"</c> for a short date or <c>"N2"</c>
    /// for a number with two decimals, in the current culture.</summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>Lets the user sort the rows by this column.</summary>
    [Parameter]
    public bool Sortable { get; set; }

    /// <summary>The value rows sort by, when it isn't the <see cref="Field"/>'s, such as a name's last word.</summary>
    [Parameter]
    public Func<TItem, object?>? SortBy { get; set; }

    /// <summary>Draws each cell instead of the <see cref="Field"/>'s text. Its context is the row's item. Clicks on
    /// buttons, links and fields in it don't select the row.</summary>
    [Parameter]
    public RenderFragment<TItem>? Cell { get; set; }

    /// <summary>Draws the header instead of the <see cref="Title"/>, such as a title with a filter beside it. A
    /// sortable column keeps its sort button, with this content after it.</summary>
    [Parameter]
    public RenderFragment? Header { get; set; }

    /// <summary>The column's width, as CSS, such as <c>"8rem"</c> or <c>"20%"</c>.</summary>
    [Parameter]
    public string? Width { get; set; }

    /// <summary>Where the header and cells sit. Defaults to the end for numbers and the start for everything else.
    /// </summary>
    [Parameter]
    public ColumnAlign? Align { get; set; }

    /// <summary>Extra CSS classes for the column's cells.</summary>
    [Parameter]
    public string? Class { get; set; }

    internal string Key => Id ?? MemberName ?? Title ?? throw new InvalidOperationException(
        $"A {nameof(TbColumn<TItem>)} needs a {nameof(Field)}, an {nameof(Id)} or a {nameof(Title)}.");

    internal string Heading => Title ?? (MemberName is { } name ? Words(name) : "");

    internal ColumnAlign Alignment => Align ?? (IsNumber(FieldType) ? ColumnAlign.End : ColumnAlign.Start);

    internal bool CanSort => Sortable && (SortBy is not null || _value is not null);

    private string? MemberName => (Body(_field) as MemberExpression)?.Member.Name;

    private Type? FieldType => Body(_field)?.Type;

    /// <inheritdoc />
    protected override void OnInitialized() => Grid.AddColumn(this);

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
            _compiled++;
        }

        object shown = (_compiled, Title, Id, Format, Sortable, SortBy?.Method, Width, Align, Class, Cell is null,
            Header is null);
        if (!shown.Equals(_shown))
        {
            _shown = shown;
            Grid.ColumnChanged();
        }
    }

    internal object? ValueOf(TItem item) => _value?.Invoke(item);

    internal object? SortValueOf(TItem item) => SortBy is not null ? SortBy(item) : ValueOf(item);

    internal string? TextOf(TItem item) => ValueOf(item) switch
    {
        null => null,
        IFormattable formattable when Format is not null => formattable.ToString(Format, CultureInfo.CurrentCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentCulture),
        var value => value.ToString(),
    };

    /// <inheritdoc />
    public void Dispose() => Grid.RemoveColumn(this);

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

    // Where a new word starts: before a capital after a small letter or digit, and before the last capital of a run
    // that a small letter follows, so "HTMLPage" becomes "HTML page".
    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex WordStart();
}
