using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// A TbFileUpload given Upload: each send reports half its bytes, then waits for the test to finish, fail or cancel it.
public class TbFileUploadSendTests : BunitContext
{
    private readonly Dictionary<string, TaskCompletionSource> _sends = [];
    private readonly List<string> _started = [];
    private readonly List<CancellationToken> _tokens = [];

    public TbFileUploadSendTests() => this.AddTrailblazor();

    private static InputFileContent Pdf(string name) =>
        InputFileContent.CreateFromBinary(new byte[100], name, contentType: "application/pdf");

    private async Task SendAsync(IBrowserFile file, IProgress<long> progress, CancellationToken stop)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _sends[file.Name] = done;
        _started.Add(file.Name);
        _tokens.Add(stop);
        progress.Report(file.Size / 2);
        await done.Task.WaitAsync(stop);
    }

    private IReadOnlyList<string> Polite => Services.GetRequiredService<FakeAnnouncer>().Polite;

    [Fact]
    public async Task Upload_ShouldShowProgressThenUploaded_WhenTheUploadSucceeds()
    {
        var cut = RenderUpload();
        var field = cut.Tb().FileUpload("Documents");

        await field.ChooseAsync(Pdf("trail.pdf"));
        var during = (field.Status("trail.pdf"), cut.Find("[role=progressbar]")
            .GetAttribute("aria-valuenow"), cut.FindAll("[data-part=cancel]").Count);
        await cut.InvokeAsync(() => _sends["trail.pdf"].SetResult());
        await field.WaitForUploadsAsync();

        Assert.Equal((FileUploadStatus.Uploading, "50", 1), during);
        Assert.Equal(FileUploadStatus.Uploaded, field.Status("trail.pdf"));
        Assert.Equal("Uploaded", cut.Find("[data-part=status]").TextContent.Trim());
        Assert.Contains("trail.pdf uploaded.", Polite);
    }

    [Fact]
    public async Task Retry_ShouldSendTheFileAgain_WhenItsUploadFailed()
    {
        var cut = RenderUpload();
        var field = cut.Tb().FileUpload("Documents");
        await field.ChooseAsync(Pdf("trail.pdf"));

        await cut.InvokeAsync(() => _sends["trail.pdf"].SetException(new IOException("Disk full")));
        await field.WaitForUploadsAsync();
        var failed = (field.Status("trail.pdf"), cut.Find("[data-part=status]").TextContent.Trim());
        await field.RetryAsync("trail.pdf");
        await cut.InvokeAsync(() => _sends["trail.pdf"].SetResult());
        await field.WaitForUploadsAsync();

        Assert.Equal((FileUploadStatus.Failed, "Couldn't upload"), failed);
        Assert.Contains("trail.pdf didn't upload.", Polite);
        Assert.Equal(["trail.pdf", "trail.pdf"], _started);
        Assert.Equal(FileUploadStatus.Uploaded, field.Status("trail.pdf"));
    }

    [Fact]
    public async Task Cancel_ShouldStopTheUploadAndOfferRetry_WhenPressed()
    {
        var cut = RenderUpload();
        var field = cut.Tb().FileUpload("Documents");
        await field.ChooseAsync(Pdf("trail.pdf"));

        await field.CancelAsync("trail.pdf");
        await field.WaitForUploadsAsync();

        Assert.True(_tokens.Single().IsCancellationRequested);
        Assert.DoesNotContain(Polite, message => message.Contains("upload"));
        Assert.Equal(FileUploadStatus.Canceled, field.Status("trail.pdf"));
        Assert.Equal("Canceled", cut.Find("[data-part=status]").TextContent.Trim());
        Assert.NotNull(cut.Find("[data-part=retry]"));
    }

    [Fact]
    public async Task Upload_ShouldSendAtMostMaxConcurrentAtOnce_WhenMoreFilesAreChosen()
    {
        var cut = RenderUpload(parameters => parameters.Add(upload => upload.MaxConcurrent, 2));
        var field = cut.Tb().FileUpload("Documents");

        await field.ChooseAsync(Pdf("a.pdf"), Pdf("b.pdf"), Pdf("c.pdf"));
        var first = string.Join(",", _started);
        var third = field.Status("c.pdf");
        await cut.InvokeAsync(() => _sends["a.pdf"].SetResult());

        Assert.Equal(("a.pdf,b.pdf", FileUploadStatus.Chosen), (first, third));
        // The finished upload resumes on its own, so the third file starts a moment later.
        cut.WaitForAssertion(() => Assert.Equal(FileUploadStatus.Uploading, field.Status("c.pdf")));
        Assert.Equal(["a.pdf", "b.pdf", "c.pdf"], _started);
    }

    [Fact]
    public async Task Upload_ShouldWaitForTheUploadButton_WhenAutoUploadIsOff()
    {
        var cut = RenderUpload(parameters => parameters.Add(upload => upload.AutoUpload, false));
        var field = cut.Tb().FileUpload("Documents");

        await field.ChooseAsync(Pdf("trail.pdf"));
        var before = (_started.Count, field.Status("trail.pdf"));
        await field.UploadAsync();

        Assert.Equal((0, FileUploadStatus.Chosen), before);
        Assert.Equal(["trail.pdf"], _started);
        Assert.True(cut.Find("[data-part=upload]").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Clear_ShouldStopTheUploads_WhenTheFieldIsEmptied()
    {
        var cut = RenderUpload();
        var field = cut.Tb().FileUpload("Documents");
        await field.ChooseAsync(Pdf("trail.pdf"));

        await cut.InvokeAsync(() => cut.Instance.ClearAsync());

        Assert.True(_tokens.Single().IsCancellationRequested);
        Assert.Empty(field.Files);
    }

    [Fact]
    public async Task Cancel_ShouldBeOff_WhenTheFieldIsReadOnly()
    {
        var cut = RenderUpload();
        await cut.Tb().FileUpload("Documents").ChooseAsync(Pdf("trail.pdf"));

        cut.Render(parameters => parameters.Add(upload => upload.ReadOnly, true));

        Assert.True(cut.Find("[data-part=cancel]").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Reset_ShouldStopTheUploadsAndEndTheWait_WhenTheFormResets()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenComponent<TbFileUpload>(0);
            builder.AddAttribute(1, nameof(TbFileUpload.Label), "Documents");
            builder.AddAttribute(2, nameof(TbFileUpload.Multiple), true);
            builder.AddAttribute(3, nameof(TbFileUpload.MaxConcurrent), 1);
            builder.AddAttribute(4, nameof(TbFileUpload.Upload),
                (Func<IBrowserFile, IProgress<long>, CancellationToken, Task>)SendAsync);
            builder.CloseComponent();
        })));
        var field = cut.Tb().FileUpload("Documents");
        await field.ChooseAsync(Pdf("a.pdf"), Pdf("b.pdf"));

        await cut.InvokeAsync(() => cut.Instance.ResetAsync());
        await field.WaitForUploadsAsync();

        Assert.True(_tokens.Single().IsCancellationRequested);
        Assert.Equal(["a.pdf"], _started);
        Assert.Empty(field.Files);
    }

    [Fact]
    public async Task Value_ShouldStopTheUploadsOfTheFilesItLeavesOut_WhenTheAppSetsIt()
    {
        var cut = RenderUpload();
        var field = cut.Tb().FileUpload("Documents");
        await field.ChooseAsync(Pdf("trail.pdf"));

        cut.Render(parameters => parameters.Add(upload => upload.Value, null));
        await field.WaitForUploadsAsync();

        Assert.True(_tokens.Single().IsCancellationRequested);
    }

    [Fact]
    public async Task Remove_ShouldKeepAWaitingFileFromBeingSent_WhenPressed()
    {
        var cut = RenderUpload(parameters => parameters.Add(upload => upload.MaxConcurrent, 1));
        var field = cut.Tb().FileUpload("Documents");
        await field.ChooseAsync(Pdf("a.pdf"), Pdf("b.pdf"));

        await cut.Find("[data-name='b.pdf'] [data-part=remove]").ClickAsync();
        await cut.InvokeAsync(() => _sends["a.pdf"].SetResult());
        await field.WaitForUploadsAsync();

        Assert.Equal(["a.pdf"], _started);
        Assert.Equal(["a.pdf"], field.Files);
    }

    [Fact]
    public async Task FileTemplate_ShouldShowInPlaceOfTheNameAndSize_WhenGiven()
    {
        var cut = RenderUpload(parameters => parameters.Add(upload => upload.FileTemplate,
            item => builder => builder.AddMarkupContent(0, $"<b class=\"row\">{item.File.Name}: {item.Status}</b>")));
        var field = cut.Tb().FileUpload("Documents");

        await field.ChooseAsync(Pdf("trail.pdf"));

        Assert.Equal("trail.pdf: Uploading", cut.Find("[data-part=file] .row").TextContent);
        Assert.Empty(cut.FindAll(".tb-file-upload__name"));
        Assert.Equal(["trail.pdf"], field.Files);
    }

    private IRenderedComponent<TbFileUpload> RenderUpload(
        Action<ComponentParameterCollectionBuilder<TbFileUpload>>? configure = null) =>
        Render<TbFileUpload>(parameters =>
        {
            parameters.Add(upload => upload.Label, "Documents").Add(upload => upload.Multiple, true)
                .Add(upload => upload.Upload, SendAsync);
            configure?.Invoke(parameters);
        });
}
