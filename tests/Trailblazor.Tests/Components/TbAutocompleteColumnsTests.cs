using Bunit;
using Microsoft.AspNetCore.Components;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbAutocompleteColumnsTests : BunitContext
{
    private static readonly Trail[] Trails =
        [new("Ridge loop", 7, "North"), new("River walk", 3, "South"), new("Summit trail", 11, "North")];

    private static readonly AutocompleteColumn<Trail>[] Columns =
    [
        new("Trail", trail => trail.Name),
        new("Length", trail => $"{trail.Miles} mi") { Width = "5rem" },
        new("Region", trail => trail.Region),
    ];

    public TbAutocompleteColumnsTests() => this.AddTrailblazor();

    [Fact]
    public void Columns_ShouldShowEachSuggestionAsCellsUnderHiddenTitles_WhenGiven()
    {
        var cut = RenderTrails();

        cut.Find("input").Input("r");

        var titles = cut.Find("[data-part=columns]");
        Assert.Equal("true", titles.GetAttribute("aria-hidden"));
        Assert.Equal(["Trail", "Length", "Region"],
            titles.QuerySelectorAll("[data-part=column-title]").Select(title => title.TextContent));
        Assert.Equal(["Ridge loop", "7 mi", "North"],
            cut.FindAll("[role=option]")[0].QuerySelectorAll("[data-part=cell]").Select(cell => cell.TextContent));
        Assert.Equal("--_tb-autocomplete-columns: minmax(0, 1fr) 5rem minmax(0, 1fr)", titles.GetAttribute("style"));
    }

    [Fact]
    public void Columns_ShouldNameEachSuggestionByItsTitlesAndValues_WhenGiven()
    {
        var cut = RenderTrails();

        cut.Find("input").Input("r");

        Assert.Equal(["Trail: Ridge loop, Length: 7 mi, Region: North",
            "Trail: River walk, Length: 3 mi, Region: South", "Trail: Summit trail, Length: 11 mi, Region: North"],
            cut.Tb().Autocomplete("Trail").Suggestions);
    }

    [Fact]
    public async Task Choose_ShouldPutTheSuggestionsTextInTheField_WhenASuggestionWithColumnsIsChosen()
    {
        Trail? chosen = null;
        var cut = RenderTrails(parameters => parameters.Add(field => field.ValueChanged,
            (Trail trail) => chosen = trail));

        await cut.Tb().Autocomplete("Trail").ChooseAsync("riv", "Trail: River walk, Length: 3 mi, Region: South");

        Assert.Equal(Trails[1], chosen);
        Assert.Equal("River walk", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void ItemTemplate_ShouldDrawTheSuggestionsInsteadOfColumns_WhenBothAreGiven()
    {
        var cut = RenderTrails(parameters => parameters.Add(field => field.ItemTemplate,
            trail => builder => builder.AddContent(0, $"{trail.Name} ({trail.Region})")));

        cut.Find("input").Input("ridge");

        Assert.Empty(cut.FindAll("[data-part=columns]"));
        Assert.Equal(("Ridge loop (North)", false), (cut.Find("[role=option]").TextContent.Trim(),
            cut.Find("[role=option]").HasAttribute("aria-label")));
    }

    private IRenderedComponent<TbAutocomplete<Trail>> RenderTrails(
        Action<ComponentParameterCollectionBuilder<TbAutocomplete<Trail>>>? configure = null) =>
        Render<TbAutocomplete<Trail>>(parameters =>
        {
            parameters.Add(field => field.Label, "Trail").Add(field => field.Items, Trails)
                .Add(field => field.ItemText, trail => trail.Name).Add(field => field.Columns, Columns);
            configure?.Invoke(parameters);
        });

    private sealed record Trail(string Name, int Miles, string Region);
}
