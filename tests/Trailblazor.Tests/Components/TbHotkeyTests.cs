using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbHotkeyTests : BunitContext
{
    private int _presses;

    public TbHotkeyTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldDrawAHiddenMarkerAndStartTheScriptWithItsKeysAndScope_WhenGivenKeys()
    {
        Render<TbHotkey>(parameters => parameters.Add(hotkey => hotkey.Keys, "Mod+S")
            .Add(hotkey => hotkey.Scope, HotkeyScope.Parent));

        var call = Assert.Single(JSInterop.Invocations["attachHotkey"]);
        Assert.Equal(["Mod+S", "Parent"], call.Arguments.Skip(1));
    }

    [Theory]
    [InlineData("Mod+", "can't read the keys")]
    [InlineData("Hyper+S", "can't read the keys")]
    [InlineData("g", "is a key people type")]
    [InlineData("Shift+?", "is a key people type")]
    [InlineData("Space", "is a key people type")]
    public void Render_ShouldThrow_WhenTheKeysCantBeReadOrAreACharacterKeyNotAllowed(string keys, string message)
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            Render<TbHotkey>(parameters => parameters.Add(hotkey => hotkey.Keys, keys)));

        Assert.Contains(message, error.Message);
    }

    [Theory]
    [InlineData("Escape")]
    [InlineData("Shift+Enter")]
    [InlineData("F2")]
    public void Render_ShouldAcceptANamedKeyWithoutLeave_WhenItIsntACharacterPeopleType(string keys)
    {
        var cut = Render<TbHotkey>(parameters => parameters.Add(hotkey => hotkey.Keys, keys));

        Assert.Equal(keys, cut.Find("[data-part=hotkey]").GetAttribute("data-keys"));
    }

    [Fact]
    public void Render_ShouldAcceptACharacterKey_WhenAllowed()
    {
        var cut = Render<TbHotkey>(parameters => parameters.Add(hotkey => hotkey.Keys, "?")
            .Add(hotkey => hotkey.AllowCharacterKey, true));

        Assert.Equal("?", cut.Find("[data-part=hotkey]").GetAttribute("data-keys"));
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    public async Task Press_ShouldRunOnPressUnlessDisabled_WhenTheScriptReportsTheKeys(bool disabled, int expected)
    {
        var cut = RenderHotkey(disabled);

        await cut.Find("[data-part=hotkey]").TriggerEventAsync("ontbhotkey", EventArgs.Empty);

        Assert.Equal(expected, _presses);
        Assert.Equal(disabled, cut.Find("[data-part=hotkey]").HasAttribute("data-disabled"));
    }

    [Fact]
    public void Render_ShouldStartTheScriptAgain_WhenTheKeysChange()
    {
        var cut = RenderHotkey(false);

        cut.Render(parameters => parameters.Add(hotkey => hotkey.Keys, "Mod+Shift+S"));
        cut.Render(parameters => parameters.Add(hotkey => hotkey.Keys, "Mod+Shift+S"));

        Assert.Equal(["Mod+S", "Mod+Shift+S"], JSInterop.Invocations["attachHotkey"].Select(call => call.Arguments[1]));
    }

    [Fact]
    public async Task Driver_ShouldPressTheHotkeyOrThrow_WhenFoundByItsKeys()
    {
        var cut = RenderHotkey(false);

        await cut.Tb().Hotkey("Mod+S").PressAsync();
        cut.Render(parameters => parameters.Add(hotkey => hotkey.Disabled, true));

        Assert.Equal(1, _presses);
        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Hotkey("Mod+S").PressAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Hotkey("Mod+K").PressAsync());
    }

    [Fact]
    public void Help_ShouldListTheDescribedHotkeysThatFireByGroup_WhenOpen()
    {
        var cut = Render(builder =>
        {
            // A group's hotkey first, so the order shown isn't the order they came in.
            Hotkey(builder, 0, "Mod+B", "Bold", "Editing");
            Hotkey(builder, 10, "Mod+S", "Save", null);
            Hotkey(builder, 20, "Mod+I", null, "Editing");
            Hotkey(builder, 30, "Mod+P", "Print", null, disabled: true);
            builder.OpenComponent<TbHotkeysHelp>(40);
            builder.AddComponentParameter(41, nameof(TbHotkeysHelp.Open), true);
            builder.AddComponentParameter(42, nameof(TbHotkeysHelp.Keys), "?");
            builder.CloseComponent();
        });

        var groups = cut.FindAll("[data-part=group]");
        Assert.Equal(["General", "Editing"], groups.Select(group => group.QuerySelector("caption")!.TextContent));
        Assert.Equal(["Save", "Show keyboard shortcuts"], Descriptions(groups[0]));
        Assert.Equal(["Bold"], Descriptions(groups[1]));
        Assert.Contains("Keyboard shortcuts", cut.Markup);
    }

    [Fact]
    public async Task Help_ShouldOpenAndClose_WhenItsOwnHotkeyIsPressed()
    {
        var changes = new List<bool>();
        var cut = Render<TbHotkeysHelp>(parameters => parameters.Add(help => help.Keys, "?")
            .Add(help => help.OpenChanged, open => changes.Add(open)));

        await cut.Tb().Hotkey("?").PressAsync();
        Assert.NotEmpty(cut.FindAll("[data-part=group]"));
        // The open dialog has its own hotkey for the same keys, as the page's don't fire under it.
        await cut.FindAll("[data-part=hotkey][data-keys='?']")[^1].TriggerEventAsync("ontbhotkey", EventArgs.Empty);

        Assert.Equal([true, false], changes);
    }

    private IRenderedComponent<TbHotkey> RenderHotkey(bool disabled) => Render<TbHotkey>(parameters => parameters
        .Add(hotkey => hotkey.Keys, "Mod+S").Add(hotkey => hotkey.Disabled, disabled)
        .Add(hotkey => hotkey.OnPress, () => _presses++));

    private static void Hotkey(RenderTreeBuilder builder, int sequence, string keys, string? description, string? group,
        bool disabled = false)
    {
        builder.OpenComponent<TbHotkey>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(TbHotkey.Keys), keys);
        builder.AddComponentParameter(sequence + 2, nameof(TbHotkey.Description), description);
        builder.AddComponentParameter(sequence + 3, nameof(TbHotkey.Group), group);
        builder.AddComponentParameter(sequence + 4, nameof(TbHotkey.Disabled), disabled);
        builder.CloseComponent();
    }

    private static IEnumerable<string> Descriptions(AngleSharp.Dom.IElement group) =>
        group.QuerySelectorAll("[data-part=row] td:last-child").Select(cell => cell.TextContent.Trim());
}
