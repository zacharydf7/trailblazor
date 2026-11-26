namespace Trailblazor.BrowserTests.Host;

// Where the strict run's middleware leaves the response's nonce for the page's head.
public static class StrictPolicy
{
    public const string NonceKey = "csp-nonce";
}
