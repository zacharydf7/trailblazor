using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbCascaderTests : BunitContext
{
    private static readonly CascaderItem<string>[] Places = Tree();

    // Builds a new tree on each call, as a reload would, so a test can rename Ontario.
    private static CascaderItem<string>[] Tree(string ontario = "Ontario") =>
    [
        new("Canada", "ca")
        {
            Children =
            [
                new(ontario, "on") { Children = [new("Toronto", "tor"), new("Ottawa", "ott")] },
                new("Quebec", "qc") { Children = [new("Montréal", "mtl")] },
            ],
        },
        new("Iceland", "is") { Disabled = true, Children = [new("Reykjavik", "rey")] },
        new("Japan", "jp") { Children = [new("Kyoto", "kyo"), new("Osaka", "osa")] },
    ];

    private string? _value;

    public TbCascaderTests() => this.AddTrailblazor();

    private FakeAnnouncer Announcer => Services.GetRequiredService<FakeAnnouncer>();

    [Fact]
    public void Render_ShouldBeAComboboxOverTheFirstLevel_WhenInteractive()
    {
        var cut = RenderPlaces();

        var input = cut.Find("input");
        var popup = cut.Find("[data-part=popup]");
        Assert.Equal(("combobox", "listbox", popup.Id), (input.GetAttribute("role"),
            input.GetAttribute("aria-haspopup"), input.GetAttribute("aria-controls")));
        var column = cut.Find("[data-part=column]");
        Assert.Equal("Place", cut.Find($"#{column.GetAttribute("aria-labelledby")}").TextContent.Trim());
        Assert.Equal(["Canada, more choices", "Iceland, more choices", "Japan, more choices"],
            column.QuerySelectorAll("[role=option]").Select(option => option.GetAttribute("aria-label")));
        Assert.Equal("true", column.QuerySelectorAll("[role=option]")[1].GetAttribute("aria-disabled"));
    }

    [Fact]
    public async Task Pick_ShouldOpenTheNextLevel_WhenAChoiceHasMoreUnderIt()
    {
        var cut = RenderPlaces();

        await Option(cut, 0, "Canada").ClickAsync(new());
        await Option(cut, 1, "Ontario").ClickAsync(new());

        var columns = cut.FindAll("[data-part=column]");
        Assert.Equal(["", "Canada", "Ontario"], columns.Select(column => column.GetAttribute("aria-label") ?? ""));
        Assert.Equal(["Toronto", "Ottawa"], columns[2].QuerySelectorAll("[role=option]").Select(option =>
            option.GetAttribute("aria-label")));
        Assert.Null(_value);
    }

    [Fact]
    public async Task Pick_ShouldChooseThePathAndShowIt_WhenAChoiceEndsIt()
    {
        var cut = RenderPlaces();

        await Option(cut, 0, "Japan").ClickAsync(new());
        await Option(cut, 1, "Osaka").ClickAsync(new());

        Assert.Equal(("osa", "Japan / Osaka"), (_value, cut.Find("input").GetAttribute("value")));
        Assert.Equal("true", Option(cut, 1, "Osaka").GetAttribute("aria-selected"));
    }

    [Fact]
    public void Render_ShouldShowTheValuesPathAndOpenItsLevels_WhenGivenAValue()
    {
        _value = "mtl";

        var cut = RenderPlaces();

        Assert.Equal("Canada / Quebec / Montréal", cut.Find("input").GetAttribute("value"));
        Assert.Equal(3, cut.FindAll("[data-part=column]").Count);
        Assert.Equal("true", Option(cut, 2, "Montréal").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task Typing_ShouldSearchEveryLevelIgnoringAccents_WhenTheUserTypes()
    {
        var cut = RenderPlaces();

        cut.Find("input").Input("montreal");

        Assert.Equal(["Canada / Quebec / Montréal"], cut.FindAll("[data-part=results] [role=option]")
            .Select(option => option.TextContent.Trim()));
        Assert.Equal("Results: 1", Announcer.Polite[^1]);
        await cut.Find("[data-part=results] [role=option]").ClickAsync(new());
        Assert.Equal(("mtl", "Canada / Quebec / Montréal"), (_value, cut.Find("input").GetAttribute("value")));
        Assert.Empty(cut.FindAll("[data-part=results]"));
    }

    [Fact]
    public void Typing_ShouldSayNothingMatches_WhenNoPathHasTheText()
    {
        var cut = RenderPlaces();

        cut.Find("input").Input("Lisbon");

        var status = cut.Find("[data-part=status]");
        Assert.Equal(("No matches", "true"), (status.TextContent.Trim(), status.GetAttribute("aria-disabled")));
    }

    [Fact]
    public async Task Pick_ShouldChooseAParentAndOpenIt_WhenParentsAreAllowed()
    {
        var cut = RenderPlaces(parameters => parameters.Add(field => field.AllowParents, true));

        await Option(cut, 0, "Japan").ClickAsync(new());

        Assert.Equal(("jp", "Japan"), (_value, cut.Find("input").GetAttribute("value")));
        Assert.Equal(2, cut.FindAll("[data-part=column]").Count);
    }

    [Fact]
    public async Task Pick_ShouldDoNothing_WhenTheChoiceIsDisabled()
    {
        var cut = RenderPlaces();

        await Option(cut, 0, "Iceland").ClickAsync(new());

        Assert.Null(_value);
        Assert.Single(cut.FindAll("[data-part=column]"));
    }

    [Fact]
    public async Task Change_ShouldTakeAPathTypedInFull_WhenTheFieldIsLeft()
    {
        var cut = RenderPlaces();

        cut.Find("input").Input("japan / kyoto");
        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = "japan / kyoto" });

        Assert.Equal(("kyo", "Japan / Kyoto"), (_value, cut.Find("input").GetAttribute("value")));
    }

    [Fact]
    public async Task Change_ShouldPutTheValuesPathBack_WhenTheTextIsNoPath()
    {
        _value = "kyo";
        var cut = RenderPlaces();

        cut.Find("input").Input("Kyo");
        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = "Kyo" });

        Assert.Equal(("kyo", "Japan / Kyoto"), (_value, cut.Find("input").GetAttribute("value")));
    }

    [Fact]
    public async Task Change_ShouldNotTakeAPath_WhenAStepOnItIsDisabled()
    {
        var cut = RenderPlaces();

        cut.Find("input").Input("Iceland / Reykjavik");
        var results = cut.FindAll("[data-part=results] [data-part=option]").Count;
        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = "Iceland / Reykjavik" });

        Assert.Equal((0, null, ""), (results, _value, cut.Find("input").GetAttribute("value")));
    }

    [Fact]
    public void Items_ShouldReopenTheValuesLevels_WhenTheTreeIsLoadedAgain()
    {
        _value = "tor";
        var cut = RenderPlaces();

        cut.Render(parameters => parameters.Add(field => field.Items, Tree("Ontario province")));

        Assert.True(Option(cut, 0, "Canada").HasAttribute("data-open"));
        Assert.True(Option(cut, 1, "Ontario province").HasAttribute("data-open"));
        Assert.Equal("true", Option(cut, 2, "Toronto").GetAttribute("aria-selected"));
        Assert.Equal("Canada / Ontario province / Toronto", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Typing_ShouldSearchTheTree_WhenItArrivesAfterTheTextWasTyped()
    {
        var cut = Render<TbCascader<string>>(parameters => parameters.Add(field => field.Label, "Place")
            .Add(field => field.Items, []));

        cut.Find("input").Input("tor");
        var before = cut.FindAll("[data-part=status]").Count;
        cut.Render(parameters => parameters.Add(field => field.Items, Places));

        Assert.Equal(1, before);
        Assert.Equal(["Canada / Ontario / Toronto"], cut.FindAll("[data-part=results] [data-part=option]")
            .Select(option => option.TextContent.Trim()));
    }

    [Fact]
    public async Task Helper_ShouldChooseAPathOrAResult_WhenUsed()
    {
        var cut = RenderPlaces();
        var cascader = cut.Tb().Cascader("Place");

        await cascader.ChooseAsync("Japan", "Kyoto");
        var chosen = (_value, cascader.Text);
        await cascader.ChooseResultAsync("otta", "Canada / Ontario / Ottawa");

        Assert.Equal(("kyo", "Japan / Kyoto"), chosen);
        Assert.Equal(("ott", "Canada / Ontario / Ottawa"), (_value, cascader.Text));
    }

    [Fact]
    public async Task Helper_ShouldRefuse_WhenAChoiceIsMissingOrTheFieldIsDisabled()
    {
        var cut = RenderPlaces();
        var disabled = Render<TbCascader<string>>(parameters => parameters.Add(field => field.Label, "Home")
            .Add(field => field.Items, Places).Add(field => field.Disabled, true));

        var missing = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Cascader("Place").ChooseAsync("Japan", "Nara"));
        await Assert.ThrowsAnyAsync<InvalidOperationException>(
            () => disabled.Tb().Cascader("Home").ChooseAsync("Japan"));

        Assert.Equal("Level 2 has no choice \"Nara\".", missing.Message);
    }

    [Fact]
    public async Task Clear_ShouldEmptyTheFieldAndCloseItsLevels_WhenTheClearButtonIsClicked()
    {
        _value = "tor";
        var cut = RenderPlaces(parameters => parameters.Add(field => field.Clearable, true));

        await cut.Find("[data-part=clear]").ClickAsync(new());

        Assert.Equal((null, ""), (_value, cut.Find("input").GetAttribute("value")));
        Assert.Single(cut.FindAll("[data-part=column]"));
    }

    private static AngleSharp.Dom.IElement Option(IRenderedComponent<TbCascader<string>> cut, int level, string text) =>
        cut.FindAll($"[data-part=column][data-level='{level}'] [role=option]")
            .Single(option => option.QuerySelector(".tb-cascader__text")!.TextContent == text);

    private IRenderedComponent<TbCascader<string>> RenderPlaces(
        Action<ComponentParameterCollectionBuilder<TbCascader<string>>>? configure = null) =>
        Render<TbCascader<string>>(parameters =>
        {
            parameters.Add(field => field.Label, "Place").Add(field => field.Items, Places)
                .Add(field => field.Value, _value).Add(field => field.ValueChanged, (string value) => _value = value);
            configure?.Invoke(parameters);
        });
}
