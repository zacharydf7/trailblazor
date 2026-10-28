using Microsoft.Extensions.DependencyInjection;
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
}
