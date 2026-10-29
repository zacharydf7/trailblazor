using Trailblazor.Docs.Examples;

namespace Trailblazor.BrowserTests;

public class DesignTokensTests
{
    [Fact]
    public void Read_ShouldListTheThemeTokensButNotTheComponentsOwnVariables_WhenGivenADialog()
    {
        var tokens = DesignTokens.Read(["tb-dialog"]);

        Assert.Contains("--tb-color-surface-raised", tokens);
        Assert.Contains("--tb-shadow-lg", tokens);
        Assert.DoesNotContain("--tb-dialog-width", tokens);
        Assert.DoesNotContain("--tb-dialog-left", tokens);
    }

    [Fact]
    public void Read_ShouldIncludeTheBlocksModifiers_WhenGivenItsName()
    {
        Assert.Contains("--tb-color-danger", DesignTokens.Read(["tb-button"]));
    }

    [Fact]
    public void Read_ShouldLeaveOutLongerBlocksThatStartTheSame_WhenGivenAShorterOne()
    {
        Assert.Contains("--tb-control-height", DesignTokens.Read(["tb-icon-button"]));
        Assert.Empty(DesignTokens.Read(["tb-icon"]));
    }
}
