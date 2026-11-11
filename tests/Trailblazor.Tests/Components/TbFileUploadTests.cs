using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Trailblazor.Tests.Components;

public class TbFileUploadTests : BunitContext
{
    private IReadOnlyList<IBrowserFile>? _files;
    private int _changes;

    public TbFileUploadTests()
    {
        this.AddTrailblazor();
    }

    private static InputFileContent Pdf(string name, int size = 10) =>
        InputFileContent.CreateFromBinary(new byte[size], name, contentType: "application/pdf");

    [Fact]
    public async Task Render_ShouldLeaveTheFieldReadyWithNothingLeftToRun_WhenItReturns()
    {
        // Rendered from the dispatcher, so work the field leaves for a later turn can't run before the check.
        var ready = await Renderer.Dispatcher.InvokeAsync(() =>
            RenderUpload().Find("input[type=file]").HasAttribute("data-ready"));

        Assert.True(ready);
    }

    [Fact]
    public void Render_ShouldTurnOffThePickerAndEveryButton_WhenReadOnly()
    {
        var cut = RenderUpload(parameters => parameters.Add(upload => upload.Multiple, true));
        cut.FindComponent<InputFile>().UploadFiles(Pdf("trail.pdf"));
        cut.Render(parameters => parameters.Add(upload => upload.ReadOnly, true));

        Assert.True(cut.Find("input[type=file]").HasAttribute("disabled"));
        Assert.All(cut.FindAll("button"), button => Assert.True(button.HasAttribute("disabled")));
        Assert.Contains("tb-field--readonly", cut.Find(".tb-file-upload").ClassList);
    }

    [Fact]
    public void Choose_ShouldListTheFilesWithTheirSizes_WhenTheyAreTaken()
    {
        var cut = RenderUpload(parameters => parameters.Add(upload => upload.Multiple, true));

        cut.FindComponent<InputFile>().UploadFiles(Pdf("trail.pdf", 1_500), Pdf("map.pdf"));

        Assert.Equal(["trail.pdf", "map.pdf"], _files!.Select(file => file.Name));
        Assert.Equal(["trail.pdf 1.5 KB", "map.pdf 10 B"], cut.FindAll("[data-part=file]")
            .Select(file => $"{file.QuerySelector(".tb-file-upload__name")!.TextContent} "
                + file.QuerySelector(".tb-file-upload__size")!.TextContent));
    }

    [Fact]
    public void Choose_ShouldLeaveOutAndExplainFilesOfAnotherType_WhenGivenAnAcceptList()
    {
        var cut = RenderUpload(parameters => parameters.Add(upload => upload.Multiple, true)
            .Add(upload => upload.Accept, ".pdf, image/*"));

        cut.FindComponent<InputFile>().UploadFiles(Pdf("trail.pdf"),
            InputFileContent.CreateFromText("x", "notes.docx", contentType: "application/msword"),
            InputFileContent.CreateFromBinary([1], "photo.jpg", contentType: "image/jpeg"));

        Assert.Equal(["trail.pdf", "photo.jpg"], _files!.Select(file => file.Name));
        Assert.Equal("notes.docx isn't one of .pdf, image/*.", cut.Find("[data-part=rejected]").TextContent.Trim());
    }

    [Fact]
    public void Choose_ShouldKeepTheFirstFilesAndSaySo_WhenThereAreMoreThanMaxFiles()
    {
        var cut = RenderUpload(parameters => parameters.Add(upload => upload.Multiple, true)
            .Add(upload => upload.MaxFiles, 2));

        cut.FindComponent<InputFile>().UploadFiles(Pdf("a.pdf"), Pdf("b.pdf"), Pdf("c.pdf"));

        Assert.Equal(["a.pdf", "b.pdf"], _files!.Select(file => file.Name));
        Assert.Equal("Choose at most 2 files.", cut.Find("[data-part=rejected]").TextContent.Trim());
    }

    [Fact]
    public void Choose_ShouldAskTheLocalizerForTheCountedWords_WhenThereAreMoreThanMaxFiles()
    {
        Services.AddScoped<ITbLocalizer, PluralRecorder>();
        var cut = RenderUpload(parameters => parameters.Add(upload => upload.Multiple, true)
            .Add(upload => upload.MaxFiles, 2));

        cut.FindComponent<InputFile>().UploadFiles(Pdf("a.pdf"), Pdf("b.pdf"), Pdf("c.pdf"));

        Assert.Equal("FileUpload.TooMany for 2", cut.Find("[data-part=rejected]").TextContent.Trim());
    }

