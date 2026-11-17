using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// What a field shows while its ValidationAsync check runs, once it passes and once it fails.
public class FieldValidationStatusTests : BunitContext
{
    private readonly List<TaskCompletionSource<IEnumerable<string>>> _answers = [];
    private readonly List<FieldValidationStatus> _statuses = [];

    private FakeAnnouncer Announcer => Services.GetRequiredService<FakeAnnouncer>();

    [Fact]
    public void Status_ShouldShowCheckingAndMarkTheInputBusy_WhenTheCheckRuns()
    {
        this.AddTrailblazor();
        var cut = RenderName(showStatus: true);

        cut.Find("input").Change("Robin");

        var input = cut.Find("input");
        var status = cut.Find("[data-part=validation-status]");
        Assert.Equal(("checking", "Checking…", "true", true), (status.GetAttribute("data-status"),
            status.TextContent.Trim(), input.GetAttribute("aria-busy"),
            input.GetAttribute("aria-describedby")!.Split(' ').Contains(status.Id)));
        Assert.NotNull(status.QuerySelector(".tb-field__spinner"));
        Assert.Equal([FieldValidationStatus.Checking], _statuses);
    }

    [Fact]
    public async Task Status_ShouldShowAndReadOutTheValidMessage_WhenTheCheckPasses()
    {
        this.AddTrailblazor();
        var cut = RenderName(showStatus: true);

        cut.Find("input").Change("Robin");
        await cut.InvokeAsync(() => _answers.Single().SetResult([]));

        var status = cut.Find("[data-part=validation-status]");
        Assert.Equal(("valid", "That name is free.", null), (status.GetAttribute("data-status"),
            status.TextContent.Trim(), cut.Find("input").GetAttribute("aria-busy")));
        Assert.NotNull(status.QuerySelector("svg"));
        Assert.Contains("That name is free.", Announcer.Polite);
        Assert.Equal([FieldValidationStatus.Checking, FieldValidationStatus.Valid], _statuses);
    }

    [Fact]
    public async Task Status_ShouldPutAnIconBesideTheError_WhenTheCheckFails()
    {
        this.AddTrailblazor();
        var cut = RenderName(showStatus: true);

        cut.Find("input").Change("Robin");
        await cut.InvokeAsync(() => _answers.Single().SetResult(["That name is taken."]));

        Assert.Empty(cut.FindAll("[data-part=validation-status]"));
        Assert.NotNull(cut.Find("[data-part=error] [data-part=message] svg"));
        Assert.DoesNotContain("That name is free.", Announcer.Polite);
        Assert.Equal([FieldValidationStatus.Checking, FieldValidationStatus.Invalid], _statuses);
    }

    [Fact]
    public async Task Status_ShouldShowNothingButStillBeReported_WhenTheFieldDoesntShowIt()
    {
        this.AddTrailblazor();
        var cut = RenderName(showStatus: null);

        cut.Find("input").Change("Robin");
        var busy = cut.Find("input").GetAttribute("aria-busy");
        await cut.InvokeAsync(() => _answers.Single().SetResult([]));

        Assert.Equal((null, 0), (busy, cut.FindAll("[data-part=validation-status]").Count));
        Assert.Equal([FieldValidationStatus.Checking, FieldValidationStatus.Valid], _statuses);
    }

    [Fact]
    public async Task Status_ShouldFollowTheAppsSettingAndIcons_WhenTheFieldSetsNeither()
    {
        var valid = Icon.FromSvg("<svg viewBox=\"0 0 24 24\"><circle cx=\"12\" cy=\"12\" r=\"6\" /></svg>");
        this.AddTrailblazor(options =>
        {
            options.Fields.ShowValidationStatus = true;
            options.Fields.StatusIcons = new() { Valid = valid };
        });
        var cut = RenderName(showStatus: null);

        cut.Find("input").Change("Robin");
        await cut.InvokeAsync(() => _answers.Single().SetResult([]));

        Assert.Contains("<circle", cut.Find("[data-part=validation-status] svg").InnerHtml);
    }

    [Fact]
    public void StatusTemplate_ShouldDrawTheStatus_WhenGivenATemplate()
    {
        this.AddTrailblazor();
        var cut = RenderName(showStatus: true, template: status => builder => builder.AddContent(0, $"Now {status}"));

        cut.Find("input").Change("Robin");

        Assert.Equal("Now Checking", cut.Find("[data-part=validation-status]").TextContent.Trim());
    }

