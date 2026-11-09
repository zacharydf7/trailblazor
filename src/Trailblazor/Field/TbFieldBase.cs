using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Trailblazor;

/// <summary>What every Trailblazor form field shares: <c>@bind-Value</c>, a label, helper text and validation
/// messages, inside or outside an <see cref="EditForm"/>. An <c>id</c> given to a field names its control, such as the
/// input of a text field or the box of a checkbox, as on a native input; its other attributes go on the field's root.
/// A field with two controls, such as a range, keeps the <c>id</c> on its root, as do checkbox and radio groups.
/// </summary>
public abstract partial class TbFieldBase<TValue> : TbComponentBase, IFormField, IDisposable
{
    private readonly List<string> _messages = [];
    private EditContext? _editContext;
    private ValidationMessageStore? _messageStore;
    private FieldIdentifier? _field;
    private ControlTelemetry? _telemetry;
    private string? _parseError;
    private string? _unparsedText;
    private TValue? _valueAtParseError;
    private string? _typedText;
    private TValue? _typedValue;
    private TValue? _lastValue;
    private TValue? _valueToResetTo;
    private string? _inputName;
    private string? _shownError;

    // Each message this field announced and the errors it named, so the message is withdrawn once any is fixed.
    private readonly List<(AnnouncementHandle Handle, List<string> Errors)> _announced = [];
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
    /// <see cref="Disabled"/>, the value still posts with its form. A file upload, whose input can't be read only,
    /// turns its picker off instead.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Marks the field as required: screen readers hear it, the label shows a marker, and an empty value
    /// shows <see cref="RequiredMessage"/> once the user changes the field or its form validates. A disabled or
    /// read-only field isn't checked, since the user can't fill it in.</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>The message for a <see cref="Required"/> field left empty. Defaults to the app's text for
    /// <see cref="TbStrings.FieldRequired"/>, "{label} is required." in English.</summary>
    [Parameter]
    public string? RequiredMessage { get; set; }

    /// <summary>Shows the field as invalid, with its styling and <c>aria-invalid</c>, without a message of its own,
    /// as when a summary elsewhere lists the form's errors. Unlike <see cref="Error"/>, it doesn't keep a form from
    /// submitting.</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>An error from the parent, such as one the server returned. It shows and is announced with the field's
    /// own messages, and keeps the form from submitting until the parent clears it.</summary>
    [Parameter]
    public string? Error { get; set; }

    /// <summary>Checks a new value and returns its error messages, if any. It runs when the value changes and
    /// when the surrounding <see cref="EditForm"/> validates, but not while the field shows
    /// <see cref="RequiredMessage"/>, so that message is the only one then.</summary>
    [Parameter]
    public Func<TValue?, IEnumerable<string>>? Validation { get; set; }

    /// <summary>Attributes for the input element itself, such as <c>autocomplete</c> or <c>name</c>.</summary>
    [Parameter]
    public IReadOnlyDictionary<string, object>? InputAttributes { get; set; }

    /// <summary>The control's accessible name when the field shows no <see cref="Label"/>, such as a search box marked
    /// only by an icon. It is read like a label but not shown; a visible <see cref="Label"/> is better where there is
    /// room.</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>Raised when focus moves into the field from outside it. Moves between the field's own parts, such as a
    /// range's two boxes, a select's filter or a group's items, don't count.</summary>
    [Parameter]
    public EventCallback<FocusEventArgs> OnFocus { get; set; }

    /// <summary>Raised when focus leaves the field for somewhere outside it, for example to check or save what was
    /// entered when the user moves on. Moves between the field's own parts don't count; a field placed in another
    /// field's slot counts as outside it.</summary>
    [Parameter]
    public EventCallback<FocusEventArgs> OnBlur { get; set; }

    /// <summary>Moves focus to the control, as a dialog or a search page does with its first field, when it appears on
    /// an interactive page, unless the user is typing in another field by then. In a dialog or focus scope it takes
    /// focus as that opens. Give it to one field at most.</summary>
    [Parameter]
    public bool AutoFocus { get; set; }

    /// <summary>A label this field's use is counted under, such as <c>"email"</c>, so the app can see how people use
    /// it through the <c>Trailblazor</c> meter: counted in <c>trailblazor.control.shown</c> when it first renders, and
    /// in <c>trailblazor.field.invalid</c> each time its <see cref="TbForm"/> is submitted while it has an error. A
    /// checkbox, switch, select or list box also counts each change or choice. Its value is never recorded. It names
    /// what the field holds, never the user.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [CascadingParameter]
    private EditContext? CascadedEditContext { get; set; }

