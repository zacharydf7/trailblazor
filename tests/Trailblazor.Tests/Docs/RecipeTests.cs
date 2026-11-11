using Bunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Trailblazor.Docs.Examples;
using Trailblazor.Docs.Recipes;
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
    public void GridDatabaseRecipe_ShouldShowCodeThatQueriesAndRefreshes_WhenRendered()
    {
        var cut = PageAt("recipes/grid-from-database");

        var code = string.Join(' ', cut.FindAll("pre").Select(block => block.TextContent));

        Assert.Contains("ToListAsync", code);
        Assert.Contains("RefreshAsync", code);
        Assert.Contains("class HikeContext", code);
        Assert.Contains("AddDbContextFactory", code);
    }

    // The ties' ids run against the order they're added in, so only the id in the sort puts them in order.
    private static readonly Hike[] Hikes =
    [
        new() { Id = 3, Date = new(2026, 6, 1), Trail = "Pine Hollow loop", Hours = 3 },
        new() { Id = 2, Date = new(2026, 6, 2), Trail = "Cedar Flats", Hours = 1 },
        new() { Id = 1, Date = new(2026, 6, 3), Trail = "Eagle Ridge", Hours = 3 },
    ];

    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(5);

    private IRenderedComponent<HikeLog> RenderHikeLog(Hike[] hikes, IInterceptor? interceptor = null)
    {
        var database = Guid.NewGuid().ToString();
        Services.AddDbContextFactory<HikeContext>(options =>
        {
            options.UseInMemoryDatabase(database);
            if (interceptor is not null)
            {
                options.AddInterceptors(interceptor);
            }
        });
        using var context = Services.GetRequiredService<IDbContextFactory<HikeContext>>().CreateDbContext();
        context.Hikes.AddRange(hikes);
        context.SaveChanges();
        var cut = Render<HikeLog>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-part=row]")), QueryTimeout);
        return cut;
    }

    private static IReadOnlyList<string> TrailsIn(IRenderedComponent<HikeLog> cut) =>
        [.. cut.FindAll("tr[data-part=row] td[data-column=Trail]").Select(cell => cell.TextContent.Trim())];

    [Fact]
    public void HikeLog_ShouldBreakTiesByTheId_WhenAColumnIsSortedDescending()
    {
        var cut = RenderHikeLog(Hikes);

        cut.Find("th[data-title=Hours] [data-part=sort]").Click();
        cut.WaitForAssertion(() => Assert.Equal("Cedar Flats", TrailsIn(cut)[0]), QueryTimeout);
        cut.Find("th[data-title=Hours] [data-part=sort]").Click();

        cut.WaitForAssertion(() => Assert.Equal(["Eagle Ridge", "Pine Hollow loop", "Cedar Flats"], TrailsIn(cut)),
            QueryTimeout);
    }

    [Fact]
    public async Task HikeLog_ShouldShowOnlyMatchingRows_WhenATrailIsTyped()
    {
        var cut = RenderHikeLog(Hikes);

        await cut.Tb().Grid("Hike log").FilterAsync("Trail", "Ridge");

        cut.WaitForAssertion(() => Assert.Equal(["Eagle Ridge"], TrailsIn(cut)), QueryTimeout);
    }

    [Fact]
    public void HikeLog_ShouldShowTheNextPageAndTheFullCount_WhenThereAreMoreRowsThanFit()
    {
        var hikes = Enumerable.Range(1, 25).Select(id =>
            new Hike { Id = id, Date = new(2026, 6, 1), Trail = $"Trail {id}", Hours = 1 }).ToArray();
        var cut = RenderHikeLog(hikes);
        Assert.Equal(20, TrailsIn(cut).Count);
        Assert.Contains("of 25", cut.Find("[data-part=summary]").TextContent);

        cut.Find("button[aria-label='Page 2']").Click();

        cut.WaitForAssertion(() => Assert.Equal(["Trail 21", "Trail 22", "Trail 23", "Trail 24", "Trail 25"],
            TrailsIn(cut)), QueryTimeout);
    }

    [Fact]
    public void HikeLog_ShouldShowTheNewRow_WhenAHikeIsSaved()
    {
        var cut = RenderHikeLog(Hikes);

        Button(cut, "Log today's hike").Click();

        cut.WaitForAssertion(() => Assert.Equal(4, cut.FindAll("[data-part=row]").Count), QueryTimeout);
    }

    [Fact]
    public void HikeLog_ShouldLogAndSayItFailedWithoutAddingARow_WhenASaveFails()
    {
        var log = new FakeLogger<HikeLog>();
        Services.AddSingleton<ILogger<HikeLog>>(log);
        var cut = RenderHikeLog(Hikes, new FailingSave());

        Button(cut, "Log today's hike").Click();

        cut.WaitForAssertion(() => Assert.Contains("Couldn't save the hike.", cut.Markup), QueryTimeout);
        Assert.Equal(3, cut.FindAll("[data-part=row]").Count);
        Assert.Equal("Couldn't save the hike.", log.LatestRecord.Message);
        Assert.IsType<InvalidOperationException>(log.LatestRecord.Exception);

        Button(cut, "Log today's hike").Click();

        cut.WaitForAssertion(() => Assert.Equal(4, cut.FindAll("[data-part=row]").Count), QueryTimeout);
        Assert.DoesNotContain("Couldn't save the hike.", cut.Markup);
    }

    [Fact]
    public void HikeLog_ShouldLogAndShowTheFailedRow_WhenALoadFails()
    {
        var log = new FakeLogger<HikeLog>();
        Services.AddSingleton<ILogger<HikeLog>>(log);
        Services.AddSingleton<IDbContextFactory<HikeContext>>(new FailingFactory());

        var cut = Render<HikeLog>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-part=failed]")), QueryTimeout);
        Assert.Equal("Couldn't load the hike log.", log.LatestRecord.Message);
        Assert.IsType<InvalidOperationException>(log.LatestRecord.Exception);
    }

    // Fails the first save, so the test can try again.
    private sealed class FailingSave : SaveChangesInterceptor
    {
        private bool _failed;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (_failed)
            {
                return ValueTask.FromResult(result);
            }

            _failed = true;
            throw new InvalidOperationException("The database is offline.");
        }
    }

    private sealed class FailingFactory : IDbContextFactory<HikeContext>
    {
        public HikeContext CreateDbContext() => throw new InvalidOperationException("The database is offline.");
    }

    [Fact]
    public async Task FilterGrid_ShouldShowOnlyMatchingRows_WhenAFilterChanges()
    {
        var cut = Render<RecipeFilterGridExample>();
        cut.WaitForAssertion(() => Assert.Contains("Pine Hollow loop", cut.Markup), TimeSpan.FromSeconds(5));

        await cut.Find("input[type=search]").InputAsync(new() { Value = "ridge" });

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
