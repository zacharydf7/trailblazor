using Trailblazor.Docs.Examples;

namespace Trailblazor.BrowserTests;

public class ExampleCatalogTests
{
    public static TheoryData<string> Names => new(ExampleCatalog.All.Select(example => example.Name));

    [Theory, MemberData(nameof(Names))]
    public void Example_ShouldHaveTheSourceOfEachOfItsFiles_WhenInTheCatalog(string name)
    {
        var example = ExampleCatalog.Find(name)!;

        Assert.All(example.Sources, source => Assert.Contains("<Tb", source.Text));
    }

    [Theory, MemberData(nameof(Names))]
    public void Example_ShouldListTheTypeOfEachClassesParameter_WhenItsComponentsHaveOne(string name)
    {
        var example = ExampleCatalog.Find(name)!;

        var parts = example.Api.Select(type => type.GetProperty(nameof(TbButton.Classes))?.PropertyType).OfType<Type>();

        Assert.All(parts, type => Assert.Contains(type, example.Api));
    }

    [Fact]
    public void ExampleCatalog_ShouldShowEveryEmbeddedSource_WhenCalled()
    {
        var shown = ExampleCatalog.All.SelectMany(example => example.Sources).Select(source => $"Source/{source.File}");

        var embedded = typeof(Example).Assembly.GetManifestResourceNames().Where(name => name.StartsWith("Source/"));

        Assert.Equal(embedded.Order(), shown.Order());
    }
}
