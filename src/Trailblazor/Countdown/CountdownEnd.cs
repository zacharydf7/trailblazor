namespace Trailblazor;

/// <summary>Why a <see cref="TbCountdown"/> acted, which its <see cref="TbCountdown.OnElapsed"/> gets.</summary>
public enum CountdownEnd
{
    /// <summary>The time ran out. Focus wasn't in the countdown, which pauses while it is.</summary>
    TimeRanOut,

    /// <summary>The user pressed Go now, whose button had focus and goes with the countdown.</summary>
    GoNow,
}
