using System.Text;
using System.Text.Json;

namespace Trailblazor;

// A layout as JSON: {"version":1,"root":…,"closed":[…]}, where a group is {"panels":[…],"active":…,"size":…} and a
// split is {"split":"row" or "column","size":…,"children":[…]}.
internal static class DockLayoutJson
{
    private const int Version = 1;

    public static string Write(DockLayout layout)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("version", Version);
            if (layout.Root is not null)
            {
                writer.WritePropertyName("root");
                Write(writer, layout.Root);
            }

            writer.WriteStartArray("closed");
            foreach (var panel in layout.Closed)
            {
                writer.WriteStringValue(panel);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    // Null for anything that isn't a layout this version wrote.
    public static DockLayout? Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("version", out var version)
                || !version.TryGetInt32(out var number) || number != Version)
            {
                return null;
            }

            var closed = root.TryGetProperty("closed", out var list) ? Strings(list) : [];
            return root.TryGetProperty("root", out var node) ? new DockLayout(Read(node), closed)
                : new DockLayout(null, closed);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException
            or KeyNotFoundException or ArgumentException or FormatException)
        {
            return null;
        }
    }

    private static void Write(Utf8JsonWriter writer, DockNode node)
    {
        writer.WriteStartObject();
        if (node is DockGroup group)
        {
            writer.WriteStartArray("panels");
            foreach (var panel in group.Panels)
            {
                writer.WriteStringValue(panel);
            }

            writer.WriteEndArray();
            writer.WriteString("active", group.Active);
        }
        else
        {
            var split = (DockSplit)node;
            writer.WriteString("split", split.Vertical ? "column" : "row");
            writer.WriteStartArray("children");
            foreach (var child in split.Children)
            {
                Write(writer, child);
            }

            writer.WriteEndArray();
        }

        writer.WriteNumber("size", node.Size);
        writer.WriteEndObject();
    }

    // A missing property throws KeyNotFoundException, a value of the wrong kind InvalidOperationException, and a bad
    // size or panel id ArgumentException.
    private static DockNode Read(JsonElement node)
    {
        var size = node.GetProperty("size").GetDouble();
        if (node.TryGetProperty("panels", out var panels))
        {
            return new DockGroup(Strings(panels)) { Active = node.GetProperty("active").GetString(), Size = size };
        }

        var direction = node.GetProperty("split").GetString();
        if (direction is not ("row" or "column"))
        {
            throw new FormatException();
        }

        return new DockSplit(node.GetProperty("children").EnumerateArray().Select(Read).ToList())
        {
            Vertical = direction == "column",
            Size = size,
        };
    }

    private static List<string> Strings(JsonElement list) =>
        [.. list.EnumerateArray().Select(item => item.GetString() ?? throw new FormatException())];
}
