using System.Text.RegularExpressions;
using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class GuideTests : DocsTest
{
    public static TheoryData<string> Guides => new(GuideCatalog.All.Select(guide => guide.Path));

    [Theory, MemberData(nameof(Guides))]
    public void Guide_ShouldListEachSectionOnThePageAndKeepItsNotesQuiet_WhenOpened(string path)
    {
        var cut = PageAt(path);
        var sections = cut.FindAll(".docs-article__body > h2");

        var rail = cut.FindAll(".docs-article__rail a");

        Assert.Equal(GuideCatalog.All.Single(guide => guide.Path == path).Title, cut.Find("h1").TextContent.Trim());
        Assert.NotEmpty(sections);
        Assert.Equal(Texts(sections), Texts(rail));
        Assert.Equal(sections.Select(heading => $"#{heading.Id}"),
            rail.Select(link => new Uri(link.GetAttribute("href")!).Fragment));
        // A guide's notes are part of its text, so none is a live region that a screen reader would read out at once.
        Assert.Empty(cut.FindAll(".docs-article__body .tb-alert:not([role=note])"));
    }

    [Fact]
    public void AnalyzersGuide_ShouldListEveryRuleTheAnalyzerReleases_WhenOpened()
    {
        var releases = Path.Combine(Repository.Root, "src", "Trailblazor.Analyzers");
        var released = new[] { "AnalyzerReleases.Shipped.md", "AnalyzerReleases.Unshipped.md" }
            .SelectMany(file => File.ReadLines(Path.Combine(releases, file)))
            .Select(line => Regex.Match(line, @"^(TB\d+) \|")).Where(match => match.Success)
            .Select(match => match.Groups[1].Value).Order().ToArray();

        var rules = PageAt("guides/analyzers").FindAll("table[aria-label='Analyzer rules'] tbody th[scope=row]");

        Assert.NotEmpty(released);
        Assert.Equal(released, Texts(rules));
    }

    [Fact]
    public async Task SpacingGuide_ShouldShowAndApplyTheChosenClass_WhenAPropertySideAndStepArePicked()
    {
        var cut = PageAt("guides/spacing");

        await cut.Tb().Select("Property").ChooseAsync("Margin");
        await cut.Tb().Select("Side").ChooseAsync("Block start (t)");
        await cut.Tb().Select("Step").ChooseAsync("8");

        var box = cut.Find(".docs-spacing-box");
        Assert.Equal("tb-mt-8", box.TextContent.Trim());
        Assert.Contains("tb-mt-8", box.ClassList);
        Assert.Matches(@"\.tb-mt-8 \{\s*margin-block-start: var\(--tb-space-8\);",
            File.ReadAllText(Path.Combine(Repository.Root, "src", "Trailblazor", "wwwroot", "trailblazor.css")));
    }

    [Fact]
    public void PalettesGuide_ShouldShowDisabledInvalidAndSelectedStates_WhenOpened()
    {
        var cobalt = PageAt("guides/palettes")
            .Find(".docs-palette__panel[data-tb-palette=cobalt][data-tb-theme=light]");

        Assert.True(cobalt.QuerySelectorAll("button").Single(button => button.TextContent.Trim() == "Publish")
            .HasAttribute("disabled"));
        Assert.Equal("true", Input(cobalt, "Owner email").GetAttribute("aria-invalid"));
        Assert.Equal("true", cobalt.QuerySelectorAll("button").Single(button => button.TextContent.Trim() == "Open")
            .GetAttribute("aria-pressed"));
    }

    [Fact]
    public void PalettesGuide_ShouldShowEachRampsStepsInItsColor_WhenOpened()
    {
        var cut = PageAt("guides/palettes");

        var teal = cut.FindAll("section[aria-label='teal ramp'] li");

        Assert.Equal(["50", "100", "200", "300", "400", "500", "600", "700", "800", "900", "950"], Texts(teal));
        Assert.Equal(15, cut.FindAll("section[aria-label='graphite ramp'] li").Count);
        Assert.Equal("background: var(--tb-teal-600)",
            teal[6].QuerySelector(".docs-ramp__swatch")!.GetAttribute("style"));
    }

    [Fact]
    public void ThemingGuide_ShouldNameEveryPalette_WhenOpened()
    {
        var text = PageAt("guides/theming").Find(".docs-article").TextContent;

        Assert.All(PaletteCatalog.All,
            palette => Assert.Contains(palette.Name, text, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IconsGuide_ShouldShowAndCopyTheMatchingIcons_WhenSearched()
    {
        var root = Render<TbRoot>();
        var cut = PageAt("guides/icons");

        Input(cut.Find(".docs-article"), "Icon name").Input("hiking");
        var icons = cut.FindAll("[role=list][aria-label='Matching icons'] button");
        Assert.Equal(["Hiking"], Texts(icons));
        icons[0].Click();

        Assert.Equal(["MaterialSymbols.Outlined.Hiking"], Browser.Copied);
        root.WaitForAssertion(() => Assert.Contains("Copied MaterialSymbols.Outlined.Hiking", root.Markup));
    }

    [Fact]
    public void AnnouncementsGuide_ShouldSpeakClearAndExpireMessages_WhenTheButtonsArePressed()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = PageAt("guides/announcements");
        IReadOnlyList<string> Polite() => Texts(announcer.FindAll("[role=status] p"));
        IReadOnlyList<string> Assertive() => Texts(announcer.FindAll("[role=alert] p"));

        Button(cut, "Announce politely").Click();
        Button(cut, "Announce now").Click();
        announcer.WaitForAssertion(() => Assert.StartsWith("Trail saved", Assert.Single(Polite())));
        Assert.Equal(["Couldn't reach the server"], Assertive());
        Button(cut, "Clear").Click();
        announcer.WaitForAssertion(() => Assert.Empty(Polite()));
        Assert.Empty(Assertive());

        Button(cut, "Announce politely").Click();
        announcer.WaitForAssertion(() => Assert.Single(Polite()));
        Time.Advance(TimeSpan.FromSeconds(7));

        announcer.WaitForAssertion(() => Assert.Empty(Polite()));
    }
}

// The demo counts every named control on the process's Trailblazor meter, so it runs with no other test alongside.
[CollectionDefinition(nameof(MeterReaders), DisableParallelization = true)]
public class MeterReaders;

[Collection(nameof(MeterReaders))]
public class TelemetryGuideTests : DocsTest
{
    [Fact]
    public void TelemetryGuide_ShouldCountTheDemoButtonsShownAndUsed_WhenTheyAreClicked()
    {
        var cut = PageAt("guides/telemetry");
        var counts = cut.Find("[data-testid=telemetry-counts]");
        IReadOnlyList<string> Counts(string name) => Texts(cut.FindAll("[data-testid=telemetry-counts] tbody tr")
            .Single(row => row.QuerySelector("th")!.TextContent.Trim() == name).QuerySelectorAll("td"));

        Assert.Equal(5, counts.QuerySelectorAll("tbody tr").Length);
        Assert.True(Button(cut, "Archive").HasAttribute("disabled"));
        Button(cut, "Export").Click();
        Button(cut, "Export").Click();

        cut.WaitForAssertion(() => Assert.Equal(["1", "2"], Counts("export-csv")));
        Assert.Equal(["1", "0"], Counts("archive"));
        Assert.Equal(["1", "0"], Counts("print"));
    }
}
