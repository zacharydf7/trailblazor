using System.Globalization;
using System.Text;

namespace Trailblazor;

// An SVG path for the dark modules, one rectangle per run along each row, offset by the quiet zone.
internal static class BarPath
{
    public static string Rows(bool[,] modules, int quietColumns, int? quietRows = null)
    {
        var path = new StringBuilder();
        var top = quietRows ?? quietColumns;
        for (var y = 0; y < modules.GetLength(0); y++)
        {
            for (var x = 0; x < modules.GetLength(1); x++)
            {
                if (!modules[y, x])
                {
                    continue;
                }

                var run = 1;
                while (x + run < modules.GetLength(1) && modules[y, x + run])
                {
                    run++;
                }

                path.Append(CultureInfo.InvariantCulture, $"M{x + quietColumns} {y + top}h{run}v1h-{run}z");
                x += run - 1;
            }
        }

        return path.ToString();
    }
}
