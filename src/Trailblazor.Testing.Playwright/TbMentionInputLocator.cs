using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbMentionInput</c>, found by its label or a locator.</summary>
public sealed class TbMentionInputLocator(ILocator scope, ILocator field) : TbFieldLocator
{
    private protected override ILocator Control => Input;

    /// <summary>The field's text area or input, for typing into and reading its text.</summary>
    public ILocator Input => @field.And(scope.Locator(".tb-mention-input__input"));

    private ILocator Listbox => scope.Page.Locator(".tb-mention-input__listbox:popover-open");

    /// <summary>The text the field shows.</summary>
    public Task<string> TextAsync() => Input.InputValueAsync();

    /// <summary>The names of the suggestions in the open list, in order, or none while it's closed.</summary>
    public async Task<IReadOnlyList<string>> SuggestionsAsync() =>
        await Listbox.CountAsync() == 0 ? [] : await Listbox.EvaluateAsync<string[]>(Choices.OptionNames);

    /// <summary>Types this text, as a user does with the caret at its end. When it ends with a trigger and the start of
    /// a mention, such as <c>"Thanks @an"</c>, this waits for the suggestions.</summary>
    public async Task TypeAsync(string text)
    {
        await Assertions.Expect(Input).ToHaveAttributeAsync("data-ready", "");
        var listbox = scope.Page.Locator($"[id='{await Input.GetAttributeAsync("aria-controls")}']");
        var searched = await listbox.GetAttributeAsync("data-results-for");
        await Input.FillAsync(text);
        if (EndsWithMention(text, await Input.GetAttributeAsync("data-triggers") ?? "@"))
        {
            await Assertions.Expect(listbox).Not.ToHaveAttributeAsync("data-results-for", searched ?? "");
        }
    }

    // Whether a trigger starts the last word, as the field's script finds one before the caret.
    private static bool EndsWithMention(string text, string triggers)
    {
        for (var start = text.Length - 1; start >= 0 && text.Length - start <= 50; start--)
        {
            if (char.IsWhiteSpace(text[start]))
            {
                return false;
            }

            if (triggers.Contains(text[start]) && (start == 0 || char.IsWhiteSpace(text[start - 1])
                || "([{\"'".Contains(text[start - 1])))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Chooses the suggestion with this name in the open list, as clicking it does, and waits until the field
    /// shows the mention. Type the start of the mention first with <see cref="TypeAsync"/>.</summary>
    public async Task ChooseAsync(string suggestion)
    {
        var option = Listbox.GetByRole(AriaRole.Option, new() { Name = suggestion, Exact = true });
        var insert = await option.GetAttributeAsync("data-insert");
        await option.ClickAsync();
        await Assertions.Expect(Input).ToHaveValueAsync(new System.Text.RegularExpressions.Regex(
            System.Text.RegularExpressions.Regex.Escape(insert + " ")));
    }
}