    [CascadingParameter]
    private TbForm? Form { get; set; }

    [CascadingParameter]
    private FormMappingContext? MappingContext { get; set; }

    [Inject]
    private protected IAnnouncer Announcer { get; set; } = default!;

    [Inject]
    private protected ITbLocalizer Localizer { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    [CascadingParameter]
    private TbFocusScope? FocusScope { get; set; }

    private protected ControlTelemetry Telemetry => _telemetry ??= new(Services, GetType());

    private protected string FieldId { get; } = $"tb-field-{Guid.NewGuid():N}";

    // An id given to the field names its control, so the label, styles, scripts and tests using it reach the control.
    private protected string InputId => IdOfControl ?? $"{FieldId}-input";

    // A field with two controls, such as a range, keeps an id on its root. Groups do too, since their roots don't use
    // RootAttributes.
    private protected virtual bool IdNamesControl => true;

    private string? IdOfControl => IdNamesControl ? GivenId : null;

    // The root's attributes, less an id that names the control.
    private protected IReadOnlyDictionary<string, object>? RootAttributes =>
        WithFocusEvents(IdOfControl is null ? AdditionalAttributes : AttributesWithoutId);

    // A group's root keeps its id, since its items each have their own, and carries the group's autofocus.
    private protected IReadOnlyDictionary<string, object>? GroupRootAttributes =>
        WithFocusEvents(MarkAutoFocus ? WithAutofocus(AdditionalAttributes) : AdditionalAttributes);

    private protected string LabelId => $"{FieldId}-label";

    // An AriaLabel is drawn as a label only screen readers see, so every part named by the label is named by it too.
    private protected string? ShownLabel => Label ?? AriaLabel;

    private protected string? LabelHiddenClass => Label is null && AriaLabel is not null ? "tb-visually-hidden" : null;

    // A clear button is described by the field's label, so a screen reader says which field it clears; its name stays
    // "Clear", since a name holding the label would also match a test looking the field up by its label.
    private protected string? ClearDescribedBy => ShownLabel is null ? null : LabelId;

    // The consumer's input attributes, marked for focus on an interactive page when asked, which the browser, dialogs
    // and focus scopes look for. It isn't the native autofocus, which browsers act on even where a closed dialog or a
    // focus scope should place focus. A field with several controls marks one itself: a group its root, a range its
    // first part.
    private protected IReadOnlyDictionary<string, object>? ControlAttributes { get; private set; }

    private protected string? AutoFocusMark => MarkAutoFocus ? "" : null;

    // RendererInfo is read only when asked for, since a test renderer may never set it.
    private bool MarkAutoFocus => AutoFocus && RendererInfo.IsInteractive;

    // A label's for attribute works only on form controls; other inputs name themselves with aria-labelledby.
    private protected virtual bool LabelTargetsInput => true;

    // A field drawn in a box adds its look and size.
    private protected virtual string? FieldModifiers => null;

    private protected string HelperId => $"{FieldId}-helper";

    private protected string ErrorId => $"{FieldId}-error";

    // The input attributes come first, so ours win; where ours are empty the consumer's still apply.
    private protected object? InputNameAttribute => _inputName ?? InputAttributes?.GetValueOrDefault("name");

    // Radios group by their shared name; only a static form post needs that name to be the bound field path.
    private protected string RadioGroupName => (RendererInfo.IsInteractive
        ? InputAttributes?.GetValueOrDefault("name")?.ToString()
        : InputNameAttribute?.ToString()) ?? FieldId;

    private protected string? InputDescribedBy =>
        (DescribedBy, InputAttributes?.GetValueOrDefault("aria-describedby")) switch
        {
            (null, var theirs) => theirs as string,
            (var ours, string theirs) => $"{ours} {theirs}",
            (var ours, _) => ours,
        };

    // Typing that has updated the value stays as typed until the field commits it, unless the value has moved on.
    private protected string? Text => _unparsedText
        ?? (_typedText is not null && EqualityComparer<TValue>.Default.Equals(Value, _typedValue) ? _typedText
            : FormatValue(Value));

    private protected bool HasParseError => _parseError is not null;

    // Inside an EditForm the context holds every validator's messages for this field, including ours.
    private protected IReadOnlyList<string> Errors =>
        _messageStore is null ? OwnMessages : _editContext!.GetValidationMessages(_field!.Value).ToList();

    private List<string> OwnMessages => ParentError is null ? _messages : [ParentError, .. _messages];

    // Whether the field looks invalid: it has a message, or the parent marked it so.
    private protected bool ShowsInvalid => Errors.Count > 0 || Invalid;

    // What counts as no value for Required: null, blank text or an empty list.
    private protected virtual bool IsMissing(TValue? value) => value switch
    {
        null => true,
        string text => string.IsNullOrWhiteSpace(text),
        System.Collections.IEnumerable items => !items.GetEnumerator().MoveNext(),
        _ => false,
    };

    // A field the user can't change isn't asked of them, as the browser's own validation skips one.
    private bool MissesRequired => Required && !Disabled && !ReadOnly && _parseError is null && IsMissing(Value);

    // What's wrong with a value however it arrived (typed, posted or set by code), such as a number past a limit.
    private protected virtual string? ValueError(TValue? value) => null;

    private string? ValueProblem => Disabled || ReadOnly || _parseError is not null ? null : ValueError(Value);

    // A blank error from the parent is no error, so it doesn't mark the field invalid.
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
        ControlAttributes = MarkAutoFocus ? WithAutofocus(InputAttributes) : InputAttributes;
        // A value the field didn't report itself came from the parent, and a form reset goes back to it.
        if (!EqualityComparer<TValue>.Default.Equals(Value, _lastValue))
        {
            _lastValue = _valueToResetTo = Value;
        }

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

        // A static form post whose text didn't convert keeps the model's old value, so the field shows what was typed.
        // Only the model the error belongs to, inside a form that reports it, gets the text back. RendererInfo is read
        // last, since a test renderer that never sets it throws.
        if (MappingContext is { } mapping && _field is { } bound && !RendererInfo.IsInteractive
            && mapping.GetAllErrors().FirstOrDefault(error => ReferenceEquals(error.Container, bound.Model)
                && error.Name == bound.FieldName) is { AttemptedValue: { } attempted })
        {
            _unparsedText = attempted;
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
                Announce(_shownError, [_shownError]);
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

    // Blazor reads a static form's post in the invariant culture, so a static page writes its numbers and dates that
    // way. Text needs no culture, which also leaves RendererInfo unread where a test renderer never sets it.
    private protected CultureInfo CultureFor(Type type) =>
        typeof(IFormattable).IsAssignableFrom(Nullable.GetUnderlyingType(type) ?? type) && AssignedRenderMode is null
            && !RendererInfo.IsInteractive ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;

    /// <summary>Turns a value into the text the input shows.</summary>
    private protected virtual string? FormatValue(TValue? value) =>
        BindConverter.FormatValue(value, CultureFor(typeof(TValue)))?.ToString();

    /// <summary>Turns the input's text into a value, or explains why it can't.</summary>
    private protected virtual bool TryParse(string? text, out TValue? value, out string? error)
    {
        if (BindConverter.TryConvertTo(text, CultureFor(typeof(TValue)), out value))
        {
            error = null;
            return true;
        }

        error = Message(TbStrings.FieldInvalid);
        return false;
    }

    // A message about the field: the key's text with the field's label, or a stand-in, first and then the args.
    private protected string Message(string key, params object?[] args) => string.Format(CultureInfo.CurrentCulture,
        Localizer[key], [ShownLabel ?? Localizer[TbStrings.FieldUnnamed], .. args]);

    // Typed text, from a field that updates as the user types, stays with the value it was typed for.
    private protected async Task SetValueAsync(TValue? value, string? typedText = null)
    {
        (_typedText, _typedValue) = (typedText, value);
        if (_parseError is null && EqualityComparer<TValue>.Default.Equals(value, Value))
        {
            return;
        }

        var before = Errors.ToList();
        _parseError = null;
        _unparsedText = null;
        _lastValue = Value = value;
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
        _typedText = null;
        if (TryParse(text, out var value, out var error))
        {
            await SetValueAsync(value);
            return;
        }

        ShowParseError(error!, text);
    }

    // A field that updates as the user types takes text that parses; all of it stays as typed until the field commits,
    // and an error the last commit showed stays with it.
    private protected async Task SetTypedTextAsync(string? text, bool parse = true)
    {
        if (parse && TryParse(text, out var value, out _))
        {
            await SetValueAsync(value, text);
        }
        else if (_unparsedText is not null)
        {
            _unparsedText = text;
        }
        else
        {
            (_typedText, _typedValue) = (text, Value);
        }
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
            Announce(string.Join(" ", added), added);
        }
    }

    private void Announce(string message, List<string> errors)
    {
        _announced.Add((Announcer.Polite(message), errors));
    }

    // A message about an error that's gone would mislead whoever finds it in the live region later.
    private void WithdrawFixedErrors(IReadOnlyList<string> errors)
    {
        foreach (var announced in _announced.Where(announced => announced.Errors.Except(errors).Any()).ToList())
        {
            Announcer.Withdraw(announced.Handle);
            _announced.Remove(announced);
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

        if (ValueProblem is { } problem)
        {
            _messages.Add(problem);
        }

        if (MissesRequired)
        {
            _messages.Add(RequiredMessage ?? Message(TbStrings.FieldRequired));
        }
        else if (Validation is not null)
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

    bool IFormField.HasProblems() =>
        _parseError is not null || ValueProblem is not null || MissesRequired
        || (Validation?.Invoke(Value).Any() ?? false);

    void IFormField.CountIfInvalid()
    {
        if (Errors.Count > 0)
        {
            Telemetry.Invalid(TelemetryName);
        }
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        Telemetry.Rendered(TelemetryName);

        // Every way an error goes away ends in a render, whether an edit, a form reset or another validator did it.
        if (_announced.Count > 0)
        {
            WithdrawFixedErrors(Errors);
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // A focus scope decides when its field takes focus; elsewhere the browser focuses the marked control as it is
        // added (autofocus.js), or an opening dialog does.
        if (firstRender && AutoFocus && RendererInfo.IsInteractive && FocusScope is not null)
        {
            try
            {
                await FocusScope.FocusWhenActiveAsync(() => FocusAsync());
            }
            catch (JSDisconnectedException)
            {
                // The circuit is gone, so there is nothing to focus.
            }
        }
    }

    // The browser tells whether focus crossed the root, since .NET's focus events don't say where focus went.
    private IReadOnlyDictionary<string, object>? WithFocusEvents(IReadOnlyDictionary<string, object>? attributes)
    {
        if (!OnFocus.HasDelegate && !OnBlur.HasDelegate)
        {
            return attributes;
        }

        var withEvents = attributes?.ToDictionary() ?? [];
        withEvents["data-tb-focus-root"] = "";
        if (OnFocus.HasDelegate)
        {
            withEvents["ontbfocusenter"] = EventCallback.Factory.Create<FieldFocusEventArgs>(this, crossing =>
                crossing.CrossesField ? OnFocus.InvokeAsync(new() { Type = "focus" }) : Task.CompletedTask);
        }

        if (OnBlur.HasDelegate)
        {
            withEvents["ontbfocusleave"] = EventCallback.Factory.Create<FieldFocusEventArgs>(this, crossing =>
                crossing.CrossesField ? OnBlur.InvokeAsync(new() { Type = "blur" }) : Task.CompletedTask);
        }

        return withEvents;
    }

    private static Dictionary<string, object> WithAutofocus(IReadOnlyDictionary<string, object>? attributes)
    {
        var marked = attributes?.ToDictionary() ?? [];
        marked["data-tb-autofocus"] = "";
        return marked;
    }

    // The parent's error stays: only the parent can clear it.
    void IFormField.HideMessages()
    {
        _messages.Clear();
        Store();
    }

    // The field runs no check of its own and announces nothing: a reset field looks untouched, even if the value it goes
    // back to would fail.
    async Task IFormField.ResetAsync()
    {
        (_parseError, _unparsedText, _typedText) = (null, null, null);
        ((IFormField)this).HideMessages();
        var changed = !EqualityComparer<TValue>.Default.Equals(Value, _valueToResetTo);
        if (changed)
        {
            _lastValue = Value = _valueToResetTo;
            await ValueChanged.InvokeAsync(Value!);
        }

        // An earlier field's reset can remove this one.
        if (_disposed)
        {
            return;
        }

        // A message a validator inside the form is still showing may no longer hold, so it checks the field again.
        if (changed && Errors.Except(OwnMessages).Any())
        {
            _editContext!.NotifyFieldChanged(_field!.Value);
        }

        await OnResetAsync();
    }

    // Override to clear what a field holds beside its value when a form resets.
    private protected virtual Task OnResetAsync() => Task.CompletedTask;

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
        _form?.Remove(this);
        DetachEditContext();
        WithdrawFixedErrors([]);
    }
}
