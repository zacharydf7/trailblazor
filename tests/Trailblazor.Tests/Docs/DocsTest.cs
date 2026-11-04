using System.Reflection;
using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Trailblazor.Docs.Pages;

namespace Trailblazor.Tests.Docs;

// The docs site's pages, rendered without a browser. Calls to the site's scripts return default values.
public abstract class DocsTest : BunitContext
{
    protected DocsTest()
    {
        Services.AddSingleton<TimeProvider>(Time);
        this.AddTrailblazor();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    protected FakeTimeProvider Time { get; } = new();

    protected FakeBrowserService Browser => Services.GetRequiredService<FakeBrowserService>();

    protected IRenderedComponent<ComponentPage> ComponentPage(string name) =>
        Render<ComponentPage>(parameters => parameters.Add(page => page.Name, name));

    protected IRenderedComponent<ComponentApiPage> ApiPage(string name) =>
        Render<ComponentApiPage>(parameters => parameters.Add(page => page.Name, name));

    // A page with no route parameters, found by its address as the site's router finds it.
    protected IRenderedComponent<ContainerFragment> PageAt(string path)
    {
        var type = typeof(ComponentPage).Assembly.GetTypes().Single(type =>
            type.GetCustomAttributes<RouteAttribute>().Any(route => route.Template == $"/{path}"));
        return Render(builder =>
        {
            builder.OpenComponent(0, type);
            builder.CloseComponent();
        });
    }

    // A button by its accessible name: its aria-label, or else its text.
    protected static IElement Button<T>(IRenderedComponent<T> cut, string name) where T : IComponent =>
        cut.FindAll("button").Single(button =>
            (button.GetAttribute("aria-label") ?? button.TextContent.Trim()) == name);

    // A field's input by the text of its label.
    protected static IElement Input(IParentNode scope, string label) =>
        scope.QuerySelectorAll("input").Single(input =>
            scope.QuerySelector($"label[for='{input.Id}']")?.TextContent.Trim() == label);

    protected static IReadOnlyList<string> Texts(IEnumerable<IElement> elements) =>
        [.. elements.Select(element => element.TextContent.Trim())];
}
