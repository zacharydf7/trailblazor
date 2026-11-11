using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

public class FieldValidationAsyncTests : BunitContext
{
    private readonly List<(string? Value, CancellationToken Token, TaskCompletionSource<IEnumerable<string>> Answer)>
        _checks = [];

    private bool _valid = true;

    public FieldValidationAsyncTests() => this.AddTrailblazor();

    [Fact]
    public async Task Change_ShouldShowTheChecksMessageAndMakeTheFormInvalid_WhenTheCheckFindsAProblem()
    {
        var cut = RenderForm();

        cut.Find("input").Change("Robin");
        await cut.InvokeAsync(() => _checks.Single().Answer.SetResult(["That name is taken."]));

        Assert.Equal("That name is taken.", cut.Find("[data-part=error]").TextContent.Trim());
        Assert.False(_valid);
    }

    [Fact]
    public async Task Change_ShouldCancelTheEarlierCheckAndIgnoreItsAnswer_WhenANewerValueArrives()
    {
        var cut = RenderForm();

        cut.Find("input").Change("Robin");
        cut.Find("input").Change("Sam");
        await cut.InvokeAsync(() => _checks[0].Answer.SetResult(["That name is taken."]));
        await cut.InvokeAsync(() => _checks[1].Answer.SetResult([]));

        Assert.True(_checks[0].Token.IsCancellationRequested);
        Assert.Empty(cut.FindAll("[data-part=error]"));
        Assert.True(_valid);
    }

    [Fact]
    public async Task Validate_ShouldWaitForARunningCheck_WhenTheFormValidates()
    {
        var cut = RenderForm();
        cut.Find("input").Change("Robin");

        var validating = cut.InvokeAsync(() => cut.Instance.ValidateAsync());
        Assert.False(validating.IsCompleted);
        await cut.InvokeAsync(() => _checks.Single().Answer.SetResult(["That name is taken."]));

        Assert.False(await validating);
    }

    [Fact]
    public async Task Reset_ShouldDropTheAnswerOfACheckStillRunning_WhenTheFormResets()
    {
        var cut = RenderForm();
        cut.Find("input").Change("Robin");

        await cut.InvokeAsync(() => cut.Instance.ResetAsync());
        await cut.InvokeAsync(() => _checks.Single().Answer.SetResult(["That name is taken."]));

        Assert.Empty(cut.FindAll("[data-part=error]"));
        Assert.Single(_checks);
    }

    [Fact]
    public async Task Validate_ShouldWaitForTheNewerCheck_WhenTheValueChangesWhileItWaits()
    {
        var cut = RenderForm();
        cut.Find("input").Change("Robin");
        var validating = cut.InvokeAsync(() => cut.Instance.ValidateAsync());

        cut.Find("input").Change("Sam");
        await cut.InvokeAsync(() => _checks[0].Answer.SetResult([]));
        Assert.False(validating.IsCompleted);
        await cut.InvokeAsync(() => _checks[1].Answer.SetResult([]));

        Assert.True(await validating);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenTheCheckThrows()
    {
        var cut = RenderForm();
        cut.Find("input").Change("Robin");
        var validating = cut.InvokeAsync(() => cut.Instance.ValidateAsync());

        await cut.InvokeAsync(() => _checks.Single().Answer.SetException(new HttpRequestException("Offline.")));

        Assert.False(await validating);
        Assert.False(_valid);
        Assert.Equal("Offline.", (await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(5),
            Xunit.TestContext.Current.CancellationToken)).Message);
    }

    [Fact]
    public async Task Change_ShouldReadOutTheChecksMessage_WhenTheCheckAnswersAfterAnEdit()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderForm();

        cut.Find("input").Change("Robin");
        await cut.InvokeAsync(() => _checks.Single().Answer.SetResult(["That name is taken."]));

