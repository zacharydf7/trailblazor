using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Trailblazor.Docs.Examples;

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
