using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbTextField</c> or <c>TbCodeField</c>, found by its label or a locator.</summary>
public sealed class TbTextFieldLocator(ILocator scope, ILocator field) : TbFieldLocator
{
    private protected override ILocator Control => Input;

    private ILocator Input =>
        @field.And(scope.Locator(".tb-text-field input[data-part=input], .tb-text-field textarea[data-part=input], "
            + "input.tb-code-field__input"));

    private ILocator RevealButton =>
        Input.Locator("xpath=ancestor::*[@data-part='control'][1]").Locator("[data-part=reveal]");

    /// <summary>The text the field shows.</summary>
    public Task<string> TextAsync() => Input.InputValueAsync();

    /// <summary>Types this text into the field and leaves it, which commits it, and waits until the app has it. A
    /// statically rendered field just gets the text. A read-only or disabled field throws.</summary>
    public async Task EnterAsync(string text)
    {
        var input = Input;
        if (!await input.IsEditableAsync())
        {
            throw new InvalidOperationException("This field is read-only or disabled, so text can't be typed into it.");
        }

        if (await input.GetAttributeAsync("data-static") is not null)
        {
            await input.FillAsync(text);
            return;
        }

        // A prerendered field is replaced once the page is interactive, and would lose the text.
        await Assertions.Expect(input).ToHaveAttributeAsync("data-ready", "");
        await Commit.WaitAsync(input, async () =>
        {
            // A mask or a code field can change what's typed, so wait only when the fill changed the value.
            var before = await input.InputValueAsync();
            await input.FillAsync(text);
            var changes = await input.InputValueAsync() != before;
            await input.BlurAsync();
            return changes;
        });
    }

    /// <summary>Whether a <c>Revealable</c> password field shows its password.</summary>
    public async Task<bool> PasswordShownAsync() =>
        await RevealButton.CountAsync() > 0 && await RevealButton.GetAttributeAsync("aria-pressed") == "true";

    /// <summary>Shows the password with the field's show-password button, as a user does, unless it shows already, and
    /// waits until it shows. A field that isn't <c>Revealable</c>, or is read-only or disabled, throws.</summary>
    public Task ShowPasswordAsync() => RevealAsync(true);

    /// <summary>Hides the password with the field's show-password button, unless it's hidden already, and waits until
    /// it's hidden. It throws as <see cref="ShowPasswordAsync"/> does.</summary>
    public Task HidePasswordAsync() => RevealAsync(false);

    private async Task RevealAsync(bool shown)
    {
        var button = RevealButton;
        if (await button.CountAsync() == 0)
        {
            throw new InvalidOperationException(
                "The field has no show-password button; make it Revealable, with Type set to Password.");
        }

        if (await button.IsDisabledAsync())
        {
            throw new InvalidOperationException("The field is read-only or disabled, so its password stays hidden.");
        }

        if (await PasswordShownAsync() == shown)
        {
            return;
        }

        // The button does nothing until a prerendered page is interactive or a static page's script has loaded.
        await (await Input.GetAttributeAsync("data-static") is null
            ? Assertions.Expect(Input).ToHaveAttributeAsync("data-ready", "")
            : Assertions.Expect(Scope.Document(Input)).ToHaveAttributeAsync("data-tb-script", ""));
        await button.ClickAsync();
        await Assertions.Expect(button).ToHaveAttributeAsync("aria-pressed", shown ? "true" : "false");
    }
}
