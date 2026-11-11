namespace Trailblazor.Tests;

public class ClassNamesTests
{
    [Fact]
    public void Join_ShouldSeparateTheNamesGivenAndSkipBlankOnes_WhenGivenSeveral()
    {
        Assert.Equal("tb-chip tb-chip--soft tb-chip--small",
            ClassNames.Join("tb-chip", null, "tb-chip--soft", " ", "", "tb-chip--small"));
    }

    [Fact]
    public void Join_ShouldReturnEmpty_WhenAllAreBlank()
    {
        Assert.Equal("", ClassNames.Join(null, " "));
    }

    [Fact]
    public void Join_ShouldHandBackTheNameItself_WhenOnlyOneIsNotBlank()
    {
        var name = string.Concat("tb-", "chip");

        Assert.Same(name, ClassNames.Join(null, name, ""));
    }

    [Fact]
    public void Join_ShouldAllocateOnlyTheString_WhenGivenSeveralNames()
    {
        ClassNames.Join("tb-select__option", null, "tb-select__option--active");
        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < 1000; i++)
        {
            ClassNames.Join("tb-select__option", null, "tb-select__option--active");
        }

        // The 43-character string takes 112 bytes; the old Join's array, iterator and builder took over 200 more.
        Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 1000 * 112 + 1999);
    }

    [Fact]
    public void Modifier_ShouldNameTheValueInLowercase_WhenGivenAnEnumValue()
    {
        Assert.Equal("tb-chip--danger", ClassNames.Modifier("tb-chip", Tone.Danger));
        Assert.Equal("tb-stack--align-center", ClassNames.Modifier("tb-stack", "align", Align.Center));
    }

    [Fact]
    public void Modifier_ShouldMakeNoString_WhenTheSameModifierIsAskedForAgain()
    {
        var first = ClassNames.Modifier("tb-icon", Size.Small);
        ClassNames.Modifier("tb-stack", "align", Align.End);
        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < 1000; i++)
        {
            ClassNames.Modifier("tb-icon", Size.Small);
            ClassNames.Modifier("tb-stack", "align", Align.End);
        }

        // Under a byte a call: the runtime may allocate once while tuning the loop, but no call makes a string.
        Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 1999);
        Assert.Same(first, ClassNames.Modifier("tb-icon", Size.Small));
    }
}
