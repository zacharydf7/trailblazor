using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs;

public class FileUploadExampleTests : DocsTest
{
    private static InputFileContent Pdf(string name, int size = 10) =>
        InputFileContent.CreateFromBinary(new byte[size], name, contentType: "application/pdf");

    [Fact]
    public void FileUploadExample_ShouldListTheChosenFiles_WhenFilesAreChosen()
    {
        var cut = Render<FileUploadExample>();
        Assert.Contains("Trail maps value: null", cut.Markup);

        cut.FindComponent<InputFile>().UploadFiles(Pdf("map.pdf"), Pdf("ridge.pdf"));

        Assert.Contains("Trail maps value: map.pdf, ridge.pdf", cut.Markup);
    }

    [Fact]
    public async Task SendExample_ShouldListTheFileSent_WhenItIsChosen()
    {
        var cut = Render<FileUploadSendExample>();
        var photos = cut.Tb().FileUpload("Trip photos");

        await photos.ChooseAsync(InputFileContent.CreateFromBinary(new byte[100], "ridge.png", contentType: "image/png"));
        await photos.WaitForUploadsAsync();

        Assert.Equal(FileUploadStatus.Uploaded, photos.Status("ridge.png"));
        Assert.Equal("Sent: ridge.png.", cut.Find("output").TextContent);
    }

    [Fact]
    public async Task SendExample_ShouldOfferRetry_WhenTheUploadsFail()
    {
        var cut = Render<FileUploadSendExample>();
        await cut.Tb().Switch("Make the uploads fail").CheckAsync();
        var photos = cut.Tb().FileUpload("Trip photos");

        await photos.ChooseAsync(InputFileContent.CreateFromBinary(new byte[100], "ridge.png", contentType: "image/png"));
        await photos.WaitForUploadsAsync();

        Assert.Equal(FileUploadStatus.Failed, photos.Status("ridge.png"));
        Assert.NotNull(cut.Find("[data-part=retry]"));
    }

    [Fact]
    public async Task OnRequestExample_ShouldSendTheFiles_WhenUploadIsPressed()
    {
        var cut = Render<FileUploadOnRequestExample>();
        var routes = cut.Tb().FileUpload("Route files");
        await routes.ChooseAsync(Pdf("ridge.pdf"));
        var before = routes.Status("ridge.pdf");

        await routes.UploadAsync();
        await routes.WaitForUploadsAsync();

        Assert.Equal((FileUploadStatus.Chosen, FileUploadStatus.Uploaded), (before, routes.Status("ridge.pdf")));
    }

    [Fact]
    public async Task TemplateExample_ShouldShowHowMuchWasSent_WhenAFileIsSent()
    {
        var cut = Render<FileUploadTemplateExample>();
        var notes = cut.Tb().FileUpload("Field notes");

        await notes.ChooseAsync(Pdf("ridge.pdf", 2048));
        await notes.WaitForUploadsAsync();

        Assert.Equal("2.0 of 2.0 KB sent", cut.Find("[data-part=file] .tb-text").TextContent.Trim());
        Assert.Equal(["ridge.pdf"], notes.Files);
    }

    [Fact]
    public void LimitsExample_ShouldSayWhyAFileWasLeftOut_WhenItIsAnotherType()
    {
        var cut = Render<FileUploadLimitsExample>();

        cut.FindComponent<InputFile>().UploadFiles(Pdf("notes.pdf"));

        Assert.Contains("notes.pdf isn't one of", cut.Find("[data-part=rejected]").TextContent);
    }

    [Fact]
    public void LimitsExample_ShouldSayWhyAFileWasLeftOut_WhenItIsTooLarge()
    {
        var cut = Render<FileUploadLimitsExample>();

        cut.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromBinary(new byte[1_000_001], "huge.png", contentType: "image/png"));

