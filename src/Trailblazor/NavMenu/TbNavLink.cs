using Microsoft.AspNetCore.Components.Routing;

namespace Trailblazor;

/// <summary>A link in a <see cref="TbNavMenu"/>, marked as the current page when the address matches it.</summary>
public partial class TbNavLink
{
    // As Blazor's NavLink: the query, the fragment and a trailing slash don't count, and a prefix only matches whole
    // segments, so "trails" isn't current on "trailheads".
    internal static bool IsCurrent(string currentUri, string href, NavLinkMatch match)
    {
        var current = PathOf(currentUri);
        var target = PathOf(href);
        if (string.Equals(current, target, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return match == NavLinkMatch.Prefix && current.StartsWith(target, StringComparison.OrdinalIgnoreCase)
            && current[target.Length] == '/';
    }

    private static string PathOf(string uri) => uri.Split('?', '#')[0].TrimEnd('/');
}
