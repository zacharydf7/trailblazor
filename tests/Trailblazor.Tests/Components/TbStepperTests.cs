using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

public class TbStepperTests : BunitContext
{
    public TbStepperTests()
    {
        this.AddTrailblazor();
    }

    private IRenderedComponent<TbStepper> RenderStepper(string active,
        Action<ComponentParameterCollectionBuilder<TbStepper>>? configure = null) => Render<TbStepper>(parameters =>
    {
        parameters.Add(stepper => stepper.Active, active)
            .AddChildContent<TbStep>(step => step.Add(s => s.Title, "Trip").Add(s => s.Completed, true)
                .Add(s => s.Description, "Where and when").AddChildContent("Pick the dates"))
            .AddChildContent<TbStep>(step => step.Add(s => s.Title, "Gear").Add(s => s.Optional, true)
                .AddChildContent("Pack light"))
            .AddChildContent<TbStep>(step => step.Add(s => s.Title, "Payment").Add(s => s.Key, "pay"));
        configure?.Invoke(parameters);
    });

    private static string Header(IRenderedComponent<TbStepper> cut, int step) =>
        cut.FindAll("[data-part=step]")[step].Children[0].GetAttribute("data-part")!;

    [Fact]
    public void Render_ShouldListTheStepsAndMarkTheCurrentOne_WhenGivenSteps()
    {
        var cut = RenderStepper("Gear");

        var list = cut.Find("ol[data-part=stepper]");
        var current = cut.Find("[aria-current=step]");

        Assert.Equal(("list", "Progress"), (list.GetAttribute("role"), list.GetAttribute("aria-label")));
        Assert.Equal(("BUTTON", "Gear"), (current.TagName, current.QuerySelector("[data-part=title]")!.TextContent));
        Assert.Equal(["step-button", "step-button", "step-header"], [Header(cut, 0), Header(cut, 1), Header(cut, 2)]);
        Assert.Equal("Optional", cut.Find("[data-part=optional]").TextContent);
        Assert.Equal("Where and when", cut.Find("[data-part=description]").TextContent);
        Assert.Contains("Completed", cut.FindAll("[data-part=step]")[0].TextContent);
        Assert.Contains("tb-stepper__step--completed", cut.FindAll("[data-part=step]")[0].ClassList);
        Assert.Equal("true", cut.Find("[data-part=indicator]").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void StepButton_ShouldMoveToACompletedStep_WhenPressed()
    {
        var changes = new List<string?>();
        var cut = RenderStepper("Gear", parameters => parameters.Add(stepper => stepper.ActiveChanged,
            (string? active) => changes.Add(active)));

        cut.FindAll("[data-part=step-button]")[0].Click();

        Assert.Equal(["Trip"], changes);
        Assert.Equal("Trip", cut.Find("[aria-current=step] [data-part=title]").TextContent);
        Assert.Equal("step-button", Header(cut, 0));
    }

    [Fact]
    public void StepButton_ShouldDoNothing_WhenItIsTheCurrentStep()
    {
        var changes = new List<string?>();
        var cut = RenderStepper("Gear", parameters => parameters.Add(stepper => stepper.ActiveChanged,
            (string? active) => changes.Add(active)));

        cut.Find("[aria-current=step]").Click();

        Assert.Empty(changes);
    }

    [Fact]
    public void StepButton_ShouldEndItsTextWithItsState_WhenItIsCompleted()
    {
        var cut = RenderStepper("Gear");

        var trip = cut.FindAll("[data-part=step-button]")[0];

        Assert.Equal(["Trip", "Where and when", "Completed"], trip.QuerySelectorAll(".tb-stepper__text > span")
            .Select(part => part.TextContent));
    }

    [Fact]
    public void Render_ShouldLetPeopleGoToAnyStep_WhenNotLinear()
    {
        var cut = RenderStepper("Trip", parameters => parameters.Add(stepper => stepper.Linear, false));

        cut.FindAll("[data-part=step-button]")[2].Click();

        Assert.Equal("Payment", cut.Find("[aria-current=step] [data-part=title]").TextContent);
    }

    [Fact]
    public void Render_ShouldKeepPeopleFromAStep_WhenItIsDisabled()
    {
        var cut = Render<TbStepper>(parameters => parameters.Add(stepper => stepper.Active, "Trip")
            .Add(stepper => stepper.Linear, false)
            .AddChildContent<TbStep>(step => step.Add(s => s.Title, "Trip"))
            .AddChildContent<TbStep>(step => step.Add(s => s.Title, "Gear").Add(s => s.Disabled, true)));

        Assert.Equal("step-header", Header(cut, 1));
        Assert.Contains("tb-stepper__step--disabled", cut.FindAll("[data-part=step]")[1].ClassList);
    }

    [Fact]
    public void Render_ShouldShowTheErrorInPlaceOfTheCheck_WhenAStepHasOne()
    {
        var cut = Render<TbStepper>(parameters => parameters.Add(stepper => stepper.Active, "Gear")
            .AddChildContent<TbStep>(step => step.Add(s => s.Title, "Trip").Add(s => s.Completed, true)
                .Add(s => s.Error, "Pick a date."))
            .AddChildContent<TbStep>(step => step.Add(s => s.Title, "Gear")));

        var step = cut.FindAll("[data-part=step]")[0];

        Assert.Equal("Pick a date.", step.QuerySelector("[data-part=error]")!.TextContent);
        Assert.Contains("tb-stepper__step--error", step.ClassList);
        Assert.DoesNotContain("tb-stepper__step--completed", step.ClassList);
        Assert.DoesNotContain("Completed", step.TextContent);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void Render_ShouldShowTheCurrentStepsContentOnlyInAColumn_WhenGivenALayout(bool vertical,
        int contents)
    {
        var cut = RenderStepper("Gear", parameters => parameters.Add(stepper => stepper.Vertical, vertical));

        Assert.Equal(contents, cut.FindAll("[data-part=step] [data-part=content]").Count);
    }

    [Fact]
    public void Active_ShouldAnnounceTheNewStep_WhenItChangesAfterTheFirstRender()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderStepper("Gear");
        Assert.Empty(announcer.FindAll("[role=status] p"));

        cut.Render(parameters => parameters.Add(stepper => stepper.Active, "pay"));

        announcer.WaitForAssertion(() =>
            Assert.Equal("Current step: Payment", announcer.Find("[role=status] p").TextContent));
    }

    [Fact]
    public void Render_ShouldPutAStepsClassStyleAndAttributesOnItsListItem_WhenTheyArePassed()
    {
        var cut = Render<TbStepper>(parameters => parameters.AddChildContent<TbStep>(step => step
            .Add(s => s.Title, "Trip").Add(s => s.Class, "trip").Add(s => s.Style, "color: teal")
            .AddUnmatched("data-kind", "first")));

        var item = cut.Find("li");

        Assert.Contains("trip", item.ClassList);
        Assert.Equal(("color: teal", "first"), (item.GetAttribute("style"), item.GetAttribute("data-kind")));
    }

    [Fact]
    public void Render_ShouldThrow_WhenAStepIsOutsideAStepperOrHasNoTitle()
    {
        Assert.Contains(nameof(TbStepper), Assert.Throws<InvalidOperationException>(() =>
            Render<TbStep>(parameters => parameters.Add(step => step.Title, "Trip"))).Message);
        Assert.Contains(nameof(TbStep.Title), Assert.Throws<InvalidOperationException>(() =>
            Render<TbStepper>(parameters => parameters.AddChildContent<TbStep>())).Message);
    }
}
