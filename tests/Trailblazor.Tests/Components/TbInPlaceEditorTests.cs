using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.JSInterop;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbInPlaceEditorTests : BunitContext
{
    private readonly BunitJSModuleInterop _handle;
    private readonly List<string?> _saved = [];

    public TbInPlaceEditorTests()
    {
        this.AddTrailblazor();
        _handle = JSInterop.SetupModule("./_content/Trailblazor/in-place-editor.js")
            .SetupModule("attachInPlaceEditor", _ => true);
        _handle.SetupVoid("focusEditor").SetVoidResult();
        _handle.SetupVoid("dispose").SetVoidResult();
    }

    // A plain input that edits the draft, as an app's field would.
    private RenderFragment<InPlaceEditorContext<string>> Input => context => builder =>
    {
        builder.OpenElement(0, "input");
        builder.AddAttribute(1, "aria-label", "Trail name");
        builder.AddAttribute(2, "value", context.Draft);
        builder.AddAttribute(3, "onchange",
            EventCallback.Factory.CreateBinder<string?>(this, value => context.Draft = value, context.Draft));
        builder.CloseElement();
    };

    private IRenderedComponent<TbInPlaceEditor<string>> RenderEditor(string? value = "Pine Hollow",
        Action<ComponentParameterCollectionBuilder<TbInPlaceEditor<string>>>? configure = null) =>
        Render<TbInPlaceEditor<string>>(parameters =>
        {
            parameters.Add(editor => editor.Label, "Trail name").Add(editor => editor.Value, value)
                .Add(editor => editor.ValueChanged, saved => _saved.Add(saved)).Add(editor => editor.Editor, Input)
                .Add(editor => editor.Placeholder, "Add a name");
            configure?.Invoke(parameters);
        });

    // The accessible name built from the elements aria-labelledby points at, in order.
    private static string Name(IRenderedComponent<TbInPlaceEditor<string>> cut, AngleSharp.Dom.IElement element) =>
        string.Join(" ", element.GetAttribute("aria-labelledby")!.Split(' ')
            .Select(id => cut.Find($"#{id}").TextContent.Trim()));

    [Fact]
    public void Render_ShouldShowTheValueInAButtonNamedForEditingIt_WhenCalled()
    {
        var cut = RenderEditor();
        var empty = RenderEditor(value: null);

        var display = cut.Find("[data-part=display]");
        Assert.Equal(("button", "Edit Trail name Pine Hollow"), (display.LocalName, Name(cut, display)));
        Assert.Equal("Add a name", empty.Find("[data-part=value]").TextContent.Trim());
        Assert.Contains("tb-in-place-editor__value--empty", empty.Find("[data-part=value]").ClassList);
    }

    [Fact]
    public async Task EditAsync_ShouldShowTheEditorWithTheDraft_WhenPressed()
    {
        var cut = RenderEditor();

        await cut.Tb().InPlaceEditor("Trail name").EditAsync();

        var editor = cut.Find("[data-part=editor]");
        Assert.True(cut.Tb().InPlaceEditor("Trail name").IsEditing);
        Assert.Equal(("group", "Trail name"), (editor.GetAttribute("role"),
            cut.Find($"#{editor.GetAttribute("aria-labelledby")}").TextContent));
        Assert.Equal("Pine Hollow", cut.Find("input").GetAttribute("value"));
        Assert.Equal(("Save", "Cancel"), (cut.Find("[data-part=save]").GetAttribute("aria-label"),
            cut.Find("[data-part=cancel]").GetAttribute("aria-label")));
        Assert.Single(_handle.Invocations["focusEditor"]);
    }

    [Fact]
    public async Task SaveAsync_ShouldSetTheValueToTheDraft_WhenPressed()
    {
        var cut = RenderEditor();
        var editor = cut.Tb().InPlaceEditor("Trail name");
        await editor.EditAsync();

        await cut.Find("input").ChangeAsync(new() { Value = "Ridge loop" });
        await editor.SaveAsync();

        Assert.Equal(["Ridge loop"], _saved);
        Assert.Equal(("Ridge loop", false), (editor.Text, editor.IsEditing));
    }

    [Fact]
    public async Task CancelAsync_ShouldKeepTheValue_WhenPressed()
    {
        var cut = RenderEditor();
        var editor = cut.Tb().InPlaceEditor("Trail name");
        await editor.EditAsync();

        await cut.Find("input").ChangeAsync(new() { Value = "Ridge loop" });
        await editor.CancelAsync();

        Assert.Empty(_saved);
        Assert.Equal(("Pine Hollow", false), (editor.Text, editor.IsEditing));
    }

    [Fact]
    public async Task Keys_ShouldSaveOrCancel_WhenTheScriptHearsEnterOrEscape()
    {
        var cut = RenderEditor();
        var editor = cut.Tb().InPlaceEditor("Trail name");
        var events = (DotNetObjectReference<InPlaceEditorEvents>)JSInterop.Invocations["attachInPlaceEditor"]
            .Single().Arguments[1]!;

        await editor.EditAsync();
        await cut.Find("input").ChangeAsync(new() { Value = "Ridge loop" });
        await cut.InvokeAsync(events.Value.OnCancel);
        await editor.EditAsync();
        await cut.Find("input").ChangeAsync(new() { Value = "Eagle Ridge" });
        await cut.InvokeAsync(events.Value.OnSave);

        Assert.Equal(["Eagle Ridge"], _saved);
    }

    [Fact]
    public async Task Render_ShouldNotEdit_WhenDisabled()
    {
        var cut = RenderEditor(configure: parameters => parameters.Add(editor => editor.Disabled, true));

        Assert.True(cut.Find("[data-part=display]").HasAttribute("disabled"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().InPlaceEditor("Trail name").EditAsync());
    }

    [Fact]
    public async Task TelemetryName_ShouldCountEditingSavingAndCanceling_WhenGiven()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var cut = RenderEditor(configure: parameters => parameters.Add(editor => editor.TelemetryName, "trail-name"));
        var editor = cut.Tb().InPlaceEditor("Trail name");

        await editor.EditAsync();
        await editor.SaveAsync();
        await editor.EditAsync();
        await editor.CancelAsync();

        Assert.Equal(["edit", "save", "edit", "cancel"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));
    }

    [Fact]
    public async Task Labels_ShouldNameTheButtons_WhenGiven()
    {
        var cut = RenderEditor(configure: parameters => parameters.Add(editor => editor.Labels,
            new InPlaceEditorLabels { Edit = "Change", Save = "Keep", Cancel = "Undo" }));

        Assert.Equal("Change Trail name Pine Hollow", Name(cut, cut.Find("[data-part=display]")));
        await cut.Tb().InPlaceEditor("Trail name").EditAsync();
        Assert.Equal("Keep", cut.Find("[data-part=save]").GetAttribute("aria-label"));
        Assert.Equal("Undo", cut.Find("[data-part=cancel]").GetAttribute("aria-label"));
    }
}
