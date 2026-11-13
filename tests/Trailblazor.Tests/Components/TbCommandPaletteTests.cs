using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests.Components;

public class TbCommandPaletteTests : BunitContext
{
    private static readonly Command[] Commands =
    [
        new("New tab", "Actions", ["open", "setup"]),
        new("Close tab", "Actions"),
        new("Settings", "Pages", ["preferences"], Href: "settings"),
        new("Trails", "Pages", Href: "trails"),
        new("Delete everything", "Actions", Disabled: true),
    ];

    private readonly List<Command> _chosen = [];
    private readonly List<bool> _closedWhenChosen = [];
    private readonly List<bool> _openChanges = [];

    public TbCommandPaletteTests() => this.AddTrailblazor();

    [Fact]
    public void Open_ShouldShowEveryCommandUnderItsGroupInOrder_WhenNothingIsTyped()
    {
        var cut = RenderPalette();

        Assert.Equal(["Actions", "Pages"], cut.FindAll("[data-part=group-heading]").Select(Text));
        Assert.Equal(["New tab", "Close tab", "Delete everything", "Settings", "Trails"],
            cut.FindAll("[role=option]").Select(Text));
        Assert.Equal("true", cut.Find("[data-part=option]:nth-child(4)").GetAttribute("aria-disabled"));
        var input = cut.Find("[role=combobox]");
        Assert.Equal(cut.Find("[role=listbox]").Id, input.GetAttribute("aria-controls"));
        Assert.Equal("Command palette", input.GetAttribute("aria-label"));
    }

    [Fact]
    public void Open_ShouldRenderNothingButItsMarker_WhenClosed()
    {
        var cut = RenderPalette(open: false);

        Assert.Empty(cut.FindAll("dialog"));
        Assert.Equal("Command palette", cut.Find("[data-command-palette]").GetAttribute("data-command-palette"));
    }

    [Fact]
    public void Input_ShouldPutTheBestMatchesFirstAndMarkTheirLetters_WhenTheUserTypes()
    {
        var cut = RenderPalette();

        cut.Find("[role=combobox]").Input("tab");

        Assert.Equal(["New tab", "Close tab"], cut.FindAll("[role=option]").Select(Text));
        Assert.Equal(["tab"], cut.FindAll("[role=option]")[0].QuerySelectorAll("mark").Select(Text));
    }

    [Fact]
    public void Input_ShouldFindACommandByItsKeywordsBelowNameMatches_WhenTheNameDoesntMatch()
    {
        var cut = RenderPalette();

        cut.Find("[role=combobox]").Input("pre");

        Assert.Equal(["Settings"], cut.FindAll("[role=option]").Select(Text));
        Assert.Empty(cut.FindAll("mark"));
    }

    [Fact]
    public void Input_ShouldRankAKeywordMatchBelowNameMatches_WhenBothMatch()
    {
        var cut = RenderPalette();

        cut.Find("[role=combobox]").Input("set");

        // New tab's keyword "setup" matches as strongly as the name Settings does, yet New tab ranks below Close tab's
        // weaker name match.
        Assert.Equal(["Settings", "Close tab", "New tab"], cut.FindAll("[role=option]").Select(Text));
    }

    [Fact]
    public void Input_ShouldOrderTheGroupsByTheirBestMatch_WhenTheUserTypes()
    {
        var cut = RenderPalette();

        cut.Find("[role=combobox]").Input("t");

        Assert.Equal("Pages", Text(cut.FindAll("[data-part=group-heading]")[0]));
        Assert.Equal("Trails", Text(cut.FindAll("[role=option]")[0]));
    }

    [Fact]
    public void Input_ShouldAnnounceOnlyANewCountAndShowWhenNothingMatches_WhenTheUserTypes()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderPalette();

        cut.Find("[role=combobox]").Input("tab");
        cut.Find("[role=combobox]").Input("tab ");
        cut.Find("[role=combobox]").Input("tabz");

