using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class FileUploadScenarioTests : BunitContext
{
    public FileUploadScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task RangerReport_ShouldAskForAnotherName_WhenTheFileNameHasSpaces()
    {
        var cut = Render<FileUploadValidationExample>();
        var report = cut.Tb().FileUpload("Ranger report");

        // ChooseAsync takes files made in the test, as dropping or picking them does.
        await report.ChooseAsync(InputFileContent.CreateFromText("Bridge out at Pine Hollow", "ranger report.txt"));
        Assert.Equal("Rename the file so its name has no spaces.", report.Error);

        await report.ChooseAsync(InputFileContent.CreateFromText("Bridge out at Pine Hollow", "ranger-report.txt"));
        Assert.Equal(["ranger-report.txt"], report.Files);
        Assert.Null(report.Error);
    }
}
