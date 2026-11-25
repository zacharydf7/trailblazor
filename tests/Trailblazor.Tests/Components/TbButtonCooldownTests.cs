using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// After a click whose OnClick finishes, a button with a Cooldown stays off for that long.
public class TbButtonCooldownTests : BunitContext
{
    private readonly FakeTimeProvider _time = new();
    private int _calls;

    public TbButtonCooldownTests()
    {
        Services.AddSingleton<TimeProvider>(_time);
        this.AddTrailblazor();
    }

    private IRenderedComponent<TbButton> RenderResend(TimeSpan cooldown, Func<Task>? work = null) =>
        Render<TbButton>(parameters => parameters.Add(button => button.Cooldown, cooldown)
            .Add(button => button.OnClick, () =>
            {
                _calls++;
                return work?.Invoke() ?? Task.CompletedTask;
            }).AddChildContent("Resend code"));

    private static string? Left(IRenderedComponent<TbButton> cut) =>
        cut.FindAll("[data-part=cooldown]").SingleOrDefault()?.TextContent;

    [Fact]
    public async Task Click_ShouldTurnTheButtonOffAndCountDown_WhenItHasACooldown()
    {
        var cut = RenderResend(TimeSpan.FromSeconds(30));

        await cut.Find("button").ClickAsync(new MouseEventArgs());

        var button = cut.Find("button");
        Assert.Equal(("true", "(30)"), (button.GetAttribute("aria-disabled"), Left(cut)));
        Assert.Equal("Available again in 30 seconds",
            cut.Find($"#{button.GetAttribute("aria-describedby")}").TextContent);
        Assert.False(button.HasAttribute("disabled"));
        _time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal("(29)", Left(cut));
        await cut.Find("button").ClickAsync(new MouseEventArgs());
        Assert.Equal(1, _calls);
        // It's read out once, as it starts, not with each second.
        Assert.Equal(["Available again in 30 seconds"], Services.GetRequiredService<FakeAnnouncer>().Polite);

        _time.Advance(TimeSpan.FromSeconds(29));

        Assert.Null(Left(cut));
        Assert.Equal((null, null), (cut.Find("button").GetAttribute("aria-disabled"),
            cut.Find("button").GetAttribute("aria-describedby")));
        await cut.Find("button").ClickAsync(new MouseEventArgs());
        Assert.Equal(2, _calls);
    }

