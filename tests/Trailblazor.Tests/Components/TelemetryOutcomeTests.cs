using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Time.Testing;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// How dialogs, toasts, forms and choices with a TelemetryName say how they ended.
public class TelemetryOutcomeTests : BunitContext
{
    private readonly FakeTimeProvider _time = new();
    private readonly IMeterFactory _meters;

    public TelemetryOutcomeTests()
    {
        Services.AddSingleton<TimeProvider>(_time);
        this.AddTrailblazor();
        _meters = Services.GetRequiredService<IMeterFactory>();
    }

    private sealed class EditTrail : TbDialogContent<bool>;

    private sealed class PickTrail<TTrail> : TbDialogContent<bool>;

    [Fact]
    public async Task Dialog_ShouldCountItsOpenHowItClosedAndHowLongItStayed_WhenItHasATelemetryName()
    {
        using var shown = Collect("trailblazor.control.shown");
        using var closed = Collect("trailblazor.dialog.closed");
        using var duration = new MetricCollector<double>(_meters, "Trailblazor", "trailblazor.dialog.duration");
        var dialogs = Services.GetRequiredService<IDialogService>();
        DialogOptions options = new() { TelemetryName = "edit-trail" };

        var confirmed = dialogs.Show<EditTrail, bool>("Edit Pine Hollow", options: options);
        _time.Advance(TimeSpan.FromSeconds(3));
        await confirmed.CloseAsync(true);
        var escaped = dialogs.Show<EditTrail, bool>("Edit Pine Hollow", options: options);
        await escaped.DismissAsync(DialogDismissReason.Escape);
        await dialogs.Show<EditTrail, bool>("Edit Pine Hollow", options: options).CancelAsync();
        dialogs.Show<EditTrail, bool>("Unlabeled");

        Assert.Equal(3, shown.GetMeasurementSnapshot().Count);
        Assert.Equal(["confirmed", "escape", "cancelled"], Outcomes(closed));
        Assert.All(closed.GetMeasurementSnapshot(), measurement =>
            Assert.Equal(nameof(EditTrail), measurement.Tags["trailblazor.component"]));
        Assert.Equal(3, duration.GetMeasurementSnapshot()[0].Value);
    }

    [Fact]
    public async Task Dialog_ShouldCountItsWindowButtons_WhenItHasATelemetryName()
    {
        using var used = Collect("trailblazor.control.used");
        var host = Render<DialogHost>();
        Services.GetRequiredService<IDialogService>().Show<EditTrail, bool>("Edit Pine Hollow",
            options: new() { TelemetryName = "edit-trail", Minimizable = true, Maximizable = true });

        await host.WaitForElement("[data-part=minimize]").ClickAsync(new());
        await host.Find("[data-part=minimize]").ClickAsync(new());
        await host.Find("[data-part=maximize]").ClickAsync(new());

        Assert.Equal(["minimize", "restore", "maximize"], used.GetMeasurementSnapshot()
            .Select(measurement => measurement.Tags["trailblazor.action"]));
    }

    [Fact]
    public async Task Dialog_ShouldNameAGenericContentTypeWithoutItsArity_WhenItCloses()
    {
        using var closed = Collect("trailblazor.dialog.closed");
        var dialogs = Services.GetRequiredService<IDialogService>();

        await dialogs.Show<PickTrail<int>, bool>("Pick a trail", options: new() { TelemetryName = "pick-trail" })
            .CancelAsync();

        Assert.Equal("PickTrail", Assert.Single(closed.GetMeasurementSnapshot()).Tags["trailblazor.component"]);
    }

    [Fact]
    public async Task Confirm_ShouldCountHowItWasAnswered_WhenGivenATelemetryName()
    {
        using var closed = Collect("trailblazor.dialog.closed");
        var dialogs = Services.GetRequiredService<IDialogService>();

        var answer = dialogs.ConfirmAsync("Delete the trail?", "It can't be undone.", telemetryName: "delete-trail");
        await dialogs.OpenDialogs.Single().CancelAsync();
        await answer;

        var measurement = Assert.Single(closed.GetMeasurementSnapshot());
        Assert.Equal("delete-trail", measurement.Tags["trailblazor.telemetry_name"]);
        Assert.Equal("ConfirmDialog", measurement.Tags["trailblazor.component"]);
        Assert.Equal("cancelled", measurement.Tags["trailblazor.outcome"]);
    }

