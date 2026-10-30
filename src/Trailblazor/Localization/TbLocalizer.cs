using Microsoft.Extensions.Options;

namespace Trailblazor;

/// <summary>The default <see cref="ITbLocalizer"/>: the app's overrides from <see cref="TrailblazorOptions.Strings"/>,
/// and English for every other key.</summary>
public sealed class TbLocalizer(IOptions<TrailblazorOptions> options) : ITbLocalizer
{
    /// <inheritdoc />
    public string this[string key] =>
        options.Value.Strings.TryGetValue(key, out var text) ? text : TbStrings.English(key);
}
