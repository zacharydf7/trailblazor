using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class CodeBlockTests : BunitContext
{
    public CodeBlockTests() => this.AddTrailblazor();

    private FakeBrowserService Browser => Services.GetRequiredService<FakeBrowserService>();

    [Theory]
    [InlineData(true, "Copied Project reference.")]
    [InlineData(false, "Couldn't copy Project reference.")]
    public void CodeBlock_ShouldCopyItsCodeWithUnixLineBreaksAndSaySo_WhenCopyIsClicked(bool canCopy, string message)
    {
        Browser.CanCopy = canCopy;
        var announcer = Render<AnnouncerHost>();
        var cut = Render<CodeBlock>(parameters => parameters.Add(block => block.Label, "Project reference")
            .Add(block => block.Code, "<ItemGroup>\r\n  <PackageReference Include=\"Trailblazor\" />\r\n</ItemGroup>"));

        cut.Find("button[aria-label='Copy Project reference']").Click();

        Assert.Equal(canCopy ? ["<ItemGroup>\n  <PackageReference Include=\"Trailblazor\" />\n</ItemGroup>"] : [],
            Browser.Copied);
        announcer.WaitForAssertion(() => Assert.Equal([message],
            announcer.FindAll("[role=status] p").Select(p => p.TextContent)));
    }

    [Fact]
    public void CodeBlock_ShouldWrapEachTokenInASpanNamedForItsKind_WhenItShowsRazor()
    {
        var cut = Render<CodeBlock>(parameters => parameters.Add(block => block.Label, "A first component")
            .Add(block => block.Code, "<TbButton OnClick=\"SaveAsync\">Save</TbButton>"));

        var code = cut.Find("pre[role=region][tabindex='0'][aria-label='A first component'] code");
        Assert.Equal("<TbButton OnClick=\"SaveAsync\">Save</TbButton>", code.TextContent);
        Assert.Equal([("docs-code__tag", "<TbButton"), ("docs-code__attribute", "OnClick"),
                ("docs-code__string", "\"SaveAsync\""), ("docs-code__tag", ">"), ("docs-code__tag", "</TbButton"),
                ("docs-code__tag", ">")],
            code.QuerySelectorAll("span").Select(span => (span.ClassName, span.TextContent)));
    }

    [Fact]
    public void CodeBlock_ShouldColorNothing_WhenItsLanguageIsPlain()
    {
        var cut = Render<CodeBlock>(parameters => parameters.Add(block => block.Label, "Page.razor")
            .Add(block => block.Code, "<TbButton />").Add(block => block.Language, CodeLanguage.Plain));

        Assert.Empty(cut.FindAll("code span"));
    }
}
