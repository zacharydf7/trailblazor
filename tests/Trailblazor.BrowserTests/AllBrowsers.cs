namespace Trailblazor.BrowserTests;

public class AllBrowsers : TheoryData<string>
{
    public static readonly string[] Names = ["chromium", "firefox", "webkit"];

    public AllBrowsers() => AddRange(Names);
}
