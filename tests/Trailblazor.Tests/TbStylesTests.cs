using System.Text.RegularExpressions;
using Bunit;
using Trailblazor.Tests.Docs;

namespace Trailblazor.Tests;

public class TbStylesTests : BunitContext
{
    [Fact]
    public void Render_ShouldLinkTheStylesheet_WhenCalled()
    {
        var cut = Render<TbStyles>();

        var link = cut.Find("link[rel=stylesheet]");
        Assert.Equal("_content/Trailblazor/trailblazor.css", link.GetAttribute("href"));
    }

    // Safari is known to honor only -webkit-user-select.
    [Fact]
    public void Stylesheet_ShouldGiveEachUserSelectItsPrefixedForm_WhenRead()
    {
        var css = File.ReadAllText(Path.Combine(Repository.Root, "src", "Trailblazor", "wwwroot", "trailblazor.css"));
        var rules = Regex.Matches(css, @"\{([^{}]*)\}").Select(rule => rule.Groups[1].Value).ToList();

        var unprefixed = rules
            .SelectMany(body => Regex.Matches(body, @"(?<!-webkit-)user-select:\s*([a-z]+)")
                .Where(setting => !body.Contains($"-webkit-user-select: {setting.Groups[1].Value};"))
                .Select(setting => setting.Value));

        Assert.Contains(rules, body => body.Contains("user-select"));
        Assert.Empty(unprefixed);
    }
}
