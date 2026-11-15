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
    /// <c>OnValidSubmit</c> or <c>OnInvalidSubmit</c>. The wait also ends when handling it takes the form off the page.
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
        // The form is marked, so the wait also ends when the submit takes it off the page or replaces it.
        var mark = Guid.NewGuid().ToString("N");
        var before = await Form.EvaluateAsync<string>(
            "(form, mark) => { form.dataset.tbAwaited = mark; return form.dataset.submits; }", mark);
        await Form.EvaluateAsync(Submit);
        var waiting = Scope.Document(Form).Locator($"[data-tb-awaited='{mark}'][data-submits='{before}']");
        await Assertions.Expect(waiting).ToHaveCountAsync(0);
    }
}
