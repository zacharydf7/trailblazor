using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs;

public class SelectExampleTests : DocsTest
{
    [Fact]
    public async Task SelectExample_ShouldShowTheBoundValue_WhenAnOptionIsChosen()
    {
        var cut = Render<SelectExample>();
        Assert.Contains("Shipping value: null", cut.Markup);

        await cut.Tb().Select("Shipping").ChooseAsync("Express (1 day)");

        Assert.Contains("Shipping value: Express", cut.Markup);
    }

    [Fact]
    public async Task ClearExample_ShouldShowNull_WhenTheChoiceIsCleared()
    {
        var cut = Render<SelectClearExample>();
        Assert.Contains("Campsite value: Lake", cut.Markup);

        await cut.Find("[data-part=clear]").ClickAsync(new());

        Assert.Contains("Campsite value: null", cut.Markup);
    }

    [Fact]
    public async Task MultipleExample_ShouldListTheChosenValues_WhenAnotherIsChosen()
    {
        var cut = Render<SelectMultipleExample>();
        Assert.Contains("Notifications value: Orders", cut.Markup);

        await cut.Tb().Select("Notify me about").ChooseAsync("Returns");

        Assert.Contains("Notifications value: Orders, Returns", cut.Markup);
    }

    [Fact]
    public void ContentExample_ShouldShowTheContentAndNameItByItsWords_WhenAnOptionIsChosen()
    {
        var cut = Render<SelectContentExample>();

        Assert.Contains("Easy", cut.Find("[data-part=value]").TextContent);
        Assert.Equal("Lake loop Easy", cut.Tb().Select("Route").Text);
    }

    [Fact]
    public void ContentExample_ShouldNameEachOptionByItsWordsOrItsText_WhenOptionsHaveContent()
    {
        var cut = Render<SelectContentExample>();

        Assert.Equal(["Lake loop Easy", "Summit trail Hard", "Any trail"], cut.Tb().Select("Route").Options);
    }

    [Fact]
    public void LateExample_ShouldShowTheChosenName_WhenItsOptionsHaveNotLoaded()
    {
        var cut = Render<SelectLateExample>();

        Assert.Equal("Grace", cut.Tb().Select("Guide").Text);
    }

    [Fact]
    public async Task LateExample_ShouldShowThatItIsLoadingThenItsOptions_WhenTheListOpens()
    {
        var cut = Render<SelectLateExample>();

        var opening = cut.InvokeAsync(() => cut.FindComponent<TbSelect<int>>().Instance.OpenChanged.InvokeAsync(true));

        cut.WaitForAssertion(() => Assert.Contains("Loading guides...", cut.Tb().Select("Guide").Options));
        await opening;
        cut.WaitForAssertion(() => Assert.Contains("Katherine", cut.Tb().Select("Guide").Options));
        Assert.DoesNotContain("Loading guides...", cut.Tb().Select("Guide").Options);
    }

    [Fact]
    public async Task LateExample_ShouldFailTheNextLoad_WhenTheBoxIsCheckedAfterTheOptionsLoaded()
    {
        var cut = Render<SelectLateExample>();
        await cut.InvokeAsync(() => cut.FindComponent<TbSelect<int>>().Instance.OpenChanged.InvokeAsync(true));
        await cut.Find("input[type=checkbox]").ChangeAsync(new() { Value = true });

        await cut.InvokeAsync(() => cut.FindComponent<TbSelect<int>>().Instance.OpenChanged.InvokeAsync(true));

        cut.WaitForAssertion(() => Assert.Equal("The guides didn't load. Open the list to try again.",
            cut.Find("[data-part=error]").TextContent.Trim()));
        Assert.DoesNotContain("Katherine", cut.Tb().Select("Guide").Options);
    }

    [Fact]
    public async Task LateExample_ShouldSayItDidNotLoad_WhenTheNextLoadIsSetToFail()
    {
        var cut = Render<SelectLateExample>();
        await cut.Find("input[type=checkbox]").ChangeAsync(new() { Value = true });

        await cut.InvokeAsync(() => cut.FindComponent<TbSelect<int>>().Instance.OpenChanged.InvokeAsync(true));

        cut.WaitForAssertion(() => Assert.Equal("The guides didn't load. Open the list to try again.",
            cut.Find("[data-part=error]").TextContent.Trim()), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void EmptyExample_ShouldSayWhyTheListIsEmpty_WhenRendered()
    {
        var cut = Render<SelectEmptyExample>();

        Assert.Empty(cut.Tb().Select("Saved routes").Options);
        Assert.Equal("You haven't saved a route yet", cut.Find("[data-part=status]").TextContent.Trim());
    }

    [Fact]
    public void ComparerExample_ShouldShowTheSavedTrail_WhenItsValueIsAnotherInstance()
    {
        var cut = Render<SelectComparerExample>();

        Assert.Equal("Lake", cut.Tb().Select("Saved trail").Text);
    }

    [Fact]
    public void RequiredExample_ShouldShowItsMessage_WhenItAppears()
    {
        var cut = Render<SelectRequiredExample>();

        cut.WaitForAssertion(() => Assert.Equal("Choose a permit type.",
            cut.Find("[data-part=error]").TextContent.Trim()));
    }

    [Fact]
    public async Task ValidationExample_ShouldShowItsMessageUntilAnotherOptionIsChosen_WhenItAppears()
    {
        var cut = Render<SelectValidationExample>();
        cut.WaitForAssertion(() => Assert.Equal("The huts close for the winter.",
            cut.Find("[data-part=error]").TextContent.Trim()));

        await cut.Tb().Select("Season").ChooseAsync("Summer");

        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public async Task ErrorExample_ShouldShowTheErrorUntilTheDayChanges_WhenTheCheckFindsAProblem()
    {
        var cut = Render<SelectErrorExample>();
        Assert.Empty(cut.FindAll("[data-part=error]"));

        await Button(cut, "Check the day").ClickAsync(new());
        Assert.Equal("Saturday is fully booked.", cut.Find("[data-part=error]").TextContent.Trim());

        await cut.Tb().Select("Start day").ChooseAsync("Friday");

        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public void StatesExample_ShouldMarkOneReadOnlyAndDisableTheOther_WhenRendered()
    {
        var cut = Render<SelectStatesExample>();
        var fields = cut.FindAll("[role=combobox]");

        Assert.Equal("true", fields[0].GetAttribute("aria-readonly"));
        Assert.Equal("true", fields[1].GetAttribute("aria-disabled"));
    }

    [Fact]
    public void LabelsExample_ShouldUseItsOwnText_WhenRendered()
    {
        var cut = Render<SelectLabelsExample>();

        Assert.Contains("Every hut", cut.Tb().Select("Huts").Options);
        Assert.Contains("Clear the huts", cut.Markup);
    }

    [Fact]
    public void ComponentPage_ShouldKeepFeaturesOutOfAccessibility_WhenShowingTheSelect()
    {
        var accessibility = ComponentPage("select").Find("ul[aria-label=Accessibility]").TextContent;

        Assert.DoesNotContain("ReadOnly", accessibility);
        Assert.DoesNotContain("TbOptionGroup", accessibility);
    }
}
