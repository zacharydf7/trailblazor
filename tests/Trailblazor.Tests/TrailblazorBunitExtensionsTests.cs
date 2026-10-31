using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Trailblazor.Tests;

public class TrailblazorBunitExtensionsTests : BunitContext
{
    private string? _size;

    [Fact]
    public async Task AddTrailblazor_ShouldLetScriptBackedComponentsRenderAndRespond_WhenNoJsInteropIsSetUp()
    {
        this.AddTrailblazor();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        var cut = Render(builder =>
        {
            builder.OpenComponent<TbRoot>(0);
            builder.CloseComponent();
            builder.OpenComponent<TbTooltip>(1);
            builder.AddAttribute(2, nameof(TbTooltip.Text), "Saves the file");
            builder.AddAttribute(3, nameof(TbTooltip.ChildContent),
                (RenderFragment)(button => button.AddContent(0, "Save")));
            builder.CloseComponent();
            builder.OpenComponent<TbMenu>(4);
            builder.AddAttribute(5, nameof(TbMenu.Label), "More");
            builder.CloseComponent();
            builder.OpenComponent<TbSelect<string>>(6);
            builder.AddAttribute(7, nameof(TbSelect<string>.Label), "Size");
            builder.AddAttribute(8, nameof(TbSelect<string>.ValueChanged),
                EventCallback.Factory.Create<string>(this, size => _size = size));
            builder.AddAttribute(9, nameof(TbSelect<string>.ChildContent), (RenderFragment)AddSizes);
            builder.CloseComponent();
            builder.OpenComponent<TbThemePicker>(10);
            builder.CloseComponent();
        });

        cut.Find("[role=option]").Click();
        Services.GetRequiredService<IToastService>().Show("Saved");
        _ = Services.GetRequiredService<IDialogService>().ConfirmAsync("Delete?", "This can't be undone.");

        Assert.Equal("Small", _size);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-part=toast]")));
        await cut.WaitForAssertionAsync(() => Assert.Single(cut.FindAll("dialog")));
    }

    [Fact]
    public void AddTrailblazor_ShouldLeaveATestsOwnModuleSetupInCharge_WhenTheTestSetsOneUp()
    {
        this.AddTrailblazor();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        var select = JSInterop.SetupModule("./_content/Trailblazor/select.js");
        select.SetupModule("createSelect", _ => true);

        Render<TbSelect<string>>(parameters => parameters.Add(component => component.ChildContent, AddSizes));

        Assert.Single(select.Invocations["createSelect"]);
    }

    [Fact]
    public async Task AddTrailblazor_ShouldLeaveOtherModulesUnanswered_WhenTheTestHasNotSetThemUp()
    {
        this.AddTrailblazor();
        var js = Services.GetRequiredService<IJSRuntime>();

        await Assert.ThrowsAsync<JSRuntimeUnhandledInvocationException>(() => js
            .InvokeAsync<IJSObjectReference>("import", ["./_content/TrailblazorExtras/extras.js"]).AsTask());
    }

    [Fact]
    public void AddTrailblazor_ShouldApplyTheOptions_WhenConfigured()
    {
        this.AddTrailblazor(options => options.Toasts.MaxVisible = 1);

        Assert.Equal(1, Services.GetRequiredService<IOptions<TrailblazorOptions>>().Value.Toasts.MaxVisible);
    }

    private static void AddSizes(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TbOption<string>>(0);
        builder.AddAttribute(1, nameof(TbOption<string>.Value), "Small");
        builder.CloseComponent();
    }
}
