using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>What <see cref="TbToggleGroup{TValue}"/> and <see cref="TbMultiToggleGroup{TItem}"/> share: a row of
/// <see cref="TbToggleItem{TValue}"/> buttons that is one tab stop, with the arrow keys moving between them.</summary>
public abstract partial class TbToggleGroupBase<TValue> : TbComponentBase, IToggleGroup, IAsyncDisposable
{
    private readonly string _id = $"tb-toggle-group-{Guid.NewGuid():N}";
    private ElementReference _group;
    private IJSObjectReference? _handle;
    private bool? _attachedWrapped;
    private int _attaches;
    private bool _disposed;

    /// <summary>A visible label above the items, which also names the group. A group needs a label or an
    /// <see cref="AriaLabel"/>.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>A hint shown under the items and read by screen readers with the group.</summary>
    [Parameter]
    public string? HelperText { get; set; }

    /// <summary>The group's accessible name, such as "Text alignment", for a group with no
    /// <see cref="Label"/>.</summary>
    [Parameter]
    public string AriaLabel { get; set; } = "";

    /// <summary>The <see cref="TbToggleItem{TValue}"/>s.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The group's value.</summary>
    [Parameter]
    public TValue? Value { get; set; }

    /// <summary>Raised, and awaited, when the user presses an item.</summary>
    [Parameter]
    public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>Stacks the items, and moves between them with Up and Down instead of Left and Right.</summary>
    [Parameter]
    public bool Vertical { get; set; }

    /// <summary>Turns every item off.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>How strongly the items are drawn. Defaults to <see cref="Trailblazor.Variant.Outlined"/>.</summary>
    [Parameter]
    public Variant Variant { get; set; } = Variant.Outlined;

    /// <summary>The items' color. Defaults to <see cref="Trailblazor.Tone.Neutral"/>.</summary>
    [Parameter]
    public Tone Tone { get; set; } = Tone.Neutral;

    /// <summary>The items' size. Defaults to <see cref="Trailblazor.Size.Medium"/>.</summary>
    [Parameter]
    public Size Size { get; set; } = Size.Medium;

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    // The type every item's value must have.
    private protected abstract Type ItemType { get; }

    private protected abstract bool IsPressed(object? item);

    // Whether several items can be pressed, and whether pressing a pressed item releases it; test helpers read both.
    private protected abstract bool TakesSeveral { get; }

    private protected abstract bool LetsGo { get; }

    // The group's value after the user presses this item.
    private protected abstract TValue? Toggle(object? item);

    private string? ShownLabel => string.IsNullOrWhiteSpace(Label) ? null : Label;

    private bool Wrapped => ShownLabel is not null || HelperText is not null;

    private string LabelId => $"{_id}-label";

    private string HelperId => $"{_id}-helper";

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (ShownLabel is null && string.IsNullOrWhiteSpace(AriaLabel))
        {
            throw new InvalidOperationException($"A toggle group needs a {nameof(Label)} or an {nameof(AriaLabel)}.");
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Adding or removing the label or helper text rebuilds the group element, which needs its script again. If
        // another render rebuilds it while this one awaits JS, only the latest render's handle is kept.
        if (_attachedWrapped == Wrapped)
        {
            return;
        }

        _attachedWrapped = Wrapped;
        var attach = ++_attaches;
        try
        {
            if (_handle is { } previous)
            {
                _handle = null;
                await previous.DisposeHandleAsync();
            }

            var module = await Modules.ImportAsync("./_content/Trailblazor/toggle-group.js");
            if (_disposed || attach != _attaches)
            {
                return;
            }

            var handle = await module.InvokeAsync<IJSObjectReference?>("attachToggleGroup", _group);
            if (handle is not null && (_disposed || attach != _attaches))
            {
                await handle.DisposeHandleAsync();
                return;
            }

            _handle = handle;
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the group went with it.
        }
    }

    bool IToggleGroup.IsPressed(object? value) => IsPressed(value);

    async Task IToggleGroup.ToggleAsync(object? value)
    {
        var next = Toggle(value);
        if (EqualityComparer<TValue?>.Default.Equals(next, Value))
        {
            return;
        }

        Value = next;
        await ValueChanged.InvokeAsync(Value);
        StateHasChanged();
    }

    void IToggleGroup.Accept(object? value)
    {
        if (value is not null && !ItemType.IsInstanceOfType(value))
        {
            throw new InvalidOperationException($"A TbToggleItem's value must be a {ItemType.Name}, the type of its " +
                $"group, but {value} is a {value.GetType().Name}.");
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }
    }
}
