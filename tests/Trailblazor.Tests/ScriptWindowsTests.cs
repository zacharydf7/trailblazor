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
        // Blazor's reconnect UI, which says that the connection is lost, is on the page only.
        "dock-manager.js: const ui = document.getElementById('components-reconnect-modal');",
        "dock-manager.js: frameworkUi.observe(document.body, { childList: true });",
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

    // Events only the page's own window has: Blazor's reconnect notice, the page being left or shown again, and
    // find-in-page revealing a hidden panel, which only a static page has and a popped-out panel is always interactive.
    private static readonly HashSet<string> PageEvents =
        ["components-reconnect-state-changed", "pagehide", "pageshow", "beforematch"];

    // By event type only: a type pop-out.js sends on only as it captures needs a capturing listener on the page, and
    // one it sends on only to the page's window needs a listener there.
    [Fact]
    public void Scripts_ShouldOnlyListenOnThePageForEventsAnotherWindowSendsOn_WhenCalled()
    {
        var popOut = File.ReadAllText(Path.Combine(Repository.Root, "src", "Trailblazor", "wwwroot", "pop-out.js"));
        var sentOn = SentOnLists().Matches(popOut).SelectMany(list => Quoted().Matches(list.Groups["list"].Value))
            .Select(type => type.Groups["type"].Value).ToHashSet();
        var scripts = new[] { "Trailblazor", "Trailblazor.Primitives" }
            .SelectMany(project => Directory.GetFiles(Path.Combine(Repository.Root, "src", project, "wwwroot"), "*.js"))
            .Select(file => (Name: Path.GetFileName(file), Text: File.ReadAllText(file))).ToList();
        // A listener whose type is a loop's variable takes each type the loop lists.
        var heard = scripts.SelectMany(script => OnThePage().Matches(script.Text)
                .Select(listener => listener.Groups["type"].Value)
                .Concat(LoopOnThePage().Matches(script.Text).SelectMany(loop => Quoted()
                    .Matches(loop.Groups["list"].Value).Select(type => type.Groups["type"].Value)))
                .Select(type => (script.Name, Type: type)))
            .ToList();

        Assert.Empty(heard.Where(listener => !sentOn.Contains(listener.Type) && !PageEvents.Contains(listener.Type))
            .Select(listener => $"{listener.Name}: {listener.Type}"));
        // Any other listener on the page whose type isn't written out can't be checked.
        Assert.Equal(scripts.Sum(script => LoopOnThePage().Count(script.Text)),
            scripts.Sum(script => ByVariableOnThePage().Count(script.Text)));
        // A page event that a script no longer hears, or that pop-out.js now sends on, has to come off the list.
        Assert.DoesNotContain(PageEvents,
            type => sentOn.Contains(type) || heard.All(listener => listener.Type != type));
    }

    // The lists in pop-out.js of the events another window sends on to the page: those that bubble, those heard on
    // their way down, and those of the window itself.
    [GeneratedRegex(@"(?:const bubbling = new Set\(|const captured = |const ofTheWindow = )\[(?<list>[^\]]*)\]")]
    private static partial Regex SentOnLists();

    [GeneratedRegex("'(?<type>[^']+)'")]
    private static partial Regex Quoted();

    // A listener on the page's own document or window, named or not, which hears another window's events only when
    // they're sent on.
    [GeneratedRegex(@"(?<![\w.])(?:(?:document|window)\.)?addEventListener\(\s*'(?<type>[^']+)'")]
    private static partial Regex OnThePage();

    // A listener on the page whose type is a variable.
    [GeneratedRegex(@"(?<![\w.])(?:(?:document|window)\.)?addEventListener\(\s*[A-Za-z_]")]
    private static partial Regex ByVariableOnThePage();

    // A loop over a list of types that puts a listener for each on the page.
    [GeneratedRegex(@"for \(const (?<name>\w+) of \[(?<list>[^\]]*)\]\) \{\s*"
        + @"(?:(?:document|window)\.)?addEventListener\(\k<name>,")]
    private static partial Regex LoopOnThePage();

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
            |new\ ResizeObserver\(
        )
        """, RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex OfThePage();
}
