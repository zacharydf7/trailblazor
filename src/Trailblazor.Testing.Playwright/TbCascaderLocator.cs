using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbCascader</c>, found by its label. Its choices are named by their text.</summary>
public sealed class TbCascaderLocator(ILocator scope, ILocator field) : TbFieldLocator
{
    private protected override ILocator Control => @field.And(scope.Locator("input"));

    /// <summary>The text the field shows: the chosen path, or what was typed.</summary>
    public Task<string> TextAsync() => Control.InputValueAsync();

    /// <summary>The paths that match what was typed, as the field shows them.</summary>
    public async Task<IReadOnlyList<string>> ResultsAsync() =>
        await (await PopupAsync()).Locator("[data-part=results] [data-part=option]").EvaluateAllAsync<string[]>(
            "options => options.map(option => option.textContent.trim())");

    /// <summary>Chooses a path a level at a time, as a user clicks through the lists: the choice with each text in
    /// turn, such as <c>ChooseAsync("Canada", "Ontario", "Toronto")</c>. Waits until the app has heard each choice.
    /// </summary>
    public async Task ChooseAsync(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        var popup = await OpenAsync();
        for (var level = 0; level < path.Length; level++)
        {
            var choice = popup.Locator($"[data-part=column][data-level='{level}'] [data-part=option]").Filter(new()
            {
                Has = popup.Page.Locator(".tb-cascader__text",
                    new() { HasTextRegex = new Regex($@"^\s*{Regex.Escape(path[level])}\s*$") }),
            });
            if (await choice.CountAsync() == 0)
            {
                throw new InvalidOperationException($"Level {level + 1} has no choice \"{path[level]}\".");
            }

            if (await choice.GetAttributeAsync("aria-disabled") == "true")
            {
                throw new InvalidOperationException($"\"{path[level]}\" is disabled.");
            }

            await Commit.CountedAsync(popup, "data-handled", async () =>
            {
                await choice.ClickAsync();
                return true;
            });
        }
    }

    /// <summary>Types the text, as a user does, which searches every level for paths that hold it, and waits for the
    /// results; read them with <see cref="ResultsAsync"/>.</summary>
    public async Task SearchAsync(string text)
    {
        var popup = await OpenAsync();
        await Commit.CountedAsync(popup, "data-handled", async () =>
        {
            await Control.FillAsync(text);
            return true;
        });
    }

    /// <summary>Types the text, then chooses the matching path shown as <paramref name="result"/>, such as
    /// <c>"Canada / Ontario / Toronto"</c>, and waits until the app has heard it.</summary>
    public async Task ChooseResultAsync(string text, string result)
    {
        await SearchAsync(text);
        var popup = await PopupAsync();
        await Commit.CountedAsync(popup, "data-handled", async () =>
        {
            await popup.Locator("[data-part=results]").GetByRole(AriaRole.Option, new() { Name = result, Exact = true })
                .ClickAsync();
            return true;
        });
    }

    // A click on the field opens its lists once its script has attached, which data-ready marks.
    private async Task<ILocator> OpenAsync()
    {
        if (await Control.GetAttributeAsync("data-static") is not null)
        {
            throw new InvalidOperationException("A cascader on a static page is a plain input that posts with its "
                + "form; render it on an interactive page to choose.");
        }

        await Assertions.Expect(Control).ToHaveAttributeAsync("data-ready", "");
        var popup = await PopupAsync();
        if (!await popup.EvaluateAsync<bool>("popup => popup.matches(':popover-open')"))
        {
            await Control.ClickAsync();
        }

        return popup;
    }

    private async Task<ILocator> PopupAsync() =>
        Scope.Document(scope).Locator($"[id='{await Control.GetAttributeAsync("aria-controls")}']");
}
