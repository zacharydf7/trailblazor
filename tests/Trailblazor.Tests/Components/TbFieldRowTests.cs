using Bunit;

namespace Trailblazor.Tests.Components;

public class TbFieldRowTests : BunitContext
{
    [Fact]
    public void Render_ShouldSetTheDefaultGapAndNoColumns_WhenNoParametersAreSet()
    {
        var cut = Render<TbFieldRow>(parameters => parameters.AddChildContent("Item"));

        var row = cut.Find("div");
        Assert.Equal(("tb-field-row", "--_tb-field-row-gap: var(--tb-space-3)"),
            (row.GetAttribute("class"), row.GetAttribute("style")));
    }

    [Fact]
    public void Render_ShouldSetTheGapColumnsAndStyle_WhenTheyAreGiven()
    {
        var cut = Render<TbFieldRow>(parameters => parameters.Add(row => row.Gap, 2)
            .Add(row => row.Columns, "1fr auto").Add(row => row.Style, "margin: 0").Add(row => row.Class, "lookup"));

        var row = cut.Find("div");
        Assert.Equal(("tb-field-row lookup",
            "--_tb-field-row-gap: var(--tb-space-2); --_tb-field-row-columns: 1fr auto; margin: 0"),
            (row.GetAttribute("class"), row.GetAttribute("style")));
    }
}
