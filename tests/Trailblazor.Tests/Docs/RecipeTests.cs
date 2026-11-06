using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Docs.Examples;
using Trailblazor.Docs.Layout;

namespace Trailblazor.Tests.Docs;

public class RecipeTests : DocsTest
{
    [Fact]
    public void NavMenu_ShouldListRecipesApartFromGuides_WhenRendered()
    {
        var cut = Render<NavMenu>();

        var headings = Texts(cut.FindAll(".docs-nav__heading"));
        var guides = cut.FindAll(".docs-nav__list")[0].QuerySelectorAll("a");
        var recipes = cut.FindAll(".docs-nav__list")[1].QuerySelectorAll("a");

        Assert.Equal(["Guides", "Recipes"], headings.Take(2));
        Assert.DoesNotContain(guides, link => link.GetAttribute("href")!.StartsWith("recipes/"));
        Assert.Equal(GuideCatalog.All.Where(guide => guide.IsRecipe).Select(guide => guide.Path),
            recipes.Select(link => link.GetAttribute("href")));
    }

    [Fact]
    public async Task FilterGrid_ShouldShowOnlyMatchingRows_WhenAFilterChanges()
    {
        var cut = Render<RecipeFilterGridExample>();
        cut.WaitForAssertion(() => Assert.Contains("Pine Hollow loop", cut.Markup), TimeSpan.FromSeconds(5));

        await cut.Find("input[type=search]").InputAsync(new() { Value = "ridge" });
        Time.Advance(TimeSpan.FromMilliseconds(300));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Eagle Ridge traverse", cut.Markup);
            Assert.DoesNotContain("Pine Hollow loop", cut.Markup);
        }, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task FilterGrid_ShouldGoBackToTheFirstPage_WhenAFilterChangesOnALaterPage()
    {
        var cut = Render<RecipeFilterGridExample>();
        cut.WaitForAssertion(() => Assert.Contains("Pine Hollow loop", cut.Markup), TimeSpan.FromSeconds(5));
        await cut.Find("button[aria-label='Page 2']").ClickAsync(new());
        cut.WaitForAssertion(() => Assert.DoesNotContain("Pine Hollow loop", cut.Markup), TimeSpan.FromSeconds(5));

        // Five trails match, so a grid left on page 2 would show only the last of them.
        await cut.Find("input[type=search]").InputAsync(new() { Value = "a" });
        Time.Advance(TimeSpan.FromMilliseconds(300));

        cut.WaitForAssertion(() => Assert.Contains("Eagle Ridge traverse", cut.Markup), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task EditDialog_ShouldCloseWithWhatWasEntered_WhenSaved()
    {
        var dialog = await this.ShowDialogAsync<RecipeEditDialog, (string Name, int Kilometers)>("Edit",
            parameters => parameters.Set(content => content.Name, "Cedar Flats").Set(content => content.Kilometers, 4));
        await dialog.Content.Find("input[type=text]").ChangeAsync(new() { Value = "Cedar Meadows" });

        await dialog.Content.Find("form").SubmitAsync();

        var result = await dialog.Reference.Result;
        Assert.True(result.Confirmed);
        Assert.Equal(("Cedar Meadows", 4), result.Value);
    }

    [Fact]
    public async Task EditDialog_ShouldCloseWithoutAValue_WhenCanceled()
    {
        var dialog = await this.ShowDialogAsync<RecipeEditDialog, (string Name, int Kilometers)>("Edit",
            parameters => parameters.Set(content => content.Name, "Cedar Flats").Set(content => content.Kilometers, 4));

        await Button(dialog.Content, "Cancel").ClickAsync(new());

        Assert.False((await dialog.Reference.Result).Confirmed);
    }

    [Fact]
    public async Task EditDialog_ShouldStayOpenAndSayWhatIsMissing_WhenTheNameIsCleared()
    {
        var dialog = await this.ShowDialogAsync<RecipeEditDialog, (string Name, int Kilometers)>("Edit",
            parameters => parameters.Set(content => content.Name, "Cedar Flats").Set(content => content.Kilometers, 4));
        await dialog.Content.Find("input[type=text]").ChangeAsync(new() { Value = "" });

        await dialog.Content.Find("form").SubmitAsync();

        Assert.Equal("Enter the trail's name.", dialog.Content.Find("[data-part=error]").TextContent.Trim());
        Assert.False(dialog.Reference.Result.IsCompleted);
    }

    [Fact]
    public async Task EditExample_ShouldUpdateTheRow_WhenTheDialogSaves()
    {
        var cut = Render<RecipeEditExample>();
        var click = Button(cut, "Edit Cedar Flats").ClickAsync(new());

        await Services.GetRequiredService<FakeDialogService>().Last<RecipeEditDialog, (string Name, int Kilometers)>()
            .CloseAsync(("Cedar Meadows", 5));
        await click;

        Assert.Equal(["Edit Pine Hollow loop", "Edit Cedar Meadows"],
            cut.FindAll("tbody button").Select(button => button.GetAttribute("aria-label")));
        Assert.Contains("5", cut.FindAll("tbody tr")[1].TextContent);
    }

    [Fact]
    public async Task EditExample_ShouldLeaveTheRowAlone_WhenTheDialogIsCanceled()
    {
        var cut = Render<RecipeEditExample>();
        var click = Button(cut, "Edit Cedar Flats").ClickAsync(new());

        await Services.GetRequiredService<FakeDialogService>().Last<RecipeEditDialog, (string Name, int Kilometers)>()
            .CancelAsync();
        await click;

        Assert.Contains("Cedar Flats", cut.FindAll("tbody tr")[1].TextContent);
    }

    [Fact]
    public void EditExample_ShouldNameEachRowsButton_WhenRendered()
    {
        var cut = Render<RecipeEditExample>();

        Assert.Equal(["Edit Pine Hollow loop", "Edit Cedar Flats"],
            cut.FindAll("tbody button").Select(button => button.GetAttribute("aria-label")));
    }

    [Fact]
    public async Task DeleteExample_ShouldRemoveTheTrailAndFocusTheList_WhenConfirmed()
    {
        Services.GetRequiredService<FakeDialogService>().Answer = _ => true;
        var cut = Render<RecipeDeleteExample>();

        await Button(cut, "Delete Cedar Flats").ClickAsync(new());

        Assert.DoesNotContain("Cedar Flats", cut.Find("[role=group]").TextContent);
        JSInterop.VerifyFocusAsyncInvoke();
    }

    [Fact]
    public async Task DeleteExample_ShouldRestoreEachTrailInItsPlace_WhenUndoneOutOfOrder()
    {
        Services.GetRequiredService<FakeDialogService>().Answer = _ => true;
        var root = Render<TbRoot>();
        var cut = Render<RecipeDeleteExample>();
        await Button(cut, "Delete Fern Gully").ClickAsync(new());
        await Button(cut, "Delete Cedar Flats").ClickAsync(new());
        await Button(cut, "Delete Pine Hollow loop").ClickAsync(new());
        Assert.Empty(cut.FindAll("[role=group] button"));

        foreach (var trail in new[] { "Fern Gully", "Pine Hollow loop", "Cedar Flats" })
        {
            var toast = root.FindAll("[data-part=toast]").Single(toast => toast.TextContent.Contains(trail));
            await toast.QuerySelectorAll("button").Single(button => button.TextContent.Trim() == "Undo").ClickAsync(new());
        }

        cut.WaitForAssertion(() => Assert.Equal(
            ["Delete Pine Hollow loop", "Delete Cedar Flats", "Delete Fern Gully"],
            cut.FindAll("[role=group] button").Select(button => button.GetAttribute("aria-label"))));
    }

    [Fact]
    public async Task DeleteExample_ShouldKeepTheTrail_WhenNotConfirmed()
    {
        Services.GetRequiredService<FakeDialogService>().Answer = _ => false;
        var cut = Render<RecipeDeleteExample>();

        await Button(cut, "Delete Cedar Flats").ClickAsync(new());

        Assert.Contains("Cedar Flats", cut.Find("[role=group]").TextContent);
    }

    [Fact]
    public async Task SettingsExample_ShouldNameTheTabThatNeedsAttention_WhenASaveFails()
    {
        var cut = Render<RecipeSettingsExample>();

        await cut.Find("form").SubmitAsync();

        Assert.Contains(cut.FindAll("[role=tab]"), tab => tab.TextContent.Contains("Profile, needs attention"));
        Assert.Equal("Enter a display name.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public async Task MasterDetailExample_ShouldShowTheTrailsDetails_WhenATrailIsSelected()
    {
        var cut = Render<RecipeMasterDetailExample>();

        await cut.Tb().Tree("Trails by region").Item("North ridge", "Eagle Ridge traverse").ClickAsync(new());

        Assert.Contains("18 kilometers.", cut.Find("[aria-label='Trail details']").TextContent);
    }

    [Fact]
    public void Search_ShouldFindTheRecipes_WhenGivenTheirCategory()
    {
        var results = DocsSearch.Find("recipes");

        Assert.Equal(GuideCatalog.All.Where(guide => guide.IsRecipe).Select(guide => guide.Title),
            results.Where(result => result.Match == SearchMatch.Title).Select(result => result.Page));
    }

    [Fact]
    public void MasterDetailExample_ShouldAskForAChoice_WhenNothingIsSelected()
    {
        var cut = Render<RecipeMasterDetailExample>();

        Assert.Contains("Choose a trail", cut.Find("[aria-label='Trail details']").TextContent);
        Assert.Equal("polite", cut.Find("[aria-label='Trail details']").GetAttribute("aria-live"));
    }
}
