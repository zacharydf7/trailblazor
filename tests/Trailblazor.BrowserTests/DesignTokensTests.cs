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
        Assert.DoesNotContain("--tb-control-height", DesignTokens.Read(["tb-icon"]));
        Assert.Contains("--tb-color-danger-text", DesignTokens.Read(["tb-icon"]));
    }

    [Fact]
    public void Hooks_ShouldListAComponentsOwnTokensWithTheirDefaults_WhenGivenItsBlocks()
    {
        var button = DesignTokens.Hooks(["tb-button"]);

        Assert.Contains(("--tb-button-radius", "var(--tb-radius-md)"), button);
        Assert.Contains(("--tb-button-height", "var(--tb-control-height)"), button);
        Assert.Contains(("--tb-dialog-z-index", "1100"), DesignTokens.Hooks(["tb-dialog"]));
    }

    [Fact]
    public void Hooks_ShouldListOnlyTokensAnAppCanSet_WhenAComponentAlsoReadsVariablesItsCodeSets()
    {
        string[] Names(params string[] blocks) => [.. DesignTokens.Hooks(blocks).Select(hook => hook.Name)];

        Assert.Contains("--tb-table-row-height", Names("tb-table"));
        Assert.Equal(["--tb-data-grid-row-height"], Names("tb-data-grid"));
        Assert.Equal(["--tb-tabs-indicator-color", "--tb-tabs-indicator-size"], Names("tb-tabs"));
        Assert.DoesNotContain("--tb-dialog-tray-block", Names("tb-dialog"));
        Assert.DoesNotContain("--tb-color-picker-value", Names("tb-field", "tb-color-picker"));
        Assert.DoesNotContain("--tb-available-block-size", Names("tb-menu"));
    }

    [Fact]
    public void ComponentTokens_ShouldEachBeReadByAComponentOnTheSite_WhenTheCatalogIsRead()
    {
        var read = ExampleCatalog.All.SelectMany(example => DesignTokens.Hooks(example.Blocks))
            .Select(hook => hook.Name).ToHashSet();

        Assert.All(DesignTokens.ComponentTokens, token => Assert.Contains(token, read));
    }

    [Fact]
    public void Role_ShouldDescribeEveryThemeToken_WhenTheReferenceIsRead()
    {
        Assert.All(DesignTokens.Reference(), token => Assert.NotEqual("", DesignTokens.Role(token.Name)));
    }

    [Fact]
    public void Reference_ShouldListThemeTokensWithTheirDefaultsButNoRampSteps_WhenCalled()
    {
        var reference = DesignTokens.Reference();

        Assert.Contains(("--tb-control-height", "2.5rem"), reference);
        Assert.Contains(reference, token => token.Name == "--tb-color-primary-hover");
        Assert.DoesNotContain(reference, token => token.Name == "--tb-blue-600");
        Assert.Equal(reference.Count, reference.Select(token => token.Name).Distinct().Count());
    }
}
