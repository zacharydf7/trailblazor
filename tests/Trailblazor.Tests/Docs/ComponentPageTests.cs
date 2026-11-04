using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class ComponentPageTests : DocsTest
{
    // The titles in the order the site lists them: by category, then as the catalog has them.
    private static string[] Titles => [.. ExampleCatalog.All.OrderBy(example => example.Category)
        .Select(example => example.Title)];

    [Fact]
    public void ComponentPage_ShouldTableItsOwnTokensWithTheirDefaults_WhenItHasSome()
    {
        var cut = ComponentPage("button");

        var rows = cut.FindAll("table[aria-label='Its own tokens'] tbody tr").Select(row => row.TextContent.Trim());

        Assert.Contains(rows, row => row.StartsWith("--tb-button-radius") && row.EndsWith("var(--tb-radius-md)"));
    }

    [Fact]
    public async Task ComponentPage_ShouldShowEveryFileOfTheExample_WhenItHasMoreThanOne()
    {
        var cut = ComponentPage("dialog");

        Assert.Contains("ShowAsync<RenameDialog, string>",
            cut.Find("#code-example [role=region][aria-label='DialogExample.razor']").TextContent);
        await cut.Tb().Tabs("Files").SelectAsync("RenameDialog.razor");

        cut.WaitForAssertion(() => Assert.Contains("TbDialogContent<string>",
            cut.Find("#code-example [role=region][aria-label='RenameDialog.razor']").TextContent));
    }

    [Fact]
    public void ComponentPage_ShouldShowEachFeatureWithItsCodeOnRequest_WhenOpened()
    {
        var cut = ComponentPage("tree-view");
        var toggle = cut.Find("button[aria-controls='code-many-rows']");

        Assert.Contains("Many rows", Texts(cut.FindAll("h2")));
        Assert.NotNull(cut.Find("[role=tree][aria-label='Trail atlas']"));
        Assert.True(cut.Find("#code-many-rows").HasAttribute("hidden"));
        toggle.Click();
        Assert.False(cut.Find("#code-many-rows").HasAttribute("hidden"));
        Assert.Contains("Virtualize", cut.Find("#code-many-rows").TextContent);
        Assert.Equal("Hide code", cut.Find("button[aria-controls='code-many-rows']").TextContent.Trim());
        cut.Find("button[aria-controls='code-many-rows']").Click();

        Assert.True(cut.Find("#code-many-rows").HasAttribute("hidden"));
    }

    [Theory]
    [InlineData(true, "Copied TreeViewVirtualExample.razor.")]
    [InlineData(false, "Couldn't copy TreeViewVirtualExample.razor.")]
    public void ComponentPage_ShouldCopyTheShownFileAndSayWhichItCopied_WhenCopyCodeIsClicked(bool canCopy,
        string message)
    {
        Browser.CanCopy = canCopy;
        var announcer = Render<AnnouncerHost>();
        var cut = ComponentPage("tree-view");
        var frame = cut.Find("#code-many-rows").ParentElement!;

        frame.QuerySelectorAll("button").Single(button => button.TextContent.Trim() == "Copy code").Click();

        Assert.Equal(canCopy ? [ExampleCatalog.Find("tree-view")!.Shown.Single(section => section.Id == "many-rows")
            .Sources[0].Text] : [], Browser.Copied);
        announcer.WaitForAssertion(() => Assert.Equal([message],
            announcer.FindAll("[role=status] p").Select(p => p.TextContent)));
    }

    [Fact]
    public void ComponentPage_ShouldLinkItsOverviewAndApiPagesAndMarkTheCurrentOne_WhenEitherIsOpened()
    {
        var overview = ComponentPage("button");
        var api = ApiPage("button");

        Assert.Equal(["Overview", "API"], Texts(overview.FindAll("nav[aria-label='Button pages'] a")));
        Assert.Equal("Overview", overview.Find("nav[aria-label='Button pages'] a[aria-current=page]").TextContent);
        Assert.Equal("components/button/api", api.Find("nav[aria-label='Button pages'] a[aria-current=page]")
            .GetAttribute("href"));
        Assert.Empty(overview.FindAll("[role=region][aria-label='TbButton parameters']"));
        Assert.NotNull(api.Find("[role=region][aria-label='TbButton parameters']"));
    }

    [Fact]
    public void ComponentPage_ShouldLinkTheNextComponentInTheListsOrder_WhenItIsNotTheLast()
    {
        var next = ExampleCatalog.All.Single(example => example.Title == Titles[Array.IndexOf(Titles, "Button") + 1]);

        var link = ComponentPage("button").Find("nav[aria-label='Previous and next components'] a[rel=next]");

        Assert.Equal($"Next{next.Title}", link.TextContent.Trim());
        Assert.Equal($"components/{next.Name}", link.GetAttribute("href"));
    }

    [Fact]
    public void ComponentPage_ShouldStartTheNextComponentWithItsCodeHidden_WhenItsNameChangesWithCodeShown()
    {
        var cut = ComponentPage("tree-view");
        cut.Find("button[aria-controls='code-filter-and-select']").Click();
        var next = ExampleCatalog.All.Single(example =>
            example.Title == Titles[Array.IndexOf(Titles, "Tree view") + 1]);

        cut.Render(parameters => parameters.Add(page => page.Name, next.Name));

        Assert.Equal(next.Title, cut.Find("h1").TextContent.Trim());
        Assert.DoesNotContain("Hide code", Texts(cut.FindAll("button")));
    }

    [Fact]
    public void ComponentPage_ShouldTableTheComponentsParameters_WhenItsApiPageIsOpened()
    {
        var cut = ApiPage("button");

        Assert.StartsWith("A button with text", cut.Find(".docs-api__summary").TextContent);
        var variant = cut.FindAll("[role=region][aria-label='TbButton parameters'] tbody tr")
            .Single(row => row.QuerySelector("th[scope=row] code")!.TextContent == "Variant");
        var cells = variant.QuerySelectorAll("td");
        Assert.Equal("Variant", cells[0].QuerySelector("code")!.TextContent);
        Assert.Equal("Variant.Filled", cells[1].QuerySelector("code")!.TextContent);
        Assert.StartsWith("How strongly the button is drawn", cells[2].TextContent.Trim());
    }

    [Fact]
    public void ApiPage_ShouldListAnEnumsValuesWithTheDefaultAndLinkEachRow_WhenItsApiPageIsOpened()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("components/button/api");
        var cut = ApiPage("button");

        var variant = cut.Find("tr#tbbutton-variant");
        Assert.Equal(Enum.GetNames<Variant>(), variant.QuerySelectorAll("ul[aria-label='Values of Variant'] li code")
            .Select(code => code.TextContent));
        Assert.Contains("(default)", variant.QuerySelectorAll("li").Single(item => item.TextContent.Contains("Filled"))
            .TextContent);
        Assert.Equal("components/button/api#tbbutton-variant", variant.QuerySelector("a.docs-api__anchor")!
            .GetAttribute("href"));
        Assert.Equal("Link to FocusAsync",
            cut.Find("tr#tbbutton-focusasync a.docs-api__anchor").GetAttribute("aria-label"));
        Assert.NotNull(cut.Find("[role=region][aria-label='TbButton events'] tr#tbbutton-onclick"));
    }

    [Fact]
    public void ApiPage_ShouldLabelEachCellForPhonesButNotScreenReaders_WhenItsApiPageIsOpened()
    {
        var cut = ApiPage("button");

        Assert.All(cut.FindAll(".docs-api__label"), label => Assert.Equal("true", label.GetAttribute("aria-hidden")));
        Assert.Equal(["Type", "Default"], Texts(cut.FindAll("tr#tbbutton-variant .docs-api__label")));
        Assert.Single(cut.FindAll("tr#tbbutton-starticon td.docs-api__none"));
        // A phone restyles the rows as grids, so the table's roles are spelled out rather than left to its tags.
        IEnumerable<string?> Roles(string selector) => cut.FindAll($"[aria-label='TbButton parameters'] {selector}")
            .Select(element => element.GetAttribute("role"));
        Assert.Equal(["table"], Roles("table"));
        Assert.Equal(["rowgroup", "row", "rowgroup"], Roles(":is(thead, thead > tr, tbody)"));
        Assert.Equal(["columnheader", "columnheader", "columnheader", "columnheader"], Roles("thead th"));
        Assert.Equal(["row", "rowheader", "cell", "cell", "cell"],
            Roles("tr#tbbutton-variant, tr#tbbutton-variant > *"));
        Assert.Equal(["Type"], Texts(cut.FindAll("tr#tbbutton-starticon .docs-api__label")));
    }

    [Fact]
    public void ApiPage_ShouldListEachTypesGroupsUnderItInTheContents_WhenOpened()
    {
        var rail = ApiPage("button").Find(".docs-article__rail");

        Assert.Equal(["TbButton", "Parameters", "Content", "Events", "Methods"],
            Texts(rail.QuerySelectorAll("a")).Take(5));
        Assert.EndsWith("#tbbutton--events", rail.QuerySelectorAll("a")[3].GetAttribute("href"));
    }

    [Fact]
    public void ApiPage_ShouldListTheGridsMethods_WhenItsApiPageIsOpened()
    {
        var methods = ApiPage("data-grid").Find("[role=region][aria-label='TbDataGrid<TItem> methods']");

        Assert.Contains(Texts(methods.QuerySelectorAll("tbody th code")), name => name == "RefreshAsync()");
        Assert.Contains(Texts(methods.QuerySelectorAll("tbody th code")), name => name == "RenderRows()");
        Assert.Equal("components/data-grid/api#tbdatagrid-refreshasync",
            ApiPage("data-grid").Find("tr#tbdatagrid-items a[href*=refreshasync]").GetAttribute("href"));
    }

    [Fact]
    public void ComponentPage_ShouldTableTheKeys_WhenTheEntryHasKeyboardRows()
    {
        var keys = ComponentPage("select").Find("[role=region][aria-label=Keyboard] table");

        Assert.Equal(ExampleCatalog.Find("select")!.Keyboard.Count,
            keys.QuerySelectorAll("tbody th[scope=row]").Length);
        Assert.Equal(["While closed", "While open", "With Chips", "With Filterable"],
            Texts(keys.QuerySelectorAll("tbody th[scope=rowgroup]")));
        Assert.Equal(["Down", "Up", "Enter", "Space"],
            Texts(keys.QuerySelector("tbody th[scope=row]")!.QuerySelectorAll("kbd")));
    }

    [Fact]
    public void ComponentPage_ShouldSplitAccessibilityIntoHeadingsAndLinkThePattern_WhenTheEntryFillsThem()
    {
        var cut = ComponentPage("select");

        Assert.Equal(["Keyboard", "Labeling", "What screen readers announce", "What the component does",
                "What your app must do", "Before the page is interactive"],
            Texts(cut.FindAll(
                "#keyboard, #labeling, #announcements, #what-it-does, #app-duties, #before-interactive")));
        Assert.Equal(ExampleCatalog.Find("select")!.Pattern!.Url,
            cut.FindAll("a").Single(link => link.TextContent == "combobox pattern").GetAttribute("href"));
        Assert.Equal("components/text-field", cut.FindAll("ul[aria-label='Notes and limitations'] a")
            .Single(link => link.TextContent == "text field").GetAttribute("href"));
        Assert.Single(cut.FindAll("[role=note]"));
    }

    [Fact]
    public void ComponentPage_ShouldPutTheFieldAdviceInLabelingAndNotes_WhenItIsAField()
    {
        var cut = ComponentPage("text-field");

        Assert.Contains("AriaLabel", cut.FindAll("ul[aria-label=Labeling] li")[^1].TextContent);
        Assert.Contains("OnFocus", cut.FindAll("ul[aria-label='Notes and limitations'] li")[^1].TextContent);
        Assert.DoesNotContain("OnFocus", cut.Find("ul[aria-label=Accessibility]").TextContent);
        Assert.DoesNotContain("TB004", cut.Find("ul[aria-label=Accessibility]").TextContent);
    }

    [Fact]
    public void ComponentPage_ShouldLinkTheRelatedComponentsAndItsUsage_WhenTheEntryNamesThem()
    {
        var cut = ComponentPage("select");
        var names = ExampleCatalog.Find("select")!.Related;

        var related = cut.FindAll("ul[aria-label=Related] a");

        Assert.Equal([.. names.Select(name => ExampleCatalog.Find(name)!.Title)], Texts(related));
        Assert.Equal([.. names.Select(name => $"components/{name}")],
            related.Select(link => link.GetAttribute("href")));
        Assert.Contains("Usage", Texts(cut.FindAll(".docs-article__rail a")));
    }

    [Fact]
    public void ComponentPage_ShouldLeaveOutUsageNotesAndRelated_WhenTheEntryHasNone()
    {
        var cut = ComponentPage("button");

        foreach (var heading in new[] { "Usage", "Notes and limitations", "Related" })
        {
            Assert.DoesNotContain(heading, Texts(cut.FindAll("h2")));
            Assert.DoesNotContain(heading, Texts(cut.FindAll(".docs-article__rail a")));
        }
    }

    [Fact]
    public void ComponentPage_ShouldListTheAccessibilityNotesAndTheLocator_WhenOpened()
    {
        var cut = ComponentPage("button");
        var button = ExampleCatalog.Find("button")!;

        var notes = cut.FindAll("ul[aria-label=Accessibility] li");

        Assert.Equal(button.Accessibility.Count, notes.Count);
        Assert.Equal(button.Telemetry.Count, cut.FindAll("ul[aria-label=Telemetry] li").Count);
        Assert.StartsWith("A native <button>, so Enter and Space press it.", notes[0].TextContent);
        Assert.Equal("<button>", notes[0].QuerySelector("code")!.TextContent);
        Assert.Equal("""page.GetByRole(AriaRole.Button, new() { Name = "Add item" })""",
            cut.Find("[role=region][aria-label='Finding it in Playwright']").TextContent);
    }

    [Fact]
    public void ComponentPage_ShouldListTheThemeTokensTheComponentReads_WhenOpened()
    {
        var tokens = Texts(ComponentPage("dialog").FindAll("ul[aria-label='Design tokens'] li"));

        Assert.Equal(DesignTokens.Read(["tb-dialog"]), tokens);
        Assert.Single(tokens, token => token.Contains("--tb-shadow-lg"));
    }

    [Fact]
    public void ComponentPage_ShouldSayItReadsNoThemeTokens_WhenTheComponentReadsNone()
    {
        var cut = ComponentPage("stack");

        Assert.Contains("Its styles read no theme tokens.", cut.Markup);
        Assert.Empty(cut.FindAll("ul[aria-label='Design tokens']"));
    }

    [Fact]
    public void ComponentPage_ShouldShowTheStepThatOpensTheComponent_WhenItOnlyShowsAfterOne()
    {
        var steps = ComponentPage("dialog").Find("[role=region][aria-label='Finding it in Playwright']");

        Assert.Equal("""
            await page.GetByRole(AriaRole.Button, new() { Name = "Rename report" }).ClickAsync();
            page.GetByRole(AriaRole.Dialog, new() { Name = "Rename report" })
            """, steps.TextContent);
    }

    [Fact]
    public void ComponentPage_ShouldSayThereIsNoRoleToFindItBy_WhenTheComponentHasNone()
    {
        var cut = ComponentPage("stack");

        Assert.Contains("It has no role and name a test can single out", cut.Markup);
        Assert.Empty(cut.FindAll("[role=region][aria-label='Finding it in Playwright']"));
    }
}
