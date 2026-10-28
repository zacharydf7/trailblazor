namespace Trailblazor.Spikes.Tests;

public class BrowsersAndModes : TheoryData<string, string>
{
    public static readonly string[] Browsers = ["chromium", "firefox", "webkit"];

    public static readonly string[] Modes = ["server", "wasm"];

    public BrowsersAndModes()
    {
        foreach (var browser in Browsers)
        {
            foreach (var mode in Modes)
            {
                Add(browser, mode);
            }
        }
    }
}
