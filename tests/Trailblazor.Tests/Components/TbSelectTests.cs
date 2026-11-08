using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Trailblazor.Tests.Components;

public class TbSelectTests : BunitContext
{
    private string? Size { get; set; }

    public TbSelectTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldBeAComboboxNamedByItsLabelThatControlsAListbox_WhenInteractive()
    {
        var cut = RenderSelect("Medium");

        var combobox = cut.Find("[role=combobox]");
        var label = cut.Find("label");
        Assert.Null(label.GetAttribute("for"));
        Assert.Equal(label.Id, combobox.GetAttribute("aria-labelledby"));
        Assert.Equal("listbox", combobox.GetAttribute("aria-haspopup"));
        Assert.Equal("false", combobox.GetAttribute("aria-expanded"));
        Assert.Equal("0", combobox.GetAttribute("tabindex"));
        var listbox = cut.Find($"#{combobox.GetAttribute("aria-controls")}");
        Assert.Equal("listbox", listbox.GetAttribute("role"));
        Assert.Equal("auto", listbox.GetAttribute("popover"));
        var options = cut.FindAll("[role=option]");
        Assert.Equal(["Small", "Medium", "Large"], options.Select(option => option.TextContent.Trim()));
        Assert.Equal(["false", "true", "false"], options.Select(option => option.GetAttribute("aria-selected")));
        Assert.Equal("true", options[2].GetAttribute("aria-disabled"));
        Assert.Equal("Medium", cut.Find("[data-part=value]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldControlADialogNamedByTheLabel_WhenFilterable()
    {
        var cut = Render<TbSelect<string>>(parameters => parameters.Add(select => select.Label, "Size")
            .Add(select => select.Filterable, true)
            .AddChildContent<TbOption<string>>(option => option.Add(o => o.Value, "Small")));

        var combobox = cut.Find("[role=combobox]");
        Assert.Equal("dialog", combobox.GetAttribute("aria-haspopup"));
        var popup = cut.Find($"#{combobox.GetAttribute("aria-controls")}");
        Assert.Equal("dialog", popup.GetAttribute("role"));
        Assert.Equal(cut.Find("label").Id, popup.GetAttribute("aria-labelledby"));
        Assert.Single(popup.QuerySelectorAll("[role=listbox]"));
    }

    [Fact]
    public void Render_ShouldCheckOnlyTheChosenOption_WhenASingleSelectHasAValue()
    {
        var cut = RenderSelect("Medium");

        Assert.Equal(["Medium"], cut.FindAll("[role=option]:has([data-part=check])")
            .Select(option => option.TextContent.Trim()));
    }

    [Fact]
    public void Render_ShouldLeaveTheChecksToTheBoxes_WhenTheSelectIsMultiple()
    {
        var cut = Render<TbMultiSelect<string>>(parameters => parameters.Add(select => select.Label, "Toppings")
            .Add(select => select.Value, ["Olives"])
            .AddChildContent<TbOption<string>>(option => option.Add(o => o.Value, "Olives")));

        Assert.Equal("true", cut.Find("[role=option]").GetAttribute("aria-selected"));
        Assert.Empty(cut.FindAll("[data-part=check]"));
    }

    [Fact]
    public void Click_ShouldKeepTheValueAndHideClear_WhenReadOnly()
    {
        var cut = RenderSelect("Small");
        cut.Render(parameters => parameters.Add(select => select.ReadOnly, true));

        cut.FindAll("[role=option]")[1].Click();

        Assert.Equal("Small", Size);
        Assert.Equal("true", cut.Find("[role=combobox]").GetAttribute("aria-readonly"));
        Assert.Empty(cut.FindAll("[data-part=clear]"));
    }

    [Fact]
    public void Render_ShouldMarkTheNativeListReadOnlyButNotDisabled_WhenReadOnlyWithoutACircuit()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = RenderSelect("Small");
        cut.Render(parameters => parameters.Add(select => select.ReadOnly, true));

        var select = cut.Find("select");
        Assert.Equal("true", select.GetAttribute("aria-readonly"));
        Assert.False(select.HasAttribute("disabled"));
        Assert.True(cut.Find("option[value=Small]").HasAttribute("selected"));
    }

    [Fact]
    public void Render_ShouldShowThePlaceholder_WhenNothingIsChosen()
    {
        var cut = RenderSelect(null);

        Assert.Equal("Pick a size", cut.Find("[data-part=placeholder]").TextContent.Trim());
        Assert.Empty(cut.FindAll("[data-part=value]"));
        Assert.Empty(cut.FindAll("[data-part=clear]"));
    }

    [Fact]
    public void Click_ShouldChooseTheOption_WhenAnEnabledOptionIsClicked()
    {
        var cut = RenderSelect("Small");

        cut.FindAll("[role=option]")[1].Click();

        Assert.Equal("Medium", Size);
        Assert.Equal("true", cut.FindAll("[role=option]")[1].GetAttribute("aria-selected"));
        Assert.Equal("Medium", cut.Find("[data-part=value]").TextContent.Trim());
    }

    [Fact]
    public void Click_ShouldIgnoreTheOption_WhenItIsDisabled()
    {
        var cut = RenderSelect("Small");

        cut.FindAll("[role=option]")[2].Click();

        Assert.Equal("Small", Size);
    }

    [Fact]
    public void Clear_ShouldSetTheDefaultValue_WhenTheClearButtonIsClicked()
    {
        var cut = RenderSelect("Small");

        cut.Find("[data-part=clear]").Click();

        Assert.Null(Size);
        Assert.Equal("Pick a size", cut.Find("[data-part=placeholder]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldHideTheClearButtonAndLeaveTheComboboxOutOfTheTabOrder_WhenDisabled()
    {
        var cut = RenderSelect("Small", disabled: true);

        var combobox = cut.Find("[role=combobox]");
        Assert.Equal("-1", combobox.GetAttribute("tabindex"));
        Assert.Equal("true", combobox.GetAttribute("aria-disabled"));
        Assert.Empty(cut.FindAll("[data-part=clear]"));
    }

    [Fact]
    public void Render_ShouldBeANativeSelectNamedForTheBoundProperty_WhenRenderedStatically()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderSelect("Medium");

        var select = cut.Find("select");
        Assert.Equal(cut.Find("label").GetAttribute("for"), select.Id);
        Assert.Equal("Size", select.GetAttribute("name"));
        var options = cut.FindAll("option");
        Assert.Equal(["Small", "Medium", "Large"], options.Select(option => option.GetAttribute("value")));
        Assert.True(options[1].HasAttribute("selected"));
        Assert.True(options[2].HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("[data-part=chevron]"));
    }

    [Theory]
    [InlineData(null, "Nothing to choose from")]
    [InlineData("You haven't saved a route yet", "You haven't saved a route yet")]
    public void Render_ShouldSayTheListIsEmptyInAMessageThatCantBeChosen_WhenItHasNoOptions(string? empty,
        string expected)
    {
        var cut = Render<TbSelect<string>>(parameters => parameters.Add(select => select.Label, "Route")
            .Add(select => select.Labels, new SelectLabels { Empty = empty }));

        var status = cut.Find("[role=listbox] [data-part=status]");
        Assert.Equal(expected, status.TextContent.Trim());
        Assert.Equal("option", status.GetAttribute("role"));
        Assert.Equal("true", status.GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Render_ShouldSayTheListIsEmptyInADisabledOption_WhenRenderedStaticallyWithNoOptions()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<TbSelect<string>>(parameters => parameters.Add(select => select.Label, "Route")
            .Add(select => select.Labels, new SelectLabels { Empty = "You haven't saved a route yet" }));

        var status = cut.Find("select option[data-part=status]");
        Assert.Equal("You haven't saved a route yet", status.TextContent.Trim());
        Assert.True(status.HasAttribute("disabled"));
        Assert.Empty(cut.Tb().Select("Route").Options);
    }

    [Fact]
    public void Render_ShouldLeaveOutTheEmptyMessage_WhenTheListHasAnOption()
    {
        var cut = Render<TbSelect<string>>(parameters => parameters.Add(select => select.Label, "Route")
            .AddChildContent<TbOption<string>>(option => option.Add(o => o.Value, "loop")));

        Assert.Empty(cut.FindAll("[data-part=status]"));
    }

    [Fact]
    public void Render_ShouldNameAWordlessOptionByItsText_WhenRenderedStatically()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<TbSelect<string>>(parameters => parameters.Add(select => select.Label, "Route")
            .AddChildContent<TbOption<string>>(option => option.Add(o => o.Value, "any").Add(o => o.Text, "Any trail")
                .AddChildContent("<svg aria-hidden=\"true\"></svg>")));

        Assert.Equal("Any trail", cut.Find("option[value=any]").InnerHtml.Trim());
    }

    [Fact]
    public void Render_ShouldStartWithAnEmptyPlaceholderOption_WhenRenderedStaticallyWithNoChoice()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderSelect(null);

        var first = cut.FindAll("option")[0];
        Assert.Equal("", first.GetAttribute("value"));
        Assert.True(first.HasAttribute("selected"));
        Assert.Equal("Pick a size", first.TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldShowAnOptionsTextOrValue_WhenItHasNoContent()
    {
        var cut = Render<TbSelect<int>>(parameters => parameters
            .Add(select => select.Label, "Copies")
            .Add(select => select.Value, 2)
            .Add(select => select.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbOption<int>>(0);
                builder.AddAttribute(1, nameof(TbOption<int>.Value), 1);
                builder.CloseComponent();
                builder.OpenComponent<TbOption<int>>(2);
                builder.AddAttribute(3, nameof(TbOption<int>.Value), 2);
                builder.AddAttribute(4, nameof(TbOption<int>.Text), "Two copies");
                builder.CloseComponent();
            })));

        Assert.Equal(["1", "Two copies"], cut.FindAll("[role=option]").Select(option => option.TextContent.Trim()));
        Assert.Equal("Two copies", cut.FindAll("[role=option]")[1].GetAttribute("data-text"));
        Assert.Equal("Two copies", cut.Find("[data-part=value]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldUpdateTheShownChoice_WhenTheChosenOptionsTextChanges()
    {
        var cut = Render<TbSelect<int>>(parameters => parameters.Add(select => select.Value, 1)
            .Add(select => select.ChildContent, (RenderFragment)(builder => AddNumber(builder, "One"))));

        cut.Render(parameters => parameters.Add(select => select.ChildContent,
            (RenderFragment)(builder => AddNumber(builder, "Uno"))));

        Assert.Equal("Uno", cut.Find("[data-part=value]").TextContent.Trim());

        static void AddNumber(RenderTreeBuilder builder, string text)
        {
            builder.OpenComponent<TbOption<int>>(0);
            builder.AddAttribute(1, nameof(TbOption<int>.Value), 1);
            builder.AddAttribute(2, nameof(TbOption<int>.Text), text);
            builder.CloseComponent();
        }
    }

    [Fact]
    public void Click_ShouldChooseAnEnumOption_WhenTheSelectIsBoundToANullableEnum()
    {
        DayOfWeek? day = null;
        var cut = Render<TbSelect<DayOfWeek?>>(parameters => parameters
            .Add(select => select.Value, day)
            .Add(select => select.ValueChanged, (DayOfWeek? next) => day = next)
            .Add(select => select.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbOption<DayOfWeek>>(0);
                builder.AddAttribute(1, nameof(TbOption<DayOfWeek>.Value), DayOfWeek.Monday);
                builder.CloseComponent();
                builder.OpenComponent<TbOption<DayOfWeek>>(2);
                builder.AddAttribute(3, nameof(TbOption<DayOfWeek>.Value), DayOfWeek.Friday);
                builder.CloseComponent();
            })));

        cut.FindAll("[role=option]")[1].Click();

        Assert.Equal(DayOfWeek.Friday, day);
        Assert.Equal("Friday", cut.Find("[data-part=value]").TextContent.Trim());
        Assert.Equal(["Monday", "Friday"], cut.FindAll("[role=option]").Select(option => option.TextContent.Trim()));
    }

    [Fact]
    public void Render_ShouldUpdateTheShownChoice_WhenTheChosenOptionsContentChanges()
    {
        var cut = Render<TbSelect<int>>(parameters => parameters.Add(select => select.Value, 1)
            .Add(select => select.ChildContent, (RenderFragment)(builder => AddNumbers(builder, (1, "One")))));

        cut.Render(parameters => parameters.Add(select => select.ChildContent,
            (RenderFragment)(builder => AddNumbers(builder, (1, "Uno")))));

        Assert.Equal("Uno", cut.Find("[data-part=value]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldShowTheNewChoice_WhenTheValueAndTheOptionsChangeTogether()
    {
        var cut = Render<TbSelect<int>>(parameters => parameters.Add(select => select.Value, 1)
            .Add(select => select.ChildContent, (RenderFragment)(builder => AddNumbers(builder, (1, "One"), (2, "Two")))));

        cut.Render(parameters => parameters.Add(select => select.Value, 3).Add(select => select.ChildContent,
            (RenderFragment)(builder => AddNumbers(builder, (3, "Three"), (4, "Four")))));

        Assert.Equal("Three", cut.Find("[data-part=value]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldShowTheValuesText_WhenTheChosenOptionIsSwappedForAnother()
    {
        var cut = Render<TbSelect<int>>(parameters => parameters.Add(select => select.Value, 1)
            .Add(select => select.Placeholder, "Pick a number")
            .Add(select => select.ChildContent, (RenderFragment)(builder => AddNumbers(builder, (1, "One"), (2, "Two")))));

        cut.Render(parameters => parameters.Add(select => select.ChildContent,
            (RenderFragment)(builder => AddNumbers(builder, (5, "Five"), (2, "Two")))));

        Assert.Equal("1", cut.Find("[data-part=value]").TextContent.Trim());
        Assert.Empty(cut.FindAll("[data-part=placeholder]"));
    }

    [Fact]
    public void Render_ShouldShowTheValuesTextAndOfferToClearIt_WhenNoOptionHasTheValue()
    {
        var cut = RenderSelect("Huge");

        Assert.Equal("Huge", cut.Find("[data-part=value]").TextContent.Trim());
        Assert.Empty(cut.FindAll("[data-part=placeholder]"));
        Assert.Single(cut.FindAll("[data-part=clear]"));
    }

    [Fact]
    public void Render_ShouldShowThePlaceholder_WhenTheValueIsAnEmptyString()
    {
        var cut = RenderSelect("");

        Assert.Equal("Pick a size", cut.Find("[data-part=placeholder]").TextContent.Trim());
        Assert.Empty(cut.FindAll("[data-part=value]"));
    }

    [Fact]
    public void Render_ShouldStartWithThePlaceholderOption_WhenRenderedStaticallyWithAnEmptyString()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderSelect("");

        var first = cut.FindAll("option")[0];
        Assert.Equal(("", "Pick a size"), (first.GetAttribute("value"), first.TextContent.Trim()));
    }

    [Fact]
    public void Render_ShouldShowTheUnmatchedText_WhenGivenOneForAValueNoOptionHas()
    {
        var cut = Render<TbSelect<int>>(parameters => parameters.Add(select => select.Value, 7)
            .Add(select => select.UnmatchedText, number => $"Archived ({number})")
            .Add(select => select.ChildContent, (RenderFragment)(builder => AddNumbers(builder, (1, "One"), (2, "Two")))));

        Assert.Equal("Archived (7)", cut.Find("[data-part=value]").TextContent.Trim());

        cut.Render(parameters => parameters.Add(select => select.Value, 2));

        Assert.Equal("Two", cut.Find("[data-part=value]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldKeepAValueNoOptionHasSelected_WhenRenderedStatically()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderSelect("Huge");

        var first = cut.FindAll("option")[0];
        Assert.Equal("Huge", first.GetAttribute("value"));
        Assert.True(first.HasAttribute("selected"));
        Assert.Equal("Huge", first.TextContent.Trim());
        Assert.Empty(cut.FindAll("option[value='']"));
    }

    [Fact]
    public void Render_ShouldMarkOnlyTheChosenOptionSelected_WhenOptionsHaveNoContent()
    {
        var chosen = 1;
        var cut = Render<TbSelect<int>>(parameters => parameters
            .Add(select => select.Value, chosen)
            .Add(select => select.ValueChanged, (int next) => chosen = next)
            .Add(select => select.ChildContent, (RenderFragment)(builder =>
            {
                for (var value = 1; value <= 3; value++)
                {
                    builder.OpenComponent<TbOption<int>>(0);
                    builder.AddAttribute(1, nameof(TbOption<int>.Value), value);
                    builder.AddAttribute(2, nameof(TbOption<int>.Text), $"Option {value}");
                    builder.CloseComponent();
                }
            })));

        cut.FindAll("[role=option]")[1].Click();
        Assert.Equal(["false", "true", "false"], cut.FindAll("[role=option]").Select(o => o.GetAttribute("aria-selected")));

        cut.Render(parameters => parameters.Add(select => select.Value, 3));
        Assert.Equal(["false", "false", "true"], cut.FindAll("[role=option]").Select(o => o.GetAttribute("aria-selected")));
    }

    [Fact]
    public void Render_ShouldThrow_WhenAnOptionsValueIsNotTheSelectsType()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Render<TbSelect<long>>(parameters => parameters
            .Add(select => select.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbOption<int>>(0);
                builder.AddAttribute(1, nameof(TbOption<int>.Value), 1);
                builder.CloseComponent();
            }))));

        Assert.Contains("Int64", exception.Message);
        exception = Assert.Throws<InvalidOperationException>(() => Render<TbSelect<long?>>(parameters => parameters
            .Add(select => select.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbOption<int>>(0);
                builder.AddAttribute(1, nameof(TbOption<int>.Value), 1);
                builder.CloseComponent();
            }))));
        Assert.Contains("must be a Int64", exception.Message);
    }

    [Fact]
    public void Validate_ShouldIgnoreASelect_WhenItWasRemovedFromTheForm()
    {
        var editContext = new EditContext(this);
        var cut = Render<EditForm>(parameters => parameters
            .Add(form => form.EditContext, editContext)
            .Add(form => form.ChildContent, (RenderFragment<EditContext>)(_ => builder =>
            {
                builder.OpenComponent<TbSelect<string?>>(0);
                builder.AddAttribute(1, nameof(TbSelect<string?>.ValueExpression),
                    (Expression<Func<string?>>)(() => Size));
                builder.AddAttribute(2, nameof(TbSelect<string?>.Validation),
                    (Func<string?, IEnumerable<string>>)(_ => ["Pick a size."]));
                builder.CloseComponent();
            })));
        Assert.False(editContext.Validate());

        cut.Render(parameters => parameters.Add(form => form.ChildContent,
            (RenderFragment<EditContext>)(_ => _ => { })));

        Assert.True(editContext.Validate());
    }

    [Fact]
    public void Render_ShouldThrow_WhenAnOptionIsOutsideASelect()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => Render<TbOption<string>>(parameters => parameters.Add(option => option.Value, "Small")));

        Assert.Contains("TbSelect", exception.Message);
    }

    [Fact]
    public void Choose_ShouldShowTheValidationMessage_WhenTheValueIsInvalid()
    {
        var cut = RenderSelect("Small", validation: size => size == "Medium" ? ["Medium is sold out."] : []);

        cut.FindAll("[role=option]")[1].Click();

        Assert.Equal("Medium is sold out.", cut.Find("[data-part=error]").TextContent.Trim());
        Assert.Equal("true", cut.Find("[role=combobox]").GetAttribute("aria-invalid"));
        Assert.Contains(cut.Find("[data-part=error]").Id!, cut.Find("[role=combobox]").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void Open_ShouldOpenAndCloseTheList_WhenTheParentChangesIt()
    {
        var handle = SetUpSetOpen();
        var cut = RenderSelect("Small");
        Assert.Empty(handle.Invocations["setOpen"]);

        cut.Render(parameters => parameters.Add(select => select.Open, true));
        cut.Render(parameters => parameters.Add(select => select.Open, false));

        Assert.Equal([true, false], handle.Invocations["setOpen"].Select(call => call.Arguments[0]));
    }

    [Fact]
    public async Task OpenChanged_ShouldReportTheToggleWithoutCallingSetOpen_WhenTheBrowserOpensTheList()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/select.js");
        var handle = SetUpSetOpen(module);
        var opened = new List<bool>();
        var cut = RenderSelect("Small");
        cut.Render(parameters => parameters.Add(select => select.OpenChanged, (bool open) => opened.Add(open)));
        var toggle = (DotNetObjectReference<SelectToggle>)module.Invocations["createSelect"].Single().Arguments[2]!;

        await cut.InvokeAsync(() => toggle.Value.OnToggle(true));
        cut.Render(parameters => parameters.Add(select => select.Open, true));
        await cut.InvokeAsync(() => toggle.Value.OnToggle(true));

        Assert.Equal([true], opened);
        Assert.Empty(handle.Invocations["setOpen"]);
    }

    [Fact]
    public async Task Render_ShouldLeaveTheListOpen_WhenAParentThatDoesntBindOpenRendersAgain()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/select.js");
        var handle = SetUpSetOpen(module);
        var cut = RenderSelect("Small");
        var toggle = (DotNetObjectReference<SelectToggle>)module.Invocations["createSelect"].Single().Arguments[2]!;

        await cut.InvokeAsync(() => toggle.Value.OnToggle(true));
        cut.Render(parameters => parameters.Add(select => select.Placeholder, "Choose"));

        Assert.Empty(handle.Invocations["setOpen"]);
    }

    [Fact]
    public void Open_ShouldOpenTheList_WhenItStartsTrue()
    {
        var handle = SetUpSetOpen();

        RenderSelect("Small", open: true);

        Assert.Equal([true], handle.Invocations["setOpen"].Select(call => call.Arguments[0]));
    }

    [Fact]
    public void Open_ShouldReportTheListClosed_WhenTheParentOpensADisabledSelect()
    {
        var handle = JSInterop.SetupModule("./_content/Trailblazor/select.js").SetupModule("createSelect", _ => true);
        handle.Setup<bool>("setOpen", _ => true).SetResult(false);
        var opened = new List<bool>();
        var cut = RenderSelect("Small", disabled: true);
        cut.Render(parameters => parameters.Add(select => select.OpenChanged, (bool open) => opened.Add(open)));

        cut.Render(parameters => parameters.Add(select => select.Open, true));

        Assert.Equal([false], opened);
    }

    [Fact]
    public async Task Dispose_ShouldDetachTheScript_WhenTheSelectGoesAway()
    {
        var handle = JSInterop.SetupModule("./_content/Trailblazor/select.js").SetupModule("createSelect", _ => true);
        handle.SetupVoid("dispose").SetVoidResult();
        RenderSelect("Small");

        await DisposeComponentsAsync();

        handle.VerifyInvoke("dispose");
    }

    [Fact]
    public void Click_ShouldClearToTheDefaultAndShowThePlaceholder_WhenAClearableSelectHasANonNullableValueType()
    {
        var rangers = Guid.NewGuid();
        var team = rangers;
        var cut = Render<TbSelect<Guid>>(parameters => parameters
            .Add(select => select.Label, "Team").Add(select => select.Clearable, true)
            .Add(select => select.Placeholder, "No team").Add(select => select.Value, team)
            .Add(select => select.ValueChanged, (Guid next) => team = next)
            .Add(select => select.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbOption<Guid>>(0);
                builder.AddAttribute(1, nameof(TbOption<Guid>.Value), rangers);
                builder.AddAttribute(2, nameof(TbOption<Guid>.Text), "Rangers");
                builder.CloseComponent();
            })));

        cut.Find("[data-part=clear]").Click();

        Assert.Equal(Guid.Empty, team);
        Assert.Equal("No team", cut.Find("[data-part=placeholder]").TextContent.Trim());
        Assert.Empty(cut.FindAll("[data-part=clear]"));
    }

    [Fact]
    public void Render_ShouldDescribeTheClearButtonByTheFieldsLabel_WhenTheSelectHasALabel()
    {
        var cut = RenderSelect("Small");

        var clear = cut.Find("[data-part=clear]");
        Assert.Equal("Clear", clear.GetAttribute("aria-label"));
        Assert.Equal("Size", cut.Find($"[id='{clear.GetAttribute("aria-describedby")}']").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldNameAnOptionByItsText_WhenItShowsContentOfItsOwn()
    {
        var cut = Render<TbSelect<string>>(parameters => parameters.Add(select => select.Label, "Badge")
            .Add(select => select.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbOption<string>>(0);
                builder.AddAttribute(1, nameof(TbOption<string>.Value), "gold");
                builder.AddAttribute(2, nameof(TbOption<string>.Text), "Gold");
                builder.AddAttribute(3, nameof(TbOption<string>.ChildContent),
                    (RenderFragment)(content => content.AddMarkupContent(0, "<img src=\"gold.png\" /> gold.png")));
                builder.CloseComponent();
                builder.OpenComponent<TbOption<string>>(4);
                builder.AddAttribute(5, nameof(TbOption<string>.Value), "silver");
                builder.AddAttribute(6, nameof(TbOption<string>.Text), "Silver");
                builder.CloseComponent();
            })));

        var options = cut.FindAll("[role=option]");
        Assert.Equal(["Gold", null], options.Select(option => option.GetAttribute("aria-label")));
        Assert.Equal("Silver", options[1].TextContent.Trim());

        options[0].Click();

        // Chosen, it shows its content to the eye and its Text to screen readers.
        var value = cut.Find("[data-part=value]");
        Assert.Equal("true", value.QuerySelector("img")!.ParentElement!.GetAttribute("aria-hidden"));
        Assert.Equal("Gold", value.QuerySelector(".tb-visually-hidden")!.TextContent);
    }

    [Fact]
    public void Render_ShouldNameTheClearButtonFromTheLabels_WhenGivenAClearLabel()
    {
        var cut = Render<TbSelect<DayOfWeek?>>(parameters => parameters
            .Add(select => select.Label, "Day").Add(select => select.Clearable, true)
            .Add(select => select.Value, DayOfWeek.Monday)
            .Add(select => select.Labels, new SelectLabels { Clear = "Any day" })
            .Add(select => select.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbOption<DayOfWeek>>(0);
                builder.AddAttribute(1, nameof(TbOption<DayOfWeek>.Value), DayOfWeek.Monday);
                builder.CloseComponent();
            })));

