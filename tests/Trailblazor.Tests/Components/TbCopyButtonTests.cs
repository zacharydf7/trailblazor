using System.Diagnostics.Metrics;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Time.Testing;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbCopyButtonTests : BunitContext
{
    private const string Command = "dotnet add package Trailblazor";
    private readonly FakeTimeProvider _time = new();

    public TbCopyButtonTests()
    {
        Services.AddSingleton<TimeProvider>(_time);
        this.AddTrailblazor();
    }

    private FakeBrowserService Browser => Services.GetRequiredService<FakeBrowserService>();

    private FakeAnnouncer Announcer => Services.GetRequiredService<FakeAnnouncer>();

    private IRenderedComponent<TbCopyButton> RenderButton(
        Action<ComponentParameterCollectionBuilder<TbCopyButton>>? more = null) => Render<TbCopyButton>(parameters =>
        {
            parameters.Add(button => button.Text, Command);
            more?.Invoke(parameters);
        });

    [Fact]
    public async Task TbCopyButton_ShouldCopyTheTextAndSayItCopied_WhenPressed()
    {
        var cut = RenderButton();
        var button = cut.Tb().CopyButton("Copy");

        await button.CopyAsync();

        Assert.Equal([Command], Browser.Copied);
        Assert.Equal(["Copied"], Announcer.Polite);
        Assert.Equal(("copied", "Copied"), (button.State, cut.Find("button").TextContent.Trim()));
    }

    [Fact]
    public async Task TbCopyButton_ShouldSayItCouldntCopy_WhenTheBrowserRefuses()
    {
        Browser.CanCopy = false;
        var cut = RenderButton();

        await cut.Tb().CopyButton().CopyAsync();

        Assert.Empty(Browser.Copied);
        Assert.Equal(["Couldn't copy"], Announcer.Polite);
        Assert.Equal(("failed", "Couldn't copy"), (cut.Tb().CopyButton().State, cut.Find("button").TextContent.Trim()));
    }

    [Fact]
    public async Task TbCopyButton_ShouldOfferToCopyAgain_WhenTheMomentPasses()
    {
        var cut = RenderButton();
        var button = cut.Tb().CopyButton();
        await button.CopyAsync();

        _time.Advance(TimeSpan.FromSeconds(1.9));
        Assert.Equal("copied", button.State);
        _time.Advance(TimeSpan.FromSeconds(0.1));

        cut.WaitForAssertion(() =>
            Assert.Equal(("idle", "Copy"), (button.State, cut.Find("button").TextContent.Trim())));
    }

    [Fact]
    public async Task TbCopyButton_ShouldShowTheResultForTheWholeMoment_WhenPressedAgainBeforeItResets()
    {
        var cut = RenderButton();
        var button = cut.Tb().CopyButton();
        await button.CopyAsync();
        _time.Advance(TimeSpan.FromSeconds(1.5));

        await button.CopyAsync();
        _time.Advance(TimeSpan.FromSeconds(0.6));

        Assert.Equal("copied", button.State);
        Assert.Equal("2", cut.Find("button").GetAttribute("data-copies"));
    }

    [Fact]
    public async Task IconOnly_ShouldShowAnotherIcon_WhenTheBrowserRefuses()
    {
        Browser.CanCopy = false;
        var cut = RenderButton(parameters => parameters.Add(button => button.IconOnly, true));

        await cut.Tb().CopyButton().CopyAsync();

        // Not by color alone: the icon changes too.
        Assert.Equal(PathOf(BuiltInIcons.Error), cut.Find("svg path").GetAttribute("d"));
    }

    private static string PathOf(Icon icon) => Regex.Match(icon.Svg, "d=\"([^\"]+)\"").Groups[1].Value;

    [Fact]
    public void IconOnly_ShouldShowTheCopyIcon_WhenNothingWasCopiedYet()
    {
        var cut = RenderButton(parameters => parameters.Add(button => button.IconOnly, true));

        Assert.Equal(PathOf(BuiltInIcons.Copy), cut.Find("svg path").GetAttribute("d"));
    }

    [Fact]
    public async Task CopyAsync_ShouldLeaveNothingRunning_WhenTheButtonGoesAwayMidCopy()
    {
        var copying = new TaskCompletionSource<bool>();
        Services.AddSingleton<IBrowserService>(new SlowBrowser(copying.Task));
        var host = Render<ToggleHost>();
        var press = host.Find("button").ClickAsync(new());

        host.Render(parameters => parameters.Add(toggle => toggle.Shown, false));
        await host.InvokeAsync(() => copying.SetResult(true));
        await press;

        Assert.Empty(Announcer.Polite);
    }

    // Shows the button until told not to.
    private sealed class ToggleHost : ComponentBase
    {
        [Parameter]
        public bool Shown { get; set; } = true;

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            if (Shown)
            {
                builder.OpenComponent<TbCopyButton>(0);
                builder.AddComponentParameter(1, nameof(TbCopyButton.Text), Command);
                builder.CloseComponent();
            }
        }
    }

    // A browser whose copy finishes only when the test says so; the rest is the kit's fake.
    private sealed class SlowBrowser(Task<bool> copy) : IBrowserService
    {
        private readonly FakeBrowserService _fake = new();

        public Task<bool> CopyTextAsync(string text) => copy;

        public Task DownloadAsync(Stream content, string fileName, string? contentType = null) =>
            _fake.DownloadAsync(content, fileName, contentType);

        public Task<bool> SaveAsync(Stream content, string fileName, string? contentType = null) =>
            _fake.SaveAsync(content, fileName, contentType);

        public Task<bool> CanPickColorAsync() => _fake.CanPickColorAsync();

        public Task<string?> PickColorAsync() => _fake.PickColorAsync();

        public Task ScrollIntoViewAsync(ElementReference element, ScrollAlignment alignment = ScrollAlignment.Nearest,
            bool smooth = false) => _fake.ScrollIntoViewAsync(element, alignment, smooth);

        public Task<TimeZoneInfo?> GetTimeZoneAsync() => _fake.GetTimeZoneAsync();

        public Task<Viewport> GetViewportAsync() => _fake.GetViewportAsync();

        public Task<IAsyncDisposable> ObserveViewportAsync(Func<Viewport, Task> onChange) =>
            _fake.ObserveViewportAsync(onChange);

        public Task<IAsyncDisposable> ObserveBreakpointAsync(Func<Breakpoint, Task> onChange) =>
            _fake.ObserveBreakpointAsync(onChange);
    }

    [Fact]
    public async Task IconOnly_ShouldNameTheIconByItsWords_WhenSet()
    {
        var cut = RenderButton(parameters => parameters.Add(button => button.IconOnly, true)
            .Add(button => button.Labels,
                new CopyButtonLabels { Copy = "Copy the command", Copied = "Command copied" }));

        await cut.Tb().CopyButton("Copy the command").CopyAsync();

        Assert.Equal("Copy the command", cut.Find("button").GetAttribute("aria-label"));
        Assert.Equal(["Command copied"], Announcer.Polite);
    }

    [Fact]
    public void Render_ShouldBeOff_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderButton();

        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Telemetry_ShouldCountCopies_WhenNamed()
    {
        var meters = Services.GetRequiredService<IMeterFactory>();
        using var used = new MetricCollector<long>(meters, "Trailblazor", "trailblazor.control.used");
        var cut = RenderButton(parameters => parameters.Add(button => button.TelemetryName, "install-command"));

        await cut.Tb().CopyButton().CopyAsync();

        var use = Assert.Single(used.GetMeasurementSnapshot());
        Assert.Equal(("copied", "install-command"),
            ((string?)use.Tags["trailblazor.action"], (string?)use.Tags["trailblazor.telemetry_name"]));
    }

    [Fact]
    public async Task Copyable_ShouldCopyTheFieldsText_WhenTheTextFieldsButtonIsPressed()
    {
        var cut = Render<TbTextField<string>>(parameters => parameters.Add(field => field.Label, "API key")
            .Add(field => field.Value, "tb_live_4f9a").Add(field => field.ReadOnly, true)
            .Add(field => field.Copyable, true));

        await cut.Tb().CopyButton().CopyAsync();

        Assert.Equal(["tb_live_4f9a"], Browser.Copied);
        // Screen readers hear the button with the field's label.
        var label = cut.Find("label").GetAttribute("id");
        Assert.Equal(label, cut.Find("[data-part=copy]").GetAttribute("aria-describedby"));
    }
}
