using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Trailblazor.Tests.Components;

public class TbToolbarTests : BunitContext
{
    private static readonly Icon Square = new("<path d=\"M4 4h16v16H4z\" />");

    public TbToolbarTests()
    {
        this.AddTrailblazor();
    }

    [Fact]
    public void Render_ShouldBeANamedToolbarWithAnOrientation_WhenCalled()
    {
        var cut = RenderToolbar(vertical: true);

        var toolbar = cut.Find("[role=toolbar]");
        Assert.Equal("Notes", toolbar.GetAttribute("aria-label"));
        Assert.Equal("vertical", toolbar.GetAttribute("aria-orientation"));
        Assert.Empty(cut.FindAll("[data-part=overflow]"));
    }

    [Fact]
    public void Render_ShouldNameAnIconButtonByItsAriaLabel_WhenItHasAnIcon()
    {
        var cut = RenderToolbar();

        var add = cut.Find("button[aria-label='Add a note']");
        Assert.Contains("tb-icon-button", add.ClassList);
        Assert.Equal("Add a note", cut.Find("[role=tooltip]").TextContent);
        Assert.Equal("Delete note", cut.Find("button.tb-button--text:not(.tb-icon-button)").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldDrawASeparatorAcrossTheToolbarsDirection_WhenGivenOne()
    {
        var row = Render<TbToolbar>(parameters => parameters.Add(toolbar => toolbar.AriaLabel, "Notes")
            .AddChildContent<TbToolbarSeparator>());
        var column = Render<TbToolbar>(parameters => parameters.Add(toolbar => toolbar.AriaLabel, "Notes")
            .Add(toolbar => toolbar.Vertical, true).AddChildContent<TbToolbarSeparator>());

        Assert.Equal("vertical", row.Find("[role=separator]").GetAttribute("aria-orientation"));
        Assert.Equal("horizontal", column.Find("[role=separator]").GetAttribute("aria-orientation"));
        Assert.NotNull(row.Find("[role=separator]").GetAttribute("data-toolbar-item"));
    }

    [Fact]
    public async Task Overflow_ShouldTurnSeparatorsIntoMenuLinesBetweenItems_WhenTheyMoveWithTheButtons()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/toolbar.js");
        module.SetupModule("attachToolbar", _ => true);
        var cut = Render<TbToolbar>(parameters => parameters.Add(toolbar => toolbar.AriaLabel, "Notes")
            .AddChildContent<TbToolbarSeparator>()
            .AddChildContent<TbToolbarButton>(button => button.Add(b => b.Label, "Bold"))
            .AddChildContent<TbToolbarSeparator>().AddChildContent<TbToolbarSeparator>()
            .AddChildContent<TbToolbarButton>(button => button.Add(b => b.Label, "Link"))
            .AddChildContent<TbToolbarSeparator>());
        var overflow =
            (DotNetObjectReference<ToolbarOverflow>)module.Invocations["attachToolbar"].Single().Arguments[2]!;
        var moved = cut.FindAll("[data-toolbar-item]").Select(item => item.GetAttribute("data-toolbar-item")!).ToArray();

        await cut.InvokeAsync(() => overflow.Value.OnOverflow(moved));

        // The leading, trailing and doubled separators would draw lines at the menu's edges or twice.
        var menu = cut.Find("[role=menu]").Children.Select(item => item.GetAttribute("role")).ToList();
        Assert.Equal(["menuitem", "separator", "menuitem"], menu);
    }

    [Fact]
    public async Task Overflow_ShouldListTheMovedButtonsInTheMenu_WhenTheBrowserReportsThem()
    {
        var clicks = new List<string>();
        var module = JSInterop.SetupModule("./_content/Trailblazor/toolbar.js");
        module.SetupModule("attachToolbar", _ => true);
        var cut = RenderToolbar(onClick: clicks.Add);
        var overflow =
            (DotNetObjectReference<ToolbarOverflow>)module.Invocations["attachToolbar"].Single().Arguments[2]!;
        var delete = cut.Find("button.tb-button--text:not(.tb-icon-button)").GetAttribute("data-toolbar-item")!;

        await cut.InvokeAsync(() => overflow.Value.OnOverflow([delete]));
        cut.Find("[role=menuitem]").Click();

        Assert.Equal("Delete note", cut.Find("[role=menuitem]").TextContent.Trim());
        Assert.Equal(["Delete note"], clicks);
    }

    [Fact]
    public async Task Overflow_ShouldFollowAMovedButtonsText_WhenTheButtonRendersOnItsOwn()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/toolbar.js");
        module.SetupModule("attachToolbar", _ => true);
        var cut = Render<TbToolbar>(parameters => parameters.Add(toolbar => toolbar.AriaLabel, "Notes")
            .Add(toolbar => toolbar.ChildContent, builder =>
            {
                builder.OpenComponent<RenamingButton>(0);
                builder.CloseComponent();
            }));
        var overflow =
            (DotNetObjectReference<ToolbarOverflow>)module.Invocations["attachToolbar"].Single().Arguments[2]!;
        await cut.InvokeAsync(() => overflow.Value.OnOverflow([cut.Find("button").GetAttribute("data-toolbar-item")!]));

        await cut.InvokeAsync(() => cut.FindComponent<RenamingButton>().Instance.Rename("Add a trail note"));

        Assert.Equal("Add a trail note", cut.Find("[role=menuitem]").TextContent.Trim());
    }

