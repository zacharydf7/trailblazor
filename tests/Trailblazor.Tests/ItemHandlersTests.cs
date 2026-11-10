namespace Trailblazor.Tests;

public class ItemHandlersTests
{
    // Equal when the ids are, as an entity often is, though the other fields can differ.
    private sealed class Trail(int id, string name)
    {
        public int Id { get; } = id;

        public string Name { get; } = name;

        public override bool Equals(object? other) => other is Trail trail && trail.Id == Id;

        public override int GetHashCode() => Id;
    }

    private sealed record GroupKey(string Value);

    [Fact]
    public void For_ShouldGiveTheSameHandler_WhenAskedForTheSameItemAgain()
    {
        var pine = new Trail(1, "Pine");
        var handlers = new ItemHandlers<Trail, Func<string>>(trail => () => trail.Name);

        Assert.Same(handlers.For(pine), handlers.For(pine));
    }

    [Fact]
    public void For_ShouldGiveAHandlerForTheNewInstance_WhenAnEqualItemIsANewOne()
    {
        var handlers = new ItemHandlers<Trail, Func<string>>(trail => () => trail.Name);
        handlers.For(new Trail(1, "Pine"));

        Assert.Equal("Pine Hollow", handlers.For(new Trail(1, "Pine Hollow"))());
    }

    [Fact]
    public void For_ShouldGiveTheSameHandler_WhenAnEqualKeyIsANewInstanceMatchedByValue()
    {
        var handlers = new ItemHandlers<GroupKey, Func<string>>(key => () => key.Value, byValue: true);

        Assert.Same(handlers.For(new GroupKey("Pine")), handlers.For(new GroupKey("Pine")));
    }

    [Fact]
    public void For_ShouldGiveTheSameHandler_WhenAnEqualStringIsANewInstance()
    {
        var handlers = new ItemHandlers<string, Func<string>>(color => () => color);

        Assert.Same(handlers.For(new string("#336699")), handlers.For(new string("#336699")));
    }

    [Fact]
    public void KeepOnly_ShouldDropTheOldInstancesHandler_WhenOnlyAnEqualNewOneIsHeld()
    {
        var old = new Trail(1, "Pine");
        var handlers = new ItemHandlers<Trail, Func<string>>(trail => () => trail.Name);
        var dropped = handlers.For(old);

        handlers.KeepOnly([new Trail(1, "Pine Hollow")]);

        Assert.NotSame(dropped, handlers.For(old));
    }

    [Fact]
    public void KeepOnly_ShouldDropTheHandlersOfItemsNoLongerHeld_WhenCalled()
    {
        var pine = new Trail(1, "Pine");
        var cedar = new Trail(2, "Cedar");
        var handlers = new ItemHandlers<Trail, Func<string>>(trail => () => trail.Name);
        var kept = handlers.For(pine);
        var dropped = handlers.For(cedar);

        handlers.KeepOnly(trail => trail == pine);

        Assert.Same(kept, handlers.For(pine));
        Assert.NotSame(dropped, handlers.For(cedar));
    }
}
