using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Time.Testing;

namespace Trailblazor.Tests.Components;

// A button counts as busy while the task its OnClick returned is still running.
public class TbButtonBusyTests : BunitContext
{
    private readonly FakeTimeProvider _time = new();
    private readonly TaskCompletionSource _work = new();
    private int _calls;

    public TbButtonBusyTests()
    {
        Services.AddSingleton<TimeProvider>(_time);
    }

    private IRenderedComponent<TbButton> RenderSave(Action<ComponentParameterCollectionBuilder<TbButton>>? more = null,
        Action<TrailblazorOptions>? configure = null)
    {
        this.AddTrailblazor(configure);
        return Render<TbButton>(parameters =>
        {
            parameters.Add(button => button.OnClick, () =>
            {
                _calls++;
                return _work.Task;
            }).AddChildContent("Save");
            more?.Invoke(parameters);
        });
    }

    private static Task ClickAsync(IRenderedComponent<TbButton> cut) =>
        cut.Find("button").ClickAsync(new MouseEventArgs());

    [Fact]
    public async Task Click_ShouldCallOnClickOnce_WhenClickedTwiceWhileTheHandlerRuns()
    {
        var cut = RenderSave();

        var first = ClickAsync(cut);
        // While the click runs, the button stops a second one's default, so it submits no form twice either. That
        // starts a moment after the click, so the click's own default, such as its form's submit, still happens.
        cut.WaitForAssertion(() => Assert.True(cut.Find("button").HasAttribute("blazor:onclick:preventDefault")));
        var second = ClickAsync(cut);
        Assert.Equal(1, _calls);
        await cut.InvokeAsync(_work.SetResult);
        await Task.WhenAll(first, second);

        await ClickAsync(cut);
        Assert.Equal(2, _calls);
    }

    [Fact]
    public async Task Click_ShouldShowNoSpinner_WhenTheHandlerFinishesWithinTheShowDelay()
    {
        var cut = RenderSave();

        var click = ClickAsync(cut);
        _time.Advance(TimeSpan.FromMilliseconds(199));
        await cut.InvokeAsync(_work.SetResult);
        await click;
        _time.Advance(TimeSpan.FromSeconds(1));

        Assert.Empty(cut.FindAll("[data-part=spinner]"));
        Assert.Null(cut.Find("button").GetAttribute("aria-busy"));
    }

    [Fact]
    public async Task Click_ShouldKeepTheSpinnerForItsMinimum_WhenTheHandlerFinishesJustAfterTheShowDelay()
    {
        var cut = RenderSave();

        var click = ClickAsync(cut);
        _time.Advance(TimeSpan.FromMilliseconds(200));
        Assert.Single(cut.FindAll("[data-part=spinner]"));
        Assert.Equal(("true", "true"), (cut.Find("button").GetAttribute("aria-busy"),
            cut.Find("button").GetAttribute("aria-disabled")));
        _time.Advance(TimeSpan.FromMilliseconds(50));
        await cut.InvokeAsync(_work.SetResult);
        await click;

        // Shown for 50 of its 400 milliseconds, it stays, and the button still ignores clicks.
        _time.Advance(TimeSpan.FromMilliseconds(349));
        Assert.Single(cut.FindAll("[data-part=spinner]"));
        await ClickAsync(cut);
        Assert.Equal(1, _calls);
        _time.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Empty(cut.FindAll("[data-part=spinner]"));
        Assert.False(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Click_ShouldAcceptClicksAgain_WhenTheHandlerThrew()
    {
        var cut = RenderSave();

        var click = ClickAsync(cut);
        _time.Advance(TimeSpan.FromMilliseconds(200));
        await cut.InvokeAsync(() => _work.SetException(new InvalidOperationException("Offline")));
        // bUnit hands the handler's exception to the renderer, not to the click.
        await Record.ExceptionAsync(() => click);
        _time.Advance(TimeSpan.FromMilliseconds(400));

        // bUnit dispatches no more events once a handler has thrown, so the button's state shows it would take one:
        // no spinner, and no longer stopping a click's default as it does while a click runs.
        Assert.Empty(cut.FindAll("[data-part=spinner]"));
        Assert.Equal((null, false), (cut.Find("button").GetAttribute("aria-busy"),
            cut.Find("button").HasAttribute("blazor:onclick:preventDefault")));
    }

    [Fact]
    public async Task Click_ShouldCallOnClickTwice_WhenBusyWhileRunningIsOff()
    {
        var cut = RenderSave(more: parameters => parameters.Add(button => button.BusyWhileRunning, false));

        _ = ClickAsync(cut);
        _ = ClickAsync(cut);
        _time.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(2, _calls);
        Assert.Empty(cut.FindAll("[data-part=spinner]"));
    }

    [Fact]
    public void Click_ShouldCallOnClickTwice_WhenTheAppTurnsBusyWhileRunningOff()
    {
        var cut = RenderSave(configure: options => options.Buttons.BusyWhileRunning = false);

        _ = ClickAsync(cut);
        _ = ClickAsync(cut);

        Assert.Equal(2, _calls);
    }

    [Fact]
    public void Click_ShouldToggleTwice_WhenPressedIsBoundAndClickedTwice()
    {
        this.AddTrailblazor();
        var pressed = new List<bool>();
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.PressedChanged, (bool now) =>
        {
            pressed.Add(now);
            return _work.Task;
        }).AddChildContent("Bold"));

        _ = cut.Find("button").ClickAsync(new MouseEventArgs());
        _ = cut.Find("button").ClickAsync(new MouseEventArgs());

        Assert.Equal([true, false], pressed);
    }

    [Fact]
    public void Click_ShouldCallOnClickTwice_WhenTheButtonIsALink()
    {
        var cut = RenderSave(more: parameters => parameters.Add(button => button.Href, "/trails"));

        _ = cut.Find("a").ClickAsync(new MouseEventArgs());
        _ = cut.Find("a").ClickAsync(new MouseEventArgs());

        Assert.Equal(2, _calls);
    }

    [Fact]
    public void Click_ShouldCountAClickIgnoredWhileBusy_WhenTheButtonHasATelemetryName()
    {
        var cut = RenderSave(more: parameters => parameters.Add(button => button.TelemetryName, "save-trip"));
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");

        _ = ClickAsync(cut);
        _ = ClickAsync(cut);

        var actions = used.GetMeasurementSnapshot().Select(measurement => measurement.Tags.GetValueOrDefault(
            "trailblazor.action")).ToList();
        Assert.Equal([null, "ignored_busy"], actions);
    }
}
