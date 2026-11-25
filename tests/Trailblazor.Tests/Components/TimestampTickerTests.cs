using Microsoft.Extensions.Time.Testing;

namespace Trailblazor.Tests.Components;

// The one timer every relative timestamp in a circuit shares.
public class TimestampTickerTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
    private readonly List<string> _ticks = [];

    [Fact]
    public void Wait_ShouldFireTheEarlierOwnerFirst_WhenItRegistersSecond()
    {
        using var ticker = new TimestampTicker(_time);
        ticker.Wait("hours", TimeSpan.FromMinutes(5), () => _ticks.Add("hours"));
        ticker.Wait("minutes", TimeSpan.FromSeconds(30), () => _ticks.Add("minutes"));

        _time.Advance(TimeSpan.FromSeconds(30));

        Assert.Equal(["minutes"], _ticks);
    }

    [Fact]
    public void Stop_ShouldKeepTheTickFromFiring_WhenCalledBeforeItIsDue()
    {
        using var ticker = new TimestampTicker(_time);
        ticker.Wait("gone", TimeSpan.FromSeconds(30), () => _ticks.Add("gone"));

        ticker.Stop("gone");
        _time.Advance(TimeSpan.FromMinutes(1));

        Assert.Empty(_ticks);
    }

    [Fact]
    public void Fire_ShouldCallEveryDueOwnerOnce_WhenTwoAreDueTogether()
    {
        using var ticker = new TimestampTicker(_time);
        ticker.Wait("first", TimeSpan.FromSeconds(30), () => _ticks.Add("first"));
        // Registered ten seconds later with the same wait, it still comes due on the same tick.
        _time.Advance(TimeSpan.FromSeconds(10));
        ticker.Wait("second", TimeSpan.FromSeconds(30), () => _ticks.Add("second"));

        _time.Advance(TimeSpan.FromSeconds(20));

        Assert.Equal(["first", "second"], _ticks.Order());
    }
}