        var status = cut.Find("[data-part=status]");
        Assert.Equal(("No matching commands", "true"), (Text(status), status.GetAttribute("aria-disabled")));
        Assert.Equal(["Results: 2", "No matching commands"], cut.FindAll("[role=status] p")
            .Concat(announcer.FindAll("[role=status] p")).Select(Text));
    }

    [Fact]
    public async Task Click_ShouldCloseTheDialogThenRaiseOnChooseAndGoToItsPage_WhenACommandIsChosen()
    {
        var cut = RenderPalette();

        await cut.FindAll("[role=option]").Single(option => Text(option) == "Settings")
            .ClickAsync(new MouseEventArgs { Detail = 1 });

        Assert.Equal(["Settings"], _chosen.Select(command => command.Name));
        Assert.Equal([true], _closedWhenChosen);
        Assert.Equal([false], _openChanges);
        Assert.Empty(cut.FindAll("dialog"));
        Assert.EndsWith("/settings", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public async Task Click_ShouldDoNothing_WhenTheCommandIsDisabled()
    {
        var cut = RenderPalette();

        await cut.FindAll("[role=option]").Single(option => Text(option) == "Delete everything")
            .ClickAsync(new MouseEventArgs { Detail = 1 });

        Assert.Empty(_chosen);
        Assert.Single(cut.FindAll("dialog"));
    }

    [Fact]
    public void Open_ShouldStartWithAnEmptySearch_WhenOpenedAgain()
    {
        var cut = RenderPalette();
        cut.Find("[role=combobox]").Input("tab");

        cut.Render(parameters => parameters.Add(palette => palette.Open, false));
        cut.Render(parameters => parameters.Add(palette => palette.Open, true));

        Assert.Equal("", cut.Find("[role=combobox]").GetAttribute("value"));
        Assert.Equal(5, cut.FindAll("[role=option]").Count);
    }

    [Fact]
    public void Render_ShouldShowEachCommandsParts_WhenGivenThem()
    {
        var cut = Render<TbCommandPalette<Command>>(parameters => parameters.Add(palette => palette.Items, Commands)
            .Add(palette => palette.Open, true).Add(palette => palette.ItemText, command => command.Name)
            .Add(palette => palette.ItemDescription, command => command.Href is null ? null : $"Go to /{command.Href}")
            .Add(palette => palette.ItemIcon, command => command.Group == "Pages" ? BuiltInIcons.ChevronEnd : null)
            .Add(palette => palette.ItemShortcut, command => command.Name == "New tab" ? "Ctrl+T" : null)
            .Add(palette => palette.Label, "Jump to").Add(palette => palette.Placeholder, "Find anything"));

        Assert.Equal("Go to /settings", Text(cut.Find("[data-part=description]")));
        Assert.Equal(2, cut.FindAll(".tb-command-palette__icon").Count);
        Assert.Equal(["Ctrl", "T"], cut.FindAll("[data-part=shortcut] [data-part=key]").Select(Text));
        Assert.Equal(("Jump to", "Find anything"), (cut.Find("[role=combobox]").GetAttribute("aria-label"),
            cut.Find("[role=combobox]").GetAttribute("placeholder")));
        Assert.Equal("Jump to", Text(cut.Find("[data-command-palette]").ParentElement!.QuerySelector("dialog span")!));
    }

    [Fact]
    public void Render_ShouldDrawEachCommandWithTheTemplate_WhenGivenOne()
    {
        var cut = Render<TbCommandPalette<Command>>(parameters => parameters.Add(palette => palette.Items, Commands)
            .Add(palette => palette.Open, true).Add(palette => palette.ItemText, command => command.Name)
            .Add(palette => palette.ItemTemplate, command => builder =>
            {
                builder.OpenElement(0, "em");
                builder.AddContent(1, command.Name);
                builder.CloseElement();
            }));

        Assert.Equal(["New tab", "Close tab"], cut.FindAll("[role=option] > em").Take(2).Select(Text));
        Assert.Empty(cut.FindAll("[data-part=label]"));
    }

    [Theory]
    [InlineData("Control+K")]
    [InlineData("Mod+")]
    public void Render_ShouldThrow_WhenGivenAShortcutItCantRead(string shortcut)
    {
        var error = Assert.Throws<InvalidOperationException>(() => Render<TbCommandPalette<Command>>(parameters =>
            parameters.Add(palette => palette.Items, Commands).Add(palette => palette.Shortcut, shortcut)));

        Assert.Contains(shortcut, error.Message, StringComparison.Ordinal);
    }

    private IRenderedComponent<TbCommandPalette<Command>> RenderPalette(bool open = true) =>
        Render<TbCommandPalette<Command>>(parameters => parameters.Add(palette => palette.Items, Commands)
            .Add(palette => palette.Open, open).Add(palette => palette.ItemText, command => command.Name)
            .Add(palette => palette.ItemGroup, command => command.Group)
            .Add(palette => palette.ItemKeywords, command => command.Keywords ?? [])
            .Add(palette => palette.ItemDisabled, command => command.Disabled)
            .Add(palette => palette.ItemHref, command => command.Href)
            .Add(palette => palette.OpenChanged, (bool value) => _openChanges.Add(value))
            .Add(palette => palette.OnChoose, (Command command) =>
            {
                _chosen.Add(command);
                _closedWhenChosen.Add(_openChanges is [.., false]);
            }));

    private static string Text(AngleSharp.Dom.IElement element) =>
        (element.QuerySelector("[data-part=label]") ?? element).TextContent.Trim();

    private sealed record Command(string Name, string? Group = null, string[]? Keywords = null, bool Disabled = false,
        string? Href = null);
}
