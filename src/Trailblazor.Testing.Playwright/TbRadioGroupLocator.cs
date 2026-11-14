using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbRadioGroup</c>, found by its label. Its options are named by their text.</summary>
public sealed class TbRadioGroupLocator(ILocator scope, string label) : TbFieldLocator
{
    private protected override ILocator Control => Group;

    private ILocator Group => scope.GetByRole(AriaRole.Radiogroup, new() { NameRegex = FieldLabel.Matching(label) })
        .And(scope.Locator("fieldset.tb-radio-group"));

    /// <summary>The chosen option's name, or null when none is chosen.</summary>
    public async Task<string?> ChosenAsync() =>
        (await Group.EvaluateAsync<string[]>(Choices.CheckedNames)).SingleOrDefault();

    /// <summary>Chooses the option with this name and waits until the app has it. A statically rendered group is
    /// chosen the native way. A read-only or disabled group or option throws.</summary>
    public async Task ChooseAsync(string option)
    {
        if (await Group.GetAttributeAsync("aria-readonly") == "true")
        {
            throw new InvalidOperationException("This group is read-only, so its options can't be chosen.");
        }

        await Ticks.SetAsync(Group.GetByRole(AriaRole.Radio, new() { Name = option, Exact = true }), on: true);
    }
}
