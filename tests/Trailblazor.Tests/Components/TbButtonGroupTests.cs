using Bunit;

namespace Trailblazor.Tests.Components;

public class TbButtonGroupTests : BunitContext
{
    [Fact]
    public void Render_ShouldGroupTheButtons_WhenCalled()
    {
        var cut = Render<TbButtonGroup>(parameters => parameters
            .AddUnmatched("aria-label", "Text alignment")
            .AddChildContent<TbButton>(button => button.AddChildContent("Left")));

        var group = cut.Find("[role=group]");
        Assert.Equal("Text alignment", group.GetAttribute("aria-label"));
        Assert.NotNull(group.QuerySelector("button"));
    }
}
