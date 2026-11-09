using Bunit;
using Microsoft.AspNetCore.Components.Web;
using ErrorEventArgs = Microsoft.AspNetCore.Components.Web.ErrorEventArgs;

namespace Trailblazor.Tests.Components;

public class TbAvatarTests : BunitContext
{
    public TbAvatarTests()
    {
        this.AddTrailblazor();
    }

    [Fact]
    public void Render_ShouldNameTheImageByTheName_WhenGivenAName()
    {
        var cut = Render<TbAvatar>(parameters => parameters.Add(avatar => avatar.Name, "Ada Lovelace")
            .Add(avatar => avatar.Src, "ada.png").Add(avatar => avatar.Tone, Tone.Success)
            .Add(avatar => avatar.Shape, AvatarShape.Rounded).Add(avatar => avatar.Class, "my-avatar"));

        var avatar = cut.Find("[data-part=avatar]");
        var image = cut.Find("[data-part=image]");

        Assert.Equal(("img", "Ada Lovelace"), (avatar.GetAttribute("role"), avatar.GetAttribute("aria-label")));
        Assert.Equal(["tb-avatar", "tb-avatar--medium", "tb-avatar--rounded", "tb-avatar--success", "my-avatar"],
            avatar.ClassList);
        Assert.Equal("background-image: url(\"ada.png\")", image.GetAttribute("style"));
        Assert.Equal("true", image.GetAttribute("aria-hidden"));
        Assert.Equal(("AL", "true"),
            (cut.Find("[data-part=initials]").TextContent, cut.Find("[data-part=initials]").GetAttribute("aria-hidden")));
    }

    [Theory]
    [InlineData("Ada Lovelace", "AL")]
    [InlineData("grace", "G")]
    [InlineData("  María José  García ", "MG")]
    [InlineData("(Ada) Lovelace", "AL")]
    [InlineData("李小龙", "李")]
    [InlineData("émile zola", "ÉZ")]
    [InlineData("-- --", "")]
    public void InitialsOf_ShouldTakeTheFirstLettersOfTheFirstAndLastWords_WhenGivenAName(string name,
        string initials) => Assert.Equal(initials, TbAvatar.InitialsOf(name));

    [Theory]
    [InlineData("ada.png", "url(\"ada.png\")")]
    [InlineData("a\");b{color:red}", "url(\"a\\\");b{color:red}\")")]
    [InlineData("a\\b\nc", "url(\"a\\\\b\\a c\")")]
    public void CssUrl_ShouldKeepTheAddressInsideTheString_WhenItHasQuotesBackslashesOrBreaks(string address,
        string url) => Assert.Equal(url, TbAvatar.CssUrl(address));

    [Fact]
    public void Render_ShouldShowItsIcon_WhenItHasNoInitials()
    {
        var cut = Render<TbAvatar>(parameters => parameters.Add(avatar => avatar.Name, "A guest")
            .Add(avatar => avatar.Initials, ""));

        Assert.Empty(cut.FindAll("[data-part=initials]"));
        Assert.Equal("true", cut.Find("[data-part=icon]").GetAttribute("aria-hidden"));
    }

    [Fact]
    public async Task Render_ShouldDropThePicture_WhenItFailsToLoad()
    {
        var cut = Render<TbAvatar>(parameters => parameters.Add(avatar => avatar.Name, "Alan Turing")
            .Add(avatar => avatar.Src, "missing.png"));

        await cut.Find("img[hidden]").TriggerEventAsync("onerror", new ErrorEventArgs());
        Assert.Empty(cut.FindAll("[data-part=image]"));
        Assert.Equal("AT", cut.Find("[data-part=initials]").TextContent);
        cut.Render(parameters => parameters.Add(avatar => avatar.Src, "alan.png"));

        Assert.Equal("alan.png", cut.Find("img[hidden]").GetAttribute("src"));
    }

    [Fact]
    public async Task Render_ShouldDropTheInitials_WhenThePictureLoads()
    {
        var cut = Render<TbAvatar>(parameters => parameters.Add(avatar => avatar.Name, "Trail Crew")
            .Add(avatar => avatar.Src, "crew-logo.png"));

        await cut.Find("img[hidden]").TriggerEventAsync("onload", new ProgressEventArgs());
        Assert.Empty(cut.FindAll("[data-part=initials]"));
        cut.Render(parameters => parameters.Add(avatar => avatar.Src, "new-logo.png"));

        Assert.Equal("TC", cut.Find("[data-part=initials]").TextContent);
    }

    [Fact]
    public void Render_ShouldHideTheAvatarFromScreenReaders_WhenDecorative()
    {
        var cut = Render<TbAvatar>(parameters => parameters.Add(avatar => avatar.Decorative, true));

        var avatar = cut.Find("[data-part=avatar]");

        Assert.Equal("true", avatar.GetAttribute("aria-hidden"));
        Assert.Null(avatar.GetAttribute("role"));
        Assert.Null(avatar.GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenItHasNoNameAndIsNotDecorative()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbAvatar>());

        Assert.Contains("Name", thrown.Message);
    }

    [Fact]
    public void Group_ShouldShowAndNameTheCountOfTheRest_WhenMoreIsSet()
    {
        var cut = Render<TbAvatarGroup>(parameters => parameters.Add(group => group.AriaLabel, "Trip members")
            .Add(group => group.More, 3).Add(group => group.Size, Size.Small)
            .AddChildContent<TbAvatar>(avatar => avatar.Add(a => a.Name, "Joan Clarke"))
            .AddChildContent<TbAvatar>(avatar => avatar.Add(a => a.Name, "Hedy Lamarr").Add(a => a.Size, Size.Large)));

        var group = cut.Find("[data-part=group]");
        var more = cut.Find("[data-part=more]");
        var avatars = cut.FindAll("[data-part=avatar]");

        Assert.Equal(("group", "Trip members"), (group.GetAttribute("role"), group.GetAttribute("aria-label")));
        Assert.Equal(("img", "3 more", "+3"), (more.GetAttribute("role"), more.GetAttribute("aria-label"),
            more.TextContent.Trim()));
        Assert.Contains("tb-avatar--small", avatars[0].ClassList);
        Assert.Contains("tb-avatar--large", avatars[1].ClassList);
    }

    [Fact]
    public void Group_ShouldResizeItsAvatars_WhenItsSizeChanges()
    {
        var cut = Render<TbAvatarGroup>(parameters => parameters.Add(group => group.Size, Size.Small)
            .AddChildContent<TbAvatar>(avatar => avatar.Add(a => a.Name, "Joan Clarke")));

        cut.Render(parameters => parameters.Add(group => group.Size, Size.Large));

        Assert.Contains("tb-avatar--large", cut.Find("[data-part=avatar]").ClassList);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(2, "2 others")]
    public void Group_ShouldShowTheCountOnlyWhenThereAreMore_WhenGivenACount(int more, string? name)
    {
        var cut = Render<TbAvatarGroup>(parameters => parameters.Add(group => group.More, more)
            .Add(group => group.MoreLabel, "{0} others"));

        Assert.Equal(name, cut.FindAll("[data-part=more]").SingleOrDefault()?.GetAttribute("aria-label"));
    }
}
