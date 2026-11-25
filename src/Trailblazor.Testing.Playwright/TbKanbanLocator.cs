using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbKanban</c>, found by its <c>AriaLabel</c>, or the only one in the scope. Each column is a sortable
/// list named by its title, or with lanes each lane's cell of it is, named "{column}, {lane}".</summary>
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

    /// <summary>The names of the cards in the column titled <paramref name="column"/>, top first, or none while it's
    /// collapsed. On a board with lanes, this throws: name the lane.</summary>
    public async Task<IReadOnlyList<string>> CardsAsync(string column) =>
        (await LanesAsync()).Count > 0 ? throw LanesNamed() : await Column(column).ItemsAsync();

    private static InvalidOperationException LanesNamed() =>
        new("This board has lanes, so name the lane too, as CardsAsync(column, lane) or Cell(column, lane) do.");

    /// <summary>The lanes' titles, top to bottom, or none on a board without lanes.</summary>
    public async Task<IReadOnlyList<string>> LanesAsync()
    {
        await Kanban.WaitForAsync();
        return await Kanban.Locator(":scope > [data-part=lanes] > [data-part=lane]")
            .EvaluateAllAsync<string[]>("lanes => lanes.map(lane => lane.textContent.trim())");
    }

    /// <summary>The names of the cards in the lane titled <paramref name="lane"/> of the column titled
    /// <paramref name="column"/>, top first, or none while the column is collapsed.</summary>
    public Task<IReadOnlyList<string>> CardsAsync(string column, string lane) => Cell(column, lane).ItemsAsync();

    /// <summary>The lane titled <paramref name="lane"/>'s cell of the column titled <paramref name="column"/>, as a
    /// sortable list named "{column}, {lane}".</summary>
    public TbSortableLocator Cell(string column, string lane) => new(Kanban, $"{column}, {lane}");

    /// <summary>Moves the card named <paramref name="card"/> to the end of the lane titled <paramref name="lane"/> in
    /// the column titled <paramref name="column"/>, through its handle's menu as a user would. Throws when the board
    /// has no such card, or the card is in a collapsed column.</summary>
    public async Task MoveToColumnAsync(string card, string column, string lane)
    {
        if ((await LanesAsync()).Count == 0)
        {
            throw new InvalidOperationException("This board has no lanes, so name only the column.");
        }

        foreach (var from in await ColumnsAsync())
        {
            foreach (var fromLane in await LanesAsync())
            {
                if ((await CardsAsync(from, fromLane)).Contains(card))
                {
                    await Cell(from, fromLane).MoveToListAsync(card, $"{column}, {lane}");
                    return;
                }
            }
        }

        throw new InvalidOperationException($"This board has no card \"{card}\".");
    }

    /// <summary>The column titled <paramref name="column"/>, as a sortable list: its cards move within it with
    /// <c>MoveAsync</c> or a drag, or to another column with <c>MoveToListAsync</c> or <c>DragToListAsync</c>. On a
    /// board with lanes, use <see cref="Cell"/>.</summary>
    public TbSortableLocator Column(string column) => new(Kanban, column);

    /// <summary>Whether the column titled <paramref name="column"/> is collapsed.</summary>
    public async Task<bool> IsCollapsedAsync(string column) =>
        await ColumnElement(column).GetAttributeAsync("data-collapsed") is not null;

    /// <summary>Collapses the column titled <paramref name="column"/>, as its header's button does, and waits until
    /// the app's <c>CollapsedColumnsChanged</c> has run. Throws when the board isn't <c>Collapsible</c>, or the column
    /// is already collapsed.</summary>
    public Task CollapseAsync(string column) => ToggleAsync(column, collapse: true);

    /// <summary>Opens the collapsed column titled <paramref name="column"/>, as its header's button does, and waits
    /// until the app's <c>CollapsedColumnsChanged</c> has run. Throws when the board isn't <c>Collapsible</c>, or the
    /// column is already open.</summary>
    public Task ExpandAsync(string column) => ToggleAsync(column, collapse: false);

    private async Task ToggleAsync(string column, bool collapse)
    {
        if (await IsCollapsedAsync(column) == collapse)
        {
            throw new InvalidOperationException(
                $"The column \"{column}\" is already {(collapse ? "collapsed" : "open")}.");
        }

        var button = ColumnElement(column).Locator(":scope > [data-part=header] > [data-part=collapse]");
        if (await button.CountAsync() == 0)
        {
            throw new InvalidOperationException("This board isn't Collapsible, so its columns can't collapse.");
        }

        await Commit.ClickedAsync(button);
    }

    private ILocator ColumnElement(string column) => Kanban.Locator(":scope > [data-part=column]").Filter(new()
    {
        Has = Kanban.Page.Locator("[data-part=header] > [data-part=title]",
            new() { HasTextRegex = new Regex($"^{Regex.Escape(column)}$") }),
    });

    /// <summary>Moves the card named <paramref name="card"/> to the end of the column titled <paramref name="column"/>,
    /// through its handle's menu as a user would. Throws when the board has no such card or column, the card is in a
    /// collapsed column, or the column is collapsed.</summary>
    public async Task MoveToColumnAsync(string card, string column)
    {
        if ((await LanesAsync()).Count > 0)
        {
            throw LanesNamed();
        }

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
