namespace Trailblazor;

/// <summary>Speaks short messages to screen reader users through live regions that <see cref="TbRoot"/> and each
/// modal dialog render.</summary>
public interface IAnnouncer
{
    /// <summary>Announces a message once the screen reader finishes what it's saying, such as "Saved".</summary>
    void Polite(string message);

    /// <summary>Announces a message right away, interrupting the screen reader. Keep it for errors and other
    /// messages the user must hear now.</summary>
    void Assertive(string message);
}
