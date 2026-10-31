namespace Trailblazor.Tests.Field;

public class GroupValuesTests
{
    [Fact]
    public void TryFit_ShouldTakeTheValue_WhenItIsTheGroupsTypeOrItsNullable()
    {
        Assert.True(GroupValues.TryFit<int?>(2, out var size));
        Assert.True(GroupValues.TryFit<DayOfWeek?>(DayOfWeek.Friday, out var day));
        Assert.True(GroupValues.TryFit<int?>(null, out var none));

        Assert.Equal((2, DayOfWeek.Friday, null), (size, day, none));
    }

    [Theory]
    [InlineData(2L)]
    [InlineData(2.0)]
    [InlineData("2")]
    public void TryFit_ShouldRefuseTheValue_WhenItIsAnotherType(object value)
    {
        Assert.False(GroupValues.TryFit<int?>(value, out _));
    }

    [Fact]
    public void TryFit_ShouldRefuseNull_WhenTheGroupsTypeCantHoldIt()
    {
        Assert.False(GroupValues.TryFit<int>(null, out _));
    }
}
