using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Trailblazor.Tests.Field;

public class FieldRequiredTests : BunitContext
{
    public FieldRequiredTests() => this.AddTrailblazor();

    [Theory]
    [InlineData(typeof(TbTextField<string>))]
    [InlineData(typeof(TbCheckbox<bool>))]
    [InlineData(typeof(TbCheckboxGroup<string>))]
    [InlineData(typeof(TbRadioGroup<string>))]
    [InlineData(typeof(TbRating))]
    [InlineData(typeof(TbSignaturePad))]
    [InlineData(typeof(TbSlider<int>))]
    [InlineData(typeof(TbTransferList<string>))]
    [InlineData(typeof(TbFileUpload))]
    [InlineData(typeof(TbCalendar<DateOnly>))]
    [InlineData(typeof(TbLabel))]
    public void Label_ShouldPutTheRequiredMarkRightAfterItsText_WhenTheFieldIsRequired(Type field)
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent(0, field);
            builder.AddAttribute(1, field == typeof(TbLabel) ? "Text" : "Label", "Trail");
            builder.AddAttribute(2, "Required", true);
            builder.CloseComponent();
        });

        var mark = cut.Find(".tb-field__required, .tb-label__required");
        Assert.False(mark.PreviousSibling is AngleSharp.Dom.IText text && text.Data.Length > 0
            && char.IsWhiteSpace(text.Data[^1]), $"A space comes before the mark in {mark.ParentElement!.OuterHtml}");
    }

    [Fact]
    public void TextField_ShouldMarkItselfRequired_WhenTheBoundPropertyHasRequired()
    {
        var cut = RenderForm(new Signup(), signup => () => signup.Email);

        Assert.Equal("true", cut.Find("input").GetAttribute("aria-required"));
        Assert.NotNull(cut.Find(".tb-field__required"));
    }

    [Fact]
    public void TextField_ShouldNotMarkItselfRequired_WhenTheBoundPropertyHasNoRequired()
    {
        var cut = RenderForm(new Signup(), signup => () => signup.Nickname);

        Assert.Null(cut.Find("input").GetAttribute("aria-required"));
        Assert.Empty(cut.FindAll(".tb-field__required"));
    }

    [Fact]
    public void TextField_ShouldShowOneMessage_WhenItsRequiredAndTheModelsRequiredBothApply()
    {
        var cut = RenderForm(new Signup(), signup => () => signup.Email, required: true);

        cut.Find("form").Submit();

        Assert.Equal(["Email is required."], Messages(cut));
    }

    [Fact]
    public void TextField_ShouldShowTheModelsOwnMessage_WhenItsRequiredSetsOne()
    {
        var cut = RenderForm(new Signup(), signup => () => signup.Trail);

        cut.Find("form").Submit();

        Assert.Equal(["Pick a trail."], Messages(cut));
    }

    [Fact]
    public void TextField_ShouldShowOnlyTheRequiredMessage_WhenClearedWithAnEmailRuleOnTheModel()
    {
        var cut = RenderForm(new Signup { Email = "ada@example.com" }, signup => () => signup.Email);

        cut.Find("input").Change("");

        Assert.Equal(["Email is required."], Messages(cut));
    }

    [Fact]
    public void TextField_ShouldShowTheEmailMessage_WhenTheTextIsNotAnEmail()
    {
        var cut = RenderForm(new Signup(), signup => () => signup.Email);

        cut.Find("input").Change("ada");

        Assert.Equal(["The Email field is not a valid e-mail address."], Messages(cut));
    }

    [Fact]
    public void TextField_ShouldNotMarkItselfRequired_WhenTheModelsRequiredAllowsEmptyText()
    {
        var cut = RenderForm(new Signup(), signup => () => signup.Note);

        Assert.Null(cut.Find("input").GetAttribute("aria-required"));
        Assert.Empty(cut.FindAll(".tb-field__required"));
    }

    [Fact]
    public void FileUpload_ShouldMarkItselfRequired_WhenTheBoundPropertyHasRequired()
    {
        var application = new Application();

        var cut = Render<TbFileUpload>(parameters => parameters.Add(upload => upload.Label, "Permit")
            .Add(upload => upload.ValueExpression, () => application.Permit));

        Assert.Equal("true", cut.Find("input[type=file]").GetAttribute("aria-required"));
        Assert.NotNull(cut.Find(".tb-field__required"));
    }

    [Fact]
    public void TextField_ShouldKeepTheAppsAriaRequired_WhenNotRequiredItself()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Label, "Email")
            .Add(field => field.InputAttributes, new Dictionary<string, object> { ["aria-required"] = "true" }));

        Assert.Equal("true", cut.Find("input").GetAttribute("aria-required"));
    }

    [Fact]
    public void Checkbox_ShouldNeitherMarkNorBlockTheSubmit_WhenBoundToARequiredBool()
    {
        var agreement = new Agreement();
        var submitted = 0;
        var cut = RenderTbForm(agreement, () => submitted++, builder =>
        {
            builder.OpenComponent<TbCheckbox<bool>>(0);
            builder.AddAttribute(1, nameof(TbCheckbox<bool>.Label), "Send me trail news");
            builder.AddAttribute(2, nameof(TbCheckbox<bool>.Value), agreement.Subscribe);
            builder.AddAttribute(3, nameof(TbCheckbox<bool>.ValueExpression),
                (Expression<Func<bool>>)(() => agreement.Subscribe));
            builder.CloseComponent();
        });

        cut.Find("form").Submit();

        Assert.Equal(1, submitted);
        Assert.Null(cut.Find("input").GetAttribute("aria-required"));
        Assert.Empty(cut.FindAll(".tb-field__required"));
    }

    [Fact]
    public void MultiListBox_ShouldLetTheFormSubmit_WhenARequiredListIsEmpty()
    {
        var agreement = new Agreement();
        var submitted = 0;
        var cut = RenderTbForm(agreement, () => submitted++, builder =>
        {
            builder.OpenComponent<TbMultiListBox<string>>(0);
            builder.AddAttribute(1, nameof(TbMultiListBox<string>.Label), "Tags");
            builder.AddAttribute(2, nameof(TbMultiListBox<string>.Value), agreement.Tags);
            builder.AddAttribute(3, nameof(TbMultiListBox<string>.ValueExpression),
                (Expression<Func<IReadOnlyList<string>>>)(() => agreement.Tags));
            builder.CloseComponent();
        });

        cut.Find("form").Submit();

        Assert.Equal(1, submitted);
        Assert.Empty(cut.FindAll("[data-part=error]"));
        Assert.Empty(cut.FindAll(".tb-field__required"));
    }

    [Fact]
    public void Form_ShouldListOneMessage_WhenARequiredFieldIsEmptyWithTheModelsRequiredAndEmailRules()
    {
        var signup = new Contact { Email = "" };
        IReadOnlyList<string> errors = [];
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.Model, signup)
            .Add(form => form.DataAnnotations, true)
            .Add(form => form.ErrorsChanged, (IReadOnlyList<string> shown) => errors = shown)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbTextField<string?>>(0);
                builder.AddAttribute(1, nameof(TbTextField<string?>.Label), "Email");
                builder.AddAttribute(2, nameof(TbTextField<string?>.Value), signup.Email);
                builder.AddAttribute(3, nameof(TbTextField<string?>.ValueExpression),
                    (Expression<Func<string?>>)(() => signup.Email));
                builder.AddAttribute(4, nameof(TbTextField<string?>.Required), true);
                builder.CloseComponent();
            })));

        cut.Find("form").Submit();

        Assert.Equal(["Email is required."], errors);
        Assert.Equal(["Email is required."],
            cut.FindAll("[data-part=error]").Select(error => error.TextContent.Trim()));
    }

    private static List<string> Messages(IRenderedComponent<EditForm> cut) =>
        cut.FindAll("[data-part=error]").Select(error => error.TextContent.Trim()).ToList();

    private IRenderedComponent<EditForm> RenderForm(Signup signup, Func<Signup, Expression<Func<string?>>> bound,
        bool required = false)
    {
        var expression = bound(signup);
        var property = (System.Reflection.PropertyInfo)((MemberExpression)expression.Body).Member;
        return Render<EditForm>(parameters => parameters.Add(form => form.Model, signup)
            .Add(form => form.ChildContent, (EditContext _) => builder =>
            {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();
                builder.OpenComponent<TbTextField<string?>>(1);
                builder.AddAttribute(2, nameof(TbTextField<string?>.Label), property.Name);
                builder.AddAttribute(3, nameof(TbTextField<string?>.Value), (string?)property.GetValue(signup));
                builder.AddAttribute(4, nameof(TbTextField<string?>.ValueChanged),
                    EventCallback.Factory.Create<string?>(this, text => property.SetValue(signup, text)));
                builder.AddAttribute(5, nameof(TbTextField<string?>.ValueExpression), expression);
                builder.AddAttribute(6, nameof(TbTextField<string?>.Required), required);
                builder.CloseComponent();
            }));
    }

    [Fact]
    public void Form_ShouldListTheModelsOwnMessageOnce_WhenARequiredFieldWithOneIsEmpty()
    {
        var signup = new Signup();
        IReadOnlyList<string> errors = [];
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.Model, signup)
            .Add(form => form.DataAnnotations, true)
            .Add(form => form.ErrorsChanged, (IReadOnlyList<string> shown) => errors = shown)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                foreach (var (sequence, name, bound) in new[]
                {
                    (0, "Email", (Expression<Func<string?>>)(() => signup.Email)),
                    (10, "Trail", () => signup.Trail),
                })
                {
                    builder.OpenComponent<TbTextField<string?>>(sequence);
                    builder.AddAttribute(sequence + 1, nameof(TbTextField<string?>.Label), name);
                    builder.AddAttribute(sequence + 2, nameof(TbTextField<string?>.ValueExpression), bound);
                    builder.CloseComponent();
                }
            })));

        cut.Find("form").Submit();

        Assert.Equal(["Email is required.", "Pick a trail."], errors);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EditContext_ShouldHoldOneRequiredMessagePerField_WhenAFieldIsEmptied(bool submitted)
    {
        var booking = new Booking { Email = "ash@example.com", Trail = "North", Camp = "Lake", Phone = "555" };
        var context = new EditContext(booking);
        var fields = new (string Label, Expression<Func<string?>> Bound, Action<string?> Set, string? Message)[]
        {
            ("Email", () => booking.Email, text => booking.Email = text, null),
            ("Trail", () => booking.Trail, text => booking.Trail = text, null),
            ("Camp", () => booking.Camp, text => booking.Camp = text, "Choose a camp."),
            ("Phone number", () => booking.Phone, text => booking.Phone = text, null),
        };
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.EditContext, context)
            .Add(form => form.DataAnnotations, true)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                foreach (var (label, bound, set, message) in fields)
                {
                    builder.OpenComponent<TbTextField<string?>>(0);
                    builder.AddAttribute(1, nameof(TbTextField<string?>.Label), label);
                    builder.AddAttribute(2, nameof(TbTextField<string?>.Value), bound.Compile()());
                    builder.AddAttribute(3, nameof(TbTextField<string?>.ValueChanged),
                        EventCallback.Factory.Create(this, set));
                    builder.AddAttribute(4, nameof(TbTextField<string?>.ValueExpression), bound);
                    builder.AddAttribute(5, nameof(TbTextField<string?>.Required), true);
                    builder.AddAttribute(6, nameof(TbTextField<string?>.RequiredMessage), message);
                    builder.CloseComponent();
                }
            })));

        for (var index = 0; index < fields.Length; index++)
        {
            cut.FindAll("input")[index].Change("");
        }

        if (submitted)
        {
            cut.Find("form").Submit();
        }

        Assert.Equal([1, 1, 1, 1],
            fields.Select(field => context.GetValidationMessages(FieldIdentifier.Create(field.Bound)).Count()));
    }

    [Fact]
    public async Task Field_ShouldShowItsOwnWording_WhenTheModelsRequiredSetsAMessageAndTheUserCantChangeIt()
    {
        var booking = new Booking();
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.Model, booking)
            .Add(form => form.DataAnnotations, true)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbTextField<string?>>(0);
                builder.AddAttribute(1, nameof(TbTextField<string?>.Label), "Phone number");
                builder.AddAttribute(2, nameof(TbTextField<string?>.ValueExpression),
                    (Expression<Func<string?>>)(() => booking.Phone));
                builder.AddAttribute(3, nameof(TbTextField<string?>.Disabled), true);
                builder.CloseComponent();
            })));

        await cut.InvokeAsync(() => cut.Instance.ValidateAsync());

        Assert.Equal(["Phone number is required."],
            cut.FindAll("[data-part=error]").Select(error => error.TextContent));
    }

    private IRenderedComponent<TbForm> RenderTbForm(object model, Action submitted, RenderFragment fields) =>
        Render<TbForm>(parameters => parameters.Add(form => form.Model, model).Add(form => form.DataAnnotations, true)
            .Add(form => form.OnValidSubmit, (EditContext _) => submitted())
            .Add(form => form.ChildContent, fields));

    private sealed class Contact
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
    }

    private sealed class Application
    {
        [Required]
        public IReadOnlyList<IBrowserFile>? Permit { get; set; }
    }

    private sealed class Agreement
    {
        [Required]
        public bool Subscribe { get; set; }

        [Required]
        public IReadOnlyList<string> Tags { get; set; } = [];
    }

    // Each field's [Required] words its message another way: the default, the field's own text, a message the field
    // replaces, and a {0} that names the member rather than the field's label.
    private sealed class Booking
    {
        [Required]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Trail is required.")]
        public string? Trail { get; set; }

        [Required(ErrorMessage = "Pick a camp.")]
        public string? Camp { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        public string? Phone { get; set; }
    }

    private sealed class Signup
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Pick a trail.")]
        public string? Trail { get; set; }

        public string? Nickname { get; set; }

        [Required(AllowEmptyStrings = true)]
        public string? Note { get; set; } = "";
    }
}
