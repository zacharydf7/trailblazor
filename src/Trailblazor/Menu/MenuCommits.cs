namespace Trailblazor;

// Counts the item handlers that have finished, which the menu shows as data-commits for tests to wait on. The
// menu closes in the browser as an item is clicked, before the app's handler has run. TbMenu and TbContextMenu
// cascade one to their items.
internal sealed class MenuCommits(Action changed)
{
    public int Count { get; private set; }

    public async Task RunAsync(Func<Task> handler)
    {
        try
        {
            await handler();
        }
        finally
        {
            Count++;
            changed();
        }
    }
}
