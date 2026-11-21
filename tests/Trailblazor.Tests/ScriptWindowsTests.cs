using System.Text.RegularExpressions;
using Trailblazor.Tests.Docs;

namespace Trailblazor.Tests;

// A part of the page can show in a browser window of its own, where the page's `document` and `window` are the wrong
// ones to ask. A script asks through the element it works on, with the helpers in pages.js.
public partial class ScriptWindowsTests
{
    // Scripts whose subject is the page itself, each with why.
    private static readonly Dictionary<string, string> PageScripts = new()
    {
        ["pages.js"] = "It is what finds an element's window.",
        ["pop-out.js"] = "It copies the page's styles to the other window.",
        ["page-scroll.js"] = "It measures the page's own scrollbar.",
        ["theme.js"] = "It sets the theme on the page's root.",
        ["theme-head.js"] = "It sets the theme on the page's root.",
        ["browser.js"] = "It reports the page's own window.",
        ["static-form.js"] = "It serves static pages, which show in no other window.",
        ["speech-to-text.js"] = "It reads the page's language.",
        ["dialog.js"] = "Dialogs show in the page's window.",
    };

    // Single lines that are about the page itself, in scripts that otherwise follow their element.
    private static readonly HashSet<string> PageLines =
    [
        // The first scan of a static page, before any other window exists.
        "accordion.js: for (const trigger of document.querySelectorAll(closedTriggers)) {",
        "accordion.js: if (document.getElementById(trigger.getAttribute('aria-controls'))?.hidden === false) {",
        "tabs.js: for (const tab of document.querySelectorAll("
            + "'.tb-tabs__list > [role=\"tab\"]:not([data-ready])[aria-selected=\"false\"]')) {",
        "tabs.js: if (document.getElementById(tab.getAttribute('aria-controls'))?.hidden === false) {",
        "tabs.js: for (const tab of document.querySelectorAll(shown)) {",
        "tabs.js: const panel = document.getElementById(tab.getAttribute('aria-controls'));",
        // The scan after an enhanced navigation, which only the page has.
        "autofocus.js: focusFirst([...document.querySelectorAll('[data-tb-autofocus]')]);",
        "number-field.js: document.querySelectorAll('[data-part=\"control\"]:has(> [data-part=\"increment\"])')"
            + ".forEach(control => {",
        // The marks on the page's root.
        "Trailblazor.lib.module.js: document.documentElement.dataset.tbScript = '';",
        "Trailblazor.lib.module.js: document.documentElement.dataset.tbNavigated = '';",
        "Trailblazor.lib.module.js: document.querySelectorAll('[data-tb-interactive][data-tb-session]')",
        "Trailblazor.lib.module.js: document.documentElement.dataset.tbApple = '';",
        "Trailblazor.lib.module.js: Object.assign(document.documentElement.dataset, globalThis.trailblazorTheme);",
        // A tour's step names its target with a selector, which is looked up on the page and watched from there.
        "tour.js: return document.querySelector(selector);",
        "tour.js: watch = new ResizeObserver(highlight);",
        // Another window's events are sent on to the page's root, so a listener there hears every window's.
        "debounced-input.js: document.documentElement.addEventListener('input', event => {",
    ];

    [Fact]
    public void Scripts_ShouldAskTheirElementForItsWindow_WhenCalled()
    {
        var asked = new[] { "Trailblazor", "Trailblazor.Primitives" }
            .SelectMany(project => Directory.GetFiles(Path.Combine(Repository.Root, "src", project, "wwwroot"), "*.js"))
            .Where(file => !PageScripts.ContainsKey(Path.GetFileName(file)))
            .SelectMany(file => File.ReadLines(file).Select(line => line.Trim())
                .Where(line => !line.StartsWith("//") && OfThePage().IsMatch(line))
                .Select(line => $"{Path.GetFileName(file)}: {line}"))
            .ToList();

        Assert.Empty(asked.Except(PageLines));
        // A line that no longer asks the page has to come off the list.
        Assert.Empty(PageLines.Except(asked));
    }

    // What only the page's own window answers for: its focused element, what it holds, its size, its frames, its
    // selection, the windows it opens and its own kinds of element, which one drawn in another window may not be. The
    // name must stand by itself, not be the member of something else, as in `list.ownerDocument.body`.
    [GeneratedRegex("""
        (?<![\w']|[\w)\]]\.)
        (
            document\.(activeElement|getElementById|querySelector(All)?|body|documentElement|scrollingElement)\b
            |document\.(elementFromPoint|getSelection|execCommand|createRange)\b
            |(window\.)?(requestAnimationFrame|cancelAnimationFrame|getSelection)\(
            |window\.open\(
            |instanceof\ (Node|Element|HTML\w+|SVG\w+)\b
            |(window\.)?(innerWidth|innerHeight|scrollX|scrollY|visualViewport)\b
            |new\ (ResizeObserver|IntersectionObserver)\(
        )
        """, RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex OfThePage();
}
