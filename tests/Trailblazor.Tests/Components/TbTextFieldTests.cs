using System.Globalization;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests.Components;

public class TbTextFieldTests : BunitContext
{
    public TbTextFieldTests()
    {
        this.AddTrailblazor();
    }

    [Theory]
    [InlineData(null, "Name is required.")]
    [InlineData("Tell us your name.", "Tell us your name.")]
    public void Change_ShouldShowTheRequiredMessage_WhenARequiredFieldIsEmptied(string? message, string expected)
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Label, "Name")
            .Add(field => field.Value, "Ada").Add(field => field.Required, true)
            .Add(field => field.RequiredMessage, message));
        Assert.Empty(cut.FindAll("[data-part=error]"));

        cut.Find("input").Change("  ");

        Assert.Equal(expected, cut.Find("[data-part=error]").TextContent.Trim());
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
        cut.Find("input").Change("Ada");
        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public void Change_ShouldShowOnlyTheRequiredMessage_WhenTheFieldsOwnValidationAlsoRejectsItsEmptiness()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Label, "Name")
            .Add(field => field.Value, "Ada").Add(field => field.Required, true)
            .Add(field => field.Validation, (string? name) => name?.Trim().Length switch
            {
                null or 0 => ["Enter a name."],
                1 => ["Too short."],
                _ => [],
            }));

        cut.Find("input").Change(" ");
        Assert.Equal(["Name is required."], cut.FindAll("[data-part=error] > div").Select(error => error.TextContent));

        cut.Find("input").Change("A");
        Assert.Equal(["Too short."], cut.FindAll("[data-part=error] > div").Select(error => error.TextContent));
    }

    [Fact]
    public void Render_ShouldLinkTheLabelAndHelperToTheInput_WhenBothAreSet()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Label, "Name")
            .Add(field => field.HelperText, "As on your badge")
            .Add(field => field.Required, true));

        var input = cut.Find("[data-part=input]");
        Assert.Equal(input.Id, cut.Find("label").GetAttribute("for"));
        Assert.Equal(cut.Find("[data-part=helper]").Id, input.GetAttribute("aria-describedby"));
        Assert.Equal("true", input.GetAttribute("aria-required"));
        Assert.Null(input.GetAttribute("aria-invalid"));
    }

    [Theory]
    [InlineData(1, false, null, "onchange")]
    [InlineData(1, true, null, "oninput")]
    [InlineData(1, true, 300, "onchange")]
    [InlineData(3, false, null, "onchange")]
    [InlineData(3, true, null, "oninput")]
    public async Task Change_ShouldReachTheField_WhenItRendersBetweenATestReadingTheInputAndRaisingItsEvent(int lines,
        bool updateOnInput, int? delay, string committing)
    {
        string? name = null;
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Lines, lines)
            .Add(field => field.UpdateOnInput, updateOnInput)
            .Add(field => field.UpdateDelay, delay is { } ms ? TimeSpan.FromMilliseconds(ms) : null)
            .Add(field => field.ValueChanged, (string value) => name = value));
        var handler = ulong.Parse(cut.Find("[data-part=input]").GetAttribute($"blazor:{committing}")!,
            CultureInfo.InvariantCulture);

        // As when a background continuation renders the field while a test is raising the event it found.
        cut.Render();
#pragma warning disable BL0006 // Dispatching by handler id, as the browser does, needs RenderTree's EventFieldInfo.
        var field = new Microsoft.AspNetCore.Components.RenderTree.EventFieldInfo
        {
            ComponentId = cut.ComponentId,
            FieldValue = "Ada",
        };
        await Renderer.DispatchEventAsync(handler, field, new ChangeEventArgs { Value = "Ada" });
