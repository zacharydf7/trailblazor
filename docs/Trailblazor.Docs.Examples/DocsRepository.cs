namespace Trailblazor.Docs.Examples;

// Links into the library's GitHub repository, for reading a page's source, editing it or reporting a problem.
public static class DocsRepository
{
    public const string Url = "https://github.com/zacharydf7/trailblazor";

    public static string Folder(string path) => $"{Url}/tree/main/{path}";

    public static string File(string path, int line) => $"{Url}/blob/main/{path}#L{line}";

    public static string Edit(string path) => $"{Url}/edit/main/{path}";

    public static string NewIssue(string title) => $"{Url}/issues/new?title={Uri.EscapeDataString(title)}";
}