    [Fact]
    public async Task Toast_ShouldCountItsShowingAndHowItClosed_WhenItHasATelemetryName()
    {
        using var shown = Collect("trailblazor.control.shown");
        using var closed = Collect("trailblazor.toast.closed");
        var toasts = Services.GetRequiredService<IToastService>();
        var host = Render<ToastHost>();
        ToastOptions options = new() { TelemetryName = "trail-saved" };
        toasts.Show("Saved.", options: options);
        toasts.Show("Copied.", options: options);
        toasts.Show("Deleted.", options: options with { Action = new("Undo", EventCallback.Empty) });
        host.WaitForAssertion(() => Assert.Equal(3, host.FindAll("[data-part=toast]").Count));

        host.FindAll("[data-part=close]")[1].Click();
        await host.Find("[data-part=action]").ClickAsync(new());
        _time.Advance(TimeSpan.FromSeconds(5));
        toasts.Show("Moved.", options: options).Dismiss();
        toasts.Show("Unlabeled.").Dismiss();

        Assert.Equal(4, shown.GetMeasurementSnapshot().Count);
        Assert.Equal(["dismissed", "action", "timed_out", "closed"], Outcomes(closed));
    }

    [Fact]
    public void Form_ShouldCountEachSubmitAndTheFieldsWithErrors_WhenTheyHaveTelemetryNames()
    {
        using var submitted = Collect("trailblazor.form.submitted");
        using var invalid = Collect("trailblazor.field.invalid");
        using var shown = Collect("trailblazor.control.shown");
        string? name = null;
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.TelemetryName, "new-trail")
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbTextField<string>>(0);
                builder.AddAttribute(1, nameof(TbTextField<string>.Label), "Name");
                builder.AddAttribute(2, nameof(TbTextField<string>.TelemetryName), "trail-name");
                builder.AddAttribute(3, nameof(TbTextField<string>.Value), name);
                builder.AddAttribute(4, nameof(TbTextField<string>.ValueChanged),
                    EventCallback.Factory.Create<string>(this, value => name = value));
                builder.AddAttribute(5, nameof(TbTextField<string>.Validation),
                    (Func<string?, IEnumerable<string>>)(value => string.IsNullOrWhiteSpace(value) ? ["Enter a name."]
                        : []));
                builder.CloseComponent();
            })));

        cut.Find("form").Submit();
        cut.Find("input").Change("Pine Hollow");
        cut.Find("form").Submit();

        Assert.Equal(["invalid", "valid"], Outcomes(submitted));
        Assert.Equal("trail-name", Assert.Single(invalid.GetMeasurementSnapshot()).Tags["trailblazor.telemetry_name"]);
        Assert.Equal(["new-trail", "trail-name"], shown.GetMeasurementSnapshot()
            .Select(measurement => measurement.Tags["trailblazor.telemetry_name"]).Order());
    }

    [Fact]
    public void Option_ShouldSayWhetherAPointerOrTheKeyboardChoseIt_WhenItHasATelemetryName()
    {
        using var used = Collect("trailblazor.control.used");
        var cut = Render<TbSelect<string>>(parameters => parameters.Add(select => select.Label, "Size")
            .AddChildContent<TbOption<string>>(option => option.Add(choice => choice.Value, "L")
                .Add(choice => choice.TelemetryName, "size-large").AddChildContent("Large")));

        cut.Find("[role=option]").Click(new MouseEventArgs { Detail = 1 });
        cut.Find("[role=option]").Click(new MouseEventArgs { Detail = 0 });

        Assert.Equal(["pointer", "keyboard"], used.GetMeasurementSnapshot()
            .Select(measurement => measurement.Tags["trailblazor.input"]));
    }

    [Fact]
    public void ListBox_ShouldCountEachChoiceAndHowItWasMade_WhenItHasATelemetryName()
    {
        using var used = Collect("trailblazor.control.used");
        var cut = Render<TbListBox<string>>(parameters => parameters.Add(listbox => listbox.Label, "Size")
            .Add(listbox => listbox.TelemetryName, "trail-size")
            .AddChildContent<TbOption<string>>(option => option.Add(choice => choice.Value, "L").AddChildContent("L")));

        cut.Find("[role=option]").Click(new MouseEventArgs { Detail = 1 });

        var use = Assert.Single(used.GetMeasurementSnapshot());
        Assert.Equal("TbListBox", use.Tags["trailblazor.component"]);
        Assert.Equal("pointer", use.Tags["trailblazor.input"]);
    }

    private MetricCollector<long> Collect(string instrument) => new(_meters, "Trailblazor", instrument);

    private static List<object?> Outcomes(MetricCollector<long> collector) =>
        [.. collector.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.outcome"])];
}
