using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Trailblazor;

/// <summary>What every Trailblazor form field shares: <c>@bind-Value</c>, a label, helper text and validation
/// messages, inside or outside an <see cref="EditForm"/>.</summary>
public abstract class TbFieldBase<TValue> : TbComponentBase, IDisposable
{
    private readonly List<string> _messages = [];
    private EditContext? _editContext;
    private ValidationMessageStore? _messageStore;
    private FieldIdentifier? _field;
    private string? _parseError;
    private bool _disposed;

    /// <summary>The field's value.</summary>
    [Parameter]
    public TValue? Value { get; set; }

    /// <summary>Raised, and awaited, when the user changes the value. Setting <see cref="Value"/> never raises it.
    /// </summary>
    [Parameter]
    public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>The bound property, supplied by <c>@bind-Value</c>. It ties the field to its validation messages in
    /// an <see cref="EditForm"/> and names the input for static-SSR form posts.</summary>
    [Parameter]
    public Expression<Func<TValue>>? ValueExpression { get; set; }

    /// <summary>The field's visible label, which is also its accessible name.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>A hint shown under the field and read by screen readers with it.</summary>
    [Parameter]
    public string? HelperText { get; set; }

    /// <summary>Turns the field off: it can't be focused or changed, and it isn't posted with its form.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Marks the field as required for assistive technology and shows a marker by the label. Checking
    /// for a value is left to <see cref="Validation"/> or the form's validator.</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>Checks a new value and returns its error messages, if any. It runs when the value changes and
    /// when the surrounding <see cref="EditForm"/> validates.</summary>
    [Parameter]
    public Func<TValue?, IEnumerable<string>>? Validation { get; set; }

    [CascadingParameter]
    private EditContext? CascadedEditContext { get; set; }

    private protected string FieldId { get; } = $"tb-field-{Guid.NewGuid():N}";

    private protected string InputId => $"{FieldId}-input";

    private protected string HelperId => $"{FieldId}-helper";

    private protected string ErrorId => $"{FieldId}-error";

    private protected string? InputName { get; private set; }

    private protected string? Text => FormatValue(Value);

    // Inside an EditForm the context holds every validator's messages for this field, including ours.
    private protected IReadOnlyList<string> Errors =>
        _messageStore is null ? _messages : _editContext!.GetValidationMessages(_field!.Value).ToList();

    private protected string? DescribedBy => (HelperText is null, Errors.Count == 0) switch
    {
        (true, true) => null,
        (false, true) => HelperId,
        (true, false) => ErrorId,
        (false, false) => $"{HelperId} {ErrorId}",
    };

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        InputName = FieldNames.From(ValueExpression);
        // Only a form needs the identifier, and outside one an expression it can't parse must not throw.
        FieldIdentifier? field = CascadedEditContext is not null && ValueExpression is not null
            ? FieldIdentifier.Create(ValueExpression)
            : null;
        if (!field.Equals(_field))
        {
            // A field rebound to another property, as in a list without @key, drops the old property's messages.
            _field = field;
            _parseError = null;
            _messages.Clear();
            _messageStore?.Clear();
        }

        if (!ReferenceEquals(CascadedEditContext, _editContext))
        {
            DetachEditContext();
            _editContext = CascadedEditContext;
            if (_editContext is not null)
            {
                _editContext.OnValidationRequested += OnValidationRequested;
                _editContext.OnValidationStateChanged += OnValidationStateChanged;
            }
        }

        // The context can only hold messages for a field it can identify, so an unbound field keeps its own.
        if (_editContext is null || _field is null)
        {
            _messageStore?.Clear();
            _messageStore = null;
        }
        else
        {
            _messageStore ??= new ValidationMessageStore(_editContext);
        }
    }

    /// <summary>Turns a value into the text the input shows.</summary>
    private protected abstract string? FormatValue(TValue? value);

    /// <summary>Turns the input's text into a value, or explains why it can't.</summary>
    private protected abstract bool TryParse(string? text, out TValue? value, out string? error);

    private protected async Task SetValueAsync(TValue? value)
    {
        if (_parseError is null && EqualityComparer<TValue>.Default.Equals(value, Value))
        {
            return;
        }

        _parseError = null;
        Value = value;
        await ValueChanged.InvokeAsync(value!);
        if (_disposed)
        {
            return;
        }

        Validate();
        if (_messageStore is not null)
        {
            _editContext!.NotifyFieldChanged(_field!.Value);
        }
    }

    // Text that doesn't parse leaves the value, and so the rendered text, alone; the browser keeps what was typed.
    private protected async Task SetTextAsync(string? text)
    {
        if (TryParse(text, out var value, out var error))
        {
            await SetValueAsync(value);
            return;
        }

        _parseError = error;
        Validate();
        _editContext?.NotifyValidationStateChanged();
    }

    private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e) => Validate();

    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e) => StateHasChanged();

    private void Validate()
    {
        _messages.Clear();
        if (_parseError is not null)
        {
            _messages.Add(_parseError);
        }

        if (Validation is not null)
        {
            _messages.AddRange(Validation(Value));
        }

        if (_messageStore is not null)
        {
            _messageStore.Clear();
            _messageStore.Add(_field!.Value, _messages);
        }
    }

    private void DetachEditContext()
    {
        if (_editContext is not null)
        {
            _editContext.OnValidationRequested -= OnValidationRequested;
            _editContext.OnValidationStateChanged -= OnValidationStateChanged;
            _messageStore?.Clear();
            _messageStore = null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
        DetachEditContext();
    }
}