    [Fact]
    public async Task Overflow_ShouldGiveTheMenuItemsTheButtonsIconsAndStateInTheOrderReported_WhenButtonsMove()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/toolbar.js");
        module.SetupModule("attachToolbar", _ => true);
        var cut = Render<TbToolbar>(parameters => parameters.Add(toolbar => toolbar.AriaLabel, "Notes")
            .Add(toolbar => toolbar.ChildContent, builder =>
            {
                AddButton(builder, 0, "Add a note", Square, null);
                builder.OpenComponent<TbToolbarButton>(10);
                builder.AddAttribute(11, nameof(TbToolbarButton.Label), "Delete note");
                builder.AddAttribute(12, nameof(TbToolbarButton.Disabled), true);
                builder.CloseComponent();
            }));
        var overflow =
            (DotNetObjectReference<ToolbarOverflow>)module.Invocations["attachToolbar"].Single().Arguments[2]!;
        var ids = cut.FindAll("[data-toolbar-item]").Select(item => item.GetAttribute("data-toolbar-item")!).ToArray();

        await cut.InvokeAsync(() => overflow.Value.OnOverflow(ids));

        var items = cut.FindAll("[role=menuitem]");
        Assert.Equal(["Add a note", "Delete note"], items.Select(item => item.TextContent.Trim()));
        Assert.NotEmpty(items[0].QuerySelectorAll("svg"));
        Assert.Equal("true", items[1].GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenTheToolbarHasNoAriaLabel()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbToolbar>(parameters => parameters
            .Add(toolbar => toolbar.AriaLabel, " ")));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Render_ShouldThrow_WhenAToolbarButtonHasNoLabel(string text)
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbToolbarButton>(parameters => parameters
            .Add(button => button.Label, text)));
    }

    [Fact]
    public void Render_ShouldThrow_WhenAnIconToolbarButtonHasALabelButNoAriaLabel()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Render<TbToolbarButton>(parameters => parameters
            .Add(button => button.Icon, Square).Add(button => button.Label, "Add a note")));

        Assert.Contains("AriaLabel", error.Message);
    }

    [Theory]
    [InlineData(Size.Small, "tb-button--small")]
    [InlineData(Size.Large, "tb-button--large")]
    public void Render_ShouldSizeItsButtonsAndOverflowButton_WhenGivenASize(Size size, string modifier)
    {
        var cut = RenderToolbar();

        cut.Render(parameters => parameters.Add(toolbar => toolbar.Size, size));

        var buttons = cut.FindAll("[role=toolbar] button");
        Assert.Equal(3, buttons.Count);
        Assert.All(buttons, button => Assert.Contains(modifier, button.ClassList));
    }

    [Fact]
    public void Render_ShouldResizeATextOnlyButton_WhenTheToolbarsSizeChanges()
    {
        var cut = Render<TbToolbar>(parameters => parameters.Add(toolbar => toolbar.AriaLabel, "Notes")
            .Add(toolbar => toolbar.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbToolbarButton>(0);
                builder.AddAttribute(1, nameof(TbToolbarButton.Label), "Delete note");
                builder.CloseComponent();
            })));

        cut.Render(parameters => parameters.Add(toolbar => toolbar.Size, Size.Small));

        Assert.Contains("tb-button--small", cut.Find("button[data-toolbar-item]").ClassList);
    }

    [Fact]
    public void Render_ShouldKeepAButtonsOwnSize_WhenTheToolbarHasAnother()
    {
        var cut = Render<TbToolbar>(parameters => parameters.Add(toolbar => toolbar.AriaLabel, "Notes")
            .Add(toolbar => toolbar.Size, Size.Small)
            .Add(toolbar => toolbar.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbToolbarButton>(0);
                builder.AddAttribute(1, nameof(TbToolbarButton.Label), "Delete note");
                builder.AddAttribute(2, nameof(TbToolbarButton.Size), Size.Large);
                builder.CloseComponent();
            })));

        Assert.Contains("tb-button--large", cut.Find("button[data-toolbar-item]").ClassList);
    }

    private IRenderedComponent<TbToolbar> RenderToolbar(bool vertical = false, Action<string>? onClick = null) =>
        Render<TbToolbar>(parameters => parameters.Add(toolbar => toolbar.AriaLabel, "Notes")
            .Add(toolbar => toolbar.Vertical, vertical)
            .Add(toolbar => toolbar.ChildContent, Buttons(onClick)));

    private RenderFragment Buttons(Action<string>? onClick) => builder =>
    {
        AddButton(builder, 0, "Add a note", Square, onClick);
        AddButton(builder, 10, "Delete note", null, onClick);
    };

    private void AddButton(RenderTreeBuilder builder, int sequence, string text, Icon? icon, Action<string>? onClick)
    {
        builder.OpenComponent<TbToolbarButton>(sequence);
        var name = icon is null ? nameof(TbToolbarButton.Label) : nameof(TbToolbarButton.AriaLabel);
        builder.AddAttribute(sequence + 1, name, text);
        builder.AddAttribute(sequence + 2, nameof(TbToolbarButton.Icon), icon);
        builder.AddAttribute(sequence + 3, nameof(TbToolbarButton.OnClick),
            EventCallback.Factory.Create<MouseEventArgs>(this, () => onClick?.Invoke(text)));
        builder.CloseComponent();
    }

    // A button that re-renders without its toolbar, as one inside a component of its own does.
    private sealed class RenamingButton : ComponentBase
    {
        private string _text = "Add a note";

        public void Rename(string text)
        {
            _text = text;
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TbToolbarButton>(0);
            builder.AddAttribute(1, nameof(TbToolbarButton.Label), _text);
            builder.CloseComponent();
        }
    }

    [Fact]
    public async Task Vertical_ShouldAttachAgainWithoutAndThenWithOverflow_WhenItTurnsOnAndOff()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/toolbar.js");
        module.SetupModule("attachToolbar", _ => true).Mode = JSRuntimeMode.Loose;
        var cut = RenderToolbar();
        var overflow =
            (DotNetObjectReference<ToolbarOverflow>)module.Invocations["attachToolbar"].Single().Arguments[2]!;
        var delete = cut.Find("button.tb-button--text:not(.tb-icon-button)").GetAttribute("data-toolbar-item")!;
        await cut.InvokeAsync(() => overflow.Value.OnOverflow([delete]));
        Assert.Single(cut.FindAll("[role=menuitem]"));

        cut.Render(parameters => parameters.Add(toolbar => toolbar.Vertical, true));
        cut.Render(parameters => parameters.Add(toolbar => toolbar.Vertical, false));

        var calls = module.Invocations["attachToolbar"];
        Assert.Equal(3, calls.Count);
        Assert.Null(calls[1].Arguments[1]);
        Assert.NotNull(calls[2].Arguments[1]);
        // The button the old script moved is back, so the new menu starts empty.
        Assert.Empty(cut.FindAll("[role=menuitem]"));
    }
}