        Assert.Contains("That name is taken.", announcer.FindAll("[role=status] p").Select(spoken => spoken.TextContent));
    }

    [Fact]
    public async Task Render_ShouldDropTheAnswerOfACheckStillRunning_WhenTheParentGivesANewValue()
    {
        var cut = RenderForm();
        cut.Find("input").Change("Robin");

        cut.Render(parameters => parameters.Add(form => form.ChildContent, Field(Check, "Sam")));
        await cut.InvokeAsync(() => _checks[0].Answer.SetResult(["That name is taken."]));

        Assert.True(_checks[0].Token.IsCancellationRequested);
        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenTheCheckThrowsBeforeItAwaits()
    {
        var cut = RenderForm((_, _) => throw new HttpRequestException("Offline."));
        cut.Find("input").Change("Robin");

        var valid = await cut.InvokeAsync(() => cut.Instance.ValidateAsync());

        Assert.False(valid);
    }

    [Fact]
    public void Change_ShouldReadOutAnAnswerOnce_WhenTheCheckAnswersAtOnce()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderForm((_, _) => Task.FromResult<IEnumerable<string>>(["That name is taken."]));

        cut.Find("input").Change("Robin");

        cut.WaitForAssertion(() => Assert.Equal(["That name is taken."],
            announcer.FindAll("[role=status] p").Select(spoken => spoken.TextContent)));
    }

    [Fact]
    public async Task Validate_ShouldCheckAgain_WhenACanceledCheckIsStillRunning()
    {
        var cut = RenderForm();
        cut.Find("input").Change("Robin");
        await cut.InvokeAsync(() => cut.Instance.ResetValidationAsync());

        var validating = cut.InvokeAsync(() => cut.Instance.ValidateAsync());
        await cut.InvokeAsync(() => _checks[^1].Answer.SetResult([]));

        Assert.Equal(2, _checks.Count);
        Assert.True(await validating);
    }

    [Fact]
    public async Task Validate_ShouldNotWaitForACanceledCheck_WhenTheValueNowFailsItsOtherChecks()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenComponent<TbTextField<string>>(0);
            builder.AddComponentParameter(1, nameof(TbTextField<string>.Label), "Name");
            builder.AddComponentParameter(2, nameof(TbTextField<string>.Required), true);
            builder.AddComponentParameter(3, nameof(TbTextField<string>.ValidationAsync),
                (Func<string?, CancellationToken, Task<IEnumerable<string>>>)Check);
            builder.CloseComponent();
        })));
        cut.Find("input").Change("Robin");
        cut.WaitForAssertion(() => Assert.Single(_checks));
        cut.Find("input").Change("");

        var validating = cut.InvokeAsync(() => cut.Instance.ValidateAsync());

        Assert.True(validating.IsCompleted);
        Assert.False(await validating);
    }

    private IRenderedComponent<TbForm> RenderForm(
        Func<string?, CancellationToken, Task<IEnumerable<string>>>? check = null) =>
        Render<TbForm>(parameters => parameters.Add(form => form.IsValid, _valid)
            .Add(form => form.IsValidChanged, (bool valid) => _valid = valid)
            .Add(form => form.ChildContent, Field(check ?? Check)));

    private static RenderFragment Field(Func<string?, CancellationToken, Task<IEnumerable<string>>> check,
        string? value = null) => builder =>
    {
        builder.OpenComponent<TbTextField<string>>(0);
        builder.AddComponentParameter(1, nameof(TbTextField<string>.Label), "Name");
        builder.AddComponentParameter(2, nameof(TbTextField<string>.ValidationAsync), check);
        builder.AddComponentParameter(3, nameof(TbTextField<string>.Value), value);
        builder.CloseComponent();
    };

    private Task<IEnumerable<string>> Check(string? value, CancellationToken token)
    {
        var answer = new TaskCompletionSource<IEnumerable<string>>();
        _checks.Add((value, token, answer));
        return answer.Task;
    }
}
