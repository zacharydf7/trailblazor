using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.JSInterop;

namespace Trailblazor.Tests.Components;

public class TbCarouselTests : BunitContext
{
    private readonly FakeTimeProvider _time = new();
    private readonly BunitJSModuleInterop _module;
    private readonly BunitJSModuleInterop _handle;
    // Unanswered until a test says whether motion is reduced, so until then the slides neither turn nor move.
    private readonly JSRuntimeInvocationHandler<bool> _reducedMotion;

    public TbCarouselTests()
    {
        Services.AddSingleton<TimeProvider>(_time);
        this.AddTrailblazor();
        _module = JSInterop.SetupModule("./_content/Trailblazor/carousel.js");
        _handle = _module.SetupModule("createCarousel", _ => true);
        _handle.SetupVoid("dispose");
        _reducedMotion = _handle.Setup<bool>("prefersReducedMotion");
    }

    private static TimeSpan FiveSeconds => TimeSpan.FromSeconds(5);

    private DotNetObjectReference<CarouselEvents> Events() =>
        (DotNetObjectReference<CarouselEvents>)_module.Invocations["createCarousel"].Single().Arguments[1]!;

    // Renders a carousel whose slides turn every five seconds, once its script says whether motion is reduced.
    private IRenderedComponent<TbCarousel> RenderTurning(bool reducedMotion = false)
    {
        _reducedMotion.SetResult(reducedMotion);
        // Moves wait for the script to end them, so these slides turn without moving.
        var cut = RenderCarousel(parameters => parameters.Add(carousel => carousel.AutoPlayInterval, FiveSeconds)
            .Add(carousel => carousel.Transition, CarouselTransition.None));
        cut.WaitForAssertion(() => Assert.Equal(reducedMotion ? "polite" : "off",
            cut.Find("[data-part=slides]").GetAttribute("aria-live")));
        return cut;
    }

    private List<int> Indexes { get; } = [];

    // The tick fires on another thread, so while the renderer is busy its turn waits behind the work in hand.
    private void TickFromAnotherThread()
    {
        var tick = new Thread(() => _time.Advance(FiveSeconds));
        tick.Start();
        tick.Join();
    }

    private IRenderedComponent<TbCarousel> RenderCarousel(
        Action<ComponentParameterCollectionBuilder<TbCarousel>>? configure = null) => Render<TbCarousel>(parameters =>
    {
        parameters.Add(carousel => carousel.AriaLabel, "Featured trails")
            .Add(carousel => carousel.ActiveIndexChanged, (int index) => Indexes.Add(index))
            .Add(carousel => carousel.ChildContent, (RenderFragment)(builder =>
            {
                foreach (var (trail, at) in new[] { "Pine", "Cedar", "Eagle" }.Select((trail, at) => (trail, at)))
                {
                    builder.OpenComponent<TbCarouselSlide>(at * 2);
                    builder.AddAttribute(at * 2 + 1, nameof(TbCarouselSlide.ChildContent),
                        (RenderFragment)(content => content.AddContent(0, trail)));
                    builder.CloseComponent();
                }
            }));
        configure?.Invoke(parameters);
    });

    private static string Shown(IRenderedComponent<TbCarousel> cut) =>
        cut.FindAll("[data-part=slide]").Single(slide => !slide.HasAttribute("hidden")).TextContent.Trim();

    [Fact]
    public void Render_ShouldBeANamedCarouselShowingItsFirstSlide_WhenCalled()
    {
        var cut = RenderCarousel();

        var region = cut.Find("section");
        Assert.Equal(("carousel", "Featured trails"), (region.GetAttribute("aria-roledescription"),
            region.GetAttribute("aria-label")));
        Assert.Equal(["1 of 3", "2 of 3", "3 of 3"],
            cut.FindAll("[data-part=slide]").Select(slide => slide.GetAttribute("aria-label")));
        Assert.All(cut.FindAll("[data-part=slide]"), slide => Assert.Equal("slide",
            slide.GetAttribute("aria-roledescription")));
        Assert.Equal("Pine", Shown(cut));
        Assert.Equal("polite", cut.Find("[data-part=slides]").GetAttribute("aria-live"));
    }

