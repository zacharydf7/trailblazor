using Microsoft.AspNetCore.Components;

namespace Trailblazor.Docs.Shared;

public static class SectionLink
{
    // The site sets <base href>, so a bare #id would point at the home page; the link keeps this page's path.
    public static string ToSection(this NavigationManager navigation, string id) =>
        $"{navigation.ToBaseRelativePath(navigation.Uri).Split('#')[0]}#{id}";
}