    [Fact]
    public async Task Button_ShouldKeepItsNameAndTheAppsDescription_WhenItCoolsDown()
    {
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.Cooldown, TimeSpan.FromSeconds(2))
            .AddUnmatched("aria-describedby", "code-hint").AddChildContent("Resend code"));

        await cut.Tb().Button("Resend code").ClickAsync();

        // The description is hidden, so the name is still the text, and the app's own description stays first.
        Assert.False(cut.Tb().Button("Resend code").Busy);
        Assert.Equal("true", cut.Find("button").GetAttribute("aria-disabled"));
        Assert.StartsWith("code-hint ", cut.Find("button").GetAttribute("aria-describedby"));
        _time.Advance(TimeSpan.FromSeconds(2));
        Assert.Equal("code-hint", cut.Find("button").GetAttribute("aria-describedby"));
    }

    [Fact]
    public async Task Click_ShouldEndTheCooldownOnTime_WhenItIsntWholeSeconds()
    {
        var cut = RenderResend(TimeSpan.FromMilliseconds(1500));

        await cut.Find("button").ClickAsync(new MouseEventArgs());
        _time.Advance(TimeSpan.FromMilliseconds(1499));
        Assert.Equal("(1)", Left(cut));
        _time.Advance(TimeSpan.FromMilliseconds(1));

        Assert.Null(Left(cut));
    }

    [Fact]
    public async Task Labels_ShouldWordTheSecondsAndTheDescription_WhenGiven()
    {
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.Cooldown, TimeSpan.FromSeconds(5))
            .Add(button => button.Labels, new ButtonLabels { CooldownLeft = "{0} s", Cooldown = "Wait {0} s" })
            .AddChildContent("Resend code"));

        await cut.Find("button").ClickAsync(new MouseEventArgs());

        Assert.Equal("5 s", Left(cut));
        Assert.Equal(["Wait 5 s"], Services.GetRequiredService<FakeAnnouncer>().Polite);
    }

    [Fact]
    public async Task Click_ShouldTakeTheSpinnerAway_WhenACooldownStartsAfterASlowClick()
    {
        var work = new TaskCompletionSource();
        var cut = RenderResend(TimeSpan.FromSeconds(30), () => work.Task);

        var click = cut.Find("button").ClickAsync(new MouseEventArgs());
        _time.Advance(TimeSpan.FromMilliseconds(250));
        Assert.Single(cut.FindAll("[data-part=spinner]"));
        await cut.InvokeAsync(work.SetResult);
        await click;

        Assert.Empty(cut.FindAll("[data-part=spinner]"));
        Assert.Equal("(30)", Left(cut));
    }

    [Fact]
    public async Task Click_ShouldSayTheSecondsLeft_WhenTheButtonIsFocusedLater()
    {
        var cut = RenderResend(TimeSpan.FromSeconds(30));

        await cut.Find("button").ClickAsync(new MouseEventArgs());
        _time.Advance(TimeSpan.FromSeconds(25));

        Assert.Equal("Available again in 5 seconds",
            cut.Find($"#{cut.Find("button").GetAttribute("aria-describedby")}").TextContent);
    }

    [Fact]
    public async Task Click_ShouldSayOneSecond_WhenTheCooldownIsASecond()
    {
        var cut = RenderResend(TimeSpan.FromSeconds(1));

        await cut.Find("button").ClickAsync(new MouseEventArgs());

        Assert.Equal(["Available again in 1 second"], Services.GetRequiredService<FakeAnnouncer>().Polite);
    }

    [Fact]
    public async Task Click_ShouldNotCoolDown_WhenTheHandlerThrew()
    {
        var cut = RenderResend(TimeSpan.FromSeconds(30),
            () => Task.FromException(new InvalidOperationException("Down")));

        // bUnit hands the handler's exception to the renderer, not to the click.
        await Record.ExceptionAsync(() => cut.Find("button").ClickAsync(new MouseEventArgs()));

        Assert.Null(Left(cut));
        Assert.Null(cut.Find("button").GetAttribute("aria-disabled"));
    }

    [Fact]
    public async Task Click_ShouldCoolDown_WhenTheButtonIsNotBusyWhileItRuns()
    {
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.Cooldown, TimeSpan.FromSeconds(30))
            .Add(button => button.BusyWhileRunning, false).Add(button => button.OnClick, () => _calls++)
            .AddChildContent("Resend code"));

        await cut.Find("button").ClickAsync(new MouseEventArgs());

        Assert.Equal("(30)", Left(cut));
    }

    [Fact]
    public async Task Click_ShouldNotCoolDown_WhenTheButtonIsAToggle()
    {
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.Cooldown, TimeSpan.FromSeconds(30))
            .Add(button => button.PressedChanged, (bool _) => _calls++).AddChildContent("Bold"));

        await cut.Find("button").ClickAsync(new MouseEventArgs());
        await cut.Find("button").ClickAsync(new MouseEventArgs());

        Assert.Equal(2, _calls);
    }

    [Fact]
    public async Task Click_ShouldNotCoolDown_WhenTheButtonIsALink()
    {
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.Cooldown, TimeSpan.FromSeconds(30))
            .Add(button => button.Href, "/codes").Add(button => button.OnClick, () => _calls++)
            .AddChildContent("Codes"));

        await cut.Find("a").ClickAsync(new MouseEventArgs());
        await cut.Find("a").ClickAsync(new MouseEventArgs());

        Assert.Equal(2, _calls);
    }
}