    [Fact]
    public void Render_ShouldMirrorThePreviousAndNextChevronsInRightToLeftText_WhenCalled()
    {
        var cut = RenderCarousel();

        var icons = cut.FindAll("[data-part=previous] .tb-icon, [data-part=next] .tb-icon");
        Assert.Equal(2, icons.Count);
        Assert.All(icons, icon => Assert.Contains("tb-icon--mirrored", icon.ClassList));
    }

    [Fact]
    public async Task Buttons_ShouldMoveBetweenSlidesAndWrapAround_WhenPressed()
    {
        var cut = RenderCarousel();

        await cut.Find("[data-part=previous]").ClickAsync(new());
        Assert.Equal("Eagle", Shown(cut));
        await cut.Find("[data-part=next]").ClickAsync(new());

        Assert.Equal("Pine", Shown(cut));
        Assert.Equal([2, 0], Indexes);
    }

    [Fact]
    public async Task Picker_ShouldShowTheSlideItNamesAndMarkIt_WhenItsButtonIsPressed()
    {
        var cut = RenderCarousel();
        var picks = cut.FindAll("[data-part=pick]");
        Assert.Equal(["Slide 1", "Slide 2", "Slide 3"], picks.Select(pick => pick.GetAttribute("aria-label")));

        await picks[2].ClickAsync(new());

        Assert.Equal("Eagle", Shown(cut));
        Assert.Equal("true", cut.FindAll("[data-part=pick]")[2].GetAttribute("aria-current"));
        Assert.Null(cut.FindAll("[data-part=pick]")[0].GetAttribute("aria-current"));
    }

    [Fact]
    public async Task Picker_ShouldCountThePickOnceActiveIndexChangedHasFinished_WhenTheHandlerIsSlow()
    {
        var changed = new TaskCompletionSource();
        var cut = Render<TbCarousel>(parameters => parameters.Add(carousel => carousel.AriaLabel, "Featured trails")
            .Add(carousel => carousel.ActiveIndexChanged, (int _) => changed.Task)
            .AddChildContent<TbCarouselSlide>().AddChildContent<TbCarouselSlide>().AddChildContent<TbCarouselSlide>());
        string? Commits() => cut.Find("[data-part=picker]").GetAttribute("data-commits");

        await cut.FindAll("[data-part=pick]")[0].ClickAsync(new());
        var unmoved = Commits();
        var picked = cut.FindAll("[data-part=pick]")[2].ClickAsync(new());
        var pending = Commits();
        await cut.InvokeAsync(changed.SetResult);
        await picked;

        Assert.Equal(("0", "0", "1"), (unmoved, pending, Commits()));
    }

    [Fact]
    public void Render_ShouldTurnOffTheButtonsAtTheEnds_WhenItDoesntLoop()
    {
        var cut = RenderCarousel(parameters => parameters.Add(carousel => carousel.Loop, false)
            .Add(carousel => carousel.ActiveIndex, 2).Add(carousel => carousel.ShowPicker, false));

        Assert.Null(cut.Find("[data-part=previous]").GetAttribute("aria-disabled"));
        Assert.Equal("true", cut.Find("[data-part=next]").GetAttribute("aria-disabled"));
        // It stays focusable, so focus isn't lost when the last press reaches the end.
        Assert.False(cut.Find("[data-part=next]").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("[data-part=picker]"));
    }

    [Fact]
    public void AutoPlayInterval_ShouldTurnTheSlidesAndStopWhenAsked_WhenGivenAnInterval()
    {
        var cut = RenderTurning();

        _time.Advance(FiveSeconds);
        cut.WaitForAssertion(() => Assert.Equal("Cedar", Shown(cut)));
        cut.Find("[data-part=rotation]").Click();
        _time.Advance(FiveSeconds * 2);

        Assert.Equal("Cedar", Shown(cut));
        Assert.Equal("Start slide rotation", cut.Find("[data-part=rotation]").GetAttribute("aria-label"));
        Assert.Equal("polite", cut.Find("[data-part=slides]").GetAttribute("aria-live"));
    }

