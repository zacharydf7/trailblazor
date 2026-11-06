using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class FormExampleTests : DocsTest
{
    // A required field's label also holds its asterisk, so this matches on the label's text rather than all of it.
    private static IElement Field<T>(IRenderedComponent<T> cut, string label) where T : IComponent =>
        cut.Find($"#{cut.FindAll("label").Single(found => found.TextContent.Contains(label)).GetAttribute("for")}");

    [Fact]
    public async Task FormExample_ShouldLeaveSignUpOnAndCountWhatToFix_WhenSubmittedEmpty()
    {
        var cut = Render<FormExample>();
        Assert.False(Button(cut, "Sign up").HasAttribute("disabled"));

        await cut.Find("form").SubmitAsync();

        Assert.Equal("3 to fix.", cut.Find("output").TextContent);
        Assert.Equal(3, cut.FindAll("[data-part=error]").Count);
    }

    [Fact]
    public async Task FormExample_ShouldSayItIsReadyAndThenSignUp_WhenEveryFieldPasses()
    {
        var cut = Render<FormExample>();

        await Field(cut, "Leader's name").ChangeAsync(new() { Value = "Robin" });
        await cut.Tb().NumberField("Party size").EnterAsync("4");
        await cut.Find("input[type=checkbox]").ChangeAsync(new() { Value = true });
        cut.WaitForAssertion(() => Assert.Equal("Ready to sign up.", cut.Find("output").TextContent));
        await cut.Find("form").SubmitAsync();

        Assert.Equal("Signed up Robin's party of 4.", cut.Find("output").TextContent);
        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public async Task FormExample_ShouldReplaceItsSuccessWithWhatToFix_WhenALaterSubmitFails()
    {
        var cut = Render<FormExample>();
        await Field(cut, "Leader's name").ChangeAsync(new() { Value = "Robin" });
        await cut.Tb().NumberField("Party size").EnterAsync("4");
        await cut.Find("input[type=checkbox]").ChangeAsync(new() { Value = true });
        await cut.Find("form").SubmitAsync();

        await Field(cut, "Leader's name").ChangeAsync(new() { Value = "" });
        await cut.Find("form").SubmitAsync();

        Assert.Equal("1 to fix.", cut.Find("output").TextContent);
        await Field(cut, "Leader's name").ChangeAsync(new() { Value = "Sam" });
        Assert.Equal("Ready to sign up.", cut.Find("output").TextContent);
    }

    [Theory]
    [InlineData(typeof(FormValidationExample), "Trail code", "PC12")]
    [InlineData(typeof(FormServerExample), "Campsite", "Ridge 14")]
    [InlineData(typeof(FormStaticExample), "Trail", "Ridge loop")]
    public async Task Example_ShouldClearItsSuccess_WhenALaterSubmitFails(Type example, string label, string value)
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent(0, example);
            builder.CloseComponent();
        });
        await Field(cut, label).ChangeAsync(new() { Value = value });
        await cut.Find("form").SubmitAsync();
        Assert.NotEmpty(cut.Find("output").TextContent);

        await Field(cut, label).ChangeAsync(new() { Value = "" });
        await cut.Find("form").SubmitAsync();

        Assert.Empty(cut.Find("output").TextContent);
    }

    [Fact]
    public async Task FormModelExample_ShouldShowOnlyTheRequiredMessage_WhenAFilledFieldIsCleared()
    {
        var cut = Render<FormModelExample>();
        await Field(cut, "Contact's call sign").ChangeAsync(new() { Value = "KD8TRL" });

        await Field(cut, "Contact's call sign").ChangeAsync(new() { Value = "" });

        Assert.Equal("Enter the contact's call sign.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public async Task FormModelExample_ShouldClearItsSuccess_WhenALaterSubmitFails()
    {
        var cut = Render<FormModelExample>();
        await Field(cut, "Contact's call sign").ChangeAsync(new() { Value = "KD8TRL" });
        await cut.Tb().NumberField("Contact's age").EnterAsync("30");
        await cut.Find("form").SubmitAsync();
        Assert.NotEmpty(cut.Find("output").TextContent);

        await Field(cut, "Contact's call sign").ChangeAsync(new() { Value = "TOO-LONG" });
        await cut.Find("form").SubmitAsync();

        Assert.Empty(cut.Find("output").TextContent);
    }

    [Fact]
    public async Task FormValidationExample_ShouldShowTheCodesMessage_WhenTheCodeDoesntFitThePattern()
    {
        var cut = Render<FormValidationExample>();

        await Field(cut, "Trail code").ChangeAsync(new() { Value = "12" });

        Assert.Equal("Enter two letters and a number, such as PC12.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public async Task FormValidationExample_ShouldMarkBothFieldsAndMessageOne_WhenTheLimitsCross()
    {
        var cut = Render<FormValidationExample>();

        await cut.Tb().NumberField("Largest group").EnterAsync("1");

        Assert.Equal(2, cut.FindAll(".tb-field--invalid").Count);
        Assert.Equal("Enter a largest group at least as big as the smallest.",
            cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public async Task FormModelExample_ShouldShowTheModelsMessages_WhenItsAnnotationsFail()
    {
        var cut = Render<FormModelExample>();

        await Field(cut, "Contact's call sign").ChangeAsync(new() { Value = "TOO-LONG" });
        await cut.Tb().NumberField("Contact's age").EnterAsync("12");

        Assert.Equal(["Enter a call sign of 6 characters or fewer.", "Enter the age of a contact who is 18 or over."],
            Texts(cut.FindAll("[data-part=error]")));
    }

    [Fact]
    public async Task FormEditContextExample_ShouldTrackUnsavedChangesUntilSaved_WhenTheFieldChanges()
    {
        var cut = Render<FormEditContextExample>();
        Assert.Equal("Saved: North trailhead.", cut.Find("output").TextContent);

        await Field(cut, "Meeting point").ChangeAsync(new() { Value = "South trailhead" });
        Assert.Equal("You have unsaved changes.", cut.Find("output").TextContent);

        await cut.Find("form").SubmitAsync();
        Assert.Equal("Saved: South trailhead.", cut.Find("output").TextContent);
    }

    [Fact]
    public async Task FormEditContextExample_ShouldRestoreTheValueAndHideItsMessage_WhenUndone()
    {
        var cut = Render<FormEditContextExample>();
        await Field(cut, "Meeting point").ChangeAsync(new() { Value = "" });
        Assert.Single(cut.FindAll("[data-part=error]"));

        await Button(cut, "Undo changes").ClickAsync(new());

        Assert.Empty(cut.FindAll("[data-part=error]"));
        Assert.Equal("North trailhead", Field(cut, "Meeting point").GetAttribute("value"));
        Assert.Equal("Saved: North trailhead.", cut.Find("output").TextContent);
    }

    [Fact]
    public async Task FormServerExample_ShouldShowTheServersErrorAndClearItOnEdit_WhenTheCampsiteIsTaken()
    {
        var cut = Render<FormServerExample>();
        await Field(cut, "Campsite").ChangeAsync(new() { Value = "Ridge 12" });

        await cut.Find("form").SubmitAsync();

        Assert.Equal("Ridge 12 is already taken. Choose another campsite.",
            cut.Find("[data-part=error]").TextContent.Trim());
        await Field(cut, "Campsite").ChangeAsync(new() { Value = "Ridge 14" });
        Assert.Empty(cut.FindAll("[data-part=error]"));
        await cut.Find("form").SubmitAsync();
        Assert.Equal("Reserved Ridge 14.", cut.Find("output").TextContent);
    }

    [Fact]
    public async Task FormServerExample_ShouldMarkTheButtonBusy_WhenTheServerHasNotAnswered()
    {
        var cut = Render<FormServerExample>();
        await Field(cut, "Campsite").ChangeAsync(new() { Value = "Ridge 14" });

        var submit = cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() => Assert.Equal("true", Button(cut, "Reserve").GetAttribute("aria-busy")));
        await submit;
        Assert.Null(Button(cut, "Reserve").GetAttribute("aria-busy"));
    }

    [Fact]
    public async Task FormErrorSummaryExample_ShouldListEachFieldsMessageAndFocusTheSummary_WhenSubmittedEmpty()
    {
        var cut = Render<FormErrorSummaryExample>();
        Assert.Empty(cut.FindAll("[role=group]"));

        await cut.Find("form").SubmitAsync();

        Assert.Equal("There are 2 problems to fix.", cut.Find("[role=group]").GetAttribute("aria-label"));
        Assert.Equal(["Enter the permit holder's name.", "Enter how many nights you'll stay."],
            Texts(cut.FindAll("[role=group] li")));
        JSInterop.VerifyFocusAsyncInvoke();
    }

    [Fact]
    public async Task FormErrorSummaryExample_ShouldShrinkAndThenGoAway_WhenFieldsAreFixed()
    {
        var cut = Render<FormErrorSummaryExample>();
        await cut.Find("form").SubmitAsync();

        await Field(cut, "Permit holder").ChangeAsync(new() { Value = "Robin" });
        Assert.Equal("There is 1 problem to fix.", cut.Find("[role=group]").GetAttribute("aria-label"));
        Assert.Equal(["Enter how many nights you'll stay."], Texts(cut.FindAll("[role=group] li")));

        await cut.Tb().NumberField("Nights").EnterAsync("2");
        Assert.Empty(cut.FindAll("[role=group]"));
    }

    [Theory]
    [InlineData("text-field")]
    [InlineData("number-field")]
    [InlineData("checkbox")]
    [InlineData("select")]
    [InlineData("file-upload")]
    public void ComponentPage_ShouldLinkTheFormsGuide_WhenShowingAField(string name)
    {
        var cut = ComponentPage(name);

        Assert.Contains(cut.FindAll("ul[aria-label='Notes and limitations'] a"),
            link => link.GetAttribute("href") == "guides/forms");
    }

    [Fact]
    public void GettingStarted_ShouldLinkTheFormsGuide_WhenOpened()
    {
        var cut = PageAt("guides/getting-started");

        Assert.Contains(cut.FindAll("a"), link => link.GetAttribute("href") == "guides/forms");
    }
}
