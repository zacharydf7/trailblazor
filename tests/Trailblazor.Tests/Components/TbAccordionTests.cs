using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor.Tests.Components;

public class TbAccordionTests : BunitContext
{
    public TbAccordionTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldLinkEachHeaderButtonToItsHiddenRegion_WhenCollapsed()
    {
        var cut = RenderAccordion();

        var heading = cut.FindAll("[data-part=heading]")[0];
        Assert.Equal("h3", heading.LocalName);
        var trigger = heading.QuerySelector("button")!;
        Assert.Equal("Shipping options", trigger.TextContent.Trim());
        Assert.Equal("false", trigger.GetAttribute("aria-expanded"));
        var panel = cut.Find($"#{trigger.GetAttribute("aria-controls")}");
        Assert.Equal("region", panel.GetAttribute("role"));
        Assert.Equal(trigger.Id, panel.GetAttribute("aria-labelledby"));
        Assert.True(panel.HasAttribute("inert"));
        Assert.Equal("Shipping content", panel.TextContent.Trim());
    }

    [Fact]
    public void Click_ShouldOpenTheItemAndCloseTheOpenOne_WhenOnlyOneMayBeOpen()
    {
        var changes = new List<string>();
        var cut = RenderAccordion(onChanged: (title, expanded) => changes.Add($"{title} {expanded}"),
            firstExpanded: true);

        cut.FindAll("[data-part=trigger]")[1].Click();

        Assert.Equal(["Return policy True", "Shipping options False"], changes);
        Assert.Equal(["false", "true"], cut.FindAll("[data-part=trigger]").Select(t => t.GetAttribute("aria-expanded")));
        Assert.True(cut.FindAll("[data-part=panel]")[0].HasAttribute("inert"));
        Assert.Equal([false, true], cut.FindAll("[data-part=item]")
            .Select(item => item.ClassList.Contains("tb-accordion__item--expanded")));
    }

    [Fact]
    public void Render_ShouldLeaveItemsAsTheParentSetsThem_WhenOnlyOneMayBeOpenFromItsHeader()
    {
        var cut = RenderAccordion(firstExpanded: true, secondExpanded: true);

        Assert.Equal(["true", "true"], cut.FindAll("[data-part=trigger]").Select(t => t.GetAttribute("aria-expanded")));
    }

    [Fact]
    public void Click_ShouldLeaveOtherItemsOpen_WhenMultipleIsSet()
    {
        var cut = RenderAccordion(multiple: true, firstExpanded: true);

        cut.FindAll("[data-part=trigger]")[1].Click();

        Assert.Equal(["true", "true"], cut.FindAll("[data-part=trigger]").Select(t => t.GetAttribute("aria-expanded")));
    }

    [Fact]
    public void Click_ShouldCloseTheItem_WhenItIsOpen()
    {
        bool? expanded = null;
        var cut = Render<TbAccordionItem>(parameters => parameters
            .Add(item => item.Title, "Warranty")
            .Add(item => item.Expanded, true)
            .Add(item => item.ExpandedChanged, (bool value) => expanded = value));

        cut.Find("[data-part=trigger]").Click();

        Assert.False(expanded);
        Assert.True(cut.Find("[data-part=panel]").HasAttribute("inert"));
        Assert.DoesNotContain("tb-accordion__item--expanded", cut.Find("[data-part=item]").ClassList);
    }