    [Fact]
    public void Choose_ShouldAskForOneFile_WhenOnlyOneIsAllowed()
    {
        var cut = RenderUpload(parameters => parameters.Add(upload => upload.Multiple, true)
            .Add(upload => upload.MaxFiles, 1));

        cut.FindComponent<InputFile>().UploadFiles(Pdf("a.pdf"), Pdf("b.pdf"));

        Assert.Equal("Choose only one file.", cut.Find("[data-part=rejected]").TextContent.Trim());
    }

    [Fact]
    public void Choose_ShouldFilterAndCheckByAnInputAttributesAccept_WhenAcceptIsntSet()
    {
        var cut = RenderUpload(parameters => parameters.Add(upload => upload.Multiple, true)
            .Add(upload => upload.InputAttributes, new Dictionary<string, object> { ["accept"] = ".pdf" }));

        cut.FindComponent<InputFile>().UploadFiles(Pdf("trail.pdf"),
            InputFileContent.CreateFromText("x", "notes.docx", contentType: "application/msword"));

        Assert.Equal(".pdf", cut.Find("input[type=file]").GetAttribute("accept"));
        Assert.Equal(["trail.pdf"], _files!.Select(file => file.Name));
        Assert.Equal("notes.docx isn't one of .pdf.", cut.Find("[data-part=rejected]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldUseAccept_WhenInputAttributesHaveAnotherToo()
    {
        var cut = RenderUpload(parameters => parameters.Add(upload => upload.Accept, ".gpx")
            .Add(upload => upload.InputAttributes, new Dictionary<string, object> { ["accept"] = ".pdf" }));

        Assert.Equal(".gpx", cut.Find("input[type=file]").GetAttribute("accept"));
    }

    [Fact]
    public void Choose_ShouldTakeFilesOfOtherTypesAndStillFilterThePicker_WhenRejectOtherTypesIsOff()
    {
        var cut = RenderUpload(parameters => parameters.Add(upload => upload.Multiple, true)
            .Add(upload => upload.Accept, ".pdf").Add(upload => upload.RejectOtherTypes, false));

        cut.FindComponent<InputFile>().UploadFiles(Pdf("trail.pdf"),
            InputFileContent.CreateFromText("x", "notes.docx", contentType: "application/msword"));

        Assert.Equal(".pdf", cut.Find("input[type=file]").GetAttribute("accept"));
        Assert.Equal(["trail.pdf", "notes.docx"], _files!.Select(file => file.Name));
        Assert.Empty(cut.FindAll("[data-part=rejected]"));
    }

    [Fact]
    public void Choose_ShouldLeaveOutAFileOver512Kilobytes_WhenMaxFileSizeIsntSet()
    {
        var cut = RenderUpload();

        cut.FindComponent<InputFile>().UploadFiles(Pdf("atlas.pdf", 512_001));

        Assert.Null(_files);
        Assert.Equal("atlas.pdf is larger than 512 KB.", cut.Find("[data-part=rejected]").TextContent.Trim());
    }

    [Fact]
    public void Choose_ShouldLeaveOutAFileOverTheLimit_WhenItIsTooLarge()
    {
        var cut = RenderUpload(parameters => parameters.Add(upload => upload.MaxFileSize, 1_000));

        cut.FindComponent<InputFile>().UploadFiles(Pdf("big.pdf", 2_000));

        Assert.Null(_files);
        Assert.Equal("big.pdf is larger than 1 KB.", cut.Find("[data-part=rejected]").TextContent.Trim());
        Assert.Contains($"{cut.Find("[data-part=rejected]").Id}",
            cut.Find("[data-part=input]").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void Choose_ShouldReplaceTheEarlierFiles_WhenFilesAreChosenAgain()
    {
        var cut = RenderUpload(parameters => parameters.Add(upload => upload.Multiple, true));

        cut.FindComponent<InputFile>().UploadFiles(Pdf("a.pdf"));
        cut.FindComponent<InputFile>().UploadFiles(Pdf("b.pdf"));

        Assert.Equal(["b.pdf"], _files!.Select(file => file.Name));
    }

    [Fact]
    public void Remove_ShouldDropOnlyThatFile_WhenClicked()
    {
        var cut = RenderUpload(parameters => parameters.Add(upload => upload.Multiple, true));
        cut.FindComponent<InputFile>().UploadFiles(Pdf("a.pdf"), Pdf("b.pdf"));

        cut.Find("[data-part=remove][aria-label='Remove a.pdf']").Click();

        Assert.Equal(["b.pdf"], _files!.Select(file => file.Name));
    }

    [Fact]
    public void Clear_ShouldEmptyTheField_WhenClicked()
    {
        var cut = RenderUpload();
        cut.FindComponent<InputFile>().UploadFiles(Pdf("a.pdf"));

        cut.Find("[data-part=clear]").Click();

        Assert.Null(_files);
        Assert.Empty(cut.FindAll("[data-part=file]"));
    }

    [Fact]
    public async Task OpenAsync_ShouldAskTheBrowserToOpenThePicker_WhenTheFieldIsEnabled()
    {
        var cut = RenderUpload();

        await cut.InvokeAsync(cut.Instance.OpenAsync);

        Assert.Single(JSInterop.Invocations["openPicker"]);
    }

    [Fact]
    public async Task OpenAsync_ShouldDoNothing_WhenTheFieldIsDisabled()
    {
        var cut = RenderUpload(parameters => parameters.Add(upload => upload.Disabled, true));

        await cut.InvokeAsync(cut.Instance.OpenAsync);

        Assert.Empty(JSInterop.Invocations["openPicker"]);
    }

    [Fact]
    public async Task ClearAsync_ShouldEmptyTheFieldAndSaySo_WhenCalled()
    {
        var cut = RenderUpload();
        cut.FindComponent<InputFile>().UploadFiles(Pdf("a.pdf"));

        await cut.InvokeAsync(cut.Instance.ClearAsync);

        Assert.Null(_files);
        Assert.Equal(2, _changes);
        Assert.Empty(cut.FindAll("[data-part=file]"));
    }

    [Fact]
    public void Render_ShouldKeepTheFieldOutOfSightAndReachButShowWhyFilesWereLeftOut_WhenHidden()
    {
        var cut = RenderUpload(parameters => parameters.Add(upload => upload.Hidden, true)
            .Add(upload => upload.Multiple, true).Add(upload => upload.MaxFileSize, 100));

        cut.FindComponent<InputFile>().UploadFiles(Pdf("trail.pdf"), Pdf("atlas.pdf", 200));

        Assert.Equal(["trail.pdf"], _files!.Select(file => file.Name));
        Assert.All(cut.FindAll("[data-part=label], [data-part=zone]"), part =>
        {
            Assert.Contains("tb-visually-hidden", part.ClassList);
            Assert.Equal("true", part.GetAttribute("aria-hidden"));
        });
        Assert.Equal("-1", cut.Find("[data-part=input]").GetAttribute("tabindex"));
        Assert.Empty(cut.FindAll("button"));
        Assert.Null(cut.Find("[data-part=rejected]").Closest("[aria-hidden]"));
    }

    [Fact]
    public void Render_ShouldBeAButtonNamedByItsText_WhenThereIsNoDropZoneOrLabel()
    {
        var cut = Render<TbFileUpload>(parameters => parameters.Add(upload => upload.DropZone, false));

        Assert.Single(cut.FindAll(".tb-file-upload__button"));
        Assert.Equal("Choose files", cut.Find("[data-part=input]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_ShouldNotRaiseValueChanged_WhenTheParentSetsTheFiles()
    {
        var cut = RenderUpload();

        cut.Render(parameters => parameters.Add(upload => upload.Value, []));

        Assert.Equal(0, _changes);
    }

    private IRenderedComponent<TbFileUpload> RenderUpload(
        Action<ComponentParameterCollectionBuilder<TbFileUpload>>? configure = null) =>
        Render<TbFileUpload>(parameters =>
        {
            parameters.Add(upload => upload.Label, "Documents").Add(upload => upload.Value, _files)
                .Add(upload => upload.ValueChanged, (IReadOnlyList<IBrowserFile>? files) =>
                {
                    _files = files;
                    _changes++;
                });
            configure?.Invoke(parameters);
        });
}
