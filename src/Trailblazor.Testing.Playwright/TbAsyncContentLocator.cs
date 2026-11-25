using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbAsyncContent</c>, found by its <c>Label</c>, such as "groups", or the only one in the scope.
/// </summary>
public sealed class TbAsyncContentLocator(ILocator scope, string? label)
{
    /// <summary>The region.</summary>
    public ILocator Region => label is null ? scope.Locator(".tb-async-content")
        : scope.Locator(".tb-async-content").And(scope.Locator($"[data-label='{label.Replace("'", "\\'")}']"));

    /// <summary>What it shows: loading, failed, empty, no matches or the content. A reload keeps the state of what it
    /// shows; <see cref="BusyAsync"/> says it's loading.</summary>
    public async Task<AsyncContentState> StateAsync()
    {
        var state = (await Region.GetAttributeAsync("data-state"))!;
        return Enum.Parse<AsyncContentState>(state.Replace("-", ""), ignoreCase: true);
    }

    /// <summary>Whether a load is running, a first load or a reload.</summary>
    public async Task<bool> BusyAsync() =>
        await StateAsync() == AsyncContentState.Loading || await Region.GetAttributeAsync("aria-busy") == "true";

    /// <summary>Waits until no load is running and the first one has finished, loaded or failed.</summary>
    public async Task WaitUntilLoadedAsync()
    {
        await Assertions.Expect(Region).Not.ToHaveAttributeAsync("data-state", "loading");
        await Assertions.Expect(Region).Not.ToHaveAttributeAsync("aria-busy", "true");
    }

    /// <summary>Presses the Retry button a failed load shows, and returns once the load it starts has finished. Throws
    /// when no Retry button shows: the load hasn't failed, or its <c>FailedContent</c> has buttons of its own.
    /// </summary>
    public async Task RetryAsync()
    {
        var failed = Region.Locator(":scope > [data-part=failed]");
        var retry = failed.Locator("[data-part=retry]");
        if (await retry.CountAsync() == 0)
        {
            throw new InvalidOperationException(await StateAsync() == AsyncContentState.Failed
                ? "The failure shows no Retry button; its FailedContent has buttons of its own."
                : "No load has failed, so there's no Retry button.");
        }

        // The button's own helper waits until its OnClick, the load, has run.
        await new TbButtonLocator(failed, (await retry.TextContentAsync())!.Trim()).ClickAsync();
    }
}