    [Fact]
    public void Render_ShouldKeepAClickedItemOpen_WhenTheParentRendersAgainWithTheOldValue()
    {
        var cut = Render<TbAccordionItem>(parameters => parameters.Add(item => item.Title, "Warranty"));
        cut.Find("[data-part=trigger]").Click();

        cut.Render(parameters => parameters.Add(item => item.Expanded, false).Add(item => item.Disabled, false));

        Assert.Equal("true", cut.Find("[data-part=trigger]").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Render_ShouldUseTheHeadingLevelAndTitleContent_WhenGiven()
    {
        var cut = Render<TbAccordion>(parameters => parameters
            .Add(accordion => accordion.HeadingLevel, 2)
            .Add(accordion => accordion.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbAccordionItem>(0);
                builder.AddAttribute(1, nameof(TbAccordionItem.Title), "Ignored");
                builder.AddAttribute(2, nameof(TbAccordionItem.TitleContent),
                    (RenderFragment)(title => title.AddMarkupContent(0, "<strong>Sizes</strong>")));
                builder.CloseComponent();
            })));

        Assert.Equal("h2", cut.Find("[data-part=heading]").LocalName);
        Assert.Equal("Sizes", cut.Find("[data-part=trigger] strong").TextContent);
        Assert.DoesNotContain("Ignored", cut.Markup);
    }