        Assert.Contains("huge.png is larger than", cut.Find("[data-part=rejected]").TextContent);
    }

    [Fact]
    public void OtherTypesExample_ShouldTakeTheFile_WhenItIsAnotherType()
    {
        var cut = Render<FileUploadOtherTypesExample>();

        cut.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromText("x", "notes.docx", contentType: "application/msword"));

        Assert.Empty(cut.FindAll("[data-part=rejected]"));
        Assert.Empty(cut.FindAll("[data-part=error]"));
        Assert.Single(cut.FindAll("[data-part=file]"));
    }

    [Fact]
    public void OtherTypesExample_ShouldShowItsOwnMessage_WhenTheTypeIsNotOneItChecksFor()
    {
        var cut = Render<FileUploadOtherTypesExample>();

        cut.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromText("x", "photo.png", contentType: "image/png"));

        Assert.Equal("Attach only PDF or Word files.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void ZoneExample_ShouldShowItsOwnContent_WhenRendered()
    {
        var cut = Render<FileUploadZoneExample>();

        Assert.Contains("Drop a GPX file here", cut.Find("[data-part=zone]").TextContent);
    }

    [Fact]
    public void OwnButtonExample_ShouldOpenThePicker_WhenItsButtonIsClicked()
    {
        var cut = Render<FileUploadOwnButtonExample>();

        Button(cut, "Import a route").Click();

        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations["openPicker"]));
    }

    [Fact]
    public async Task OwnButtonExample_ShouldTakeAFileFromTheTestHelpers_WhenTheFieldIsHidden()
    {
        var folder = Directory.CreateTempSubdirectory();
        try
        {
            var route = Path.Combine(folder.FullName, "ridge.gpx");
            await File.WriteAllBytesAsync(route, new byte[3], Xunit.TestContext.Current.CancellationToken);
            var cut = Render<FileUploadOwnButtonExample>();

            await cut.Tb().FileUpload("Import a route").ChooseAsync(route);

            Assert.Contains("Route value: ridge.gpx", cut.Markup);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [Fact]
    public void OwnButtonExample_ShouldForgetTheRoute_WhenItIsCleared()
    {
        var cut = Render<FileUploadOwnButtonExample>();
        cut.FindComponent<InputFile>().UploadFiles(Pdf("ridge.gpx"));
        Assert.Contains("Route value: ridge.gpx", cut.Markup);

        Button(cut, "Forget the route").Click();

        Assert.Contains("Route value: null", cut.Markup);
    }

    [Fact]
    public void ReadExample_ShouldReadTheFile_WhenItIsChosen()
    {
        var cut = Render<FileUploadReadExample>();

        cut.FindComponent<InputFile>().UploadFiles(Pdf("notes.pdf", 42));

        cut.WaitForAssertion(() => Assert.Contains("Bytes read: 42", cut.Markup));
    }

    [Fact]
    public void RequiredExample_ShouldShowItsMessage_WhenItAppears()
    {
        var cut = Render<FileUploadRequiredExample>();

        cut.WaitForAssertion(() => Assert.Equal("Attach your permit form.",
            cut.Find("[data-part=error]").TextContent.Trim()));
    }

    [Fact]
    public void ValidationExample_ShouldShowItsMessageUntilTheFileIsRenamed_WhenTheNameHasASpace()
    {
        var cut = Render<FileUploadValidationExample>();
        Assert.Empty(cut.FindAll("[data-part=error]"));

        cut.FindComponent<InputFile>().UploadFiles(Pdf("ranger report.pdf"));
        Assert.Equal("Rename the file so its name has no spaces.", cut.Find("[data-part=error]").TextContent.Trim());

        cut.FindComponent<InputFile>().UploadFiles(Pdf("ranger-report.pdf"));

        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public void ErrorExample_ShouldShowTheErrorUntilTheFileChanges_WhenTheCheckFindsAProblem()
    {
        var cut = Render<FileUploadErrorExample>();
        Assert.Empty(cut.FindAll("[data-part=error]"));

        Button(cut, "Check the waiver").Click();
        Assert.Equal("The server has no waiver for this trip.", cut.Find("[data-part=error]").TextContent.Trim());

        cut.FindComponent<InputFile>().UploadFiles(Pdf("waiver.pdf"));
        Assert.Empty(cut.FindAll("[data-part=error]"));

        Button(cut, "Check the waiver").Click();

        Assert.Equal("The server couldn't read that waiver.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void StatesExample_ShouldKeepTheFileListedButNotRemovable_WhenTheFieldIsLocked()
    {
        var cut = Render<FileUploadStatesExample>();
        cut.FindComponent<InputFile>().UploadFiles(Pdf("signed.pdf"));
        Assert.False(cut.Find("[data-part=remove]").HasAttribute("disabled"));

        cut.Find("input[role=switch]").Change(true);

        Assert.Contains("signed.pdf", cut.Find("[data-part=file]").TextContent);
        Assert.True(cut.Find("[data-part=remove]").HasAttribute("disabled"));
    }

    [Fact]
    public void LabelsExample_ShouldUseItsOwnWords_WhenRendered()
    {
        var cut = Render<FileUploadLabelsExample>();

        Assert.Contains("Drop shelter photos here", cut.Find("[data-part=zone]").TextContent);
    }

    [Fact]
    public void ComponentPage_ShouldListTheKeysAndKeepFeaturesOutOfAccessibility_WhenShowingTheFileUpload()
    {
        var cut = ComponentPage("file-upload");
        var accessibility = cut.Find("ul[aria-label=Accessibility]").TextContent;

        Assert.Equal(["Enter", "Space"], cut.FindAll("kbd").Select(key => key.TextContent));
        Assert.DoesNotContain("ReadOnly", accessibility);
        Assert.DoesNotContain("Hidden", accessibility);
    }
}
