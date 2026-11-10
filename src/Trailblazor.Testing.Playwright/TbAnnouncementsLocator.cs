using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>What Trailblazor's components say to screen readers, read from the live regions of <c>TbRoot</c> and of
/// each open modal dialog. A message stays there for seven seconds after it is announced, unless five newer
/// messages of its kind push it out first.</summary>
public sealed class TbAnnouncementsLocator(ILocator scope)
{
    /// <summary>Every message showing, polite and assertive, for assertions such as
    /// <c>Expect(announcements.Messages).ToContainTextAsync(["3 results"])</c>.</summary>
    public ILocator Messages => scope.Locator("[data-part=announcer] p");

    /// <summary>The polite messages showing, said once the screen reader finishes what it's saying.</summary>
    public ILocator Polite => scope.Locator("[data-part=announcer] [data-part=polite] p");

    /// <summary>The assertive messages showing, said right away.</summary>
    public ILocator Assertive => scope.Locator("[data-part=announcer] [data-part=assertive] p");

    /// <summary>Waits until a message containing this text is announced, politely or assertively.</summary>
    public Task ExpectAnnouncedAsync(string text) =>
        Assertions.Expect(Messages.Filter(new() { HasText = text }).First).ToBeAttachedAsync();
}
