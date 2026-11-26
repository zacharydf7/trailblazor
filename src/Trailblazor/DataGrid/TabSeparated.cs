using System.Text;

namespace Trailblazor;

// Text copied from a spreadsheet: rows end at line breaks and cells at tabs, and a cell in double quotes holds tabs,
// line breaks and doubled quotes of its own. The line break after the last row adds no row.
internal static class TabSeparated
{
    public static List<List<string>> Read(string text)
    {
        List<List<string>> rows = [];
        List<string> row = [];
        var cell = new StringBuilder();
        var quoted = false;
        for (var at = 0; at < text.Length; at++)
        {
            var next = text[at];
            if (quoted)
            {
                if (next == '"' && at + 1 < text.Length && text[at + 1] == '"')
                {
                    cell.Append('"');
                    at++;
                }
                else if (next == '"')
                {
                    quoted = false;
                }
                else
                {
                    cell.Append(next);
                }
            }
            else if (next == '"' && cell.Length == 0)
            {
                quoted = true;
            }
            else if (next == '\t')
            {
                row.Add(cell.ToString());
                cell.Clear();
            }
            else if (next is '\r' or '\n')
            {
                if (next == '\r' && at + 1 < text.Length && text[at + 1] == '\n')
                {
                    at++;
                }

                row.Add(cell.ToString());
                cell.Clear();
                rows.Add(row);
                row = [];
            }
            else
            {
                cell.Append(next);
            }
        }

        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            rows.Add(row);
        }

        return rows;
    }
}
