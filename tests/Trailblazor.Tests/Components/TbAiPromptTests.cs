using System.Runtime.CompilerServices;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbAiPromptTests : BunitContext
{
    public TbAiPromptTests() => this.AddTrailblazor();

    private FakeAnnouncer Announcer => Services.GetRequiredService<FakeAnnouncer>();

    private FakeBrowserService Browser => Services.GetRequiredService<FakeBrowserService>();

    [Fact]
    public async Task Prompt_ShouldStreamTheResponseAndSayWhenItIsReady_WhenGenerated()
    {
        var cut = RenderPrompt(Answer("The ", "ridge ", "trail."));

        await cut.Tb().AiPrompt("Ask about this trail").PromptAsync("  Which trail is shadiest?  ");

        var prompt = cut.Tb().AiPrompt();
        Assert.Equal(["The ridge trail."], prompt.Responses);
        Assert.Equal([AiPromptStatus.Done], prompt.Statuses);
        Assert.Equal(("Which trail is shadiest?", ""), (cut.Find("[data-part=prompt]").TextContent,
            cut.Find("[data-part=input]").GetAttribute("value")));
        Assert.Equal(["Generating a response", "Response ready"], Announcer.Polite);
    }

    [Fact]
    public async Task Prompt_ShouldMarkTheResponseBusyAndOfferStop_WhenItStreams()
    {
        var gate = new TaskCompletionSource();
        var cut = RenderPrompt((prompt, token) => Held(gate, token));

        var generating = cut.Tb().AiPrompt().PromptAsync("Which trail?");
        var output = cut.Find("[data-part=output]");
        var during = (output.GetAttribute("aria-busy"), output.GetAttribute("data-status"),
            cut.Find("[data-part=generate]").TextContent.Trim(), cut.Find("[data-part=text]").TextContent);
        await cut.Tb().AiPrompt().StopAsync();
        await generating;

        Assert.Equal(("true", "generating", "Stop", "The ridge"), during);
        Assert.Equal(["The ridge"], cut.Tb().AiPrompt().Responses);
        Assert.Equal([AiPromptStatus.Stopped], cut.Tb().AiPrompt().Statuses);
        Assert.Equal(["Generating a response", "Response stopped"], Announcer.Polite);
        Assert.Equal("Generate", cut.Find("[data-part=generate]").TextContent.Trim());
        Assert.False(cut.Find("section").HasAttribute("data-generating"));
    }

    [Fact]
    public async Task Stop_ShouldStillWork_WhenThePromptIsTurnedOffMidResponse()
    {
        var gate = new TaskCompletionSource();
        var cut = RenderPrompt((prompt, token) => Held(gate, token));

        var generating = cut.Tb().AiPrompt().PromptAsync("Which trail?");
        cut.Render(parameters => parameters.Add(prompt => prompt.Disabled, true));
        Assert.False(cut.Find("[data-part=generate]").HasAttribute("disabled"));
        await cut.Find("[data-part=generate]").ClickAsync(new());
        await generating;

        Assert.Equal([AiPromptStatus.Stopped], cut.Tb().AiPrompt().Statuses);
        Assert.True(cut.Find("[data-part=generate]").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Stop_ShouldEndTheResponseQuietly_WhenTheModelReportsTheCancelItsOwnWay()
    {
        var cut = RenderPrompt((prompt, token) => Closing(token));

        var generating = cut.Tb().AiPrompt().PromptAsync("Which trail?");
        await cut.Tb().AiPrompt().StopAsync();
        await generating;

        Assert.Equal([AiPromptStatus.Stopped], cut.Tb().AiPrompt().Statuses);
        Assert.Equal(["Generating a response", "Response stopped"], Announcer.Polite);
    }

    [Fact]
    public async Task Dispose_ShouldEndTheResponseWithoutReadingItOut_WhenThePromptGoesAwayMidResponse()
    {
        var gate = new TaskCompletionSource();
        var cut = RenderPrompt((prompt, token) => Held(gate, token));

        var generating = cut.Tb().AiPrompt().PromptAsync("Which trail?");
        await DisposeComponentsAsync();
        await generating;

        Assert.Equal(["Generating a response"], Announcer.Polite);
    }

    [Fact]
    public async Task Stop_ShouldEndTheResponseAtItsNextChunk_WhenTheModelIgnoresTheToken()
    {
        var gate = new TaskCompletionSource();
        var cut = RenderPrompt((prompt, token) => Deaf(gate));

        var generating = cut.Tb().AiPrompt().PromptAsync("Which trail?");
        await cut.Tb().AiPrompt().StopAsync();
        gate.SetResult();
        await generating;

        Assert.Equal(["The ridge"], cut.Tb().AiPrompt().Responses);
        Assert.Equal([AiPromptStatus.Stopped], cut.Tb().AiPrompt().Statuses);
    }

    [Fact]
    public async Task Prompt_ShouldRefuseASecondPrompt_WhenOneGenerates()
    {
        var gate = new TaskCompletionSource();
        var cut = RenderPrompt((prompt, token) => Held(gate, token));

        var first = cut.Tb().AiPrompt().PromptAsync("Which trail?");
        var helper = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().AiPrompt().PromptAsync("And the longest?"));
        cut.Find("[data-part=input]").Input("And the longest?");
        var second = cut.Find("form").SubmitAsync();
        gate.SetResult();
        await Task.WhenAll(first, second);

        Assert.Equal("This AI prompt is still generating; stop it or let it finish.", helper.Message);
        Assert.Equal(["The ridge trail."], cut.Tb().AiPrompt().Responses);
        Assert.Equal("And the longest?", cut.Find("[data-part=input]").GetAttribute("value"));
    }

    [Fact]
    public async Task Prompt_ShouldMarkTheResponseFailedAndKeepTheError_WhenGenerateThrows()
    {
        var cut = RenderPrompt((prompt, token) => Broken(), parameters => parameters.Add(prompt => prompt.OutputTemplate,
            output => builder => builder.AddContent(0, $"{output.Text}|{output.Error?.Message}")));

        await cut.Tb().AiPrompt().PromptAsync("Which trail?");

        Assert.Equal([AiPromptStatus.Failed], cut.Tb().AiPrompt().Statuses);
        Assert.Equal("The|The model is busy.", cut.Find("[data-part=text]").TextContent);
        Assert.Equal("Couldn't generate a response. Try again.", cut.Find("[data-part=error]").TextContent);
        Assert.Equal(["Generating a response", "Couldn't generate a response. Try again."], Announcer.Polite);
    }

    [Fact]
    public async Task Retry_ShouldGenerateAgainInPlace_WhenPressedOnAnOlderResponse()
    {
        var answers = new Queue<string>(["The ridge trail.", "Bring water.", "The river trail."]);
        var cut = RenderPrompt((prompt, token) => Answer(answers.Dequeue())(prompt, token));

        await cut.Tb().AiPrompt().PromptAsync("Which trail?");
        await cut.Tb().AiPrompt().PromptAsync("What to pack?");
        await cut.Tb().AiPrompt().RetryAsync(1);

        Assert.Equal(["Bring water.", "The river trail."], cut.Tb().AiPrompt().Responses);
        Assert.Equal(["What to pack?", "Which trail?"], cut.FindAll("[data-part=prompt]").Select(prompt =>
            prompt.TextContent));
    }

    [Fact]
    public async Task Responses_ShouldListNewestFirst_WhenPromptedTwice()
    {
        var cut = RenderPrompt((prompt, token) => Answer($"About {prompt}")(prompt, token));

        await cut.Tb().AiPrompt().PromptAsync("the ridge");
        await cut.Tb().AiPrompt().PromptAsync("the river");

        Assert.Equal(["About the river", "About the ridge"], cut.Tb().AiPrompt().Responses);
    }

    [Fact]
    public async Task Suggestion_ShouldFillTheBox_WhenChosen()
    {
        var cut = RenderPrompt(Answer("Done."), parameters => parameters.Add(prompt => prompt.Suggestions,
            ["Plan a day hike", "Pack for rain"]));

        await cut.Tb().AiPrompt().ChooseSuggestionAsync("Pack for rain");

        Assert.Equal("Pack for rain", cut.Find("[data-part=input]").GetAttribute("value"));
        Assert.Equal("Suggestions", cut.Find("[data-part=suggestions]").GetAttribute("aria-label"));
        Assert.Empty(cut.FindAll("[data-part=output]"));
    }

    [Fact]
    public async Task Copy_ShouldCopyTheTextAndSaySo_WhenPressed()
    {
        var cut = RenderPrompt(Answer("The ridge trail."));

        await cut.Tb().AiPrompt().PromptAsync("Which trail?");
        await cut.Tb().AiPrompt().CopyAsync();

        Assert.Equal(["The ridge trail."], Browser.Copied);
        Assert.Equal("Copied", Announcer.Polite[^1]);
    }

    [Fact]
    public async Task Insert_ShouldRaiseOnInsertWithTheResponse_WhenPressed()
    {
        AiPromptOutput? inserted = null;
        var cut = RenderPrompt(Answer("The ridge trail."),
            parameters => parameters.Add(prompt => prompt.OnInsert, (AiPromptOutput output) => inserted = output));
        var without = RenderPrompt(Answer("The river trail."));

        await cut.Tb().AiPrompt().PromptAsync("Which trail?");
        await cut.Tb().AiPrompt().InsertAsync();
        await without.Tb().AiPrompt().PromptAsync("Which trail?");

        Assert.Equal(new AiPromptOutput("Which trail?", "The ridge trail.", AiPromptStatus.Done), inserted);
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => without.Tb().AiPrompt().InsertAsync());
        Assert.Equal("This AI prompt has no Insert; set OnInsert.", thrown.Message);
    }

    [Fact]
    public async Task Prompt_ShouldDoNothing_WhenTheBoxIsBlank()
    {
        var cut = RenderPrompt(Answer("Done."));

        cut.Find("[data-part=input]").Input("   ");
        await cut.Find("form").SubmitAsync();

        Assert.Empty(cut.FindAll("[data-part=output]"));
        Assert.Empty(Announcer.Polite);
    }

    [Fact]
    public async Task Prompt_ShouldRefuseToGenerate_WhenDisabled()
    {
        var cut = RenderPrompt(Answer("Done."), parameters => parameters.Add(prompt => prompt.Disabled, true));

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().AiPrompt().PromptAsync("Which trail?"));

        Assert.Equal("This AI prompt is disabled.", thrown.Message);
        Assert.True(cut.Find("[data-part=input]").HasAttribute("disabled"));
    }

    [Fact]
    public void Render_ShouldNameTheRegionByTheVisibleLabel_WhenRendered()
    {
        var cut = RenderPrompt(Answer("Done."));

        var labelledBy = cut.Find("section").GetAttribute("aria-labelledby");
        Assert.Equal("Ask about this trail", cut.Find($"#{labelledBy}").TextContent.Trim());
        Assert.Equal(cut.Find("[data-part=input]").Id, cut.Find("label").GetAttribute("for"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenItHasNoLabelOrGenerate()
    {
        var noLabel = Assert.Throws<InvalidOperationException>(() => Render<TbAiPrompt>(parameters =>
            parameters.Add(prompt => prompt.Generate, Answer("Done."))));
        var noGenerate = Assert.Throws<InvalidOperationException>(() => Render<TbAiPrompt>(parameters =>
            parameters.Add(prompt => prompt.Label, "Ask about this trail")));

        Assert.Equal(("TbAiPrompt needs a Label.", "TbAiPrompt needs a Generate function."),
            (noLabel.Message, noGenerate.Message));
    }

    private static Func<string, CancellationToken, IAsyncEnumerable<string>> Answer(params string[] chunks) =>
        (prompt, token) => Stream(chunks, token);

    private static async IAsyncEnumerable<string> Stream(string[] chunks,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        foreach (var chunk in chunks)
        {
            await Task.Yield();
            token.ThrowIfCancellationRequested();
            yield return chunk;
        }
    }

    // Streams part of an answer, then waits on the gate, as a slow model does.
    private static async IAsyncEnumerable<string> Held(TaskCompletionSource gate,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        yield return "The ridge";
        await gate.Task.WaitAsync(token);
        yield return " trail.";
    }

    // Like Held, but with no token at all, as a model client that can't be canceled.
    private static async IAsyncEnumerable<string> Deaf(TaskCompletionSource gate)
    {
        yield return "The ridge";
        await gate.Task;
        yield return " trail.";
    }

    // Reports a cancel as a closed stream, as some model clients do.
    private static async IAsyncEnumerable<string> Closing([EnumeratorCancellation] CancellationToken token = default)
    {
        yield return "The ridge";
        try
        {
            await Task.Delay(Timeout.Infinite, token);
        }
        catch (OperationCanceledException)
        {
            throw new IOException("The response stream was closed.");
        }
    }

    private static async IAsyncEnumerable<string> Broken()
    {
        await Task.Yield();
        yield return "The";
        throw new InvalidOperationException("The model is busy.");
    }

    private IRenderedComponent<TbAiPrompt> RenderPrompt(Func<string, CancellationToken, IAsyncEnumerable<string>> generate,
        Action<ComponentParameterCollectionBuilder<TbAiPrompt>>? configure = null) => Render<TbAiPrompt>(parameters =>
    {
        parameters.Add(prompt => prompt.Label, "Ask about this trail").Add(prompt => prompt.Generate, generate);
        configure?.Invoke(parameters);
    });
}
