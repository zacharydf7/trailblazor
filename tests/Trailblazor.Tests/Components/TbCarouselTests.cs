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

    public TbCarouselTests()
    {
        Services.AddSingleton<TimeProvider>(_time);
        this.AddTrailblazor();
        _module = JSInterop.SetupModule("./_content/Trailblazor/carousel.js");
        _handle = _module.SetupModule("createCarousel", _ => true);
        _handle.SetupVoid("dispose");
    }

    private static TimeSpan FiveSeconds => TimeSpan.FromSeconds(5);

    private DotNetObjectReference<CarouselEvents> Events() =>
        (DotNetObjectReference<CarouselEvents>)_module.Invocations["createCarousel"].Single().Arguments[1]!;

    // Renders a carousel whose slides turn every five seconds, once its script says whether motion is reduced.
    private IRenderedComponent<TbCarousel> RenderTurning(bool reducedMotion = false)
    {
        _handle.Setup<bool>("prefersReducedMotion").SetResult(reducedMotion);
        var cut = RenderCarousel(parameters => parameters.Add(carousel => carousel.AutoPlay, FiveSeconds));
        cut.WaitForAssertion(() => Assert.Equal(reducedMotion ? "polite" : "off",
            cut.Find("[data-part=slides]").GetAttribute("aria-live")));
        return cut;
    }

    private List<int> Indexes { get; } = [];

    private IRenderedComponent<TbCarousel> RenderCarousel(
        Action<ComponentParameterCollectionBuilder<TbCarousel>>? configure = null) => Render<TbCarousel>(parameters =>
    {
        parameters.Add(carousel => carousel.AriaLabel, "Featured trails")
            .Add(carousel => carousel.IndexChanged, (int index) => Indexes.Add(index))
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
    public void Render_ShouldTurnOffTheButtonsAtTheEnds_WhenItDoesntLoop()
    {
        var cut = RenderCarousel(parameters => parameters.Add(carousel => carousel.Loop, false)
            .Add(carousel => carousel.Index, 2).Add(carousel => carousel.ShowPicker, false));

        Assert.Null(cut.Find("[data-part=previous]").GetAttribute("aria-disabled"));
        Assert.Equal("true", cut.Find("[data-part=next]").GetAttribute("aria-disabled"));
        // It stays focusable, so focus isn't lost when the last press reaches the end.
        Assert.False(cut.Find("[data-part=next]").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("[data-part=picker]"));
    }

    [Fact]
    public void AutoPlay_ShouldTurnTheSlidesAndStopWhenAsked_WhenGivenAnInterval()
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
    public void AutoPlay_ShouldStartStopped_WhenTheUserAsksForReducedMotion()
    {
        var cut = RenderTurning(reducedMotion: true);

        _time.Advance(FiveSeconds * 2);

        Assert.Equal("Pine", Shown(cut));
        Assert.Equal("Start slide rotation", cut.Find("[data-part=rotation]").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task AutoPlay_ShouldPauseAndGoOn_WhenThePointerOrAPointersFocusComesAndGoes()
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
    public async Task AutoPlay_ShouldStayStoppedUntilStartIsPressed_WhenKeyboardFocusEntersAndLeaves()
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
    public void AutoPlay_ShouldStartTurning_WhenItIsSetAfterTheFirstRender()
    {
        _handle.Setup<bool>("prefersReducedMotion").SetResult(false);
        var cut = RenderCarousel();

        cut.Render(parameters => parameters.Add(carousel => carousel.AutoPlay, FiveSeconds));
        cut.WaitForAssertion(() => Assert.Equal("off", cut.Find("[data-part=slides]").GetAttribute("aria-live")));
        _time.Advance(FiveSeconds);

        cut.WaitForAssertion(() => Assert.Equal("Cedar", Shown(cut)));
    }

    [Fact]
    public void AutoPlay_ShouldTurnAtTheNewInterval_WhenItChanges()
    {
        var cut = RenderTurning();

        cut.Render(parameters => parameters.Add(carousel => carousel.AutoPlay, FiveSeconds * 2));
        _time.Advance(FiveSeconds);
        Assert.Equal("Pine", Shown(cut));
        _time.Advance(FiveSeconds);

        cut.WaitForAssertion(() => Assert.Equal("Cedar", Shown(cut)));
    }

    [Fact]
    public async Task AutoPlay_ShouldHandTheErrorToTheRenderer_WhenIndexChangedThrowsOnATurn()
    {
        _handle.Setup<bool>("prefersReducedMotion").SetResult(false);
        var cut = Render<TbCarousel>(parameters => parameters.Add(carousel => carousel.AriaLabel, "Featured trails")
            .Add(carousel => carousel.AutoPlay, FiveSeconds)
            .Add(carousel => carousel.IndexChanged, (int _) => throw new InvalidOperationException("Turned"))
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
            .Add(carousel => carousel.Index, 1).Add(carousel => carousel.IndexChanged, (int index) => Indexes.Add(index))
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
        await cut.InvokeAsync(() => cut.Render(parameters => parameters.Add(carousel => carousel.Index, 1)));

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
}
