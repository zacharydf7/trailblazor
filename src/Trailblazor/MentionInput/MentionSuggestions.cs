using Microsoft.AspNetCore.Components;

namespace Trailblazor;

// The suggestions for the mention being typed, which the mention input and the rich text editor both list. The script
// numbers each query, and the suggestions carry the number of the query they answer, so only the latest can be chosen.
internal sealed class MentionSuggestions : IDisposable
{
    private CancellationTokenSource? _search;

    public IReadOnlyList<MentionItem> Results { get; private set; } = [];

    public string Trigger { get; private set; } = "";

    public int ResultsFor { get; private set; }

    // Finds the suggestions for a query, or clears them for none. False when a newer query took over meanwhile. A
    // search's own failure clears them and is thrown on.
    public async Task<bool> FindAsync(MentionQuery? query, int number,
        Func<MentionQuery, CancellationToken, Task<IEnumerable<MentionItem>>>? search, IEnumerable<MentionItem>? items,
        int most)
    {
        Stop();
        if (query is null)
        {
            Clear(number);
            return true;
        }

        _search = new CancellationTokenSource();
        var token = _search.Token;
        try
        {
            var found = search is not null ? await search(query, token)
                : items?.Where(item => TextFold.Contains(item.Text, query.Text)) ?? [];
            token.ThrowIfCancellationRequested();
            (Results, Trigger, ResultsFor) = ([.. found.Take(most)], query.Trigger, number);
            return true;
        }
        // A search the user moved on from ends quietly, even when it reports the cancel its own way.
        catch (Exception) when (token.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception)
        {
            Clear(number);
            throw;
        }
    }

    // The item at an index of the suggestions that answer the query numbered, or null when newer ones replaced them.
    public MentionItem? At(int index, int resultsFor) =>
        resultsFor == ResultsFor && index >= 0 && index < Results.Count ? Results[index] : null;

    public void Clear(int number) => (Results, ResultsFor) = ([], number);

    public void Stop()
    {
        _search?.Cancel();
        _search?.Dispose();
        _search = null;
    }

    public void Dispose() => Stop();

    // The suggestions as a list box's options. An item's text is its accessible name and its description goes in
    // aria-describedby, so the name matches the text inserted. The rich text editor's script needs each item's id.
    public RenderFragment Options(string listboxId, RenderFragment<MentionItem>? template, bool withIds) => builder =>
    {
        for (var index = 0; index < Results.Count; index++)
        {
            var item = Results[index];
            var id = $"{listboxId}-{index}";
            var described = template is null && item.Description is not null;
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "id", id);
            builder.AddAttribute(2, "role", "option");
            builder.AddAttribute(3, "aria-selected", "false");
            builder.AddAttribute(4, "data-part", "option");
            builder.AddAttribute(5, "data-insert", Trigger + item.Text);
            builder.AddAttribute(6, "data-mention-id", withIds ? item.Id : null);
            builder.AddAttribute(7, "class", "tb-select__option");
            builder.AddAttribute(8, "aria-label", template is null ? item.Text : null);
            builder.AddAttribute(9, "aria-describedby", described ? $"{id}-description" : null);
            if (template is not null)
            {
                builder.AddContent(10, template(item));
            }
            else
            {
                builder.OpenElement(11, "span");
                builder.AddAttribute(12, "class", "tb-mention-input__text");
                builder.AddContent(13, item.Text);
                builder.CloseElement();
                if (item.Description is not null)
                {
                    builder.OpenElement(14, "span");
                    builder.AddAttribute(15, "id", $"{id}-description");
                    builder.AddAttribute(16, "data-part", "description");
                    builder.AddAttribute(17, "class", "tb-mention-input__description");
                    builder.AddContent(18, item.Description);
                    builder.CloseElement();
                }
            }

            builder.CloseElement();
        }
    };
}
