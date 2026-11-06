using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class TextFieldExampleTests : DocsTest
{
    [Fact]
    public void RequiredExample_ShouldShowBothMessages_WhenItAppears()
    {
        var cut = Render<TextFieldRequiredExample>();

        cut.WaitForAssertion(() => Assert.Equal(["Group leader is required.", "Enter where the group meets."],
            Texts(cut.FindAll("[data-part=error]"))));
    }

    [Fact]
    public async Task ValidationExample_ShouldCheckThePermit_WhenTheNumberChanges()
    {
        var cut = Render<TextFieldValidationExample>();
        var permit = () => Input(cut.Find(".tb-stack"), "Permit number");

        await permit().ChangeAsync(new() { Value = "12" });
        Assert.Contains("Enter six digits.", cut.Markup);

        await permit().ChangeAsync(new() { Value = "654321" });
        Assert.DoesNotContain("Enter six digits.", cut.Markup);
    }

    [Fact]
    public async Task ValidationExample_ShouldShowATakenHandlesErrorUntilItChanges_WhenChecked()
    {
        var cut = Render<TextFieldValidationExample>();
        await Input(cut.Find(".tb-stack"), "Handle").InputAsync(new() { Value = "robin" });

        await Button(cut, "Check the handle").ClickAsync(new());
        Assert.Contains("That handle is taken.", cut.Markup);

        await Input(cut.Find(".tb-stack"), "Handle").InputAsync(new() { Value = "robyn" });
        Assert.DoesNotContain("That handle is taken.", cut.Markup);
    }

    [Fact]
    public async Task ValidationExample_ShouldMarkBothCodesAndMessageOne_WhenTheyDiffer()
    {
        var cut = Render<TextFieldValidationExample>();

        await Input(cut.Find(".tb-stack"), "Gate code").ChangeAsync(new() { Value = "1234" });
        await Input(cut.Find(".tb-stack"), "Gate code again").ChangeAsync(new() { Value = "4321" });

        Assert.Equal("true", Input(cut.Find(".tb-stack"), "Gate code").GetAttribute("aria-invalid"));
        Assert.Equal("true", Input(cut.Find(".tb-stack"), "Gate code again").GetAttribute("aria-invalid"));
        Assert.Contains("Enter the same gate code in both fields.", cut.Markup);
    }

    [Fact]
    public async Task ValidationExample_ShouldNotCompareTheCodes_WhenOneIsEmpty()
    {
        var cut = Render<TextFieldValidationExample>();

        await Input(cut.Find(".tb-stack"), "Gate code").ChangeAsync(new() { Value = "1234" });
        Assert.DoesNotContain("Enter the same gate code", cut.Markup);

        await Input(cut.Find(".tb-stack"), "Gate code").ChangeAsync(new() { Value = "" });
        Assert.DoesNotContain("Enter the same gate code", cut.Markup);
    }

    [Fact]
    public async Task TypesExample_ShouldPutAttributesOnTheInputAndExplainBadText_WhenRendered()
    {
        var cut = Render<TextFieldTypesExample>();
        Assert.Equal("email", Input(cut.Find(".tb-stack"), "Email").GetAttribute("autocomplete"));

        await Input(cut.Find(".tb-stack"), "Age").ChangeAsync(new() { Value = "abc" });

        Assert.Contains("Age isn't valid.", cut.Markup);
        Assert.Contains("Age as an int: 30", cut.Markup);
    }

    [Fact]
    public async Task SearchExample_ShouldSetTheValueToNull_WhenCleared()
    {
        var cut = Render<TextFieldSearchExample>();
        Assert.Contains("Region: North ridge", cut.Markup);

        await Button(cut, "Clear the region").ClickAsync(new());

        Assert.Contains("Region: null", cut.Markup);
    }

    [Fact]
    public async Task MaskExample_ShouldBindTheWholeText_WhenIncludeMaskLiteralsIsSet()
    {
        var cut = Render<TextFieldMaskExample>();

        await Input(cut.Find(".tb-stack"), "Contact number").ChangeAsync(new() { Value = "5551234567" });

        Assert.Contains("Whole text: (555) 123-4567", cut.Markup);
    }

    [Fact]
    public void StatesExample_ShouldMarkEachField_WhenRendered()
    {
        var cut = Render<TextFieldStatesExample>();

        Assert.True(Input(cut.Find(".tb-stack"), "Account").HasAttribute("readonly"));
        Assert.True(Input(cut.Find(".tb-stack"), "Referral").HasAttribute("disabled"));
    }

    [Fact]
    public void AriaLabelExample_ShouldFilterTheListBelowIt_WhenTyped()
    {
        var cut = Render<TextFieldAriaLabelExample>();

        cut.Find("input").Input("fern");

        Assert.Equal(["Fern Gully"], Texts(cut.FindAll("[aria-label=Trails] li")));
    }

    [Fact]
    public void ComponentPage_ShouldListTheKeysAndKeepFeaturesOutOfAccessibility_WhenShowingTheTextField()
    {
        var cut = ComponentPage("text-field");

        Assert.Contains(cut.FindAll("kbd"), key => key.TextContent == "Enter");
        Assert.DoesNotContain("UpdateDelay", cut.Find("ul[aria-label=Accessibility]").TextContent);
        Assert.DoesNotContain("Mask", cut.Find("ul[aria-label=Accessibility]").TextContent);
    }
}
