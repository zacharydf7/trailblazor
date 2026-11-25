using System.Diagnostics.Metrics;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Time.Testing;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbCountdownTests : BunitContext
{
    private readonly FakeTimeProvider _time = new();

    public TbCountdownTests()
    {
        Services.AddSingleton<TimeProvider>(_time);
        this.AddTrailblazor();
    }

    private FakeAnnouncer Announcer => Services.GetRequiredService<FakeAnnouncer>();

    private string Url => Services.GetRequiredService<NavigationManager>().Uri;

    private IRenderedComponent<TbCountdown> RenderRedirect(TimeSpan duration,
        Action<ComponentParameterCollectionBuilder<TbCountdown>>? more = null) => Render<TbCountdown>(parameters =>
        {
            parameters.Add(countdown => countdown.Duration, duration).Add(countdown => countdown.Href, "/sign-in")
                .Add(countdown => countdown.Text, "Going to sign in in {0}.");
            more?.Invoke(parameters);
        });

    private static string Text(IRenderedComponent<TbCountdown> cut) => cut.Find("[data-part=text]").TextContent;

    private static string State(IRenderedComponent<TbCountdown> cut) =>
        cut.Find(".tb-countdown").GetAttribute("data-state")!;

    // Moves the clock a second at a time, letting each tick render before the next.
    private async Task PassAsync(IRenderedComponent<TbCountdown> cut, int seconds)
    {
        for (var second = 0; second < seconds; second++)
        {
            _time.Advance(TimeSpan.FromSeconds(1));
            await cut.InvokeAsync(() => { });
        }
    }

    [Fact]
    public async Task TbCountdown_ShouldNavigate_WhenTimeRunsOut()
    {
        var cut = RenderRedirect(TimeSpan.FromSeconds(5));
        Assert.Equal("Going to sign in in 5 seconds.", Text(cut));

        await PassAsync(cut, 4);
        Assert.Equal(("Going to sign in in 1 second.", "running"), (Text(cut), State(cut)));
        Assert.DoesNotContain("/sign-in", Url);

        await PassAsync(cut, 1);

        Assert.EndsWith("/sign-in", Url);
        Assert.Equal("done", State(cut));
    }

    [Fact]
    public async Task TbCountdown_ShouldRunOnElapsedOnce_WhenGoNowIsPressed()
    {
        var ends = new List<CountdownEnd>();
        var cut = Render<TbCountdown>(parameters => parameters
            .Add(countdown => countdown.Duration, TimeSpan.FromMinutes(2))
            .Add(countdown => countdown.Text, "Signing out in {0}.")
            .Add(countdown => countdown.OnElapsed, (CountdownEnd end) => ends.Add(end)));
        Assert.Equal("Signing out in 2 minutes.", Text(cut));

        await cut.Tb().Button("Go now").ClickAsync();
        Assert.Equal([CountdownEnd.GoNow], ends);
        Assert.Equal("done", State(cut));
        await PassAsync(cut, 130);

        Assert.Equal([CountdownEnd.GoNow], ends);
    }

    [Fact]
    public async Task TbCountdown_ShouldNavigateAtOnce_WhenGoNowIsPressed()
    {
        var cut = RenderRedirect(TimeSpan.FromSeconds(30));

        await cut.Tb().Button("Go now").ClickAsync();

        Assert.EndsWith("/sign-in", Url);
    }

    [Fact]
    public async Task OnElapsed_ShouldGetTimeRanOut_WhenTimeRunsOut()
    {
        var ends = new List<CountdownEnd>();
        var cut = RenderRedirect(TimeSpan.FromSeconds(3), parameters =>
            parameters.Add(countdown => countdown.OnElapsed, (CountdownEnd end) => ends.Add(end)));

        await PassAsync(cut, 3);

        Assert.Equal([CountdownEnd.TimeRanOut], ends);
    }

    [Fact]
    public async Task OnElapsed_ShouldHandTheErrorToTheRenderer_WhenItThrowsAsTimeRunsOut()
    {
        var cut = RenderRedirect(TimeSpan.FromSeconds(3), parameters => parameters.Add(countdown => countdown.OnElapsed,
            (CountdownEnd _) => throw new InvalidOperationException("Sign-out failed")));

        await PassAsync(cut, 3);

        var exception = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(5),
            Xunit.TestContext.Current.CancellationToken);
        Assert.Equal("Sign-out failed", exception.Message);
    }

    [Theory]
    [InlineData(61, "2 minutes")]
    [InlineData(60, "1 minute")]
    [InlineData(59, "59 seconds")]
    public void Text_ShouldShowSecondsUnderAMinuteAndRoundUpToMinutes_WhenGivenTimeLeft(int seconds, string left)
    {
        var cut = RenderRedirect(TimeSpan.FromSeconds(seconds));

        Assert.Equal($"Going to sign in in {left}.", Text(cut));
    }

    [Fact]
    public async Task TbCountdown_ShouldShowTheNewMinute_WhenAMinuteBoundaryPasses()
    {
        var cut = RenderRedirect(TimeSpan.FromSeconds(125));
        Assert.Equal("Going to sign in in 3 minutes.", Text(cut));

        await PassAsync(cut, 5);
        Assert.Equal("Going to sign in in 2 minutes.", Text(cut));
        await PassAsync(cut, 60);
        Assert.Equal("Going to sign in in 1 minute.", Text(cut));
        await PassAsync(cut, 1);

        Assert.Equal("Going to sign in in 59 seconds.", Text(cut));
    }

    [Fact]
    public async Task TbCountdown_ShouldStop_WhenStayIsPressed()
    {
        var cut = RenderRedirect(TimeSpan.FromSeconds(5));

        await cut.Tb().Button("Stay on this page").ClickAsync();
        await PassAsync(cut, 10);

        Assert.Equal(("Stayed on this page", "stayed"), (Text(cut), State(cut)));
        Assert.Empty(cut.FindAll("button"));
        Assert.DoesNotContain("/sign-in", Url);
        JSInterop.VerifyFocusAsyncInvoke();
    }

    [Fact]
    public async Task TbCountdown_ShouldPause_WhenFocusOrThePointerIsInside()
    {
        var cut = RenderRedirect(TimeSpan.FromSeconds(5));
        var root = cut.Find(".tb-countdown");

        await PassAsync(cut, 1);
        await root.TriggerEventAsync("onfocusin", new FocusEventArgs());
        await root.TriggerEventAsync("onpointerenter", new PointerEventArgs());
        await PassAsync(cut, 10);
        Assert.Equal(("Going to sign in in 4 seconds.", "paused"), (Text(cut), State(cut)));
        await root.TriggerEventAsync("onfocusout", new FocusEventArgs());
        await PassAsync(cut, 10);
        // The pointer is still on it.
        Assert.Equal("Going to sign in in 4 seconds.", Text(cut));

        await root.TriggerEventAsync("onpointerleave", new PointerEventArgs());
        await PassAsync(cut, 3);

        Assert.Equal(("Going to sign in in 1 second.", "running"), (Text(cut), State(cut)));
        Assert.DoesNotContain("/sign-in", Url);
    }

    [Fact]
    public async Task TbCountdown_ShouldRestart_WhenExtended()
    {
        var extended = 0;
        var cut = RenderRedirect(TimeSpan.FromSeconds(30), parameters => parameters
            .Add(countdown => countdown.OnExtend, () => extended++)
            .Add(countdown => countdown.ExtendText, "Stay signed in"));
        await PassAsync(cut, 25);

        await cut.Tb().Button("Stay signed in").ClickAsync();

        Assert.Equal((1, "Going to sign in in 30 seconds."), (extended, Text(cut)));
        Assert.Equal(["Going to sign in in 30 seconds.", "Going to sign in in 30 seconds."], Announcer.Polite);
        await PassAsync(cut, 29);
        Assert.DoesNotContain("/sign-in", Url);
        await PassAsync(cut, 1);
        Assert.EndsWith("/sign-in", Url);
    }

    [Fact]
    public async Task TbCountdown_ShouldAnnounceOnce_WhenStarted()
    {
        var cut = RenderRedirect(TimeSpan.FromSeconds(30));

        await PassAsync(cut, 29);

        Assert.Equal(["Going to sign in in 30 seconds."], Announcer.Polite);
    }

    [Fact]
    public async Task TbCountdown_ShouldAnnounceAgainNearTheEnd_WhenLong()
    {
        var cut = RenderRedirect(TimeSpan.FromMinutes(2));

        await PassAsync(cut, 100);

        Assert.Equal(["Going to sign in in 2 minutes.", "Going to sign in in 20 seconds."], Announcer.Polite);
    }

    [Fact]
    public void TbCountdown_ShouldRenderLinkWithoutTimer_WhenStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderRedirect(TimeSpan.FromSeconds(5));
        _time.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal("/sign-in", cut.Find("a").GetAttribute("href"));
        Assert.Empty(cut.FindAll("button"));
        Assert.Equal("Going to sign in in 5 seconds.", Text(cut));
        Assert.DoesNotContain("/sign-in", Url);
    }

    [Fact]
    public void Render_ShouldThrow_WhenARequiredSettingIsMissing()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbCountdown>(parameters =>
            parameters.Add(countdown => countdown.Duration, TimeSpan.FromSeconds(5))));
        Assert.Throws<InvalidOperationException>(() => RenderRedirect(TimeSpan.FromSeconds(5), parameters =>
            parameters.Add(countdown => countdown.OnExtend, () => { })));
        Assert.Throws<ArgumentOutOfRangeException>(() => RenderRedirect(TimeSpan.Zero));
        // Without Stay or Extend, nothing would stop it.
        Assert.Throws<InvalidOperationException>(() => RenderRedirect(TimeSpan.FromSeconds(5), parameters =>
            parameters.Add(countdown => countdown.ShowStay, false)));
    }

    [Fact]
    public void ShowStay_ShouldLeaveGoNowAndExtend_WhenFalse()
    {
        var cut = RenderRedirect(TimeSpan.FromSeconds(30), parameters => parameters
            .Add(countdown => countdown.ShowStay, false).Add(countdown => countdown.OnExtend, () => { })
            .Add(countdown => countdown.ExtendText, "Stay signed in")
            .Add(countdown => countdown.Labels, new CountdownLabels { GoNow = "Sign out now" }));

        Assert.Equal(["Sign out now", "Stay signed in"], cut.FindAll("button").Select(button => button.TextContent));
    }

    [Fact]
    public async Task Telemetry_ShouldCountWhatTheUserDid_WhenNamed()
    {
        var meters = Services.GetRequiredService<IMeterFactory>();
        using var used = new MetricCollector<long>(meters, "Trailblazor", "trailblazor.control.used");
        var cut = RenderRedirect(TimeSpan.FromSeconds(5), parameters =>
            parameters.Add(countdown => countdown.TelemetryName, "reset-redirect"));

        await cut.Tb().Button("Stay on this page").ClickAsync();

        var use = Assert.Single(used.GetMeasurementSnapshot());
        Assert.Equal(("stay", "reset-redirect"),
            ((string?)use.Tags["trailblazor.action"], (string?)use.Tags["trailblazor.telemetry_name"]));
    }
}
