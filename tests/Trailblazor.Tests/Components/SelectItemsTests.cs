using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Trailblazor.Tests.Components;

public class SelectItemsTests : BunitContext
{
    private static readonly Trail[] Trails = [new(1, "Ridge loop"), new(2, "Lake shore"), new(3, "Cedar flats")];

    public SelectItemsTests() => this.AddTrailblazor();

    [Fact]
    public async Task Click_ShouldChooseTheItem_WhenItsOptionIsClicked()
    {
        Trail? chosen = null;
        var cut = Render<TbSelect<Trail>>(parameters => parameters.Add(select => select.Label, "Trail")
            .Add(select => select.Items, Trails).Add(select => select.ItemText, trail => trail.Name)
            .Add(select => select.ValueChanged, (Trail trail) => chosen = trail));

        await cut.FindAll("[role=option]")[1].ClickAsync(new());

        Assert.Equal(Trails[1], chosen);
        Assert.Equal(["Ridge loop", "Lake shore", "Cedar flats"],
            cut.FindAll("[role=option]").Select(option => option.GetAttribute("data-text")));
    }

    [Fact]
    public async Task Click_ShouldLeaveTheValue_WhenTheItemIsDisabled()
    {
        var chosen = 0;
        var cut = Render<TbSelect<Trail>>(parameters => parameters.Add(select => select.Label, "Trail")
            .Add(select => select.Items, Trails).Add(select => select.ItemText, trail => trail.Name)
            .Add(select => select.ItemDisabled, trail => trail.Id == 2)
            .Add(select => select.ValueChanged, (Trail _) => chosen++));

        await cut.FindAll("[role=option]")[1].ClickAsync(new());

        Assert.Equal(0, chosen);
        Assert.Equal("true", cut.FindAll("[role=option]")[1].GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Render_ShouldShowTheTemplateAndNameItWithItsText_WhenGivenAnItemTemplate()
    {
        var cut = Render<TbSelect<Trail>>(parameters => parameters.Add(select => select.Label, "Trail")
            .Add(select => select.Items, Trails).Add(select => select.ItemText, trail => trail.Name)
            .Add(select => select.ItemTemplate, trail => builder => builder.AddMarkupContent(0,
                $"<strong>{trail.Name}</strong> ({trail.Id})"))
            .Add(select => select.Value, Trails[2]));

        var option = cut.FindAll("[role=option]")[2];
        Assert.Equal("Cedar flats", option.GetAttribute("aria-label"));
        Assert.Equal("Cedar flats", option.QuerySelector("strong")!.TextContent);
        Assert.Contains("Cedar flats", cut.Find("[role=combobox]").TextContent);
    }

    [Fact]
    public async Task SelectAll_ShouldChooseEveryEnabledItem_WhenTheMultiSelectHasItems()
    {
        IReadOnlyList<Trail> chosen = [];
        var cut = Render<TbMultiSelect<Trail>>(parameters => parameters.Add(select => select.Label, "Trails")
            .Add(select => select.Items, Trails).Add(select => select.ItemText, trail => trail.Name)
            .Add(select => select.ItemDisabled, trail => trail.Id == 3).Add(select => select.SelectAll, true)
            .Add(select => select.ValueChanged, (IReadOnlyList<Trail> trails) => chosen = trails));

        await cut.Find("[data-part=select-all]").ClickAsync(new());

        Assert.Equal(Trails[..2], chosen);
    }

    [Fact]
    public void Render_ShouldListTheItemsAsNativeOptions_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<TbSelect<Trail>>(parameters => parameters.Add(select => select.Label, "Trail")
            .Add(select => select.Items, Trails).Add(select => select.ItemText, trail => trail.Name)
            .Add(select => select.Value, Trails[1]));

        Assert.Equal(["Ridge loop", "Lake shore", "Cedar flats"],
            cut.FindAll("option:not([value=''])").Select(option => option.TextContent));
        Assert.True(cut.FindAll("option")[^2].HasAttribute("selected"));
    }

    [Fact]
    public async Task RemoveChip_ShouldTakeOutTheItem_WhenItsChipIsRemoved()
    {
        IReadOnlyList<Trail> chosen = [Trails[0], Trails[1]];
        var cut = Render<TbMultiSelect<Trail>>(parameters => parameters.Add(select => select.Label, "Trails")
            .Add(select => select.Items, Trails).Add(select => select.ItemText, trail => trail.Name)
            .Add(select => select.Chips, true).Add(select => select.Value, chosen)
            .Add(select => select.ValueChanged, (IReadOnlyList<Trail> trails) => chosen = trails));
        var chips = cut.FindAll("[data-part=chip]");
        Assert.Equal(["Ridge loop", "Lake shore"], chips.Select(chip => chip.TextContent.Trim()));
        var toggle = ((DotNetObjectReference<SelectToggle>)JSInterop.Invocations["createSelect"].Single().Arguments[2]!)
            .Value;

        await cut.InvokeAsync(() => toggle.OnRemoveChip(chips[0].GetAttribute("data-option")!, "Ridge loop"));

        Assert.Equal([Trails[1]], chosen);
    }

    [Fact]
    public void Render_ShouldShowItemsAddedToTheList_WhenItIsFilledInPlace()
    {
        var trails = new List<Trail>();
        var cut = Render<TbSelect<Trail>>(parameters => parameters.Add(select => select.Label, "Trail")
            .Add(select => select.Items, trails).Add(select => select.ItemText, trail => trail.Name));

        trails.AddRange(Trails);
        cut.Render();

        Assert.Equal(3, cut.FindAll("[role=option]").Count);
    }

    [Fact]
    public void Render_ShouldVirtualizeTheOptions_WhenThereAreMoreThanAHundredItems()
    {
        var cut = RenderLong(count: 150, value: null);

        var listbox = cut.Find("[role=listbox]");
        Assert.Equal("150", listbox.GetAttribute("data-count"));
        var option = cut.Find("[role=option][data-index='3']");
        Assert.Equal(("Trail 4", "4", "150"), (option.GetAttribute("data-text"), option.GetAttribute("aria-posinset"),
            option.GetAttribute("aria-setsize")));
        Assert.False(RenderLong(count: 100, value: null).Find("[role=listbox]").HasAttribute("data-count"));
    }

    [Fact]
    public void Render_ShouldGiveTheChosenOptionsPlace_WhenTheListIsVirtualized()
    {
        var cut = RenderLong(count: 150, value: 120);

        Assert.Equal("119", cut.Find("[role=listbox]").GetAttribute("data-chosen"));
    }

    [Fact]
    public async Task Filter_ShouldShowOnlyMatchingItems_WhenTheVirtualizedListIsFiltered()
    {
        var cut = RenderLong(count: 150, value: null);

        await cut.InvokeAsync(() => Toggle().OnFilter("TRAIL 14"));

        Assert.Equal("11", cut.Find("[role=listbox]").GetAttribute("data-count"));
        Assert.All(cut.FindAll("[role=option][data-index]"),
            option => Assert.StartsWith("Trail 14", option.GetAttribute("data-text")));
        await cut.InvokeAsync(() => Toggle().OnFilter(""));
        Assert.Equal("150", cut.Find("[role=listbox]").GetAttribute("data-count"));
    }

    [Theory]
    [InlineData(-1, "trail 9", 8)]
    [InlineData(8, "t", 9)]
    [InlineData(-1, "nowhere", -1)]
    [InlineData(-1, "trail 2", 19)]
    public async Task Find_ShouldGiveThePlaceOfTheNextEnabledMatch_WhenGivenTypedText(int from, string typed, int found)
    {
        var cut = RenderLong(count: 150, value: null);

        Assert.Equal(found, await cut.InvokeAsync(() => Toggle().OnFind(from, typed)));
    }

    // Trail 2 is disabled, so typing "trail 2" finds Trail 20.
    private IRenderedComponent<TbSelect<Trail>> RenderLong(int count, int? value) =>
        Render<TbSelect<Trail>>(parameters => parameters.Add(select => select.Label, "Trail")
            .Add(select => select.Filterable, true)
            .Add(select => select.Items, Enumerable.Range(1, count).Select(id => new Trail(id, $"Trail {id}")).ToList())
            .Add(select => select.ItemText, trail => trail.Name)
            .Add(select => select.ItemDisabled, trail => trail.Id == 2)
            .Add(select => select.Value, value is null ? null : new Trail(value.Value, $"Trail {value}")));

    private SelectToggle Toggle() =>
        ((DotNetObjectReference<SelectToggle>)JSInterop.Invocations["createSelect"].Last().Arguments[2]!).Value;

    private sealed record Trail(int Id, string Name);
}
