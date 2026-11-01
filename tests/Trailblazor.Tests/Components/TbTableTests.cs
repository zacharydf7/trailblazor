using Bunit;

namespace Trailblazor.Tests.Components;

public class TbTableTests : BunitContext
{
    public TbTableTests() => this.AddTrailblazor();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Render_ShouldMarkTheHeaderToStayInViewOnlyWhenAsked_WhenGivenStickyHeader(bool sticky)
    {
        var cut = Render<TbTable>(parameters => parameters.Add(table => table.StickyHeader, sticky));

        Assert.Equal(sticky, cut.Find("table").ClassList.Contains("tb-table--sticky-header"));
    }
}
