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

    // A tree row's template may wrap its marked text, so the tree's rules for wrapped text must list every other
    // component's text part, since a row can hold those too.
    [Fact]
    public void Stylesheet_ShouldNameEveryOtherComponentsTextPartInTheTreesWrappedTextRules_WhenRead()
    {
        var source = Path.Combine(Repository.Root, "src", "Trailblazor");
        var others = Directory.GetFiles(source, "*.razor", SearchOption.AllDirectories)
            .SelectMany(file => Regex.Matches(File.ReadAllText(file), @"<[^>]*data-part=""text""[^>]*>"))
            .Select(tag => Regex.Match(tag.Value, @"class=""(tb-[a-z-]+__[a-z-]+)""").Groups[1].Value)
            .Where(name => name != "tb-tree__text").Distinct().Order().ToList();
        var css = File.ReadAllText(Path.Combine(source, "wwwroot", "trailblazor.css"));

        // The tree's are the stylesheet's only rules with such a list: two for text in one wrapper, and three in
        // the scopes for text in deeper ones.
        var lists = Regex.Matches(css, @"\[data-part=""text""\]:not\(([^)]*)\)")
            .Select(rule => rule.Groups[1].Value.Split(',').Select(name => name.Trim().TrimStart('.')).Order())
            .ToList();

        Assert.DoesNotContain("", others);
        Assert.Equal(5, lists.Count);
        Assert.All(lists, list => Assert.Equal(others, list));
    }
}
