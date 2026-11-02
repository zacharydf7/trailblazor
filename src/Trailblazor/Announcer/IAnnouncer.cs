namespace Trailblazor;

/// <summary>Speaks short messages to screen reader users through live regions that <see cref="TbRoot"/> and each
/// modal dialog render. Each message leaves the page seven seconds after it is announced, so browsing doesn't find
/// it later.</summary>
public interface IAnnouncer
{
    /// <summary>Announces a message once the screen reader finishes what it's saying, such as "Saved".</summary>
    void Polite(string message);

    /// <summary>Announces a message right away, interrupting the screen reader. Keep it for errors and other
    /// messages the user must hear now.</summary>
    void Assertive(string message);

    /// <summary>Removes every message with exactly this text, such as an error the user has fixed, so no one finds it
    /// later. It matches on text alone, whoever announced the message. Fields withdraw their own errors this way.
    /// </summary>
    void Withdraw(string message);

    /// <summary>Removes every message now, such as when the view they were about goes away.</summary>
    void Clear();
}
