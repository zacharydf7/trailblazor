using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.Extensions.Options;
using Trailblazor.Primitives;

namespace Trailblazor.Tests;

public class TrailblazorServiceCollectionExtensionsTests
{
    [Fact]
    public void AddTrailblazor_ShouldKeepEveryTypeScriptsExchange_WhenTheAppIsTrimmed()
    {
        var types = typeof(TbButton).Assembly.GetTypes();
        var events = types.SelectMany(type => type.GetCustomAttributes<EventHandlerAttribute>())
            .Select(handler => handler.EventArgsType);
        var invokable = types.SelectMany(type => type.GetMethods())
            .Where(method => method.IsDefined(typeof(JSInvokableAttribute)))
            .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(
                method.ReturnType.IsGenericType ? method.ReturnType.GetGenericArguments()[0] : method.ReturnType));

        var kept = typeof(TrailblazorServiceCollectionExtensions).GetMethod(nameof(
                TrailblazorServiceCollectionExtensions.AddTrailblazor))!
            .GetCustomAttributes<DynamicDependencyAttribute>().Select(dependency => dependency.Type);

        // The framework's own types and enums need nothing kept. A type parameter's closed type can't be seen here, so
        // a type sent through BrowserListener<T>, such as Viewport, goes on AddTrailblazor's list by hand.
        Assert.Empty(events.Concat(invokable).Distinct().Where(type => !type.IsGenericParameter && !type.IsEnum
            && type.Namespace?.StartsWith("System", StringComparison.Ordinal) != true).Except(kept!));
    }

    [Fact]
    public void AddTrailblazor_ShouldRegisterJsModuleLoaderAsScoped_WhenCalled()
    {
        var services = new ServiceCollection();

        services.AddTrailblazor();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(JsModuleLoader));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddTrailblazor_ShouldRegisterTheBrowserServiceAsScoped_WhenCalled()
    {
        var services = new ServiceCollection();

        services.AddTrailblazor();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(IBrowserService));
        Assert.Equal((ServiceLifetime.Scoped, typeof(BrowserService)),
            (descriptor.Lifetime, descriptor.ImplementationType));
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

    [Fact]
    public void AddTrailblazor_ShouldResolveTheToastDialogAndAnnouncerServices_WhenLoggingIsntRegistered()
    {
        using var scope = new ServiceCollection().AddTrailblazor().BuildServiceProvider().CreateScope();

        var toasts = scope.ServiceProvider.GetRequiredService<IToastService>();
        var dialogs = scope.ServiceProvider.GetRequiredService<IDialogService>();

        toasts.Show("Saved");
        Assert.Single(toasts.Toasts);
        Assert.Empty(dialogs.OpenDialogs);
    }
}