        Assert.Equal("Any day", cut.Find("[data-part=clear]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Click_ShouldClearToNull_WhenAClearableSelectHasANullableValueType()
    {
        DayOfWeek? day = DayOfWeek.Monday;
        var cut = Render<TbSelect<DayOfWeek?>>(parameters => parameters
            .Add(select => select.Label, "Day").Add(select => select.Clearable, true).Add(select => select.Value, day)
            .Add(select => select.ValueChanged, (DayOfWeek? next) => day = next)
            .Add(select => select.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbOption<DayOfWeek>>(0);
                builder.AddAttribute(1, nameof(TbOption<DayOfWeek>.Value), DayOfWeek.Monday);
                builder.CloseComponent();
            })));

        cut.Find("[data-part=clear]").Click();

        Assert.Null(day);
    }

    // The script's setOpen reports the list open exactly when asked to open it.
    private BunitJSModuleInterop SetUpSetOpen(BunitJSModuleInterop? module = null)
    {
        var handle = (module ?? JSInterop.SetupModule("./_content/Trailblazor/select.js"))
            .SetupModule("createSelect", _ => true);
        handle.Setup<bool>("setOpen", call => call.Arguments[0] is true).SetResult(true);
        handle.Setup<bool>("setOpen", call => call.Arguments[0] is false).SetResult(false);
        return handle;
    }

