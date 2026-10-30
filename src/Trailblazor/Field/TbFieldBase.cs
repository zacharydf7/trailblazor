using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Trailblazor;

/// <summary>What every Trailblazor form field shares: <c>@bind-Value</c>, a label, helper text and validation
/// messages, inside or outside an <see cref="EditForm"/>.</summary>
public abstract partial class TbFieldBase<TValue> : TbComponentBase, IFormField, IDisposable
{
    private readonly List<string> _messages = [];
    private EditContext? _editContext;
    private ValidationMessageStore? _messageStore;
    private FieldIdentifier? _field;
    private string? _parseError;
    private string? _unparsedText;
    private TValue? _valueAtParseError;
    private string? _inputName;
    private string? _shownError;
    private TbForm? _form;
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

    /// <summary>Lets the user focus the field and read or copy its value, but not change it. Unlike
    /// <see cref="Disabled"/>, the value still posts with its form.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Marks the field as required for assistive technology and shows a marker by the label. Checking
    /// for a value is left to <see cref="Validation"/> or the form's validator.</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>An error from the parent, such as one the server returned. It shows and is announced with the field's
    /// own messages, and keeps the form from submitting until the parent clears it.</summary>
    [Parameter]
    public string? Error { get; set; }

    /// <summary>Checks a new value and returns its error messages, if any. It runs when the value changes and
    /// when the surrounding <see cref="EditForm"/> validates.</summary>
    [Parameter]
    public Func<TValue?, IEnumerable<string>>? Validation { get; set; }

    /// <summary>Attributes for the input element itself, such as <c>autocomplete</c> or <c>name</c>.</summary>
    [Parameter]
    public IReadOnlyDictionary<string, object>? InputAttributes { get; set; }

    [CascadingParameter]
    private EditContext? CascadedEditContext { get; set; }

    [CascadingParameter]
    private TbForm? Form { get; set; }

    [Inject]
    private protected IAnnouncer Announcer { get; set; } = default!;

    [Inject]
    private protected ITbLocalizer Localizer { get; set; } = default!;

    private protected string FieldId { get; } = $"tb-field-{Guid.NewGuid():N}";

    private protected string InputId => $"{FieldId}-input";

    private protected string LabelId => $"{FieldId}-label";

    // A label's for attribute works only on form controls; other inputs name themselves with aria-labelledby.
    private protected virtual bool LabelTargetsInput => true;

    private protected string HelperId => $"{FieldId}-helper";

    private protected string ErrorId => $"{FieldId}-error";

    // The input attributes come first, so ours win; where ours are empty the consumer's still apply.
    private protected object? InputNameAttribute => _inputName ?? InputAttributes?.GetValueOrDefault("name");

    private protected string? InputDescribedBy =>
        (DescribedBy, InputAttributes?.GetValueOrDefault("aria-describedby")) switch
        {
            (null, var theirs) => theirs as string,
            (var ours, string theirs) => $"{ours} {theirs}",
            (var ours, _) => ours,
        };

    private protected string? Text => _unparsedText ?? FormatValue(Value);

    private protected bool HasParseError => _parseError is not null;

    // Inside an EditForm the context holds every validator's messages for this field, including ours.
    private protected IReadOnlyList<string> Errors =>
        _messageStore is null ? OwnMessages : _editContext!.GetValidationMessages(_field!.Value).ToList();

    private List<string> OwnMessages => ParentError is null ? _messages : [ParentError, .. _messages];

    // A blank error from the parent is no error, so the field is never marked invalid without a message.
    private string? ParentError => string.IsNullOrWhiteSpace(Error) ? null : Error;

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
        // A new value from the parent replaces text the user typed that didn't parse.
        if (_parseError is not null && !EqualityComparer<TValue>.Default.Equals(Value, _valueAtParseError))
        {
            _parseError = null;
            _unparsedText = null;
            _messages.Clear();
            _messageStore?.Clear();
            _editContext?.NotifyValidationStateChanged();
        }

