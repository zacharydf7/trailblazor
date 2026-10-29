using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Trailblazor.Primitives;

namespace Trailblazor.Tests;

public class TrailblazorServiceCollectionExtensionsTests
{
    [Fact]
    public void AddTrailblazor_ShouldRegisterJsModuleLoaderAsScoped_WhenCalled()
    {
        var services = new ServiceCollection();

        services.AddTrailblazor();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(JsModuleLoader));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddTrailblazor_ShouldRegisterServicesOnce_WhenCalledTwice()
    {
        var services = new ServiceCollection();

        services.AddTrailblazor();
        services.AddTrailblazor();

        Assert.Single(services, d => d.ServiceType == typeof(JsModuleLoader));
    }

    [Fact]
    public void AddTrailblazor_ShouldConfigureOneSetOfOptions_WhenCalledTwice()
    {
        var services = new ServiceCollection();

        services.AddTrailblazor(options => options.Toasts.MaxVisible = 5);
        services.AddTrailblazor(options => options.Toasts.Duration = TimeSpan.FromSeconds(8));

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<TrailblazorOptions>>().Value;
        Assert.Equal(5, options.Toasts.MaxVisible);
        Assert.Equal(TimeSpan.FromSeconds(8), options.Toasts.Duration);
    }
}
