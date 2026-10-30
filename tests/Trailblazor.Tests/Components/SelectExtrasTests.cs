using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Trailblazor.Tests.Components;

// The comparer, option groups and chips that the selects and list boxes share.
public class SelectExtrasTests : BunitContext
{
    private static readonly Trail[] Trails = [new(1, "Pine Hollow"), new(2, "Eagle Ridge"), new(3, "Cedar Flats")];

    public SelectExtrasTests()
    {
        this.AddTrailblazor();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    // Reference equality, so a trail loaded again matches its option only through a comparer.
    private sealed class Trail(int id, string name)
    {
        public int Id { get; } = id;

        public string Name { get; } = name;

        public Trail Reloaded() => new(Id, Name);
    }

    private sealed class SameId : IEqualityComparer<Trail>
    {
        public bool Equals(Trail? first, Trail? second) => first?.Id == second?.Id;

        public int GetHashCode(Trail trail) => trail.Id;
    }

    [Fact]
    public void Comparer_ShouldMatchAValueLoadedAgain_WhenTheSelectHasOne()
    {
        var cut = Render<TbSelect<Trail>>(parameters => parameters.Add(select => select.Value, Trails[1].Reloaded())
            .Add(select => select.Comparer, new SameId())
            .Add(select => select.ChildContent, (RenderFragment)AddTrails));

        Assert.Equal("true", cut.FindAll("[role=option]")[1].GetAttribute("aria-selected"));
        Assert.Equal("Eagle Ridge", cut.Find("[data-part=value]").TextContent.Trim());
    }

    [Fact]
    public void Comparer_ShouldTakeOutAValueLoadedAgain_WhenItsOptionIsClickedInAMultiSelect()
    {
        IReadOnlyList<Trail> chosen = [Trails[0].Reloaded()];
        var cut = Render<TbMultiSelect<Trail>>(parameters => parameters.Add(select => select.Value, chosen)
            .Add(select => select.Comparer, new SameId())
            .Add(select => select.ValueChanged, (IReadOnlyList<Trail> value) => chosen = value)
            .Add(select => select.ChildContent, (RenderFragment)AddTrails));

        cut.FindAll("[role=option]")[0].Click();

        Assert.Empty(chosen);
    }

    [Fact]
    public void Comparer_ShouldMatchAValueLoadedAgain_WhenTheListBoxHasOne()
    {
        var cut = Render<TbMultiListBox<Trail>>(parameters => parameters.Add(list => list.Value, [Trails[2].Reloaded()])
            .Add(list => list.Comparer, new SameId()).Add(list => list.ChildContent, (RenderFragment)AddTrails));

        Assert.Equal("true", cut.FindAll("[role=option]")[2].GetAttribute("aria-selected"));
    }

    [Fact]
    public void Group_ShouldLabelItsOptions_WhenInteractive()
    {
        var cut = Render<TbSelect<Trail>>(parameters => parameters.Add(select => select.ChildContent,
            (RenderFragment)AddGroups));

        var group = cut.Find("[role=listbox] [role=group]");
        Assert.Equal("North", cut.Find($"#{group.GetAttribute("aria-labelledby")}").TextContent);
        Assert.Equal(2, group.QuerySelectorAll("[role=option]").Length);
    }

    [Fact]
    public void Group_ShouldBeANativeOptionGroup_WhenStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<TbSelect<Trail>>(parameters => parameters.Add(select => select.ChildContent,
            (RenderFragment)AddGroups));

        Assert.Equal(["North", "Valley"], cut.FindAll("select optgroup").Select(group => group.GetAttribute("label")));
    }

    [Fact]
    public async Task Chips_ShouldShowEachChoiceAndRemoveTheOneTheScriptReports_WhenOn()
    {
        var host = Render<AnnouncerHost>();
        IReadOnlyList<Trail> chosen = [Trails[0], Trails[2]];
        var cut = RenderChips(() => chosen, value => chosen = value);

        Assert.Equal(["Pine Hollow", "Cedar Flats"], cut.FindAll("[data-part=chip]").Select(chip => chip.TextContent));
        Assert.Equal(2, cut.FindAll("[data-part=chip-remove]").Count);
        await cut.InvokeAsync(() => Toggle().OnRemoveChip(OptionId(cut, 0), "Pine Hollow"));

        Assert.Equal([Trails[2]], chosen);
        host.WaitForAssertion(() => Assert.Contains("Pine Hollow removed", host.Markup));
    }

    [Fact]
    public async Task Chips_ShouldStayAndOfferNoRemoveButtons_WhenTheSelectIsDisabled()
    {
        IReadOnlyList<Trail> chosen = [Trails[0]];
        var cut = RenderChips(() => chosen, value => chosen = value, disabled: true);

        await cut.InvokeAsync(() => Toggle().OnRemoveChip(cut.FindAll("[role=option]")[0].Id!, "Pine Hollow"));

        Assert.Empty(cut.FindAll("[data-part=chip-remove]"));
        Assert.Equal([Trails[0]], chosen);
    }

