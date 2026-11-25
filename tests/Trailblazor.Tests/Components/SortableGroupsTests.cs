namespace Trailblazor.Tests.Components;

public class SortableGroupsTests
{
    private sealed class List(string? listId) : ISortableGroupMember
    {
        public string? ListId { get; } = listId;

        public void GroupChanged()
        {
        }
    }

    [Fact]
    public void Join_ShouldPutAListBackInItsPlace_WhenItLeftAndJoinsAgain()
    {
        var groups = new SortableGroups();
        List todo = new("todo"), doing = new("doing"), done = new("done");
        foreach (var list in new[] { todo, doing, done })
        {
            groups.Join("board", list);
        }

        groups.Leave("board", doing);
        groups.Join("board", new List("doing"));

        Assert.Equal(["todo", "doing", "done"],
            groups.Lists<ISortableGroupMember>("board").Select(list => list.ListId));
    }
}
