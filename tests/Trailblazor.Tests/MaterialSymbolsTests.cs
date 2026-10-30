using System.Reflection;
using Bunit;
using Trailblazor.Icons.Material;

namespace Trailblazor.Tests;

public class MaterialSymbolsTests : BunitContext
{
    private static readonly Type[] Styles = typeof(MaterialSymbols).GetNestedTypes();

    [Fact]
    public void Render_ShouldDrawTheSymbolOnItsGrid_WhenGivenToTbIcon()
    {
        var cut = Render<TbIcon>(parameters => parameters.Add(icon => icon.Icon, MaterialSymbols.Outlined.Search));

        Assert.Equal("0 -960 960 960", cut.Find("svg").GetAttribute("viewBox"));
        Assert.StartsWith("M796-121", cut.Find("svg > path").GetAttribute("d"));
    }

    [Fact]
    public void Styles_ShouldEachHaveEverySymbolAsOnePath_WhenCalled()
    {
        Assert.Equal(["Outlined", "OutlinedFilled", "Rounded", "RoundedFilled", "Sharp", "SharpFilled"],
            Styles.Select(style => style.Name).Order());
        var names = Names(typeof(MaterialSymbols.Outlined));
        Assert.True(names.Count > 3900, $"Only {names.Count} symbols.");
        Assert.Contains("_10k", names);
        Assert.Contains("AddCircle", names);
        foreach (var style in Styles)
        {
            Assert.Equal(names, Names(style));
            foreach (var icon in Icons(style))
            {
                Assert.Equal("0 -960 960 960", icon.ViewBox);
                Assert.Matches("^<path d=\"[Mm][^\"]+\" />$", icon.Svg);
            }
        }
    }

    [Fact]
    public void Filled_ShouldDifferFromPlain_WhenTheSymbolHasAFilledForm()
    {
        Assert.NotEqual(MaterialSymbols.Outlined.Home, MaterialSymbols.OutlinedFilled.Home);
        Assert.NotEqual(MaterialSymbols.Rounded.Home, MaterialSymbols.Outlined.Home);
        Assert.Equal(MaterialSymbols.Outlined.Search, MaterialSymbols.OutlinedFilled.Search);
    }

    private static List<string> Names(Type style) =>
        style.GetProperties(BindingFlags.Public | BindingFlags.Static).Select(icon => icon.Name).Order().ToList();

    private static IEnumerable<Icon> Icons(Type style) =>
        style.GetProperties(BindingFlags.Public | BindingFlags.Static).Select(icon => (Icon)icon.GetValue(null)!);
}