        _inputName = FieldNames.From(ValueExpression);
        // Only a form needs the identifier, and outside one an expression it can't parse must not throw. A field
        // without a binding names itself, so the form still counts its messages.
        FieldIdentifier? field = CascadedEditContext is null ? null
            : ValueExpression is not null ? FieldIdentifier.Create(ValueExpression)
            : new FieldIdentifier(this, nameof(Value));
        if (!field.Equals(_field))
        {
            // A field rebound to another property, as in a list without @key, drops the old property's messages.
            _field = field;
            _parseError = null;
            _unparsedText = null;
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

        if (_editContext is null)
        {
            _messageStore?.Clear();
            _messageStore = null;
        }
        else
        {
            _messageStore ??= new ValidationMessageStore(_editContext);
            Store();
        }

        // The parent's error usually arrives after a submit, so it's read out as soon as it shows.
        if (ParentError != _shownError)
        {
            _shownError = ParentError;
            _editContext?.NotifyValidationStateChanged();
            if (_shownError is not null)
            {
                Announcer.Polite(_shownError);
            }
        }

        if (!ReferenceEquals(Form, _form))
        {
            _form?.Remove(this);
            _form = Form;
            _form?.Add(this);
        }
    }

    /// <summary>Moves focus to the field's input, such as to the field to fix after a failed save.</summary>
    /// <param name="preventScroll">Whether to keep the page from scrolling the field into view.</param>
    public abstract ValueTask FocusAsync(bool preventScroll = false);

    /// <summary>Turns a value into the text the input shows.</summary>
    private protected virtual string? FormatValue(TValue? value) =>
        BindConverter.FormatValue(value, CultureInfo.CurrentCulture)?.ToString();

    /// <summary>Turns the input's text into a value, or explains why it can't.</summary>
    private protected virtual bool TryParse(string? text, out TValue? value, out string? error)
    {
        if (BindConverter.TryConvertTo(text, CultureInfo.CurrentCulture, out value))
        {
            error = null;
            return true;
        }

        error = Message(TbStrings.FieldInvalid);
        return false;
    }

    // A message about the field: the key's text with the field's label, or a stand-in, first and then the args.
    private protected string Message(string key, params object?[] args) => string.Format(CultureInfo.CurrentCulture,
        Localizer[key], [Label ?? Localizer[TbStrings.FieldUnnamed], .. args]);

    private protected async Task SetValueAsync(TValue? value)
    {
        if (_parseError is null && EqualityComparer<TValue>.Default.Equals(value, Value))
        {
            return;
        }

        var before = Errors.ToList();
        _parseError = null;
        _unparsedText = null;
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

        AnnounceNewErrors(before);
    }

    // Text that doesn't parse leaves the value alone and stays in the input, so the user can fix it.
    private protected async Task SetTextAsync(string? text)
    {
        if (TryParse(text, out var value, out var error))
        {
            await SetValueAsync(value);
            return;
        }

        ShowParseError(error!, text);
    }

    // The text stays in the input for the user to fix, and rendering what the browser sent doesn't write over it.
    private protected void ShowParseError(string error, string? text)
    {
        var before = Errors.ToList();
        _unparsedText = text;
        _parseError = error;
        _valueAtParseError = Value;
        Validate();
        _editContext?.NotifyValidationStateChanged();
        AnnounceNewErrors(before);
    }

    // Only edits speak, and only new messages, so a submit with several errors doesn't read them all out at once. The
    // parent's error has already spoken if the parent set it while the edit was awaited.
    private void AnnounceNewErrors(List<string> before)
    {
        var added = Errors.Except(before).Where(error => error != _shownError).ToList();
        if (added.Count > 0)
        {
            Announcer.Polite(string.Join(" ", added));
        }
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

        Store();
    }

    private void Store()
    {
        _messageStore?.Clear();
        _messageStore?.Add(_field!.Value, OwnMessages);
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

    bool IFormField.HasProblems() => _parseError is not null || (Validation?.Invoke(Value).Any() ?? false);

    // The parent's error stays: only the parent can clear it.
    void IFormField.HideMessages()
    {
        _messages.Clear();
        Store();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
        _form?.Remove(this);
        DetachEditContext();
    }
}
