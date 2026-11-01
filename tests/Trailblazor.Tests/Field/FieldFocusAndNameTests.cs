using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor.Tests.Field;

public class FieldFocusAndNameTests : BunitContext
{
    public FieldFocusAndNameTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldNameTheInputWithAHiddenLabel_WhenGivenAnAriaLabel()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.AriaLabel, "Search"));

        var label = cut.Find("label");
        Assert.Equal("Search", label.TextContent.Trim());
        Assert.Contains("tb-visually-hidden", label.ClassList);
        Assert.Equal(cut.Find("input").Id, label.GetAttribute("for"));
    }

    [Fact]
    public void Render_ShouldShowTheLabel_WhenGivenBothALabelAndAnAriaLabel()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Label, "Trail")
            .Add(field => field.AriaLabel, "Search"));

        var label = cut.Find("label");
        Assert.Equal("Trail", label.TextContent.Trim());
        Assert.DoesNotContain("tb-visually-hidden", label.ClassList);
    }

    [Fact]
    public void Render_ShouldNameTheComboboxWithTheHiddenLabel_WhenASelectHasAnAriaLabel()
    {
        var cut = Render<TbSelect<string>>(parameters => parameters
            .Add(field => field.AriaLabel, "Sort by")
            .Add(field => field.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbOption<string>>(0);
                builder.AddAttribute(1, nameof(TbOption<string>.Value), "Name");
                builder.CloseComponent();
            })));

        var label = cut.Find("[data-part=label]");
        Assert.Contains("tb-visually-hidden", label.ClassList);
        Assert.Equal(label.Id, cut.Find("[role=combobox]").GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void Render_ShouldNameTheGroupWithAHiddenLegend_WhenARadioGroupHasAnAriaLabel()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        var cut = Render<TbRadioGroup<string>>(parameters => parameters
            .Add(field => field.AriaLabel, "Pace")
            .Add(field => field.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbRadio<string>>(0);
                builder.AddAttribute(1, nameof(TbRadio<string>.Value), "Slow");
                builder.CloseComponent();
            })));

        var legend = cut.Find("legend");
        Assert.Equal("Pace", legend.TextContent.Trim());
        Assert.Contains("tb-visually-hidden", legend.ClassList);
    }

    [Fact]
    public async Task Focus_ShouldRaiseOnFocusAndOnBlur_WhenFocusCrossesTheField()
    {
        var events = new List<string>();
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Label, "Name")
            .Add(field => field.OnFocus, (FocusEventArgs _) => events.Add("focus"))
            .Add(field => field.OnBlur, (FocusEventArgs _) => events.Add("blur")));

        var root = cut.Find("[data-tb-focus-root]");
        await root.TriggerEventAsync("ontbfocusenter", new FieldFocusEventArgs { CrossesField = true });
        await root.TriggerEventAsync("ontbfocusleave", new FieldFocusEventArgs { CrossesField = true });

        Assert.Equal(["focus", "blur"], events);
    }

    [Fact]
    public async Task Focus_ShouldRaiseNothing_WhenFocusMovesBetweenTheFieldsOwnParts()
    {
        var events = 0;
        var cut = Render<TbDateRangePicker<DateOnly>>(parameters => parameters
            .Add(field => field.Label, "Dates")
            .Add(field => field.OnFocus, (FocusEventArgs _) => events++)
            .Add(field => field.OnBlur, (FocusEventArgs _) => events++));

        var root = cut.Find("[data-tb-focus-root]");
        await root.TriggerEventAsync("ontbfocusleave", new FieldFocusEventArgs { CrossesField = false });
        await root.TriggerEventAsync("ontbfocusenter", new FieldFocusEventArgs { CrossesField = false });

        Assert.Equal(0, events);
    }

    [Fact]
    public void Focus_ShouldAddNoHandler_WhenNoFocusEventIsBound()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Label, "Name"));

        Assert.Empty(cut.FindAll("[data-tb-focus-root]"));
        Assert.Throws<MissingEventHandlerException>(() => cut.Find("input").Focus());
    }

    // The browser focuses the marked control as it is added; the browser tests prove it.
    [Fact]
    public void Render_ShouldMarkTheInputForFocus_WhenAutoFocusIsSet()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Label, "Name")
            .Add(field => field.AutoFocus, true));

        Assert.True(cut.Find("input").HasAttribute("data-tb-autofocus"));
    }

    // Which of the field and an opening scope places focus depends on the browser's timing; the browser tests prove
    // it. Here the scope's script answers at once, so the scope is open by the time the field asks.
    [Fact]
    public void Render_ShouldFocusTheField_WhenItsFocusScopeHasOpened()
    {
        JSInterop.SetupModule("./_content/Trailblazor.Primitives/focus-scope.js")
            .SetupModule("createFocusScope", _ => true);

        var cut = RenderScope(scopeAutoFocus: false);

        Assert.True(cut.Find("input").HasAttribute("data-tb-autofocus"));
        JSInterop.VerifyFocusAsyncInvoke();
    }

    private IRenderedComponent<TbFocusScope> RenderScope(bool scopeAutoFocus)
    {
        RenderFragment field = builder =>
        {
            builder.OpenComponent<TbTextField<string>>(0);
            builder.AddAttribute(1, nameof(TbTextField<string>.Label), "Name");
            builder.AddAttribute(2, nameof(TbTextField<string>.AutoFocus), true);
            builder.CloseComponent();
        };

        return Render<TbFocusScope>(parameters => parameters
            .Add(scope => scope.AutoFocus, scopeAutoFocus)
            .Add(scope => scope.ChildContent, field));
    }

    [Fact]
    public void Render_ShouldNotMarkTheField_WhenAutoFocusIsNotSet()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Label, "Name"));

        Assert.Empty(cut.FindAll("[data-tb-autofocus]"));
    }

    [Fact]
    public void Render_ShouldNotMarkTheField_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<TbTextField<string>>(parameters => parameters
            .Add(field => field.Label, "Name").Add(field => field.AutoFocus, true));

        Assert.Empty(cut.FindAll("[data-tb-autofocus]"));
    }

    [Fact]
    public void Render_ShouldMarkTheGroupAndNotEachItem_WhenARadioGroupHasAutoFocus()
    {
        var cut = Render<TbRadioGroup<string>>(parameters => parameters
            .Add(field => field.Label, "Pace")
            .Add(field => field.AutoFocus, true)
            .Add(field => field.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbRadio<string>>(0);
                builder.AddAttribute(1, nameof(TbRadio<string>.Value), "Slow");
                builder.CloseComponent();
            })));

        Assert.True(cut.Find("fieldset").HasAttribute("data-tb-autofocus"));
        Assert.Empty(cut.FindAll("input[type=radio][data-tb-autofocus]"));
    }

    [Fact]
    public void Render_ShouldMarkTheComboboxForFocus_WhenASelectHasAutoFocus()
    {
        var cut = Render<TbSelect<string>>(parameters => parameters
            .Add(field => field.Label, "Trail")
            .Add(field => field.AutoFocus, true));

        Assert.True(cut.Find("[role=combobox]").HasAttribute("data-tb-autofocus"));
    }
}
