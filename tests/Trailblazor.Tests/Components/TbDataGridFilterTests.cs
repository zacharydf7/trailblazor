using System.Globalization;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Rendering;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbDataGridFilterTests : BunitContext
{
    public TbDataGridFilterTests()
    {
        this.AddTrailblazor();
    }

    public enum Difficulty
    {
        Easy,
        Moderate,
        Hard,
    }

    public sealed record Hike(int Id, string Name, double LengthKm, DateOnly Date, Difficulty Difficulty, bool Loop);

    private static IReadOnlyList<Hike> Hikes =>
    [
        new(1, "Pine Hollow", 6.4, new(2026, 5, 2), Difficulty.Easy, true),
        new(2, "Eagle Ridge", 14.2, new(2026, 6, 14), Difficulty.Hard, false),
        new(3, "Cedar Flats", 3.1, new(2026, 4, 20), Difficulty.Easy, false),
        new(4, "Ridge Loop", 8.5, new(2026, 8, 23), Difficulty.Moderate, true),
    ];

    [Fact]
    public void FilterRow_ShouldNameAFieldForEachFilterableColumn_WhenRendered()
    {
        var cut = RenderHikes(filterable: id => id != "Date");

        var labels = cut.FindAll("[data-part=filter] label").Select(label => label.TextContent.Trim());

        Assert.Equal(["Filter Name", "Filter Length km", "Filter Difficulty", "Filter Loop"], labels);
        Assert.Empty(cut.Find("[data-part=filter][data-column=Date]").Children);
    }

    [Fact]
    public void FilterRow_ShouldNotShow_WhenNoColumnIsFilterable()
    {
        var cut = RenderHikes(filterable: _ => false);

        Assert.Empty(cut.FindAll("[data-part=filters]"));
    }

    [Fact]
    public async Task TextFilter_ShouldShowRowsWhoseTextContainsItIgnoringCase_WhenTyped()
    {
        IReadOnlyList<GridFilter>? filters = null;
        var cut = RenderHikes(configure: parameters => parameters
            .Add(grid => grid.FiltersChanged, (IReadOnlyList<GridFilter> changed) => filters = changed));

        await cut.Tb().Grid("Hikes").FilterAsync("Name", "RIDGE");

        Assert.Equal(["Eagle Ridge", "Ridge Loop"], Names(cut));
        Assert.Equal([new GridFilter("Name", FilterOperator.Contains, "RIDGE")], filters);
        Assert.Equal("", cut.Find("th[data-column=Name]").GetAttribute("data-filtered"));
    }

    [Fact]
    public async Task NumberFilter_ShouldShowRowsWithThatNumber_WhenEntered()
    {
        var cut = RenderHikes();

        await cut.Tb().Grid("Hikes").FilterAsync("Length km", "8.5");

        Assert.Equal(["Ridge Loop"], Names(cut));
    }

    [Fact]
    public async Task ChoiceFilter_ShouldShowRowsWithTheChosenEnumValue_WhenChosen()
    {
        var cut = RenderHikes();

        await cut.Tb().Select("Filter Difficulty").ChooseAsync("Easy");

        Assert.Equal(["Pine Hollow", "Cedar Flats"], Names(cut));
    }

    [Fact]
    public async Task ChoiceFilter_ShouldShowRowsThatAreNot_WhenNoIsChosenForAYesOrNoColumn()
    {
        IReadOnlyList<GridFilter>? filters = null;
        var cut = RenderHikes(configure: parameters => parameters
            .Add(grid => grid.FiltersChanged, (IReadOnlyList<GridFilter> changed) => filters = changed));

        await cut.Tb().Select("Filter Loop").ChooseAsync("No");

        Assert.Equal(["Eagle Ridge", "Cedar Flats"], Names(cut));
        Assert.Equal([new GridFilter("Loop", FilterOperator.EqualTo, false)], filters);
    }

    [Fact]
    public void DateFilter_ShouldShowRowsInTheRangeWithAFilterForEachEnd_WhenBothEndsAreTyped()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        try
        {
            IReadOnlyList<GridFilter>? filters = null;
            var cut = RenderHikes(configure: parameters => parameters
                .Add(grid => grid.FiltersChanged, (IReadOnlyList<GridFilter> changed) => filters = changed));

            cut.Find("[data-part=filter][data-column=Date] [data-part=start-input]").Change("5/1/2026");
            cut.Find("[data-part=filter][data-column=Date] [data-part=end-input]").Change("6/30/2026");

            Assert.Equal(["Pine Hollow", "Eagle Ridge"], Names(cut));
            Assert.Equal([new GridFilter("Date", FilterOperator.GreaterThanOrEqualTo, new DateOnly(2026, 5, 1)),
                new GridFilter("Date", FilterOperator.LessThanOrEqualTo, new DateOnly(2026, 6, 30))], filters);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void Filters_ShouldShowOnlyRowsInTheRangeAndFillTheFields_WhenSetByTheParent()
    {
        var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.Filters,
        [
            new GridFilter("Date", FilterOperator.GreaterThanOrEqualTo, new DateOnly(2026, 5, 1)),
            new GridFilter("Date", FilterOperator.LessThanOrEqualTo, new DateOnly(2026, 6, 30)),
            new GridFilter("Name", FilterOperator.Contains, "e"),
        ]));

        Assert.Equal(["Pine Hollow", "Eagle Ridge"], Names(cut));
        Assert.Equal("e", cut.Find("[data-part=filter][data-column=Name] input").GetAttribute("value"));
        Assert.Equal("", cut.Find("th[data-column=Date]").GetAttribute("data-filtered"));
    }

    [Fact]
    public async Task TextFilter_ShouldMatchTheFilterByValue_WhenTheColumnHasOne()
    {
        var cut = RenderHikes(columns: builder =>
        {
            builder.OpenComponent<TbColumn<Hike>>(0);
            builder.AddAttribute(1, nameof(TbColumn<Hike>.Field),
                (Expression<Func<Hike, object?>>)(hike => hike.Name));
            builder.AddAttribute(2, nameof(TbColumn<Hike>.Filterable), true);
            builder.AddAttribute(3, nameof(TbColumn<Hike>.FilterBy),
                (Func<Hike, object?>)(hike => hike.Difficulty.ToString()));
            builder.CloseComponent();
        });

        await cut.Tb().Grid("Hikes").FilterAsync("Name", "easy");

        Assert.Equal(["Pine Hollow", "Cedar Flats"], Names(cut));
    }

    [Fact]
    public async Task TextFilter_ShouldFilter_WhenTheColumnHasOnlyAFilterByAndACell()
    {
        var cut = RenderHikes(columns: builder =>
        {
            builder.OpenComponent<TbColumn<Hike>>(0);
            builder.AddAttribute(1, nameof(TbColumn<Hike>.Title), "Trail");
            builder.AddAttribute(2, nameof(TbColumn<Hike>.Filterable), true);
            builder.AddAttribute(3, nameof(TbColumn<Hike>.FilterBy), (Func<Hike, object?>)(hike => hike.Name));
            builder.AddAttribute(4, nameof(TbColumn<Hike>.CellTemplate),
                (RenderFragment<Hike>)(hike => cell => cell.AddContent(0, hike.Name)));
            builder.CloseComponent();
        });

        await cut.Tb().Grid("Hikes").FilterAsync("Trail", "ridge");

        Assert.Equal(["Eagle Ridge", "Ridge Loop"],
            cut.FindAll("tr[data-part=row] td[data-column=Trail]").Select(cell => cell.TextContent));
    }

    [Fact]
    public async Task Reorder_ShouldNotBeOffered_WhenAFilterHidesRows()
    {
        var cut = RenderHikes(configure: parameters => parameters
            .Add(grid => grid.OnRowMove, (SortableMove<Hike> _) => { }));
        Assert.NotEmpty(cut.FindAll("[data-part=order]"));

        await cut.Tb().Grid("Hikes").FilterAsync("Name", "ridge");

        Assert.Empty(cut.FindAll("[data-part=order]"));
        Assert.Empty(cut.FindAll("tr[data-part=row] .tb-data-grid__handle"));
    }

    [Fact]
    public void FilterCell_ShouldHoldTheFilterAsInvariantText_WhenTheCultureWritesNumbersOtherwise()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.Filters,
                [new GridFilter("LengthKm", FilterOperator.EqualTo, 14.2m)]));

            Assert.Equal("14.2", cut.Find("[data-part=filter][data-column=LengthKm]").GetAttribute("data-value"));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Theory]
    [InlineData('x')]
    [InlineData(double.NaN)]
    [InlineData(1e30)]
    public void NumberFilter_ShouldStayEmpty_WhenTheParentsValueIsntADecimal(object value)
    {
        var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.Filters,
            [new GridFilter("LengthKm", FilterOperator.EqualTo, value)]));

        Assert.Equal("", cut.Find("[data-part=filter][data-column=LengthKm] input").GetAttribute("value") ?? "");
    }

    [Fact]
    public void Filters_ShouldHideNoRows_WhenTheyNameAColumnThatIsntThere()
    {
        var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.Filters,
            [new GridFilter("Elevation", FilterOperator.GreaterThan, 1000)]));

        Assert.Equal(4, Names(cut).Count);
    }

    [Fact]
    public async Task Filter_ShouldGoBackToTheFirstPageAndCountTheRowsAloud_WhenChanged()
    {
        var announcer = Render<AnnouncerHost>();
        var pages = new List<int>();
        var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.PageSize, 1)
            .Add(grid => grid.Page, 3).Add(grid => grid.PageChanged, (int page) => pages.Add(page)));

        await cut.Tb().Grid("Hikes").FilterAsync("Name", "ridge");

        Assert.Equal([1], pages);
        Assert.Equal(["Eagle Ridge"], Names(cut));
        announcer.WaitForAssertion(() =>
            Assert.Equal("Rows shown: 2.", announcer.Find("[role=status] p").TextContent));
    }

    [Fact]
    public async Task DataProvider_ShouldBeAskedForTheFilteredRowsFromTheFirstPage_WhenAFilterChanges()
    {
        var requests = new List<GridRequest>();
        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.AriaLabel, "Hikes")
            .Add(grid => grid.PageSize, 2).Add(grid => grid.Page, 2).Add(grid => grid.ChildContent, Columns(_ => true))
            .Add(grid => grid.DataProvider, (GridRequest request, CancellationToken _) =>
            {
                requests.Add(request);
                return Task.FromResult(new GridResult<Hike>([.. Hikes.Take(2)], Hikes.Count));
            }));

        await cut.Tb().Grid("Hikes").FilterAsync("Name", "ridge");

        var last = requests[^1];
        Assert.Equal(0, last.StartIndex);
        Assert.Equal([new GridFilter("Name", FilterOperator.Contains, "ridge")], last.Filters);
    }

    [Fact]
    public void Menu_ShouldPutANamedFilterButtonOnEachFilterableHeader_WhenTheFilterModeIsMenu()
    {
        var cut = RenderHikes(filterable: id => id != "Date",
            configure: parameters => parameters.Add(grid => grid.FilterMode, GridFilterMode.Menu));

        var buttons = cut.FindAll("th [data-part=filter-menu] > button")
            .Select(button => button.GetAttribute("aria-label"));

        Assert.Empty(cut.FindAll("[data-part=filters]"));
        Assert.Equal(["Filter Name", "Filter Length km", "Filter Difficulty", "Filter Loop"], buttons);
    }

    [Fact]
    public async Task Menu_ShouldFilterByTheChosenConditionAndValue_WhenApplied()
    {
        IReadOnlyList<GridFilter>? filters = null;
        var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.FilterMode, GridFilterMode.Menu)
            .Add(grid => grid.FiltersChanged, (IReadOnlyList<GridFilter> changed) => filters = changed));

        await cut.Tb().Grid("Hikes").FilterAsync("Length km", "At least", "8");

        Assert.Equal(["Eagle Ridge", "Ridge Loop"], Names(cut));
        Assert.Equal([new GridFilter("LengthKm", FilterOperator.GreaterThanOrEqualTo, 8m)], filters);
        Assert.Equal("Filter Length km, on",
            cut.Find("th[data-column=LengthKm] [data-part=filter-menu] > button").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task Menu_ShouldMatchTheTextAsTheConditionSays_WhenATextConditionIsChosen()
    {
        var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.FilterMode, GridFilterMode.Menu));

        await cut.Tb().Grid("Hikes").FilterAsync("Name", "Starts with", "ridge");

        Assert.Equal(["Ridge Loop"], Names(cut));
    }

    [Fact]
    public async Task Menu_ShouldShowRowsWithAnyCheckedValue_WhenAnEnumsValuesAreChecked()
    {
        IReadOnlyList<GridFilter>? filters = null;
        var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.FilterMode, GridFilterMode.Menu)
            .Add(grid => grid.FiltersChanged, (IReadOnlyList<GridFilter> changed) => filters = changed));

        await cut.Tb().Grid("Hikes").FilterChoicesAsync("Difficulty", "Easy", "Hard");

        Assert.Equal(["Pine Hollow", "Eagle Ridge", "Cedar Flats"], Names(cut));
        var filter = Assert.Single(filters!);
        Assert.Equal(FilterOperator.IsAnyOf, filter.Operator);
        Assert.Equal([Difficulty.Easy, Difficulty.Hard], Assert.IsAssignableFrom<IEnumerable<object>>(filter.Value));
    }

    [Fact]
    public async Task Menu_ShouldClearTheColumnsFilter_WhenClearIsClicked()
    {
        var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.FilterMode, GridFilterMode.Menu)
            .Add(grid => grid.Filters, [new GridFilter("Name", FilterOperator.Contains, "ridge")]));
        Assert.Equal(2, Names(cut).Count);

        await cut.Find("th[data-column=Name] [data-part=filter-clear]").ClickAsync(new());

        Assert.Equal(4, Names(cut).Count);
        Assert.Null(cut.Find("th[data-column=Name]").GetAttribute("data-filtered"));
    }

    [Fact]
    public void Menu_ShouldStartFromTheColumnsFilter_WhenTheParentSetsIt()
    {
        var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.FilterMode, GridFilterMode.Menu)
            .Add(grid => grid.Filters, [new GridFilter("LengthKm", FilterOperator.GreaterThanOrEqualTo, 8m)]));

        var menu = cut.Find("th[data-column=LengthKm] [data-part=filter-menu]");

        Assert.Equal("At least", menu.QuerySelector("[role=combobox] [data-part=value]")!.TextContent.Trim());
        Assert.Equal("8", menu.QuerySelector("input[data-part=input]")!.GetAttribute("value"));
    }

    [Fact]
    public async Task FilterChoices_ShouldBeAMultipleSelectInTheFilterRow_WhenAColumnHasThem()
    {
        IReadOnlyList<GridFilter>? filters = null;
        var cut = RenderHikes(configure: parameters => parameters
                .Add(grid => grid.FiltersChanged, (IReadOnlyList<GridFilter> changed) => filters = changed),
            columns: builder =>
            {
                builder.OpenComponent<TbColumn<Hike>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Hike>.Field),
                    (Expression<Func<Hike, object?>>)(hike => hike.Name));
                builder.AddAttribute(2, nameof(TbColumn<Hike>.Filterable), true);
                builder.AddAttribute(3, nameof(TbColumn<Hike>.FilterChoices),
                    (IEnumerable<object?>)["Pine Hollow", "Ridge Loop"]);
                builder.CloseComponent();
            });

        await cut.Tb().Grid("Hikes").FilterChoicesAsync("Name", "Ridge Loop", "Pine Hollow");

        Assert.Equal(["Pine Hollow", "Ridge Loop"], Names(cut));
        Assert.Equal(FilterOperator.IsAnyOf, Assert.Single(filters!).Operator);
    }

    [Theory]
    [InlineData(GridFilterMode.Row)]
    [InlineData(GridFilterMode.Menu)]
    public async Task FilterTemplate_ShouldDrawTheFilterAndApplyWhatItSets_WhenAColumnHasOne(GridFilterMode mode)
    {
        var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.FilterMode, mode),
            columns: builder =>
            {
                builder.OpenComponent<TbColumn<Hike>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Hike>.Field),
                    (Expression<Func<Hike, object?>>)(hike => hike.LengthKm));
                builder.AddAttribute(2, nameof(TbColumn<Hike>.Filterable), true);
                builder.AddAttribute(3, nameof(TbColumn<Hike>.FilterTemplate), (RenderFragment<GridFilterContext>)(
                    context => button =>
                    {
                        button.OpenElement(0, "button");
                        button.AddAttribute(1, "data-testid", "long");
                        button.AddAttribute(2, "onclick", EventCallback.Factory.Create(this,
                            () => context.ApplyAsync(new GridFilter(context.Column, FilterOperator.GreaterThan, 10))));
                        button.AddContent(3, "Long hikes");
                        button.CloseElement();
                    }));
                builder.CloseComponent();
            });

        await cut.Find("[data-testid=long]").ClickAsync(new());

        Assert.Equal(["14.2"], cut.FindAll("tr[data-part=row] td").Select(cell => cell.TextContent));
    }

    [Fact]
    public async Task Menu_ShouldAskTheProviderAgain_WhenTheCheckedValuesChangeButNotWhenTheyDont()
    {
        var requests = new List<GridRequest>();
        var changes = 0;
        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.AriaLabel, "Hikes")
            .Add(grid => grid.FilterMode, GridFilterMode.Menu).Add(grid => grid.ChildContent, Columns(_ => true))
            .Add(grid => grid.FiltersChanged, (IReadOnlyList<GridFilter> _) => changes++)
            .Add(grid => grid.DataProvider, (GridRequest request, CancellationToken _) =>
            {
                requests.Add(request);
                return Task.FromResult(new GridResult<Hike>([.. Hikes], Hikes.Count));
            }));
        var hikes = cut.Tb().Grid("Hikes");

        await hikes.FilterChoicesAsync("Difficulty", "Easy");
        await hikes.FilterChoicesAsync("Difficulty", "Easy", "Hard");
        await hikes.FilterChoicesAsync("Difficulty", "Easy", "Hard");

        Assert.Equal(3, requests.Count);
        Assert.Equal(2, changes);
        Assert.Equal([new GridFilter("Difficulty", FilterOperator.IsAnyOf, new List<object> { Difficulty.Easy,
            Difficulty.Hard })], requests[^1].Filters);
    }

    [Fact]
    public async Task Menu_ShouldStartAgainFromTheDefaultCondition_WhenItsFilterWasCleared()
    {
        IReadOnlyList<GridFilter>? filters = null;
        var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.FilterMode, GridFilterMode.Menu)
            .Add(grid => grid.FiltersChanged, (IReadOnlyList<GridFilter> changed) => filters = changed));
        var hikes = cut.Tb().Grid("Hikes");
        await hikes.FilterAsync("Length km", "At least", "8");
        await cut.Find("th[data-column=LengthKm] [data-part=filter-clear]").ClickAsync(new());

        await hikes.FilterAsync("Length km", "14.2");

        Assert.Equal([new GridFilter("LengthKm", FilterOperator.EqualTo, 14.2m)], filters);
    }

    [Fact]
    public void Menu_ShouldShowTheParentsNewFilter_WhenTheParentChangesItAfterwards()
    {
        var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.FilterMode, GridFilterMode.Menu));

        cut.Render(parameters => parameters.Add(grid => grid.Filters,
            [new GridFilter("LengthKm", FilterOperator.LessThan, 5m)]));

        var menu = cut.Find("th[data-column=LengthKm] [data-part=filter-menu]");
        Assert.Equal("Less than", menu.QuerySelector("[role=combobox] [data-part=value]")!.TextContent.Trim());
        Assert.Equal("5", menu.QuerySelector("input[data-part=input]")!.GetAttribute("value"));
    }

    [Theory]
    [InlineData(GridFilterMode.Row)]
    [InlineData(GridFilterMode.Menu)]
    public async Task Filters_ShouldLeaveThePagesFormUnmodified_WhenTheGridIsInsideIt(GridFilterMode mode)
    {
        var form = new EditContext(new object());
        var cut = Render<CascadingValue<EditContext>>(parameters => parameters.Add(value => value.Value, form)
            .Add(value => value.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbDataGrid<Hike>>(0);
                builder.AddComponentParameter(1, nameof(TbDataGrid<Hike>.Items), Hikes);
                builder.AddComponentParameter(2, nameof(TbDataGrid<Hike>.AriaLabel), "Hikes");
                builder.AddComponentParameter(3, nameof(TbDataGrid<Hike>.FilterMode), mode);
                builder.AddComponentParameter(4, nameof(TbDataGrid<Hike>.ChildContent), Columns(_ => true));
                builder.CloseComponent();
            })));

        await cut.Tb().Grid("Hikes").FilterAsync("Name", "ridge");

        Assert.Equal(2, cut.FindAll("tr[data-part=row]").Count);
        Assert.False(form.IsModified());
        Assert.Empty(cut.FindAll("form"));
    }

    [Theory]
    [InlineData(GridFilterMode.Row)]
    [InlineData(GridFilterMode.Menu)]
    public async Task Filters_ShouldLeaveThePagesTbFormValid_WhenAFilterHoldsTextThatIsntANumber(GridFilterMode mode)
    {
        var valid = new List<bool>();
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.Model, new object())
            .Add(form => form.IsValidChanged, (bool isValid) => valid.Add(isValid))
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbDataGrid<Hike>>(0);
                builder.AddComponentParameter(1, nameof(TbDataGrid<Hike>.Items), Hikes);
                builder.AddComponentParameter(2, nameof(TbDataGrid<Hike>.AriaLabel), "Hikes");
                builder.AddComponentParameter(3, nameof(TbDataGrid<Hike>.FilterMode), mode);
                builder.AddComponentParameter(4, nameof(TbDataGrid<Hike>.ChildContent), Columns(_ => true));
                builder.CloseComponent();
            })));
        var filter = mode == GridFilterMode.Row ? "[data-part=filter][data-column=LengthKm] input"
            : "th[data-column=LengthKm] [data-part=filter-menu] input[data-part=input]:not([role=combobox])";

        await cut.Find(filter).TriggerEventAsync("ontbnumberchange", new NumberChangeEventArgs { Value = "1.2.3" });
        await cut.Find("form").SubmitAsync();

        Assert.DoesNotContain(false, valid);
    }

    [Fact]
    public async Task Menu_ShouldKeepATemplatesFieldOutOfThePagesTbForm_WhenItHoldsTextThatIsntANumber()
    {
        var valid = new List<bool>();
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.Model, new object())
            .Add(form => form.IsValidChanged, (bool isValid) => valid.Add(isValid))
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbDataGrid<Hike>>(0);
                builder.AddComponentParameter(1, nameof(TbDataGrid<Hike>.Items), Hikes);
                builder.AddComponentParameter(2, nameof(TbDataGrid<Hike>.AriaLabel), "Hikes");
                builder.AddComponentParameter(3, nameof(TbDataGrid<Hike>.FilterMode), GridFilterMode.Menu);
                builder.AddComponentParameter(4, nameof(TbDataGrid<Hike>.ChildContent), (RenderFragment)(columns =>
                {
                    columns.OpenComponent<TbColumn<Hike>>(0);
                    columns.AddAttribute(1, nameof(TbColumn<Hike>.Field),
                        (Expression<Func<Hike, object?>>)(hike => hike.LengthKm));
                    columns.AddAttribute(2, nameof(TbColumn<Hike>.Filterable), true);
                    columns.AddAttribute(3, nameof(TbColumn<Hike>.FilterTemplate),
                        (RenderFragment<GridFilterContext>)(_ => field =>
                        {
                            field.OpenComponent<TbNumberField<double?>>(0);
                            field.AddComponentParameter(1, nameof(TbNumberField<double?>.Label), "Longer than");
                            field.CloseComponent();
                        }));
                    columns.CloseComponent();
                }));
                builder.CloseComponent();
            })));

        await cut.Tb().NumberField("Longer than").EnterAsync("1.2.3");
        await cut.Find("form").SubmitAsync();

        Assert.DoesNotContain(false, valid);
    }

    [Theory]
    [InlineData(true, 4)]
    [InlineData(false, 2)]
    public async Task Menu_ShouldApplyOnEnterOnlyOutsideAComposition_WhenGivenAKeyPress(bool composing, int rows)
    {
        var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.FilterMode, GridFilterMode.Menu));
        const string value = "th[data-column=Name] [data-part=filter-menu] input[data-part=input]:not([role=combobox])";
        await cut.Find(value).InputAsync(new ChangeEventArgs { Value = "ridge" });

        await cut.Find(value).KeyDownAsync(new KeyboardEventArgs { Key = "Enter", IsComposing = composing });

        Assert.Equal(rows, Names(cut).Count);
    }

    [Fact]
    public async Task FilterChoicesAsync_ShouldThrow_WhenAMenuHasNoChoiceWithThatName()
    {
        var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.FilterMode, GridFilterMode.Menu));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Grid("Hikes").FilterChoicesAsync("Difficulty", "Eazy"));

        Assert.Equal("The \"Difficulty\" column's filter has no choice \"Eazy\".", error.Message);
        Assert.Equal(4, Names(cut).Count);
    }

    [Fact]
    public async Task FilterTemplate_ShouldThrow_WhenItAppliesAFilterForAnotherColumn()
    {
        var cut = RenderHikes(columns: builder =>
        {
            builder.OpenComponent<TbColumn<Hike>>(0);
            builder.AddAttribute(1, nameof(TbColumn<Hike>.Field), (Expression<Func<Hike, object?>>)(hike => hike.Name));
            builder.AddAttribute(2, nameof(TbColumn<Hike>.Filterable), true);
            builder.AddAttribute(3, nameof(TbColumn<Hike>.FilterTemplate), (RenderFragment<GridFilterContext>)(
                context => button =>
                {
                    button.OpenElement(0, "button");
                    button.AddAttribute(1, "onclick", EventCallback.Factory.Create(this,
                        () => context.ApplyAsync(new GridFilter("Title", FilterOperator.Contains, "x"))));
                    button.CloseElement();
                }));
            builder.CloseComponent();
        });

        var error = await Assert.ThrowsAsync<ArgumentException>(() => cut.Find("button").ClickAsync(new()));

        Assert.Contains("\"Name\"", error.Message);
    }

    [Fact]
    public async Task FilterChoices_ShouldLeaveExactlyTheGivenChoices_WhenSomeWereChosenAlready()
    {
        var cut = RenderHikes(columns: builder =>
        {
            builder.OpenComponent<TbColumn<Hike>>(0);
            builder.AddAttribute(1, nameof(TbColumn<Hike>.Field), (Expression<Func<Hike, object?>>)(hike => hike.Name));
            builder.AddAttribute(2, nameof(TbColumn<Hike>.Filterable), true);
            builder.AddAttribute(3, nameof(TbColumn<Hike>.FilterChoices),
                (IEnumerable<object?>)["Pine Hollow", "Ridge Loop", "Cedar Flats"]);
            builder.CloseComponent();
        });
        var hikes = cut.Tb().Grid("Hikes");
        await hikes.FilterChoicesAsync("Name", "Pine Hollow", "Ridge Loop");

        await hikes.FilterChoicesAsync("Name", "Cedar Flats");

        Assert.Equal(["Cedar Flats"], Names(cut));
    }

    [Fact]
    public async Task FilterAsync_ShouldSayTheFilterHasNoConditions_WhenItsColumnIsAList()
    {
        var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.FilterMode, GridFilterMode.Menu));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Grid("Hikes").FilterAsync("Difficulty", "Is", "Easy"));

        Assert.Contains("has no conditions", error.Message);
    }

    private static List<string> Names(IRenderedComponent<TbDataGrid<Hike>> cut) =>
        [.. cut.FindAll("tr[data-part=row] td[data-column=Name]").Select(cell => cell.TextContent)];

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(Func<string, bool>? filterable = null,
        Action<ComponentParameterCollectionBuilder<TbDataGrid<Hike>>>? configure = null,
        RenderFragment? columns = null) =>
        Render<TbDataGrid<Hike>>(parameters =>
        {
            parameters.Add(grid => grid.Items, Hikes).Add(grid => grid.AriaLabel, "Hikes")
                .Add(grid => grid.KeySelector, hike => hike.Id)
                .Add(grid => grid.ChildContent, columns ?? Columns(filterable ?? (_ => true)));
            configure?.Invoke(parameters);
        });

    // A column for each of a hike's values, filterable when the test says so for its id.
    private static RenderFragment Columns(Func<string, bool> filterable) => builder =>
    {
        Column(builder, hike => hike.Name, "Name", filterable);
        Column(builder, hike => hike.LengthKm, "LengthKm", filterable);
        Column(builder, hike => hike.Date, "Date", filterable);
        Column(builder, hike => hike.Difficulty, "Difficulty", filterable);
        Column(builder, hike => hike.Loop, "Loop", filterable);
    };

    private static void Column(RenderTreeBuilder builder, Expression<Func<Hike, object?>> field, string id,
        Func<string, bool> filterable)
    {
        builder.OpenComponent<TbColumn<Hike>>(0);
        builder.SetKey(id);
        builder.AddAttribute(1, nameof(TbColumn<Hike>.Field), field);
        builder.AddAttribute(2, nameof(TbColumn<Hike>.Filterable), filterable(id));
        builder.CloseComponent();
    }
}