    [Fact]
    public void Chips_ShouldOfferNoRemoveButton_WhenTheOptionIsDisabled()
    {
        IReadOnlyList<Trail> chosen = [Trails[0]];
        var cut = Render<TbMultiSelect<Trail>>(parameters => parameters.Add(select => select.Value, chosen)
            .Add(select => select.Chips, true).Add(select => select.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbOption<Trail>>(0);
                builder.AddAttribute(1, nameof(TbOption<Trail>.Value), Trails[0]);
                builder.AddAttribute(2, nameof(TbOption<Trail>.Text), Trails[0].Name);
                builder.AddAttribute(3, nameof(TbOption<Trail>.Disabled), true);
                builder.CloseComponent();
            })));

        Assert.Single(cut.FindAll("[data-part=chip]"));
        Assert.Empty(cut.FindAll("[data-part=chip-remove]"));
    }

    [Fact]
    public async Task Chips_ShouldRemoveOnlyTheNamedOption_WhenTwoRemovalsArriveBeforeTheyRender()
    {
        IReadOnlyList<Trail> chosen = [Trails[0], Trails[1], Trails[2]];
        var cut = RenderChips(() => chosen, value => chosen = value);
        var first = OptionId(cut, 0);
        var second = OptionId(cut, 1);

        await cut.InvokeAsync(async () =>
        {
            await Toggle().OnRemoveChip(first, "Pine Hollow");
            await Toggle().OnRemoveChip(first, "Pine Hollow");
            await Toggle().OnRemoveChip(second, "Eagle Ridge");
        });

        Assert.Equal([Trails[2]], chosen);
    }

    [Fact]
    public void Filter_ShouldPutAFilterBoxAboveTheListInOnePopup_WhenFilterable()
    {
        var cut = Render<TbSelect<Trail>>(parameters => parameters.Add(select => select.Filterable, true)
            .Add(select => select.FilterLabel, "Find a trail").Add(select => select.NoResultsText, "No trails")
            .Add(select => select.ChildContent, (RenderFragment)AddTrails));

        var popup = cut.Find("[popover]");
        var listbox = popup.QuerySelector("[role=listbox]")!;
        var filter = popup.QuerySelector("[data-part=filter]")!;
        Assert.Equal("combobox", filter.GetAttribute("role"));
        Assert.Equal(listbox.Id, filter.GetAttribute("aria-controls"));
        Assert.Equal("Find a trail", filter.GetAttribute("aria-label"));
        Assert.Equal("No trails", popup.QuerySelector("[role=status]")!.GetAttribute("data-text"));
        Assert.False(listbox.HasAttribute("popover"));
    }

    [Fact]
    public void Filter_ShouldLeaveTheListAsThePopup_WhenNotFilterable()
    {
        var cut = Render<TbMultiSelect<Trail>>(parameters => parameters.Add(select => select.ChildContent,
            (RenderFragment)AddTrails));

        Assert.Equal("auto", cut.Find("[role=listbox]").GetAttribute("popover"));
        Assert.Empty(cut.FindAll("[data-part=filter]"));
    }

    private static string OptionId(IRenderedComponent<TbMultiSelect<Trail>> cut, int chip) =>
        cut.FindAll("[data-part=chip]")[chip].GetAttribute("data-option")!;

    private SelectToggle Toggle() =>
        ((DotNetObjectReference<SelectToggle>)JSInterop.Invocations["createSelect"].Single().Arguments[2]!).Value;

    private IRenderedComponent<TbMultiSelect<Trail>> RenderChips(Func<IReadOnlyList<Trail>> value,
        Action<IReadOnlyList<Trail>> changed, bool disabled = false) =>
        Render<TbMultiSelect<Trail>>(parameters => parameters.Add(select => select.Value, value())
            .Add(select => select.Chips, true).Add(select => select.Label, "Trails")
            .Add(select => select.Disabled, disabled)
            .Add(select => select.ValueChanged, changed)
            .Add(select => select.ChildContent, (RenderFragment)AddTrails));

    private static void AddTrails(RenderTreeBuilder builder)
    {
        foreach (var trail in Trails)
        {
            builder.OpenComponent<TbOption<Trail>>(0);
            builder.AddAttribute(1, nameof(TbOption<Trail>.Value), trail);
            builder.AddAttribute(2, nameof(TbOption<Trail>.Text), trail.Name);
            builder.CloseComponent();
        }
    }

    private static void AddGroups(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TbOptionGroup>(0);
        builder.AddAttribute(1, nameof(TbOptionGroup.Label), "North");
        builder.AddAttribute(2, nameof(TbOptionGroup.ChildContent), (RenderFragment)(inner =>
        {
            AddOption(inner, Trails[0]);
            AddOption(inner, Trails[1]);
        }));
        builder.CloseComponent();
        builder.OpenComponent<TbOptionGroup>(3);
        builder.AddAttribute(4, nameof(TbOptionGroup.Label), "Valley");
        builder.AddAttribute(5, nameof(TbOptionGroup.ChildContent),
            (RenderFragment)(inner => AddOption(inner, Trails[2])));
        builder.CloseComponent();
    }

    private static void AddOption(RenderTreeBuilder builder, Trail trail)
    {
        builder.OpenComponent<TbOption<Trail>>(0);
        builder.AddAttribute(1, nameof(TbOption<Trail>.Value), trail);
        builder.AddAttribute(2, nameof(TbOption<Trail>.Text), trail.Name);
        builder.CloseComponent();
    }
}
