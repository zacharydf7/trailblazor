using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbForm</c>, found by its name.</summary>
public sealed class TbFormLocator(ILocator scope, string? name)
{
    /// <summary>The form element.</summary>
    public ILocator Form => (name is null ? scope.Locator("form.tb-form")
        : scope.GetByRole(AriaRole.Form, new() { Name = name, Exact = true }).And(scope.Locator("form.tb-form")));

    /// <summary>The messages the form's fields show, in order.</summary>
    public async Task<IReadOnlyList<string>> ErrorsAsync()
    {
        await Form.WaitForAsync();
        return [.. (await Form.Locator("[data-part=error] [data-part=message]").AllTextContentsAsync())
            .Select(text => text.Trim())];
    }

    /// <summary>Submits the form, as its submit button does, once the page is interactive, and waits until the form
    /// has handled it: raised <c>OnSubmit</c> when it has one, or else checked the fields and raised
    /// <c>OnValidSubmit</c> or <c>OnInvalidSubmit</c>. The wait also ends when handling it takes the form off the page,
    /// and when a dialog opens, since a handler that awaits the dialog's answer finishes only once the test answers it.
    /// A disabled form ignores it. A statically rendered form posts with the browser, without the wait.</summary>
    public async Task SubmitAsync()
    {
        await Form.WaitForAsync();
        const string Submit = "form => form.requestSubmit()";
        if (await Form.GetAttributeAsync("data-static") is not null)
        {
            await Form.EvaluateAsync(Submit);
            return;
        }

        // A prerendered form is replaced once the page is interactive, and would lose the submit.
        await Assertions.Expect(Form).ToHaveAttributeAsync("data-submits", new Regex(@"^\d+$"));
        await Commit.CountedAsync(Form, "data-submits", async () =>
        {
            await Form.EvaluateAsync(Submit);
            return true;
        });
    }
}