    [Fact]
    public void Render_ShouldThrow_WhenGivenAHeadingLevelOutsideOneToSix()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Render<TbAccordion>(parameters => parameters.Add(accordion => accordion.HeadingLevel, 7)));
    }

    [Fact]
    public void Render_ShouldDisableTheTrigger_WhenDisabledIsSet()
    {
        var cut = Render<TbAccordionItem>(parameters => parameters
            .Add(item => item.Title, "Warranty")
            .Add(item => item.Disabled, true));

        Assert.True(cut.Find("[data-part=trigger]").HasAttribute("disabled"));
    }

    [Fact]
    public void Render_ShouldMarkTheAccordionReady_WhenItsScriptHasAttached()
    {
        JSInterop.SetupModule("./_content/Trailblazor/accordion.js").SetupModule("attachAccordion", _ => true);

        var cut = RenderAccordion();

        cut.WaitForAssertion(() => Assert.True(cut.Find(".tb-accordion").HasAttribute("data-ready")));
    }

    [Fact]
    public async Task Dispose_ShouldDetachTheKeyboardScript_WhenTheAccordionGoesAway()
    {
        var handle = JSInterop.SetupModule("./_content/Trailblazor/accordion.js")
            .SetupModule("attachAccordion", _ => true);
        handle.SetupVoid("dispose").SetVoidResult();
        RenderAccordion();

        await DisposeComponentsAsync();

        handle.VerifyInvoke("dispose");
    }

    [Fact]
    public void Render_ShouldPutTheHeaderActionsBesideTheHeadingAndOutOfItsButton_WhenGivenThem()
    {
        var cut = Render<TbAccordionItem>(parameters => parameters.Add(item => item.Title, "Day one")
            .Add(item => item.HeaderActions, (RenderFragment)(actions => actions.AddMarkupContent(0,
                "<button type=\"button\">Delete day one</button>"))));

        var header = cut.Find("[data-part=header]");
        Assert.Equal(["heading", "actions"], header.Children.Select(child => child.GetAttribute("data-part")));
        Assert.Equal("Delete day one", header.QuerySelector("[data-part=actions] button")!.TextContent);
        Assert.Equal("Day one", cut.Find("[data-part=trigger]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldLeaveTheHeadingOnItsOwn_WhenNoHeaderActionsAreGiven()
    {
        var cut = Render<TbAccordionItem>(parameters => parameters.Add(item => item.Title, "Day one"));

        Assert.Empty(cut.FindAll("[data-part=header]"));
        Assert.Equal("item", cut.Find("[data-part=heading]").ParentElement!.GetAttribute("data-part"));
    }

    [Fact]
    public void Click_ShouldOpenAndCloseTheItemUnderAnH3_WhenItIsOutsideAnAccordion()
    {
        var cut = Render<TbAccordionItem>(parameters => parameters.Add(item => item.Title, "Notes")
            .AddChildContent("Notes content"));

        Assert.Equal("h3", cut.Find("[data-part=heading]").LocalName);
        cut.Find("[data-part=trigger]").Click();
        Assert.Equal("true", cut.Find("[data-part=trigger]").GetAttribute("aria-expanded"));
        cut.Find("[data-part=trigger]").Click();
        Assert.Equal("false", cut.Find("[data-part=trigger]").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Render_ShouldUseTheItemsOwnHeadingLevel_WhenItGivesOneInsideAnAccordionOrOnItsOwn()
    {
        var cut = Render<TbAccordion>(parameters => parameters.Add(accordion => accordion.HeadingLevel, 2)
            .Add(accordion => accordion.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbAccordionItem>(0);
                builder.AddAttribute(1, nameof(TbAccordionItem.Title), "Day one");
                builder.AddAttribute(2, nameof(TbAccordionItem.ChildContent), (RenderFragment)(panel =>
                {
                    panel.OpenComponent<TbAccordionItem>(0);
                    panel.AddAttribute(1, nameof(TbAccordionItem.Title), "Gear list");
                    panel.AddAttribute(2, nameof(TbAccordionItem.HeadingLevel), 5);
                    panel.CloseComponent();
                }));
                builder.CloseComponent();
                builder.OpenComponent<TbAccordionItem>(10);
                builder.AddAttribute(11, nameof(TbAccordionItem.Title), "Day two");
                builder.AddAttribute(12, nameof(TbAccordionItem.HeadingLevel), 4);
                builder.CloseComponent();
            })));

        Assert.Equal(["h2", "h5", "h4"], cut.FindAll("[data-part=heading]").Select(heading => heading.LocalName));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void Render_ShouldThrow_WhenGivenAnItemHeadingLevelOutsideOneToSix(int level) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Render<TbAccordionItem>(parameters => parameters
            .Add(item => item.Title, "Day one").Add(item => item.HeadingLevel, level)));

    [Fact]
    public void Click_ShouldLeaveTheItemAroundItOpen_WhenAnItemInItsPanelOpens()
    {
        var cut = Render<TbAccordion>(parameters => parameters.Add(accordion => accordion.ChildContent,
            (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbAccordionItem>(0);
                builder.AddAttribute(1, nameof(TbAccordionItem.Title), "Day one");
                builder.AddAttribute(2, nameof(TbAccordionItem.Expanded), true);
                builder.AddAttribute(3, nameof(TbAccordionItem.ChildContent), (RenderFragment)(panel =>
                {
                    panel.OpenComponent<TbAccordionItem>(0);
                    panel.AddAttribute(1, nameof(TbAccordionItem.Title), "Gear list");
                    panel.CloseComponent();
                }));
                builder.CloseComponent();
            })));

        cut.FindAll("[data-part=trigger]")[1].Click();

        Assert.Equal(["true", "true"],
            cut.FindAll("[data-part=trigger]").Select(trigger => trigger.GetAttribute("aria-expanded")));
    }

    private IRenderedComponent<TbAccordion> RenderAccordion(Action<string, bool>? onChanged = null,
        bool multiple = false, bool firstExpanded = false, bool secondExpanded = false) =>
        Render<TbAccordion>(parameters => parameters
            .Add(accordion => accordion.Multiple, multiple)
            .Add(accordion => accordion.ChildContent, (RenderFragment)(builder =>
            {
                AddItem(builder, 0, "Shipping options", "Shipping content", firstExpanded, onChanged);
                AddItem(builder, 10, "Return policy", "Returns content", secondExpanded, onChanged);
            })));

    private void AddItem(RenderTreeBuilder builder, int sequence, string title, string content, bool expanded,
        Action<string, bool>? onChanged)
    {
        builder.OpenComponent<TbAccordionItem>(sequence);
        builder.AddAttribute(sequence + 1, nameof(TbAccordionItem.Title), title);
        builder.AddAttribute(sequence + 2, nameof(TbAccordionItem.Expanded), expanded);
        builder.AddAttribute(sequence + 3, nameof(TbAccordionItem.ExpandedChanged),
            EventCallback.Factory.Create<bool>(this, value => onChanged?.Invoke(title, value)));
        builder.AddAttribute(sequence + 4, nameof(TbAccordionItem.ChildContent),
            (RenderFragment)(panel => panel.AddContent(0, content)));
        builder.CloseComponent();
    }
}
