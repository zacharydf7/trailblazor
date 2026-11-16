using Bunit;
using Trailblazor.Docs.Recipes;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs;

// Runs each bUnit cell of the Plain bUnit and Playwright guide, the plain way and with the helpers, and checks both
// leave TripPlanner the same way.
public class TestingComparisonTests : BunitContext
{
    public TestingComparisonTests() => this.AddTrailblazor();

    public static TheoryData<bool> BothWays => new(false, true);

    [Theory, MemberData(nameof(BothWays))]
    public async Task ClickAButton_ShouldAddAStop_WhenRunEitherWay(bool helpers)
    {
        var cut = Render<TripPlanner>();

        await (helpers ? BunitHelpers.ClickAButton(cut) : PlainBunit.ClickAButton(cut));

        Assert.Equal("1", cut.Find("output[aria-label=Stops]").TextContent);
    }

    [Theory, MemberData(nameof(BothWays))]
    public async Task TypeAndLeave_ShouldRenameTheTrip_WhenRunEitherWay(bool helpers)
    {
        var cut = Render<TripPlanner>();

        await (helpers ? BunitHelpers.TypeAndLeave(cut) : PlainBunit.TypeAndLeave(cut));

        Assert.StartsWith("Lake loop,", cut.Find("output[aria-label=Trip]").TextContent);
    }

    [Fact]
    public async Task ReadAnError_ShouldFindTheRequiredMessageAndInvalidMark_WhenRunEitherWay()
    {
        var plain = await PlainBunit.ReadAnError(Render<TripPlanner>());
        var helped = await BunitHelpers.ReadAnError(Render<TripPlanner>());

        Assert.True(plain.Invalid);
        Assert.Contains(helped.Error, plain.Descriptions);
        Assert.Equal((true, "Trip name is required."), helped);
    }

    [Theory, MemberData(nameof(BothWays))]
    public async Task ChooseAnOption_ShouldSetTheSeason_WhenRunEitherWay(bool helpers)
    {
        var cut = Render<TripPlanner>();

        await (helpers ? BunitHelpers.ChooseAnOption(cut) : PlainBunit.ChooseAnOption(cut));

        Assert.Contains("Summer", cut.Find("output[aria-label=Trip]").TextContent);
    }

    [Theory, MemberData(nameof(BothWays))]
    public async Task AnswerADialog_ShouldDeleteTheTrip_WhenRunEitherWay(bool helpers)
    {
        var root = Render<TbRoot>();
        var cut = Render<TripPlanner>();

        await (helpers ? BunitHelpers.AnswerADialog(cut, root) : PlainBunit.AnswerADialog(cut, root));

        Assert.Equal("yes", cut.Find("output[aria-label=Deleted]").TextContent);
    }

    [Theory, MemberData(nameof(BothWays))]
    public async Task SelectATreeRow_ShouldSelectRidgeLoop_WhenRunEitherWay(bool helpers)
    {
        var cut = Render<TripPlanner>();

        await (helpers ? BunitHelpers.SelectATreeRow(cut) : PlainBunit.SelectATreeRow(cut));

        Assert.Equal("true", cut.Find("[role=treeitem][data-text='Ridge loop']").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task SelectAGridRow_ShouldSelectLakePathAndReadEveryRow_WhenRunEitherWay()
    {
        var plainCut = Render<TripPlanner>();
        var helpedCut = Render<TripPlanner>();

        var plain = await PlainBunit.SelectAGridRow(plainCut);
        var helped = await BunitHelpers.SelectAGridRow(helpedCut);

        Assert.Equal(["Ridge loop", "Lake path", "Fern gully"], plain);
        Assert.Equal(plain, helped.Select(row => row[0]));
        Assert.Equal([["Lake path", "5"]], helpedCut.Tb().Grid("Hikes").Selected);
        Assert.Equal(helpedCut.Tb().Grid("Hikes").Selected, plainCut.Tb().Grid("Hikes").Selected);
    }

    [Theory, MemberData(nameof(BothWays))]
    public async Task ReadATabPanel_ShouldReadTheRoutePanel_WhenRunEitherWay(bool helpers)
    {
        var cut = Render<TripPlanner>();

        var text = await (helpers ? BunitHelpers.ReadATabPanel(cut) : PlainBunit.ReadATabPanel(cut));

        Assert.Equal("North along the ridge, back by the lake.", text);
    }

    [Theory, MemberData(nameof(BothWays))]
    public async Task UploadAFile_ShouldGiveTheTripItsMap_WhenRunEitherWay(bool helpers)
    {
        var cut = Render<TripPlanner>();

        if (helpers)
        {
            await BunitHelpers.UploadAFile(cut);
        }
        else
        {
            PlainBunit.UploadAFile(cut);
        }

        Assert.EndsWith("route.gpx", cut.Find("output[aria-label=Trip]").TextContent);
    }

    [Fact]
    public void ReadAControlWithNoHelper_ShouldReadTheProgress_WhenRun()
    {
        Assert.Equal("40", PlainBunit.ReadAControlWithNoHelper(Render<TripPlanner>()));
    }
}
