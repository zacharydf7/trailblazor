namespace Trailblazor;

/// <summary>Speaks short messages to screen reader users through live regions that <see cref="TbRoot"/> and each
/// modal dialog render. Each message leaves the page seven seconds after it is announced, so browsing doesn't find
/// it later.</summary>
public interface IAnnouncer
{
    /// <summary>Announces a message once the screen reader finishes what it's saying, such as "Saved".</summary>
    /// <returns>A handle to withdraw this message alone.</returns>
    AnnouncementHandle Polite(string message);

    /// <summary>Announces a message right away, interrupting the screen reader. Keep it for errors and other
    /// messages the user must hear now.</summary>
    /// <returns>A handle to withdraw this message alone.</returns>
    AnnouncementHandle Assertive(string message);

    /// <summary>Removes the message this handle stands for, such as an error the user has fixed, so no one finds it
    /// later. Other messages with the same text stay. Fields withdraw their own errors this way.</summary>
    void Withdraw(AnnouncementHandle handle);

    /// <summary>Removes every message with exactly this text, whoever announced it.</summary>
    void Withdraw(string message);

    /// <summary>Removes every message now, such as when the view they were about goes away.</summary>
    void Clear();
}
