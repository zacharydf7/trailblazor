using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>Changes how a <see cref="TbModelFields{TModel}"/> shows one of its model's properties: its label, helper
/// text, place, width or editor, or hides it. It goes inside the <c>TbModelFields</c> and draws nothing itself.
/// </summary>
public sealed class TbModelField : ComponentBase, IDisposable
{
    private (string, string?, string?, int?, int, bool, bool, bool) _shown;

    /// <summary>The name of the property this changes, best given with <c>nameof</c>. Required.</summary>
    [Parameter, EditorRequired]
    public string Name { get; set; } = "";

    /// <summary>The field's label, in place of the one the property's attributes or name give.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Text shown under the field, in place of the property's description.</summary>
    [Parameter]
    public string? HelperText { get; set; }

    /// <summary>Where the field comes, counted as <see cref="DisplayAttribute.Order"/> is, in place of the property's
    /// own order. A field with neither counts as 10000.</summary>
    [Parameter]
    public int? Order { get; set; }

    /// <summary>How many of the fields' columns this one spans, up to all of them. Defaults to 1.</summary>
    [Parameter]
    public int Span { get; set; } = 1;

    /// <summary>Leaves the property out.</summary>
    [Parameter]
    public bool Hidden { get; set; }

    /// <summary>Makes the field read-only, even when the property can be set.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>The field to show in place of the one the property's type picks, such as a <c>TbRating</c> bound to
    /// the property. Its context holds the label, helper text and read-only state the field would have had.</summary>
    [Parameter]
    public RenderFragment<ModelFieldContext>? Editor { get; set; }

    [CascadingParameter]
    private ModelFieldOverrides? Overrides { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Overrides is null)
        {
            throw new InvalidOperationException($"{nameof(TbModelField)} must be inside a TbModelFields.");
        }

        _shown = Shown;
        Overrides.Add(this);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException($"{nameof(TbModelField)} needs the {nameof(Name)} of a property.");
        }

        // An editor made in the parent's markup is a new delegate each render, so only whether there is one counts.
        if (Shown != _shown)
        {
            _shown = Shown;
            Overrides!.Changed();
        }
    }

    private (string, string?, string?, int?, int, bool, bool, bool) Shown =>
        (Name, Label, HelperText, Order, Span, Hidden, ReadOnly, Editor is not null);

    /// <inheritdoc />
    public void Dispose() => Overrides?.Remove(this);
}

/// <summary>What a <see cref="TbModelField.Editor"/> needs to draw a field as <see cref="TbModelFields{TModel}"/>
/// would have.</summary>
/// <param name="Name">The property's name.</param>
/// <param name="Label">The field's label.</param>
/// <param name="HelperText">The text under the field, if any.</param>
/// <param name="ReadOnly">Whether the field can't be changed.</param>
public sealed record ModelFieldContext(string Name, string Label, string? HelperText, bool ReadOnly);

// The TbModelField elements inside one TbModelFields, which draws again when they change.
internal sealed class ModelFieldOverrides(Action changed)
{
    private readonly List<TbModelField> _fields = [];

    internal IEnumerable<string> Names => _fields.Select(one => one.Name);

    // A later TbModelField for the same property wins.
    internal TbModelField? For(string name) => _fields.LastOrDefault(field => field.Name == name);

    internal void Add(TbModelField field)
    {
        _fields.Add(field);
        changed();
    }

    internal void Remove(TbModelField field)
    {
        _fields.Remove(field);
        changed();
    }

    internal void Changed() => changed();
}
