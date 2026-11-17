using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbKanban</c>, found by its <c>AriaLabel</c>, or the only one in the scope. Each column is a sortable
/// list named by its title.</summary>
public sealed class TbKanbanLocator(ILocator scope, string? name)
{
    /// <summary>The board's region.</summary>
    public ILocator Kanban => Scope.ByRole(scope, AriaRole.Region, name).And(scope.Locator("section.tb-kanban"));

    /// <summary>The columns' titles, left to right.</summary>
    public async Task<IReadOnlyList<string>> ColumnsAsync()
    {
        await Kanban.WaitForAsync();
        return await Kanban.Locator(":scope > [data-part=column] > [data-part=header] > [data-part=title]")
            .EvaluateAllAsync<string[]>("titles => titles.map(title => title.textContent.trim())");
    }

    /// <summary>The names of the cards in the column titled <paramref name="column"/>, top first.</summary>
    public Task<IReadOnlyList<string>> CardsAsync(string column) => Column(column).ItemsAsync();

    /// <summary>The column titled <paramref name="column"/>, as a sortable list: its cards move within it with
    /// <c>MoveAsync</c> or a drag, or to another column with <c>MoveToListAsync</c> or <c>DragToListAsync</c>.
    /// </summary>
    public TbSortableLocator Column(string column) => new(Kanban, column);

    /// <summary>Moves the card named <paramref name="card"/> to the end of the column titled <paramref name="column"/>,
    /// through its handle's menu as a user would. Throws when the board has no such card or column.</summary>
    public async Task MoveToColumnAsync(string card, string column)
    {
        var columns = await ColumnsAsync();
        if (!columns.Contains(column))
        {
            throw new InvalidOperationException($"This board has no column \"{column}\".");
        }

        foreach (var from in columns)
        {
            if ((await CardsAsync(from)).Contains(card))
            {
                await Column(from).MoveToListAsync(card, column);
                return;
            }
        }

        throw new InvalidOperationException($"This board has no card \"{card}\".");
    }
}