#pragma warning restore BL0006

        Assert.Equal("Ada", name);
    }

    [Fact]
    public async Task Change_ShouldAwaitValueChanged_WhenTheTextIsCommitted()
    {
        var handler = new TaskCompletionSource();
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.ValueChanged, (string _) => handler.Task));

        var change = cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = "Ada" });
        Assert.False(change.IsCompleted);
        handler.SetResult();
        await change;
    }

    [Fact]
    public async Task FocusAsync_ShouldFocusTheInput_WhenCalled()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Lines, 3));

        await cut.InvokeAsync(() => cut.Instance.FocusAsync());

        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(cut.Find("textarea"));
    }

    [Fact]
    public async Task SelectTextAsync_ShouldSelectTheInputsText_WhenCalled()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/field.js");
        module.SetupVoid("selectText", _ => true).SetVoidResult();
        var cut = Render<TbTextField<string>>();

        await cut.InvokeAsync(() => cut.Instance.SelectTextAsync());

        module.VerifyInvoke("selectText").Arguments[0].ShouldBeElementReferenceTo(cut.Find("input"));
    }

    [Theory]
    [InlineData(1, "input")]
    [InlineData(3, "textarea")]
    public void Render_ShouldMakeTheInputReadOnlyButNotDisabled_WhenReadOnly(int lines, string element)
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.ReadOnly, true)
            .Add(field => field.Lines, lines));

        var input = cut.Find(element);
        Assert.True(input.HasAttribute("readonly"));
        Assert.False(input.HasAttribute("disabled"));
        Assert.Contains("tb-field--readonly", cut.Find(".tb-field").ClassList);
    }

    [Fact]
    public void Render_ShouldNotRaiseValueChanged_WhenTheParentSetsTheValue()
    {
        var raised = false;
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Value, "Ada")
            .Add(field => field.ValueChanged, (string _) => raised = true));

        cut.Render(parameters => parameters.Add(field => field.Value, "Grace"));

        Assert.Equal("Grace", cut.Find("input").GetAttribute("value"));
        Assert.False(raised);
    }

    [Fact]
    public void Input_ShouldNotBeWired_WhenUpdateOnInputIsOff()
    {
        var cut = Render<TbTextField<string>>();

        Assert.Throws<MissingEventHandlerException>(() => cut.Find("input").Input("A"));
    }

    [Fact]
    public void Input_ShouldUpdateTheValue_WhenUpdateOnInputIsOn()
    {
        string? value = null;
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.UpdateOnInput, true)
            .Add(field => field.ValueChanged, (string text) => value = text));

        cut.Find("input").Input("A");

        Assert.Equal("A", value);
        Assert.Throws<MissingEventHandlerException>(() => cut.Find("input").Change("AB"));
    }

    [Fact]
    public void Input_ShouldUpdateAtOnceAndLeaveTheWaitToTheBrowser_WhenGivenAnUpdateDelay()
    {
        var values = new List<string?>();
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.UpdateOnInput, true)
            .Add(field => field.UpdateDelay, TimeSpan.FromMilliseconds(300))
            .Add(field => field.ValueChanged, (string text) => values.Add(text)));

        cut.Find("input").Input("tea");

        Assert.Equal(["tea"], values);
        Assert.Equal("300", cut.Find("input").GetAttribute("data-input-delay"));
    }

    [Fact]
    public void Change_ShouldUpdateTheValueAtOnce_WhenTheFieldHasAnUpdateDelay()
    {
        var values = new List<string?>();
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.UpdateOnInput, true)
            .Add(field => field.UpdateDelay, TimeSpan.FromMilliseconds(300))
            .Add(field => field.ValueChanged, (string text) => values.Add(text)));

        cut.Find("input").Change("tea");

        Assert.Equal(["tea"], values);
    }

    [Fact]
    public void Render_ShouldThrow_WhenAClearableFieldsValueTypeIsNotNullable()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbTextField<int>>(parameters => parameters
            .Add(field => field.Clearable, true)));
    }

    [Fact]
    public void Render_ShouldDescribeTheClearButtonByTheFieldsLabel_WhenTheFieldHasALabel()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Label, "Search")
            .Add(field => field.Clearable, true).Add(field => field.Value, "tea"));

        var clear = cut.Find("[data-part=clear]");
        Assert.Equal("Search", cut.Find($"[id='{clear.GetAttribute("aria-describedby")}']").TextContent.Trim());
        // A name holding the label would also match a test that finds the field by its label.
        Assert.Empty(cut.FindAll("[aria-label*=Search]"));
    }

    [Theory]
    [InlineData("tea", false, false, true)]
    [InlineData("", false, false, false)]
    [InlineData("tea", true, false, false)]
    [InlineData("tea", false, true, false)]
    public void Render_ShouldShowTheClearButtonOnlyForTextThatCanChange_WhenClearable(string text, bool disabled,
        bool readOnly, bool shown)
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Clearable, true)
            .Add(field => field.Value, text).Add(field => field.Disabled, disabled)
            .Add(field => field.ReadOnly, readOnly));

        var clear = cut.FindAll("[data-part=clear]");
        Assert.Equal(shown, clear.Count == 1);
        if (shown)
        {
            Assert.Equal("Clear", clear[0].GetAttribute("aria-label"));
        }
    }

    [Fact]
    public async Task Clear_ShouldEmptyTheFieldAndFocusIt_WhenPressed()
    {
        var values = new List<string?>();
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Clearable, true)
            .Add(field => field.Labels, new() { Clear = "Clear search" }).Add(field => field.UpdateOnInput, true)
            .Add(field => field.UpdateDelay, TimeSpan.FromMilliseconds(300))
            .Add(field => field.ValueChanged, (string text) => values.Add(text)));
        // bUnit leaves the reference out of the markup once the input re-renders.
        var input = cut.Find("input").GetAttribute("blazor:elementreference");
        cut.Find("input").Input("tea");

        var clear = cut.Find("[data-part=clear]");
        Assert.Equal("Clear search", clear.GetAttribute("aria-label"));
        await cut.InvokeAsync(() => clear.Click());

        Assert.Equal(["tea", null], values);
        Assert.Null(cut.Find("input").GetAttribute("value"));
        Assert.Equal(input, ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);
    }

    [Theory]
    [InlineData(3, 8, "8")]
    [InlineData(3, null, null)]
    [InlineData(3, 3, null)]
    [InlineData(1, 8, null)]
    public void Render_ShouldLetATextAreaGrowToMaxLines_WhenMaxLinesIsAboveLines(int lines, int? maxLines,
        string? grows)
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Lines, lines)
            .Add(field => field.MaxLines, maxLines));

        var input = cut.Find("[data-part=input]");
        Assert.Equal(grows, input.GetAttribute("data-max-lines"));
        Assert.Equal(grows is null ? null : "--tb-text-field-lines: 3; --tb-text-field-max-lines: 8",
            input.GetAttribute("style"));
    }

    [Fact]
    public void Change_ShouldKeepTheTextAndShowAnError_WhenTheTextDoesNotParse()
    {
        var raised = false;
        var cut = Render<TbTextField<int>>(parameters => parameters
            .Add(field => field.Label, "Age")
            .Add(field => field.Value, 30)
            .Add(field => field.ValueChanged, (int _) => raised = true));

        cut.Find("input").Change("thirty");

        var input = cut.Find("input");
        Assert.Equal("thirty", input.GetAttribute("value"));
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
        Assert.Equal(cut.Find("[data-part=error]").Id, input.GetAttribute("aria-describedby"));
        Assert.Equal("Age isn't valid.", cut.Find("[data-part=error]").TextContent.Trim());
        Assert.False(raised);
    }

    [Fact]
    public void Render_ShouldDropTheUnparsedTextAndError_WhenTheParentSetsAnotherValue()
    {
        var cut = Render<TbTextField<int>>(parameters => parameters
            .Add(field => field.Label, "Age")
            .Add(field => field.Value, 30));
        cut.Find("input").Change("thirty");

        cut.Render(parameters => parameters.Add(field => field.Value, 40));

        Assert.Equal("40", cut.Find("input").GetAttribute("value"));
        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public void Render_ShouldKeepTheUnparsedTextAndError_WhenTheParentRendersTheSameValue()
    {
        var cut = Render<TbTextField<int>>(parameters => parameters
            .Add(field => field.Label, "Age")
            .Add(field => field.Value, 30));
        cut.Find("input").Change("thirty");

        cut.Render(parameters => parameters.Add(field => field.Value, 30));

        Assert.Equal("thirty", cut.Find("input").GetAttribute("value"));
        Assert.Equal("Age isn't valid.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldDropTheUnparsedText_WhenTheFieldIsReboundToAPropertyWithTheSameValue()
    {
        var first = new Ages();
        var second = new Ages();
        var cut = Render<EditForm>(parameters => parameters
            .Add(form => form.Model, first)
            .Add(form => form.ChildContent, (EditContext _) => AgeField(first)));
        cut.Find("input").Change("thirty");

        cut.Render(parameters => parameters.Add(form => form.ChildContent, (EditContext _) => AgeField(second)));

        Assert.Equal("0", cut.Find("input").GetAttribute("value"));
        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public void Change_ShouldClearTheParseError_WhenValidTextFollows()
    {
        var cut = Render<TbTextField<int>>(parameters => parameters.Add(field => field.Label, "Age"));
        cut.Find("input").Change("thirty");

        cut.Find("input").Change("31");

        Assert.Empty(cut.FindAll("[data-part=error]"));
        Assert.Equal("31", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Change_ShouldShowValidationMessages_WhenValidationFails()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Validation, (string? text) => text?.Length < 3 ? ["Too short."] : []));

        cut.Find("input").Change("Al");

        Assert.Equal("Too short.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void Change_ShouldAddEachPartsClass_WhenClassesAreSet()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Label, "Name")
            .Add(field => field.HelperText, "As on your badge")
            .Add(field => field.Start, (RenderFragment)(start => start.AddContent(0, "@")))
            .Add(field => field.End, (RenderFragment)(end => end.AddContent(0, "!")))
            .Add(field => field.Validation, (string? text) => text?.Length < 3 ? ["Too short."] : [])
            .Add(field => field.Classes, new FieldClasses
            {
                Label = "my-label",
                Control = "my-control",
                Input = "my-input",
                Start = "my-start",
                End = "my-end",
                Helper = "my-helper",
                Error = "my-error",
            }));

        cut.Find("input").Change("Al");

        foreach (var part in new[] { "label", "control", "input", "start", "end", "helper", "error" })
        {
            Assert.Contains($"my-{part}", cut.Find($"[data-part={part}]").ClassList);
        }
    }

    [Fact]
    public void Change_ShouldSpeakOnlyNewMessages_WhenEditsAddAndKeepThem()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Validation, Password));

        cut.Find("input").Change("abc");
        cut.Find("input").Change("abcd");

        Assert.Equal(["At least 8 characters. Include a digit."], Spoken(announcer));
    }

    [Fact]
    public void Change_ShouldWithdrawAnAnnouncedError_WhenAnEditFixesIt()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Validation, Password));
        cut.Find("input").Change("abcdefgh");
        Assert.Equal(["Include a digit."], Spoken(announcer));

        cut.Find("input").Change("abcdefg1");

        Assert.Empty(Spoken(announcer));
    }

    [Fact]
    public void Change_ShouldWithdrawAMessageWithoutSpeakingAgain_WhenAnEditFixesOneOfItsErrors()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Validation, Password));
        cut.Find("input").Change("abc");
        Assert.Equal(["At least 8 characters. Include a digit."], Spoken(announcer));

        cut.Find("input").Change("abc1");

        Assert.Equal("At least 8 characters.", cut.Find("[data-part=error]").TextContent.Trim());
        Assert.Empty(Spoken(announcer));
    }

    [Fact]
    public void Change_ShouldWithdrawTheFormsAnnouncedMessage_WhenAnEditFixesIt()
    {
        var announcer = Render<AnnouncerHost>();
        var signup = new Signup { Email = "ada@example.com" };
        var cut = Render<EditForm>(parameters => parameters.Add(form => form.Model, signup)
            .Add(form => form.ChildContent, (EditContext _) => EmailField(signup)));
        cut.Find("input").Change("");
        Assert.Equal(["Enter an email."], Spoken(announcer));

        cut.Find("input").Change("ada@example.com");

        Assert.Empty(Spoken(announcer));
    }

    [Fact]
    public void Render_ShouldWithdrawTheParentsAnnouncedError_WhenTheParentClearsIt()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = Render<TbTextField<string>>(parameters =>
            parameters.Add(field => field.Error, "That name is taken."));
        Assert.Equal(["That name is taken."], Spoken(announcer));

        cut.Render(parameters => parameters.Add(field => field.Error, null));

        Assert.Empty(Spoken(announcer));
    }

    [Fact]
    public void Render_ShouldLeaveAnotherFieldsSameMessage_WhenOneFieldsErrorIsCleared()
    {
        var announcer = Render<AnnouncerHost>();
        var first = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Error, "That name is taken."));
        Render<TbTextField<string>>(parameters => parameters.Add(field => field.Error, "That name is taken."));
        Assert.Equal(["That name is taken.", "That name is taken."], Spoken(announcer));

        first.Render(parameters => parameters.Add(field => field.Error, null));

        Assert.Equal(["That name is taken."], Spoken(announcer));
    }

    [Fact]
    public void Dispose_ShouldWithdrawTheFieldsAnnouncedErrors_WhenTheFieldGoesAway()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = Render<CascadingValue<int>>(parameters => parameters.Add(parent => parent.Value, 0)
            .AddChildContent<TbTextField<int>>(field => field.Add(age => age.Label, "Age")));
        cut.Find("input").Change("twelve");
        Assert.Equal(["Age isn't valid."], Spoken(announcer));

        cut.Render(parameters => parameters.Add(parent => parent.Value, 0)
            .Add(parent => parent.ChildContent, (RenderFragment)(_ => { })));

        Assert.Empty(Spoken(announcer));
    }

    [Fact]
    public void Change_ShouldSpeakTheParseError_WhenTheTextDoesNotParse()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = Render<TbTextField<int>>(parameters => parameters.Add(field => field.Label, "Age"));

        cut.Find("input").Change("twelve");

        Assert.Equal(["Age isn't valid."], Spoken(announcer));
    }

    [Fact]
    public void Change_ShouldSpeakTheFormsMessage_WhenAnEditBreaksARuleOnTheModel()
    {
        var announcer = Render<AnnouncerHost>();
        var signup = new Signup { Email = "ada@example.com" };
        var cut = Render<EditForm>(parameters => parameters.Add(form => form.Model, signup)
            .Add(form => form.ChildContent, (EditContext _) => EmailField(signup)));

        cut.Find("input").Change("");

        Assert.Equal(["Enter an email."], Spoken(announcer));
    }

    private static IEnumerable<string> Password(string? text) => new[]
    {
        text?.Length < 8 ? "At least 8 characters." : null,
        text?.Any(char.IsDigit) == true ? null : "Include a digit.",
    }.OfType<string>();

    private static RenderFragment EmailField(Signup signup) => builder =>
    {
        builder.OpenComponent<DataAnnotationsValidator>(0);
        builder.CloseComponent();
        builder.OpenComponent<TbTextField<string>>(1);
        builder.AddAttribute(2, "Value", signup.Email);
        builder.AddAttribute(3, "ValueChanged",
            EventCallback.Factory.Create<string>(new object(), (string email) => signup.Email = email));
        builder.AddAttribute(4, "ValueExpression",
            (System.Linq.Expressions.Expression<Func<string>>)(() => signup.Email!));
        builder.CloseComponent();
    };

    [Fact]
    public void Submit_ShouldSpeakNoMessage_WhenTheFormChecksEveryField()
    {
        var announcer = Render<AnnouncerHost>();
        var model = new Person();
        var cut = Render<EditForm>(parameters => parameters
            .Add(form => form.Model, model)
            .Add(form => form.ChildContent, (EditContext _) => NameField(model)));

        cut.Find("form").Submit();

        Assert.Equal("Enter a name.", cut.Find("[data-part=error]").TextContent.Trim());
        Assert.Empty(Spoken(announcer));
    }

    [Fact]
    public void Render_ShouldAnnounceTheParentsErrorOnce_WhenTheParentSetsOne()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Label, "Name")
            .Add(field => field.Error, "That name is taken."));

        cut.Render(parameters => parameters.Add(field => field.HelperText, "As on your badge"));

        Assert.Equal(["That name is taken."], Spoken(announcer));
        Assert.Contains(cut.Find("[data-part=error]").Id!, cut.Find("input").GetAttribute("aria-describedby"));
    }

    [Fact]
    public async Task Change_ShouldAnnounceTheParentsErrorOnce_WhenTheParentSetsItWhileTheEditIsAwaited()
    {
        var announcer = Render<AnnouncerHost>();
        var check = new TaskCompletionSource();
        var cut = Render<UsernameForm>(parameters => parameters.Add(form => form.Check, check.Task));

        var change = cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = "robin" });
        await cut.InvokeAsync(check.SetResult);
        await change;

        Assert.Equal("That username is taken.", cut.Find("[data-part=error]").TextContent.Trim());
        Assert.Equal(["That username is taken."], Spoken(announcer));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Render_ShouldLeaveTheFieldValid_WhenGivenABlankError(string error)
    {
        var announcer = Render<AnnouncerHost>();
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Error, error));

        Assert.Null(cut.Find("input").GetAttribute("aria-invalid"));
        Assert.Empty(cut.FindAll("[data-part=error]"));
        Assert.Empty(Spoken(announcer));
    }

    [Fact]
    public void Submit_ShouldBlockTheFormAndShowBothMessages_WhenTheParentSetsAnErrorInAnEditForm()
    {
        var model = new Person { Name = "Ada" };
        var submitted = false;
        var cut = Render<EditForm>(parameters => parameters
            .Add(form => form.Model, model)
            .Add(form => form.OnValidSubmit, () => submitted = true)
            .Add(form => form.ChildContent, (EditContext _) => builder =>
            {
                builder.OpenComponent<TbTextField<string>>(0);
                builder.AddComponentParameter(1, nameof(TbTextField<string>.Value), model.Name);
                builder.AddComponentParameter(2, nameof(TbTextField<string>.ValueExpression),
                    (Expression<Func<string>>)(() => model.Name!));
                builder.AddComponentParameter(3, nameof(TbTextField<string>.Error), "That name is taken.");
                builder.AddComponentParameter(4, nameof(TbTextField<string>.Validation),
                    (Func<string?, IEnumerable<string>>)(_ => ["Too plain."]));
                builder.CloseComponent();
            }));

        cut.Find("form").Submit();

        Assert.False(submitted);
        Assert.Equal(["That name is taken.", "Too plain."],
            cut.FindAll("[data-part=error] > div").Select(error => error.TextContent));
    }

    [Fact]
    public void Render_ShouldUpdateTheFormsSummary_WhenTheParentSetsAnErrorLater()
    {
        var model = new Person();
        string? error = null;
        var cut = Render<EditForm>(parameters => parameters
            .Add(form => form.Model, model)
            .Add(form => form.ChildContent, (EditContext _) => builder =>
            {
                builder.OpenComponent<ValidationSummary>(0);
                builder.CloseComponent();
                builder.OpenComponent<TbTextField<string>>(1);
                builder.AddComponentParameter(2, nameof(TbTextField<string>.ValueExpression),
                    (Expression<Func<string>>)(() => model.Name!));
                builder.AddComponentParameter(3, nameof(TbTextField<string>.Error), error);
                builder.CloseComponent();
            }));

        error = "That name is taken.";
        cut.Render();

        Assert.Equal("That name is taken.", cut.Find(".validation-message").TextContent);
    }

    [Fact]
    public void Submit_ShouldBlockTheFormAndShowTheMessage_WhenValidationFailsInAnEditForm()
    {
        var model = new Person();
        var submitted = false;
        var cut = Render<EditForm>(parameters => parameters
            .Add(form => form.Model, model)
            .Add(form => form.OnValidSubmit, () => submitted = true)
            .Add(form => form.ChildContent, (EditContext _) => NameField(model)));

        cut.Find("form").Submit();

        Assert.False(submitted);
        Assert.Equal("Enter a name.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void Submit_ShouldShowOnlyTheRequiredMessage_WhenARequiredFieldsOwnValidationAlsoRejectsItsEmptiness()
    {
        var model = new Person();
        var cut = Render<EditForm>(parameters => parameters
            .Add(form => form.Model, model)
            .Add(form => form.ChildContent, (EditContext _) => NameField(model, required: true)));

        cut.Find("form").Submit();

        Assert.Equal(["Name is required."], cut.FindAll("[data-part=error] > div").Select(error => error.TextContent));
    }

    [Fact]
    public void Render_ShouldNameTheInputAfterTheBoundProperty_WhenValueExpressionIsSet()
    {
        var model = new Person();
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.ValueExpression, () => model.Name!));

        Assert.Equal("model.Name", cut.Find("input").GetAttribute("name"));
    }

    [Fact]
    public void Submit_ShouldGoThrough_WhenTheFieldIsFixedAfterAFailedSubmit()
    {
        var model = new Person();
        var submitted = false;
        var cut = Render<EditForm>(parameters => parameters
            .Add(form => form.Model, model)
            .Add(form => form.OnValidSubmit, () => submitted = true)
            .Add(form => form.ChildContent, (EditContext _) => NameField(model)));
        cut.Find("form").Submit();

        cut.Find("input").Change("Ada");
        cut.Find("form").Submit();

        Assert.True(submitted);
        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public void Submit_ShouldGoThrough_WhenAFieldWithAMessageIsReboundToAValidProperty()
    {
        var first = new Person();
        var second = new Person { Name = "Grace" };
        var submitted = false;
        var cut = Render<EditForm>(parameters => parameters
            .Add(form => form.Model, first)
            .Add(form => form.OnValidSubmit, () => submitted = true)
            .Add(form => form.ChildContent, (EditContext _) => NameField(first)));
        cut.Find("form").Submit();

        cut.Render(parameters => parameters.Add(form => form.ChildContent, (EditContext _) => NameField(second)));
        cut.Find("form").Submit();

        Assert.True(submitted);
    }

    [Fact]
    public async Task Change_ShouldNotValidate_WhenTheFieldIsDisposedWhileValueChangedRuns()
    {
        var handler = new TaskCompletionSource();
        var validations = 0;
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.ValueChanged, (string _) => handler.Task)
            .Add(field => field.Validation, (string? _) =>
            {
                validations++;
                return [];
            }));
        var change = cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = "Ada" });

        await DisposeComponentsAsync();
        handler.SetResult();
        await change;

        Assert.Equal(0, validations);
    }

    [Fact]
    public void Render_ShouldKeepTheConsumerNameAndDescription_WhenTheFieldHasNoneOfItsOwn()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.InputAttributes, new Dictionary<string, object>
            {
                ["name"] = "lines[0].qty", ["aria-describedby"] = "qty-hint",
            })
            .Add(field => field.HelperText, "Whole units"));

        var input = cut.Find("input");
        Assert.Equal("lines[0].qty", input.GetAttribute("name"));
        Assert.Equal($"{cut.Find("[data-part=helper]").Id} qty-hint", input.GetAttribute("aria-describedby"));
    }

    [Fact]
    public void Change_ShouldUpdateTheElement_WhenBoundToAListElement()
    {
        var tags = new List<string> { "red" };
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Value, tags[0])
            .Add(field => field.ValueChanged, (string tag) => tags[0] = tag)
            .Add(field => field.ValueExpression, () => tags[0]));

        cut.Find("input").Change("blue");

        Assert.Equal("blue", tags[0]);
        Assert.Null(cut.Find("input").GetAttribute("name"));
    }

    [Fact]
    public void Render_ShouldNotThrow_WhenGivenAnExpressionFormsCannotIdentifyOutsideAForm()
    {
        var tags = new List<string> { "red" };
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Value, tags.First())
            .Add(field => field.ValueExpression, () => tags.First()));

        Assert.Equal("red", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Render_ShouldRenderATextArea_WhenLinesIsMoreThanOne()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Lines, 4));

        Assert.Equal("4", cut.Find("textarea").GetAttribute("rows"));
    }

    [Fact]
    public void Render_ShouldPutInputAttributesOnTheInput_WhenTheyArePassed()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.InputAttributes, new Dictionary<string, object> { ["autocomplete"] = "name" }));

        Assert.Equal("name", cut.Find("input").GetAttribute("autocomplete"));
    }

    // What the page's polite live region holds, which a screen reader reads out.
    private static IEnumerable<string> Spoken(IRenderedComponent<AnnouncerHost> announcer) =>
        announcer.FindAll("[role=status] p").Select(spoken => spoken.TextContent);

    // A name field bound to the model, whose own validation requires a name.
    private static RenderFragment NameField(Person model, bool required = false) => builder =>
    {
        builder.OpenComponent<TbTextField<string>>(0);
        builder.AddAttribute(1, "Value", model.Name);
        builder.AddAttribute(2, "ValueChanged",
            EventCallback.Factory.Create<string>(new object(), (string name) => model.Name = name));
        builder.AddAttribute(3, "ValueExpression", (System.Linq.Expressions.Expression<Func<string>>)(() => model.Name!));
        builder.AddAttribute(4, "Validation", (Func<string?, IEnumerable<string>>)(name =>
            string.IsNullOrEmpty(name) ? ["Enter a name."] : []));
        builder.AddAttribute(5, "Required", required);
        builder.AddAttribute(6, "Label", "Name");
        builder.CloseComponent();
    };

    private static RenderFragment AgeField(Ages ages) => builder =>
    {
        builder.OpenComponent<TbTextField<int>>(0);
        builder.AddAttribute(1, "Label", "Age");
        builder.AddAttribute(2, "Value", ages.Age);
        builder.AddAttribute(3, "ValueExpression", (System.Linq.Expressions.Expression<Func<int>>)(() => ages.Age));
        builder.CloseComponent();
    };

    private sealed class Ages
    {
        public int Age { get; set; }
    }

    // Its rule lives on the model, so only the form's validator knows the message.
    private sealed class Signup
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Enter an email.")]
        public string? Email { get; set; }
    }

    private sealed class Person
    {
        public string? Name { get; set; }
    }

    // A parent that checks the name with a server as it changes, and shows the answer as the field's error.
    private sealed class UsernameForm : ComponentBase
    {
        private string? _name;
        private string? _error;

        [Parameter]
        public Task Check { get; set; } = Task.CompletedTask;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TbTextField<string>>(0);
            builder.AddComponentParameter(1, nameof(TbTextField<string>.Value), _name);
            builder.AddComponentParameter(2, nameof(TbTextField<string>.ValueChanged),
                EventCallback.Factory.Create<string>(this, CheckAsync));
            builder.AddComponentParameter(3, nameof(TbTextField<string>.Error), _error);
            builder.CloseComponent();
        }

        private async Task CheckAsync(string name)
        {
            _name = name;
            await Check;
            _error = name == "robin" ? "That username is taken." : null;
        }
    }
}