    [Fact]
    public async Task Form_ShouldBeValidatingOnlyWhileACheckRuns_WhenAFieldChanges()
    {
        this.AddTrailblazor();
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.ChildContent, Name(showStatus: null)));

        cut.Find("input").Change("Robin");
        var during = cut.Instance.IsValidating;
        await cut.InvokeAsync(() => _answers.Single().SetResult([]));

        Assert.Equal((true, false), (during, cut.Instance.IsValidating));
    }

    [Fact]
    public async Task Status_ShouldUseTheFieldsOwnIconsAndText_WhenItSetsThemOverTheApps()
    {
        var mine = Icon.FromSvg("<svg viewBox=\"0 0 24 24\"><rect width=\"8\" height=\"8\" /></svg>");
        this.AddTrailblazor(options => options.Fields.StatusIcons = new()
        {
            Valid = Icon.FromSvg("<svg viewBox=\"0 0 24 24\"><circle cx=\"12\" cy=\"12\" r=\"6\" /></svg>"),
        });
        var cut = RenderName(showStatus: true, configure: parameters => parameters
            .Add(field => field.StatusIcons, new FieldStatusIcons { Checking = mine, Valid = mine })
            .Add(field => field.CheckingMessage, "Asking the rangers…"));

        cut.Find("input").Change("Robin");
        var checking = cut.Find("[data-part=validation-status]");
        var during = (checking.TextContent.Trim(), checking.QuerySelector(".tb-field__spinner") is null,
            checking.QuerySelector("svg")!.InnerHtml.Contains("<rect", StringComparison.Ordinal));
        await cut.InvokeAsync(() => _answers.Single().SetResult([]));

        Assert.Equal(("Asking the rangers…", true, true), during);
        Assert.Contains("<rect", cut.Find("[data-part=validation-status] svg").InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Status_ShouldCheckAgain_WhenANewerValueArrivesWhileItShowsValid()
    {
        this.AddTrailblazor();
        var cut = RenderName(showStatus: true);
        cut.Find("input").Change("Robin");
        await cut.InvokeAsync(() => _answers[0].SetResult([]));

        cut.Find("input").Change("Sam");

        Assert.Equal("checking", cut.Find("[data-part=validation-status]").GetAttribute("data-status"));
        Assert.Equal([FieldValidationStatus.Checking, FieldValidationStatus.Valid, FieldValidationStatus.Checking],
            _statuses);
    }

    [Fact]
    public async Task Form_ShouldNoLongerBeValidating_WhenAFieldReportsItsCheckAnswered()
    {
        this.AddTrailblazor();
        var validating = new List<bool>();
        IRenderedComponent<TbForm>? form = null;
        form = Render<TbForm>(parameters => parameters.Add(tbForm => tbForm.ChildContent, builder =>
        {
            builder.OpenComponent<TbTextField<string>>(0);
            builder.AddComponentParameter(1, nameof(TbTextField<string>.Label), "Name");
            builder.AddComponentParameter(2, nameof(TbTextField<string>.ValidationAsync),
                (Func<string?, CancellationToken, Task<IEnumerable<string>>>)Check);
            builder.AddComponentParameter(3, nameof(TbTextField<string>.ValidationStatusChanged),
                EventCallback.Factory.Create<FieldValidationStatus>(this,
                    (FieldValidationStatus _) => validating.Add(form!.Instance.IsValidating)));
            builder.CloseComponent();
        }));

        form.Find("input").Change("Robin");
        await form.InvokeAsync(() => _answers.Single().SetResult([]));

        Assert.Equal([true, false], validating);
    }

    private IRenderedComponent<TbTextField<string>> RenderName(bool? showStatus,
        RenderFragment<FieldValidationStatus>? template = null,
        Action<ComponentParameterCollectionBuilder<TbTextField<string>>>? configure = null) =>
        Render<TbTextField<string>>(parameters =>
    {
        parameters.Add(field => field.Label, "Name").Add(field => field.ValidationAsync, Check)
            .Add(field => field.ShowValidationStatus, showStatus).Add(field => field.ValidMessage, "That name is free.")
            .Add(field => field.ValidationStatusChanged, (FieldValidationStatus status) => _statuses.Add(status));
        if (template is not null)
        {
            parameters.Add(field => field.StatusTemplate, template);
        }

        configure?.Invoke(parameters);
    });

    private RenderFragment Name(bool? showStatus) => builder =>
    {
        builder.OpenComponent<TbTextField<string>>(0);
        builder.AddComponentParameter(1, nameof(TbTextField<string>.Label), "Name");
        builder.AddComponentParameter(2, nameof(TbTextField<string>.ValidationAsync),
            (Func<string?, CancellationToken, Task<IEnumerable<string>>>)Check);
        builder.AddComponentParameter(3, nameof(TbTextField<string>.ShowValidationStatus), showStatus);
        builder.CloseComponent();
    };

    private Task<IEnumerable<string>> Check(string? value, CancellationToken token)
    {
        var answer = new TaskCompletionSource<IEnumerable<string>>();
        _answers.Add(answer);
        return answer.Task;
    }
}
