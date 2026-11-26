namespace Trailblazor.Docs.Examples;

// The strictest Content Security Policy Trailblazor works under: the Content Security Policy guide shows it, and the
// browser tests' strict run sends it.
public static class ContentSecurityPolicy
{
    public static IReadOnlyList<string> Directives(string nonce) =>
    [
        "default-src 'self'",
        $"script-src 'self' 'nonce-{nonce}' 'wasm-unsafe-eval'",
        $"style-src 'self' 'nonce-{nonce}'",
        "style-src-attr 'unsafe-inline'",
        "img-src 'self' data: blob:",
        "connect-src 'self'",
        "object-src 'none'",
        "base-uri 'self'",
        "form-action 'self'",
    ];

    public static string For(string nonce) => string.Join("; ", Directives(nonce));
}
