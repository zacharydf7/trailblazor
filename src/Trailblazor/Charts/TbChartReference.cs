using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>What <see cref="TbReferenceLine"/> and <see cref="TbReferenceBand"/> share: a name and a tone for
/// something a <see cref="TbChart{TItem}"/> marks on its value axis, such as a target or a normal range, or along
/// its items, such as the day of a storm.</summary>
public abstract class TbChartReference : ComponentBase, IDisposable
{
    private object? _shown;

    [CascadingParameter]
    private ChartReferences? References { get; set; }

    /// <summary>What is marked, such as "Target". Required. The chart writes it in the plot with its value, and
    /// lists it under the data table, so color is never the only way to tell what is marked.</summary>
    [Parameter, EditorRequired]
    public string? Name { get; set; }

    /// <summary>The color of the line or band, such as <see cref="Trailblazor.Tone.Danger"/> for a limit. Without one
    /// it is drawn in the muted text color.</summary>
    [Parameter]
    public Tone? Tone { get; set; }

    // The lowest and the highest value marked on the value axis, which are the same for a line. Null for a reference
    // along the chart's items.
    internal abstract (double Low, double High)? Values { get; }

    // The first and the last place marked along the chart's items, which are the same for a line. Null for a
    // reference on the value axis.
    internal abstract (object From, object To)? Places { get; }

    // What its values have to be, for the message when they aren't: "a finite Value".
    private protected abstract string Needs { get; }

    private protected abstract bool HasWhatItNeeds { get; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (References is null)
        {
            throw new InvalidOperationException($"{GetType().Name} must be inside a {nameof(TbChart<>)}.");
        }

        References.Add(this);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(Name) || !HasWhatItNeeds)
        {
            throw new InvalidOperationException($"{GetType().Name} needs a {nameof(Name)} and {Needs}.");
        }

        // The chart draws before its references take their parameters, so one that changed has it draw again.
        object shown = (Name, Tone, Values, Places);
        if (_shown is not null && !shown.Equals(_shown))
        {
            References!.Changed();
        }

        _shown = shown;
    }

    /// <summary>Takes the line or band out of its chart.</summary>
    public void Dispose() => References?.Remove(this);
}