    [Fact]
    public void AutoPlayInterval_ShouldStartStopped_WhenTheUserAsksForReducedMotion()
    {
        var cut = RenderTurning(reducedMotion: true);

        _time.Advance(FiveSeconds * 2);

        Assert.Equal("Pine", Shown(cut));
        Assert.Equal("Start slide rotation", cut.Find("[data-part=rotation]").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task AutoPlayInterval_ShouldStayOnTheSlide_WhenATickQueuedBeforeAPauseRunsAfterIt()
    {
        var cut = RenderTurning();

        // The tick's turn waits until the pause has been handled.
        await cut.InvokeAsync(() =>
        {
            TickFromAnotherThread();
            Events().Value.OnFocusEnter(fromKeyboard: false);
        });
        // Work queued on the renderer runs in order, so the tick's turn has run once this has.
        await cut.InvokeAsync(() => { });

        Assert.Equal("Pine", Shown(cut));
    }

    [Fact]
    public async Task AutoPlayInterval_ShouldWaitTheNewInterval_WhenATickOfTheOldOneRunsAfterTheChange()
    {
        var cut = RenderTurning();

        await cut.InvokeAsync(() =>
        {
            TickFromAnotherThread();
            cut.Render(parameters => parameters.Add(carousel => carousel.AutoPlayInterval, FiveSeconds * 2));
        });
        await cut.InvokeAsync(() => { });

        Assert.Equal("Pine", Shown(cut));
    }

    [Fact]
    public async Task Carousel_ShouldNotTurn_WhenATickQueuedBeforeItIsDisposedRunsAfterIt()
    {
        var cut = RenderTurning();
        _handle.SetupVoid("dispose").SetVoidResult();

        await cut.InvokeAsync(async () =>
        {
            TickFromAnotherThread();
            await cut.Instance.DisposeAsync();
        });
        await cut.InvokeAsync(() => { });

        Assert.Empty(Indexes);
    }

    [Fact]
    public async Task AutoPlayInterval_ShouldPauseAndGoOn_WhenThePointerOrAPointersFocusComesAndGoes()
    {
        var cut = RenderTurning();

        await cut.Find("section").TriggerEventAsync("onmouseenter", new());
        _time.Advance(FiveSeconds * 2);
        Assert.Equal("Pine", Shown(cut));
        await cut.InvokeAsync(() => Events().Value.OnFocusEnter(fromKeyboard: false));
        await cut.Find("section").TriggerEventAsync("onmouseleave", new());
        _time.Advance(FiveSeconds * 2);
        Assert.Equal("Pine", Shown(cut));
        await cut.InvokeAsync(() => Events().Value.OnFocusLeave());
        _time.Advance(FiveSeconds);

        cut.WaitForAssertion(() => Assert.Equal("Cedar", Shown(cut)));
        Assert.Equal("Stop slide rotation", cut.Find("[data-part=rotation]").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task AutoPlayInterval_ShouldStayStoppedUntilStartIsPressed_WhenKeyboardFocusEntersAndLeaves()
    {
        var cut = RenderTurning();

        await cut.InvokeAsync(() => Events().Value.OnFocusEnter(fromKeyboard: true));
        await cut.InvokeAsync(() => Events().Value.OnFocusLeave());
        _time.Advance(FiveSeconds * 2);
        Assert.Equal("Pine", Shown(cut));
        Assert.Equal("Start slide rotation", cut.Find("[data-part=rotation]").GetAttribute("aria-label"));

        await cut.InvokeAsync(() => Events().Value.OnFocusEnter(fromKeyboard: true));
        await cut.Find("[data-part=rotation]").ClickAsync(new());
        _time.Advance(FiveSeconds);

        cut.WaitForAssertion(() => Assert.Equal("Cedar", Shown(cut)));
    }

    [Fact]
    public void AutoPlayInterval_ShouldStartTurning_WhenItIsSetAfterTheFirstRender()
    {
        _reducedMotion.SetResult(false);
        // Without a transition, nothing else needs the script before AutoPlayInterval does.
        var cut = RenderCarousel(parameters => parameters.Add(carousel => carousel.Transition, CarouselTransition.None));

        cut.Render(parameters => parameters.Add(carousel => carousel.AutoPlayInterval, FiveSeconds));
        cut.WaitForAssertion(() => Assert.Equal("off", cut.Find("[data-part=slides]").GetAttribute("aria-live")));
        _time.Advance(FiveSeconds);

        cut.WaitForAssertion(() => Assert.Equal("Cedar", Shown(cut)));
    }

    [Fact]
    public void AutoPlayInterval_ShouldTurnAtTheNewInterval_WhenItChanges()
    {
        var cut = RenderTurning();

        cut.Render(parameters => parameters.Add(carousel => carousel.AutoPlayInterval, FiveSeconds * 2));
        _time.Advance(FiveSeconds);
        Assert.Equal("Pine", Shown(cut));
        _time.Advance(FiveSeconds);

        cut.WaitForAssertion(() => Assert.Equal("Cedar", Shown(cut)));
    }

    [Fact]
    public async Task AutoPlayInterval_ShouldHandTheErrorToTheRenderer_WhenActiveIndexChangedThrowsOnATurn()
    {
        _reducedMotion.SetResult(false);
        var cut = Render<TbCarousel>(parameters => parameters.Add(carousel => carousel.AriaLabel, "Featured trails")
            .Add(carousel => carousel.AutoPlayInterval, FiveSeconds)
            .Add(carousel => carousel.ActiveIndexChanged, (int _) => throw new InvalidOperationException("Turned"))
            .Add(carousel => carousel.Transition, CarouselTransition.None)
            .AddChildContent<TbCarouselSlide>().AddChildContent<TbCarouselSlide>());
        cut.WaitForAssertion(() => Assert.Equal("off", cut.Find("[data-part=slides]").GetAttribute("aria-live")));

        _time.Advance(FiveSeconds);

        var exception = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(5),
            Xunit.TestContext.Current.CancellationToken);
        Assert.Equal("Turned", exception.Message);
    }

    [Fact]
    public async Task Remove_ShouldKeepTheShownSlideAndReportItsNewIndex_WhenAnEarlierSlideGoes()
    {
        var trails = new List<string> { "Pine", "Cedar", "Eagle" };
        var cut = Render<TbCarousel>(parameters => parameters.Add(carousel => carousel.AriaLabel, "Featured trails")
            .Add(carousel => carousel.ActiveIndex, 1)
            .Add(carousel => carousel.ActiveIndexChanged, (int index) => Indexes.Add(index))
            .Add(carousel => carousel.ChildContent, (RenderFragment)(builder =>
            {
                foreach (var trail in trails)
                {
                    builder.OpenComponent<TbCarouselSlide>(0);
                    builder.SetKey(trail);
                    builder.AddAttribute(1, nameof(TbCarouselSlide.ChildContent),
                        (RenderFragment)(content => content.AddContent(0, trail)));
                    builder.CloseComponent();
                }
            })));

        trails.Remove("Pine");
        await cut.InvokeAsync(() => cut.Render(parameters => parameters.Add(carousel => carousel.ActiveIndex, 1)));

        cut.WaitForAssertion(() => Assert.Equal([0], Indexes));
        Assert.Equal("Cedar", Shown(cut));
        Assert.Equal(["1 of 2", "2 of 2"],
            cut.FindAll("[data-part=slide]").Select(slide => slide.GetAttribute("aria-label")));
    }

    [Fact]
    public void Render_ShouldPutASlidesClassStyleAndAttributesOnIt_WhenGivenThem()
    {
        var cut = Render<TbCarousel>(parameters => parameters.Add(carousel => carousel.AriaLabel, "Featured trails")
            .AddChildContent<TbCarouselSlide>(slide => slide.Add(item => item.Class, "my-slide")
                .Add(item => item.Style, "color: red").AddUnmatched("data-trail", "pine")
                .Add(item => item.AriaLabel, "Pine Hollow loop")));

        var slide = cut.Find("[data-part=slide]");
        Assert.Contains("my-slide", slide.ClassList);
        Assert.Equal(("color: red", "pine", "Pine Hollow loop"), (slide.GetAttribute("style"),
            slide.GetAttribute("data-trail"), slide.GetAttribute("aria-label")));
    }

    [Fact]
    public void Render_ShouldThrow_WhenItHasNoName()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbCarousel>(parameters => parameters
            .Add(carousel => carousel.AriaLabel, " ")));
    }

    // Renders a carousel whose script allows motion, once the script has said so.
    private IRenderedComponent<TbCarousel> RenderMoving(CarouselTransition transition = CarouselTransition.Slide,
        Action<ComponentParameterCollectionBuilder<TbCarousel>>? configure = null)
    {
        _reducedMotion.SetResult(false);
        var cut = RenderCarousel(parameters =>
        {
            parameters.Add(carousel => carousel.Transition, transition);
            configure?.Invoke(parameters);
        });
        cut.WaitForAssertion(() => Assert.Single(_handle.Invocations["prefersReducedMotion"]));
        return cut;
    }

    private static (string? Move, string? Direction, string? Transition) Move(IRenderedComponent<TbCarousel> cut)
    {
        var slides = cut.Find("[data-part=slides]");
        return (slides.GetAttribute("data-move"), slides.GetAttribute("data-direction"),
            slides.GetAttribute("data-transition"));
    }

    private static string Drawn(IRenderedComponent<TbCarousel> cut) => string.Join(", ",
        cut.FindAll("[data-part=slide]").Select(slide => slide.HasAttribute("hidden") ? "hidden" : "drawn"));

    private Task EndMoveAsync(IRenderedComponent<TbCarousel> cut, int move) =>
        cut.InvokeAsync(() => Events().Value.OnMoveEnd(move));

    [Fact]
    public async Task Transition_ShouldKeepTheLeavingSlideDrawnButOutOfReach_WhenTheSlidesTurnUntilTheMoveEnds()
    {
        var cut = RenderMoving();

        await cut.Find("[data-part=next]").ClickAsync(new());
        var pine = cut.FindAll("[data-part=slide]")[0];
        var during = (Move(cut), Drawn(cut), pine.GetAttribute("aria-hidden"), pine.HasAttribute("inert"),
            pine.HasAttribute("data-leaving"));
        await EndMoveAsync(cut, 1);

        Assert.Equal((("1", "forward", "slide"), "drawn, drawn, hidden", "true", true, true), during);
        Assert.Equal(((string?)null, (string?)null, (string?)null), Move(cut));
        Assert.Equal("Cedar", Shown(cut));
    }

    [Theory]
    [InlineData(0, "previous", "back")]
    [InlineData(2, "next", "forward")]
    public async Task Transition_ShouldMoveInTheButtonsDirection_WhenItWrapsAround(int start, string button,
        string direction)
    {
        var cut = RenderMoving(configure: parameters => parameters.Add(carousel => carousel.ActiveIndex, start));

        await cut.Find($"[data-part={button}]").ClickAsync(new());

        Assert.Equal(direction, Move(cut).Direction);
    }

    [Fact]
    public async Task Transition_ShouldMoveTowardThePickedSlide_WhenAPickIsPressed()
    {
        var cut = RenderMoving(CarouselTransition.Fade);

        await cut.FindAll("[data-part=pick]")[2].ClickAsync(new());
        var forward = Move(cut);
        await cut.FindAll("[data-part=pick]")[0].ClickAsync(new());

        Assert.Equal((("1", "forward", "fade"), ("2", "back", "fade")), (forward, Move(cut)));
    }

    [Fact]
    public async Task Transition_ShouldReplaceTheMoveAndIgnoreItsEnd_WhenTheSlidesTurnAgainDuringIt()
    {
        var cut = RenderMoving();

        await cut.Find("[data-part=next]").ClickAsync(new());
        await cut.Find("[data-part=next]").ClickAsync(new());
        await EndMoveAsync(cut, 1);

        Assert.Equal(("2", "hidden, drawn, drawn"), (Move(cut).Move, Drawn(cut)));
        Assert.True(cut.FindAll("[data-part=slide]")[1].HasAttribute("data-leaving"));
    }

    [Theory]
    [InlineData(CarouselTransition.None, false)]
    [InlineData(CarouselTransition.Slide, true)]
    public async Task Transition_ShouldTurnAtOnce_WhenItIsNoneOrMotionIsReduced(CarouselTransition transition,
        bool reducedMotion)
    {
        _reducedMotion.SetResult(reducedMotion);
        var cut = RenderCarousel(parameters => parameters.Add(carousel => carousel.Transition, transition));
        if (reducedMotion)
        {
            cut.WaitForAssertion(() => Assert.Single(_handle.Invocations["prefersReducedMotion"]));
        }

        await cut.Find("[data-part=next]").ClickAsync(new());

        Assert.Equal(((string?)null, "Cedar"), (Move(cut).Move, Shown(cut)));
    }

    [Fact]
    public async Task AutoPlayInterval_ShouldCountItsIntervalFromTheMovesEnd_WhenATurnMovesTheSlides()
    {
        var cut = RenderMoving(configure: parameters => parameters.Add(carousel => carousel.AutoPlayInterval,
            FiveSeconds));
        cut.WaitForAssertion(() => Assert.Equal("off", cut.Find("[data-part=slides]").GetAttribute("aria-live")));

        _time.Advance(FiveSeconds);
        cut.WaitForAssertion(() => Assert.Equal("1", Move(cut).Move));
        _time.Advance(FiveSeconds);
        await EndMoveAsync(cut, 1);
        _time.Advance(FiveSeconds - TimeSpan.FromSeconds(1));
        var beforeTheInterval = (Move(cut).Move, Shown(cut));
        _time.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(((string?)null, "Cedar"), beforeTheInterval);
        cut.WaitForAssertion(() => Assert.Equal("2", Move(cut).Move));
    }

    [Fact]
    public void AutoPlayInterval_ShouldKeepTheLiveRegionOff_WhenATurnByItselfMovesTheSlides()
    {
        var cut = RenderMoving(configure: parameters => parameters.Add(carousel => carousel.AutoPlayInterval,
            FiveSeconds));
        cut.WaitForAssertion(() => Assert.Equal("off", cut.Find("[data-part=slides]").GetAttribute("aria-live")));

        _time.Advance(FiveSeconds);
        cut.WaitForAssertion(() => Assert.Equal("1", Move(cut).Move));

        Assert.Equal("off", cut.Find("[data-part=slides]").GetAttribute("aria-live"));
    }

    [Fact]
    public async Task ActiveIndex_ShouldKeepTheMoveGoing_WhenABoundParentPassesTheTurnBack()
    {
        var cut = RenderMoving();
        await cut.Find("[data-part=next]").ClickAsync(new());

        cut.Render(parameters => parameters.Add(carousel => carousel.ActiveIndex, 1));

        Assert.Equal(("1", "drawn, drawn, hidden"), (Move(cut).Move, Drawn(cut)));
    }

    [Fact]
    public async Task ActiveIndex_ShouldShowTheParentsSlideAtOnce_WhenItChangesDuringAMove()
    {
        var cut = RenderMoving();
        await cut.Find("[data-part=next]").ClickAsync(new());

        cut.Render(parameters => parameters.Add(carousel => carousel.ActiveIndex, 2));

        Assert.Equal(((string?)null, "hidden, hidden, drawn"), (Move(cut).Move, Drawn(cut)));
    }
}
