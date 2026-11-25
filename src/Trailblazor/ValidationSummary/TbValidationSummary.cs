using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A list of the messages a <see cref="TbForm"/> shows, each field's message a link that moves focus to the
/// field, for a long form where the fields with problems are out of sight. Put it inside the form, usually at its top.
/// It shows only while there are messages; a failed submit still moves focus to the first field with one.</summary>
public partial class TbValidationSummary
{
    private readonly string _titleId = $"tb-validation-summary-{Guid.NewGuid():N}";
    private readonly ItemHandlers<IFormField, Func<Task>> _follow = new(member => async () => await member.FocusAsync());
    private TbForm? _registered;

    [CascadingParameter]
    private TbForm? Form { get; set; }

    [Inject]
    private ITbLocalizer Localizer { get; set; } = default!;

    /// <summary>The heading above the list. Leave it null for the app's text for
    /// <see cref="TbStrings.ValidationSummaryTitle"/>, "Fix these to continue" in English.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>The level of the heading, from 1 to 6, so it fits the page's outline. Defaults to 2, for a summary
    /// under the page's main heading.</summary>
    [Parameter]
    public int HeadingLevel { get; set; } = 2;

    private string TitleText => Title ?? Localizer[TbStrings.ValidationSummaryTitle];

    private RenderFragment Heading => builder =>
    {
        builder.OpenElement(0, $"h{HeadingLevel}");
        builder.AddAttribute(1, "id", _titleId);
        builder.AddAttribute(2, "class", "tb-validation-summary__title");
        builder.AddAttribute(3, "data-part", "title");
        builder.AddContent(4, TitleText);
        builder.CloseElement();
    };

    // Each field's messages in the order the fields came, then the form's messages that belong to no field. A field's
    // message takes one copy of the same text from the form's, so a field-less message with that text still shows.
    private List<(string Message, IFormField? Field)> Items
    {
        get
        {
            var items = Form!.Fields.SelectMany(member => member.ShownErrors()
                .Select(message => (message, (IFormField?)member))).ToList();
            var unclaimed = Form.Errors.ToList();
            foreach (var (message, _) in items)
            {
                unclaimed.Remove(message);
            }

            items.AddRange(unclaimed.Select(message => (message, (IFormField?)null)));
            return items;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(HeadingLevel, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(HeadingLevel, 6);
        if (Form is null)
        {
            throw new InvalidOperationException($"{nameof(TbValidationSummary)} lists a form's messages, so it goes "
                + $"inside a {nameof(TbForm)}.");
        }

        if (_registered != Form)
        {
            _registered?.Remove(this);
            (_registered = Form).Add(this);
        }
    }

    internal void Refresh() => _ = InvokeAsync(() =>
    {
        _follow.KeepOnly(Form!.Fields);
        StateHasChanged();
    });

    /// <inheritdoc />
    public void Dispose() => _registered?.Remove(this);
}
