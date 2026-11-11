using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class AutocompleteExampleTests : DocsTest
{
    [Fact]
    public async Task ClearExample_ShouldShowNull_WhenTheFieldIsCleared()
    {
        var cut = Render<AutocompleteClearExample>();
        Assert.Contains("Lake value: Mirror", cut.Markup);

        await cut.Find("[data-part=clear]").ClickAsync(new());

        Assert.Contains("Lake value: null", cut.Markup);
    }

    [Fact]
    public void FilterExample_ShouldNarrowTheList_WhenTheUserPausesTyping()
    {
        var cut = Render<AutocompleteFilterExample>();

        cut.Find("[role=combobox]").Input("pine");
        Time.Advance(TimeSpan.FromMilliseconds(200));

        cut.WaitForAssertion(() => Assert.Contains("2 of 7 parks match", cut.Markup));
    }

    [Fact]
    public void SearchStatesExample_ShouldSayItFailedAndCountIt_WhenTheNextSearchIsSetToFail()
    {
        var cut = Render<AutocompleteStatesOfSearchExample>();
        cut.Find("input[type=checkbox]").Change(true);

        cut.Find("[role=combobox]").Input("alder");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Failed searches: 1", cut.Markup);
            Assert.Equal("Couldn't load suggestions.", cut.Find("[data-part=status]").TextContent.Trim());
        }, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SearchStatesExample_ShouldSayNothingMatched_WhenTheSearchFindsNothing()
    {
        var cut = Render<AutocompleteStatesOfSearchExample>();

        cut.Find("[role=combobox]").Input("zzz");

        cut.WaitForAssertion(() => Assert.Equal("No matches", cut.Find("[data-part=status]").TextContent.Trim()),
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void LimitsExample_ShouldShowAtMostThreeSuggestions_WhenEnoughIsTyped()
    {
        var cut = Render<AutocompleteLimitsExample>();

        cut.Find("[role=combobox]").Input("as");

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[role=option]").Count));
    }

    [Fact]
    public void LabelsExample_ShouldUseItsOwnWords_WhenNothingMatches()
    {
        var cut = Render<AutocompleteLabelsExample>();
        Assert.Equal("Clear the shelter", cut.Find("[data-part=clear]").GetAttribute("aria-label"));

        cut.Find("[role=combobox]").Input("zzz");

        cut.WaitForAssertion(() => Assert.Equal("No shelter has that name",
            cut.Find("[data-part=status]").TextContent.Trim()));
    }

    [Fact]
    public void RequiredExample_ShouldShowItsMessage_WhenItAppears()
    {
        var cut = Render<AutocompleteRequiredExample>();

        cut.WaitForAssertion(() => Assert.Equal("Choose a guide.", cut.Find("[data-part=error]").TextContent.Trim()));
    }

    [Fact]
    public void ValidationExample_ShouldShowItsMessage_WhenItAppears()
    {
        var cut = Render<AutocompleteValidationExample>();

        cut.WaitForAssertion(() => Assert.Equal("The Stone is closed to paddling.",
            cut.Find("[data-part=error]").TextContent.Trim()));
    }

    [Fact]
    public async Task ErrorExample_ShouldShowTheErrorUntilTheHutChanges_WhenTheCheckFindsAProblem()
    {
        var cut = Render<AutocompleteErrorExample>();
        Assert.Empty(cut.FindAll("[data-part=error]"));

        await Button(cut, "Check the hut").ClickAsync(new());
        Assert.Equal("Birch hut is fully booked.", cut.Find("[data-part=error]").TextContent.Trim());

        cut.Find("[role=combobox]").Input("Alder");
        cut.WaitForAssertion(() => cut.Find("[role=option]").Click());

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-part=error]")));
    }

    [Fact]
    public void StatesExample_ShouldMarkOneReadOnlyAndDisableTheOther_WhenRendered()
    {
        var cut = Render<AutocompleteStatesExample>();
        var ridge = Input(cut.Find(".tb-stack"), "Ridge");

        Assert.True(ridge.HasAttribute("readonly"));
        Assert.True(Input(cut.Find(".tb-stack"), "Pass").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("[data-part=clear]"));
    }

    [Fact]
    public void ComponentPage_ShouldListTheKeysAndKeepFeaturesOutOfAccessibility_WhenShowingTheAutocomplete()
    {
        var cut = ComponentPage("autocomplete");

        Assert.Equal(["Down", "Alt", "Down", "Up", "Enter", "Down", "Up", "Enter", "Escape"],
            cut.FindAll("kbd").Select(key => key.TextContent));
        Assert.DoesNotContain("ReadOnly", cut.Find("ul[aria-label=Accessibility]").TextContent);
    }
}
