namespace Trailblazor.BrowserTests.Host;

// The server and WebAssembly both set Trailblazor up here, so a page behaves the same in either mode.
public static class TestAppOptions
{
    public static void Configure(TrailblazorOptions options) => options.Dialogs.OutsidePopups.Add(".reused-popup");
}
