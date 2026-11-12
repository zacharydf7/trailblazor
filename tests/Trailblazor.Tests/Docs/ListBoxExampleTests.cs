using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs;

public class ListBoxExampleTests : DocsTest
{
    [Fact]
    public async Task ListBoxExample_ShouldShowTheBoundValue_WhenAnOptionIsChosen()
    {
        var cut = Render<ListBoxExample>();
        Assert.Contains("Trail: Creek path", cut.Markup);

        await cut.Tb().ListBox("Trail").ChooseAsync("Old mill");

        Assert.Contains("Trail: Old mill", cut.Markup);
    }

    [Fact]
    public async Task MultipleExample_ShouldListTheChosenValues_WhenAnotherIsChosen()
    {
        var cut = Render<ListBoxMultipleExample>();
        Assert.Contains("Packing: Tent", cut.Markup);

        await cut.Tb().ListBox("Gear to pack").ChooseAsync("Stove");

        Assert.Contains("Packing: Tent, Stove", cut.Markup);
    }

    [Fact]
    public void GroupsExample_ShouldNameEachGroup_WhenRendered()
    {
        var cut = Render<ListBoxGroupsExample>();

        Assert.Equal(["North ridge", "River valley"], Texts(cut.FindAll("[data-part=group-label]")));
    }

    [Fact]
    public void ContentExample_ShouldNameEachOptionByItsWordsOrItsText_WhenOptionsHaveContent()
    {
        var cut = Render<ListBoxContentExample>();

        Assert.Equal(["Lake loop Easy", "Summit trail Hard", "Any trail"], cut.Tb().ListBox("Route").Options);
    }

    [Fact]
    public void EmptyExample_ShouldSayWhyTheListIsEmpty_WhenRendered()
    {
        var cut = Render<ListBoxEmptyExample>();

        Assert.Empty(cut.Tb().ListBox("Saved trips").Options);
        Assert.Equal("You haven't saved a trip yet", cut.Find("[data-part=status]").TextContent.Trim());
    }

    [Fact]
    public void ComparerExample_ShouldChooseTheSavedTrail_WhenItsValueIsAnotherInstance()
    {
        var cut = Render<ListBoxComparerExample>();

        Assert.Equal(["Lake"], cut.Tb().ListBox("Saved route").Chosen);
    }

    [Fact]
    public void RequiredExample_ShouldShowItsMessage_WhenItAppears()
    {
        var cut = Render<ListBoxRequiredExample>();

        cut.WaitForAssertion(() => Assert.Equal("Choose a trip leader.",
            cut.Find("[data-part=error]").TextContent.Trim()));
    }

    [Fact]
    public async Task ValidationExample_ShouldShowItsMessageUntilAnotherOptionIsChosen_WhenItAppears()
    {
        var cut = Render<ListBoxValidationExample>();
        cut.WaitForAssertion(() => Assert.Equal("The huts close for the winter.",
            cut.Find("[data-part=error]").TextContent.Trim()));

        await cut.Tb().ListBox("Season").ChooseAsync("Summer");

        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public async Task ErrorExample_ShouldShowTheErrorUntilTheDayChanges_WhenTheCheckFindsAProblem()
    {
        var cut = Render<ListBoxErrorExample>();
        Assert.Empty(cut.FindAll("[data-part=error]"));

        await Button(cut, "Check the day").ClickAsync(new());
        Assert.Equal("Saturday is fully booked.", cut.Find("[data-part=error]").TextContent.Trim());

        await cut.Tb().ListBox("Start day").ChooseAsync("Friday");

        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public void StatesExample_ShouldMarkOneReadOnlyAndTakeTheOtherOutOfTheTabOrder_WhenRendered()
    {
        var cut = Render<ListBoxStatesExample>();
        var lists = cut.FindAll("[role=listbox]");

        Assert.Equal("true", lists[0].GetAttribute("aria-readonly"));
        Assert.Equal("true", lists[1].GetAttribute("aria-disabled"));
        Assert.Equal("-1", lists[1].GetAttribute("tabindex"));
    }

    [Fact]
    public void ComponentPage_ShouldListTheKeysAndKeepFeaturesOutOfAccessibility_WhenShowingTheListBox()
    {
        var cut = ComponentPage("listbox");

        Assert.Equal(["Down", "Down", "Up", "Home", "End", "Page Up", "Page Down", "A letter", "Space", "Down", "Up",
            "Home", "End", "Page Up", "Page Down", "Ctrl", "A", "Cmd", "A", "A letter", "Space", "Shift", "Down", "Shift",
            "Up"],
            cut.FindAll("kbd").Select(key => key.TextContent));
        Assert.DoesNotContain("ReadOnly", cut.Find("ul[aria-label=Accessibility]").TextContent);
    }
}
