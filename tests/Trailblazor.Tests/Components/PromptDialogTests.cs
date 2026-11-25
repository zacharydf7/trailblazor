using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class PromptDialogTests : BunitContext
{
    public PromptDialogTests() => this.AddTrailblazor();

    private IDialogService Dialogs => Services.GetRequiredService<IDialogService>();

    private static string? AtLeastThree(string name) => name.Length < 3 ? "Use at least 3 letters." : null;

    [Fact]
    public async Task PromptAsync_ShouldReturnTheText_WhenConfirmed()
    {
        var root = Render<TbRoot>();
        var asking = Dialogs.PromptAsync("Rename trail", "Name", "Cedar");
        var name = root.Tb().TextField("Name");
        Assert.Equal("Cedar", name.Text);

        await name.EnterAsync("Cedar Flats");
        await root.Tb().Dialog("Rename trail").ChooseAsync("OK");

        Assert.Equal("Cedar Flats", await asking);
    }

    [Fact]
    public async Task PromptAsync_ShouldStayOpenAndSayWhy_WhenTheCheckRefusesTheText()
    {
        var root = Render<TbRoot>();
        var asking = Dialogs.PromptAsync("Rename trail", "Name", validate: AtLeastThree);
        var name = root.Tb().TextField("Name");
        await name.EnterAsync("Ox");

        await root.Tb().Dialog("Rename trail").ChooseAsync("OK");

        Assert.True(root.Tb().Dialog("Rename trail").IsOpen);
        Assert.Equal("Use at least 3 letters.", name.Error);
        await name.EnterAsync("Oxbow");
        // Editing the text clears the message until it's submitted again.
        Assert.Null(name.Error);
        await root.Tb().Dialog("Rename trail").ChooseAsync("OK");
        Assert.Equal("Oxbow", await asking);
    }

    [Fact]
    public async Task PromptAsync_ShouldSubmit_WhenEnterIsPressedInTheField()
    {
        var root = Render<TbRoot>();
        var asking = Dialogs.PromptAsync("Rename trail", "Name", "Ridge loop");

        // The field's script raises tbenter for Enter; bUnit raises it directly.
        await root.Find("input").TriggerEventAsync("ontbenter", EventArgs.Empty);

        Assert.Equal("Ridge loop", await asking);
    }

    [Fact]
    public async Task PromptAsync_ShouldReturnNull_WhenCanceled()
    {
        var root = Render<TbRoot>();
        var asking = Dialogs.PromptAsync("Rename trail", "Name", "Cedar");

        await root.Tb().Dialog("Rename trail").ChooseAsync("Cancel");

        Assert.Null(await asking);
    }

    [Fact]
    public async Task PromptAsync_ShouldAcceptTheText_WhenTheCheckReturnsABlankMessage()
    {
        var root = Render<TbRoot>();
        var asking = Dialogs.PromptAsync("Rename trail", "Name", "Cedar", _ => "");

        await root.Tb().Dialog("Rename trail").ChooseAsync("OK");

        Assert.Equal("Cedar", await asking);
    }

    [Fact]
    public void PromptAsync_ShouldOpenSmallAndIgnoreTheBackdrop_WhenGivenNoOptions()
    {
        _ = Dialogs.PromptAsync("Rename trail", "Name", telemetryName: "rename-trail");

        var options = Services.GetRequiredService<FakeDialogService>().Opened[^1].Options;
        Assert.Equal((DialogSize.Small, false, "rename-trail"),
            (options.Size, options.DismissOnBackdrop, options.TelemetryName));
    }

    [Fact]
    public async Task PromptAsync_ShouldThrow_WhenTheLabelIsBlank()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Dialogs.PromptAsync("Rename trail", " "));
    }
}
