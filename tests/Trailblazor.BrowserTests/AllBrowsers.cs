namespace Trailblazor.BrowserTests;

public class AllBrowsers : TheoryData<string>
{
    internal static readonly string[] Supported = ["chromium", "firefox", "webkit"];

    public static readonly string[] Names = Choose(Environment.GetEnvironmentVariable("TRAILBLAZOR_BROWSERS"));

    public AllBrowsers() => AddRange(Names);

    private static string[] Choose(string? list)
    {
        if (string.IsNullOrWhiteSpace(list))
        {
            return Supported;
        }

        var names = list.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var unknown = names.Except(Supported).ToArray();
        return unknown.Length == 0 ? names : throw new InvalidOperationException(
            $"TRAILBLAZOR_BROWSERS names {string.Join(", ", unknown)}; the browsers are {string.Join(", ", Supported)}.");
    }
}
