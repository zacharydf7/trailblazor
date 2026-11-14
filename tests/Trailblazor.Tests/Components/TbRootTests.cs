using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor.Tests.Components;

public class TbRootTests : BunitContext
{
    private static readonly PropertyInfo AssignedRenderMode = typeof(ComponentBase)
        .GetProperty("AssignedRenderMode", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public TbRootTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldGiveTheHostsTheHostRenderMode_WhenTheRootRendersStatically()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<TbRoot>(parameters => parameters.Add(root => root.HostRenderMode, RenderMode.InteractiveServer));

        Assert.All(ModesOfTheHosts(cut), mode => Assert.IsType<InteractiveServerRenderMode>(mode));
    }

    [Fact]
    public void Render_ShouldLeaveTheHostsInTheRootsOwnMode_WhenTheRootRendersInteractively()
    {
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        var cut = Render<TbRoot>(parameters => parameters.Add(root => root.HostRenderMode, RenderMode.InteractiveServer));

        Assert.All(ModesOfTheHosts(cut), Assert.Null);
    }

    [Fact]
    public void Render_ShouldMarkThePageInteractive_WhenTheHostsRunInteractively()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        var cut = Render<TbRoot>();

        Assert.Single(cut.FindAll("[data-tb-interactive]"));
    }

    [Fact]
    public void Render_ShouldLeaveThePageUnmarked_WhenTheHostsRenderStatically()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<TbRoot>();

        Assert.Empty(cut.FindAll("[data-tb-interactive]"));
    }

    private static IEnumerable<object?> ModesOfTheHosts(IRenderedComponent<TbRoot> cut) =>
        new IComponent[]
        {
            cut.FindComponent<DialogHost>().Instance, cut.FindComponent<ToastHost>().Instance,
            cut.FindComponent<AnnouncerHost>().Instance, cut.FindComponent<StylesheetCheck>().Instance,
            cut.FindComponent<InteractiveMark>().Instance,
        }.Select(host => AssignedRenderMode.GetValue(host));
}
