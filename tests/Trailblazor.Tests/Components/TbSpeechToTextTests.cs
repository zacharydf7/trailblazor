using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.JSInterop;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbSpeechToTextTests : BunitContext
{
    private readonly BunitJSModuleInterop _module;
    private readonly List<string> _said = [];
    private readonly List<SpeechToTextError> _errors = [];

    public TbSpeechToTextTests()
    {
        this.AddTrailblazor();
        _module = JSInterop.SetupModule("./_content/Trailblazor/speech-to-text.js");
    }

    private BunitJSModuleInterop Recognizes()
    {
        var handle = _module.SetupModule("attachSpeechToText", _ => true);
        handle.SetupVoid("start", _ => true).SetVoidResult();
        handle.SetupVoid("stop").SetVoidResult();
        handle.SetupVoid("dispose").SetVoidResult();
        return handle;
    }

    private IRenderedComponent<TbSpeechToText> RenderButton(string? telemetryName = null) =>
        Render<TbSpeechToText>(parameters => parameters.Add(button => button.Label, "Dictate notes")
            .Add(button => button.OnText, text => _said.Add(text))
            .Add(button => button.OnError, error => _errors.Add(error))
            .Add(button => button.Language, "en-US").Add(button => button.TelemetryName, telemetryName));

    [Fact]
    public void Render_ShouldStayHidden_WhenTheScriptHasntFoundSpeechRecognition()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderButton();

        Assert.True(cut.Find(".tb-speech-to-text").HasAttribute("hidden"));
        Assert.False(cut.Tb().SpeechToText("Dictate notes").IsAvailable);
    }

    [Fact]
    public void Render_ShouldNameTheButtonDictate_WhenGivenNoLabel()
    {
        Recognizes();

        var cut = Render<TbSpeechToText>();

        Assert.True(cut.Tb().SpeechToText("Dictate").IsAvailable);
    }

    [Fact]
    public async Task StartAsync_ShouldListenAndSayItIs_WhenPressed()
    {
        var handle = Recognizes();
        var cut = RenderButton();
        var button = cut.Tb().SpeechToText("Dictate notes");

        await button.StartAsync();
        Assert.True(button.IsListening);
        Assert.Equal(["en-US", false, 1], handle.Invocations["start"].Single().Arguments);
        await button.StopAsync();

        Assert.False(button.IsListening);
        Assert.Single(handle.Invocations["stop"]);
        Assert.Equal(["Listening", "Stopped listening"], Services.GetRequiredService<FakeAnnouncer>().Polite);
    }

    [Fact]
    public async Task Stopped_ShouldUnpressTheButtonAndSaySo_WhenTheBrowserStops()
    {
        Recognizes();
        var cut = RenderButton();
        var button = cut.Tb().SpeechToText("Dictate notes");
        var events = (DotNetObjectReference<SpeechToTextEvents>)JSInterop.Invocations["attachSpeechToText"]
            .Single().Arguments[1]!;

        await button.StartAsync();
        await cut.InvokeAsync(() => events.Value.OnStopped(1));

        Assert.False(button.IsListening);
        Assert.Equal(["Listening", "Stopped listening"], Services.GetRequiredService<FakeAnnouncer>().Polite);
    }

    [Fact]
    public async Task Stopped_ShouldKeepTheButtonPressed_WhenAnEarlierRecognitionEndsLate()
    {
        Recognizes();
        var cut = RenderButton();
        var button = cut.Tb().SpeechToText("Dictate notes");
        var events = (DotNetObjectReference<SpeechToTextEvents>)JSInterop.Invocations["attachSpeechToText"]
            .Single().Arguments[1]!;

        await button.StartAsync();
        await button.StopAsync();
        await button.StartAsync();
        await cut.InvokeAsync(() => events.Value.OnStopped(1));

        Assert.True(button.IsListening);
    }

    [Fact]
    public async Task Labels_ShouldWordTheAnnouncements_WhenGiven()
    {
        Recognizes();
        var cut = Render<TbSpeechToText>(parameters => parameters.Add(button => button.Labels,
            new SpeechToTextLabels { Listening = "Dictating", Stopped = "Done dictating" }));
        var button = cut.Tb().SpeechToText("Dictate");

        await button.StartAsync();
        await button.StopAsync();

        Assert.Equal(["Dictating", "Done dictating"], Services.GetRequiredService<FakeAnnouncer>().Polite);
    }

    [Fact]
    public async Task Disabled_ShouldStopListening_WhenSetWhileListening()
    {
        var handle = Recognizes();
        var cut = RenderButton();
        var button = cut.Tb().SpeechToText("Dictate notes");

        await button.StartAsync();
        cut.Render(parameters => parameters.Add(speech => speech.Disabled, true));

        Assert.False(button.IsListening);
        Assert.Single(handle.Invocations["stop"]);
    }

    [Fact]
    public async Task SayAsync_ShouldHandThePhraseToTheApp_WhenListening()
    {
        Recognizes();
        var cut = RenderButton();
        var button = cut.Tb().SpeechToText("Dictate notes");

        await Assert.ThrowsAsync<InvalidOperationException>(() => button.SayAsync("Muddy by the bridge"));
        await button.StartAsync();
        await button.SayAsync("Muddy by the bridge");

        Assert.Equal(["Muddy by the bridge"], _said);
    }

    [Fact]
    public async Task Errors_ShouldReachTheApp_WhenRecognitionFails()
    {
        Recognizes();
        var cut = RenderButton();
        var events = (DotNetObjectReference<SpeechToTextEvents>)JSInterop.Invocations["attachSpeechToText"]
            .Single().Arguments[1]!;

        foreach (var error in new[] { "not-allowed", "no-speech", "audio-capture", "network", "aborted" })
        {
            await cut.InvokeAsync(() => events.Value.OnError(error));
        }

        Assert.Equal([SpeechToTextError.NotAllowed, SpeechToTextError.NoSpeech, SpeechToTextError.NoMicrophone,
            SpeechToTextError.Network, SpeechToTextError.Other], _errors);
    }

    [Fact]
    public void TelemetryName_ShouldCountNothingShown_WhenTheButtonStaysHidden()
    {
        using var shown = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.shown");
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        RenderButton(telemetryName: "notes-dictation");

        Assert.Empty(shown.GetMeasurementSnapshot());
    }

    [Fact]
    public async Task TelemetryName_ShouldCountItShownAndEachStartAndStop_WhenGiven()
    {
        var meters = Services.GetRequiredService<IMeterFactory>();
        using var shown = new MetricCollector<long>(meters, "Trailblazor", "trailblazor.control.shown");
        using var used = new MetricCollector<long>(meters, "Trailblazor", "trailblazor.control.used");
        Recognizes();
        var cut = RenderButton(telemetryName: "notes-dictation");

        await cut.Tb().SpeechToText("Dictate notes").StartAsync();
        await cut.Tb().SpeechToText("Dictate notes").StopAsync();

        Assert.Single(shown.GetMeasurementSnapshot());
        Assert.Equal(["start", "stop"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));
    }
}
