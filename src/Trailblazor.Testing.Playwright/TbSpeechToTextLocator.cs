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
}
