using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbSpeechToText</c>, found by its button's <c>Label</c>.</summary>
public sealed class TbSpeechToTextLocator(ILocator scope, string label)
{
    /// <summary>The button, a toggle that is pressed while it listens.</summary>
    public ILocator Button => scope.Locator(".tb-speech-to-text [data-part=button]")
        .And(scope.Page.GetByRole(AriaRole.Button, new() { Name = label, Exact = true }));

    /// <summary>Whether the button shows, which it does where the browser recognizes speech.</summary>
    public Task<bool> IsAvailableAsync() => Button.IsVisibleAsync();

    /// <summary>Whether it is listening.</summary>
    public async Task<bool> IsListeningAsync() => await Button.GetAttributeAsync("aria-pressed") == "true";

    /// <summary>Presses the button to start listening, and waits until it is.</summary>
    public async Task StartAsync()
    {
        await Button.ClickAsync();
        await Assertions.Expect(Button).ToHaveAttributeAsync("aria-pressed", "true");
    }

    /// <summary>Presses the button to stop listening, and waits until it has.</summary>
    public async Task StopAsync()
    {
        await Button.ClickAsync();
        await Assertions.Expect(Button).ToHaveAttributeAsync("aria-pressed", "false");
    }

    /// <summary>Hands this phrase to the app as the browser would on hearing it, which raises <c>OnText</c>, and waits
    /// until the app has handled it. Without <c>Continuous</c> it then stops listening, as the browser does after one
    /// phrase, and this waits until it has.
    /// Needs <see cref="FakeSpeech.UseFakeSpeechAsync"/> on the page. Throws while it isn't listening; start it with
    /// <see cref="StartAsync"/> first.</summary>
    public async Task SayAsync(string text)
    {
        var notListening =
            new InvalidOperationException("The dictation button isn't listening; call StartAsync first.");
        if (!await IsListeningAsync())
        {
            throw notListening;
        }

        if (!await Button.EvaluateAsync<bool>("() => 'trailblazorFakeSpeech' in window"))
        {
            throw new InvalidOperationException("The page isn't using fake speech; call page.UseFakeSpeechAsync() "
                + "before the page opens the app, or reload the page after.");
        }

        // A button shows it's listening a moment before its recognition starts, and one stopped by another's start
        // shows it has stopped a moment later, so wait until the page has one recognition and one pressed button.
        var page = Scope.Document(Button);
        await Assertions.Expect(page).ToHaveAttributeAsync("data-tb-fake-listening", "1");
        await Assertions.Expect(page.Locator(".tb-speech-to-text [data-part=button][aria-pressed=true]"))
            .ToHaveCountAsync(1);
        var continuous = false;
        await Commit.CountedAsync(Button.Locator("xpath=ancestor::*[@data-heard][1]"), "data-heard", async () =>
        {
            continuous = await Button.EvaluateAsync<bool?>(
                "(button, text) => button.ariaPressed === 'true' ? trailblazorFakeSpeech.say(text) : null", text)
                ?? throw notListening;
            return true;
        });
        if (!continuous)
        {
            await Assertions.Expect(Button).ToHaveAttributeAsync("aria-pressed", "false");
        }
    }
}
