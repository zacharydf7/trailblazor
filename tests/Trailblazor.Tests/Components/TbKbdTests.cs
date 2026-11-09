using Bunit;

namespace Trailblazor.Tests.Components;

public class TbKbdTests : BunitContext
{
    public TbKbdTests()
    {
        this.AddTrailblazor();
    }

    [Fact]
    public void Render_ShouldShowEachKeyInItsOwnKbd_WhenGivenACombination()
    {
        var cut = Render<TbKbd>(parameters => parameters.Add(kbd => kbd.Keys, "Ctrl + Shift+S"));

        Assert.Equal("KBD", cut.Find(".tb-kbd").TagName);
        Assert.Equal(["Ctrl", "Shift", "S"], cut.FindAll("kbd[data-part=key]").Select(key => key.TextContent));
        Assert.Equal("Ctrl+Shift+S",
            string.Concat(cut.Find(".tb-kbd").TextContent.Where(letter => !char.IsWhiteSpace(letter))));
    }

    [Fact]
    public void Render_ShouldShowOneKeycap_WhenGivenContent()
    {
        var cut = Render<TbKbd>(parameters => parameters.AddChildContent("Esc"));

        Assert.Contains("tb-kbd--key", cut.Find("kbd").ClassList);
        Assert.Empty(cut.FindAll("[data-part=key]"));
        Assert.Equal("Esc", cut.Find("kbd").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldThrow_WhenItHasNoKeys()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbKbd>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" + ")]
    public void Render_ShouldThrow_WhenItsKeysNameNoKey(string keys)
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbKbd>(parameters => parameters
            .Add(kbd => kbd.Keys, keys)));
    }
}
