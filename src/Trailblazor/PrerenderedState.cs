using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Trailblazor;

// What a component keeps while the page prerenders goes to JSON, and comes back from it in the interactive page.
internal static class PrerenderedState
{
    // Whether the value reads the same after that trip. System.Text.Json writes a property it can't set again, such
    // as one with a private setter, and skips it on the way back without an error, so what came back would be
    // missing it. When the value can't make the trip at all, `failure` says why.
    [RequiresUnreferencedCode("Writes the value's type as JSON and reads it back.")]
    [RequiresDynamicCode("Writes the value's type as JSON and reads it back.")]
    public static bool ComesBack<T>(T value, out Exception? failure)
    {
        failure = null;
        try
        {
            var kept = JsonSerializer.Serialize(value, JsonSerializerOptions.Web);
            var back = JsonSerializer.Deserialize<T>(kept, JsonSerializerOptions.Web);
            return JsonSerializer.Serialize(back, JsonSerializerOptions.Web) == kept;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            failure = exception;
            return false;
        }
    }

    // The end of the warning a component logs for a value that didn't come back, after it has named the type.
    public static string Why(Exception? failure) => failure is null
        ? "don't come back from JSON as they went in. System.Text.Json doesn't set a property with a private "
            + "setter, or one with no setter that no constructor takes; a property computed from the others is "
            + "fine. Use a type with public setters, or with a constructor that takes each property."
        : "can't be written to JSON and read back, for the reason the exception gives.";
}