    private IRenderedComponent<TbSelect<string?>> RenderSelect(string? value, bool disabled = false,
        Func<string?, IEnumerable<string>>? validation = null, bool open = false)
    {
        Size = value;
        return Render<TbSelect<string?>>(parameters => parameters
            .Add(select => select.Open, open)
            .Add(select => select.Label, "Size")
            .Add(select => select.Placeholder, "Pick a size")
            .Add(select => select.Clearable, true)
            .Add(select => select.Disabled, disabled)
            .Add(select => select.Validation, validation)
            .Add(select => select.Value, value)
            .Add(select => select.ValueChanged, (string? next) => Size = next)
            .Add(select => select.ValueExpression, (Expression<Func<string?>>)(() => Size))
            .Add(select => select.ChildContent, (RenderFragment)(builder =>
            {
                AddOption(builder, 0, "Small");
                AddOption(builder, 10, "Medium");
                AddOption(builder, 20, "Large", disabled: true);
            })));
    }

    private static void AddOption(RenderTreeBuilder builder, int sequence, string value, bool disabled = false)
    {
        builder.OpenComponent<TbOption<string?>>(sequence);
        builder.AddAttribute(sequence + 1, nameof(TbOption<string?>.Value), value);
        builder.AddAttribute(sequence + 2, nameof(TbOption<string?>.Disabled), disabled);
        builder.AddAttribute(sequence + 3, nameof(TbOption<string?>.ChildContent),
            (RenderFragment)(content => content.AddContent(0, value)));
        builder.CloseComponent();
    }

    private static void AddNumbers(RenderTreeBuilder builder, params (int Value, string Label)[] numbers)
    {
        foreach (var (value, label) in numbers)
        {
            builder.OpenComponent<TbOption<int>>(0);
            builder.AddAttribute(1, nameof(TbOption<int>.Value), value);
            builder.AddAttribute(2, nameof(TbOption<int>.ChildContent),
                (RenderFragment)(content => content.AddContent(0, label)));
            builder.CloseComponent();
        }
    }
}
