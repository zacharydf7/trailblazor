namespace Trailblazor.Docs.Examples;

// In the order the docs site lists them.
public enum ExampleCategory
{
    Layout,
    Content,
    Actions,
    Inputs,
    Overlays,
    Navigation,
    Feedback,
}

public sealed record DataPart(string Name, string Description);

// How much of a component works before the page is interactive, from all of it to nothing.
public enum ModeSupport
{
    Full,
    Partial,
    DisplayOnly,
    None,
}

// A static page and a prerendered one render alike, but only a static page's forms post, and what someone types or
// chooses on a prerendered page is lost when it turns interactive. Everything works once it has.
public sealed record RenderModes(ModeSupport Static, ModeSupport BeforeInteractive)
{
    public static string Label(ModeSupport support) => support switch
    {
        ModeSupport.Full => "Works",
        ModeSupport.Partial => "Partly",
        ModeSupport.DisplayOnly => "Display only",
        _ => "Not shown",
    };
}

public sealed record Example(string Name, string Title, ExampleCategory Category, string Summary)
{
    // The types whose parameters or options the example's page tables.
    public required IReadOnlyList<Type> Api { get; init; }

    // What the component does for people using assistive technology, such as its roles and states; text in backticks
    // shows as code, and [text](path) links to another page.
    public required IReadOnlyList<string> Accessibility { get; init; }

    // When the component fits, and when another does instead; both can link to the other component.
    public required IReadOnlyList<string> WhenToUse { get; init; }

    public IReadOnlyList<string> WhenNotToUse { get; init; } = [];

    // The catalog names of related components, linked at the end of the page.
    public required IReadOnlyList<string> Related { get; init; }

    // The WAI-ARIA Authoring Practices pattern the component follows.
    public AriaPattern? Pattern { get; init; }

    // Every key the component handles, in the order a table shows them.
    public IReadOnlyList<KeyboardRow> Keyboard { get; init; } = [];

    // Keys that swap in right-to-left layouts, said in a line in the accessibility section.
    public string? RightToLeftKeys { get; init; }

    // How the component gets its accessible name, and what each of its parts is named by.
    public IReadOnlyList<string> Labeling { get; init; } = [];

    // What a screen reader says as focus arrives, as the value changes, on an error and for live updates.
    public IReadOnlyList<string> Announcements { get; init; } = [];

    // What the app must still do, apart from what the component does.
    public IReadOnlyList<string> AppDuties { get; init; } = [];

    // What renders before the page is interactive, what works then, and what a static form posts.
    public required IReadOnlyList<string> BeforeInteractive { get; init; }

    // How much of the component works on a static page, and on a prerendered page before it turns interactive.
    public required RenderModes Modes { get; init; }

    // Behavior, limitations and known issues that belong to no other section; a pitfall shows as a warning.
    public IReadOnlyList<ExampleNote> Notes { get; init; } = [];

    // How a test finds the component in the example; null for a layout component with no role of its own.
    public required TestTarget? Target { get; init; }

    // Where a component sets data-ready once it's ready to use, after its script attaches or the page turns
    // interactive; null for one that sets none.
    public ReadyMark? Ready { get; init; }

    // The CSS blocks the component is styled under, such as tb-button, which its design tokens are read from.
    public required IReadOnlyList<string> Blocks { get; init; }

    // Extra testing advice for the page; text in backticks shows as code.
    public string? TestingTip { get; init; }

    // The same, as a list, for a component with many things to say.
    public IReadOnlyList<string> TestingTips { get; init; } = [];

    // What the component reports through the Trailblazor meter; text in backticks shows as code.
    public IReadOnlyList<string> Telemetry { get; init; } = [];

    // Every field takes a TelemetryName, so a field's page says what that counts before its own notes.
    public IReadOnlyList<string> TelemetryNotes => Api.Any(IsField) ? [FieldTelemetry, .. Telemetry] : Telemetry;

    // Every field takes AriaLabel, so a field's page ends its labeling with it.
    public IReadOnlyList<string> LabelingNotes => Api.Any(IsField) ? [.. Labeling, FieldLabeling] : Labeling;

    // Every field takes OnFocus, OnBlur and AutoFocus, and warns about a Value without ValueChanged, so a field's page
    // ends its notes with them.
    public IReadOnlyList<ExampleNote> NotesShown => Api.Any(IsField) ? [.. Notes, new(FieldNote)] : Notes;

    private const string FieldLabeling = "With no room for a label, an `AriaLabel` names the field through a label "
        + "only screen readers see. A field with neither gets build warning TB004.";

    private const string FieldNote = "`OnFocus` and `OnBlur` report focus moving into the field and out of it, not "
        + "between its own parts, so they suit checking or saving when the user moves on. `AutoFocus` moves focus to "
        + "it as it appears on an interactive page, unless the user is typing in another field by then, or, in a "
        + "dialog or focus scope, as that opens. A `Value` set without `ValueChanged` gets build warning TB003. With "
        + "`Value` and a `ValueChanged` lambda in place of `@bind-Value`, Razor can't work out a generic field's value "
        + "type, so name its type parameter yourself, such as `TValue=\"string\"` on a text field or "
        + "`TDate=\"DateOnly\"` on a date picker. The [forms and validation guide](guides/forms) covers "
        + "messages, server errors and when validation runs.";

    private const string FieldTelemetry = "With a `TelemetryName`, such as `\"trail-name\"`, it counts "
        + "`trailblazor.control.shown` as it first renders, and `trailblazor.field.invalid` each time its `TbForm` is "
        + "submitted while it has an error. Its value is never recorded.";

    private static bool IsField(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(TbFieldBase<>))
            {
                return true;
            }
        }

        return false;
    }

    // The data-part names a test or a stylesheet can find inside the component, in the order they appear.
    public IReadOnlyList<DataPart> DataParts { get; init; } = [];

    // A short skeleton of a component made of parts, showing which goes inside which.
    public string? Anatomy { get; init; }

    // A demo that needs the frame's whole width, such as a grid; others sit centered in it.
    public bool FullWidth { get; init; }

    // One section per feature.
    public required IReadOnlyList<ExampleSection> Sections { get; init; }

    public IReadOnlyList<ExampleSource> Sources => [.. Sections.SelectMany(section => section.Sources)];
}

// A pattern from the WAI-ARIA Authoring Practices Guide, by its name there and its page.
public sealed record AriaPattern(string Name, string Url);

// A keyboard table row: keys that share an action, such as ["Home", "End"]; a combination is written as "Alt+Down".
// When, such as "While open", names the state or mode the keys work in, and rows that share it are grouped under it.
public sealed record KeyboardRow(IReadOnlyList<string> Keys, string Action)
{
    public string? When { get; init; }
}

// A note on behavior or a limitation; a pitfall, which costs the reader if missed, shows as a warning.
public sealed record ExampleNote(string Text)
{
    public bool Pitfall { get; init; }
}

// One feature of a component: a heading, a sentence or two, the live demo and its code. The id names its anchor.
public sealed record ExampleSection(string Id, string Title, string Description, Type Component, params Type[] Parts)
{
    // Lets a reader change the demo's parameters on the page. Component must then inherit ControlledExample.
    public ExampleControls? Controls { get; init; }

    public IReadOnlyList<ExampleSource> Sources => [.. new[] { Component }.Concat(Parts).Select(ExampleSource.Of)];
}

// An ARIA role, as Playwright's AriaRole names it apart from case, and the accessible name of the component in the
// example; or its text, for a role that takes no name from its content.
public sealed record TestTarget(string Role, string? Name = null)
{
    public string? Text { get; init; }

    // A component that shows only once something is clicked, or hovered, is found after that.
    public TestTarget? OpenedBy { get; init; }

    public bool OpenOnHover { get; init; }

    public string Locator => Text is null
        ? $"page.GetByRole(AriaRole.{RoleName}, new() {{ Name = \"{Name}\" }})"
        : $"page.GetByRole(AriaRole.{RoleName}).Filter(new() {{ HasText = \"{Text}\" }})";

    public string Steps => OpenedBy is null
        ? Locator
        : $"await {OpenedBy.Locator}.{(OpenOnHover ? "Hover" : "Click")}Async();\n{Locator}";

    private string RoleName => $"{char.ToUpperInvariant(Role[0])}{Role[1..]}";
}

// The element a component marks with data-ready, as a CSS selector and as the testing guide names it. An element
// marked only while it's shown is checked after OpenedBy is clicked.
public sealed record ReadyMark(string Selector, string Element)
{
    public TestTarget? OpenedBy { get; init; }
}

public sealed record ExampleSource(string File, string Text)
{
    public static ExampleSource Of(Type component) => Read("Source", $"{component.Name}.razor");

    // A recipe's file that the site shows without running.
    public static ExampleSource OfRecipeFile(string file) => Read("Recipes", file);

    private static ExampleSource Read(string folder, string file)
    {
        using var stream = typeof(ExampleSource).Assembly.GetManifestResourceStream($"{folder}/{file}")
            ?? throw new InvalidOperationException($"{file} isn't embedded as source.");
        using var reader = new StreamReader(stream);
        return new(file, reader.ReadToEnd());
    }
}

// Every docs example. The docs site shows them, and the browser tests run axe against each one.
public static class ExampleCatalog
{
    // What a component with no script does before the page is interactive.
    public const string PlainHtml = "It is plain HTML, so it all renders the same before the page is interactive.";

    // What a button reports, which a link button and an icon button share.
    private static string[] ButtonTelemetry(string component) =>
    [
        "With a `TelemetryName`, such as `\"export-csv\"`, it counts `trailblazor.control.shown` once as it first "
            + "renders interactively, and `trailblazor.control.used` for each click it acts on, as a button or as a "
            + $"link. Both are tagged with the name and `trailblazor.component` `{component}`.",
        "A disabled or loading button counts as shown but not used. Without a `TelemetryName` it counts nothing.",
    ];

    // What a checkbox reports, which a switch shares.
    private static readonly string[] CheckboxTelemetry =
    [
        "It also counts `trailblazor.control.used` each time the user changes it. Only that it changed is counted, "
            + "never to what.",
    ];

    // The parts every labeled field has; start and end hold its adornments.
    private static readonly DataPart[] FieldParts =
    [
        new("label", "The field's visible label."),
        new("control", "The box around the input and its start and end content."),
        new("start", "Content before the input, such as an icon or a prefix."),
        new("end", "Content after the input, such as a unit or a button."),
        new("helper", "The help text under the field."),
        new("error", "The validation message, shown while the value is invalid."),
    ];

    public static IReadOnlyList<Example> All { get; } =
    [
        new("text", "Text", ExampleCategory.Content,
            "Headings, body copy and labels from the type scale, rendered as the HTML element you choose.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for headings, body copy and labels, so text follows the type scale and theme.",
            ],
            WhenNotToUse =
            [
                "For a label tied to a field, use the field's own `Label` or a [label](components/label).",
                "For text people follow to another page, use a [link](components/link).",
            ],
            Related = ["label", "link"],
            Sections =
            [
                new("styles", "Styles", "`TextStyle` picks a step of the type scale. It changes only the look, and "
                    + "adds no meaning for screen readers.", typeof(TextExample)),
                new("elements", "Elements", "`As` renders the text as another element, so a heading is a real heading. "
                    + "Choose the level that fits the page's outline, whatever `TextStyle` it looks like.",
                    typeof(TextElementExample)),
                new("muted-and-tones", "Muted and tones", "`Muted` makes the text quieter, for hints and timestamps, "
                    + "and `Tone` colors it by meaning.", typeof(TextToneExample)),
                new("max-lines", "Cutting text off", "`MaxLines` cuts the text off after that many lines, with an "
                    + "ellipsis.", typeof(TextMaxLinesExample)),
            ],
            Api = [typeof(TbText)],
            Accessibility =
            [
                "Text cut off by `MaxLines` is all still there for screen readers.",
            ],
            AppDuties =
            [
                "Don't cut off text sighted people need without showing it somewhere else.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Full),
            BeforeInteractive =
            [
                PlainHtml,
            ],
            Notes =
            [
                new("`As` defaults to a paragraph, so a heading moved into `TbText` can keep its look through "
                    + "`TextStyle` but stops being a heading to screen readers. "
                    + "Give it a heading level, such as `As=\"TextElement.H2\"`.")
                    { Pitfall = true },
            ],
            Target = new("heading", "Display"),
            Blocks = ["tb-text"],
            TestingTip = "Text cut off by `MaxLines` is still all in the markup, so text assertions and `GetByText` "
                + "match the whole string. Whether it is cut off shows only in a browser, not in bUnit.",
        },
        new("kbd", "Kbd", ExampleCategory.Content,
            "Keyboard keys and shortcuts shown as keycaps, such as Ctrl+K, in help text and hints.")
        {
            WhenToUse =
            [
                "Use it wherever text names a key or a shortcut, so people see what to press.",
            ],
            WhenNotToUse =
            [
                "For code, or for words people type rather than keys they press, use a `code` element.",
            ],
            Related = ["text", "tooltip"],
            Sections =
            [
                new("basic", "Kbd", "`Keys` shows keys pressed together, such as `\"Ctrl+K\"`, each as its own keycap "
                    + "with a plus between them. `ChildContent` shows a single key, such as `Esc`.",
                    typeof(KbdExample)),
            ],
            Api = [typeof(TbKbd)],
            Accessibility =
            [
                "It renders `kbd` elements, so screen readers read the keys as text and the plus sign as \"plus\".",
                "Each keycap keeps its border in forced-colors mode, such as Windows high contrast.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Full),
            BeforeInteractive =
            [
                PlainHtml,
            ],
            Target = null,
            Blocks = ["tb-kbd"],
            DataParts =
            [
                new("key", "One key of a combination."),
            ],
            TestingTip = "Keys have no role of their own; find them by their text, as `GetByText(\"Ctrl\")`.",
        },
        new("avatar", "Avatar", ExampleCategory.Content,
            "A small picture of a person or a team: their photo, their initials, or an icon.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it to show who someone is at a glance, such as beside a comment, in a list of members, or as "
                    + "the button that opens their account menu.",
            ],
            WhenNotToUse =
            [
                "For a picture that is part of the content, use an [image](components/image).",
                "For a small symbol, use an [icon](components/icon).",
            ],
            Related = ["image", "badge", "menu"],
            Sections =
            [
                new("basic", "Picture, initials or icon", "`Name` says whose avatar it is. With `Src`, the avatar "
                    + "shows their picture. Without one, and while the picture loads or if it fails to, it shows the "
                    + "first letters of their first and last names. An empty `Initials` shows an icon instead: a "
                    + "person, unless `Icon` gives another.", typeof(AvatarExample)),
                new("size-and-shape", "Size and shape", "`Size` makes an avatar `Small`, `Medium` or `Large`, and "
                    + "`Shape=\"AvatarShape.Rounded\"` gives it rounded corners, as for a team or a project.",
                    typeof(AvatarSizeExample)),
                new("tone", "Tone", "`Tone` colors the initials and the space behind them. To tell people apart, "
                    + "pick it from something that stays the same for each, such as their id.",
                    typeof(AvatarToneExample)),
                new("group", "Group", "A `TbAvatarGroup` overlaps its avatars. Render only the ones that fit and set "
                    + "`More` to how many others there are; the group shows it as \"+3\".", typeof(AvatarGroupExample)),
                new("beside-a-name", "Beside a name", "Next to the person's name, `Decorative` keeps screen readers "
                    + "from hearing the name twice. A [badge](components/badge) with `Overlap=\"BadgeOverlap.Circular\"` "
                    + "marks their status.", typeof(AvatarNameExample)),
            ],
            Api = [typeof(TbAvatar), typeof(TbAvatarGroup)],
            Labeling =
            [
                "The avatar is an image named by `Name`, so screen readers hear \"Ada Lovelace, image\". It needs a "
                    + "`Name` unless it's `Decorative`, and throws when it renders without either.",
                "A group is named by its `AriaLabel`, such as \"Trip members\", and its count by `MoreLabel`, which "
                    + "defaults to \"3 more\" in English.",
            ],
            AppDuties =
            [
                "Give each avatar the person's full name, even when it shows only their initials or picture.",
                "Make an avatar `Decorative` when the name is right beside it.",
            ],
            Accessibility =
            [
                "The avatar has `role=\"img\"`, and its picture and initials are hidden from screen readers, so it's "
                    + "heard once, by its name.",
                "It takes no focus: to make one a button, put it inside a [button](components/button) or a menu's "
                    + "trigger, whose name says what it does.",
                "In forced-colors mode, such as Windows high contrast, each avatar keeps an outline.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Full),
            BeforeInteractive =
            [
                "It renders as plain HTML. A picture that fails to load lets the initials show through, with or "
                    + "without interactivity.",
            ],
            Target = new("img", "Ada Lovelace"),
            Blocks = ["tb-avatar", "tb-avatar-group"],
            DataParts =
            [
                new("avatar", "The avatar itself."),
                new("image", "Its picture, over the initials or icon."),
                new("initials", "Its initials."),
                new("icon", "Its icon, when there are no initials."),
                new("group", "A group of avatars."),
                new("more", "The group's count of the others, as in \"+3\"."),
            ],
            TestingTip = "Find an avatar by its role and name: `GetByRole(AriaRole.Img, new() { Name = \"Ada Lovelace\" })` "
                + "in Playwright, or `[data-part=avatar][aria-label='Ada Lovelace']` in bUnit.",
        },
        new("timeline", "Timeline", ExampleCategory.Content,
            "A list of events in order, such as a trip's history or an order's progress, each with a dot on a line "
                + "that joins it to the next.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it to show what happened, or what will happen, in order, such as an order's history or the steps "
                    + "of an application.",
            ],
            WhenNotToUse =
            [
                "For the steps of a task people move through themselves, use a [stepper](components/stepper).",
                "For a list without an order in time, use a [list](components/list).",
            ],
            Related = ["stepper", "list"],
            Sections =
            [
                new("basic", "Events", "Each `TbTimelineItem` has a `Title` and, with `Time`, the moment it happened. "
                    + "`TimeFormat` sets how the time is shown in the current culture, and it sits in a `time` element "
                    + "that machines can read. A time is shown in its own offset, so convert it to the reader's time "
                    + "zone first. Its content goes under the title. `Tone` colors its dot.", typeof(TimelineExample)),
                new("progress", "Progress", "`Icon` puts an icon in an event's dot, such as a check for a step that's "
                    + "done. Say in the title which event is the current one, since the color alone doesn't reach "
                    + "everyone.", typeof(TimelineProgressExample)),
            ],
            Api = [typeof(TbTimeline), typeof(TbTimelineItem)],
            Anatomy = """
                <TbTimeline AriaLabel="…">
                    <TbTimelineItem Title="…" Time="…" Tone="…">…</TbTimelineItem>
                </TbTimeline>
                """,
            Labeling =
            [
                "A list named by `AriaLabel`. Screen readers hear each event's place in it, as in \"2 of 4\". Leave "
                    + "`AriaLabel` out when a heading just before the timeline already names it.",
            ],
            Accessibility =
            [
                "The dots and the line are hidden from screen readers, so each event is heard by its time, title and "
                    + "content.",
                "In forced-colors mode, such as Windows high contrast, the dots and the line keep the system's text "
                    + "color.",
            ],
            AppDuties =
            [
                "Say in the title what a tone means, such as \"Payment failed\", since color alone doesn't reach "
                    + "everyone.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Full),
            BeforeInteractive =
            [
                PlainHtml,
            ],
            Target = new("list", "Trip history"),
            Blocks = ["tb-timeline"],
            DataParts =
            [
                new("timeline", "The list of events."),
                new("timeline-item", "One event."),
                new("dot", "An event's dot, or its icon."),
                new("time", "When the event happened."),
                new("title", "What happened."),
                new("content", "More about the event."),
            ],
            TestingTip = "Find the timeline with `GetByRole(AriaRole.List, new() { Name = \"Trip history\" })` and an "
                + "event with `GetByRole(AriaRole.Listitem).Filter(new() { HasText = \"Trip booked\" })`. A `time` "
                + "element's `datetime` attribute holds the time in ISO 8601, whatever the culture.",
        },
        new("carousel", "Carousel", ExampleCategory.Content,
            "Slides shown one at a time, such as featured trails or photos, with buttons to move between them.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for a few items of equal weight that share one space, such as featured trails on a home page.",
            ],
            WhenNotToUse =
            [
                "For content people need to compare or read in full, show it all, in a [list](components/list) or a "
                    + "grid of [cards](components/card).",
                "For steps people move through in order, use a [stepper](components/stepper).",
            ],
            Related = ["image", "card", "tabs"],
            Pattern = new("carousel", "https://www.w3.org/WAI/ARIA/apg/patterns/carousel/"),
            Sections =
            [
                new("basic", "Carousel", "Each `TbCarouselSlide` is one slide. The previous and next buttons and a "
                    + "button for each slide move between them, and `@bind-ActiveIndex` is the shown slide's "
                    + "position. A slide is named by its place, such as \"2 of 3\", unless `AriaLabel` names it.",
                    typeof(CarouselExample)),
                new("auto-play", "Turning by itself", "`AutoPlay` sets how often the carousel turns to the next "
                    + "slide by itself, and adds a button, first in the controls, that stops and starts it. It pauses "
                    + "while the pointer is over the carousel or focus is in it, stops when keyboard focus lands "
                    + "anywhere in it, and starts stopped for someone who asks for reduced motion.",
                    typeof(CarouselAutoPlayExample)),
                new("ends", "Ends and buttons", "`Loop=\"false\"` stops at the first and last slides and turns off "
                    + "the button that would pass them. `ShowPicker=\"false\"` leaves out the button for each slide.",
                    typeof(CarouselEndsExample)),
            ],
            Api = [typeof(TbCarousel), typeof(TbCarouselSlide), typeof(CarouselLabels)],
            Keyboard =
            [
                new(["Tab"], "Moves to the stop button, when `AutoPlay` is set, then previous and next, into the "
                    + "shown slide's content, and on to the slide buttons."),
                new(["Enter", "Space"], "Presses the focused button."),
            ],
            Labeling =
            [
                "The carousel is a region named by `AriaLabel` and announced as a carousel. Each slide is a group "
                    + "announced as a slide and named by its place, such as \"2 of 3\", or by its `AriaLabel`.",
                "The buttons are named \"Previous slide\", \"Next slide\", \"Slide 2\" and \"Stop slide rotation\" "
                    + "(\"Start slide rotation\" once stopped), or by `Labels`; the shown slide's button is marked as "
                    + "the current one.",
            ],
            Announcements =
            [
                "A slide the user brings in is read out, since the slides are a polite live region. While the "
                    + "slides turn by themselves, the live region is off, so the page doesn't keep talking.",
            ],
            Accessibility =
            [
                "Turning slides pause while the pointer is over the carousel or focus is in it. The stop button "
                    + "stops them until the start button is pressed, as WCAG 2.2.2 asks, and keyboard focus anywhere "
                    + "in the carousel does the same.",
                "At the ends of a carousel that doesn't loop, previous or next turns off but keeps focus.",
                "Each slide button's box is 24 pixels across, with a smaller dot drawn inside it.",
            ],
            Notes =
            [
                new("A slide added after the first render goes after the others, whatever its place in the markup. When "
                    + "another slide is removed, the shown one stays shown and `@bind-ActiveIndex` gets its new "
                    + "position; when the shown slide itself is removed, the one after it shows, or the last one if "
                    + "there's none after it."),
            ],
            Modes = new(ModeSupport.DisplayOnly, ModeSupport.DisplayOnly),
            BeforeInteractive =
            [
                "The first slide shows, and the buttons work once the page is interactive.",
            ],
            Ready = new(".tb-carousel", "a carousel that plays on its own"),
            Target = new("region", "Featured trails"),
            Blocks = ["tb-carousel"],
            DataParts =
            [
                new("controls", "The stop, previous and next buttons."),
                new("rotation", "The button that stops and starts the turning slides."),
                new("previous", "The previous button."),
                new("next", "The next button."),
                new("slides", "The slides."),
                new("slide", "One slide."),
                new("picker", "The buttons for each slide."),
                new("pick", "The button that shows one slide."),
            ],
            TestingTip = "Find it as `GetByRole(AriaRole.Region, new() { Name = \"Featured trails\" })`, move with "
                + "`GetByRole(AriaRole.Button, new() { Name = \"Next slide\" })`, and find the shown slide as the "
                + "visible `group` named, for example, \"2 of 3\". In bUnit, register a `FakeTimeProvider` as the "
                + "`TimeProvider` and advance it past the `AutoPlay` interval to turn the slides.",
        },
        new("virtual-list", "Virtual list", ExampleCategory.Content,
            "A long list of items of any height, such as thousands of trail reports, that draws only those near the "
                + "view.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for hundreds or thousands of items read one after another, such as a feed of reports, where "
                    + "drawing them all would slow the page.",
            ],
            WhenNotToUse =
            [
                "For rows with columns to sort and filter, use a [data grid](components/data-grid), which can "
                    + "virtualize too.",
                "For a list people choose from, use a [list box](components/listbox).",
                "For a few dozen items, use a plain [list](components/list).",
            ],
            Related = ["data-grid", "list", "listbox"],
            Pattern = new("feed", "https://www.w3.org/WAI/ARIA/apg/patterns/feed/"),
            Sections =
            [
                new("items", "Items in memory", "`Items` holds every item, and the content draws each one, at any height. "
                    + "`Height` gives the list a height to scroll in; without it the page scrolls. `ItemLabel` names "
                    + "each item for screen readers.", typeof(VirtualListExample)),
                new("provider", "Loading as it scrolls", "`ItemsProvider` loads only the items in view, such as from "
                    + "a database, and gives their total. `Placeholder` stands in while they load, and `ItemSize` is "
                    + "the height to expect until items are measured. `ScrollToAsync` scrolls an item into view, and "
                    + "can focus it.", typeof(VirtualListProviderExample)),
                new("empty", "No items", "`EmptyContent` shows when there are no items.",
                    typeof(VirtualListEmptyExample)),
            ],
            Api = [typeof(TbVirtualList<>)],
            Keyboard =
            [
                new(["Tab"], "Moves into the list, to the item that last had focus, and on through that item's "
                    + "controls."),
                new(["Page Down", "Page Up"], "Moves to the next or previous item, drawing it first if needed.")
                    { When = "In the list, outside a text field" },
                new(["Ctrl+End"], "Moves to the first control after the list.")
                    { When = "In the list, outside a text field" },
                new(["Ctrl+Home"], "Moves to the last control before the list.")
                    { When = "In the list, outside a text field" },
            ],
            Labeling =
            [
                "The list is a `feed` named by `AriaLabel`. Each item is an `article` named by `ItemLabel`, with its "
                    + "place and the total, such as 12 of 5000. Without `ItemLabel`, an item has no name, though screen "
                    + "readers still read its content.",
                "A list with no items is a `group` with the same name, holding `EmptyContent`, since a feed holds only "
                    + "articles.",
            ],
            Announcements =
            [
                "The feed is `aria-busy` while `ItemsProvider` loads, so screen readers wait for the new items.",
            ],
            Accessibility =
            [
                "When the focused item scrolls out and is no longer drawn, focus moves to the list, and Page Down and "
                    + "Page Up go on from that item.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "A static or prerendered page lists every item in `Items`, so for a very long list use "
                    + "`ItemsProvider` or turn prerendering off. With `ItemsProvider`, nothing shows until the page is "
                    + "interactive. Either way, the keys work only once the page is interactive.",
            ],
            Ready = new("[data-part=feed]", "the feed"),
            Target = new("feed", "Trail reports"),
            Blocks = ["tb-virtual-list"],
            DataParts =
            [
                new("feed", "The list."),
                new("item", "One item, with `data-index` for its place from 0."),
                new("placeholder", "An item's place while it loads."),
                new("empty", "The content shown when there are no items."),
            ],
            TestingTip = "Find it as `GetByRole(AriaRole.Feed, new() { Name = \"Trail reports\" })` and an item as "
                + "`GetByRole(AriaRole.Article, new() { Name = \"Report 12: Trail 13\" })`. Only items near the view "
                + "are drawn, so call `ScrollToAsync` or press Page Down to reach a far one. In bUnit, `Items` draws "
                + "the first ones.",
        },
        new("image", "Image", ExampleCategory.Content,
            "An image that requires alternative text and can scale to fit its container.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for photos and pictures that are part of the content.",
            ],
            WhenNotToUse =
            [
                "For a small symbol, use an [icon](components/icon).",
                "For a person's picture or initials, use an [avatar](components/avatar).",
            ],
            Related = ["icon", "surface", "avatar"],
            Sections =
            [
                new("fluid", "Fluid", "`Fluid` scales the image down to fit its container, keeping its shape. `Width` "
                    + "and `Height` reserve its space before it loads, so the page doesn't "
                    + "jump.", typeof(ImageExample)),
                new("fit", "Fit", "`Fit` sets how the image fills a box of another shape: `Cover` crops it, and "
                    + "`Contain` shrinks it to fit.", typeof(ImageFitExample)),
                new("lazy", "Lazy loading", "`Lazy` waits to load the image until it nears the screen. This picture "
                    + "is only decoration, so its `Alt` is empty and screen readers skip "
                    + "it.", typeof(ImageLazyExample)),
                new("fallback", "Fallback", "`FallbackSrc` shows another picture when `Src` fails to load.",
                    typeof(ImageFallbackExample)),
                new("events", "Load and error", "`OnLoad` runs when the image has loaded, and `OnError` when it can't. "
                    + "The second picture here fails, so the browser shows its `Alt`.", typeof(ImageEventsExample)),
            ],
            Api = [typeof(TbImage)],
            Labeling =
            [
                "`Alt` is required and describes what the image shows; leaving it out throws when it renders. Set it "
                    + "to an empty string for an image that is only decoration, which screen readers then skip.",
            ],
            AppDuties =
            [
                "Say what the image shows in its context, without \"image of\": screen readers already say it's an "
                    + "image.",
            ],
            Accessibility =
            [
                "It is a native `img`.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "It renders and loads as a plain `img`. A load or failure before the page is interactive raises "
                    + "neither event and doesn't switch to `FallbackSrc`.",
            ],
            Target = new("img", "A trail winding between two hills"),
            Blocks = ["tb-image"],
            TestingTip = "Find an image by its alternative text: `GetByRole(AriaRole.Img, new() { Name = \"A trail "
                + "winding between two hills\" })` in Playwright, or `img[alt='A trail winding between two hills']` in "
                + "bUnit.",
        },
        new("divider", "Divider", ExampleCategory.Layout,
            "A horizontal or vertical line that separates content.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it to separate groups of content where space alone doesn't make the break clear.",
            ],
            WhenNotToUse =
            [
                "To space things out, use the `Gap` of a [stack](components/stack) or [grid](components/grid).",
            ],
            Related = ["stack", "surface"],
            Sections =
            [
                new("horizontal", "Horizontal", "With no parameters, a divider is a line across the space it's in.",
                    typeof(DividerExample)),
                new("content", "Content on the line", "Content inside a divider, such as \"or\", sits on the line. "
                    + "`ContentAlign` moves it to the start or end.", typeof(DividerContentExample)),
                new("vertical", "Vertical", "`Vertical` makes the divider a line between items in a row.",
                    typeof(DividerVerticalExample)),
            ],
            Api = [typeof(TbDivider)],
            Accessibility =
            [
                "A horizontal divider is an `<hr>`, which screen readers treat as a separator. A vertical one is "
                    + "a `div` with `role=\"separator\"` and `aria-orientation=\"vertical\"`.",
                "It takes no focus and has no name.",
                "A divider with content is a plain `div` rather than a separator, whose children screen readers skip, "
                    + "so the content is read as text and the lines are only drawn.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Full),
            BeforeInteractive =
            [
                PlainHtml,
            ],
            Target = null,
            Blocks = ["tb-divider"],
            DataParts =
            [
                new("content", "The content between the two lines, such as \"or\"."),
            ],
        },
        new("surface", "Surface", ExampleCategory.Layout,
            "A raised panel with the app's surface color, an optional border and a shadow, for grouping content.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it to group related content into a panel that stands out from the page.",
            ],
            WhenNotToUse =
            [
                "To lay items out, use a [stack](components/stack) or [grid](components/grid) and put surfaces in them.",
                "To interrupt people with something on top of the page, use a [dialog](components/dialog).",
            ],
            Related = ["stack", "grid", "divider", "dialog", "card"],
            Sections =
            [
                new("elevation", "Elevation", "`Elevation` sets how far the surface seems to rise above the page. "
                    + "Add your own padding with the [spacing classes](guides/spacing).", typeof(SurfaceExample)),
                new("outlined", "Outlined", "`Outlined` adds a border, on its own or with a shadow.",
                    typeof(SurfaceOutlinedExample)),
                new("element", "Element", "`As` renders the surface as another element, such as "
                    + "`LayoutElement.Article` for a card that stands on its own, or `LayoutElement.Section` with an "
                    + "`aria-label` for a region screen readers can jump to.", typeof(SurfaceElementExample)),
            ],
            Api = [typeof(TbSurface)],
            Accessibility =
            [
                "A plain `div` by default, with no role of its own.",
            ],
            AppDuties =
            [
                "Screen readers and the keyboard follow the order in the markup, so keep it the same as the order on "
                    + "screen.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Full),
            BeforeInteractive =
            [
                PlainHtml,
            ],
            Target = null,
            Blocks = ["tb-surface"],
        },
        new("card", "Card", ExampleCategory.Layout,
            "A panel about one thing, such as a trail or a person: a title, a picture, some content and actions, and "
                + "optionally a link to more.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it to show several things of the same kind side by side, such as trails or people, each with a "
                    + "summary and a way to open or act on it.",
            ],
            WhenNotToUse =
            [
                "For a plain panel that groups content, use a [surface](components/surface).",
                "For rows to compare or sort, use a [data grid](components/data-grid) or a [list](components/list).",
            ],
            Related = ["surface", "grid", "image", "avatar"],
            Sections =
            [
                new("basic", "Title, content and actions", "`Title` is the card's heading and names it for screen "
                    + "readers, with `Subtitle` on a line under it. The content goes below them, and `Actions` holds "
                    + "buttons along the bottom.", typeof(CardExample)),
                new("media", "Picture and header", "`Media` puts a picture across the top. `Start` and `End` hold "
                    + "content beside the title, such as an [avatar](components/avatar) or a menu. Screen readers "
                    + "reach the title before the picture.", typeof(CardMediaExample)),
                new("link", "Cards that link", "With `Href`, the title becomes a link that covers the whole card, so "
                    + "a click anywhere on it follows the link, and the card is one tab stop. Buttons inside the card "
                    + "still work on their own. In a list of cards, put them in a [grid](components/grid) rendered as "
                    + "a list, with `As=\"LayoutElement.Li\"` on each grid item.", typeof(CardLinkExample)),
                new("elevation", "Elevation and border", "`Elevation` sets the shadow, which is `Low` unless set, and "
                    + "`Outlined` adds a border.", typeof(CardElevationExample)),
            ],
            Api = [typeof(TbCard)],
            Anatomy = """
                <TbCard Title="…" Subtitle="…" Href="…">
                    <Start>…</Start>
                    <End>…</End>
                    <Media>…</Media>
                    <ChildContent>…</ChildContent>
                    <Actions>…</Actions>
                </TbCard>
                """,
            Keyboard =
            [
                new(["Tab"], "Moves to the card's link, then to each control inside the card.")
                    { When = "With an `Href`" },
                new(["Enter"], "Follows the link.") { When = "With an `Href`" },
            ],
            Labeling =
            [
                "An `article` named by its title, which is a heading at `HeadingLevel` (3 unless set). Pick the "
                    + "level that fits the headings around the card.",
                "With `Href`, the link is named by the title alone, so screen readers don't read the whole card as "
                    + "its name.",
            ],
            Accessibility =
            [
                "With `Href`, the focus ring goes around the whole card.",
                "In forced-colors mode, such as Windows high contrast, each card keeps a border.",
            ],
            AppDuties =
            [
                "Describe each meaningful picture in its `Alt` text, and leave `Alt` empty on a picture that only "
                    + "decorates.",
                "Name each button inside a card for the card it belongs to, such as \"Save Old mill\", so it makes "
                    + "sense on its own.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Full),
            BeforeInteractive =
            [
                "It renders as plain HTML, and a card with an `Href` already works as a link.",
            ],
            Target = new("article", "Ridge loop"),
            Blocks = ["tb-card"],
            DataParts =
            [
                new("card", "The card itself."),
                new("header", "The row with the title, and the start and end content."),
                new("start", "The content before the title."),
                new("title", "The title's heading."),
                new("link", "The title's link, which covers the card."),
                new("subtitle", "The line under the title."),
                new("end", "The content after the title."),
                new("media", "The picture across the top."),
                new("content", "The card's content."),
                new("actions", "The buttons along the bottom."),
            ],
            TestingTip = "Find a card by its role and title, with "
                + "`GetByRole(AriaRole.Article, new() { Name = \"Ridge loop\" })` in Playwright. A card with an `Href` "
                + "is also found by its link, with `GetByRole(AriaRole.Link, new() { Name = \"Old mill\" })`.",
            Telemetry =
            [
                "With a `TelemetryName`, such as `\"trail-card\"`, it counts `trailblazor.control.shown` as it first "
                    + "renders and `trailblazor.control.used` each time its `Href` is followed.",
            ],
        },
        new("icon", "Icon", ExampleCategory.Content,
            "An SVG icon from any icon set, in one of three sizes and hidden from screen readers unless named.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it beside text, or alone with a `Label` where a symbol is clear enough.",
            ],
            WhenNotToUse =
            [
                "For an icon people press, use an [icon button](components/icon-button).",
                "For a photo or picture, use an [image](components/image).",
            ],
            Related = ["icon-button", "image"],
            Sections =
            [
                new("sizes", "Sizes", "`Size` picks a fixed step: `Small`, `Medium` by default, or `Large`.",
                    typeof(IconExample)),
                new("meaning", "Meaning", "`Label` names an icon that means something no text next to it says. `Tone` "
                    + "colors it by meaning, with the same text colors as `TbText`.",
                    typeof(IconMeaningExample)),
                new("icon-sets", "Icon sets", "`Icon` takes an icon from any set, such as "
                    + "`Trailblazor.Icons.Material`. The [icons guide](guides/icons) covers them.",
                    typeof(IconSetExample)),
                new("direction", "Right to left", "An icon keeps its direction on a right-to-left page, as a check "
                    + "mark or a clock should. One that points along the text, such as a forward arrow, a send icon or "
                    + "an indent icon, turns around when its `MirrorsInRightToLeft` is set, wherever it's used.",
                    typeof(IconDirectionExample)),
            ],
            Api = [typeof(TbIcon)],
            Labeling =
            [
                "Without a `Label` the icon is decorative and hidden from screen readers with `aria-hidden`.",
                "With a `Label` it's an `img` named by the label.",
            ],
            Accessibility =
            [
                "`Tone` colors meet contrast in both themes.",
            ],
            AppDuties =
            [
                "Color alone can't carry the meaning, so pair a `Tone` with a label or text.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Full),
            BeforeInteractive =
            [
                PlainHtml,
            ],
            Notes =
            [
                new("`Size` goes up to `Large`; an empty state's `Icon` already shows larger. For another big icon, "
                    + "set its width and height through `Style`: `Style=\"width: 3rem; height: 3rem\"`."),
            ],
            Target = new("img", "Failed"),
            Blocks = ["tb-icon"],
            TestingTip = "Find an icon with a `Label` by its role and name: `GetByRole(AriaRole.Img, new() { Name = "
                + "\"Failed\" })` in Playwright, or `[role=img][aria-label='Failed']` in bUnit. An icon without one is "
                + "hidden from screen readers and from `GetByRole`, so check the text beside it instead.",
        },
        new("button", "Button", ExampleCategory.Actions,
            "Starts an action, in filled, outlined or text styles and every tone.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it to start an action, such as saving or opening a dialog. Start its text with a verb, as in "
                    + "\"Save the trip\".",
                "Keep one filled, primary button in each part of a page, so the main action stands out.",
            ],
            WhenNotToUse =
            [
                "To go to another page, use a [link](components/link). Give the button an `Href` only where a link has "
                    + "to look like a button.",
                "With an icon and no text, use an [icon button](components/icon-button).",
                "For a setting that's on or off, use a [switch](components/switch).",
                "To pick one of a few views, use a [toggle group](components/toggle-group).",
            ],
            Related = ["link", "icon-button", "button-group", "toggle-group", "toolbar", "switch"],
            Pattern = new("button", "https://www.w3.org/WAI/ARIA/apg/patterns/button/"),
            Sections =
            [
                new("try-it", "Try it", "Use the controls to see what each parameter does.",
                    typeof(ButtonTryExample))
                {
                    Controls = new("<TbButton{controls}>Try this button</TbButton>",
                        ExampleControl.Choice(nameof(TbButton.Variant), Variant.Filled),
                        ExampleControl.Choice(nameof(TbButton.Tone), Tone.Primary),
                        ExampleControl.Choice(nameof(TbButton.Size), Size.Medium),
                        ExampleControl.Switch(nameof(TbButton.Disabled)),
                        ExampleControl.Switch(nameof(TbButton.Loading))),
                },
                new("variants-and-tones", "Variants and tones", "`Variant` sets how much weight a button has, and "
                    + "`Tone` what kind of action it is. Use `Danger` for what can't be undone. A soft neutral button "
                    + "takes the body text's color on a slightly stronger fill than other neutral parts, so it doesn't "
                    + "look disabled.", typeof(ButtonExample)),
                new("clicks", "Clicks", "`OnClick` runs when the button is pressed by a pointer, Enter or Space.",
                    typeof(ButtonClickExample)),
                new("sizes", "Sizes", "`Size` is `Small`, `Medium` or `Large`.", typeof(ButtonSizeExample)),
                new("full-width", "Full width", "`FullWidth` makes the button as wide as its container, which suits "
                    + "the main action at the end of a narrow form.", typeof(ButtonFullWidthExample)),
                new("icons-and-slots", "Icons and slots", "`StartIcon` and `EndIcon` add an icon beside the text. The "
                    + "`Start` and `End` slots hold other content, such as a badge. An icon adds nothing to the "
                    + "button's name, and slot content adds to it only if it has text of its own.",
                    typeof(ButtonIconExample)),
                new("loading", "Loading", "`Loading` shows a spinner and ignores clicks, so a slow action can't run "
                    + "twice. The button stays focusable, so focus isn't lost while it waits.",
                    typeof(ButtonLoadingExample)),
                new("disabled", "Disabled", "`Disabled` turns the button off and takes it out of the tab order. With "
                    + "`FocusableWhenDisabled` it stays a tab stop, so its tooltip can say why it is off.",
                    typeof(ButtonDisabledExample)),
                new("links", "Links", "`Href` renders a link drawn as a button, so it can open in a new tab. A "
                    + "disabled one has no `href`, so it can't be followed.", typeof(ButtonLinkExample)),
                new("toggle", "Toggle", "`@bind-Pressed` makes the button a toggle that stays pressed. Keep its text "
                    + "the same when it is pressed, so the state isn't announced twice. If the text has to change, use "
                    + "a plain button instead of a toggle.", typeof(ButtonToggleExample)),
                new("submit", "Submit", "A button defaults to `type=\"button\"`, so it never submits a form by "
                    + "accident. `Type=\"ButtonType.Submit\"` makes it submit its form.", typeof(ButtonSubmitExample)),
                new("focus", "Moving focus", "Take a `@ref` to the button and call `FocusAsync()`, for example to "
                    + "move focus to a Retry button when a save fails.", typeof(ButtonFocusExample)),
                new("long-labels", "Long labels", "Long text wraps inside the button, and the button grows to fit.",
                    typeof(ButtonLongExample)),
            ],
            Api = [typeof(TbButton), typeof(ButtonClasses)],
            Keyboard =
            [
                new(["Enter", "Space"], "Presses it.") { When = "As a button" },
                new(["Enter"], "Follows the link.") { When = "With an `Href`" },
            ],
            Labeling =
            [
                "A native `<button>`, named by its text. A decorative icon, from `StartIcon`, `EndIcon` or the `Start` "
                    + "and `End` slots, adds nothing to the name, so a button with only an icon has no name. Use an "
                    + "[icon button](components/icon-button) for that.",
                "With `Href` it is a `link`, and is announced as one.",
            ],
            Announcements =
            [
                "`Loading` marks it `aria-busy`.",
                "With `@bind-Pressed`, its state is announced through `aria-pressed`.",
            ],
            Accessibility =
            [
                "A `Loading` button, or a `Disabled` one with `FocusableWhenDisabled`, is marked `aria-disabled` and "
                    + "stays a tab stop.",
                "With reduced motion, the busy spinner stops turning. In forced-colors mode a pressed button takes "
                    + "the system highlight.",
            ],
            AppDuties =
            [
                "Give it text that says what it does. If it opens a panel of your own, return focus to it when the panel "
                    + "closes. A [dialog](components/dialog) or [menu](components/menu) does this for you.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "It renders as the same native element. A link already works, and a submit button posts its form. "
                    + "`OnClick` starts working once the page is interactive.",
            ],
            Notes =
            [
                new("A button is `Filled` and `Primary` unless told otherwise, which suits the one main action in an "
                    + "area. Give the other actions an `Outlined` or `Text` variant, or `Tone.Neutral`, so the main "
                    + "one stands out."),
            ],
            Target = new("button", "Add item"),
            Blocks = ["tb-button"],
            DataParts =
            [
                new("start", "The start icon or content, before the label."),
                new("icon", "An icon button's icon."),
                new("label", "The button's text."),
                new("spinner", "The spinner shown while the button is loading."),
                new("end", "The end icon or content, after the label."),
            ],
            TestingTip = "Check a toggle with `ToHaveAttributeAsync(\"aria-pressed\", \"true\")`, and a disabled "
                + "button with `ToBeDisabledAsync()`, which also passes for `aria-disabled`. To move focus to a "
                + "button from code, `@ref` it and call `FocusAsync()`.",
            Telemetry = ButtonTelemetry("TbButton"),
        },
        new("icon-button", "Icon button", ExampleCategory.Actions,
            "A button that shows only an icon, with the accessible name it needs.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it in a row of tools, or where there's no room for text and the icon is widely understood, such as "
                    + "an X to close or a trash can to delete.",
            ],
            WhenNotToUse =
            [
                "When people could misread the icon, use a [button](components/button) with text.",
                "To link to another page with text instead of an icon, use a [link](components/link).",
            ],
            Related = ["button", "button-group", "toolbar", "tooltip"],
            Pattern = new("button", "https://www.w3.org/WAI/ARIA/apg/patterns/button/"),
            Sections =
            [
                new("variants-and-tones", "Variants and tones", "`Icon` is what the button shows and `AriaLabel` is "
                    + "its name. `Variant` and `Tone` work as they do for a [button](components/button).",
                    typeof(IconButtonExample)),
                new("sizes", "Sizes", "`Size` is `Small`, `Medium` or `Large`.", typeof(IconButtonSizeExample)),
                new("tooltip", "Tooltip", "`ShowTooltip` shows the button's name on hover and on keyboard focus, so "
                    + "sighted people can read it too.", typeof(IconButtonTooltipExample)),
                new("toggle", "Toggle", "`@bind-Pressed` makes the button a toggle. Keep the same `AriaLabel` whether "
                    + "it's on or off, since `aria-pressed` tells which.", typeof(IconButtonToggleExample)),
                new("mixed", "Mixed", "`Mixed` marks a toggle that is on for only part of what it acts on, such as Bold "
                    + "over text that is partly bold. It gets a light fill and a dashed edge, and screen readers "
                    + "announce it as mixed. A press turns it on, and the app then works out `Pressed` and `Mixed` "
                    + "again.",
                    typeof(IconButtonMixedExample)),
                new("links", "Links", "`Href` renders a link drawn as an icon button.", typeof(IconButtonLinkExample)),
                new("loading", "Loading", "`Loading` shows a spinner in place of the icon and ignores clicks.",
                    typeof(IconButtonLoadingExample)),
                new("disabled", "Disabled", "`Disabled` turns the button off and takes it out of the tab order. With "
                    + "`FocusableWhenDisabled` it stays a tab stop, so its tooltip can still be read.",
                    typeof(IconButtonDisabledExample)),
            ],
            Api = [typeof(TbIconButton), typeof(IconButtonClasses)],
            Keyboard =
            [
                new(["Enter", "Space"], "Presses it.") { When = "As a button" },
                new(["Enter"], "Follows the link.") { When = "With an `Href`" },
            ],
            Labeling =
            [
                "`AriaLabel` is required and names the button, since it shows no text. A blank label written as text "
                    + "in markup gets build warning TB001, and any blank label throws when it renders.",
                "The icon itself is hidden from screen readers.",
            ],
            Announcements =
            [
                "The tooltip repeats the name, so it isn't added as a description, and screen readers hear the name "
                    + "once.",
                "`Loading` marks it `aria-busy`.",
                "With `@bind-Pressed`, its state is announced through `aria-pressed`.",
            ],
            Accessibility =
            [
                "A `Loading` icon button, or a `Disabled` one with `FocusableWhenDisabled`, is marked `aria-disabled` "
                    + "and stays a tab stop, as a button does.",
                "With reduced motion, the busy spinner stops turning. In forced-colors mode a pressed button takes "
                    + "the system highlight.",
            ],
            AppDuties =
            [
                "Word `AriaLabel` as what the button does, and say which item it acts on where several look the same, "
                    + "as in \"Remove Pine Hollow\".",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "It renders a native button, or a link with an `Href`. The link already works, and `OnClick` starts "
                    + "working once the page is interactive.",
            ],
            Target = new("button", "Add"),
            Blocks = ["tb-button", "tb-icon-button"],
            DataParts =
            [
                new("icon", "The icon the button shows."),
                new("spinner", "The spinner shown in place of the icon while loading."),
            ],
            TestingTip = "Find it by its name: `GetByRole(AriaRole.Button, new() { Name = \"Delete\" })`. With "
                + "`ShowTooltip`, focusing it shows a `tooltip` role with the same text.",
            Telemetry = ButtonTelemetry("TbIconButton"),
        },
        new("button-group", "Button group", ExampleCategory.Actions,
            "Joins related buttons into one control, in a row or a column, with settings shared by all of them.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it to join a few buttons that belong together, such as a split button.",
            ],
            WhenNotToUse =
            [
                "To pick one option and keep it picked, use a [toggle group](components/toggle-group).",
                "For a bar of many tools that arrow keys move through, use a [toolbar](components/toolbar).",
            ],
            Related = ["button", "toggle-group", "toolbar", "menu"],
            Sections =
            [
                new("basic", "Joined buttons", "Buttons inside a `TbButtonGroup` join into one control.",
                    typeof(ButtonGroupExample)),
                new("split-button", "Split button", "A button wrapped in a menu, popover or tooltip still joins the "
                    + "group, so a split button is a button and a menu side by side. Give the menu's icon trigger a "
                    + "`Label` that says what it opens.", typeof(ButtonGroupSplitExample)),
                new("vertical", "Vertical", "`Vertical` stacks the buttons in a column.",
                    typeof(ButtonGroupVerticalExample)),
                new("shared-settings", "Shared settings", "`Variant`, `Tone` and `Size` apply to every button that "
                    + "doesn't set its own. `Disabled` turns every button off, as each button's own `Disabled` would.",
                    typeof(ButtonGroupSharedExample)),
            ],
            Api = [typeof(TbButtonGroup)],
            Anatomy = """
                <TbButtonGroup aria-label="…">
                    <TbButton>…</TbButton>
                    <TbButton>…</TbButton>
                    <TbMenu Label="…">…</TbMenu>
                </TbButtonGroup>
                """,
            Keyboard =
            [
                new(["Tab"], "Reaches each button. The group adds no arrow-key navigation."),
            ],
            Labeling =
            [
                "A `div` with `role=\"group\"`, named by `aria-label`.",
            ],
            Accessibility =
            [
                "Each button keeps its own role, name and tab stop.",
            ],
            AppDuties =
            [
                "Give the group an `aria-label` that says what the buttons do together.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "It renders its buttons, joined. A link or submit button in it already works, and `OnClick` starts "
                    + "working once the page is interactive.",
            ],
            Target = new("group", "Text alignment"),
            Blocks = ["tb-button-group"],
            TestingTip = "Find the group by its name, then its buttons inside it: `GetByRole(AriaRole.Group, new() { "
                + "Name = \"Text alignment\" }).GetByRole(AriaRole.Button, new() { Name = \"Center\" })` in "
                + "Playwright, or `[role=group][aria-label='Text alignment'] button` in bUnit.",
        },
        new("floating-action-button", "Floating action button", ExampleCategory.Actions,
            "A round button for the main action of a screen that stays in view as the page scrolls, and a speed dial "
                + "that opens a few related actions from it.")
        {
            WhenToUse =
            [
                "Use it for the one action a screen is for, such as writing a message in a mail app, especially on a "
                    + "phone, where it sits within reach of a thumb.",
                "Use a speed dial when that action comes in a few kinds, such as a new trail, folder or reminder.",
            ],
            WhenNotToUse =
            [
                "For an action among others, or for one that is minor or destructive, use a "
                    + "[button](components/button).",
                "For more than about six actions, or actions without icons, use a [menu](components/menu).",
            ],
            Related = ["button", "menu", "app-shell"],
            Sections =
            [
                new("basic", "A floating button", "`TbFab` takes an `Icon` and a `Label`, which names it for screen "
                    + "readers. It stays in the window's bottom corner on the reading direction's end as the page "
                    + "scrolls, clear of a phone's rounded corners and home bar. Dialogs and popups open over it. "
                    + "`--tb-fab-offset` moves it further from the edge, such as above a bottom bar. Here the frame's "
                    + "`contain: layout` holds it, as it would hold any fixed element.",
                    typeof(FabExample)),
                new("extended", "With its label", "`Extended` shows the `Label` beside the icon, for an action the "
                    + "icon alone doesn't make clear. `Position` picks a corner, the bottom edge's center, or "
                    + "`Inline`, which leaves the button where the layout puts it, such as in an app bar.",
                    typeof(FabExtendedExample)),
                new("speed-dial", "Speed dial", "`TbSpeedDial` opens its `TbMenuItem`s as round actions above the "
                    + "trigger, or below it at the top of the window, each with its text beside it. It is a menu "
                    + "button, so it takes a menu's keys. Choosing an action, pressing Escape or clicking outside "
                    + "closes it. "
                    + "Its default plus turns into a cross while it is open.", typeof(SpeedDialExample)),
            ],
            Api = [typeof(TbFab), typeof(TbSpeedDial), typeof(FabPosition)],
            Keyboard =
            [
                new(["Enter", "Space"], "Runs a floating button's action."),
                new(["Enter", "Space", "Down"], "Opens a speed dial on its first action.") { When = "On a speed dial" },
                new(["Up"], "Opens a speed dial on its last action.") { When = "On a speed dial" },
                new(["Up", "Down"], "Moves through the actions, wrapping at the ends.") { When = "In a speed dial" },
                new(["Home", "End"], "Jumps to the first or last action.") { When = "In a speed dial" },
                new(["Enter", "Space"], "Chooses the focused action.") { When = "In a speed dial" },
                new(["Escape"], "Closes the speed dial and returns focus to its trigger.") { When = "In a speed dial" },
                new(["Tab"], "Closes the speed dial and moves on.") { When = "In a speed dial" },
            ],
            Labeling =
            [
                "A floating button and a speed dial's trigger are named by `Label`, shown as text only with "
                    + "`Extended`.",
                "Each action is named by its text, which shows beside its icon.",
            ],
            Announcements =
            [
                "A floating button is a `button`. A speed dial's trigger is a button with `aria-haspopup=\"menu\"` "
                    + "and `aria-expanded`, and its actions are `menuitem`s in a `menu`.",
            ],
            Accessibility =
            [
                "Tab reaches the button where it comes in the page's markup, not where it shows, so put it right "
                    + "after the content it acts on.",
                "A fixed button can cover the end of the page. Leave room at the bottom of a scrolling page, as much "
                    + "as the button and its `--tb-fab-offset`, so nothing stays hidden under it.",
            ],
            AppDuties =
            [
                "Give each screen one floating button at most, and a `Label` that says what it does.",
                "Leave room under the page's last content for a fixed button.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "A floating button shows in place; its `OnClick` starts working once the page is interactive. A speed "
                    + "dial opens on click before then, as a menu does, with its actions at the window's corner "
                    + "rather than beside the trigger.",
            ],
            Ready = new("[aria-haspopup=menu]", "a speed dial's button"),
            Target = new("button", "New message"),
            Blocks = ["tb-fab", "tb-speed-dial"],
            DataParts =
            [
                new("icon", "The button's icon, and each action's."),
                new("label", "An extended button's text, and each action's."),
                new("trigger", "The button that opens a speed dial."),
                new("menu", "A speed dial's list of actions."),
            ],
            Anatomy = """
                <TbFab Icon="…" Label="…" OnClick="…" />
                <TbSpeedDial Label="…">
                    <TbMenuItem Icon="…" OnClick="…">…</TbMenuItem>
                </TbSpeedDial>
                """,
            TestingTips =
            [
                "Find a floating button by its name: `GetByRole(AriaRole.Button, new() { Name = \"New message\" })` "
                    + "in Playwright, or `button[aria-label='New message']` in bUnit.",
                "A speed dial is a menu: `await page.Tb().Menu(\"Add\").ChooseAsync(\"Trail\")` from "
                    + "`Trailblazor.Testing.Playwright` opens it and chooses the action, and so does "
                    + "`await cut.Tb().Menu(\"Add\").ChooseAsync(\"Trail\")` in bUnit.",
            ],
            Telemetry =
            [
                .. ButtonTelemetry("TbFab"),
                "A speed dial with a `TelemetryName` counts `trailblazor.control.shown` as it first renders and "
                    + "`trailblazor.control.used` each time it opens, tagged `trailblazor.component` `TbSpeedDial`. An "
                    + "action with its own `TelemetryName` counts `trailblazor.control.used` each time it is chosen.",
            ],
        },
        new("toggle-group", "Toggle group", ExampleCategory.Actions,
            "A row of toggle buttons where one, or any number, can be pressed.")
        {
            FullWidth = true,
            RightToLeftKeys = "Left moves to the next item and Right to the previous one.",
            WhenToUse =
            [
                "Use it for a compact row of choices that take effect right away, such as a map's view.",
            ],
            WhenNotToUse =
            [
                "For a choice in a form that's sent later, use a [radio group](components/radio-group).",
                "For buttons that each start an action, use a [button group](components/button-group).",
                "For one setting that's on or off, use a [switch](components/switch).",
            ],
            Related = ["radio-group", "segmented-control", "button-group", "toolbar", "switch"],
            Pattern = new("button", "https://www.w3.org/WAI/ARIA/apg/patterns/button/"),
            Sections =
            [
                new("one", "Pick one", "`TbToggleGroup` keeps one item pressed, and `Value` holds it.",
                    typeof(ToggleGroupExample)),
                new("any-number", "Pick any number", "`TbMultiToggleGroup` lets any number of items stay pressed, and "
                    + "`Value` holds them.", typeof(ToggleGroupMultipleExample)),
                new("items", "Items", "In either group, an item shows its `Text`, or `ChildContent` for richer content. "
                    + "It can have an `Icon` or be `Disabled`, and one with only an icon needs an `AriaLabel`.",
                    typeof(ToggleGroupItemsExample)),
                new("clearable", "Clearable", "`Clearable` lets people clear a `TbToggleGroup` by pressing its pressed "
                    + "item again. Without it, that item stays pressed.", typeof(ToggleGroupClearableExample)),
                new("vertical", "Vertical", "`Vertical` stacks the items in a column, and the up and down arrows move "
                    + "between them.", typeof(ToggleGroupVerticalExample)),
                new("style", "Variant, tone and size", "`Variant`, `Tone` and `Size` apply to every item.",
                    typeof(ToggleGroupStyleExample)),
                new("disabled", "Disabled", "`Disabled` turns the whole group off.", typeof(ToggleGroupDisabledExample)),
            ],
            Api = [typeof(TbToggleGroup<>), typeof(TbMultiToggleGroup<>), typeof(TbToggleItem<>)],
            Anatomy = """
                <TbToggleGroup AriaLabel="…" @bind-Value="_value">
                    <TbToggleItem Value="…" Text="…" />
                    <TbToggleItem Value="…" Text="…" />
                </TbToggleGroup>

                <TbMultiToggleGroup AriaLabel="…" @bind-Value="_values">
                    <TbToggleItem Value="…" Text="…" />
                </TbMultiToggleGroup>
                """,
            Keyboard =
            [
                new(["Tab"], "Moves into the group and out of it, as one tab stop. Focus lands on the last focused "
                    + "item, or else the first pressed item, or else the first item that isn't disabled."),
                new(["Left", "Right"], "Moves between the items, skipping disabled ones.") { When = "In a row" },
                new(["Up", "Down"], "Moves between the items, skipping disabled ones.") { When = "When `Vertical`" },
                new(["Home", "End"], "Goes to the first or last item that isn't disabled."),
                new(["Enter", "Space"], "Presses the focused item."),
            ],
            Labeling =
            [
                "A `group` named by its required `AriaLabel`. An item with only an icon needs an `AriaLabel` of its "
                    + "own, which also shows in a tooltip on hover and keyboard focus. A blank label on either throws "
                    + "when it renders.",
            ],
            Announcements =
            [
                "Each item is a toggle button, whose state is announced through `aria-pressed`.",
            ],
            Accessibility =
            [
                "In forced-colors mode a pressed item takes the system highlight.",
            ],
            AppDuties =
            [
                "Give the group an `AriaLabel` that says what people are choosing, such as \"Map view\".",
            ],
            Modes = new(ModeSupport.DisplayOnly, ModeSupport.DisplayOnly),
            BeforeInteractive =
            [
                "The items render with any pressed ones marked. Pressing them, the arrow keys and the single tab stop "
                    + "start working once the page is interactive. Until then, each item is its own tab stop.",
            ],
            Notes =
            [
                new("`Comparer` decides whether two values are the same, such as entities matched by id, so a value "
                    + "loaded again still presses its item."),
                new("For a choice with three answers, such as Any, Yes and No, give each item its own value, such as "
                    + "an enum member. Don't use `null` for one of them: `null` is also the group's value before "
                    + "anything is chosen and after a `Clearable` group is cleared, so that item would look chosen "
                    + "when nothing is."),
                new("In a `TbToggleGroup` that isn't `Clearable`, pressing the pressed item again raises no "
                    + "`ValueChanged`, since the value stays the same."),
            ],
            Ready = new(".tb-toggle-group", "the group"),
            Target = new("group", "Map view"),
            Blocks = ["tb-toggle-group", "tb-button"],
            DataParts =
            [
                new("item", "One toggle button in the group."),
            ],
            TestingTip = "Press an item by its name, as `GetByRole(AriaRole.Button, new() { Name = \"Satellite\" })`, "
                + "and check it with `ToHaveAttributeAsync(\"aria-pressed\", \"true\")`. Focusing an icon-only item shows "
                + "a `tooltip` role with its name.",
            Telemetry =
            [
                "With a `TelemetryName` on an item, such as `\"view-as-list\"`, the item counts "
                    + "`trailblazor.control.shown` as it first renders and `trailblazor.control.used` each time it is "
                    + "pressed.",
            ],
        },
        new("segmented-control", "Segmented control", ExampleCategory.Actions,
            "A row of joined segments that picks one of a few short options, as a radio group drawn more compactly.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for one choice from two to five short options, such as a unit or a size, where a column of "
                    + "radios would take too much room.",
            ],
            WhenNotToUse =
            [
                "For longer options, or more than a few, use a plain [radio group](components/radio-group).",
                "For a row whose choice takes effect right away, such as a map's view, or where several can be pressed "
                    + "at once, use a [toggle group](components/toggle-group).",
                "For switching between panels of content, use [tabs](components/tabs).",
            ],
            Related = ["radio-group", "toggle-group", "tabs"],
            Pattern = new("radio group", "https://www.w3.org/WAI/ARIA/apg/patterns/radio/"),
            RightToLeftKeys = "Left moves to the next segment and Right to the previous one, except in Safari, which "
                + "doesn't swap them.",
            Sections =
            [
                new("segments", "Segments", "`Segmented` on a `TbRadioGroup` draws its options as segments, the "
                    + "chosen one filled. It is still a radio group, so `Value`, the form post and validation work the "
                    + "same way.", typeof(SegmentedControlExample)),
                new("states", "Size, tone, disabled and read-only", "`Size` and `Tone` work as on a radio group, the "
                    + "tone filling the chosen segment. `Disabled` on an option turns off that segment alone, and "
                    + "`ReadOnly` turns off every segment but the chosen one.",
                    typeof(SegmentedControlStatesExample)),
            ],
            Api = [typeof(TbRadioGroup<>), typeof(TbRadio<>), typeof(ChoiceClasses)],
            Anatomy = """
                <TbRadioGroup Label="…" @bind-Value="_value" Segmented>
                    <TbRadio Value="…">…</TbRadio>
                    <TbRadio Value="…">…</TbRadio>
                </TbRadioGroup>
                """,
            Keyboard =
            [
                new(["Tab"], "Moves into the control and out of it, as one tab stop. Focus lands on the chosen segment. "
                    + "With nothing chosen, it lands on the first segment that isn't disabled."),
                new(["Right", "Down"], "Moves to the next segment that isn't disabled and chooses it. Most browsers "
                    + "wrap at the end of the control, but Safari stops there."),
                new(["Left", "Up"], "Moves to the previous segment that isn't disabled and chooses it, wrapping the "
                    + "same way."),
                new(["Space"], "Chooses the focused segment, when nothing is chosen yet."),
            ],
            Labeling =
            [
                "The `Label` is the `fieldset`'s legend and names the group. Each segment is a native radio, named by "
                    + "its content. An `AriaLabel` in place of the `Label` hides the legend but still names the group; use it "
                    + "only when the segments make sense without a visible label.",
            ],
            Announcements =
            [
                "Screen readers announce each segment as a radio, with its position and whether it is chosen.",
            ],
            Accessibility =
            [
                "In every `Tone`, the chosen segment's fill stands out from the unchosen ones by at least 3:1, and its "
                    + "text from the fill by at least 4.5:1.",
                "The focused segment shows the focus ring around it, since its radio is hidden.",
                "In forced-colors mode the chosen segment takes the system highlight.",
            ],
            AppDuties =
            [
                "Keep each option to a word or two, so the segments fit on one line on a narrow screen.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Partial),
            BeforeInteractive =
            [
                "Native radios give the tab stop and the arrow keys, and CSS fills the chosen segment from its checked "
                    + "radio. So choosing works before the page is interactive, and a static form posts the chosen "
                    + "value.",
            ],
            Target = new("radiogroup", "Trail difficulty"),
            Blocks = ["tb-radio-group", "tb-radio"],
            DataParts =
            [
                new("label", "The group's legend, and each segment's text."),
                new("helper", "The help text under the group."),
                new("error", "The validation message, shown while the value is invalid."),
                new("options", "The row of segments."),
                new("input", "One segment's radio, hidden over the segment."),
            ],
            TestingTip = "Each segment is a native radio, so Playwright's `CheckAsync()` works on "
                + "`GetByRole(AriaRole.Radio, new() { Name = \"Hard\" })` inside the group, as on a radio group. The "
                + "radio covers its segment's text, so click the radio rather than the text. In bUnit, `Change(true)` on "
                + "a segment's `input` chooses it.",
        },
        new("theme-picker", "Theme picker", ExampleCategory.Actions,
            "Lets people choose a light or dark page, or follow their device, and remembers the choice.")
        {
            FullWidth = true,
            RightToLeftKeys = "Left moves to the next choice and Right to the previous one.",
            WhenToUse =
[
                "Use it in a settings page or header, so people can choose a light or dark page.",
            ],
            WhenNotToUse =
            [
                "For a control of a different shape, use `IThemeService` directly, as [your own control]"
                    + "(components/theme-picker#custom) shows.",
            ],
            Related = ["toggle-group", "switch"],
            Sections =
            [
                new("modes", "Choosing a mode", "`TbThemePicker` offers Light, Dark and Match the device, and "
                    + "remembers the choice. `TbThemeHead` in `<head>` puts a remembered choice back before the page "
                    + "paints, so a dark page never flashes light first.", typeof(ThemePickerExample)),
                new("custom", "Your own control", "`IThemeService` gives the current `Mode` and `Palette`, sets them, "
                    + "and raises `Changed`, so any control can drive the theme. Call `LoadAsync` once the page is "
                    + "interactive to read the remembered choice.", typeof(ThemePickerCustomExample)),
            ],
            Api = [typeof(TbThemePicker), typeof(IThemeService), typeof(ThemeSettings)],
            Keyboard =
            [
                new(["Tab"], "Moves into the picker and out of it, as one tab stop."),
                new(["Left", "Right"], "Moves between the choices."),
                new(["Home", "End"], "Goes to the first or last choice."),
                new(["Enter", "Space"], "Picks the focused choice."),
            ],
            Labeling =
            [
                "A `group` named \"Theme\", holding toggle buttons for Light, Dark and Match the device, each named by "
                    + "the localizer.",
            ],
            Announcements =
            [
                "The current choice is announced through `aria-pressed`.",
            ],
            Accessibility =
            [
                "It behaves as a [toggle group](components/toggle-group) does.",
            ],
            Modes = new(ModeSupport.DisplayOnly, ModeSupport.DisplayOnly),
            BeforeInteractive =
            [
                "`TbThemeHead` applies a remembered theme before the page paints. Picking a choice starts working once "
                    + "the page is interactive.",
            ],
            Ready = new(".tb-toggle-group", "the group"),
            Target = new("group", "Theme"),
            Blocks = ["tb-toggle-group", "tb-button"],
            TestingTip = "Pick a mode with `GetByRole(AriaRole.Button, new() { Name = \"Dark\" })`, and check the "
                + "page with `ToHaveAttributeAsync(\"data-tb-theme\", \"dark\")` on `html`. A test that needs one "
                + "theme can set `data-tb-theme` on `html` itself. Focusing a choice shows a `tooltip` role with its "
                + "name.",
        },
        new("toolbar", "Toolbar", ExampleCategory.Actions,
            "A row of controls that is one tab stop. When it is too narrow, its toolbar buttons move into a menu at "
                + "its end.")
        {
            FullWidth = true,
            RightToLeftKeys = "Left moves to the next control and Right to the previous one.",
            WhenToUse =
            [
                "Use it for a bar of tools or view controls that apply to one area, such as an editor or a map.",
            ],
            WhenNotToUse =
            [
                "For two or three related buttons, use a [button group](components/button-group).",
                "To pick one view from a few, use a [toggle group](components/toggle-group) on its own.",
                "For site navigation, use a list of [links](components/link) in a `nav`.",
            ],
            Related = ["button-group", "toggle-group", "icon-button", "spacer"],
            Pattern = new("toolbar", "https://www.w3.org/WAI/ARIA/apg/patterns/toolbar/"),
            Sections =
            [
                new("overflow", "Buttons that move into a menu", "A `TbToolbarButton` with no room moves into a menu "
                    + "at the toolbar's end, keeping its text and action. Other content, such as a toggle group, never "
                    + "moves. Drag the frame's corner here to see it happen.", typeof(ToolbarExample)),
                new("overflow-label", "Naming the menu", "`OverflowLabel` names the button that opens the overflow "
                    + "menu. The name is \"More\" unless set.", typeof(ToolbarOverflowExample)),
                new("vertical", "Vertical", "`Vertical` stacks the controls in a column, and the up and down arrows "
                    + "move between them. A vertical toolbar never moves buttons into a menu.",
                    typeof(ToolbarVerticalExample)),
                new("sizes", "Sizes", "`Size` on the toolbar is `Small`, `Medium` or `Large`, the same steps as a "
                    + "button's. It sizes the toolbar buttons and the More button, but a `TbToolbarButton` with its "
                    + "own `Size` keeps it.", typeof(ToolbarSizeExample)),
            ],
            Api = [typeof(TbToolbar), typeof(TbToolbarButton)],
            Keyboard =
            [
                new(["Tab"], "Moves into the toolbar and out of it, as one tab stop."),
                new(["Left", "Right"], "Moves between every control in the toolbar, including toggle group items and "
                    + "a `Disabled` button with `FocusableWhenDisabled`, even one with an `Href`.")
                    { When = "In a row" },
                new(["Up", "Down"], "Moves between every control in the toolbar.") { When = "When `Vertical`" },
                new(["Home", "End"], "Goes to the first or last control."),
            ],
            Labeling =
            [
                "A `toolbar` named by its required `AriaLabel`.",
                "A `TbToolbarButton` shows its `Label`. With an `Icon`, it is an icon button named by its "
                    + "`AriaLabel`, which its tooltip shows too.",
                "The overflow menu's button is named by `OverflowLabel`, \"More\" unless set.",
            ],
            Announcements =
            [
                "A tooltip that repeats a button's name isn't read a second time as its description.",
            ],
            Accessibility =
            [
                "In the overflow menu, each button is a `menuitem` with the same text and action.",
                "When the focused button moves into the overflow menu, focus moves to the menu's button. When that "
                    + "button goes away, focus moves to the last control.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "Every control renders in the row. The arrow keys, the single tab stop and the overflow menu start "
                    + "working once the page is interactive.",
            ],
            Notes =
            [
                new("A text field, select or editable area in the toolbar keeps its own arrow, Home and End keys."),
            ],
            Ready = new("[role=toolbar]", "the toolbar"),
            Target = new("toolbar", "Trail notes"),
            Blocks = ["tb-toolbar"],
            DataParts =
            [
                new("overflow", "The More menu that holds the items a narrow toolbar can't fit."),
            ],
            Anatomy = """
                <TbToolbar AriaLabel="…">
                    <TbToolbarButton AriaLabel="…" Icon="…" OnClick="…" />
                    <TbToolbarButton AriaLabel="…" Icon="…" OnClick="…" />
                </TbToolbar>
                """,
            TestingTip = "Find a toolbar button by its name, as `GetByRole(AriaRole.Button, new() { Name = \"Add a "
                + "note\" })`. To test the overflow, set the page's viewport narrow, wait for the toolbar's "
                + "`data-ready`, open the menu button named \"More\" and choose the `menuitem`.",
        },
        new("insert-divider", "Insert divider", ExampleCategory.Actions,
            "A line between two items that reveals an insert button on hover or keyboard focus, for adding an item "
                + "in that place.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it to add an item at an exact place in a short list.",
            ],
            WhenNotToUse =
            [
                "For a long list, use one [button](components/button) that adds after the focused item, since every "
                    + "divider is a tab stop.",
                "For a line with no action, use a [divider](components/divider).",
            ],
            Related = ["divider", "button", "sortable"],
            Sections =
            [
                new("between-items", "Between items", "`OnInsert` runs when the divider's button is pressed, and the "
                    + "app adds the item. The button shows when the pointer rests on the line or the button has "
                    + "keyboard focus, and stays in view on a touch screen, which can't hover. It takes up its space "
                    + "whether it shows or not, so the list doesn't jump. A disabled divider's button shows dimmed. "
                    + "`AlwaysVisible` shows the line and the button at rest, as on the first divider here.",
                    typeof(InsertDividerExample)),
            ],
            Api = [typeof(TbInsertDivider)],
            Keyboard =
            [
                new(["Tab"], "Reaches each divider's button."),
                new(["Enter", "Space"], "Presses the focused button."),
            ],
            Labeling =
            [
                "The insert button is a real button in the tab order, named by the required `Label`.",
            ],
            AppDuties =
            [
                "Say in `Label` where the new item goes, such as \"Add a step after Pack the tent\".",
            ],
            Accessibility =
            [
                "The button stays in the tab order while it's hidden, and shows when it has keyboard focus.",
            ],
            Modes = new(ModeSupport.DisplayOnly, ModeSupport.DisplayOnly),
            BeforeInteractive =
            [
                "The lines render. `OnInsert` starts working once the page is interactive.",
            ],
            Target = new("button", "Add a step after Pack the tent"),
            Blocks = ["tb-insert-divider"],
            DataParts =
            [
                new("insert", "The button that inserts content at the divider."),
            ],
            TestingTip = "Click the insert button by its name, as `GetByRole(AriaRole.Button, new() { Name = "
                + "\"Add a step after Pack the tent\" })`; Playwright hovers it first, which shows it.",
        },
        new("stack", "Stack", ExampleCategory.Layout,
            "Lays its children out in a row or a column, evenly spaced.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it to put items in one row or one column with even gaps.",
            ],
            WhenNotToUse =
            [
                "For columns that line up across rows, or a page layout, use a [grid](components/grid).",
            ],
            Related = ["grid", "spacer", "surface", "container"],
            Sections =
            [
                new("column", "Column", "With no parameters, a stack lays its children out in a column.",
                    typeof(StackExample)),
                new("gap", "Gap", "`Gap` sets the space between items, from the [spacing scale](guides/spacing).",
                    typeof(StackGapExample)),
                new("row", "Row", "`Row` lays the items in a row. `Align` lines them up across the row, `Justify` "
                    + "spreads them along it, and `Wrap` lets them move onto a new line when they don't fit.",
                    typeof(StackRowExample)),
                new("responsive", "Column to row", "`RowFrom` keeps the items in a column on narrow screens and lays "
                    + "them in a row from a breakpoint. The reading order stays the same either way.",
                    typeof(StackResponsiveExample)),
                new("element", "Element", "`As` renders the stack as another element, such as `LayoutElement.Nav` for "
                    + "a group of links or `LayoutElement.Ul` for a list, whose items are `li` elements.",
                    typeof(StackElementExample)),
            ],
            Api = [typeof(TbStack)],
            Accessibility =
            [
                "A plain `div` by default; it only lays out what it holds.",
                "A stack rendered as a list keeps `role=\"list\"`, since Safari stops reading a list without bullets as "
                    + "one.",
            ],
            AppDuties =
            [
                "Screen readers and the keyboard follow the order in the markup, so keep it the same as the order on "
                    + "screen.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Full),
            BeforeInteractive =
            [
                PlainHtml,
            ],
            Target = null,
            Blocks = ["tb-stack"],
        },
        new("spacer", "Spacer", ExampleCategory.Layout,
            "Takes up the free space in a stack, pushing what follows to the end.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it to push some items in a [stack](components/stack) to the far end and leave others where they "
                    + "are.",
            ],
            WhenNotToUse =
            [
                "To spread every item evenly, use the stack's `Justify` instead.",
            ],
            Related = ["stack", "toolbar"],
            Sections =
            [
                new("push-to-end", "Pushing to the end", "In a row, what follows a spacer moves to the far end.",
                    typeof(SpacerExample)),
                new("between", "Between items", "Several spacers share the free space evenly, so one item can sit in "
                    + "the middle.", typeof(SpacerBetweenExample)),
                new("column", "In a column", "In a column with a set height, what follows a spacer moves to the bottom.",
                    typeof(SpacerColumnExample)),
            ],
            Api = [typeof(TbSpacer)],
            Accessibility =
            [
                "A plain `div` with no role and no content; screen readers skip it.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Full),
            BeforeInteractive =
            [
                PlainHtml,
            ],
            Target = null,
            Blocks = ["tb-spacer"],
        },
        new("grid", "Grid", ExampleCategory.Layout,
            "A responsive 12-column grid for page layouts.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for page layouts and cards whose columns line up across rows and change with the screen's "
                    + "width.",
            ],
            WhenNotToUse =
            [
                "For items in one row or column with even gaps, use a [stack](components/stack).",
            ],
            Related = ["stack", "container", "surface"],
            Sections =
            [
                new("responsive-spans", "Responsive spans", "An item spans all 12 columns unless told otherwise. `Span` "
                    + "applies from the smallest screens up. These take over on wider ones: `SpanSm` from "
                    + "640px, `SpanMd` from 768px, `SpanLg` from 1024px and `SpanXl` from 1280px.",
                    typeof(GridExample)),
                new("offsets", "Offsets", "`Offset` leaves columns empty before an item, and `OffsetSm`, `OffsetMd`, "
                    + "`OffsetLg` and `OffsetXl` take over from 640px, 768px, 1024px and 1280px. An offset doesn't "
                    + "change the reading order.",
                    typeof(GridOffsetExample)),
                new("auto-and-grow", "Sizing to content", "`GridSpan.Auto` sizes an item to its content, and "
                    + "`GridSpan.Grow` takes what's left of its row. An item that doesn't fit starts the next row.",
                    typeof(GridAutoExample)),
                new("nested", "Nested grids", "A grid inside an item has its own 12 columns and its own `Gap`, the "
                    + "space between its items.", typeof(GridNestedExample)),
                new("list", "List of cards", "Render the grid `As` `LayoutElement.Ul` and its items as "
                    + "`LayoutElement.Li` for a list of cards, which screen readers count.", typeof(GridListExample)),
            ],
            Api = [typeof(TbGrid), typeof(TbGridItem)],
            Anatomy = """
                <TbGrid>
                    <TbGridItem SpanMd="6">…</TbGridItem>
                    <TbGridItem SpanMd="6">…</TbGridItem>
                </TbGrid>
                """,
            Accessibility =
            [
                "Plain `div`s by default; they only lay out what they hold.",
            ],
            AppDuties =
            [
                "Screen readers and the keyboard follow the order in the markup, so keep it the same as the order on "
                    + "screen.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Full),
            BeforeInteractive =
            [
                PlainHtml,
            ],
            Target = null,
            Blocks = ["tb-grid", "tb-grid-item"],
        },
        new("splitter", "Splitter", ExampleCategory.Layout,
            "Lays out panes side by side or stacked, with separators that resize them by dragging, with the "
                + "keyboard or from a menu.")
        {
            RightToLeftKeys = "Left and Right move the separator the way they point, so Left grows the start pane, "
                + "now on the right.",
            FullWidth = true,
            WhenToUse =
            [
                "Use it when people need to choose how much room each part of a work area gets.",
            ],
            WhenNotToUse =
            [
                "For a fixed page layout, use a [grid](components/grid).",
                "To show one part at a time, use [tabs](components/tabs).",
            ],
            Related = ["grid", "tabs", "accordion"],
            Pattern = new("window splitter", "https://www.w3.org/WAI/ARIA/apg/patterns/windowsplitter/"),
            Sections =
            [
                new("panes", "Sizes, limits and collapsing", "A pane's `Size` is a percentage, kept between `MinSize` "
                    + "and `MaxSize`. A pane with no `Size` shares what's left. `Collapsible` lets a pane close, and "
                    + "`@bind-Collapsed` closes it from code. A splitter can sit inside a pane. Drag a separator or "
                    + "its grip to resize, in right-to-left layouts too.", typeof(SplitterExample)),
                new("vertical", "Vertical", "`Vertical` stacks the panes. Its parent needs a set height.",
                    typeof(SplitterVerticalExample)),
                new("persistence", "Remembering the layout", "`PersistenceKey` saves the panes' sizes and collapsed "
                    + "state in the browser under that key, so the layout comes back. Use a key unique to "
                    + "each splitter.",
                    typeof(SplitterPersistExample)),
            ],
            Api = [typeof(TbSplitter), typeof(TbSplitterPane)],
            Keyboard =
            [
                new(["Left", "Right"], "Resizes by 1%.") { When = "On a separator, side by side" },
                new(["Shift+Left", "Shift+Right"], "Resizes by 10%.") { When = "On a separator, side by side" },
                new(["Up", "Down"], "Resizes by 1%.") { When = "On a separator, when `Vertical`" },
                new(["Shift+Up", "Shift+Down"], "Resizes by 10%.") { When = "On a separator, when `Vertical`" },
                new(["Home", "End"], "Takes the pane before it to its smallest or largest.") { When = "On a "
                    + "separator" },
                new(["Enter"], "Collapses or restores a `Collapsible` pane.") { When = "On a separator" },
            ],
            Labeling =
            [
                "Each separator is a focusable `separator` named \"Resize\" followed by the `Label` of the pane before "
                    + "it.",
                "The grip in the middle of each separator is a menu `button` named \"Size of\" followed by the "
                    + "`Label` of the pane before it.",
            ],
            Announcements =
            [
                "On each separator, `aria-valuenow` gives the size of the pane before it in percent, and "
                    + "`aria-controls` points to that pane.",
            ],
            Accessibility =
            [
                "A collapsed pane keeps its content but hides it from sight and from the tab order. If focus is inside "
                    + "the pane when it collapses, focus moves to the separator after it, or before it for the last "
                    + "pane.",
                "A click or tap on the grip opens its menu: Make larger and Make smaller (by 10%), plus Collapse or "
                    + "Restore for a `Collapsible` pane. The menu is the single-pointer alternative to dragging (WCAG "
                    + "2.5.7).",
                "The grip stays out of the tab order, since the separator's keys do the same as its menu.",
            ],
            Modes = new(ModeSupport.DisplayOnly, ModeSupport.DisplayOnly),
            BeforeInteractive =
            [
                "The panes render at their sizes. Resizing starts working once the page is interactive.",
            ],
            Ready = new("[data-part=splitter]", "the splitter"),
            Target = new("separator", "Resize Trails"),
            Blocks = ["tb-splitter"],
            DataParts =
            [
                new("splitter", "The splitter itself, around its panes."),
                new("pane", "One pane, sized by the splitter."),
                new("bar", "The bar between two panes."),
                new("separator", "The bar's focusable separator, dragged or moved with the keyboard to resize."),
                new("grip",
                    "The bar's handle menu: its button drags the bar or opens options that resize the panes on "
                        + "either side."),
            ],
            Anatomy = """
                <TbSplitter>
                    <TbSplitterPane Label="…" @bind-Size="_size">…</TbSplitterPane>
                    <TbSplitterPane Label="…">…</TbSplitterPane>
                </TbSplitter>
                """,
            TestingTip = "Resize with the keys, as `var separator = page.GetByRole(AriaRole.Separator, new() { Name "
                + "= \"Resize Trails\" }); await separator.FocusAsync(); await page.Keyboard.PressAsync(\"End\");`, "
                + "then check `ToHaveAttributeAsync(\"aria-valuenow\", \"40\")`.",
        },
        new("container", "Container", ExampleCategory.Layout,
            "Centers content and caps its width at a readable size.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it around a page's content, so long lines don't stretch across a wide screen.",
            ],
            WhenNotToUse =
            [
                "To divide the width into columns, use a [grid](components/grid) inside it.",
            ],
            Related = ["grid", "stack", "surface"],
            Sections =
            [
                new("max-width", "Maximum width", "`MaxWidth` sets where the container stops growing, from `Small` to "
                    + "`ExtraLarge`, and `Full` never stops. The container stays centered in its parent. The frame here "
                    + "is narrower than `Large`, so open the demo on its own in a wide window to see the larger ones.",
                    typeof(ContainerExample)),
                new("element", "Element", "`As` renders the container as another element, such as "
                    + "`LayoutElement.Section` with an `aria-label`. `LayoutElement.Main` makes it the page's main "
                    + "landmark, and a page should have only one.", typeof(ContainerElementExample)),
            ],
            Api = [typeof(TbContainer)],
            Accessibility =
            [
                "A plain `div` by default; it only limits the width of what it holds.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Full),
            BeforeInteractive =
            [
                PlainHtml,
            ],
            Target = null,
            Blocks = ["tb-container"],
        },
        new("app-shell", "App shell", ExampleCategory.Layout,
            "An app's frame: an app bar, navigation beside the page that narrows to a rail or opens in a drawer on a "
                + "narrow window, and the page itself.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it once, in the app's layout, around the `@Body` of every page.",
            ],
            WhenNotToUse =
            [
                "For a panel that opens over the page, use a [drawer](components/drawer).",
                "For the sections of one page, use [tabs](components/tabs) or a "
                    + "[table of contents](components/table-of-contents).",
            ],
            Related = ["drawer", "breadcrumbs", "container"],
            Pattern = new("disclosure navigation menu",
                "https://www.w3.org/WAI/ARIA/apg/patterns/disclosure/examples/disclosure-navigation/"),
            Sections =
            [
                new("basic", "App shell", "`Header` holds a `TbAppBar` across the top, `Navigation` a `TbNavMenu` "
                    + "beside the page, and the page goes in the `main` landmark, with a link that skips to it. The "
                    + "app bar's menu button narrows the navigation to a rail of icons, bound with "
                    + "`@bind-NavigationCollapsed`. On a window narrower than 48em, the navigation leaves the page and "
                    + "the button opens it in a drawer instead, which closes when people follow a link. Here the shell "
                    + "sits inside this page, so its landmarks are inside the page's own; in an app it is the frame.",
                    typeof(AppShellExample)),
                new("nav-menu", "Nav menu", "Each `TbNavLink` is marked as the current page when the address matches "
                    + "it, the same way Blazor's `NavLink` matches: under its address by default, or exactly with "
                    + "`Match=\"NavLinkMatch.All\"`, as the home page needs. `End` holds a badge or a count. A "
                    + "`TbNavGroup` opens and closes its links, and opens by itself when it holds the current page. "
                    + "On its own, put the menu in a `nav` with an `aria-label`.", typeof(AppShellNavMenuExample)),
            ],
            Api = [typeof(TbAppShell), typeof(TbAppBar), typeof(TbNavMenu), typeof(TbNavLink), typeof(TbNavGroup)],
            Anatomy = """
                <TbAppShell @bind-NavigationCollapsed="_collapsed">
                    <Header>
                        <TbAppBar>
                            <Start>…</Start>
                            …
                            <End>…</End>
                        </TbAppBar>
                    </Header>
                    <Navigation>
                        <TbNavMenu>
                            <TbNavLink Href="…" Icon="…">…</TbNavLink>
                            <TbNavGroup Text="…">
                                <TbNavLink Href="…">…</TbNavLink>
                            </TbNavGroup>
                        </TbNavMenu>
                    </Navigation>
                    <ChildContent>@Body</ChildContent>
                </TbAppShell>
                """,
            Keyboard =
            [
                new(["Tab"], "The first Tab on a page reaches the skip link, and Enter on it moves focus to the page's "
                    + "content."),
                new(["Tab"], "Moves between the links and group buttons, each its own tab stop."),
                new(["Enter"], "Follows the focused link."),
                new(["Enter", "Space"], "Opens or closes the focused group."),
                new(["Escape"], "Closes the navigation drawer.") { When = "On a narrow window" },
            ],
            Labeling =
            [
                "The app bar is the page's `banner` landmark, the navigation a `navigation` landmark named by "
                    + "`NavigationLabel` (\"Main\" by default, heard as \"Main navigation\"), and the page the `main` "
                    + "landmark.",
                "The menu button is named by `MenuLabel` (\"Navigation\" by default), which also titles the drawer.",
                "Each group's button is named by its `Text`.",
            ],
            Announcements =
            [
                "The link to the current page has `aria-current=\"page\"`. Group buttons and the menu button say "
                    + "whether they're open with `aria-expanded`.",
            ],
            Accessibility =
            [
                "On a narrow window the navigation opens in a modal drawer, so focus moves into it, stays there, and "
                    + "goes back to the menu button as it closes.",
                "In forced-colors mode, such as Windows high contrast, the current link takes the system's highlight.",
            ],
            AppDuties =
            [
                "Put the app's name or the page's title in the app bar, and give every page's content a heading.",
                "Use `Match=\"NavLinkMatch.All\"` on the link to the home page, which every address starts with.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "The links already work, and the skip link goes to the content. The menu button, the groups and the "
                    + "drawer start working once the page is interactive; a group holding the current page is already "
                    + "open.",
            ],
            Target = new("navigation", "Main"),
            Blocks = ["tb-app-shell", "tb-app-bar", "tb-nav-menu"],
            DataParts =
            [
                new("app-shell", "The shell around the whole app."),
                new("skip-link", "The link that skips to the page's content."),
                new("navigation", "The navigation beside the page."),
                new("main", "The page's content."),
                new("footer", "The content after the page."),
                new("app-bar", "The bar across the top."),
                new("menu-button", "The button that opens the navigation drawer on a narrow window."),
                new("collapse-button", "The button that narrows the navigation to a rail on a wide window."),
                new("start", "The content at the start of the app bar."),
                new("title", "The app bar's title."),
                new("end", "The content at the end of the app bar, or after a link's text."),
                new("nav-menu", "The menu's list of links."),
                new("nav-item", "One link's list item."),
                new("link", "A link to a page."),
                new("icon", "A link's or group's icon."),
                new("label", "A link's or group's text."),
                new("nav-group", "A group of links."),
                new("group-toggle", "The button that opens and closes a group."),
                new("group-items", "A group's links."),
            ],
            TestingTips =
            [
                "Find the navigation by its landmark, with "
                    + "`GetByRole(AriaRole.Navigation, new() { Name = \"Main\" })`, and the current page's link with "
                    + "`Locator(\"[aria-current=page]\")`.",
                "Playwright's window is wide unless you set `ViewportSize`; set it narrower than 48em to test the "
                    + "drawer. On a narrow window, open it with "
                    + "`GetByRole(AriaRole.Button, new() { Name = \"Navigation\" })`.",
                "In bUnit, navigate with the `NavigationManager` to make a link current; both menu buttons render, "
                    + "since only the stylesheet chooses between them.",
            ],
            Telemetry =
            [
                "With a `TelemetryName` on the `TbNavMenu`, such as `\"main-menu\"`, it counts "
                    + "`trailblazor.control.shown` as it first renders and `trailblazor.control.used` for each link "
                    + "followed. Which link was followed is never recorded.",
                "In the shell, the menu beside the page counts as shown once, even on a narrow window that hides it, "
                    + "and the drawer's copy counts again each time the drawer opens.",
            ],
        },
        new("alert", "Alert", ExampleCategory.Feedback,
            "A message in the page, such as a warning or an error, that can be closed.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for a message that belongs to part of the page and should stay in view, such as an error that "
                    + "needs fixing.",
            ],
            WhenNotToUse =
            [
                "For a brief update that needs no action, use a [toast](components/toast).",
                "For a decision people must make before going on, use a [dialog](components/dialog).",
                "For a problem with one field, use that field's `Error`.",
            ],
            Related = ["toast", "dialog", "badge"],
            Sections =
            [
                new("tones", "Tones", "`Tone` says what kind of message an alert carries. Success, Warning, Danger and "
                    + "Info each have their own icon shape, so those tones don't rest on color "
                    + "alone.", typeof(AlertExample)),
                new("variants", "Variants", "`Variant` sets how much the alert stands out: soft by default, `Filled` "
                    + "or `Outlined`. In a `Filled` alert, buttons and the focus ring take the alert's text color, so "
                    + "they stay readable on the fill.", typeof(AlertVariantExample)),
                new("title", "Title", "`Title` adds a bold first line to an alert.", typeof(AlertTitleExample)),
                new("icon", "Icon", "`Icon` replaces an alert's tone icon, and `ShowIcon=\"false\"` removes it.",
                    typeof(AlertIconExample)),
                new("actions", "Actions", "`Actions` holds an alert's buttons, which sit after the message so screen "
                    + "readers reach them after hearing it.", typeof(AlertActionsExample)),
                new("closing", "Closing", "`OnClose` adds a close button and runs when it's pressed. The alert doesn't "
                    + "remove itself, so your handler has to. `CloseLabel` names the button, \"Close\" unless "
                    + "set.", typeof(AlertCloseExample)),
                new("density", "Density", "`data-tb-density=\"compact\"` tightens an alert for small spaces.",
                    typeof(AlertDensityExample)),
            ],
            Api = [typeof(TbAlert), typeof(AlertClasses)],
            Keyboard =
            [
                new(["Tab"], "Reaches the buttons in `Actions`, then the close button."),
                new(["Enter", "Space"], "Presses the focused button; the close button runs `OnClose`."),
            ],
            Labeling =
            [
                "The icon is decorative, so the `Title` and message must carry the meaning.",
            ],
            Announcements =
            [
                "Danger and Warning alerts have `role=\"alert\"`, which screen readers read out at once. The other "
                    + "tones have `role=\"status\"`, read out when the reader is idle.",
            ],
            Accessibility =
            [
                "Each alert keeps its role however it looks, so `Variant` doesn't change what's announced.",
            ],
            AppDuties =
            [
                "Screen readers may not read a status alert that is added to the page with its message already in it. "
                    + "Add it empty and then set the message, or use `IAnnouncer`.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "It renders as it will look. `OnClose` starts working once the page is interactive.",
            ],
            Target = new("alert") { Text = "Danger alert." },
            Blocks = ["tb-alert"],
            DataParts =
            [
                new("icon", "The icon that shows the alert's tone."),
                new("title", "The alert's title."),
                new("message", "The alert's text."),
                new("actions", "The buttons the alert offers."),
                new("close", "The button that dismisses the alert."),
            ],
            TestingTip = "A danger or warning alert has the `alert` role, and the other tones have `status`, so find "
                + "one by its role and text: `GetByRole(AriaRole.Alert).Filter(new() { HasText = \"Danger alert.\" })` "
                + "in Playwright, or `[role=alert]` in bUnit. The close button is `[data-part=close]`.",
        },
        new("banner", "Banner", ExampleCategory.Feedback,
            "A strip across the top of the page or app with a notice for everyone using it, such as planned "
                + "maintenance.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for a notice about the whole app or page that stays until people dismiss it or it no longer "
                    + "applies, such as maintenance, being offline or a new feature.",
            ],
            WhenNotToUse =
            [
                "For a message about one part of the page, such as a form, use an [alert](components/alert).",
                "For a brief confirmation of what someone just did, use a [toast](components/toast).",
            ],
            Related = ["alert", "toast", "app-shell"],
            Sections =
            [
                new("basic", "Banner", "`Title` leads in bold and names the banner for screen readers, and the "
                    + "message follows. `Actions` holds links or buttons, and `OnClose` shows a close button; hide the "
                    + "banner when it's pressed.", typeof(BannerExample)),
                new("tones", "Tones", "`Tone` sets the color and the icon.", typeof(BannerTonesExample)),
                new("plain", "Without a title or icon", "Without a `Title`, `AriaLabel` names the banner for screen "
                    + "readers; with neither, it's named \"Notice\". `ShowIcon=\"false\"` hides the icon.",
                    typeof(BannerPlainExample)),
            ],
            Api = [typeof(TbBanner)],
            Labeling =
            [
                "The banner is a `region` landmark named by its `Title`, or by `AriaLabel`, or \"Notice\" without "
                    + "either, so screen reader users can find it from the page's landmarks.",
                "The close button is named \"Dismiss\", or by `CloseLabel`.",
            ],
            Accessibility =
            [
                "The tone's icon and the colored edge carry the meaning along with the color, and the text keeps "
                    + "4.5:1 contrast with the tinted background.",
                "It handles no keys of its own; its actions and close button are ordinary buttons and links.",
            ],
            AppDuties =
            [
                "A banner that appears after the page loads, such as \"You're offline\", isn't read out on its own: "
                    + "announce it with the [announcer](guides/announcements).",
                "After the close button hides the banner, move focus somewhere useful, such as the page's main "
                    + "heading, since the button that had it is gone.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "The banner and its links render as plain HTML; the close button works once the page is interactive.",
            ],
            Target = new("region", "Maintenance tonight"),
            Blocks = ["tb-banner"],
            DataParts =
            [
                new("icon", "The tone's icon."),
                new("message", "The title and the message."),
                new("title", "The bold lead before the message."),
                new("actions", "The links or buttons after the message."),
                new("close", "The close button."),
            ],
            TestingTip = "Find it as `GetByRole(AriaRole.Region, new() { Name = \"Maintenance tonight\" })`, and its "
                + "close button as `GetByRole(AriaRole.Button, new() { Name = \"Dismiss\" })`.",
        },
        new("chip", "Chip", ExampleCategory.Content,
            "A compact label for a tag, a filter or a choice, which can be clicked, toggled or removed.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for tags and filters that people add, remove or toggle.",
            ],
            WhenNotToUse =
            [
                "For an action unrelated to a tag or filter, use a [button](components/button).",
                "For a count or status on something else, use a [badge](components/badge).",
                "To pick from a fixed set in a form, use a [checkbox group](components/checkbox-group).",
            ],
            Related = ["badge", "button", "checkbox-group", "toggle-group"],
            Sections =
            [
                new("removable", "Removable", "`OnClose` adds a remove button. When a chip in a `TbChipGroup` is "
                    + "removed, focus moves to the chip that takes its place, or to the group when none is "
                    + "left.", typeof(ChipExample)),
                new("filter", "Filter", "`@bind-Selected` makes the chip a toggle, and a selected one shows a check.",
                    typeof(ChipFilterExample)),
                new("style", "Tone, variant, size and icon", "`Tone`, `Variant` and `Size` set how the chip looks, and "
                    + "`Icon` adds an icon before its text. `IconTone` gives the icon a tone of its own, such as a red "
                    + "bell on a neutral chip.", typeof(ChipStyleExample)),
                new("actions", "Links and buttons", "`Href` makes the chip a link and `OnClick` a button, and either "
                    + "can also have `OnClose`.", typeof(ChipActionExample)),
                new("disabled", "Disabled", "`Disabled` turns off the chip and its remove button.",
                    typeof(ChipDisabledExample)),
                new("remove-label", "Naming the remove button", "`RemoveLabel` replaces the remove button's name, "
                    + "which is \"Remove\" followed by the chip's `Text` unless set. `{0}` stands for the chip's "
                    + "`Text`.", typeof(ChipRemoveLabelExample)),
            ],
            Api = [typeof(TbChip), typeof(TbChipGroup)],
            Anatomy = """
                <TbChipGroup AriaLabel="…">
                    <TbChip Text="…" OnClose="…" />
                    <TbChip Text="…" OnClose="…" />
                </TbChipGroup>

                <TbChipGroup AriaLabel="…">
                    <TbChip Text="…" @bind-Selected="_on" />
                    <TbChip Text="…" @bind-Selected="_off" />
                </TbChipGroup>
                """,
            Keyboard =
            [
                new(["Tab"], "Reaches each chip that is a button, link or toggle, and each remove button."),
                new(["Enter", "Space"], "Presses the focused chip or remove button. A link takes Enter only."),
                new(["Delete", "Backspace"], "Removes the focused chip, if it has `OnClose`."),
            ],
            Labeling =
            [
                "A `TbChipGroup` is a `list` named by its required `AriaLabel`, and each chip in it is a `listitem`.",
                "A chip with `OnClick` is a `button`, with `Href` a `link`, and with `@bind-Selected` a toggle "
                    + "button. Other chips are plain text.",
                "A remove button is named by `RemoveLabel`. Unless set, its name is \"Remove\" followed by the chip's "
                    + "`Text`.",
            ],
            Announcements =
            [
                "A toggle's state is announced through `aria-pressed`.",
            ],
            Accessibility =
            [
                "In forced-colors mode, such as Windows high contrast, every chip keeps an outline and a selected "
                    + "one is outlined in the system's highlight color.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "Chips render as they will look once interactive, and a link already works. Selecting and removing "
                    + "start working once the "
                    + "page is interactive.",
            ],
            Target = new("list", "Selected tags"),
            Blocks = ["tb-chip", "tb-chip-group"],
            DataParts =
            [
                new("check", "The check a selected filter chip shows."),
                new("icon", "The chip's icon, before its label."),
                new("label", "The chip's text."),
                new("action",
                    "The chip's body, around its icon and label: a button or link when it acts, otherwise text."),
                new("remove", "The button that removes the chip."),
            ],
            TestingTip = "Remove a chip with `GetByRole(AriaRole.Button, new() { Name = \"Remove Archive\" })`, and "
                + "check a filter chip with `ToHaveAttributeAsync(\"aria-pressed\", \"true\")`.",
            Telemetry =
            [
                "With a `TelemetryName`, such as `\"filter-open\"`, it counts `trailblazor.control.shown` as it first "
                    + "renders, `trailblazor.control.used` for each click or toggle, and `trailblazor.control.used` "
                    + "with `trailblazor.action` `remove` when it is removed. Leave the `TelemetryName` off chips that "
                    + "only show something.",
            ],
        },
        new("list", "List", ExampleCategory.Content,
            "A list of items, each with optional leading content, a description and trailing content. Items can be "
                + "links or buttons, and one can be marked as the current item.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for a short set of items that each show a little and may do something, such as open a page.",
            ],
            WhenNotToUse =
            [
                "For rows that share columns, use a [table](components/table) or a "
                    + "[data grid](components/data-grid).",
                "To pick options in a form, use a [list box](components/listbox).",
                "For items nested inside each other, use a [tree view](components/tree-view).",
            ],
            Related = ["table", "data-grid", "listbox", "tree-view"],
            Sections =
            [
                new("buttons", "Buttons", "An item with `OnClick` is a button. `Selected` marks the current "
                    + "one, `Disabled` turns one off, and `Description` adds a second line. `End` holds what sits after "
                    + "the text, and a button there works on its own.", typeof(ListExample)),
                new("links", "Links", "An item with `Href` is a link. On a link, `Selected` marks the page you're on.",
                    typeof(ListLinksExample)),
                new("start", "Leading content", "`Start` holds what comes before the text, such as an icon or an "
                    + "avatar.", typeof(ListStartExample)),
                new("ordered", "Ordered", "`Ordered` renders an `<ol>`, for steps whose order matters, so screen "
                    + "readers say each item's place. It draws no numbers, so put them in `Start` if people need to see "
                    + "them.", typeof(ListOrderedExample)),
                new("dividers", "Dividers and density", "`Dividers` adds a line between items. `data-tb-density` on "
                    + "the list makes this one compact without changing the rest of the page.",
                    typeof(ListDividersExample)),
                new("empty", "Empty", "A list has no message of its own, so show your own in its place until it has "
                    + "items.", typeof(ListEmptyExample)),
                new("long-content", "Long content", "Long text wraps inside the item, and `End` stays at the side.",
                    typeof(ListLongExample)),
            ],
            Api = [typeof(TbList), typeof(TbListItem)],
            Anatomy = """
                <TbList aria-label="…">
                    <TbListItem OnClick="…">
                        <Start>…</Start>
                        <ChildContent>…</ChildContent>
                        <Description>…</Description>
                        <End>…</End>
                    </TbListItem>
                </TbList>
                """,
            Keyboard =
            [
                new(["Tab"], "Reaches each link or button item, then any buttons in its `End`."),
                new(["Enter"], "Follows a link or presses a button."),
                new(["Space"], "Presses a button."),
            ],
            Labeling =
            [
                "A `list` of `listitem`s, named by `aria-label` when the page has several lists.",
                "An item with `Href` is a `link` and one with `OnClick` a `button`. It is named by its text and then "
                    + "its `Description`, so start the text with what the item is.",
            ],
            Accessibility =
            [
                "The `End` content sits outside the link or button, so buttons in it are separate tab stops, "
                    + "reached right after their item.",
                "`Selected` sets `aria-current`: `page` on a link and `true` on a button or plain item.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "Items and links render in full. A link already works, and an item with `OnClick` starts working once "
                    + "the page is interactive.",
            ],
            Target = new("list", "Campsites"),
            Blocks = ["tb-list"],
            DataParts =
            [
                new("start", "Content before the item's text, such as an icon or an avatar."),
                new("text", "The item's main text."),
                new("description", "The item's second line, under its text."),
                new("content", "The start content, text and description of an item that doesn't act."),
                new("end", "Content after the item's text, such as a badge or a menu."),
                new("action",
                    "The button or link around the start content, text and description of an item that acts."),
            ],
            TestingTip = "Find an item by the start of its name, as `GetByRole(AriaRole.Button, new() { NameRegex = "
                + "new Regex(\"^Pine Hollow\") })`, and check the current one with "
                + "`ToHaveAttributeAsync(\"aria-current\", \"true\")`.",
        },
        new("sortable", "Sortable", ExampleCategory.Content,
            "A list the user puts in order, by dragging an item's handle or with the arrow keys, such as rules whose "
                + "order sets which runs first.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it when the order of a short list is what people are deciding.",
            ],
            WhenNotToUse =
            [
                "To reorder rows with columns, use a [data grid](components/data-grid) with `OnRowMove`.",
                "To reorder nested items, use a [tree view](components/tree-view) with `OnMove`.",
            ],
            Related = ["data-grid", "tree-view", "list"],
            Sections =
            [
                new("order", "Put items in order", "`OnMove` gets each move and the app applies it. `CanMove` can "
                    + "refuse one and say why, as for the last rule here. `MoveAsync(from, to)` moves an item from the "
                    + "app's own buttons, such as Raise, with the same check and announcement.",
                    typeof(SortableExample)),
                new("content", "Your own content", "`ItemTemplate` draws each item, and long text wraps inside it. "
                    + "`ItemLabel` names the handle when items aren't strings, and `KeySelector` tells items apart as "
                    + "they move.", typeof(SortableContentExample)),
                new("disabled", "Disabled", "`Disabled` turns off the handles and dragging, so people can't change the "
                    + "order. An app's own `MoveAsync` still works.", typeof(SortableDisabledExample)),
                new("empty", "Empty", "A sortable list has no message of its own, so show your own in its place until "
                    + "it has items.", typeof(SortableEmptyExample)),
            ],
            Api = [typeof(TbSortable<>), typeof(SortableMove<>), typeof(MoveCheck)],
            Keyboard =
            [
                new(["Up", "Down"], "Moves the item one place, and focus stays on the handle.") { When = "On a handle" },
                new(["Home", "End"], "Moves the item to the start or end.") { When = "On a handle" },
                new(["Enter", "Space"], "Opens its menu: Move up, Move down, Move to the start and Move to the end.")
                    { When = "On a handle" },
                new(["Escape"], "Cancels a drag.") { When = "While dragging" },
            ],
            Labeling =
            [
                "A `list` named by its `Label`.",
                "Each item's handle is a menu `button` named \"Move\" followed by the item's text, and described by how "
                    + "its keys work.",
            ],
            Announcements =
            [
                "Each move is announced with the item's new position, such as \"Storm warning moved to position 1 of "
                    + "4.\"",
                "A move `CanMove` refuses is announced with its reason, which also shows beside the drop.",
            ],
            Accessibility =
            [
                "A click on a handle opens the same menu as Enter or Space, the single-pointer way that replaces "
                    + "dragging (WCAG 2.5.7). "
                    + "The menu also works where a screen reader keeps the arrow keys for itself.",
                "A drop lands before or after the item under the pointer, shown by a line. Where it can't go, the line "
                    + "is dashed, so it doesn't rely on color.",
                "A drag held near the top or bottom of what scrolls keeps it scrolling, so a drop can reach any item.",
            ],
            Modes = new(ModeSupport.DisplayOnly, ModeSupport.DisplayOnly),
            BeforeInteractive =
            [
                "The items render in their order. Dragging and the handles start working once the page is interactive.",
            ],
            Ready = new(".tb-sortable", "the list's root"),
            Target = new("list", "Trail alerts, first match wins"),
            Blocks = ["tb-sortable"],
            DataParts =
            [
                new("list", "The list that holds the items."),
                new("item", "One item."),
                new("handle", "The item's drag handle, which also moves it with the keyboard or its menu."),
                new("drop-reason", "The reason shown while a dragged item is held over a place it may not go."),
            ],
            TestingTip = "Press keys on a handle, found as "
                + "`GetByRole(AriaRole.Button, new() { Name = \"Move Storm warning\" })`, and check the order the "
                + "app shows. To drag, move the mouse from the handle to the lower half of the item it should "
                + "follow. In bUnit, `MoveAsync` makes a move as the user would.",
            Telemetry =
            [
                "With a `TelemetryName`, such as `\"alert-order\"`, each move the user makes counts "
                    + "`trailblazor.control.used` with the action `move` and the input `pointer` or `keyboard`. Moves "
                    + "made with `MoveAsync` aren't counted, and which item moved is never recorded.",
            ],
        },
        new("badge", "Badge", ExampleCategory.Feedback,
            "A small count or dot on an icon or button, or in a line of text, that draws the eye to something new.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for a small count or status on an icon or button, or in a line of text.",
            ],
            WhenNotToUse =
            [
                "For a tag people add, remove or toggle, use a [chip](components/chip).",
                "For a message that needs reading, use an [alert](components/alert).",
            ],
            Related = ["chip", "alert", "icon-button"],
            Sections =
            [
                new("count", "Count", "Wrap a badge around what it marks, and `Count` shows the number. It takes no "
                    + "focus and never catches clicks, which reach what's under it.", typeof(BadgeExample)),
                new("dot", "Dot", "With no `Count`, a badge is a dot.", typeof(BadgeDotExample)),
                new("max", "Maximum", "`Max` caps the number a badge shows, as in \"99+\". It is 99 unless set.",
                    typeof(BadgeMaxExample)),
                new("visible", "Showing and hiding", "`Visible` hides the badge and leaves what it marks.",
                    typeof(BadgeVisibleExample)),
                new("in-text", "In a line of text", "With nothing inside it, a badge sits in the line of text. A count "
                    + "of 0 hides a badge, and `ShowZero` keeps it, for a count like errors, where zero is good "
                    + "news.", typeof(BadgeTextExample)),
                new("tone", "Tone", "`Tone` colors a badge by meaning, such as `Danger` for something that needs "
                    + "attention.", typeof(BadgeToneExample)),
                new("placement", "Placement and offset", "`Placement` picks the corner a badge sits on. `Overlap` "
                    + "keeps it on the edge of a round shape, and `Offset` moves it in from the corner by a CSS "
                    + "length, such as `4px`.", typeof(BadgePlacementExample)),
            ],
            Api = [typeof(TbBadge)],
            Labeling =
            [
                "A badge on an icon or button is hidden from screen readers unless it has a `Label`.",
                "A badge in a line of text is read in its place. A `Label`, such as \"3 unsaved changes\", is heard "
                    + "instead of the bare number, and a dot with no `Label` isn't read out.",
            ],
            AppDuties =
            [
                "Put what the badge says in the button's own name, such as `AriaLabel=\"Messages, 120 unread\"`, and "
                    + "give a badge on an icon that isn't a button a `Label`.",
            ],
            Accessibility =
            [
                "In forced-colors mode, such as Windows high contrast, the badge keeps an outline and a dot keeps its "
                    + "fill.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Full),
            BeforeInteractive =
            [
                PlainHtml,
            ],
            Target = null,
            Blocks = ["tb-badge"],
            DataParts =
            [
                new("badge", "The badge itself, over or beside what it marks."),
            ],
            TestingTip = "The badge's element has `data-part=\"badge\"`. Its text holds the count, then the `Label` "
                + "when there is one, so check it with `ToContainTextAsync`.",
        },
        new("progress", "Progress", ExampleCategory.Feedback,
            "Shows how far along work is, as a bar or a circle, or that it is still going.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for work that takes more than a moment, so people know it's still going.",
            ],
            WhenNotToUse =
            [
                "For content that's loading into a known shape, use a [skeleton](components/skeleton).",
                "For a button waiting on its own action, use the [button](components/button)'s `Loading`.",
            ],
            Related = ["skeleton", "button", "alert"],
            Sections =
            [
                new("value", "Value or indeterminate", "A progress bar's `Value` shows how far along the work is. "
                    + "Leaving it null makes the bar indeterminate, for work with no known end. When `Min` equals `Max`, "
                    + "as for zero total work, the bar shows the work as finished.", typeof(ProgressExample)),
                new("label", "Label and value text", "`Label` and `ShowValue` add visible text to a progress bar or "
                    + "ring. `Min` and `Max` set the range, and `ValueFormatter` words the value, such as \"3 of 10 "
                    + "files\".",
                    typeof(ProgressLabelExample)),
                new("circular", "Circular", "`Circular` draws the progress as a ring instead of a bar. Show a ring's "
                    + "value at `Size.Large`, where it is big enough to read.", typeof(ProgressCircularExample)),
                new("size", "Size", "`Size` sets a progress bar's thickness or a ring's diameter: `Small`, `Medium` or "
                    + "`Large`.", typeof(ProgressSizeExample)),
                new("tone", "Tone", "`Tone` colors the filled part of a progress bar or ring by "
                    + "meaning.", typeof(ProgressToneExample)),
                new("failed", "A failed state", "Progress has no failed state of its own. Set a `Danger` `Tone` and "
                    + "say what went wrong beside it, as an alert does here.", typeof(ProgressFailedExample)),
            ],
            Api = [typeof(TbProgress)],
            Labeling =
            [
                "A `Label` names it through `aria-labelledby`. Without one, give an `AriaLabel`. Also give one when "
                    + "the label alone doesn't say what is in progress, such as a file name; the `AriaLabel` then "
                    + "names it instead. With neither, it throws when it renders.",
            ],
            Announcements =
            [
                "A `progressbar` with its range and current value, and no current value when indeterminate.",
                "With `ValueFormatter`, screen readers announce that text, such as \"3 of 10 files\", in place of the "
                    + "number.",
            ],
            Accessibility =
            [
                "With reduced motion, the moving bar and the spinning ring pulse instead.",
                "In forced-colors mode, such as Windows high contrast, the fill takes the system's highlight color.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Full),
            BeforeInteractive =
            [
                PlainHtml,
            ],
            Target = new("progressbar", "Upload"),
            Blocks = ["tb-progress"],
            DataParts =
            [
                new("bar", "A linear progress bar's track."),
                new("ring", "A circular progress's ring."),
                new("indicator", "The filled part that shows how far along it is."),
                new("value", "The value shown as text, such as \"40%\"."),
                new("label", "The progress's visible label."),
            ],
            TestingTip = "Find it with `GetByRole(AriaRole.Progressbar, new() { Name = \"Upload\" })` and check "
                + "`aria-valuenow`, or `aria-valuetext` when it has a `ValueFormatter`. The shown text is in "
                + "`[data-part=label]` and `[data-part=value]`.",
        },
        new("label", "Label", ExampleCategory.Inputs,
            "A label for a control or a group of controls, with an optional info button that explains the setting.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it to name a control or a group that has no label of its own, or to add an info button for help "
                    + "most people don't need every time.",
            ],
            WhenNotToUse =
            [
                "For a Trailblazor field, set the field's own `Label`, with `Info` for an info button. The examples "
                    + "here pair `TbLabel` with a text field only to keep them short.",
                "For a hint everyone should read, use the field's `HelperText`, which stays visible.",
                "For a short description of a button, use a [tooltip](components/tooltip).",
            ],
            Related = ["text-field", "radio-group", "popover", "tooltip"],
            Sections =
            [
                new("for-a-control", "For a control", "`For` takes the control's id and makes this a `<label>` tied to "
                    + "it.", typeof(LabelExample)),
                new("info-button", "Info button", "`Info` adds a button that explains the setting, and keeps the line "
                    + "breaks in its text. `InfoContent` takes markup instead, such as a link. The panel opens on a "
                    + "click, or when the pointer rests on the button.", typeof(LabelInfoExample)),
                new("for-a-group", "For a group", "Without `For` the label is plain text. `TextId` puts an id on that "
                    + "text alone, without the info button, so a group can point `aria-labelledby` at it.",
                    typeof(LabelGroupExample)),
                new("required", "Required", "`Required` shows a marker and nothing else, so set `Required` on the "
                    + "control too. With no `Label` of its own, the field's default message is just \"The field is "
                    + "required.\", so set `RequiredMessage`.", typeof(LabelRequiredExample)),
                new("rich-content", "Rich content", "`ChildContent` shows in place of `Text`. With an info button, set "
                    + "`Text` as well, because it names the button.", typeof(LabelContentExample)),
                new("info-button-name", "Info button name", "`InfoButtonLabel` replaces the button's name, with `{0}` "
                    + "for `Text`.", typeof(LabelButtonNameExample)),
                new("long-text", "Long text", "A long label wraps, and its info button stays beside it.",
                    typeof(LabelLongExample)),
            ],
            Api = [typeof(TbLabel)],
            Keyboard =
            [
                new(["Enter", "Space"], "Opens the info panel and moves focus into it.") { When = "On the info button" },
                new(["Escape"], "Closes the info panel, and returns focus to the button if focus was in the panel.")
                    { When = "While the panel is open" },
                new(["Tab"], "Closes the info panel as focus leaves it.") { When = "While the panel is open" },
            ],
            Labeling =
            [
                "With `For` it is a `<label>` tied to that control. Without `For` it is plain text, and a group of "
                    + "controls names itself by pointing `aria-labelledby` at the label's `TextId`.",
                "The info button is named \"Information about\" and the label's `Text`, unless `InfoButtonLabel` "
                    + "replaces that.",
            ],
            Accessibility =
            [
                "The info button is a toggletip, which opens a small panel on click or hover.",
            ],
            Announcements =
            [
                "The asterisk the `Required` marker adds is hidden from screen readers.",
                "Opening the panel with a click or a key moves focus into a dialog named after the button, so screen "
                    + "readers announce it. Opening it by hover takes no focus, so they don't.",
            ],
            AppDuties =
            [
                "Mark the control itself as required: the label's `Required` marker is only visual.",
            ],
            Notes =
            [
                new("An info button with no `Text` throws when it renders, because nothing would name it."),
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "With `For`, the `<label>` names its control before the page is interactive.",
                "The info button already opens its panel on click, Enter or Space, and Escape closes it. Until the page "
                    + "is interactive, hover doesn't open it, focus doesn't move into it, and it shows at the corner of "
                    + "the window rather than beside the button.",
            ],
            Target = new("button", "Information about Project code"),
            Blocks = ["tb-label"],
            DataParts =
            [
                new("text", "The label's text."),
                new("info", "The info popover: its button and the text it shows about the field."),
            ],
            TestingTip = "Open the info with "
                + "`await page.Tb().Popover(\"Information about Project code\").OpenAsync()`, which returns its panel. "
                + "The info button's name contains the label, so find the control by role with an exact name, as "
                + "`GetByRole(AriaRole.Textbox, new() { Name = \"Project code\", Exact = true })`.",
        },
        new("form", "Form", ExampleCategory.Inputs,
            "A form for Trailblazor fields that reports whether it is valid as the fields change and moves focus to "
                + "the first error when a submit fails.")
        {
            WhenToUse =
            [
                "Use it around fields the user fills in and then submits together, so they are checked as one and "
                    + "focus goes to the first error. The [forms and validation guide](guides/forms) covers the "
                    + "whole job, from messages to server errors.",
            ],
            WhenNotToUse =
            [
                "Skip it for a control that acts as soon as it changes, such as a search box or a [switch]"
                    + "(components/switch) that applies a setting.",
            ],
            Related = ["text-field", "number-field", "select", "checkbox", "alert"],
            Pattern = new("Form landmark", "https://www.w3.org/WAI/ARIA/apg/patterns/landmarks/examples/form.html"),
            Sections =
            [
                new("complete-form", "A complete form", "Fields go inside the form, and a submit button checks them "
                    + "all. Sign up stays on, so a submit can show what to fix; `@bind-IsValid` and `@bind-Errors` "
                    + "drive the line under it, and `InputAttributes` lets the browser fill in the name.",
                    typeof(FormExample)),
                new("your-own-checks", "Your own checks", "`Validation` checks one value and returns its messages. To "
                    + "compare two fields, as here when the largest group goes below the smallest, an `Error` works "
                    + "from both values and an `Invalid` marks the other field.", typeof(FormValidationExample)),
                new("checks-that-take-time", "Checks that take time", "`ValidationAsync` checks a value with a slow "
                    + "call, such as asking the server whether a name is taken, once the field's other checks pass. A newer value "
                    + "cancels the check still running, and a submit waits for it.", typeof(FormCheckExample)),
                new("model-and-annotations", "Model and data annotations", "`Model` and `DataAnnotations` check a "
                    + "class's annotations alongside the fields' own checks. Here each field's `Required` covers "
                    + "empty values, and the annotations cover the rest.", typeof(FormModelExample)),
                new("your-own-edit-context", "Your own edit context", "An `EditContext` of your own tells you what "
                    + "has changed, and can clear that after a save. `ResetValidationAsync` hides the messages again "
                    + "after an undo.", typeof(FormEditContextExample)),
                new("starting-over", "Starting over", "A button with `Type=\"ButtonType.Reset\"` takes the fields, and "
                    + "what they're bound to, back to the values the parent last gave them, and hides their messages. "
                    + "`ResetAsync` does the same from code, and `OnReset` lets you reset anything else.",
                    typeof(FormResetExample)),
                new("locked", "Read-only and disabled forms", "`ReadOnly` on the form makes every field in it read "
                    + "only, as each field's own `ReadOnly` would, and `Disabled` turns them all off, along with the "
                    + "form's submit and reset buttons. A field's own setting still applies when the form's is off, "
                    + "and other buttons in the form aren't affected.",
                    typeof(FormLockedExample)),
                new("server-errors", "Server errors", "A field's `Error` shows what the server said, and blocks the "
                    + "next submit until it is cleared, so clear it when the value changes. `Loading` shows the save "
                    + "is running and ignores more clicks.", typeof(FormServerExample)),
                new("error-summary", "Error summary", "For a long form, list the errors above it, and let each one "
                    + "move focus to its field. `OnSubmit` leaves focus to you, so the summary can take it, and the "
                    + "page title starts with \"Error:\" while there are errors.", typeof(FormErrorSummaryExample)),
                new("changes", "Unsaved changes", "`IsDirty` says whether any field differs from the value its parent "
                    + "last gave it, so you can ask before the user leaves the page, and `IsTouched` says whether the "
                    + "user has changed any field at all, even back again. After a save, `MarkAsSavedAsync` makes the "
                    + "saved values the ones the form compares against, and a reset makes it clean and untouched too.",
                    typeof(FormChangesExample)),
                new("static-form-post", "Static form post", "A `FormName` lets the form post from a static page, "
                    + "with its model supplied by `[SupplyParameterFromForm]`. These docs run in the browser with no "
                    + "server, so here the same code submits without a post.", typeof(FormStaticExample)),
            ],
            Api = [typeof(TbForm)],
            Keyboard =
            [
                new(["Enter"], "In a single-line field, submits the form through its submit button."),
            ],
            Labeling =
            [
                "An `aria-label`, as in these examples, makes the form a landmark screen readers can jump to. Without "
                    + "a name it is still a form, but not a landmark. Each field is named by its own label.",
            ],
            Announcements =
            [
                "A failed submit reads out no messages, so a form with several errors doesn't speak them all at once. "
                    + "The user hears the field focus moves to, with its message.",
                "A message that appears as someone edits a field, or an `Error` the parent sets, is read out once.",
                "A reset reads out nothing.",
            ],
            Accessibility =
            [
                "It renders a `form` element, built on `EditForm`.",
                "When a submit fails, every field shows its messages, linked to it with `aria-describedby`, and focus "
                    + "moves to the first field with an error, or into a radio or checkbox group where its "
                    + "`FocusAsync()` would.",
                "With `OnSubmit`, focus stays where it is, so your handler can move it.",
                "A reset leaves focus where it was.",
            ],
            AppDuties =
            [
                "Leave the submit button on, so a submit can show what to fix. [The guide]"
                    + "(guides/forms#leave-the-submit-button-on) explains why.",
                "Word each message so it says how to fix the problem, as [the guide](guides/forms#wording) describes.",
                "Say when a submit succeeds, with an `output` element as in these examples, or the [announcer]"
                    + "(guides/announcements).",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "With a `FormName`, it posts as a static form. The fields check themselves on the server, and the "
                    + "page comes back with their messages. With no script running, focus doesn't move and nothing "
                    + "is read out. A reset button is left to the browser, which puts the inputs back to what the page "
                    + "came with, and `OnReset` doesn't run.",
            ],
            Notes =
            [
                new("`IsValid` doesn't wait for a `ValidationAsync` check, so it can stay true while one runs. It "
                    + "counts a check's answer once it arrives, and a check that threw counts as failed. "
                    + "`ValidateAsync` and a submit wait for every check."),
                new("`@bind-IsValid` checks the fields' `Validation`, and with `DataAnnotations` the model's "
                    + "annotations, without showing any message. When the app calls `AddValidation` and marks the "
                    + "model `[ValidatableType]`, it checks nested objects too, as a submit does."),
                new("A field's `Error`, like a `Required` field left empty, keeps the form from submitting and "
                    + "`IsValid` false. `Invalid` marks a field without a message and doesn't stop a submit."),
                new("A field bound to a `[Required]` text or nullable property is marked required. It shows one "
                    + "required message whether its `Required`, the model's `[Required]` or both apply, and the "
                    + "[guide](guides/forms#model-and-data-annotations) says which message."),
                new("A form with `OnSubmit` isn't counted in `trailblazor.form.submitted`, since it doesn't validate "
                    + "the submit itself."),
                new("Every field has `FocusAsync()`, to move focus from code. A disabled field doesn't take focus."),
                new("A reset leaves a field's `Error` in place, so clear it in `OnReset` if the form is starting over. A "
                    + "message from a `DataAnnotationsValidator` inside the form stays too, unless the reset changes "
                    + "the field's value and the validator passes the value it goes back to. The form's own "
                    + "`DataAnnotations` clears its messages either way. "
                    + "A value the parent set in answer to an edit, such as a trimmed one, is what the field goes back "
                    + "to. Controls that aren't Trailblazor fields are yours to reset in `OnReset`."),
            ],
            Target = new("form", "Trip sign-up"),
            Blocks = ["tb-form"],
            TestingTip = "Submit with the button, then find errors through the fields: "
                + "`ToHaveAttributeAsync(\"aria-invalid\", \"true\")` on the input, and its message with "
                + "`ToHaveAccessibleDescriptionAsync`. A `ValidationAsync` message shows only once its check answers, "
                + "so wait for it: use `WaitForAssertion` in bUnit, or let Playwright's assertions retry.",
            Telemetry =
            [
                "With a `TelemetryName`, such as `\"new-trail\"`, it counts `trailblazor.control.shown` as it first "
                    + "renders, and `trailblazor.form.submitted` with `trailblazor.outcome` `valid` or `invalid` for "
                    + "each submit it validates.",
                "On an invalid submit, each field with a `TelemetryName` and an error counts "
                    + "`trailblazor.field.invalid`, so the fields people trip over stand out. No value is recorded.",
            ],
        },
        new("text-field", "Text field", ExampleCategory.Inputs,
            "A labeled text input with help text and validation messages.")
        {
            WhenToUse =
            [
                "Use it for free text, such as names, notes and searches, or with a `Mask` for a fixed pattern, such as "
                    + "a phone number.",
            ],
            WhenNotToUse =
            [
                "For an amount with steps and limits, use a [number field](components/number-field). Digits that aren't "
                    + "an amount, such as a phone number, belong in a masked text field.",
                "For a choice from a list, use a [select](components/select), or an [autocomplete]"
                    + "(components/autocomplete) when the list is long enough to search.",
                "For a range of dates, use a [date range picker](components/date-range-picker).",
            ],
            Related = ["number-field", "autocomplete", "select", "form", "label"],
            Sections =
            [
                new("try-it", "Try it", "Use the controls to see what each parameter does.",
                    typeof(TextFieldTryExample))
                {
                    Controls = new("<TbTextField Label=\"Try this field\"{controls} @bind-Value=\"_value\" />",
                        ExampleControl.Text(nameof(TbTextField<string>.Placeholder)),
                        ExampleControl.Text(nameof(TbTextField<string>.HelperText)),
                        ExampleControl.Switch(nameof(TbTextField<string>.Disabled)),
                        ExampleControl.Switch(nameof(TbTextField<string>.ReadOnly)),
                        ExampleControl.Switch(nameof(TbTextField<string>.Required)),
                        ExampleControl.Switch(nameof(TbTextField<string>.Clearable))),
                },
                new("label-and-help", "Label and help text", "`Label` names the field, and `HelperText` says what people "
                    + "need before they type. A long label wraps. The value stays `null` until something is typed.",
                    typeof(TextFieldExample)),
                new("info", "Info button", "`Info` adds a button beside the label that explains the field, and "
                    + "`InfoContent` shows richer content, such as a link. Every field takes them. They need a "
                    + "`Label`, which also names the button; the field's own name is still just the label.",
                    typeof(TextFieldInfoExample)),
                new("required", "Required", "`Required` marks the field and, once it is edited or its form validates, "
                    + "shows a message if it is empty or blank. `RequiredMessage` replaces the default, which uses the "
                    + "label: \"Group leader is required.\"",
                    typeof(TextFieldRequiredExample)),
                new("validation-and-server-errors", "Validation and server errors", "`Validation` checks each new value. "
                    + "`Error` shows a message from the parent, such as a server's answer, until the parent clears it, "
                    + "as the handle does here when it changes. When a handle is taken, `SelectTextAsync` selects it, "
                    + "so typing replaces it. To compare two fields, as with the gate codes, compute one field's "
                    + "`Error` from both values and set `Invalid` on the other.", typeof(TextFieldValidationExample)),
                new("types", "Types", "`Type` sets the keyboard people get, and `InputAttributes` puts attributes "
                    + "such as `autocomplete` on the input. Bind the field to a number or date instead of a string, and "
                    + "text that doesn't convert shows an error.",
                    typeof(TextFieldTypesExample)),
                new("search-and-clearing", "Search and clearing", "`UpdateOnInput` updates the value while typing, and "
                    + "`UpdateDelay` waits for a pause, so a search doesn't run on every keystroke. `Clearable` adds a "
                    + "button that empties the field, and `ClearLabel` names that button. The button sets the value to "
                    + "`null`, while deleting the text by hand leaves an empty string.", typeof(TextFieldSearchExample)),
                new("start-and-end", "Start and end content", "`Start` and `End` hold an icon, a prefix or a unit beside "
                    + "the input.", typeof(TextFieldAdornmentExample)),
                new("mask", "Mask", "`Mask` fits typing to a pattern such as `(000) 000-0000`, where `0` is a digit, "
                    + "`a` a letter and `*` either, and adds the parentheses and dashes as you type, keeping the caret "
                    + "in place. The value holds only what was typed into the slots, or the whole text, parentheses "
                    + "and dashes included, with `IncludeMaskLiterals`.",
                    typeof(TextFieldMaskExample)),
                new("several-lines", "Several lines", "`Lines` makes a text area, and `MaxLines` lets it grow with its "
                    + "text up to that many lines and then scroll.", typeof(TextFieldLinesExample)),
                new("read-only-and-disabled", "Read-only and disabled", "`ReadOnly` keeps the field focusable and posted "
                    + "with its form. `Disabled` takes it out of the tab order and the post.",
                    typeof(TextFieldStatesExample)),
                new("size-and-variant", "Size and variant", "`Size` and `Variant` work the same on every boxed field: "
                    + "text, number, select, autocomplete, color and date range.", typeof(TextFieldSizeExample)),
                new("without-a-visible-label", "Without a visible label", "Where a field's purpose is plain from what "
                    + "surrounds it, such as a filter above a list, `AriaLabel` names it for screen readers. A visible "
                    + "label is better wherever there's room.", typeof(TextFieldAriaLabelExample)),
                new("when-focus-leaves", "When focus leaves", "`OnBlur` runs when focus leaves the field, which "
                    + "suits checking or saving what was entered as someone moves on.", typeof(TextFieldBlurExample)),
            ],
            Api = [typeof(TbTextField<>), typeof(FieldClasses)],
            Keyboard =
            [
                new(["Enter"], "In a single-line field, updates the value at once and submits its form."),
                new(["Backspace"], "Just after a mask's parenthesis or dash, removes the character before it."),
            ],
            Labeling =
            [
                "The `Label` is a real `<label>` and names the field; give every field one. A placeholder is not a label.",
            ],
            Announcements =
            [
                "A validation message that appears as someone edits the field is read out once. Checking the whole "
                    + "form on submit reads none out, so a form with several errors doesn't speak them all at once.",
                "An `Error` from the parent is read out once when it appears, even after a submit.",
                "When an error is fixed or the field goes away, its message leaves the live region.",
            ],
            Accessibility =
            [
                "Helper text and validation messages are linked to the field with `aria-describedby`, an error sets "
                    + "`aria-invalid`, and `Required` sets `aria-required`; the asterisk it adds is hidden from screen "
                    + "readers.",
                "`Type` sets the role: a text box, a search box for `Search`, and none for `Password`, as browsers "
                    + "expose them.",
                "Screen readers hear a `ReadOnly` field as read only.",
                "The clear button is named \"Clear\", or `ClearLabel`, and described by the field's label. It shows "
                    + "while the field holds text it can change, and pressing it puts focus back in the field.",
                "The `Filled` variant keeps a line with 3:1 contrast along the box's bottom, red when the field is invalid "
                    + "and dashed when it's read only.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "After a static form post, text that doesn't convert, such as letters in a number, stays in the field "
                    + "with the form's error, so nothing typed is lost. Blazor reports these errors for an `EditForm` "
                    + "whose model is supplied by `[SupplyParameterFromForm]` without its own `FormName`.",
                "On a static page a number or date shows in the invariant culture, as 1.5 and 07/02/2026, because Blazor "
                    + "reads a static form's post that way; for numbers typed in the user's own format, use a number "
                    + "field.",
                "A static form post binds a `Mask`ed field's text as it was sent, so check it on the model too, with a "
                    + "`[RegularExpression]` for example.",
            ],
            Notes =
            [
                new("A press anywhere in the box outside its controls, such as its padding or a start "
                    + "icon, focuses the input."),
                new("While a `Required` message shows, the field's own `Validation` doesn't run, so it is the only "
                    + "message. `Invalid` marks the field invalid without a message, for a form that lists its errors "
                    + "elsewhere."),
                new("An `Error` keeps its form from submitting until the parent clears it."),
                new("`@ref` the field and call `FocusAsync()` to move focus to it, or `SelectTextAsync()` to also select "
                    + "its text so typing replaces it."),
                new("`Clearable` on a number needs a nullable type, such as `int?`."),
                new("With `UpdateDelay`, the browser waits for typing to pause, so the server hears nothing while the "
                    + "user types. Enter in a single-line field, or leaving the field, updates the value at once. A "
                    + "`Clearable` field's clear button follows the value, so it shows or goes once typing pauses."),
                new("`MaxLines` needs a browser with CSS `field-sizing`."),
                new("With a `Mask`, empty slots make the field invalid, the placeholder shows the pattern, and a pattern "
                    + "of digits brings up a number keyboard."),
                new("Each `Size` is set from the theme's density, as a button's is."),
            ],
            Ready = new("[data-part=input]", "a masked field's input"),
            Target = new("textbox", "Name"),
            Blocks = ["tb-field"],
            DataParts =
            [
                .. FieldParts,
                new("input", "The text input."),
                new("clear", "The button that clears the text."),
            ],
            TestingTip = "A field with `UpdateDelay` takes its value once typing pauses, so assert with Playwright's "
                + "`Expect`, which waits, or press Enter to update it at once. In bUnit, which runs no browser, the "
                + "value updates on each input, so there's nothing to wait for.",
        },
        new("rich-text-editor", "Rich text editor", ExampleCategory.Inputs,
            "A labeled field for formatted text, with a toolbar, shortcuts, undo and paste cleanup.")
        {
            WhenToUse =
            [
                "Use it where people format what they write: bold for a warning, italic for a name, headings and "
                    + "lists to organize a longer note, and subscript and superscript for formulas and footnotes.",
            ],
            WhenNotToUse =
            [
                "For plain text, even several lines of it, use a [text field](components/text-field) with `Lines`.",
            ],
            Related = ["text-field", "form", "toolbar"],
            Sections =
            [
                new("try-it", "Try it", "Use the controls to see what each parameter does.",
                    typeof(RichTextEditorTryExample))
                {
                    Controls = new("<TbRichTextEditor Label=\"Try this editor\"{controls} @bind-Value=\"_value\" />",
                        ExampleControl.Text(nameof(TbRichTextEditor.Placeholder)),
                        ExampleControl.Text(nameof(TbRichTextEditor.HelperText)),
                        ExampleControl.Switch(nameof(TbRichTextEditor.ShowToolbar), true),
                        ExampleControl.Switch(nameof(TbRichTextEditor.Disabled)),
                        ExampleControl.Switch(nameof(TbRichTextEditor.ReadOnly)),
                        ExampleControl.Switch(nameof(TbRichTextEditor.Required))),
                },
                new("value", "The value", "The value is canonical HTML: a `<p>` for each paragraph, headings, quotes "
                    + "and lists as their own elements, `<br>` for line breaks and a tag for each format, or `\"\"` "
                    + "when the editor is empty. Paste from Word or a web page, and only the formats and blocks the "
                    + "editor allows survive. [Rich text](guides/rich-text) covers reading it in C#.",
                    typeof(RichTextEditorExample)),
                new("formats", "Formats", "`Formats` lists the formats the editor allows. The toolbar shows a button "
                    + "for each, and pasted text keeps only those. `RichTextFormats.None` with "
                    + "`Blocks=\"RichTextBlocks.None\"` makes a field of plain paragraphs.",
                    typeof(RichTextEditorFormatsExample)),
                new("blocks", "Blocks", "`Blocks` lists the kinds of block the editor allows: headings, quotes, and "
                    + "bulleted and numbered lists. The toolbar shows a button for each, three for headings, with "
                    + "Outdent and Indent for lists, and pasted text keeps only those. Typing Markdown such as `# `, "
                    + "`> `, `- ` or `1. ` at the start of a plain paragraph turns it into a heading, quote or list. "
                    + "With `RichTextBlocks.Rules`, the Horizontal line button or typing `---` then Enter puts in a "
                    + "rule. "
                    + "`RichTextBlocks.None` allows only plain paragraphs.",
                    typeof(RichTextEditorBlocksExample)),
                new("links", "Links", "The Link button and Ctrl+K open a dialog to add, edit, open or remove the link "
                    + "on the selected text, or, with nothing selected, to insert linked text. A bare address such as "
                    + "`example.com` gets `https://`, and an email address gets `mailto:`. `LinkSchemes` lists the URL "
                    + "schemes a link may use; the dialog won't accept a link with any other scheme, and paste drops "
                    + "such links.", typeof(RichTextEditorLinksExample)),
                new("changing-what-is-pasted", "Changing what's pasted", "`TransformPaste` changes what is pasted "
                    + "or dropped in, after the built-in cleanup: it gets the pasted text as a `RichTextDocument` and "
                    + "returns the document to put in. This example keeps the formats but drops the links. It's a "
                    + "plain function, so test it on a `RichTextDocument` without rendering the editor.",
                    typeof(RichTextEditorPasteExample)),
                new("without-a-toolbar", "Without a toolbar", "`ShowToolbar=\"false\"` hides the toolbar for a small "
                    + "field. The shortcuts and paste still apply the formats it allows.",
                    typeof(RichTextEditorToolbarExample)),
                new("required-and-validation", "Required and validation", "`Required` counts paragraphs with no text "
                    + "as empty. `Validation` gets the HTML; read its text with `RichTextDocument` to check a length.",
                    typeof(RichTextEditorRequiredExample)),
                new("read-only-and-disabled", "Read-only and disabled", "`ReadOnly` keeps the text focusable, "
                    + "selectable and copyable. `Disabled` takes it out of the tab order. Both disable the toolbar.",
                    typeof(RichTextEditorStatesExample)),
                new("updating-while-typing", "Updating while typing", "The value updates when focus leaves the "
                    + "editor. `UpdateOnInput` updates it on every change, and `UpdateDelay` waits for a pause.",
                    typeof(RichTextEditorUpdateExample)),
                new("height", "Height", "`Lines` sets the starting height, and the editor grows with its text. "
                    + "`MaxLines` caps it, after which it scrolls.", typeof(RichTextEditorHeightExample)),
            ],
            Api = [typeof(TbRichTextEditor), typeof(RichTextEditorLabels), typeof(FieldClasses)],
            Keyboard =
            [
                new(["Ctrl+B", "Ctrl+I", "Ctrl+U"], "Turns bold, italic or underline on or off, for the selection or "
                    + "the next text typed. Cmd on macOS."),
                new(["Ctrl+Shift+X"], "Turns strikethrough on or off."),
                new(["Ctrl+,", "Ctrl+."], "Turns subscript or superscript on or off."),
                new(["Ctrl+E"], "Turns inline code on or off."),
                new(["Ctrl+\\"], "Takes every format off the selection, or off the next text typed. Links and "
                    + "blocks stay."),
                new(["Ctrl+K"], "Opens the link dialog for the selected text, or for the link the caret is in or "
                    + "next to."),
                new(["Ctrl+Alt+1", "Ctrl+Alt+2", "Ctrl+Alt+3"], "Turns the selected paragraphs into a heading of "
                    + "that level, or back into plain paragraphs. Cmd+Option on macOS."),
                new(["Ctrl+Alt+0"], "Turns the selected paragraphs back into plain paragraphs."),
                new(["Ctrl+Shift+7", "Ctrl+Shift+8"], "Turns the selected paragraphs into a numbered or bulleted list, "
                    + "or back."),
                new(["Ctrl+Shift+9"], "Turns the selected paragraphs into a quote, or back."),
                new(["Tab", "Shift+Tab"], "Indents or outdents the list item.") { When = "In a list item" },
                new(["Escape"], "Lets the next Tab leave the editor.") { When = "In a list item" },
                new(["Enter"], "Starts a new paragraph; at the end of a heading, a plain one."),
                new(["Enter"], "Leaves the list or quote, a level at a time.")
                {
                    When = "In an empty list item or quote paragraph",
                },
                new(["Backspace"], "Takes the list or quote away, a level at a time, before joining paragraphs.")
                {
                    When = "At the start of a list item or quote paragraph",
                },
                new(["Backspace", "Delete"], "Takes away the rule before or after the paragraph.")
                {
                    When = "At the start or end of a paragraph next to a rule",
                },
                new(["Enter"], "Turns the paragraph into a horizontal rule.") { When = "In a plain paragraph of `---`" },
                new(["Shift+Enter"], "Breaks the line within the paragraph."),
                new(["Ctrl+Z"], "Undoes the last change, a word at a time while typing."),
                new(["Ctrl+Y", "Ctrl+Shift+Z"], "Redoes the last change undone."),
                new(["Ctrl+Shift+V"], "Pastes plain text."),
                new(["Tab"], "Leaves the editor, as from any field.") { When = "Outside a list" },
                new(["Left", "Right"], "Moves between the buttons.") { When = "In the toolbar" },
                new(["Home", "End"], "Goes to the first or last button.") { When = "In the toolbar" },
            ],
            RightToLeftKeys = "In the toolbar, Left moves to the next button and Right to the previous one.",
            Labeling =
            [
                "The `Label` names the editing area with `aria-labelledby`, since it isn't a form control a `<label>` "
                    + "can point at. Clicking the label still focuses it.",
                "The toolbar is named \"Formatting\" and each button by what it does, such as \"Bold\" or "
                    + "\"Bulleted list\"; `Labels` changes these words and the link dialog's.",
            ],
            Announcements =
            [
                "A validation message that appears as someone edits the field is read out once, as on every field.",
                "Each format and block button's state is announced through `aria-pressed`, and it follows the "
                    + "selection as the caret moves.",
            ],
            Accessibility =
            [
                "The editing area is a multi-line `textbox`, with `aria-required`, `aria-invalid`, `aria-readonly` "
                    + "and `aria-disabled` as they apply, and the placeholder as `aria-placeholder`.",
                "The toolbar is a [toolbar](components/toolbar), following its WAI-ARIA pattern: one Tab stop, with "
                    + "the arrow keys moving between its buttons. Each format and block button uses `aria-pressed` to "
                    + "report whether it applies to all of the selection, part of it (mixed) or none, and names its "
                    + "shortcuts with `aria-keyshortcuts`. The Link button has `aria-haspopup=\"dialog\"`.",
                "The link dialog is a modal [dialog](components/dialog) with labeled fields, and focus returns to the "
                    + "editor, with the selection it had, when it closes.",
                "Formats are real `<strong>`, `<em>`, `<u>`, `<s>`, `<sub>`, `<sup>`, `<code>` and `<a>` elements, "
                    + "and blocks are real headings, block quotes, lists and `<hr>` rules, which screen readers can "
                    + "report and move between.",
                "Tab leaves the editor rather than typing a tab, so keyboard users are never trapped in it. In a list "
                    + "item, Tab indents instead, as in word processors, and Escape lets the next Tab leave.",
            ],
            AppDuties =
            [
                "Give the editor a `Label`, or an `AriaLabel` where its purpose is plain from what surrounds it.",
                "Put a `TbRoot` in the layout, which hosts the link dialog.",
                "Parse the value with `RichTextDocument` on the server before storing it; the editor does, but a "
                    + "value posted some other way may not have gone through it.",
            ],
            Modes = new(ModeSupport.DisplayOnly, ModeSupport.DisplayOnly),
            BeforeInteractive =
            [
                "Before the page is interactive, the value shows as formatted text that can't yet be edited. A static "
                    + "form posts nothing for it, since the editing area isn't a form control.",
            ],
            Notes =
            [
                new("Typing, input methods, spellcheck and autocorrect go through the browser's own editing, so they "
                    + "work as in any text box, and the editor reads back the result."),
                new("A click on a link places the caret, as on any text. Ctrl+click (Cmd+click on macOS) opens the link "
                    + "in a new tab, and so does the link dialog's Open link."),
                new("`@ref` the editor and call `FocusAsync()` to move focus to it."),
            ],
            Ready = new("[role=textbox]", "the editing area"),
            Target = new("textbox", "Trail report"),
            Blocks = ["tb-field", "tb-rich-text-editor"],
            DataParts =
            [
                .. FieldParts.Where(part => part.Name is not ("start" or "end")),
                new("editor", "The toolbar and editing area together."),
                new("toolbar", "The formatting toolbar."),
                new("input", "The editing area."),
            ],
            TestingTips =
            [
                "In bUnit, `cut.Tb().RichTextEditor(label).EnterAsync(html)` sets the value through the editor's own "
                    + "change event.",
                "In Playwright, `page.Tb().RichTextEditor(label).EnterAsync(html)` pastes the HTML and leaves the "
                    + "editor.",
                "To type instead, wait for `Input` to have its `data-ready` attribute, then type into it. The value "
                    + "reaches .NET when focus leaves, or as it's typed with `UpdateOnInput`.",
                "To add a link in Playwright, select the text and press `ControlOrMeta+k`, then fill the dialog's "
                    + "\"Link\" field and press Enter.",
                "`TransformPaste` is a plain function, so test it on a `RichTextDocument` without the editor.",
            ],
        },
        new("number-field", "Number field", ExampleCategory.Inputs,
            "A labeled number input with step buttons and limits.")
        {
            WhenToUse =
            [
                "Use it for an amount: a quantity, a price or a percentage, where stepping and limits make sense.",
            ],
            WhenNotToUse =
            [
                "For digits that aren't an amount, such as a phone number or a code, use a [text field]"
                    + "(components/text-field#mask) with a `Mask`.",
                "For a value picked along a range, where the exact number matters less, use a [slider]"
                    + "(components/slider).",
            ],
            Related = ["text-field", "slider", "form"],
            Sections =
            [
                new("limits-and-steps", "Limits and steps", "`Min` and `Max` set the limits. A typed value outside them, or "
                    + "text that isn't a number, shows an error and stays in the field to be fixed, and the bound value "
                    + "doesn't change. `Step` sets how far the arrow keys and buttons move the value, and `LargeStep` how far "
                    + "Page Up and Page Down do. Holding a step button repeats it, faster the longer it's held, and "
                    + "the value changes once, when it's let go.",
                    typeof(NumberFieldExample)),
                new("values-set-by-code", "Values set by code", "`Min` and `Max` hold for a value your code sets too, "
                    + "such as one loaded from a saved trip: once the form validates, the field explains its limits and "
                    + "the form doesn't submit. `ClampToRange` moves only typed values.",
                    typeof(NumberFieldCheckedExample)),
                new("required", "Required", "`Required` marks the field and, once it is edited or its form validates, "
                    + "shows a message if it is empty. `RequiredMessage` replaces the default, which uses the label. A "
                    + "long label wraps, as this one does.", typeof(NumberFieldRequiredExample)),
                new("format-and-culture", "Format and culture", "`Format`, such as `C2` or `P0`, shows the value with "
                    + "grouping and symbols, and the field reads and writes numbers in its `Culture`. `Decimals` rounds "
                    + "what is typed to that many places in the value, so `Decimals=\"2\"` on a `P0` field rounds to "
                    + "the nearest percent.",
                    typeof(NumberFieldFormatExample)),
                new("clamping", "Clamping", "`ClampToRange` moves a typed value outside the limits to the nearer "
                    + "limit, instead of showing an error.", typeof(NumberFieldClampExample)),
                new("empty-values", "Empty values", "A nullable type such as `int?` lets the field stay empty, which "
                    + "binds as `null`.", typeof(NumberFieldEmptyExample)),
                new("hidden-step-buttons", "Hidden step buttons", "`HideStepButtons` removes the buttons for a value "
                    + "people just type, though the arrow keys still step it.", typeof(NumberFieldNoButtonsExample)),
                new("updating-while-typing", "Updating while typing", "The value updates when the user leaves the field or "
                    + "presses Enter. `UpdateOnInput` updates it as they type, for output that follows along, like the "
                    + "fee here. A `Format` still waits until they leave or press Enter, so it doesn't rewrite the text "
                    + "under the caret.",
                    typeof(NumberFieldLiveExample)),
                new("start-and-end", "Start and end content", "`Start` and `End` hold a unit or a symbol beside the "
                    + "input.", typeof(NumberFieldAdornmentExample)),
                new("read-only-and-disabled", "Read-only and disabled", "`ReadOnly` stops typing, the arrow keys and the step "
                    + "buttons from changing the value, and turns the buttons off. The field still posts with its form. "
                    + "`Disabled` takes it out of the tab order and the post.",
                    typeof(NumberFieldStatesExample)),
                new("size-and-variant", "Size and variant", "`Size` and `Variant` work as on the [text field]"
                    + "(components/text-field#size-and-variant).", typeof(NumberFieldSizeExample)),
            ],
            Api = [typeof(TbNumberField<>), typeof(FieldClasses)],
            Keyboard =
            [
                new(["Up", "Down"], "Steps the value by `Step`."),
                new(["Page Up", "Page Down"], "Steps the value by `LargeStep`, ten steps unless it's set."),
                new(["Enter"], "Updates the value at once and submits the field's form."),
            ],
            Labeling =
            [
                "The `Label` names the field.",
            ],
            Announcements =
            [
                "A validation message that appears as someone edits the field is read out once, and a failed submit "
                    + "reads none out, as on a [text field](components/text-field).",
            ],
            Accessibility =
            [
                "A native number input, a `spinbutton`.",
                "With a `Format`, it is a text box acting as a `spinbutton` instead, since a number box can't show "
                    + "grouping or symbols. It gives screen readers the number as `aria-valuenow` and the formatted "
                    + "text as `aria-valuetext`.",
                "The Decrease and Increase buttons are left out of the tab order, since the arrow keys do the same, and "
                    + "they turn off at `Min` and `Max`. A click on one moves focus to the input, as a native spinner "
                    + "does, and a tap doesn't, so the on-screen keyboard doesn't open.",
                "Helper text and validation messages are linked to the field with `aria-describedby`, an error sets "
                    + "`aria-invalid`, and `Required` sets `aria-required`; the asterisk it adds is hidden from screen "
                    + "readers.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "Before the page is interactive, a field with a `Format` is a plain number box, since a static form "
                    + "posts numbers in the invariant culture.",
                "A static form's post is checked against `Min` and `Max` on the server, as a typed value is, unless "
                    + "the field is read-only; use `Validation` to check a read-only value.",
            ],
            Notes =
            [
                new("A press anywhere in the box outside its controls, such as its padding or a start "
                    + "icon, focuses the input."),
                new("What the user types stays as typed until they leave the field, press Enter, step the value or "
                    + "click a step button that's off at a limit, and an error waits until then too."),
                new("`ClampToRange` also moves a number too large or small for the value's type to the nearer limit."),
            ],
            Ready = new("[data-part=input]", "the input"),
            Target = new("spinbutton", "Quantity"),
            Blocks = ["tb-field"],
            DataParts =
            [
                .. FieldParts,
                new("input", "The number input."),
                new("decrement", "The button that steps the value down."),
                new("increment", "The button that steps the value up."),
            ],
            TestingTip = "Without a `Format` the field reads what was typed through its own change event, which "
                + "bUnit's `Change` doesn't reach; `await cut.Tb().NumberField(\"Quantity\").EnterAsync(\"3\")` "
                + "from `Trailblazor.Testing` types into either kind. In Playwright, "
                + "`await page.Tb().NumberField(\"Quantity\").EnterAsync(\"3\")` from "
                + "`Trailblazor.Testing.Playwright` waits for the field's `data-ready`, types and leaves it.",
        },
        new("checkbox", "Checkbox", ExampleCategory.Inputs,
            "Turns an option on or off, or shows that only some of a group are on.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for one option that takes effect when the form is submitted, or to accept something, such as "
                    + "terms.",
            ],
            WhenNotToUse =
            [
                "For a setting that applies at once, use a [switch](components/switch).",
                "For several related options, use a [checkbox group](components/checkbox-group), which names them "
                    + "together.",
                "For one choice among several, use a [radio group](components/radio-group).",
            ],
            Related = ["checkbox-group", "switch", "radio-group", "form"],
            Pattern = new("checkbox", "https://www.w3.org/WAI/ARIA/apg/patterns/checkbox/"),
            Sections =
            [
                new("label-and-help", "Label and help text", "`Label` names the box, and clicking the label toggles "
                    + "it. `HelperText` adds a hint. A long label wraps beside the box.", typeof(CheckboxExample)),
                new("mixed", "Mixed", "With a `bool?` value, `null` shows the mixed state, for a box that stands for "
                    + "several others when only some of them are on. A click sets `true` or `false`. The box never "
                    + "goes back to `null` by itself, so your code has to set it when only some of those boxes are on.",
                    typeof(CheckboxMixedExample)),
                new("required", "Required", "A `Required` checkbox must be checked, as when accepting terms. "
                    + "Once it is changed or its form validates, an unchecked box shows a message. `RequiredMessage` "
                    + "replaces the default, which uses the label, with one that says what to do.",
                    typeof(CheckboxRequiredExample)),
                new("rich-content", "Rich content", "`ChildContent` takes the place of `Label` for a label with a link "
                    + "in it. Clicking the link doesn't check the box, and opening it in a new tab keeps what's been "
                    + "entered in the form. With no `Label`, the default required message is just \"The field is "
                    + "required.\", so set `RequiredMessage`.", typeof(CheckboxContentExample)),
                new("read-only-and-disabled", "Read-only and disabled", "`ReadOnly` keeps the box focusable, and posted "
                    + "if it's checked, but neither a click nor Space changes it. `Disabled` takes it out of the tab order "
                    + "and the post.", typeof(CheckboxStatesExample)),
                new("tone-and-size", "Tone and size", "`Tone` colors the checked box, and `Size` scales the box and its "
                    + "label.", typeof(CheckboxToneExample)),
            ],
            Api = [typeof(TbCheckbox<>), typeof(ChoiceClasses)],
            Keyboard =
            [
                new(["Space"], "Toggles the box."),
            ],
            Labeling =
            [
                "A native checkbox inside its label, so the `Label` names it.",
                "Rich content as `ChildContent` names the box instead, link text and a `NewTab` link's hint included, "
                    + "so keep it short.",
            ],
            Announcements =
            [
                "Screen readers read a `null` value as partly checked, and a `ReadOnly` box as read only.",
                "A validation message that appears as someone changes the box is read out once, and a failed submit "
                    + "reads none out, as on a [text field](components/text-field).",
            ],
            Accessibility =
            [
                "Helper text and validation messages are linked to the field with `aria-describedby`, an error sets "
                    + "`aria-invalid`, and `Required` sets `aria-required`; the asterisk it adds is hidden from screen "
                    + "readers.",
                "In forced-colors mode it falls back to the system's own control, so it stays visible in every "
                    + "high-contrast theme.",
                "Every `Tone` stands out from the page by at least 3:1 when checked; warning uses its text color to "
                    + "reach that contrast.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "A `null` value looks unchecked until the page is interactive, because a script sets the mixed state.",
                "`ReadOnly` already works on a static page, so a click or Space doesn't change the box.",
                "An unchecked box posts nothing, so a static form binds it as `false`, or as `null` for a `bool?`.",
            ],
            Target = new("checkbox", "Email me updates"),
            Blocks = ["tb-checkbox"],
            DataParts =
            [
                new("input", "The checkbox input."),
                new("label", "The checkbox's label."),
                new("helper", "The help text under the checkbox."),
                new("error", "The validation message, shown while the value is invalid."),
            ],
            TestingTip = "It's a native checkbox, so Playwright's `CheckAsync()` and `UncheckAsync()` work on "
                + "`GetByRole(AriaRole.Checkbox, ...)`, and `ToBeCheckedAsync(new() { Indeterminate = true })` waits "
                + "for the mixed state, which a script sets after rendering. In bUnit, `Change(true)` on the `input` "
                + "checks it, but a browser script is what stops a `ReadOnly` box from changing, so a `Change` on one "
                + "still changes the value. Test read-only behavior in Playwright.",
            Telemetry = CheckboxTelemetry,
        },
        new("checkbox-group", "Checkbox group", ExampleCategory.Inputs,
            "Picks any number of options from a short list shown all at once.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it when someone can pick any number of options from a short list, and seeing them all helps "
                    + "them choose.",
                "[Choosing a list field](guides/forms#choosing-a-list-field) compares it with the other fields that "
                    + "pick from a list.",
            ],
            WhenNotToUse =
            [
                "For a long list, use a [list box](components/listbox) or a [select](components/select).",
                "For one choice among several, use a [radio group](components/radio-group).",
                "For one option on its own, use a [checkbox](components/checkbox).",
            ],
            Related = ["checkbox", "radio-group", "listbox", "form"],
            Pattern = new("checkbox", "https://www.w3.org/WAI/ARIA/apg/patterns/checkbox/"),
            Sections =
            [
                new("options-and-value", "Options and value", "Each `TbCheckboxItem` is an option. The group's `Value` "
                    + "holds the checked options' values, in the order they were checked.",
                    typeof(CheckboxGroupExample)),
                new("required", "Required", "`Required` shows a marker. Once the group is changed or its form "
                    + "validates, a message shows while nothing is checked. `RequiredMessage` replaces the default, "
                    + "which uses the label. Say what's needed in `HelperText` too.",
                    typeof(CheckboxGroupRequiredExample)),
                new("rich-options", "Options with more than text", "An option's content can hold more than text, such "
                    + "as a chip. All of it names the checkbox, so keep it short.",
                    typeof(CheckboxGroupContentExample)),
                new("row", "Row", "`Row` lays the options out in a row, wrapping when they don't fit. A long "
                    + "option wraps beside its box.",
                    typeof(CheckboxGroupRowExample)),
                new("comparer", "Values loaded again", "`Comparer` decides whether two values are the same, so a value "
                    + "from another query, equal only by id, still checks its option.",
                    typeof(CheckboxGroupComparerExample)),
                new("read-only-and-disabled", "Read-only and disabled", "`ReadOnly` keeps each box focusable and "
                    + "posted, but neither a click nor Space changes it. `Disabled` on the group turns off every "
                    + "option, and `Disabled` on an item turns off that option alone.",
                    typeof(CheckboxGroupStatesExample)),
                new("tone-and-size", "Tone and size", "`Tone` colors the checked boxes, and `Size` scales the boxes and "
                    + "their labels.", typeof(CheckboxGroupToneExample)),
            ],
            Api = [typeof(TbCheckboxGroup<>), typeof(TbCheckboxItem<>), typeof(ChoiceClasses)],
            Keyboard =
            [
                new(["Tab"], "Moves to the next option that isn't disabled, since each is its own tab stop."),
                new(["Space"], "Toggles the focused option."),
            ],
            Labeling =
            [
                "The `Label` is the `fieldset`'s legend and names the group. Each option is a native checkbox, named "
                    + "by its content.",
            ],
            Announcements =
            [
                "An error marks the group and every checkbox invalid, so screen readers say so on each option.",
                "A validation message that appears as someone changes the group is read out once, and a failed submit "
                    + "reads none out, as on a [text field](components/text-field).",
            ],
            Accessibility =
            [
                "Helper text and messages describe the whole group.",
                "In forced-colors mode it falls back to the system's own controls, so it stays visible in every "
                    + "high-contrast theme.",
            ],
            AppDuties =
            [
                "Say what's needed, such as \"Pick at least one\", in the helper text. ARIA has no required state for a "
                    + "group and the `Required` marker is hidden from screen readers, so until a validation message "
                    + "appears, that text is the only way screen reader users hear what's needed.",
            ],
            Notes =
            [
                new("`@ref` the group and call `FocusAsync()` to move focus to the first option that isn't disabled."),
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Partial),
            BeforeInteractive =
            [
                "A static form posts the checked options in the order they're listed, not the order they were checked.",
            ],
            Target = new("group", "Trail days"),
            Blocks = ["tb-checkbox-group", "tb-checkbox"],
            DataParts =
            [
                new("label", "The group's legend, and each option's text."),
                new("helper", "The help text under the group."),
                new("error", "The validation message, shown while the value is invalid."),
                new("options", "The list of checkboxes."),
                new("input", "One checkbox input."),
            ],
            Anatomy = """
                <TbCheckboxGroup Label="…" @bind-Value="_values">
                    <TbCheckboxItem Value="…">…</TbCheckboxItem>
                    <TbCheckboxItem Value="…">…</TbCheckboxItem>
                </TbCheckboxGroup>
                """,
            TestingTip = "Each option is a native checkbox, so Playwright's `CheckAsync()` and `UncheckAsync()` work "
                + "on `GetByRole(AriaRole.Checkbox, ...)` inside the group. In bUnit, `Change(true)` or "
                + "`Change(false)` on an option's `input` checks or unchecks it.",
        },
        new("switch", "Switch", ExampleCategory.Inputs,
            "Turns a setting on or off right away.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for a setting that takes effect as soon as it changes, with nothing to submit.",
            ],
            WhenNotToUse =
            [
                "For a choice saved when a form is submitted, a [checkbox](components/checkbox) is usually the "
                    + "better fit.",
                "For two or more named options, use a [radio group](components/radio-group) or a "
                    + "[toggle group](components/toggle-group).",
            ],
            Related = ["checkbox", "radio-group", "toggle-group"],
            Pattern = new("switch", "https://www.w3.org/WAI/ARIA/apg/patterns/switch/"),
            Sections =
            [
                new("label-and-help", "Label and help text", "`Label` names the switch, and clicking the label toggles "
                    + "it. `HelperText` adds a hint. A long label wraps beside the switch.", typeof(SwitchExample)),
                new("required", "Required", "A `Required` switch must be on, as when someone has to accept a setting "
                    + "before continuing. Once it is changed or its form validates, a switch that's off shows a "
                    + "message. `RequiredMessage` replaces the default, which uses the label, with one that says what to "
                    + "do.",
                    typeof(SwitchRequiredExample)),
                new("rich-content", "Rich content", "`ChildContent` takes the place of `Label` for a label with a link "
                    + "in it. Clicking the link doesn't toggle the switch, and opening it in a new tab keeps what's "
                    + "been entered on the page. With no `Label`, the default required message is just \"The field is "
                    + "required.\", so set `RequiredMessage`.", typeof(SwitchContentExample)),
                new("read-only-and-disabled", "Read-only and disabled", "`ReadOnly` keeps the switch focusable, and "
                    + "posted if it's on, but neither a click nor Space changes it. `Disabled` takes it out of the tab "
                    + "order and the post.", typeof(SwitchStatesExample)),
                new("tone-and-size", "Tone and size", "`Tone` colors the track when on, and `Size` scales the track "
                    + "and its label.", typeof(SwitchToneExample)),
                new("thumb-icons", "Thumb icons", "`ThumbIcon` shows an icon in the thumb while the switch is on, and "
                    + "`OffThumbIcon` while it's off, so the state shows by more than color and position.",
                    typeof(SwitchIconsExample)),
            ],
            Api = [typeof(TbSwitch), typeof(ChoiceClasses)],
            Keyboard =
            [
                new(["Space"], "Toggles the switch."),
            ],
            Labeling =
            [
                "A native checkbox with `role=\"switch\"`, named by its `Label`.",
                "`ChildContent` names the switch in place of `Label`, including any link text and a `NewTab` link's "
                    + "hint, so keep it short.",
            ],
            Announcements =
            [
                "Screen readers read it as on or off, and a `ReadOnly` switch as read only.",
                "A validation message that appears as someone changes the switch is read out once, and a failed submit "
                    + "reads none out, as on a [text field](components/text-field).",
            ],
            Accessibility =
            [
                "Helper text and validation messages are linked to the field with `aria-describedby`, an error sets "
                    + "`aria-invalid`, and `Required` sets `aria-required`; the asterisk it adds is hidden from screen "
                    + "readers.",
                "With reduced motion, the thumb moves without sliding.",
                "In forced-colors mode it falls back to the system's own control, so it stays visible in every "
                    + "high-contrast theme. That control has no thumb, so thumb icons don't show there.",
                "Thumb icons are hidden from screen readers, which already read the switch as on or off.",
                "Every `Tone` stands out from the page by at least 3:1 when on; warning uses its text color to reach "
                    + "that contrast.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Partial),
            BeforeInteractive =
            [
                "`ReadOnly` already works on a static page, so a click or Space doesn't change the switch.",
                "A switch that's off posts nothing, so a static form binds it as `false`.",
            ],
            Target = new("switch", "Auto-save"),
            Blocks = ["tb-switch"],
            DataParts =
            [
                new("input", "The switch's input."),
                new("label", "The switch's label."),
                new("helper", "The help text under the switch."),
                new("error", "The validation message, shown while the value is invalid."),
                new("thumb-icon", "An icon in the thumb, from `ThumbIcon` or `OffThumbIcon`, with `data-state` set to "
                    + "`on` or `off`."),
            ],
            TestingTip = "It's a native checkbox, so Playwright's `CheckAsync()` and `UncheckAsync()` work on "
                + "`GetByRole(AriaRole.Switch, ...)`. In bUnit, `Change(true)` on the `input` turns it on, but a "
                + "browser script is what stops a `ReadOnly` switch from changing, so a `Change` on one still changes "
                + "the value. Test read-only behavior in Playwright.",
            Telemetry = CheckboxTelemetry,
        },
        new("slider", "Slider", ExampleCategory.Inputs,
            "Chooses a number, or a range, by dragging a thumb along a track or with the arrow keys.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it when the rough position matters more than the exact number, such as brightness or a price "
                    + "range.",
            ],
            WhenNotToUse =
            [
                "For an exact number, use a [number field](components/number-field).",
                "For a few named choices, use a [radio group](components/radio-group).",
            ],
            Related = ["number-field", "radio-group", "form"],
            Pattern = new("slider", "https://www.w3.org/WAI/ARIA/apg/patterns/slider/"),
            RightToLeftKeys = "Left raises the value and Right lowers it, so the thumb moves the way the key points.",
            Sections =
            [
                new("value", "Value", "`ShowValue` shows the value above the thumb, and `ValueFormatter` sets its "
                    + "text. `@bind-Value` follows the thumb as it moves, and `ValueCommitted` reports where it "
                    + "settled, once someone lets go or finishes with the keys.", typeof(SliderExample)),
                new("label-and-help", "Label and help text", "`Label` names the slider and `HelperText` adds a hint. A "
                    + "long label wraps above the track.", typeof(SliderHelpExample)),
                new("steps-and-marks", "Steps and marks", "`Min`, `Max` and `Step` set where the thumb can stop, and "
                    + "`Marks` labels points along the track.", typeof(SliderMarksExample)),
                new("range", "Range", "`TbRangeSlider` binds a `SliderRange` with two thumbs, and neither can pass the "
                    + "other. A click on the track moves the nearer thumb. Thumbs on one value, or close enough to "
                    + "overlap, separate in the direction they're dragged. `StartLabel` and `EndLabel` name the thumbs.",
                    typeof(SliderRangeExample)),
                new("sizes", "Sizes", "`Size` is `Small`, `Medium` or `Large`, and scales the thumb, the track and the "
                    + "label. The slider stays at least 24 pixels high, so it is still easy to press. Unlike other "
                    + "components' sizes, a slider's don't change with density.",
                    typeof(SliderSizeExample)),
                new("validation", "Validation", "The thumb always sits on a value, so `Required` only catches a "
                    + "nullable slider still bound to `null`. `Validation` checks the value once it is changed or its "
                    + "form validates.",
                    typeof(SliderValidationExample)),
                new("read-only-and-disabled", "Read-only and disabled", "`ReadOnly` keeps the thumbs focusable and "
                    + "posted, but neither the pointer nor the keys move them. `Disabled` takes them out of the tab "
                    + "order and the post.", typeof(SliderStatesExample)),
                new("right-to-left", "Right to left", "Inside `dir=\"rtl\"`, the track fills from the right, and the "
                    + "shown value stays above the thumb. The [right-to-left guide](guides/right-to-left) has the rest.",
                    typeof(SliderRightToLeftExample)),
            ],
            Api = [typeof(TbSlider<>), typeof(TbRangeSlider<>), typeof(SliderMark), typeof(SliderRange<>),
                typeof(ChoiceClasses)],
            Keyboard =
            [
                new(["Right", "Up"], "Raises the value by `Step`."),
                new(["Left", "Down"], "Lowers the value by `Step`."),
                new(["Page Up", "Page Down"], "Raises or lowers the value by a larger amount, which the browser "
                    + "chooses."),
                new(["Home", "End"], "Goes to `Min` or `Max`."),
                new(["Tab"], "Moves from one thumb to the other.") { When = "On a range slider" },
            ],
            Labeling =
            [
                "Each thumb is the browser's own range input, a `slider` named by the `Label`.",
                "A range slider names each thumb with the label followed by \"Minimum\" or \"Maximum\", which "
                    + "`StartLabel` and `EndLabel` replace.",
            ],
            Announcements =
            [
                "`ValueFormatter` sets the text screen readers hear for the value (`aria-valuetext`).",
                "Screen readers read a `ReadOnly` thumb as read only.",
                "Mark labels aren't read out, so use `ValueFormatter` when the words matter.",
                "A validation message that appears as someone moves a thumb is read out once, and a failed submit "
                    + "reads none out, as on a [text field](components/text-field).",
            ],
            Accessibility =
            [
                "Helper text and validation messages are linked to each thumb with `aria-describedby`, and an error "
                    + "sets `aria-invalid`.",
                "In forced-colors mode the track and its fill take system colors.",
            ],
            Notes =
            [
                new("A value outside `Min` and `Max` shows at the nearest end."),
                new("While the thumb moves, `ValueChanged` is raised at most every 50 ms, and once more when it stops, so "
                    + "a drag over a slow link doesn't send a message per frame. `ValueCommitted` always has the value "
                    + "where the thumb was let go."),
                new("`Min` can't be above `Max`, both must fit the number type, and `Step` must be above 0. A whole "
                    + "number type also needs a whole `Min`, `Max` and `Step`. Anything else throws when it renders. When "
                    + "`Min` equals `Max`, the slider shows that value and is disabled, so it isn't posted with a form."),
                new("`@ref` the slider and call `FocusAsync()` to move focus to it. A range slider focuses its start "
                    + "thumb."),
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "It posts in a static form, as a number or as `Start` and `End`.",
                "Before the page is interactive, a thumb moves, but the fill, the shown value and the text screen "
                    + "readers hear stay where they were until the next render.",
                "Nothing stops a range slider's thumbs from passing each other until the page is interactive, so on a "
                    + "static post, check that `Start` isn't above `End`.",
                "`ReadOnly` already works on a static page.",
            ],
            Ready = new("[data-part=input]", "the input, or each thumb of a range slider"),
            Target = new("slider", "Map brightness"),
            Blocks = ["tb-slider"],
            DataParts =
            [
                new("label", "The slider's label."),
                new("helper", "The help text under the slider."),
                new("error", "The validation message, shown while the value is invalid."),
                new("value", "The value shown above each thumb."),
                new("track", "The track the thumbs move along."),
                new("fill", "The part of the track up to the value, or between a range's two values."),
                new("mark", "A tick mark under the track, with an optional label."),
                new("input", "A single slider's range input."),
                new("start-thumb", "A range slider's input for its low end."),
                new("end-thumb", "A range slider's input for its high end."),
            ],
            TestingTip = "`await page.Tb().Slider(\"Map brightness\").SetAsync(80)` from "
                + "`Trailblazor.Testing.Playwright` moves the slider and waits until the app has the value, and "
                + "`ValueAsync()` reads it back. "
                + "`RangeSlider(\"Elevation\").SetAsync(1000, 3000)` does the same for both thumbs. With "
                + "`GetByRole(AriaRole.Slider).FillAsync(\"80\")` alone, `ToHaveValueAsync(\"80\")` passes before the "
                + "app hears of the change. A range slider's thumbs are named \"Elevation Minimum\" and \"Elevation "
                + "Maximum\".",
        },
        new("radio-group", "Radio group", ExampleCategory.Inputs,
            "Picks one option from a short list shown all at once.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for one choice from a short list, when seeing every option helps someone choose.",
                "[Choosing a list field](guides/forms#choosing-a-list-field) compares it with the other fields that "
                    + "pick from a list.",
            ],
            WhenNotToUse =
            [
                "For a long list, use a [select](components/select).",
                "For a compact row where a choice takes effect right away, use a "
                    + "[toggle group](components/toggle-group).",
                "For any number of choices, use a [checkbox group](components/checkbox-group).",
                "For one setting that's on or off, use a [switch](components/switch).",
            ],
            Related = ["select", "toggle-group", "segmented-control", "checkbox-group", "switch", "form"],
            Pattern = new("radio group", "https://www.w3.org/WAI/ARIA/apg/patterns/radio/"),
            RightToLeftKeys = "Left moves to the next option and Right to the previous one, except in Safari, which "
                + "doesn't swap them.",
            Sections =
            [
                new("options-and-value", "Options and value", "Each `TbRadio` is an option, and the group's `Value` "
                    + "holds the chosen option's value, which can be any type, such as an enum. `Disabled` on an option "
                    + "turns off that option alone.",
                    typeof(RadioGroupExample)),
                new("row", "Row", "`Row` lays the options out in a row, wrapping when they don't fit. A long option "
                    + "wraps beside its radio.", typeof(RadioGroupRowExample)),
                new("required", "Required", "`Required` shows a marker. Once its form validates, a message shows "
                    + "while nothing is chosen. A non-nullable enum or number always counts as chosen, so bind a "
                    + "nullable one. `RequiredMessage` replaces the default, which uses the label.",
                    typeof(RadioGroupRequiredExample)),
                new("validation", "Validation", "`Validation` checks the choice once it is changed or its form "
                    + "validates.", typeof(RadioGroupValidationExample)),
                new("error", "Errors from your app", "`Error` shows a message your app sets, such as one from a server. "
                    + "Until your app clears it, the form won't submit, so clear it when the choice changes.",
                    typeof(RadioGroupErrorExample)),
                new("rich-options", "Options with more than text", "An option's content can hold more than text, such "
                    + "as a chip. All of it names the radio, so keep it short.", typeof(RadioGroupContentExample)),
                new("read-only-and-disabled", "Read-only and disabled", "`ReadOnly` turns off every option but the "
                    + "chosen one, which stays a tab stop and still posts; with nothing chosen, every option is off. "
                    + "`Disabled` on the group turns off every option.", typeof(RadioGroupStatesExample)),
                new("tone-and-size", "Tone and size", "`Tone` colors the chosen radio, and `Size` scales the radios "
                    + "and their labels.", typeof(RadioGroupToneExample)),
            ],
            Api = [typeof(TbRadioGroup<>), typeof(TbRadio<>), typeof(ChoiceClasses)],
            Keyboard =
            [
                new(["Tab"], "Moves into the group and out of it, as one tab stop. Focus lands on the chosen radio. "
                    + "With nothing chosen, it lands on the first radio that isn't disabled."),
                new(["Down", "Right"], "Moves to the next option that isn't disabled and chooses it. Most browsers wrap "
                    + "at the end of the group, but Safari stops there."),
                new(["Up", "Left"], "Moves to the previous option that isn't disabled and chooses it, wrapping the same "
                    + "way."),
                new(["Space"], "Chooses the focused radio, when nothing is chosen yet."),
            ],
            Labeling =
            [
                "The `Label` is the `fieldset`'s legend and names the group. Each option is a native radio, named by "
                    + "its content.",
            ],
            Announcements =
            [
                "Screen readers read a `ReadOnly` group as read only.",
                "An `Error` your app sets is read out once when it appears, even after a submit.",
                "A validation message that appears as someone changes the choice is read out once, and a failed submit "
                    + "reads none out, as on a [text field](components/text-field).",
            ],
            Accessibility =
            [
                "A `fieldset` with `role=\"radiogroup\"`. Helper text and messages describe the whole group, an error "
                    + "sets `aria-invalid` on it, and `Required` sets `aria-required`; the asterisk it adds is hidden "
                    + "from screen readers.",
                "In forced-colors mode it falls back to the system's own control, so it stays visible in every "
                    + "high-contrast theme.",
                "Every `Tone` stands out from the page by at least 3:1 when chosen; warning uses its text color to "
                    + "reach that contrast.",
            ],
            Notes =
            [
                new("`Comparer` decides whether two values are the same, such as entities matched by id, so a value "
                    + "loaded again still checks its radio."),
                new("`@ref` the group and call `FocusAsync()` to move focus where Tab would: to the chosen radio, or "
                    + "else the first radio that isn't disabled."),
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Partial),
            BeforeInteractive =
            [
                "Native radios give the tab stop and the arrow keys, so choosing works before the page is interactive, "
                    + "and a static form posts the chosen value.",
                "`ReadOnly` already works on a static page, since the other options render disabled.",
            ],
            Target = new("radiogroup", "Paper size"),
            Blocks = ["tb-radio-group", "tb-radio"],
            DataParts =
            [
                new("label", "The group's legend, and each option's text."),
                new("helper", "The help text under the group."),
                new("error", "The validation message, shown while the value is invalid."),
                new("options", "The list of radios."),
                new("input", "One radio input."),
            ],
            Anatomy = """
                <TbRadioGroup Label="…" @bind-Value="_value">
                    <TbRadio Value="…">…</TbRadio>
                    <TbRadio Value="…">…</TbRadio>
                </TbRadioGroup>
                """,
            TestingTip = "Each option is a native radio, so Playwright's `CheckAsync()` works on "
                + "`GetByRole(AriaRole.Radio, ...)` inside the group. In bUnit, `Change(true)` on an option's `input` "
                + "chooses it.",
        },
        new("rating", "Rating", ExampleCategory.Inputs,
            "A row of stars people choose from to rate something, such as a trail out of five.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it to ask how much people liked something, on a short scale everyone knows.",
            ],
            WhenNotToUse =
            [
                "For choices that aren't more or less of one thing, use a [radio group](components/radio-group).",
                "For a number on a long scale, use a [slider](components/slider).",
            ],
            Related = ["radio-group", "slider"],
            Pattern = new("radio group", "https://www.w3.org/WAI/ARIA/apg/patterns/radio/"),
            RightToLeftKeys = "Left chooses the next star and Right the previous one, except in Safari, which doesn't "
                + "swap them.",
            Sections =
            [
                new("basic", "Rating", "`@bind-Value` is the number of stars chosen, 0 for none. Chosen stars are "
                    + "filled and the rest are outlines. Pointing at a star previews the rating it would give. With "
                    + "`Clearable`, choosing the same star again takes the rating back to 0.", typeof(RatingExample)),
                new("required", "Required", "`Required` shows a marker and treats a rating of 0 as missing. Once its "
                    + "form validates, a message shows under the stars, and the outlines turn the error color. "
                    + "`RequiredMessage` replaces the default message.", typeof(RatingRequiredExample)),
                new("states", "Scale, look and states", "`Max` sets how many stars there are, `Icon` their shape, "
                    + "`Tone` their color and `Size` how big they are. `StarLabel` names each star for screen readers. "
                    + "`ReadOnly` shows a rating people can't change; with nothing chosen it turns off every star, so "
                    + "it leaves the tab order. `Disabled` turns the rating off.",
                    typeof(RatingStatesExample)),
            ],
            Api = [typeof(TbRating), typeof(ChoiceClasses)],
            Keyboard =
            [
                new(["Tab"], "Moves into the rating and out of it, as one tab stop. Focus lands on the chosen star, or "
                    + "on the first when none is chosen."),
                new(["Right", "Down"], "Chooses the next star. Most browsers wrap at the last star, but Safari "
                    + "stops there."),
                new(["Left", "Up"], "Chooses the previous star, wrapping the same way."),
                new(["Space"], "Chooses the focused star, or takes it back with `Clearable` when it is the chosen "
                    + "one."),
            ],
            Labeling =
            [
                "The `Label` is the `fieldset`'s legend and names the group. Each star is a native radio named \"1 "
                    + "star\", \"2 stars\" and so on, unless `StarLabel` names them.",
            ],
            Announcements =
            [
                "Screen readers read a `ReadOnly` rating as read only.",
            ],
            Accessibility =
            [
                "A `fieldset` with `role=\"radiogroup\"`. Helper text and messages describe the whole rating, an "
                    + "error sets `aria-invalid` on it, and `Required` sets `aria-required` and treats a rating of 0 "
                    + "as missing.",
                "Chosen stars are filled and the rest are outlines, so the rating doesn't rely on color. The gold of "
                    + "the chosen stars turns darker on a light page to keep 3:1 contrast with the background. The "
                    + "focus ring goes around the focused star.",
                "The preview shows only on devices that can hover, such as a mouse, so on a touch screen a tapped star "
                    + "doesn't stay lit.",
                "In forced-colors mode, such as Windows high contrast, the chosen stars take the system's highlight.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Partial),
            BeforeInteractive =
            [
                "Native radios give the tab stop and the arrow keys, so choosing works before the page is interactive, "
                    + "and a static form posts the chosen number. The stars follow their radios, so they show the "
                    + "choice too.",
            ],
            Target = new("radiogroup", "Rate the ridge loop"),
            Blocks = ["tb-rating"],
            DataParts =
            [
                new("label", "The rating's legend."),
                new("stars", "The row of stars."),
                new("star", "One star, holding its radio."),
                new("input", "A star's radio."),
                new("helper", "The help text under the rating."),
                new("error", "The validation message, shown while the value is invalid."),
            ],
            TestingTip = "Each star is a native radio, so choose one with "
                + "`GetByRole(AriaRole.Radio, new() { Name = \"4 stars\" }).CheckAsync()` in Playwright. In bUnit, "
                + "`Change(true)` on a star's `input` chooses it.",
        },
        new("select", "Select", ExampleCategory.Inputs,
            "Picks one or several options from a list that opens under the field.")
        {
            FullWidth = true,
            Sections =
            [
                new("try-it", "Try it", "Use the controls to see what each parameter does.",
                    typeof(SelectTryExample))
                {
                    Controls = new("<TbSelect Label=\"Try this select\"{controls} @bind-Value=\"_value\">\n    "
                    + "<TbOption Value=\"@(\"trail\")\">Trail</TbOption>\n    <TbOption "
                    + "Value=\"@(\"road\")\">Road</TbOption>\n</TbSelect>",
                        ExampleControl.Text(nameof(TbSelect<string>.Placeholder), "Choose one"),
                        ExampleControl.Text(nameof(TbSelect<string>.HelperText)),
                        ExampleControl.Switch(nameof(TbSelect<string>.Disabled)),
                        ExampleControl.Switch(nameof(TbSelect<string>.Required)),
                        ExampleControl.Switch(nameof(TbSelect<string>.Clearable))),
                },
                new("options-and-value", "Options and value", "Each `TbOption` is an option, `Placeholder` shows while "
                    + "nothing is chosen, and `Disabled` on an option turns off that option alone. A value can be any "
                    + "type, such as an enum.", typeof(SelectExample)),
                new("clearing", "Clearing", "`Clearable` adds a button that clears the choice. A nullable value clears "
                    + "to `null`.", typeof(SelectClearExample)),
                new("multiple", "Multiple, Select all and chips", "`TbMultiSelect` binds a list. `SelectAll` starts the "
                    + "list with an option that chooses every enabled option, and is selected once they all are; "
                    + "choosing it then deselects them all. It hides while the filter has text. `Chips` shows each "
                    + "choice as a chip with a remove button.", typeof(SelectMultipleExample)),
                new("groups", "Groups", "`TbOptionGroup` groups options under a heading. The arrows and typing move "
                    + "through every group.", typeof(SelectGroupsExample)),
                new("rich-options", "Options with more than text", "An option's content can hold more than text, such "
                    + "as a chip, and the field shows that content once it's chosen. Its words name the option. `Text` "
                    + "is for content without words, such as an image, and replaces that name, so include any words "
                    + "the option shows.", typeof(SelectContentExample)),
                new("filtering", "Filtering", "`Filterable` adds a box above the list. Matching ignores case and "
                    + "accents, and groups with no matches hide.", typeof(SelectFilterExample)),
                new("options-that-load-late", "Options that load late", "In a single select, a value no option has "
                    + "still shows: as its own text, or as whatever `UnmatchedText` returns. That lets a chosen key "
                    + "show its name before the options arrive. A multi-select shows only the chosen values whose "
                    + "options are there. `OpenChanged` tells you when the list opens, so you can load the options "
                    + "then. The demo shows a disabled option while they load and sets `Error` if they don't. To see "
                    + "the failure, check the box before opening the list.",
                    typeof(SelectLateExample)),
                new("empty-list", "Empty list", "A select with no options says so in its list, in text as readable as "
                    + "the options. Set `Labels.Empty` to say why.", typeof(SelectEmptyExample)),
                new("many-options", "Many options", "A long list scrolls, and `Filterable` lets people search it. A "
                    + "long option widens the list, and once chosen it's cut off with an ellipsis. When the options "
                    + "come from a search as someone types, use an "
                    + "[autocomplete](components/autocomplete). Choosing draws again only the options whose state "
                    + "changed, but an option with `ChildContent` draws again whenever the select does, since its "
                    + "content can't be compared. "
                    + "`AttachOnFirstUse` waits to set up a select's script "
                    + "until it is first focused or pointed at, or its list is opened from code. That saves a call to "
                    + "the browser for each select on a page with many of them, such as a select on every row.",
                    typeof(SelectManyExample)),
                new("items", "Options from a list", "`Items` makes the options from a list, such as one loaded from "
                    + "a database, in place of `TbOption` children, and renders them without a component each. "
                    + "`ItemText` names each one, `ItemTemplate` shows more than text, and `ItemDisabled` turns some "
                    + "off. `TbMultiSelect` takes the same parameters. Past 100 items, only the options in view are in "
                    + "the page, so a list of thousands opens quickly; the keys, type-ahead and the filter still reach "
                    + "every one.", typeof(SelectItemsExample)),
                new("comparer", "Values loaded again", "`Comparer` decides whether two values are the same, so a value "
                    + "from another query, equal only by id, still matches its option.", typeof(SelectComparerExample)),
                new("required", "Required", "`Required` shows a marker. Once the select is changed or its form "
                    + "validates, a message shows while nothing is chosen. `RequiredMessage` replaces the default, "
                    + "which uses the label.", typeof(SelectRequiredExample)),
                new("validation", "Validation", "`Validation` checks the choice once it is changed or its form "
                    + "validates.", typeof(SelectValidationExample)),
                new("error", "Errors from your app", "`Error` shows a message your app sets, such as one from a server. "
                    + "Until your app clears it, the form won't submit, so clear it when the choice changes.",
                    typeof(SelectErrorExample)),
                new("read-only-and-disabled", "Read-only and disabled", "`ReadOnly` keeps the select focusable, but it "
                    + "doesn't open and its chips can't be removed. `Disabled` takes it out of the tab order.",
                    typeof(SelectStatesExample)),
                new("size-and-variant", "Size and variant", "`Variant` is `Outlined` or `Filled`, and `Size` is "
                    + "`Small`, `Medium` or `Large`.", typeof(SelectSizeExample)),
                new("labels", "Built-in text", "`Labels` replaces the select's built-in text: the filter box's "
                    + "name, the no-results message, the clear button, the chip removal announcement and the \"Select "
                    + "all\" option.", typeof(SelectLabelsExample)),
            ],
            Api = [typeof(TbSelect<>), typeof(TbMultiSelect<>), typeof(TbOption<>), typeof(TbOptionGroup),
                typeof(SelectLabels), typeof(FieldClasses)],
            WhenToUse =
            [
                "To choose one option, or several, from a known list too long to show at once.",
                "With `Filterable`, for a list long enough that the user would rather search it than scroll.",
                "[Choosing a list field](guides/forms#choosing-a-list-field) compares it with the other fields that "
                    + "pick from a list.",
            ],
            WhenNotToUse =
            [
                "For a few options that fit on the page, use a [radio group](components/radio-group) or a "
                    + "[checkbox group](components/checkbox-group), which show every choice at once.",
                "When the user may type a value the list doesn't have, or the options come from a search as they "
                    + "type, use an [autocomplete](components/autocomplete).",
                "For a list that stays open on the page, use a [list box](components/listbox).",
            ],
            Related = ["autocomplete", "listbox", "radio-group", "checkbox-group", "text-field"],
            Pattern = new("combobox", "https://www.w3.org/WAI/ARIA/apg/patterns/combobox/"),
            Keyboard =
            [
                new(["Down", "Up", "Enter", "Space"], "Opens the list.") { When = "While closed" },
                new(["Home", "End"], "Opens the list on the first or last option.") { When = "While closed" },
                new(["A letter"], "Opens the list on an option that starts with what was typed.")
                {
                    When = "While closed",
                },
                new(["Down", "Up"], "Moves to the next or previous option.") { When = "While open" },
                new(["Home", "End"], "Moves to the first or last option.") { When = "While open" },
                new(["Page Up", "Page Down"], "Moves up or down by as many options as the list shows.") { When = "While open" },
                new(["A letter"], "Moves to an option that starts with what was typed. Pressed again quickly, it moves "
                    + "to the next one that starts with that letter. A Space typed mid-search adds to the search "
                    + "rather than choosing.")
                {
                    When = "While open",
                },
                new(["Enter", "Space"], "Chooses the active option. A multi-select stays open as options are chosen.")
                {
                    When = "While open",
                },
                new(["Alt+Up"], "Closes the list; a single select first chooses the active option.")
                {
                    When = "While open",
                },
                new(["Escape"], "Closes the list.") { When = "While open" },
                new(["Tab"], "Closes the list and moves focus on; a single select first chooses the active option.")
                {
                    When = "While open",
                },
                new(["Backspace"], "Removes the last chosen option.") { When = "With `Chips`" },
                new(["A letter"], "While closed, opens the list and starts the filter.")
                {
                    When = "With `Filterable`",
                },
                new(["A letter"], "While open, types in the filter and narrows the list.")
                {
                    When = "With `Filterable`",
                },
                new(["Down", "Up"], "Moves through the matching options.") { When = "With `Filterable`" },
                new(["Home", "End"], "Moves within the filter's text.") { When = "With `Filterable`" },
                new(["Enter"], "Chooses the active option.") { When = "With `Filterable`" },
                new(["Escape"], "Closes the list and returns focus to the select.") { When = "With `Filterable`" },
            ],
            Labeling =
            [
                "The combobox is named by the `Label`.",
                "The clear button is named \"Clear\" and described by the field's label, so a screen reader says "
                    + "which field it clears.",
                "An option's words name it, chosen or not. Content without words, such as an image, is named by its "
                    + "`Text`; with neither, it gets a warning in the browser console.",
                "A `TbOptionGroup` is a `group` named by its `Label`.",
                "The \"Select all\" option is named by `Labels.SelectAll`.",
                "With `Filterable`, the popup is a `dialog` named by the `Label`, and the filter box by "
                    + "`Labels.Filter`.",
            ],
            Announcements =
            [
                "On focus, its label, the value it shows and whether its list is open, then its helper text or error.",
                "Focus stays on the combobox, which points to the active option with `aria-activedescendant`, so a "
                    + "screen reader reads each option as it becomes active.",
                "Choosing in a single select closes the list with focus on the combobox, which then shows the chosen "
                    + "value.",
                "A new validation error from an edit is announced as it appears, and so is an `Error` the app sets, "
                    + "such as one a server returned. A submit's validation errors aren't read out all at once; each "
                    + "field reads its own on focus.",
                "The field is marked `aria-invalid`, and once an error is fixed, its message leaves the live region, "
                    + "so no one comes across it there later.",
                "With `Chips`, a removal is announced.",
                "With `Filterable`, `Labels.NoResults` is announced when nothing matches.",
                "A `ReadOnly` select is heard as read only.",
            ],
            Accessibility =
            [
                "A `combobox` that opens a `listbox` of `option`s. In a single select the chosen option shows a check, "
                    + "not only bolder text.",
                "The clear button sends focus back to the field.",
                "\"Select all\" is an option the arrows reach like any other.",
                "With `Chips`, the remove buttons are for the pointer, and options can still be deselected in the list.",
                "With `Filterable`, the select says it opens a dialog with `aria-haspopup=\"dialog\"`. Opening it "
                    + "moves focus to a filter box above the list, a `combobox` with `aria-autocomplete=\"list\"`.",
            ],
            AppDuties =
            [
                "If a single select's options load as the list opens, give it an `UnmatchedText`, such as a lookup of "
                    + "the value's name, so the chosen value doesn't show as a key such as a `Guid` before they arrive.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "It renders as the browser's own `<select>`, so it still works and posts with its form.",
                "In a single select, a value no option has stays chosen in the native select.",
                "A `ReadOnly` native select puts its choice back whenever the user changes it, and posts. A `Disabled` "
                    + "one doesn't post.",
                "Until the page is interactive, an option with `Text` shows that text instead of its content, since the "
                    + "browser's own list can only show text.",
            ],
            Notes =
            [
                new("A select bound to a value type, such as `Guid`, clears to its default, which shows the option "
                    + "with that value or else the placeholder, and `Required` never fires on it. Bind the nullable "
                    + "form, such as `Guid?`.") { Pitfall = true },
                new("In a single select, choosing the option that's already chosen raises no `ValueChanged`, since "
                    + "the value doesn't change."),
                new("`TbMultiSelect` binds an `IReadOnlyList<T>`. A model that holds a `List<T>` passes it as `Value` "
                    + "and copies the new list back in `ValueChanged`, such as "
                    + "`ValueChanged=\"chosen => _model.Tags = [.. chosen]\"`."),
            ],
            Ready = new("[role=combobox]", "the combobox"),
            Target = new("combobox", "Shipping"),
            Blocks = ["tb-field", "tb-select", "tb-chip"],
            DataParts =
            [
                .. FieldParts,
                new("input", "The combobox the user opens and types in."),
                new("placeholder", "The text shown while nothing is chosen."),
                new("value", "The chosen option's text."),
                new("chips", "The chips that show a multiple select's choices."),
                new("chip", "One chosen option, as a chip."),
                new("chip-remove", "The button that removes a chip's option."),
                new("clear", "The button that clears the choice."),
                new("chevron", "The arrow that shows the list can open."),
                new("popup", "The popup that holds the filter and the list."),
                new("filter", "The box that filters the options."),
                new("listbox", "The list of options."),
                new("group", "A group of options."),
                new("group-label", "A group's heading."),
                new("option", "One option."),
                new("check", "The check beside a chosen option."),
                new("select-all", "The option that chooses every option."),
                new("no-results", "The message shown when the filter matches nothing."),
                new("status", "The message the list shows when it has no options."),
            ],
            Anatomy = """
                <TbSelect Label="…" @bind-Value="_value">
                    <TbOptionGroup Label="…">
                        <TbOption Value="…" />
                    </TbOptionGroup>
                    <TbOption Value="…" />
                </TbSelect>

                <TbMultiSelect Label="…" @bind-Value="_values">
                    <TbOption Value="…" />
                </TbMultiSelect>
                """,
            TestingTip = "To choose options, `await page.Tb().Select(\"Shipping\").ChooseAsync(\"Express (1 day)\")` "
                + "from `Trailblazor.Testing.Playwright` opens the list, picks them and waits for the page, and it "
                + "works on the native `<select>` too. For object values loaded again, such as entities, give the "
                + "select a `Comparer` so they still match their options. The helper works the same on a `Filterable` "
                + "select. For selects that share a label, as in rows of an editable list, pass a locator instead: "
                + "`page.Tb().Select(row.GetByLabel(\"Size\"))`, or `page.Locator(\"#size\")` for one with an "
                + "`id`. In bUnit, `await cut.Tb().Select(\"Shipping\").ChooseAsync(\"Express (1 day)\")` from "
                + "`Trailblazor.Testing` does the same, and `cut.Tb().Select(\"Shipping\").Text` reads what it shows. "
                + "\"Select all\" is an option like the others, so `ChooseAsync(\"Select all\")` chooses it. With "
                + "`AttachOnFirstUse`, the combobox gets `data-ready` only when it's first focused or pointed at, so "
                + "`ChooseAsync` focuses it first.",
            Telemetry =
            [
                "The select also counts `trailblazor.control.used` for each choice, with `trailblazor.input` `pointer` "
                    + "or `keyboard`, or with `trailblazor.action` `remove` for a chip removed or `select_all` for "
                    + "\"Select all\". Only that a choice was made is counted, never which.",
                "An option with its own `TelemetryName`, such as `\"size-large\"`, counts "
                    + "`trailblazor.control.used` each time it is chosen, with `trailblazor.input` too. Its list "
                    + "renders hidden, so it counts uses only; its value is never recorded.",
            ],
        },
        new("tree-select", "Tree select", ExampleCategory.Inputs,
            "A form field that opens a tree under it for choosing one item, or several, from a hierarchy, such as a "
                + "trail within its region.")
        {
            WhenToUse =
            [
                "Use it when the choices nest, such as folders, categories or places, and seeing where an item sits "
                    + "helps tell it apart.",
            ],
            WhenNotToUse =
            [
                "For a flat list of choices, use a [select](components/select).",
                "For a tree that stays on the page, such as a folder pane, use a [tree view](components/tree-view).",
            ],
            Related = ["select", "tree-view", "autocomplete"],
            Sections =
            [
                new("choose-one", "Choosing one item", "`Items`, `KeySelector`, `ItemText` and `ChildrenSelector` "
                    + "build the tree as they do for a tree view, and `@bind-Value` holds the chosen item's key. "
                    + "Choosing an item, by click, Enter or Space, closes the tree and shows its text in the field; "
                    + "choosing the item already chosen just closes it. The key type's default, such as 0 for an "
                    + "`int`, means nothing is chosen. `IsSelectable` lets headings such as a region open without "
                    + "being chosen. `Searchable` adds a search that keeps the items whose text contains what's typed, "
                    + "with the items above and below them, once typing pauses. `LoadChildren` loads a branch when it opens, and the "
                    + "search covers only the branches loaded so far. `UnmatchedText` names a value whose item hasn't "
                    + "loaded, such as a saved choice in a branch not yet opened. `Clearable` adds a button that "
                    + "clears the choice. `@bind-Open` opens or closes the tree from code, though a disabled or "
                    + "read-only field stays closed. `Labels` words the search box, its no-match message, the clear "
                    + "button and the tree's own text.", typeof(TreeSelectExample)),
                new("choose-several", "Choosing several items", "`TbMultiTreeSelect` binds a list of keys. Its tree "
                    + "has a checkbox on each row, and stays open while items are checked by click, Enter or Space; "
                    + "the field shows their text, separated by commas. Past `MaxShown` items (3 unless set), the "
                    + "field counts the rest, as in \"4 more\", and `Labels.More` words the count. Checking an item "
                    + "checks the items inside it, and a parent shows whether all, some or none of them are checked; "
                    + "`CascadeChecks=\"false\"` "
                    + "checks each row on its own. When `IsSelectable` leaves out a heading, it has a checkbox only "
                    + "while checks cascade; the checkbox checks the items inside it, including those that load "
                    + "later, but the heading isn't part of the value. The value lists keys in tree order, except "
                    + "that with `LoadChildren` the keys of loaded items come after the rest. Search, "
                    + "`LoadChildren` and `UnmatchedText` work as they do for one item.",
                    typeof(MultiTreeSelectExample)),
            ],
            Api =
            [
                typeof(TbTreeSelect<,>), typeof(TbMultiTreeSelect<,>), typeof(TreeSelectLabels), typeof(FieldClasses),
            ],
            Keyboard =
            [
                new(["Enter", "Space"], "Opens the tree, with focus in its search when it has one.")
                    { When = "On the field" },
                new(["Down"], "Moves into the tree, to the first match of what's typed, or else to the row Tab would "
                    + "reach: the last one focused, the chosen item or the first one.") { When = "In the search" },
                new(["Tab"], "Moves from the search into the tree.") { When = "In the search" },
                new(["Up", "Down", "Left", "Right", "Home", "End"], "Move through the tree and open or close its "
                    + "branches, as in a tree view.") { When = "In the tree" },
                new(["Enter", "Space"], "Chooses the focused item, closes the tree and returns to the field.")
                    { When = "In the tree, choosing one item" },
                new(["Enter", "Space"], "Checks or unchecks the focused item, and the tree stays open.")
                    { When = "In a multi tree select's tree" },
                new(["Escape"], "Closes the tree without choosing, and returns to the field.") { When = "In the tree" },
            ],
            Notes =
            [
                new("After changing `Items`, or the children it holds, in place, call `RefreshAsync`. A new collection, "
                    + "or a branch that loads, is read again by itself.") { Pitfall = true },
            ],
            Labeling =
            [
                "The clear button is named \"Clear\", or `Labels.Clear`, and described by the field's label, so a "
                    + "screen reader says which field it clears.",
                "The field's `Label` and the chosen item's text name its button together, as they would a select; "
                    + "in a multi tree select, the first `MaxShown` checked items' text and a count of the rest take the "
                    + "chosen item's place.",
            ],
            Announcements =
            [
                "The button has `aria-haspopup=\"dialog\"` and `aria-expanded`; the tree is a `tree` of `treeitem`s, "
                    + "named by the label, with the chosen item `aria-selected`. In a multi tree select, each row has "
                    + "`aria-checked`, which is `mixed` for a parent with only some of its items checked.",
            ],
            Accessibility =
            [
                "Focus moves into the tree as it opens and back to the field as it closes, by a choice, Escape or a "
                    + "click outside.",
                "The clear button sends focus back to the field.",
                "A read-only field can still be focused and read, but opens nothing; a disabled one can't be focused.",
                "A search that matches nothing shows \"No matches\" and announces it.",
                "A required field with nothing chosen is invalid, and says so once the form validates, as any field "
                    + "does.",
            ],
            BeforeInteractive =
            [
                "The button shows the chosen items and already opens the list, but choosing starts working once "
                    + "the page is interactive, and a static form posts nothing for it.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            Ready = new("[data-part=input]", "the field's button"),
            Target = new("button", "Trail Choose a trail"),
            Blocks = ["tb-tree-select"],
            DataParts =
            [
                new("input", "The button that opens the tree and shows the choice."),
                new("value", "The chosen item's text, or the placeholder."),
                new("search", "The search field above the tree."),
                new("no-results", "The message shown when the search matches nothing."),
                new("clear", "The button that clears the choice, with `Clearable`."),
            ],
            Anatomy = """
                <TbTreeSelect Label="…" Items="…" KeySelector="…" ItemText="…" @bind-Value="_key" />
                """,
            TestingTips =
            [
                "In Playwright, `await page.Tb().TreeSelect(\"Trail\").ChooseAsync(\"River valley\", \"Cedar "
                    + "Flats\")` from `Trailblazor.Testing.Playwright` waits for the field's script, opens the tree "
                    + "and the rows along the path, and chooses the item. `CheckAsync`, `UncheckAsync` and "
                    + "`SearchAsync` work the same way, and `TextAsync()` reads what the field shows.",
                "In bUnit, `await cut.Tb().TreeSelect(\"Trail\").ChooseAsync(\"River valley\", \"Cedar Flats\")` "
                    + "from `Trailblazor.Testing` does the same, and `Text` reads the field.",
                "The field's button is named by its label and its choice together, as in \"Trail Cedar Flats\", so "
                    + "a locator that expects the exact name \"Trail\" misses it once something is chosen; "
                    + "`TreeSelect(label)` finds it by the label alone.",
            ],
        },
        new("autocomplete", "Autocomplete", ExampleCategory.Inputs,
            "A text field that suggests values as the user types, from a list or an async search.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it when the options come from a search as someone types, or when they may type a value that isn't "
                    + "in the list.",
                "[Choosing a list field](guides/forms#choosing-a-list-field) compares it with the other fields that "
                    + "pick from a list.",
            ],
            WhenNotToUse =
            [
                "For a known list where someone must choose one of the options, use a [select](components/select).",
                "For a list that stays open on the page, use a [list box](components/listbox).",
                "For free text with no suggestions, use a [text field](components/text-field).",
            ],
            Related = ["select", "listbox", "text-field", "form"],
            Pattern = new("combobox", "https://www.w3.org/WAI/ARIA/apg/patterns/combobox/"),
            Sections =
            [
                new("search", "Search", "`Search` finds the suggestions as someone types, and is canceled when they "
                    + "type again. `ItemText` turns a suggestion into text, and `ItemTemplate` draws it in the list.",
                    typeof(AutocompleteExample)),
                new("items", "A fixed list", "`Items` is a list you already have, filtered by what someone types. A "
                    + "long suggestion widens the list.", typeof(AutocompleteItemsExample)),
                new("many-items", "Many items", "A long list scrolls. It shows the first 50 matches; type to narrow it "
                    + "down, or set `MaxItems` to `null` to show them all.", typeof(AutocompleteManyExample)),
                new("free-text", "Free text", "With `FreeText`, typed text that isn't a suggestion becomes the value, "
                    + "converted to the value's type. Without it, leaving the field puts back the chosen value's text.",
                    typeof(AutocompleteFreeTextExample)),
                new("search-states", "Searching, no results and failure", "The list says when a search is running, when "
                    + "nothing matches, and when a search fails, which also raises `OnSearchFailed`. The field stays "
                    + "usable, and typing again searches again. Type a campsite's name, then something that matches "
                    + "nothing. To see a failure, check the box and type again.",
                    typeof(AutocompleteStatesOfSearchExample)),
                new("limits", "Minimum characters and maximum items", "`MinCharacters` sets how much someone must type "
                    + "before suggestions show, and `MaxItems` caps how many the list shows. It defaults to 50; set it to "
                    + "`null` to show every match.",
                    typeof(AutocompleteLimitsExample)),
                new("debounce", "Search delay", "`DebounceDelay` sets how long someone must stop typing before `Search` "
                    + "runs. It defaults to 200 milliseconds. Set it to zero to search immediately.",
                    typeof(AutocompleteDelayExample)),
                new("text-changed", "Acting on what is typed", "`OnTextChanged` gives the field's text once someone "
                    + "stops typing for `DebounceDelay`, and at once when they choose a suggestion or clear the field. "
                    + "Use it to filter other content while they type, as this list does.",
                    typeof(AutocompleteFilterExample)),
                new("clearing", "Clearing", "`Clearable` shows a button that clears the field, and a nullable value "
                    + "clears to `null`.", typeof(AutocompleteClearExample)),
                new("start", "Start content", "`Start` puts content before the input, usually an icon.",
                    typeof(AutocompleteStartExample)),
                new("required", "Required", "`Required` shows a marker. Once the field is changed or its form "
                    + "validates, a message shows while it's empty. `RequiredMessage` replaces the default, which uses "
                    + "the label.", typeof(AutocompleteRequiredExample)),
                new("validation", "Validation", "`Validation` checks the value once it is changed or its form "
                    + "validates.",
                    typeof(AutocompleteValidationExample)),
                new("error", "Errors from your app", "`Error` shows a message your app sets, such as one from a server. "
                    + "Until your app clears it, the form won't submit, so clear it when the value changes.",
                    typeof(AutocompleteErrorExample)),
                new("read-only-and-disabled", "Read-only and disabled", "A `ReadOnly` field won't open its list or let the "
                    + "user change the value, and hides the clear button. "
                    + "`Disabled` takes the field out of the tab order and the post.",
                    typeof(AutocompleteStatesExample)),
                new("size-and-variant", "Size and variant", "`Variant` is `Outlined` or `Filled`, and `Size` is "
                    + "`Small`, `Medium` or `Large`.", typeof(AutocompleteSizeExample)),
                new("labels", "Built-in text", "`Labels` replaces the words the field shows and reads out: the clear "
                    + "button's name, the searching, no-results and failure messages, the prompt to type, and the "
                    + "number of suggestions.", typeof(AutocompleteLabelsExample)),
            ],
            Api = [typeof(TbAutocomplete<>), typeof(AutocompleteLabels), typeof(FieldClasses)],
            Keyboard =
            [
                new(["Down"], "Opens the list. If it already holds suggestions, also moves to the first.")
                    { When = "While closed" },
                new(["Alt+Down"], "Opens the list without moving to a suggestion.") { When = "While closed" },
                new(["Up"], "Opens the list. If it already holds suggestions, also moves to the last.")
                    { When = "While closed" },
                new(["Enter"], "Handles what was typed the way leaving the field does. In a form, also submits the "
                    + "form.") { When = "While closed" },
                new(["Down", "Up"], "Moves to the next or previous suggestion.") { When = "While open" },
                new(["Enter"], "Chooses the active suggestion. With none active, closes the list and does what Enter "
                    + "does while closed.") { When = "While open" },
                new(["Escape"], "Closes the list, wherever focus is.") { When = "While open" },
            ],
            Labeling =
            [
                "The input is a `combobox` named by its `Label`.",
                "The clear button is named \"Clear\" and described by the field's label, so a screen reader says "
                    + "which field it clears.",
            ],
            Announcements =
            [
                "Focus stays in the input, which points to the active option with `aria-activedescendant`, so a screen "
                    + "reader reads each suggestion as it becomes active.",
                "While a search runs the list is `aria-busy`. When it finishes, the number of suggestions is announced "
                    + "if it changed, so typing doesn't bring an announcement with every key.",
                "A search that fails reads out its message.",
            ],
            Notes =
            [
                new("`Comparer` decides whether two values are the same, such as entities matched by id, so a value "
                    + "loaded again still marks its suggestion."),
                new("A press anywhere in the box outside its controls, such as its padding, focuses the "
                    + "input."),
                new("With `FreeText`, typed text becomes the value when the user presses Enter or leaves the field. "
                    + "To act on the text as it's typed, such as filtering a list, handle `OnTextChanged`."),
            ],
            Accessibility =
            [
                "The input has `aria-autocomplete=\"list\"` and controls a `listbox` of `option`s. Up and Down move "
                    + "through the list, not the caret.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "It is a plain text input, so a static form still posts what was typed.",
            ],
            Ready = new("[role=combobox]", "the input"),
            Target = new("combobox", "Trailhead"),
            Blocks = ["tb-field", "tb-select", "tb-autocomplete"],
            DataParts =
            [
                .. FieldParts,
                new("input", "The combobox the user types in."),
                new("clear", "The button that clears the text."),
                new("listbox", "The list of suggestions."),
                new("option", "One suggestion."),
                new("status", "The message shown while suggestions load, when none match, when a search fails, or "
                    + "before someone has typed enough. It's an option that can't be chosen."),
            ],
            TestingTip = "To choose a suggestion, `await page.Tb().Autocomplete(\"Trailhead\").ChooseAsync(\"pine\", "
                + "\"Pine Hollow North ridge\")` from `Trailblazor.Testing.Playwright` types the text, waits for the "
                + "search, and clicks the option by its name. With a `Search`, the browser waits out `DebounceDelay`, "
                + "so in bUnit, which has no browser, `Search` runs and `OnTextChanged` fires on each input. Without a "
                + "`Search`, `OnTextChanged` fires once `DebounceDelay` has passed on the app's `TimeProvider`, so advance a "
                + "`FakeTimeProvider` past it, or set it to `TimeSpan.Zero`. Only the first `MaxItems` matches, 50 by "
                + "default, become options, so type enough to bring the one you want into the list.",
        },
        new("tag-input", "Tag input", ExampleCategory.Inputs,
            "A field where people type any number of short labels, such as a trail's features, each shown as a chip.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it when people make up their own short labels, such as keywords, features or email addresses.",
            ],
            WhenNotToUse =
            [
                "To choose several from a known list, use a [multi-select](components/select) with `Chips`.",
                "For one value people type, use a [text field](components/text-field).",
            ],
            Related = ["select", "chip", "text-field", "autocomplete"],
            RightToLeftKeys = "Right moves from the start of the input to the last tag and back along the tags, and "
                + "Left moves forward.",
            Sections =
            [
                new("basic", "Tag input", "`@bind-Value` is the list of tags, in the order they were added. Enter or "
                    + "a comma adds what was typed, trimmed, and leaving the field adds it too. Each tag shows as a "
                    + "chip with a remove button, and Backspace in the empty input removes the last one.",
                    typeof(TagInputExample)),
                new("rules", "Separators, limits and checks", "`Separators` sets the characters that add a tag, as "
                    + "Enter does; pasted text is split at them and at line breaks. A tag that's already there, one "
                    + "past `MaxTags`, or one `TagValidation` refuses stays in the input with a message. Tags match "
                    + "ignoring case unless `Comparer` says otherwise.", typeof(TagInputRulesExample)),
                new("required", "Required", "`Required` treats an empty list as missing. Enter in the empty input "
                    + "submits the form, so a filled-in form needs no extra click.", typeof(TagInputFormExample)),
                new("states", "Look and states", "`Variant` and `Size` set the box's look, as on a text field. "
                    + "`ReadOnly` shows the tags without remove buttons, and `Disabled` turns the field off.",
                    typeof(TagInputStatesExample)),
            ],
            Api = [typeof(TbTagInput), typeof(TagInputLabels), typeof(FieldClasses)],
            Keyboard =
            [
                new(["Enter"], "Adds what was typed as a tag. In an empty input, submits the form."),
                new([","], "Adds what was typed as a tag, as does any other character in `Separators`."),
                new(["Backspace"], "In an empty input, removes the last tag."),
                new(["Left"], "At the start of the input, moves to the last tag's remove button."),
                new(["Left", "Right"], "Moves between the tags' remove buttons; Right from the last returns to the "
                    + "input.") { When = "On a tag" },
                new(["Home", "End"], "Home moves to the first tag, and End back to the input.") { When = "On a tag" },
                new(["Backspace"], "Removes the tag and moves to the one before it.") { When = "On a tag" },
                new(["Delete", "Enter", "Space"], "Removes the tag and moves to the one after it, or the input.")
                    { When = "On a tag" },
                new(["Tab"], "Leaves the tags for the input, and the input for the rest of the page. The remove "
                    + "buttons aren't tab stops of their own."),
            ],
            Labeling =
            [
                "The `Label` names the input and the list of tags. Each remove button is named \"Remove\" and its "
                    + "tag, such as \"Remove Shade\", unless `Labels.Remove` replaces it.",
            ],
            Announcements =
            [
                "Adding a tag reads out \"Shade added\", and removing one \"Shade removed\". A refused tag's message "
                    + "is read out as the field's error.",
                "The input is described by a hint, \"Enter adds a tag. Left Arrow moves to the tags.\", which "
                    + "`Labels.Hint` replaces.",
            ],
            Accessibility =
            [
                "The tags are a list, so screen readers say how many there are, and each tag's remove button is a "
                    + "native button. Helper text and messages describe the input, an error sets `aria-invalid` on "
                    + "it, and `Required` sets `aria-required`.",
                "A long tag ends in an ellipsis, and the box grows a row at a time as tags fill it.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "The tags render, and a static form posts them, but adding and removing tags start working once the "
                    + "page is interactive. Until then the tags have no remove buttons.",
            ],
            Ready = new("[data-part=input]", "the input"),
            Target = new("textbox", "Trail features"),
            Blocks = ["tb-tag-input", "tb-chip"],
            DataParts =
            [
                new("label", "The field's visible label."),
                new("control", "The box around the tags and the input."),
                new("tags", "The list of tags."),
                new("tag", "One tag."),
                new("remove", "A tag's remove button."),
                new("input", "The text box that tags are typed into."),
                new("helper", "The help text under the field."),
                new("error", "The validation message, or why a typed tag was refused."),
            ],
            TestingTip = "Add a tag with `page.Tb().TagInput(\"Trail features\").AddAsync(\"Shade\")`, which types it "
                + "and presses Enter once the field's keys work, and check the tags with "
                + "`Expect(tags.Tags).ToHaveTextAsync([...])`. In bUnit, `cut.Tb().TagInput(\"Trail features\")` has "
                + "`AddAsync`, `RemoveAsync` and `Tags`.",
            Telemetry =
            [
                "It also counts `trailblazor.control.used` with `trailblazor.action` `add` for each addition and "
                    + "`remove` for each removal. The tags themselves are never recorded.",
            ],
        },
        new("color-picker", "Color picker", ExampleCategory.Inputs,
            "Chooses a color by typing it, picking a swatch, using the custom picker, or taking it from the screen.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it when someone can choose any color, or one from a palette, and the value is a hex color.",
            ],
            WhenNotToUse =
            [
                "For a few named colors, a [radio group](components/radio-group) or a [select](components/select) may "
                    + "be clearer, since people choose by name.",
            ],
            Related = ["text-field", "radio-group", "select", "form"],
            RightToLeftKeys = "On the swatches, Left moves to the next one and Right to the previous one. On a slider, "
                + "Left raises the value and Right lowers it.",
            Sections =
            [
                new("palette", "Palette and value", "The value is a hex string. `Palette` lists the swatches to "
                    + "offer, each as `#rrggbb`, `#rgb` or `rgb()`. `SwatchName` names each one for screen readers, and "
                    + "defaults to the color as written in `Palette`.", typeof(ColorPickerExample)),
                new("opacity", "Opacity", "`Alpha` adds an opacity slider and also allows `#rgba` and `#rrggbbaa`. A "
                    + "color that isn't fully opaque gets two more digits for its opacity, as in this "
                    + "`#2da44e66`.", typeof(ColorPickerAlphaExample)),
                new("palette-only", "Palette only", "`PaletteOnly` hides the custom picker, leaving the swatches. It "
                    + "doesn't limit the value: the text box and eyedropper still take any color, so add `Validation` "
                    + "if only the palette's colors are allowed.",
                    typeof(ColorPickerPaletteOnlyExample)),
                new("eyedropper", "Eyedropper", "A button takes a color from the screen, in browsers that have an "
                    + "eyedropper. `Eyedropper=\"false\"` leaves it out.", typeof(ColorPickerEyedropperExample)),
                new("clearing", "Clearing", "`Clearable` shows a button that sets the value to `null` and puts focus "
                    + "back in the text box. `Placeholder` shows while there's no color.",
                    typeof(ColorPickerClearExample)),
                new("invalid-text", "Invalid text", "The text box takes `#rgb`, `#rrggbb` or `rgb()`, and the `#` is "
                    + "optional. For other text, a message gives the format to use, the box is marked invalid and the "
                    + "value stays as it was. Emptying the box makes the value `null`.",
                    typeof(ColorPickerInvalidExample)),
                new("required", "Required", "`Required` shows a marker. Once the field is changed or its form "
                    + "validates, a message shows while there's no color. `RequiredMessage` replaces the default, which "
                    + "uses the label.", typeof(ColorPickerRequiredExample)),
                new("validation", "Validation", "`Validation` checks the color once it is changed or its form "
                    + "validates.", typeof(ColorPickerValidationExample)),
                new("error", "Errors from your app", "`Error` shows a message your app sets, such as one from a server. "
                    + "Until your app clears it, the form won't submit, so clear it when the color changes.",
                    typeof(ColorPickerErrorExample)),
                new("read-only-and-disabled", "Read-only and disabled", "`ReadOnly` keeps the color from changing: the "
                    + "text can't be edited, the picker buttons turn off and the clear button hides. `Disabled` takes "
                    + "the field out of the tab order and the post.", typeof(ColorPickerStatesExample)),
                new("size-and-variant", "Size and variant", "`Variant` is `Outlined` or `Filled`, and `Size` is "
                    + "`Small`, `Medium` or `Large`.", typeof(ColorPickerSizeExample)),
                new("labels", "Built-in text", "`Labels` replaces the words the field shows and reads out, such as the "
                    + "picker button's name, the clear button's name and the message for text it can't read.",
                    typeof(ColorPickerLabelsExample)),
            ],
            Api = [typeof(TbColorPicker), typeof(ColorPickerLabels), typeof(FieldClasses)],
            Keyboard =
            [
                new(["Enter", "Space"], "Opens the picker.") { When = "On the picker button" },
                new(["Left", "Right", "Up", "Down"], "On the swatches, moves between them and chooses one, as native "
                    + "radios do. On a slider, changes its value.") { When = "In the picker" },
                new(["Space"], "On a swatch, chooses it.") { When = "In the picker" },
                new(["Home", "End"], "On a slider, goes to its lowest or highest value.") { When = "In the picker" },
                new(["Page Up", "Page Down"], "On a slider, changes its value by a larger amount.")
                    { When = "In the picker" },
                new(["Up", "Down"], "On a Red, Green or Blue box, steps the number.") { When = "In the picker" },
                new(["Escape"], "Closes the picker.") { When = "In the picker" },
            ],
            Labeling =
            [
                "The text box is named by the `Label`.",
                "The picker is a `dialog`, and its swatches are native radios in a `radiogroup`, each named by "
                    + "`SwatchName`.",
                "The clear button is named \"Clear\" and described by the field's label, so a screen reader says which "
                    + "field it clears.",
                "The dialog and the picker button are named by `Labels.Open`, the radio group by `Labels.Swatches`, and "
                    + "the eyedropper button, sliders and Red, Green and Blue boxes by the rest of `Labels`.",
            ],
            Announcements =
            [
                "A color the text box can't read marks it `aria-invalid`, and the message says the format to use.",
                "A validation message that appears as someone changes the color is read out once, and a failed submit "
                    + "reads none out, as on a [text field](components/text-field).",
                "An `Error` your app sets is read out once when it appears, even after a submit.",
            ],
            Notes =
            [
                new("A press around the text box, such as on the field's padding, focuses it, unless it lands on a button."),
                new("During a drag on the pad, the thumb follows the pointer at once, but `ValueChanged` is raised only a "
                    + "few times a second, and once more when the drag ends. The sliders below the pad raise `ValueChanged` "
                    + "at most every 50 ms, and once more when they stop."),
            ],
            Accessibility =
            [
                "The saturation and brightness pad is for the pointer; the Hue, Saturation, Brightness and Opacity "
                    + "sliders and the Red, Green and Blue boxes give keyboard and screen reader users the same "
                    + "control.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "It is a plain text box that only takes `#rrggbb` (or `#rrggbbaa` with `Alpha`), so a static form posts "
                    + "a hex color, as typed.",
            ],
            Ready = new("[data-part=pad]", "the button that opens it and the color pad"),
            Target = new("textbox", "Trail marker"),
            Blocks = ["tb-field", "tb-color-picker"],
            DataParts =
            [
                .. FieldParts,
                new("input", "The box that holds the color as text."),
                new("swatch-preview", "The swatch beside the box that shows the color."),
                new("clear", "The button that clears the color."),
                new("eyedropper", "The button that picks a color from the screen."),
                new("pad", "The square that sets saturation and brightness."),
                new("hue", "The hue slider."),
                new("saturation", "The saturation slider."),
                new("brightness", "The brightness slider."),
                new("alpha", "The opacity slider."),
                new("red", "The red channel box."),
                new("green", "The green channel box."),
                new("blue", "The blue channel box."),
                new("swatches", "The palette of preset colors."),
                new("swatch", "One preset color."),
            ],
            TestingTip = "`await page.Tb().ColorPicker(\"Trail marker\").ChooseAsync(\"#d1242f\")` from "
                + "`Trailblazor.Testing.Playwright` types the color and waits until the field has taken it; for "
                + "pickers that share a label, pass a locator, as `ColorPicker(row.GetByLabel(\"Color\"))`. In bUnit, "
                + "set `FakeBrowserService.PickedColor` to test the eyedropper. `@bind-Open` opens the color panel from "
                + "code.",
        },
        new("file-upload", "File upload", ExampleCategory.Inputs,
            "Chooses files by clicking or dropping them on a drop zone, and lists them with a way to remove each.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it when someone needs to send files from their device.",
            ],
            WhenNotToUse =
            [
                "For a link to a file that's already online, use a [text field](components/text-field) of type `Url`.",
            ],
            Related = ["text-field", "form"],
            Sections =
            [
                new("drop-zone", "Drop zone and value", "Click the zone or drop files on it. `Accept` lists the types "
                    + "to take, `Multiple` takes more than one file, and the value is the list of files.",
                    typeof(FileUploadExample)),
                new("button", "Button", "`DropZone=\"false\"` makes the field a button, for picking a single settings "
                    + "or project file.", typeof(FileUploadButtonExample)),
                new("limits", "Limits and files left out", "Files of another type, over `MaxFileSize`, or beyond "
                    + "`MaxFiles` are left out with a message. `MaxFileSize` is 512 KB unless set.",
                    typeof(FileUploadLimitsExample)),
                new("other-types", "Checking types yourself", "The accepted types come from `Accept`, or an `accept` in "
                    + "`InputAttributes`. With `RejectOtherTypes=\"false\"`, they only filter the picker, and your app "
                    + "checks the types that arrive, such as with `Validation`.", typeof(FileUploadOtherTypesExample)),
                new("zone-content", "Drop zone content", "`ZoneContent` replaces the zone's text, such as with a hint "
                    + "about the files to drop. It sits under the input and is hidden from screen readers, so nothing "
                    + "in it can be clicked, and anything people need to know belongs in `HelperText`.",
                    typeof(FileUploadZoneExample)),
                new("own-button", "Your own button", "`Hidden` keeps the field out of sight, the tab order and the "
                    + "reading order, apart from the reasons any files were left out and its messages. Your button then "
                    + "calls `OpenAsync()` from its click, because browsers open a file dialog only shortly after a "
                    + "user's click. `ClearAsync()` empties the field.", typeof(FileUploadOwnButtonExample)),
                new("reading-files", "Reading the files", "Each new choice replaces the last, as the browser's file "
                    + "input does, so read the files with `OpenReadStream` before someone chooses again.",
                    typeof(FileUploadReadExample)),
                new("required", "Required", "`Required` shows a marker. Once the field is changed or its form "
                    + "validates, a message shows while no file is chosen. `RequiredMessage` replaces the default, "
                    + "which uses the label.", typeof(FileUploadRequiredExample)),
                new("validation", "Validation", "`Validation` checks the files once they change or the form validates.",
                    typeof(FileUploadValidationExample)),
                new("error", "Errors from your app", "`Error` shows a message your app sets, such as one from a server. "
                    + "Until your app clears it, the form won't submit, so clear it when the files change.",
                    typeof(FileUploadErrorExample)),
                new("read-only-and-disabled", "Read-only and disabled", "`ReadOnly` turns off the picker and the remove "
                    + "and clear buttons, and leaves the chosen files listed. A file input has no read-only state, so "
                    + "the field disables it: the picker can't be focused, its files don't post, and a dropped file is "
                    + "refused. `Disabled` behaves and looks the same on this field.",
                    typeof(FileUploadStatesExample)),
                new("labels", "Built-in text", "`Labels` replaces the words the field shows and reads out, such as the "
                    + "zone's text and the number of files chosen.", typeof(FileUploadLabelsExample)),
            ],
            Api = [typeof(TbFileUpload), typeof(FileUploadLabels), typeof(FieldClasses)],
            Keyboard =
            [
                new(["Enter", "Space"], "Opens the file dialog."),
            ],
            Labeling =
            [
                "It is the browser's own file input, named by the `Label`, laid over the drop zone.",
                "The chosen files are a `list`, and each has a \"Remove\" button named with its file.",
            ],
            Announcements =
            [
                "Screen readers announce it as a file button.",
                "The reasons any files were left out are linked to the input and announced with the number of files "
                    + "chosen.",
                "A validation message that appears as someone changes the files is read out once, and a failed submit "
                    + "reads none out, as on a [text field](components/text-field).",
                "An `Error` your app sets is read out once when it appears, even after a submit.",
            ],
            Accessibility =
            [
                "Focus moves back to the input when a file is removed.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "It is the same file input, with a `name` for its form, so a static form posts the chosen files. The "
                    + "list of files and the reasons for any left out show once it is interactive.",
            ],
            Notes =
            [
                new("`OpenReadStream` throws for a file over its own limit, which is 512 KB unless you pass a larger "
                    + "`maxAllowedSize`. When you raise `MaxFileSize`, pass the same size to `OpenReadStream`.")
                    { Pitfall = true },
                new("Check each file's type and size again on your server. They come from the browser, so the limits "
                    + "here only help the person choosing.") { Pitfall = true },
            ],
            Ready = new("[data-part=input]", "the file input"),
            Target = new("button", "Trail maps"),
            Blocks = ["tb-field", "tb-file-upload"],
            DataParts =
            [
                new("label", "The field's label."),
                new("helper", "The help text under the field."),
                new("error", "The validation message, shown while the value is invalid."),
                new("zone", "The drop zone, which also opens the file picker."),
                new("input", "The file input."),
                new("files", "The list of chosen files."),
                new("file", "One chosen file."),
                new("remove", "The button that removes a file."),
                new("rejected", "The message that lists dropped files the field didn't take."),
                new("clear", "The button that removes every file."),
            ],
            TestingTip = "`await page.Tb().FileUpload(\"Trail maps\").ChooseAsync(\"map.pdf\")` from "
                + "`Trailblazor.Testing.Playwright` takes file paths or `FilePayload`s and waits until the field has "
                + "taken them. In bUnit, `FindComponent<InputFile>().UploadFiles(...)` chooses files. The field is "
                + "ready as soon as `Render` returns, with nothing left to run, so a test can raise events at once.",
        },
        new("calendar", "Calendar", ExampleCategory.Inputs,
            "Shows a month in place for choosing one date, with views of the year's months and the decade's years.")
        {
            WhenToUse =
            [
                "Use it when there's room to show a month and seeing it helps someone choose, such as which days are "
                    + "free.",
            ],
            WhenNotToUse =
            [
                "For a start and an end date, use a [date range picker](components/date-range-picker).",
                "For a date people know by heart, such as a birthday, use a [text field](components/text-field) bound "
                    + "to a `DateOnly`.",
            ],
            Related = ["date-range-picker", "text-field", "form"],
            Pattern = new("date picker dialog's grid",
                "https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/examples/datepicker-dialog/"),
            RightToLeftKeys = "Left moves to the next day, month or year, and Right to the previous one.",
            Sections =
            [
                new("value", "Value", "`@bind-Value` holds the chosen date. `TDate` can be `DateOnly` or `DateTime`; "
                    + "time zones are never converted, a date left alone keeps its time, and a picked date is "
                    + "midnight. Click the title for the year's months, and again for the decade's years, to reach a "
                    + "distant date quickly.",
                    typeof(CalendarExample)),
                new("min-and-max", "Dates that can't be chosen", "`Min` and `Max` set the earliest and latest dates, "
                    + "and `IsDateDisabled` returns true for a date that can't be chosen, such as a weekend. Focus can "
                    + "still reach those days, so a screen reader can say they're unavailable.",
                    typeof(CalendarLimitsExample)),
                new("day-content", "Content in the days", "`DayContent` adds content under a day's number, such as a "
                    + "dot for a day with events. Give it words for screen readers, which hear them after the date. "
                    + "`@bind-Month` holds the month that's shown, so your app can load what its days show.",
                    typeof(CalendarDayContentExample)),
                new("read-only-and-disabled", "Read-only and disabled", "`ReadOnly` keeps the date from changing while "
                    + "the months can still be browsed. `Disabled` turns every button off and takes the calendar out "
                    + "of the tab order.", typeof(CalendarStatesExample)),
            ],
            Api = [typeof(TbCalendar<>), typeof(CalendarLabels), typeof(ChoiceClasses)],
            Keyboard =
            [
                new(["Left", "Right"], "Moves by a day, or by a month or year in the months and years views.")
                    { When = "In the grid" },
                new(["Up", "Down"], "Moves by a week, or by a row of four months or years.") { When = "In the grid" },
                new(["Home", "End"], "Goes to the start or end of the week or row.") { When = "In the grid" },
                new(["Page Up", "Page Down"], "Changes the month, or the year or decade in the months and years views.")
                    { When = "In the grid" },
                new(["Shift+Page Up", "Shift+Page Down"], "Changes the year.") { When = "In the days view" },
                new(["Enter", "Space"], "Picks the day, or opens the month or year.") { When = "In the grid" },
                new(["Tab"], "Moves between the previous button, the title, the next button and the grid."),
            ],
            Labeling =
            [
                "The calendar is a `group` named by the `Label`, and its `grid` is named by the label and the title, "
                    + "such as \"Hike date November 2026\".",
                "Each day is named by its full date. The title button is named by the title and what it shows next, "
                    + "such as \"November 2026, choose a month\".",
            ],
            Announcements =
            [
                "The chosen day is `aria-selected`, today is `aria-current=\"date\"`, and days that can't be chosen "
                    + "are `aria-disabled`.",
                "Moving to another month, year or decade reads out the new title.",
                "A validation message that appears as someone picks a date is read out once, as on a "
                    + "[text field](components/text-field).",
            ],
            Accessibility =
            [
                "In forced-colors mode, such as Windows high contrast, the chosen day takes the system's highlight.",
            ],
            AppDuties =
            [
                "Purely visual content in `DayContent`, such as a dot, needs words for screen readers too, for example "
                    + "visually hidden text.",
            ],
            Notes =
            [
                new("`@ref` the calendar and call `FocusAsync()` to move focus to its grid."),
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "It shows the browser's own date box, so a static form still posts the date.",
            ],
            Ready = new("[role=grid]", "the grid"),
            Target = new("group", "Hike date"),
            Blocks = ["tb-calendar"],
            DataParts =
            [
                new("label", "The calendar's label."),
                new("control", "The box around the browser's date box, shown before the page is interactive."),
                new("previous", "The button that shows the month, year or decade before."),
                new("title", "The title button, which shows the year's months and then the decade's years."),
                new("next", "The button that shows the month, year or decade after."),
                new("grid", "The grid of days, months or years; `data-view` says which."),
                new("day", "One day."),
                new("day-content", "The content `DayContent` adds to a day."),
                new("month", "One month, in the months view."),
                new("year", "One year, in the years view."),
                new("input", "The browser's date box."),
                new("helper", "The help text under the calendar."),
                new("error", "The validation message, shown while the value is invalid."),
            ],
            TestingTip = "`await page.Tb().Calendar(\"Hike date\").ChooseAsync(date)` from "
                + "`Trailblazor.Testing.Playwright` picks a date through the years and months views, as a user would, "
                + "and waits until the calendar has taken it. In bUnit, `cut.Tb().Calendar(\"Hike date\")"
                + ".ChooseAsync(date)` from `Trailblazor.Testing` does the same.",
        },
        new("date-picker", "Date picker", ExampleCategory.Inputs,
            "Chooses one date, typed in a box or picked in a calendar that opens beside it.")
        {
            WhenToUse =
            [
                "Use it for one date in a form, when seeing it in a calendar helps someone choose.",
            ],
            WhenNotToUse =
            [
                "For a start and an end date, use a [date range picker](components/date-range-picker).",
                "When the month itself should stay on the page, use a [calendar](components/calendar).",
                "For a date people know by heart, such as a birthday, use a [text field](components/text-field) bound "
                    + "to a `DateOnly`.",
            ],
            Related = ["calendar", "date-range-picker", "text-field", "form"],
            Pattern = new("date picker dialog",
                "https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/examples/datepicker-dialog/"),
            RightToLeftKeys = "Left moves to the next day, month or year, and Right to the previous one.",
            Sections =
            [
                new("value", "Value", "`@bind-Value` holds the date. It can be typed in the box or picked in the "
                    + "calendar, which opens on the chosen date. A typed date follows the field's format but may leave "
                    + "out leading zeros or give the year in two digits. `TDate` can be `DateOnly` or "
                    + "`DateTime`; time zones are never converted, a date left alone keeps its time, and a newly typed "
                    + "or picked date is midnight.",
                    typeof(DatePickerExample)),
                new("limits", "Dates that can't be chosen", "`Min`, `Max` and `IsDateDisabled` set which dates can be "
                    + "chosen. Typing any other date shows a message, and the calendar marks those days as "
                    + "unavailable. `Clearable` shows a button that clears the date.", typeof(DatePickerLimitsExample)),
                new("read-only-and-disabled", "Read-only and disabled", "`ReadOnly` keeps the date from changing: the "
                    + "box can't be edited and the calendar button turns off. `Disabled` takes the field out of the tab "
                    + "order and the post.", typeof(DatePickerStatesExample)),
            ],
            Api = [typeof(TbDatePicker<>), typeof(DatePickerLabels), typeof(FieldClasses)],
            Keyboard =
            [
                new(["Enter", "Space"], "Opens the calendar with focus on the chosen date, or today.")
                    { When = "On the calendar button" },
                new(["Left", "Right", "Up", "Down"], "Moves by a day or a week, as in a "
                    + "[calendar](components/calendar#accessibility).") { When = "In the calendar" },
                new(["Enter", "Space"], "Picks the day, closes the calendar and returns focus to the button.")
                    { When = "In the calendar" },
                new(["Escape"], "Closes the calendar without changing the date.") { When = "In the calendar" },
                new(["Tab"], "Moves through the calendar's controls, and closes it as focus leaves.")
                    { When = "In the calendar" },
            ],
            Labeling =
            [
                "The box is named by the `Label`, and shows the format as its placeholder unless `Placeholder` sets "
                    + "another.",
                "The calendar button and the calendar are named \"Choose a date\", or by `Labels.Open`.",
            ],
            Announcements =
            [
                "A date that can't be read or chosen shows a message that is read out once, as on a "
                    + "[text field](components/text-field).",
            ],
            Accessibility =
            [
                "The popover holds a [calendar](components/calendar) and opens on the side with more room, so the "
                    + "keys, views and forced-colors look are the calendar's.",
            ],
            AppDuties =
            [
                "A `Placeholder` replaces the format shown in the empty box, but the box still reads dates only in that "
                    + "format, so give the format in `HelperText`.",
            ],
            Notes =
            [
                new("`@ref` the picker and call `FocusAsync()` to move focus to the box."),
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "It shows the browser's own date box, so a static form still posts the date.",
            ],
            Ready = new("[data-part=trigger]", "the button that opens the calendar"),
            Target = new("textbox", "Start date"),
            Blocks = ["tb-field"],
            DataParts =
            [
                .. FieldParts,
                new("input", "The box the date is typed in."),
                new("clear", "The button that clears the date."),
                new("popover", "The calendar button and the calendar it opens."),
            ],
            TestingTip = "`await page.Tb().DatePicker(\"Start date\").ChooseAsync(date)` from "
                + "`Trailblazor.Testing.Playwright` types the date in the field's own format and waits until the field "
                + "has taken it. In bUnit, `Change(\"6/12/2026\")` on the `input` sets it; write the date in the field's "
                + "format, as here for en-US. `@bind-Open` opens the calendar from code, so a test can check it "
                + "without pressing its button.",
        },
        new("time-picker", "Time picker", ExampleCategory.Inputs,
            "Chooses a time of day in the browser's own time box, shown in the user's language.")
        {
            WhenToUse =
            [
                "Use it for a time of day, such as a start time or a pickup slot.",
            ],
            WhenNotToUse =
            [
                "For a length of time, such as a hike's duration, use a [number field](components/number-field).",
                "For a day, use a [date picker](components/date-picker).",
            ],
            Related = ["date-picker", "number-field", "form"],
            Sections =
            [
                new("value", "Value", "`@bind-Value` holds the time. `TTime` can be `TimeOnly`, or `TimeSpan` from "
                    + "midnight. The browser shows the time in the user's own language, such as 8:30 AM or 08:30.",
                    typeof(TimePickerExample)),
                new("limits", "Earliest, latest and steps", "`Min` and `Max` set the earliest and latest times, and a "
                    + "time outside them shows a message. A `Min` after `Max` is a range across midnight. `Step` sets "
                    + "how far apart the times are, such as 15 minutes; a step that isn't a whole number of minutes "
                    + "adds seconds to the box.", typeof(TimePickerLimitsExample)),
                new("read-only-and-disabled", "Read-only and disabled", "`ReadOnly` keeps the time from changing. "
                    + "`Disabled` takes the field out of the tab order and the post.", typeof(TimePickerStatesExample)),
            ],
            Api = [typeof(TbTimePicker<>), typeof(TimePickerLabels), typeof(FieldClasses)],
            Keyboard =
            [
                new(["Left", "Right"], "Moves between the hours, minutes and, in some languages, AM or PM.")
                    { When = "In the box" },
                new(["Up", "Down"], "Changes the part with focus. The minutes move by `Step`.") { When = "In the box" },
                new(["0-9"], "Types the part with focus.") { When = "In the box" },
            ],
            Labeling =
            [
                "The box is named by the `Label`. Each part of the time is announced by the browser, such as "
                    + "\"hours\" and \"minutes\".",
            ],
            Announcements =
            [
                "A time outside `Min` and `Max` shows a message that is read out once, as on a "
                    + "[text field](components/text-field).",
            ],
            Accessibility =
            [
                "The time box is the browser's own, so its keys, its parts' names and its look in forced-colors mode "
                    + "are the ones people know from other sites.",
            ],
            AppDuties =
            [
                "The box shows the time in the browser's language, not the app's `Culture`, but its value is always "
                    + "24-hour `HH:mm`, so a test reads and fills it that way.",
            ],
            Notes =
            [
                new("`@ref` the picker and call `FocusAsync()` to move focus to the box."),
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Partial),
            BeforeInteractive =
            [
                "It is the browser's own time box either way, so the time can be chosen and a static form still posts "
                    + "it.",
            ],
            Target = new("textbox", "Start time"),
            Blocks = ["tb-field"],
            DataParts =
            [
                .. FieldParts,
                new("input", "The time box."),
            ],
            TestingTip = "In Playwright, `GetByLabel(\"Start time\").FillAsync(\"14:30\")` sets the time in 24-hour "
                + "form, whatever the box shows. In bUnit, `Change(\"14:30\")` on the `input` does the same.",
        },
        new("date-time-picker", "Date-time picker", ExampleCategory.Inputs,
            "Chooses a date and a time of day: the date typed or picked in a calendar, the time in the browser's own "
                + "time box.")
        {
            WhenToUse =
            [
                "Use it for a moment on a given day, such as when a guided hike starts or a shuttle picks up.",
            ],
            WhenNotToUse =
            [
                "For a day alone, use a [date picker](components/date-picker).",
                "For a time of day that repeats, such as opening hours, use a [time picker](components/time-picker).",
            ],
            Related = ["date-picker", "time-picker", "calendar", "form"],
            Pattern = new("date picker dialog",
                "https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/examples/datepicker-dialog/"),
            RightToLeftKeys = "Left moves to the next day, month or year, and Right to the previous one.",
            Sections =
            [
                new("value", "Value", "`@bind-Value` holds the date and time. The date is typed in its box or "
                    + "picked in the calendar; a typed date follows the field's format but may leave out leading zeros "
                    + "or give the year in two digits. The time is set in the browser's own time box, shown in the "
                    + "user's language. A date chosen with no time is midnight, and a time set first waits for a date. "
                    + "`TDateTime` can be `DateTime` or `DateTimeOffset`; the picker shows exactly the date "
                    + "and time it is given and never converts time zones.", typeof(DateTimePickerExample)),
                new("limits", "Earliest, latest and steps", "`Min` and `Max` set the earliest and latest moments; a "
                    + "value outside them shows a message, and the calendar marks the days outside them as "
                    + "unavailable. `IsDateDisabled` rules out whole days. `Step` sets how far apart the times are, "
                    + "such as 15 minutes. `Clearable` shows a button that clears both boxes.",
                    typeof(DateTimePickerLimitsExample)),
                new("read-only-and-disabled", "Read-only and disabled", "`ReadOnly` keeps the value from changing: the "
                    + "boxes can't be edited and the calendar button turns off. `Disabled` takes the field out of the "
                    + "tab order and the post.", typeof(DateTimePickerStatesExample)),
            ],
            Api = [typeof(TbDateTimePicker<>), typeof(DateTimePickerLabels), typeof(FieldClasses)],
            Keyboard =
            [
                new(["Tab"], "Moves from the date box to the calendar button, then to the time box.")
                    { When = "In the field" },
                new(["Enter", "Space"], "Opens the calendar with focus on the chosen date, or today.")
                    { When = "On the calendar button" },
                new(["Left", "Right", "Up", "Down"], "Moves by a day or a week, as in a "
                    + "[calendar](components/calendar#accessibility).") { When = "In the calendar" },
                new(["Enter", "Space"], "Picks the day, keeps the time, closes the calendar and returns focus to the "
                    + "button.") { When = "In the calendar" },
                new(["Escape"], "Closes the calendar without changing the date.") { When = "In the calendar" },
                new(["Left", "Right"], "Moves between the hours, minutes and, in some languages, AM or PM.")
                    { When = "In the time box" },
                new(["Up", "Down"], "Changes the part with focus. The minutes move by `Step`.")
                    { When = "In the time box" },
            ],
            Labeling =
            [
                "The `Label` names the group of boxes, and each box adds its own name: \"Date\" and \"Time\", or "
                    + "`Labels.Date` and `Labels.Time`.",
                "The date box shows the format as its placeholder unless `Placeholder` sets another.",
                "The calendar button and the calendar are named \"Choose a date\", or by `Labels.Open`.",
            ],
            Announcements =
            [
                "A date that can't be read or chosen, and a value outside `Min` and `Max`, show a message that is read "
                    + "out once, as on a [text field](components/text-field).",
            ],
            Accessibility =
            [
                "The calendar is a [calendar](components/calendar) in a popover, and the time box is the browser's "
                    + "own, so their keys and forced-colors looks are the ones people know.",
                "A message from typing marks only the box it is about as invalid.",
            ],
            AppDuties =
            [
                "For a `DateTimeOffset`, set `TimeZone` to the user's time zone. Without it, a changed value keeps its "
                    + "offset and a first value takes the offset of the app's `TimeProvider`, which on a server is the "
                    + "server's time zone.",
                "A static form posts the date and time with no offset, and Blazor reads it in the server's time zone "
                    + "whatever `TimeZone` says, so bind a `DateTime` in a static form and add the user's offset after "
                    + "the post.",
                "The time box shows the time in the browser's language, not the app's `Culture`.",
            ],
            Notes =
            [
                new("`@ref` the picker and call `FocusAsync()` to move focus to the date box."),
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "It shows the browser's own date and time box, so a static form still posts the value, with no "
                    + "offset.",
            ],
            Ready = new("[data-part=trigger]", "the button that opens the calendar"),
            Target = new("group", "Guided hike starts"),
            Blocks = ["tb-field"],
            DataParts =
            [
                .. FieldParts,
                new("date-input", "The box the date is typed in."),
                new("time-input", "The browser's own time box."),
                new("clear", "The button that clears the date and time."),
                new("popover", "The calendar button and the calendar it opens."),
                new("input", "The browser's own date and time box, shown before the page is interactive."),
            ],
            TestingTip = "`await page.Tb().DateTimePicker(\"Guided hike starts\").ChooseAsync(start)` from "
                + "`Trailblazor.Testing.Playwright` types the date in the field's own format, fills the time and waits "
                + "until the field has taken both. In bUnit, `Change(\"6/12/2026\")` on the `date-input` and "
                + "`Change(\"14:30\")` on the `time-input` set them; write the date in the field's format and the time "
                + "in 24-hour form. `@bind-Open` opens the calendar from code.",
        },
        new("date-range-picker", "Date range picker", ExampleCategory.Inputs,
            "Chooses a range of dates, typed or picked in a two-month calendar, with ready-made ranges.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for a start and an end date, when seeing them in a calendar helps someone choose.",
            ],
            WhenNotToUse =
            [
                "For two dates that don't form a range, or a date people know by heart, such as a birthday, use a "
                    + "[text field](components/text-field) bound to a `DateOnly`.",
            ],
            Related = ["text-field", "form"],
            Pattern = new("date picker dialog",
                "https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/examples/datepicker-dialog/"),
            RightToLeftKeys = "Left moves to the next day and Right to the previous one.",
            Sections =
            [
                new("range-and-value", "Range and value", "`@bind-Value` holds a `DateRange`, with a `Start` and an "
                    + "`End`. Dates can be typed in the boxes or picked in the calendar.",
                    typeof(DateRangePickerExample)),
                new("presets", "Presets", "`Presets` adds ready-made ranges beside the calendar.",
                    typeof(DateRangePickerPresetsExample)),
                new("min-and-max", "Earliest and latest dates", "`Min` and `Max` set the earliest and latest dates that "
                    + "can be chosen.", typeof(DateRangePickerLimitsExample)),
                new("disabled-dates", "Dates that can't be chosen", "`IsDateDisabled` returns true for a date that "
                    + "can't be chosen, such as a weekend. Focus can still reach it, so a screen reader can say it's "
                    + "unavailable.",
                    typeof(DateRangePickerDisabledDaysExample)),
                new("format-and-culture", "Format and culture", "`Format` sets how the boxes show and read dates; "
                    + "typed dates may also leave out leading zeros or give the year in two digits. "
                    + "`Culture` sets the month and day names, the first day of the week, and the format when `Format` "
                    + "isn't set. It defaults to the current culture. Dates stay Gregorian, like the calendar's grid, "
                    + "even for a culture with its own calendar, such as th-TH.",
                    typeof(DateRangePickerFormatExample)),
                new("placeholders", "Placeholders", "Each empty box shows the format. `StartPlaceholder` and "
                    + "`EndPlaceholder` show other words instead, so give the format in `HelperText`.",
                    typeof(DateRangePickerPlaceholderExample)),
                new("date-time", "DateTime values", "`TDate` can be `DateOnly` or `DateTime`, and time zones are never "
                    + "converted. A date left alone keeps its time, and a newly typed or picked date is midnight at the "
                    + "start of that day, so a range can end at the very start of its last day.",
                    typeof(DateRangePickerDateTimeExample)),
                new("clearing", "Clearing", "`Clearable` shows a button that clears both dates.",
                    typeof(DateRangePickerClearExample)),
                new("invalid-text", "Invalid dates", "If a box can't read a date, a message says which box and what "
                    + "format to use, and that box is marked invalid. A date that can't be chosen, or an end before the "
                    + "start, also shows a message.",
                    typeof(DateRangePickerInvalidExample)),
                new("required", "Required", "`Required` shows a marker. Once the field is changed or its form "
                    + "validates, a message shows until both dates are set. `RequiredMessage` replaces the default, "
                    + "which uses the label.", typeof(DateRangePickerRequiredExample)),
                new("validation", "Validation", "`Validation` checks the range once it is changed or its form "
                    + "validates.", typeof(DateRangePickerValidationExample)),
                new("error", "Errors from your app", "`Error` shows a message your app sets, such as one from a server. "
                    + "Until your app clears it, the form won't submit, so clear it when the dates change.",
                    typeof(DateRangePickerErrorExample)),
                new("read-only-and-disabled", "Read-only and disabled", "`ReadOnly` keeps the dates from changing: the "
                    + "boxes can't be edited, the calendar button turns off and the clear button hides. `Disabled` "
                    + "takes the field out of the tab order and the post.", typeof(DateRangePickerStatesExample)),
                new("size-and-variant", "Size and variant", "`Variant` is `Outlined` or `Filled`, and `Size` is "
                    + "`Small`, `Medium` or `Large`.", typeof(DateRangePickerSizeExample)),
                new("labels", "Built-in text", "`Labels` replaces the words the field shows and reads out, such as the "
                    + "names of the boxes and the calendar button, and the field's messages.",
                    typeof(DateRangePickerLabelsExample)),
            ],
            Api = [typeof(TbDateRangePicker<>), typeof(DateRangePickerLabels), typeof(FieldClasses)],
            Keyboard =
            [
                new(["Enter", "Space"], "Opens the calendar.") { When = "On the calendar button" },
                new(["Left", "Right"], "Moves by a day.") { When = "In the calendar" },
                new(["Up", "Down"], "Moves by a week.") { When = "In the calendar" },
                new(["Home", "End"], "Goes to the start or end of the week.") { When = "In the calendar" },
                new(["Page Up", "Page Down"], "Changes the month.") { When = "In the calendar" },
                new(["Shift+Page Up", "Shift+Page Down"], "Changes the year.") { When = "In the calendar" },
                new(["Enter", "Space"], "Picks the day.") { When = "In the calendar" },
                new(["Escape"], "Closes the calendar and returns focus to the button.") { When = "In the calendar" },
                new(["Tab"], "Moves through the calendar's controls, and closes it as focus leaves.")
                    { When = "In the calendar" },
            ],
            Labeling =
            [
                "The two date boxes and the calendar button sit in a `group` named by the `Label`. Each box is named by "
                    + "the label and \"Start date\" or \"End date\", and shows the format as its placeholder unless "
                    + "`StartPlaceholder` or `EndPlaceholder` sets another.",
                "The calendar is a `dialog` with a `grid` for each month.",
                "The clear button is named \"Clear\" and described by the field's label, so a screen reader says which "
                    + "field it clears.",
            ],
            Announcements =
            [
                "Days in the range are `aria-selected`, and its first and last days add \"Start date\" and \"End "
                    + "date\" to their names. Today is `aria-current=\"date\"`, and days that can't be chosen are "
                    + "`aria-disabled`.",
                "Picking the start announces that the end comes next, and picking the end announces the range, such as "
                    + "\"Friday, June 12, 2026 to Monday, June 15, 2026\".",
                "A validation message that appears as someone changes the dates is read out once, and a failed submit "
                    + "reads none out, as on a [text field](components/text-field).",
                "An `Error` your app sets is read out once when it appears, even after a submit.",
            ],
            Accessibility =
            [
                "A panel too long for the window opens on the side with more room and scrolls, so every date stays "
                    + "reachable.",
                "In forced-colors mode, such as Windows high contrast, the range's days take the system's highlight, "
                    + "its first and last days add an outline, and the range previewed up to the pointer or focus has "
                    + "only a dashed outline, so it can't be taken for a chosen range.",
            ],
            AppDuties =
            [
                "A `StartPlaceholder` or `EndPlaceholder` replaces the format the box shows, and the box reads dates "
                    + "only in that format, so give it in `HelperText`.",
            ],
            Notes =
            [
                new("A press around the date boxes, such as on the field's padding, focuses the start box, unless it lands "
                    + "on a button."),
                new("`@ref` the picker and call `FocusAsync()` to move focus to the start box."),
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "It shows the browser's own date boxes, so a static form still posts both dates.",
            ],
            Ready = new("[data-part=trigger]", "the button that opens the calendar"),
            Target = new("group", "Trip dates"),
            Blocks = ["tb-field", "tb-date-range"],
            DataParts =
            [
                .. FieldParts,
                new("start-input", "The box for the first date."),
                new("end-input", "The box for the last date."),
                new("trigger", "The button that opens the calendar."),
                new("clear", "The button that clears both dates."),
                new("panel", "The calendar popup."),
                new("presets", "The list of preset ranges."),
                new("preset", "One preset range."),
                new("previous-month", "The button that shows the month before."),
                new("next-month", "The button that shows the month after."),
                new("month", "One month of days."),
                new("day", "One day."),
            ],
            TestingTip = "`await page.Tb().DateRangePicker(\"Trip dates\").ChooseAsync(start, end)` from "
                + "`Trailblazor.Testing.Playwright` types both dates in the field's own format and waits until the "
                + "field has taken them. Its boxes have `data-part=\"start-input\"` and `data-part=\"end-input\"`. "
                + "`@bind-Open` opens the calendar from code.",
        },
        new("listbox", "List box", ExampleCategory.Inputs,
            "Picks one or several options from a list that is always shown.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it when the options should stay visible on the page, and the list is too long for a radio or "
                    + "checkbox group.",
                "[Choosing a list field](guides/forms#choosing-a-list-field) compares it with the other fields that "
                    + "pick from a list.",
            ],
            WhenNotToUse =
            [
                "To save space with a list that opens only when needed, use a [select](components/select).",
                "When the options come from a search as someone types, use an [autocomplete](components/autocomplete).",
                "For a few options, use a [radio group](components/radio-group) or a "
                    + "[checkbox group](components/checkbox-group).",
            ],
            Related = ["select", "autocomplete", "radio-group", "checkbox-group", "form"],
            Pattern = new("listbox", "https://www.w3.org/WAI/ARIA/apg/patterns/listbox/"),
            Sections =
            [
                new("options-and-value", "Options and value", "Each `TbOption` is an option, `Rows` sets how many show "
                    + "before the list scrolls, and `Disabled` on an option turns off that option alone.",
                    typeof(ListBoxExample)),
                new("multiple", "Multiple", "`TbMultiListBox` binds a list of the chosen values. A click chooses or "
                    + "clears one option. Shift+click adds the options from the active one through the clicked one, "
                    + "and Ctrl+A adds every option. Both skip disabled options.",
                    typeof(ListBoxMultipleExample)),
                new("groups", "Groups", "`TbOptionGroup` groups options under a heading. The arrows and typing move "
                    + "through every group.", typeof(ListBoxGroupsExample)),
                new("rich-options", "Options with more than text", "An option's content can hold more than text, such "
                    + "as a chip, and its words name the option. `Text` is for content without words, such as an "
                    + "image, and replaces that name, so include any words the option shows.",
                    typeof(ListBoxContentExample)),
                new("empty-list", "Empty list", "A list box with no options says so, in text as readable as the "
                    + "options. Set `Labels.Empty` to say why.", typeof(ListBoxEmptyExample)),
                new("many-options", "Many options", "A long list scrolls, and a long option wraps. When people would "
                    + "rather search than scroll, make it `Filterable`.",
                    typeof(ListBoxManyExample)),
                new("filter", "Filter", "`Filterable` adds a box above the options that narrows them as people type, "
                    + "ignoring case and accents. Down Arrow moves from the box into the list.",
                    typeof(ListBoxFilterExample)),
                new("sizes", "Sizes", "`Size` is `Small`, `Medium` or `Large`, and sets the options' height and text "
                    + "size. Each size follows the page's density, and an option is never shorter than 24 pixels.",
                    typeof(ListBoxSizeExample)),
                new("comparer", "Values loaded again", "`Comparer` decides whether two values are the same, so a value "
                    + "from another query, equal only by id, still matches its option.", typeof(ListBoxComparerExample)),
                new("required", "Required", "`Required` shows a marker. Once the list box is changed or its form "
                    + "validates, a message shows while nothing is chosen. `RequiredMessage` replaces the default, "
                    + "which uses the label.", typeof(ListBoxRequiredExample)),
                new("validation", "Validation", "`Validation` checks the choice once it is changed or its form "
                    + "validates.", typeof(ListBoxValidationExample)),
                new("error", "Errors from your app", "`Error` shows a message your app sets, such as one from a server. "
                    + "Until your app clears it, the form won't submit, so clear it when the choice changes.",
                    typeof(ListBoxErrorExample)),
                new("read-only-and-disabled", "Read-only and disabled", "`ReadOnly` keeps the list focusable, and the "
                    + "arrow keys still move through its options so they can be read, but nothing gets chosen. "
                    + "`Disabled` takes it out of the tab order.", typeof(ListBoxStatesExample)),
            ],
            Api = [typeof(TbListBox<>), typeof(TbMultiListBox<>), typeof(TbOption<>), typeof(TbOptionGroup),
                typeof(ListBoxLabels), typeof(FieldClasses)],
            Keyboard =
            [
                new(["Down"], "Moves from the filter into the list.") { When = "In a `Filterable` list box's filter" },
                new(["Down", "Up"], "Moves to the next or previous option that isn't disabled, and chooses it.")
                    { When = "In a `TbListBox`" },
                new(["Home", "End"], "Moves to the first or last option that isn't disabled, and chooses it.")
                    { When = "In a `TbListBox`" },
                new(["Page Up", "Page Down"], "Moves up or down by as many options as the list shows, and chooses "
                    + "the one it reaches.")
                    { When = "In a `TbListBox`" },
                new(["A letter"], "Moves to an option that starts with what was typed, and chooses it. Pressed again "
                    + "quickly, it moves to and chooses the next one that starts with that letter.")
                    { When = "In a `TbListBox`" },
                new(["Space"], "Chooses the active option. A Space typed mid-search adds to the search instead.")
                    { When = "In a `TbListBox`" },
                new(["Down", "Up"], "Moves to the next or previous option that isn't disabled.")
                    { When = "In a `TbMultiListBox`" },
                new(["Home", "End"], "Moves to the first or last option that isn't disabled.")
                    { When = "In a `TbMultiListBox`" },
                new(["Page Up", "Page Down"], "Moves up or down by as many options as the list shows.")
                    { When = "In a `TbMultiListBox`" },
                new(["Ctrl+A", "Cmd+A"], "Chooses every option that isn't disabled and that a filter doesn't hide.")
                    { When = "In a `TbMultiListBox`" },
                new(["A letter"], "Moves to an option that starts with what was typed. Pressed again quickly, it "
                    + "moves to the next one that starts with that letter.")
                    { When = "In a `TbMultiListBox`" },
                new(["Space"], "Chooses or clears the active option. A Space typed mid-search adds to the search "
                    + "instead.") { When = "In a `TbMultiListBox`" },
                new(["Shift+Down", "Shift+Up"], "Moves to the next or previous option that isn't disabled, and chooses "
                    + "it.")
                    { When = "In a `TbMultiListBox`" },
            ],
            Labeling =
            [
                "A `listbox` named by its `Label`. A `TbOptionGroup` is a `group` named by its `Label`.",
                "A `Filterable` list box's filter box is named by `Labels.Filter`, \"Filter\" by default.",
                "An option's words name it. Content without words, such as an image, is named by its `Text`; with "
                    + "neither, it gets a warning in the browser console.",
            ],
            Announcements =
            [
                "On focus, its label, then its helper text or error.",
                "Focus stays on the list box, which points to the active option with `aria-activedescendant`, so a "
                    + "screen reader reads each option as it becomes active.",
                "A `ReadOnly` list box is heard as read only.",
                "A validation message that appears as someone changes the choice is read out once, and a failed submit "
                    + "reads none out, as on a [text field](components/text-field). An `Error` your app sets is read "
                    + "out once when it appears.",
                "When the filter matches nothing, `Labels.NoResults`, \"No matches\" by default, is read out.",
            ],
            Accessibility =
            [
                "It is one tab stop.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "It renders as the browser's own `<select>` list, so it still works and posts with its form.",
                "A `ReadOnly` native list puts its choice back whenever the user changes it, and posts. A `Disabled` "
                    + "one doesn't post.",
                "Until the page is interactive, an option with `Text` shows that text instead of its content, since the "
                    + "browser's own list can only show text.",
            ],
            Notes =
            [
                new("`Rows` sets how many options show before the list scrolls, 5 by default. To show them all, set it "
                    + "to their count, such as `Rows=\"@trails.Count\"`."),
            ],
            Ready = new("[role=listbox]", "the listbox"),
            Target = new("listbox", "Trail"),
            Blocks = ["tb-field", "tb-listbox"],
            DataParts =
            [
                new("label", "The list box's label."),
                new("helper", "The help text under the list box."),
                new("error", "The validation message, shown while the value is invalid."),
                new("input", "The list box itself, which takes focus."),
                new("group", "A group of options."),
                new("group-label", "A group's heading."),
                new("option", "One option."),
                new("status", "The message the list shows when it has no options."),
                new("filter", "The filter box above the options, in a `Filterable` list box."),
                new("no-results", "The message shown when the filter matches nothing."),
            ],
            Anatomy = """
                <TbListBox Label="…" @bind-Value="_value">
                    <TbOptionGroup Label="…">
                        <TbOption Value="…" />
                    </TbOptionGroup>
                    <TbOption Value="…" />
                </TbListBox>

                <TbMultiListBox Label="…" @bind-Value="_values">
                    <TbOption Value="…" />
                </TbMultiListBox>
                """,
            TestingTip = "To choose options, `await page.Tb().ListBox(\"Gear to pack\").ChooseAsync(\"Stove\")` from "
                + "`Trailblazor.Testing.Playwright` picks them and waits for the page, and it works on the native "
                + "`<select>` too. For object values loaded again, give the list box a `Comparer`. In bUnit, "
                + "`await cut.Tb().ListBox(\"Gear to pack\").ChooseAsync(\"Stove\")` from `Trailblazor.Testing` "
                + "does the same.",
            Telemetry =
            [
                "An option with a `TelemetryName`, such as `\"size-large\"`, counts `trailblazor.control.used` each "
                    + "time it is chosen on its own. A disabled or read-only list box chooses nothing, so it counts "
                    + "nothing; the value is never recorded.",
                "Choosing several at once, or with Shift+Up or Shift+Down, doesn't count each option. A list box "
                    + "with its own `TelemetryName` counts one use instead, with `trailblazor.action` `select_all` for "
                    + "Ctrl+A or `select_range` for Shift+click, Shift+Up or Shift+Down. Its `trailblazor.input` says "
                    + "whether the pointer or the keys chose the options.",
            ],
        },
        new("transfer-list", "Transfer list", ExampleCategory.Inputs,
            "Moves items between a list of those available and a list of those chosen.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it to choose several items from a long list when it helps to see the chosen ones together, apart "
                    + "from the rest, such as the hikers on a trip.",
            ],
            WhenNotToUse =
            [
                "For a short list, use a [checkbox group](components/checkbox-group) or a "
                    + "[multiple list box](components/listbox#multiple).",
                "To save space with a list that opens only when needed, use a [multiple select](components/select).",
            ],
            Related = ["listbox", "select", "checkbox-group", "form"],
            Pattern = new("listbox", "https://www.w3.org/WAI/ARIA/apg/patterns/listbox/"),
            Sections =
            [
                new("move-items", "Moving items", "`Items` holds every item, and `@bind-Value` the chosen ones, which "
                    + "keep the order of `Items`. Mark items in a list, then move them with the button beside it, or "
                    + "move every item a list shows. Double-clicking an item moves it to the other list. "
                    + "`ItemText` gives each item's text, `Labels` the lists' titles, and "
                    + "`Searchable` a search box above each list, which then shows only the items whose text contains "
                    + "what's typed.", typeof(TransferListExample)),
            ],
            Api = [typeof(TbTransferList<>), typeof(TransferListLabels), typeof(FieldClasses)],
            Keyboard =
            [
                new(["Down", "Up"], "Moves through a list's items.") { When = "In a list" },
                new(["Space"], "Marks or unmarks the active item.") { When = "In a list" },
                new(["Ctrl+A", "Cmd+A"], "Marks every item in the list.") { When = "In a list" },
                new(["Tab"], "Moves from a list to the move buttons, then to the other list."),
                new(["Enter", "Space"], "Moves the marked items, or every item, to the other list.")
                    { When = "On a move button" },
            ],
            Labeling =
            [
                "A `fieldset` named by its `Label`. Each list is a `listbox` named by its title, and each search box "
                    + "by \"Search\" and the title.",
                "Each move button is named for what it does and where to, such as \"Move to Chosen\".",
            ],
            Announcements =
            [
                "After a move, how many items moved and to which list, such as \"Moved 2 to Chosen\".",
            ],
            Accessibility =
            [
                "A move button is turned off while it has nothing to move, but stays in the tab order, so focus stays "
                    + "on it after a move. Each shows its name as a tooltip.",
                "Below about 30rem wide, the lists stack, with the buttons in a row between them.",
            ],
            BeforeInteractive =
            [
                "Both lists render with their items. Moving items starts working once the page is interactive, and "
                    + "a static form posts nothing for it.",
            ],
            Modes = new(ModeSupport.DisplayOnly, ModeSupport.DisplayOnly),
            Ready = new("[role=listbox]", "each list"),
            Target = new("group", "Trip crew"),
            Blocks = ["tb-transfer-list"],
            DataParts =
            [
                new("label", "The transfer list's label."),
                new("available", "The list of items not chosen, with its search box."),
                new("chosen", "The list of chosen items, with its search box."),
                new("moves", "The move buttons between the lists."),
                new("move-chosen", "The button that moves the marked items to the chosen list."),
                new("move-all-chosen", "The button that moves every item shown to the chosen list."),
                new("move-available", "The button that moves the marked items back to the available list."),
                new("move-all-available", "The button that moves every item shown in the chosen list back to the "
                    + "available list."),
            ],
            Anatomy = """
                <TbTransferList TItem="…" Label="…" Items="_items" ItemText="…" @bind-Value="_chosen" />
                """,
            TestingTips =
            [
                "In Playwright, `page.Tb().TransferList(\"Trip crew\")` from `Trailblazor.Testing.Playwright` moves "
                    + "items with `await MoveAsync(\"Casey\", \"Devon\")` and back with `MoveBackAsync(...)`, and "
                    + "reads the chosen items with `await ChosenAsync()`.",
                "In bUnit, `cut.Tb().TransferList(\"Trip crew\")` from `Trailblazor.Testing` has the same "
                    + "`MoveAsync`, `MoveBackAsync` and `Chosen`.",
            ],
            Telemetry =
            [
                "With a `TelemetryName`, it also counts `trailblazor.control.used` with `trailblazor.action` `move` for "
                    + "each move. Which items moved is never recorded.",
            ],
        },
        new("code-field", "Code field", ExampleCategory.Inputs,
            "Takes a short verification code, such as the six digits sent to a phone, with a box for each character.")
        {
            WhenToUse =
            [
                "Use it for a one-time code from a text message, an email or an authenticator app, such as in a "
                    + "sign-in step.",
            ],
            WhenNotToUse =
            [
                "For a password, use a [text field](components/text-field) of type `Password`.",
                "For a numeric value, such as a quantity or a price, use a [number field](components/number-field).",
            ],
            Related = ["text-field", "number-field", "form"],
            Sections =
            [
                new("verification-code", "A verification code", "`Length` sets how many characters the code has, six "
                    + "by default, and `Characters` whether it takes only digits or letters too. The field is one "
                    + "input, so a phone can fill it from a text message, and pasting or deleting works as in any "
                    + "field; a paste of \"123-456\" keeps only the digits. The value is the code once every box is "
                    + "filled, and `OnComplete` runs when the last box is. A partly typed code isn't a value: the "
                    + "bound value keeps the last full code, and the field shows an error once it loses focus.",
                    typeof(CodeFieldExample)),
            ],
            Api = [typeof(TbCodeField), typeof(CodeCharacters), typeof(FieldClasses)],
            Keyboard =
            [
                new(["Backspace"], "Removes the character before the caret, from any box."),
                new(["Left", "Right"], "Moves the caret between the boxes."),
            ],
            Labeling =
            [
                "One input named by its `Label`, so the whole code has a single name rather than one per box.",
            ],
            Accessibility =
            [
                "The input says it holds a one-time code (`autocomplete=\"one-time-code\"`), so browsers and phones "
                    + "offer a code they received. A code of digits brings up a number keyboard.",
                "A partly typed code shows \"Verification code needs all 6 characters.\" once the field loses focus "
                    + "or Enter is pressed, not while the code is being typed.",
            ],
            BeforeInteractive =
            [
                "It is a single text box with a `name`, so a static form posts the code as typed. Nothing filters "
                    + "what's typed or pasted until the page is interactive, so a dash or space in a pasted code "
                    + "counts toward the field's length and cuts off the code's last characters, and other characters "
                    + "are rejected only when the form posts.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            Ready = new("[data-part=input]", "the input"),
            Target = new("textbox", "Verification code"),
            Blocks = ["tb-code-field"],
            DataParts =
            [
                new("input", "The input that holds the code."),
            ],
            Anatomy = """
                <TbCodeField Label="…" @bind-Value="_code" OnComplete="…" />
                """,
            TestingTips =
            [
                "In bUnit, raise one input event with the whole code, as the browser reports it: "
                    + "`cut.Find(\"[data-part=input]\").Input(\"246810\")`.",
                "In Playwright, fill it by its label: "
                    + "`await page.GetByLabel(\"Verification code\").FillAsync(\"246810\")`.",
            ],
        },
        new("tooltip", "Tooltip", ExampleCategory.Overlays,
            "A short description that appears on hover or keyboard focus.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for a short hint about a control, such as what it does or its keyboard shortcut.",
            ],
            WhenNotToUse =
            [
                "For links, buttons or anything longer to read, use a [popover](components/popover), which the "
                    + "keyboard can reach.",
                "For something people need in order to use the page, put it on the page, such as a field's "
                    + "`HelperText`, because a tap on a button doesn't show a tooltip.",
                "For an icon button's name, use the [icon button](components/icon-button)'s `ShowTooltip`.",
            ],
            Related = ["popover", "icon-button", "button"],
            Sections =
            [
                new("description", "Describing a control", "Wrap a tooltip around a control, and `Text` appears when "
                    + "the pointer rests on the control or it takes keyboard focus.", typeof(TooltipExample)),
                new("placement", "Placement", "`Placement` picks the side a tooltip opens on, and it flips when "
                    + "there's no room. Placed to the side in a narrow window, it wraps to fit, or overlaps the control "
                    + "if its text can't wrap that far. `Align` lines it up with the control, and `ShowArrow` adds an "
                    + "arrow that points at the control's middle.", typeof(TooltipPlacementExample)),
                new("formatted", "Formatted text", "`TextContent` replaces `Text` with formatted content, such as a "
                    + "shortcut in `<kbd>`, which is read as part of the description. A tooltip can't be reached with "
                    + "the keyboard, so it holds no links or buttons.", typeof(TooltipFormattedExample)),
                new("delays", "Delays", "A tooltip shows at once on keyboard focus and after a delay on hover. "
                    + "`ShowDelay` sets how long the pointer must rest before it appears, and `HideDelay` how long it "
                    + "stays after the pointer leaves. Both are 300 milliseconds unless set, and `options.Tooltips` in "
                    + "`AddTrailblazor` changes them for the whole app. Once the pointer shows one tooltip, the next it "
                    + "reaches shows at once, as along a toolbar, until half a second after the last one hides. A touch "
                    + "has no hover, so it shows a tooltip only by focusing a text field.", typeof(TooltipDelayExample)),
                new("turning-off", "Turning a tooltip off", "`Disabled`, or `Text` left blank, shows no tooltip and "
                    + "adds no description, so a tooltip that only sometimes applies needs no `@if`. The switch here "
                    + "turns the delete button's tooltip off.", typeof(TooltipDisabledExample)),
                new("width", "Width and styling", "`MaxWidth` sets how wide a tooltip grows before wrapping, 18rem "
                    + "unless set. `BubbleClass` styles the bubble, and `Class` styles the anchor around the control.",
                    typeof(TooltipWidthExample)),
            ],
            Api = [typeof(TbTooltip), typeof(TooltipSettings)],
            Keyboard =
            [
                new(["Tab"], "Moving focus to the control shows its tooltip, and moving away hides it."),
                new(["Escape"], "Hides the tooltip, without also closing a dialog it is in."),
            ],
            Labeling =
            [
                "A tooltip is a description, not a name: its text goes into the `aria-describedby` of the first "
                    + "focusable element it wraps. That element still needs a name of its own.",
            ],
            Announcements =
            [
                "A tooltip that only repeats its control's `aria-label` isn't added, so it isn't read twice.",
                "The arrow from `ShowArrow` is decoration and isn't read out.",
            ],
            Accessibility =
            [
                "A click or tap on a button hides its tooltip, and neither the focus that click leaves nor the focus "
                    + "handed back by a dialog closed with the mouse shows it, so it doesn't cover what someone is "
                    + "working on. A text field shows its tooltip on any focus, since its user is about to type.",
                "Moving the pointer onto the tooltip keeps it open.",
                "Only one tooltip shows at a time: showing one, by hover or focus, hides any other, including one "
                    + "shown for keyboard focus.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Full),
            BeforeInteractive =
            [
                "Tooltips work before the page is interactive, and on static pages, once Blazor's script has loaded. "
                    + "A single script, started with Blazor's, shows every tooltip and adds its description, so "
                    + "tooltips never call the server, not even to set up.",
            ],
            Ready = new(".tb-tooltip-anchor", "the element it describes"),
            Target = new("tooltip", "Add an item") { OpenedBy = new("button", "Add"), OpenOnHover = true },
            Blocks = ["tb-tooltip", "tb-tooltip-anchor"],
            DataParts =
            [
                new("tooltip", "The tooltip bubble."),
                new("arrow", "The arrow that points at the trigger."),
            ],
            TestingTip = "`await page.Tb().Tooltip(trigger).TextAsync()` from `Trailblazor.Testing.Playwright` rests "
                + "the pointer on the trigger and returns the tooltip's text once it shows. By hand, tab to or hover "
                + "the trigger, since `FocusAsync` after a click isn't keyboard focus, then find the tooltip with "
                + "`GetByRole(AriaRole.Tooltip)`; "
                + "`ToHaveAccessibleDescriptionAsync` on the trigger checks what screen readers hear. In bUnit the "
                + "tooltip's text is in the markup from the start, with `role=\"tooltip\"`.",
        },
        new("menu", "Menu", ExampleCategory.Overlays,
            "A list of actions, options and links that opens from a button, with groups and submenus.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for a list of actions or options that opens from one button, such as a row's actions.",
            ],
            WhenNotToUse =
            [
                "For choosing one value in a form, use a [select](components/select), which keeps its value and posts "
                    + "with the form.",
                "For a panel with fields or text in it, use a [popover](components/popover).",
                "For an action that deserves its own button, use a [button](components/button).",
            ],
            Related = ["select", "popover", "button"],
            RightToLeftKeys = "Left opens a submenu and Right closes it.",
            Sections =
            [
                new("items", "Items", "A menu's items run `OnClick` when chosen, which closes the menu. `Icon` adds an "
                    + "icon, and `Disabled` turns an item off but keeps it focusable, marked `aria-disabled`, so "
                    + "people can still find it.", typeof(MenuExample)),
                new("submenus", "Submenus", "`TbSubMenu` opens a second list beside an item. Resting the pointer on "
                    + "the item opens the list without moving focus into it, and on the way there the pointer can "
                    + "cross other items without the submenu closing. This menu also sets `Icon`, which makes its "
                    + "trigger an icon button named by `AriaLabel`.", typeof(MenuSubmenuExample)),
                new("checks-and-radios", "Checkbox and radio items", "`TbMenuCheckboxItem` and `TbMenuRadioItem` keep "
                    + "a value in a menu. `TbMenuGroup` and `TbMenuRadioGroup` group items under a `Label`, and "
                    + "`TbMenuSeparator` draws a line between them.", typeof(MenuCheckExample)),
                new("keep-open", "Keeping the menu open", "`KeepOpen` on a checkbox or radio item keeps the menu open "
                    + "when the item is set, with focus still on it, so several can be set in a row.",
                    typeof(MenuKeepOpenExample)),
                new("links", "Links", "A menu item with `Href` is a link. `NewTab` opens it in a new tab and tells "
                    + "screen readers so. `NewTabHint` changes what they hear.", typeof(MenuLinkExample)),
                new("custom-trigger", "A custom trigger", "Put `@attributes=\"context.Attributes\"` on your own "
                    + "button inside `Trigger`. It takes the place of the menu's button and gets the built-in "
                    + "trigger's behavior and attributes. Give it a name too, such as an `aria-label` on an avatar.",
                    typeof(MenuTriggerExample)),
                new("trigger-style", "The trigger's look", "`Variant` sets how a menu's text trigger looks, and `Size` "
                    + "how big its built-in trigger is.",
                    typeof(MenuStyleExample)),
                new("placement", "Placement and width", "`Placement` picks the side a menu opens on, and `Align` lines "
                    + "it up with the trigger. Placed to the side in a narrow window, a menu or submenu wraps to fit, "
                    + "or overlaps what opened it if its items can't wrap that far. `FullWidth` stretches the trigger, "
                    + "and the menu opens at least as wide. "
                    + "A menu too long for the window opens on the side with more room and scrolls, so every item "
                    + "stays reachable.", typeof(MenuPlacementExample)),
                new("opening-from-code", "Opening from code", "`@bind-Open` opens or closes a menu from code and tells "
                    + "you when the user does. The items are drawn again each time the menu opens, so one whose text "
                    + "or `Disabled` depends on state that changed since the page last rendered is up to date. This "
                    + "menu also sets `Description`, which adds text that screen readers read after the trigger's "
                    + "name, such as a hint about other keys it takes. `AttachOnFirstUse` waits to set up a menu's "
                    + "script until its trigger is first focused, pointed at or clicked, which saves a call to the "
                    + "browser for each menu in a long list; the move handles on a data grid's or a sortable list's "
                    + "rows use it.", typeof(MenuOpenExample)),
                new("disabled", "Disabled", "`Disabled` on a menu turns off its trigger, built in or custom.",
                    typeof(MenuDisabledExample)),
            ],
            Api =
            [
                typeof(TbMenu), typeof(MenuTrigger), typeof(TbMenuItem), typeof(TbMenuCheckboxItem),
                typeof(TbMenuRadioGroup<>), typeof(TbMenuRadioItem<>), typeof(TbMenuGroup), typeof(TbMenuSeparator),
                typeof(TbSubMenu), typeof(MenuClasses),
            ],
            Keyboard =
            [
                new(["Enter", "Space", "Down"], "Opens the menu on its first item.") { When = "On the trigger" },
                new(["Up"], "Opens the menu on its last item.") { When = "On the trigger" },
                new(["Up", "Down"], "Moves through the items from the focused one, wrapping at the ends. Moving the "
                    + "pointer onto an item focuses it.") { When = "In the menu" },
                new(["Home", "End"], "Jumps to the first or last item.") { When = "In the menu" },
                new(["A letter"], "Jumps to the next item that starts with it.") { When = "In the menu" },
                new(["Enter", "Space"], "Chooses the focused item, follows a link, or goes into a submenu.")
                {
                    When = "In the menu",
                },
                new(["Right"], "Opens a submenu.") { When = "In the menu" },
                new(["Left"], "Closes a submenu.") { When = "In the menu" },
                new(["Escape"], "Closes one level at a time: a submenu first, then the menu, which returns focus to "
                    + "the trigger.") { When = "In the menu" },
                new(["Tab"], "Closes the menu and moves on.") { When = "In the menu" },
            ],
            Labeling =
            [
                "The trigger is named by `Label`, or with an `Icon`, by `AriaLabel`.",
            ],
            Announcements =
            [
                "The trigger is a button with `aria-haspopup=\"menu\"` and `aria-expanded`, and the list is a `menu` "
                    + "of "
                    + "`menuitem`s.",
                "Checkbox items are `menuitemcheckbox`es and radio items `menuitemradio`s, with `aria-checked`. A "
                    + "`TbMenuGroup` or radio group is a `group` named by its `Label`, and `TbMenuSeparator` is a "
                    + "`separator`.",
            ],
            Accessibility =
            [
                "A click outside the menu closes it, as Escape and Tab do.",
                "The highlight marks the focused item, which is what Enter chooses. A mouse moves it, and opening the "
                    + "menu puts it on the first item.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "The trigger already opens the list on click, Enter or Space, and a link in it already works. Until "
                    + "the page is interactive, the arrow keys don't move through the items, the list shows at the "
                    + "corner of the window rather than beside the trigger, and an item with `OnClick` does nothing.",
            ],
            Ready = new("[aria-haspopup=menu]", "the trigger"),
            Target = new("button", "Actions"),
            Blocks = ["tb-menu"],
            DataParts =
            [
                new("trigger", "The button that opens the menu."),
                new("menu", "The menu popup."),
                new("icon", "An item's icon."),
                new("label", "An item's text."),
                new("indicator", "The check or dot a checkbox or radio item shows."),
                new("chevron", "The arrow that shows an item opens a submenu."),
                new("submenu-trigger", "The item that opens a submenu."),
                new("group-label", "A group's heading."),
            ],
            Anatomy = """
                <TbMenu Label="…">
                    <TbMenuItem OnClick="…">…</TbMenuItem>
                    <TbMenuSeparator />
                    <TbMenuGroup Label="…">
                        <TbMenuCheckboxItem @bind-Checked="_on">…</TbMenuCheckboxItem>
                    </TbMenuGroup>
                    <TbMenuRadioGroup Label="…" @bind-Value="_value">
                        <TbMenuRadioItem Value="…">…</TbMenuRadioItem>
                    </TbMenuRadioGroup>
                    <TbSubMenu Label="…">
                        <TbMenuItem OnClick="…">…</TbMenuItem>
                    </TbSubMenu>
                </TbMenu>
                """,
            TestingTip = "Open the menu with its trigger, then find items by role: `GetByRole(AriaRole.Menuitem)`, "
                + "`Menuitemcheckbox` or `Menuitemradio`, with `ToBeCheckedAsync` for the last two. A group is "
                + "`GetByRole(AriaRole.Group, new() { Name = \"Units\" })`. The trigger gets `data-ready` once a "
                + "click on an item reaches the server; `await page.Tb().Menu(\"More options\").ChooseAsync(\"Share\", "
                + "\"Email\")` from `Trailblazor.Testing.Playwright` waits for it, opens the menu and its submenus, "
                + "and clicks the item. With `AttachOnFirstUse`, the trigger gets `data-ready` only once it's "
                + "first focused, pointed at or clicked, so `ChooseAsync` focuses it first.",
            Telemetry =
            [
                "With a `TelemetryName`, such as `\"row-actions\"`, it counts `trailblazor.control.shown` as it "
                    + "first renders and `trailblazor.control.used` each time it opens, by pointer or key, which is "
                    + "how often its items are seen.",
                "An item with its own `TelemetryName` counts `trailblazor.control.used` each time it is chosen. Items "
                    + "render hidden until the menu opens, so they count uses only.",
            ],
        },
        new("dialog", "Dialog", ExampleCategory.Overlays,
            "A window over the page, opened from code or written in markup, that asks for input or a decision.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for input or a decision people should deal with before going on.",
            ],
            WhenNotToUse =
            [
                "For a panel beside a button that people can leave by clicking elsewhere, use a "
                    + "[popover](components/popover).",
                "For a brief update that needs no answer, use a [toast](components/toast).",
                "For a message that belongs to part of the page, use an [alert](components/alert).",
            ],
            Related = ["popover", "toast", "alert"],
            Sections =
            [
                new("try-it", "Try it", "Use the controls, then open the dialog to see what each parameter does.",
                    typeof(DialogTryExample))
                {
                    Controls = new("<TbDialog @bind-Open=\"_open\" Title=\"Try this dialog\"{controls}>…</TbDialog>",
                        ExampleControl.Choice(nameof(DialogOptions.Size), DialogSize.Medium),
                        ExampleControl.Choice(nameof(DialogOptions.Position), DialogPosition.Center),
                        ExampleControl.Switch(nameof(DialogOptions.ShowHeader), true),
                        ExampleControl.Switch(nameof(DialogOptions.CloseButton), true),
                        ExampleControl.Switch(nameof(DialogOptions.DismissOnEscape), true),
                        ExampleControl.Switch(nameof(DialogOptions.DismissOnBackdrop), true))
                    {
                        AsOptions = true,
                    },
                },
                new("opening", "Opening a dialog", "`IDialogService.ShowAsync` opens a component that inherits "
                    + "`TbDialogContent<TResult>` and waits for its result. The content closes the dialog with "
                    + "`Dialog.CloseAsync(value)` or `Dialog.CancelAsync()`. A dialog with no result, such as one "
                    + "that only informs, inherits `TbDialogContent` and closes with `Dialog.ConfirmAsync()` in place "
                    + "of `CloseAsync`. `ShowAsync` then returns whether it was confirmed. Escape, the backdrop and "
                    + "the close button call `OnDismissAsync`, which cancels by default. Override it to run a cancel "
                    + "of your own, as this rename dialog does to ask before discarding a changed name.",
                    typeof(DialogExample), typeof(RenameDialog)),
                new("confirming", "Confirming and alerting", "`ConfirmAsync` asks a question with two buttons, and "
                    + "`AlertAsync` tells the user something with one. Focus starts on the first button, so a "
                    + "confirmation dialog starts on Cancel, and Enter doesn't confirm by accident. The alert's tone "
                    + "icon is decorative, so say what happened in the message.", typeof(DialogConfirmExample)),
                new("in-markup", "Written in markup", "`TbDialog` opens with `@bind-Open`, for a dialog that needs no "
                    + "class of its own, and its content sees the page's fields. `TbDialogDescription` describes any "
                    + "dialog, in markup or opened from code, so screen readers read that text with the title as the "
                    + "dialog opens.",
                    typeof(DialogMarkupExample)),
                new("commands", "Opening before the page is interactive", "Until the page is interactive, a "
                    + "`TbDialog` with an `id` renders closed, so a button with `commandfor` set to that id and "
                    + "`command=\"show-modal\"` opens it in browsers that support invoker commands, and the close "
                    + "button closes it. Where the browser supports `closedby`, Escape and the backdrop close it as "
                    + "`DismissOnEscape` and `DismissOnBackdrop` say, except that the backdrop closes it only if Escape "
                    + "may too. Set `commandfor` only while the page isn't interactive, and give the button an "
                    + "`OnClick` that opens the dialog, so the button still works once the page is interactive.",
                    typeof(DialogCommandExample)),
                new("updating", "Updating an open dialog", "`Show` returns a reference without waiting. The page can "
                    + "`Update` a dialog's parameters through it, and close the dialog when the work is done. This "
                    + "export dialog overrides `DefaultTitle`, so the page opens it without a title.",
                    typeof(DialogUpdateExample), typeof(ExportDialog)),
                new("run-time", "Choosing at run time", "`ShowDynamicAsync` opens a dialog by its type, with its "
                    + "parameters by name, for a form chosen while the app "
                    + "runs.", typeof(DialogDynamicExample), typeof(RenameDialog), typeof(NotesDialog)),
                new("size", "Size", "`Size` sets the widest a dialog grows, in rem so it follows the user's text size: "
                    + "`Small` 24rem (384px), `Medium` 32rem (512px), `Large` 48rem (768px), `ExtraLarge` 64rem "
                    + "(1024px) and `Full`. Pick the step nearest the content's need, or set `Width` to a CSS length "
                    + "for one between the steps. `Fit` makes the dialog as wide as its content, up to `Width` when "
                    + "that is set. No dialog grows past the window. `DialogSettings.Size`, `Width` and "
                    + "`CloseButton` set the defaults for the app's dialogs.", typeof(DialogSizeExample)),
                new("position", "Position", "`Position` sets where a dialog sits in the window, in the center unless "
                    + "set.", typeof(DialogPositionExample)),
                new("header", "The header", "`ShowHeader = false` hides a dialog's header, and the title still names "
                    + "the dialog. `CloseButton = false` removes the close button. `TbDialogTitle` shows richer "
                    + "content in place of the title; keep the title's text in it, since it names the dialog. "
                    + "`TbDialogTitle` renders inside an `h2`, so keep its content inline: text, icons and badges, but "
                    + "no block elements.",
                    typeof(DialogHeaderExample)),
                new("first-focus", "Where focus starts", "A dialog starts where Tab would, on its first field or "
                    + "button or a group's chosen radio, or on an element marked `autofocus`. "
                    + "`InitialFocus = DialogFocus.Dialog` starts on the dialog itself, so a screen reader begins at "
                    + "its title. For a question with a destructive answer, start on the "
                    + "safe choice, as here. On closing, focus goes back to whatever opened the "
                    + "dialog.", typeof(DialogFocusExample)),
                new("dismissing", "Dismissing", "`DismissOnEscape = false` and `DismissOnBackdrop = false` keep Escape "
                    + "and a click on the backdrop from closing a dialog that needs an answer. On a `TbDialog`, "
                    + "`CanDismiss` is asked first, and can keep it open. Closing from code doesn't ask.",
                    typeof(DialogDismissExample)),
                new("long-content", "Long content", "A dialog never grows past the window. Its header stays in view "
                    + "and its body scrolls, and the buttons in `TbDialogActions` scroll with the body. Give long "
                    + "content that has no controls `tabindex=\"0\"` and a name, so keyboard users can scroll it.",
                    typeof(DialogLongExample)),
                new("saving", "Saving and errors", "While a dialog saves, set `Loading` on its button, and "
                    + "`CanDismiss` can keep it open until the work ends. Show a failure in the dialog with an alert, "
                    + "so the user keeps what they typed and can try again.", typeof(DialogSavingExample)),
                new("non-modal", "Non-modal dialogs", "`Modal = false` leaves the page usable. Such a dialog can be "
                    + "`Draggable` by its header and `Resizable` by its bottom-end grip. Neither needs dragging: the "
                    + "header gains a Move button whose menu picks a place, and the grip is a Resize button whose menu "
                    + "picks a size. `Minimizable` and `Maximizable` add header "
                    + "buttons.", typeof(DialogWindowExample), typeof(RenameDialog)),
                new("changing", "Changing an open dialog's window", "An open dialog can change. Set `Modal`, `Size`, "
                    + "`Position`, `WindowState`, `ShowHeader` or `CloseButton` on its reference, or on `Dialog` from "
                    + "inside a `TbDialogContent<TResult>`, as this notes dialog does.",
                    typeof(DialogChangingExample), typeof(NotesDialog)),
                new("top-layer", "On the page instead of the top layer", "A dialog opens in the browser's top layer, "
                    + "above everything on the page. A popup another library adds to the page's body then opens "
                    + "beneath a modal and can't be used. `TopLayer = false`, or `DialogSettings.TopLayer` for the "
                    + "app, keeps the dialog on the page, so popups added later open above it and work. It draws at "
                    + "`--tb-dialog-z-index` (1100) plus its place among the open dialogs, so a popup that must stay "
                    + "above it needs a z-index well above that value. A dialog on the page stays beneath any dialog "
                    + "in the top layer, even when it comes to the front.",
                    typeof(DialogTopLayerExample)),
            ],
            Api =
            [
                typeof(IDialogService), typeof(DialogReference<>), typeof(DialogOptions), typeof(DialogSettings),
                typeof(TbDialog), typeof(TbDialogActions), typeof(TbDialogTitle), typeof(TbDialogDescription),
                typeof(DialogClasses),
            ],
            Keyboard =
            [
                new(["Escape"], "Cancels the topmost dialog, or a non-modal one that has focus, unless "
                    + "`DismissOnEscape` is false. A menu, select, autocomplete list, tooltip or closable overlay open "
                    + "inside it, or a drag, takes the key first."),
                new(["Left", "Right", "Up", "Down"], "Moves the dialog instead of opening the menu, as the button's "
                    + "description says.")
                {
                    When = "On the Move button",
                },
                new(["Left", "Right", "Up", "Down"], "Resizes the dialog instead of opening the menu, as the button's "
                    + "description says.")
                {
                    When = "On the Resize button",
                },
            ],
            Labeling =
            [
                "A native `<dialog>` named by its title. A blank title, in markup or passed to `IDialogService`, gets "
                    + "build warning TB002. Leaving the title out is fine when the content overrides `DefaultTitle`; "
                    + "otherwise `Show` throws. An icon in a `TbDialogTitle` is hidden from screen readers.",
            ],
            Announcements =
            [
                "`ConfirmAsync` and `AlertAsync` open an `alertdialog` described by its message.",
                "Toasts and announcements made while a modal is open appear inside it, so the inert page doesn't hide "
                    + "them.",
            ],
            Accessibility =
            [
                "A modal dialog makes the rest of the page inert, so focus and screen readers stay inside; a non-modal "
                    + "one leaves the page usable.",
                "A modal stops the page from scrolling behind it and makes up for the width of the scrollbar it takes "
                    + "away, so the page doesn't shift sideways. An element of your own with `position: fixed` against "
                    + "the window's right edge holds still too if it adds `var(--tb-scroll-lock-offset, 0px)` to its "
                    + "`right`.",
                "A click that starts and ends on the backdrop cancels a modal. `ConfirmAsync` and `AlertAsync` dialogs "
                    + "ignore it, so a stray click can't answer them.",
                "A non-modal dialog comes to the front when it's pressed or takes focus, along with any dialog nested "
                    + "in it.",
                "A dialog fades in and out with the motion tokens; reduced motion opens and closes it at once. As it "
                    + "starts to fade out, the page takes input again and focus goes back to its opener, unless focus "
                    + "had already left the dialog.",
                "A dialog that turns non-modal while open keeps focus where it was. One that turns modal moves focus "
                    + "into itself, unless focus is already there, and makes the page inert again.",
                "A `Minimizable` modal leaves the page usable while it is minimized, with its toasts and announcements "
                    + "back on the page, and blocks it again once restored; focus stays on the button that restored "
                    + "it.",
            ],
            Notes =
            [
                new("With `TopLayer = false`, a dialog draws its own backdrop, stops the page from scrolling, is "
                    + "marked `aria-modal` and makes the rest of the page inert itself, and keys pressed in popups "
                    + "added later stay there. A library that makes its popup's element before the dialog opens, such "
                    + "as a date picker's calendar made as the page loads, needs its selector in "
                    + "`DialogSettings.OutsidePopups`; Blazor's reconnect dialog and error bar always stay usable. A "
                    + "dialog opened from a modal in the top layer opens there too."),
                new("With `TopLayer = false`, a popup with a lower z-index still opens beneath the dialog, and a "
                    + "parent with a transform or a stacking context of its own can hold the dialog beneath the "
                    + "page.") { Pitfall = true },
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "A dialog opened from code waits until the page is interactive. Before then, a `TbDialog` with an `id` "
                    + "opens from a button's `commandfor` and closes from its close button, but buttons inside it that "
                    + "run code do nothing. On a prerendered page, a dialog opened this way closes as the page turns "
                    + "interactive.",
            ],
            Target = new("dialog", "Rename report") { OpenedBy = new("button", "Rename report") },
            Blocks = ["tb-dialog"],
            DataParts =
            [
                new("dialog", "The dialog window."),
                new("header", "The title bar, with the title and the window buttons."),
                new("title", "The dialog's title, in its header."),
                new("icon", "The tone icon beside a confirmation dialog's message."),
                new("move", "The Move menu: its button and its list of places for the dialog."),
                new("minimize", "The button that minimizes the dialog."),
                new("maximize", "The button that maximizes or restores the dialog."),
                new("close", "The button that closes the dialog."),
                new("description", "The dialog's description, under its title."),
                new("body", "The dialog's content."),
                new("actions", "The buttons at the bottom of the dialog."),
                new("resize",
                    "The Size menu: its button, which also drags to resize the dialog, and its list of sizes."),
            ],
            Anatomy = """
                @* A dialog as its own component, opened with IDialogService.ShowAsync *@
                @inherits TbDialogContent<TResult>

                <TbDialogDescription>…</TbDialogDescription>
                …
                <TbDialogActions>
                    <TbButton OnClick="() => Dialog.CloseAsync(…)">…</TbButton>
                </TbDialogActions>

                @* A dialog written in the page's markup *@
                <TbDialog Title="…" @bind-Open="_open">
                    <TbDialogTitle>…</TbDialogTitle>
                    …
                    <TbDialogActions>…</TbDialogActions>
                </TbDialog>
                """,
            TestingTip = "In bUnit, `this.ShowDialogAsync<TDialog, TResult>(title)` from `Trailblazor.Testing` hosts "
                + "and opens a dialog in one call, or `this.ShowDialogAsync<TDialog>(title)` for a dialog with no "
                + "result. "
                + "`IDialogService.OpenDialogs` lists the open dialogs, newest last, and `CloseAllAsync()` "
                + "cancels them all, which resets a page between tests. `Show` returns a reference whose `Result` "
                + "finishes when the dialog closes, and whose `ContentType` and `Parameters` say what it shows. In "
                + "bUnit, `FakeDialogService` records the dialogs opened through it, finds one with `Last<TDialog, "
                + "TResult>()`, `Last<TDialog>()` or `LastConfirm()`, "
                + "and answers confirms and alerts with `Answer`, as the Testing guide shows. In Playwright, "
                + "`page.Tb().Dialog(\"Delete report?\")` from "
                + "`Trailblazor.Testing.Playwright` finds a dialog by its title: `WaitForAsync()` returns it once "
                + "open, `ChooseAsync(\"Delete\")` clicks a button and waits for it to close, and `CancelAsync()` "
                + "presses Escape.",
            Telemetry =
            [
                "With `DialogOptions.TelemetryName`, such as `\"delete-trail\"`, or the `telemetryName` of "
                    + "`ConfirmAsync` and `AlertAsync`, it counts `trailblazor.control.shown` each time it opens, and "
                    + "`trailblazor.dialog.closed` and `trailblazor.dialog.duration` with `trailblazor.outcome`: "
                    + "`confirmed`, `cancelled`, `escape`, `backdrop` or `close_button`.",
                "Its `trailblazor.component` is the content's type, such as `EditTrailDialog`, `ConfirmDialog` for "
                    + "`ConfirmAsync` and `AlertAsync`, or `TbDialog` for one in markup. Its title and result are "
                    + "never recorded.",
                "The header's window buttons count `trailblazor.control.used` with `trailblazor.action` `minimize`, "
                    + "`maximize` or `restore`, so you can see whether anyone uses them. Changes made from code "
                    + "aren't counted.",
            ],
        },
        new("drawer", "Drawer", ExampleCategory.Overlays,
            "A panel that slides in from an edge of the window over the page, such as filters, details or a bottom "
                + "sheet of choices.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for a task beside the page that needs more room than a popover, such as filters, a form or an "
                    + "item's details, or on a phone for a sheet of choices from the bottom.",
            ],
            WhenNotToUse =
            [
                "For a question or a short form people must answer before going on, use a [dialog](components/dialog).",
                "For a few actions on one item, use a [menu](components/menu).",
                "For navigation that stays on the page, put the links in the page's own layout.",
            ],
            Related = ["dialog", "popover", "menu"],
            Pattern = new("dialog (modal)", "https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/"),
            Sections =
            [
                new("basic", "Drawer", "`@bind-Open` opens the drawer, and turns false when people close it. `Title` "
                    + "names it and shows in its header with a close button. Put its buttons in a `TbDialogActions`, "
                    + "which stays at the bottom however long the content is.", typeof(DrawerExample)),
                new("sides", "Sides and sheets", "`Side` sets the edge it slides in from: `End` unless set, or `Start`, "
                    + "`Top` or `Bottom`. Start and end follow the reading direction. A drawer at the top or bottom "
                    + "is a sheet across the window, as tall as its content.", typeof(DrawerSideExample)),
                new("non-modal", "Beside the page", "With `Modal=\"false\"` the page stays usable while the drawer is "
                    + "open, with no backdrop, so people can pick another item and see it in the same drawer.",
                    typeof(DrawerNonModalExample)),
            ],
            Api = [typeof(TbDrawer), typeof(DrawerSide)],
            Anatomy = """
                <TbDrawer Title="…" Side="…" @bind-Open="_open">
                    …
                    <TbDialogActions>…</TbDialogActions>
                </TbDrawer>
                """,
            Keyboard =
            [
                new(["Tab", "Shift+Tab"], "Moves between the drawer's controls, and never reaches the page behind "
                    + "a modal drawer."),
                new(["Escape"], "Closes the drawer, unless `CanDismiss` refuses."),
            ],
            Labeling =
            [
                "A native `<dialog>` named by its title, with a close button named \"Close\".",
            ],
            Accessibility =
            [
                "A drawer is a dialog docked to an edge, so it behaves as one: a modal drawer makes the rest of the "
                    + "page inert and keeps it from scrolling, a click on the backdrop closes it, and focus goes back "
                    + "to the button that opened it as it closes.",
                "It slides in and out with the motion tokens; reduced motion opens and closes it at once.",
            ],
            AppDuties =
            [
                "Open it from a button whose text says what the drawer holds, such as \"Filters\".",
            ],
            Modes = new(ModeSupport.None, ModeSupport.None),
            BeforeInteractive =
            [
                "No drawer opens until the page is interactive, because opening one takes a script.",
            ],
            Target = new("dialog", "Trail filters") { OpenedBy = new("button", "Filters") },
            Blocks = ["tb-drawer", "tb-dialog"],
            DataParts =
            [
                new("dialog", "The drawer's panel."),
                new("header", "The title bar, with the title and the close button."),
                new("close", "The button that closes the drawer."),
                new("body", "The drawer's content."),
                new("actions", "The buttons at the bottom of the drawer."),
            ],
            TestingTips =
            [
                "In Playwright, `page.Tb().Dialog(\"Trail filters\")` from `Trailblazor.Testing.Playwright` finds a "
                    + "drawer by its title, as it does a dialog: `WaitForAsync()` returns it once open, and "
                    + "`CancelAsync()` closes it with Escape. It skips a drawer that is still sliding out.",
                "In bUnit, `IDialogService.OpenDialogs` lists the open drawers along with the dialogs, and each one's "
                    + "`Options.Position` is its side.",
                "A drawer opens only once the page is interactive, so open it after the page is ready.",
            ],
            Telemetry =
            [
                "With a `TelemetryName`, it counts as a dialog does: `trailblazor.control.shown` each time it "
                    + "opens, and `trailblazor.dialog.closed` and `trailblazor.dialog.duration` with how it closed. "
                    + "Its `trailblazor.component` is `TbDialog`.",
            ],
        },
        new("focus-scope", "Focus scope", ExampleCategory.Overlays,
            "Manages focus for a custom overlay or panel: it moves focus in, can keep Tab inside, and gives focus "
                + "back when it goes.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it when you build your own panel or overlay and need it to handle focus correctly.",
            ],
            WhenNotToUse =
            [
                "For a modal window, use a [dialog](components/dialog), since a focus scope doesn't make the page "
                    + "behind inert or close on Escape.",
                "For a panel beside a button, use a [popover](components/popover).",
            ],
            Related = ["dialog", "popover", "overlay"],
            Sections =
            [
                new("trapping", "Trapping focus", "`Trapped` keeps Tab and Shift+Tab inside a focus scope and brings "
                    + "focus back if it leaves. When scopes nest, only the newest trapped one traps.",
                    typeof(FocusScopeExample)),
                new("not-trapped", "Without a trap", "Without `Trapped`, a focus scope still moves focus in and gives "
                    + "it back, and Tab moves on to the rest of the page.", typeof(FocusScopeOpenExample)),
                new("first-focus", "Where focus goes first", "When a focus scope appears, focus moves to the element "
                    + "marked `autofocus`, such as a field with `AutoFocus`, or else where Tab would, on its first "
                    + "control or a group's chosen radio, unless focus is already inside. `AutoFocus=\"false\"` on "
                    + "the scope leaves focus where it is.",
                    typeof(FocusScopeAutoFocusExample)),
                new("giving-focus-back", "Giving focus back", "When a focus scope is removed, focus returns to where "
                    + "it was, unless the user has moved it elsewhere. `RestoreFocus=\"false\"` leaves that to you, "
                    + "for a panel whose closing moves focus on by itself.", typeof(FocusScopeRestoreExample)),
            ],
            Api = [typeof(TbFocusScope)],
            Keyboard =
            [
                new(["Tab", "Shift+Tab"], "Keeps focus inside the scope.") { When = "With `Trapped`" },
            ],
            Accessibility =
            [
                "A focus scope adds no role or name of its own, so give a panel a `role` and an `aria-label`, as these "
                    + "examples do.",
                "Like the browser's own Tab, it counts an editable region and a media player with controls as tab "
                    + "stops.",
            ],
            Notes =
            [
                new("A focus scope's options apply when it first renders, so render it conditionally, as these "
                    + "examples do, to start it again."),
            ],
            Modes = new(ModeSupport.DisplayOnly, ModeSupport.DisplayOnly),
            BeforeInteractive =
            [
                "Its content renders in full. Focus doesn't move or stay inside until the page is interactive, because "
                    + "a script does both.",
            ],
            Target = new("group", "Find a trail") { OpenedBy = new("button", "Find a trail") },
            Blocks = ["tb-focus-scope"],
            DataParts =
            [
                new("focus-scope", "The element that keeps focus inside it."),
            ],
            TestingTip = "bUnit can't move focus, so test the trap in Playwright: press Tab from the last control in "
                + "the scope and expect the first to be focused with `ToBeFocusedAsync()`.",
        },
        new("popover", "Popover", ExampleCategory.Overlays,
            "A panel of content that opens beside a trigger, such as filters or a share form, and closes on Escape or "
                + "a click elsewhere.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for a little content that opens beside its button and that people can work in, such as filters "
                    + "or a share form.",
            ],
            WhenNotToUse =
            [
                "For a list of actions, use a [menu](components/menu).",
                "For a short hint with nothing to press, use a [tooltip](components/tooltip).",
                "For a decision people must make before going on, use a [dialog](components/dialog).",
            ],
            Related = ["menu", "tooltip", "dialog"],
            Sections =
            [
                new("trigger", "The trigger", "`Label` is the text on a popover's trigger. `Icon` makes the trigger an "
                    + "icon button, named by `AriaLabel`, and `Variant` and `Size` set how it looks.",
                    typeof(PopoverExample)),
                new("title", "Title", "`Title` adds a heading to the top of a popover's panel and names the panel for "
                    + "screen readers. Without one, the trigger names it.", typeof(PopoverTitleExample)),
                new("hover", "Opening on hover", "`OpenOnHover` also opens a popover when the pointer rests on its "
                    + "trigger, without taking focus, and keeps it open while the pointer is over it. A click on the "
                    + "trigger, or working in the panel, keeps it open after the pointer leaves, and resting on the "
                    + "trigger doesn't open it over another popover that is already open.",
                    typeof(PopoverHoverExample)),
                new("opening-from-code", "Opening from code", "`@bind-Open` opens or closes a popover from code and "
                    + "tells you when the user does. Here, the Apply button closes it, and a second button opens it. "
                    + "`AttachOnFirstUse` waits to set up a popover's script until its trigger is first focused, "
                    + "pointed at or clicked, or the panel is opened from code. That saves a call to the browser for "
                    + "each popover in a long list.", typeof(PopoverOpenExample)),
                new("placement", "Placement", "`Placement` picks the side a popover's panel opens on, and it flips to "
                    + "the other side when there's no room. Placed to the side in a narrow window, it wraps to fit, or "
                    + "overlaps the trigger if its content can't wrap that far. `Align` lines it up with the trigger. A "
                    + "panel too long for the window opens on the side with more room and scrolls, so everything in it "
                    + "stays reachable.",
                    typeof(PopoverPlacementExample)),
                new("custom-trigger", "A custom trigger", "Put `@attributes=\"context.Attributes\"` on your own "
                    + "button inside `Trigger`. It takes the place of the popover's button and gets the built-in "
                    + "trigger's behavior and attributes.", typeof(PopoverTriggerExample)),
                new("disabled", "Disabled", "`Disabled` turns off a popover's trigger, built in or custom, so neither "
                    + "a click, the keyboard nor the pointer resting on it opens the panel.",
                    typeof(PopoverDisabledExample)),
            ],
            Api = [typeof(TbPopover), typeof(PopoverTrigger)],
            Keyboard =
            [
                new(["Enter", "Space"], "Opens the panel and moves focus into it.") { When = "On the trigger" },
                new(["Escape"], "Closes the panel, and focus goes back to the trigger if it was in the panel.")
                {
                    When = "While open",
                },
                new(["Tab"], "Closes the panel when focus moves past its last control.") { When = "While open" },
            ],
            Labeling =
            [
                "The panel is a non-modal `dialog`, named by its `Title`, or by the trigger without one.",
            ],
            Announcements =
            [
                "The trigger is a button with `aria-haspopup=\"dialog\"`, `aria-controls` and `aria-expanded`.",
            ],
            Accessibility =
            [
                "A click elsewhere closes the panel. Escape works on a panel opened by `OpenOnHover` too, as WCAG "
                    + "1.4.13 asks.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "The trigger already opens the panel on click, Enter or Space, and Escape closes it. Until the page is "
                    + "interactive, hover doesn't open it, focus doesn't move into it, and it shows at the corner of "
                    + "the window rather than beside the button.",
            ],
            Ready = new("[data-part=trigger]", "the trigger"),
            Target = new("dialog", "Filter the report") { OpenedBy = new("button", "Filters") },
            Blocks = ["tb-popover"],
            DataParts =
            [
                new("trigger", "The button that opens the popover."),
                new("panel", "The popover panel."),
                new("title", "The panel's title."),
            ],
            TestingTip = "`await page.Tb().Popover(\"Filters\").OpenAsync()` opens it and returns the panel, a "
                + "`dialog` named by its title or trigger; `CloseAsync()` closes it. Both wait for the popover to be "
                + "ready. With `AttachOnFirstUse`, the trigger gets `data-ready` only when it's first focused, pointed "
                + "at or clicked, so both focus it first.",
        },
        new("popconfirm", "Popconfirm", ExampleCategory.Overlays,
            "A small question that opens beside the button that asked, such as \"Delete this trail?\", with Cancel and "
                + "a confirm button.")
        {
            WhenToUse =
            [
                "Use it before an action that's quick to ask about but worth a second look, such as deleting one row.",
            ],
            WhenNotToUse =
            [
                "For a decision that needs more explanation or input, use a [dialog](components/dialog).",
                "For an action that can be undone, act at once and offer Undo in a [toast](components/toast).",
            ],
            Related = ["popover", "dialog", "toast"],
            Sections =
            [
                new("basic", "Popconfirm", "`Title` asks the question and `Description` adds a sentence under it. "
                    + "`OnConfirm` runs when the user confirms, and `OnCancel` when they cancel, press Escape or click "
                    + "away. `ConfirmText` and `Tone` set the confirm button, such as Delete in the danger tone.",
                    typeof(PopconfirmExample)),
                new("slow", "A slow action", "The confirm button shows it's busy until `OnConfirm` finishes, and then "
                    + "the confirmation closes. `Icon` makes the trigger an icon button, named by `AriaLabel`. "
                    + "`Trigger` takes a button of your own, as on a [popover](components/popover#custom-trigger).",
                    typeof(PopconfirmSlowExample)),
            ],
            Api = [typeof(TbPopconfirm)],
            Keyboard =
            [
                new(["Enter", "Space"], "Opens the confirmation and moves focus to Cancel.")
                {
                    When = "On the trigger",
                },
                new(["Tab"], "Moves between Cancel and the confirm button. Tab past the confirm button closes the "
                    + "confirmation, which counts as canceling.") { When = "While open" },
                new(["Escape"], "Cancels, and focus goes back to the trigger.") { When = "While open" },
            ],
            Labeling =
            [
                "The confirmation is a non-modal `dialog` named by its `Title` and described by its `Description` or "
                    + "content.",
            ],
            Accessibility =
            [
                "Focus lands on Cancel as it opens, so a stray Enter doesn't confirm. The confirm button stays "
                    + "focusable while it's busy, so focus isn't lost.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "The trigger opens the confirmation and Escape closes it, as on a popover, but its buttons do nothing "
                    + "until the page is interactive.",
            ],
            Ready = new("[data-part=trigger]", "the trigger"),
            Target = new("dialog", "Delete Pine Hollow loop?") { OpenedBy = new("button", "Delete trail") },
            Blocks = ["tb-popconfirm", "tb-popover"],
            DataParts =
            [
                new("trigger", "The button that opens the confirmation."),
                new("panel", "The confirmation's panel."),
                new("title", "The question."),
                new("description", "The sentence under the question."),
                new("actions", "The row of buttons."),
                new("cancel", "The cancel button."),
                new("confirm", "The confirm button."),
            ],
            TestingTip = "`await page.Tb().Popconfirm(\"Delete trail\").ConfirmAsync()` opens it and confirms; "
                + "`CancelAsync()` cancels. In bUnit, click `[data-part=confirm]` or `[data-part=cancel]`, which work "
                + "without opening it first.",
            Telemetry =
            [
                "With a `TelemetryName`, such as `\"delete-trail\"`, it counts `trailblazor.control.shown` as it first "
                    + "renders, and `trailblazor.control.used` with `trailblazor.action` `confirm` or `cancel` for "
                    + "each answer.",
            ],
        },
        new("overlay", "Overlay", ExampleCategory.Overlays,
            "A scrim over the page or over one region, which blocks clicks behind it and can hold a message or a "
                + "spinner in its center.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it to cover the page or one region while something finishes, such as a card that's loading.",
            ],
            WhenNotToUse =
            [
                "For a message that needs an answer, use a [dialog](components/dialog), which moves focus and traps "
                    + "it.",
                "For content that's loading into a known shape, use a [skeleton](components/skeleton).",
            ],
            Related = ["dialog", "progress", "focus-scope"],
            Sections =
            [
                new("page", "Over the page", "An overlay covers the whole page, with `ChildContent` in its center. "
                    + "`CloseOnClick` closes it on a click outside that content, and `LockScroll` stops the page from "
                    + "scrolling behind it, without the page shifting sideways when its scrollbar disappears.",
                    typeof(OverlayExample)),
                new("contained", "Over one region", "`Contained` covers only the overlay's nearest positioned parent, "
                    + "here a card with `position: relative`, and `Blur` blurs what's behind it.",
                    typeof(OverlayContainedExample)),
                new("catching-clicks", "Catching clicks", "`Transparent` hides an overlay's scrim, so the overlay only "
                    + "catches clicks, and `OnClick` reports them. It has no `Blocking` here, so while the map is "
                    + "locked Tab still reaches the Zoom in button.", typeof(OverlayClickExample)),
            ],
            Api = [typeof(TbOverlay)],
            Keyboard =
            [
                new(["Escape"], "Closes the overlay, unless a menu, popover or dialog above it takes the key first. "
                    + "An overlay inside a dialog takes the key before the dialog.")
                {
                    When = "With `CloseOnClick`",
                },
            ],
            Announcements =
            [
                "The scrim is hidden from screen readers; the content in its center is read as usual.",
            ],
            Accessibility =
            [
                "An overlay blocks the pointer. With `Blocking`, it blocks the keyboard and screen readers too: what "
                    + "it covers turns inert while it shows, so Tab can't reach controls behind it, and if focus was "
                    + "behind it, it goes back there when the overlay closes. Dialogs opened through `IDialogService`, "
                    + "toasts and announcements stay usable; content added beside it after it opens isn't blocked.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "An overlay with `Open` already shows and catches the pointer. `Blocking`, `CloseOnClick` and "
                    + "Escape start working once the page is interactive.",
            ],
            Ready = new("[data-part=overlay]", "a visible overlay") { OpenedBy = new("button", "Show page overlay") },
            Target = null,
            Blocks = ["tb-overlay"],
            DataParts =
            [
                new("overlay", "The overlay itself."),
                new("scrim", "The shade over the page behind the content."),
                new("content", "The overlay's content."),
            ],
            TestingTip = "The overlay's element has `data-part=\"overlay\"`; click its `data-part=\"scrim\"` to "
                + "close one with `CloseOnClick`; the value bound with `@bind-Open` turns false.",
        },
        new("tabs", "Tabs", ExampleCategory.Navigation,
            "Switches between panels of related content, one at a time.")
        {
            RightToLeftKeys = "Left moves to the next tab and Right to the previous one.",
            FullWidth = true,
            WhenToUse =
            [
                "Use it to switch between related views of one thing, where people look at one at a time.",
            ],
            WhenNotToUse =
            [
                "For sections people may want open side by side, or read in order down the page, use an "
                    + "[accordion](components/accordion).",
                "For moving between pages, use links.",
            ],
            Related = ["accordion", "toggle-group", "pagination"],
            Sections =
            [
                new("try-it", "Try it", "Use the controls to see what each parameter does.",
                    typeof(TabsTryExample))
                {
                    Controls = new("<TbTabs AriaLabel=\"Try these tabs\"{controls}>\n    <TbTab "
                    + "Text=\"First\">…</TbTab>\n    <TbTab Text=\"Second\">…</TbTab>\n    <TbTab "
                    + "Text=\"Third\">…</TbTab>\n</TbTabs>",
                        ExampleControl.Choice(nameof(TbTabs.Variant), Variant.Text),
                        ExampleControl.Choice(nameof(TbTabs.Activation), TabActivation.Automatic),
                        ExampleControl.Choice(nameof(TbTabs.Size), Size.Medium),
                        ExampleControl.Switch(nameof(TbTabs.Vertical)), ExampleControl.Switch(nameof(TbTabs.Centered))),
                },
                new("tabs", "Tabs and panels", "Each `TbTab` adds a tab and its panel. `Text` or `TextContent` is the "
                    + "tab's name, so a count inside it is read as part of the name, and `Disabled` turns a tab off. "
                    + "`Key` and `@bind-ActiveKey` track the selected tab by a key you choose. Once the page is "
                    + "interactive, only the selected panel renders.",
                    typeof(TabsExample)),
                new("sizes", "Sizes", "`Size` is `Small`, `Medium` or `Large`, and sets the tabs' height and text "
                    + "size, the same steps as a button's.", typeof(TabsSizeExample)),
                new("variants", "Variants", "`Variant` marks the selected tab: `Text` and `Outlined` underline it, and "
                    + "`Soft` and `Filled` make it a pill, in vertical lists too. `--tb-tabs-indicator-color` and "
                    + "`--tb-tabs-indicator-size` restyle the underline.", typeof(TabsVariantExample)),
                new("vertical", "Vertical", "`Vertical` stands a tab list beside its panel, and `Icon` adds an icon to "
                    + "a tab. The icon isn't read out.", typeof(TabsVerticalExample)),
                new("by-index", "Selecting by position", "`@bind-ActiveIndex` tracks the selected tab by its position, "
                    + "from 0, for tabs that have no `Key`.", typeof(TabsIndexExample)),
                new("activation", "Manual activation", "With `Activation=\"TabActivation.Manual\"`, the arrow keys "
                    + "only move focus between tabs, and Enter or Space selects. Use it when showing a panel is slow.",
                    typeof(TabsManualExample)),
                new("keep-alive", "Keeping panels alive", "`KeepAlive` keeps a tab's panel rendered once it has been "
                    + "shown, and only hides it when another tab is selected, so what's typed in a field that isn't "
                    + "bound survives switching tabs.",
                    typeof(TabsKeepAliveExample)),
                new("centered", "Centered", "`Centered` centers a tab list in its row.", typeof(TabsCenteredExample)),
                new("overflow", "Many tabs", "A tab list too long for its row scrolls sideways, and moving focus to a "
                    + "tab brings it into view. The selected tab scrolls fully into view, leaving the page where it is.",
                    typeof(TabsOverflowExample)),
                new("scroll-buttons", "Scroll buttons", "`ScrollButtons` puts a button at each end of the row; a click "
                    + "scrolls the tabs by most of the row's width. They show only while the row overflows, each turns "
                    + "off at its own end, and they're for the pointer, since the arrow keys already bring each tab "
                    + "into view. A `Vertical` list has none.", typeof(TabsScrollButtonsExample)),
                new("forms", "A form across tabs", "`RenderAllPanels` renders every panel from the start, hidden, so a "
                    + "form across the tabs validates and saves every field. A hidden panel's fields can't take focus, "
                    + "so name the tab that needs attention with `Text` and a `HeaderClass`; a class alone changes "
                    + "only the look, which screen readers don't announce.",
                    typeof(TabsFormExample)),
                new("on-click", "Clicks on a tab", "A tab's `OnClick` is raised each time its header is clicked, or "
                    + "Enter or Space is pressed on it, even if it was already selected. It is raised after the tab is "
                    + "selected, and the arrow keys select without raising it.", typeof(TabsClickExample)),
                new("closable", "Closing tabs", "A tab with an `OnClose` shows a close mark, and Delete or Backspace "
                    + "closes it while it has focus. Closing raises `OnClose` without selecting the tab. Remove the tab "
                    + "there, and focus moves to the next enabled tab, or to the one before it when none follows; keep "
                    + "rendering it to leave it open, such as after asking about unsaved work.",
                    typeof(TabsClosableExample)),
                new("add", "Adding tabs", "`OnAdd` ends the row of tabs with an add button, named by `AddLabel`, or "
                    + "\"Add tab\" in English. Add a tab there, and select it to show it. The button stays at the row's "
                    + "end while the tabs scroll, and sits under them in a `Vertical` list.", typeof(TabsAddExample)),
            ],
            Api = [typeof(TbTabs), typeof(TbTab), typeof(TabsClasses)],
            Keyboard =
            [
                new(["Left", "Right"], "Moves between tabs and selects the one reached, unless activation is manual. "
                    + "Skips disabled tabs and wraps at the ends.")
                {
                    When = "In a horizontal list",
                },
                new(["Up", "Down"], "Moves between tabs and selects the one reached, unless activation is manual. "
                    + "Skips disabled tabs and wraps at the ends.")
                {
                    When = "In a `Vertical` list",
                },
                new(["Home", "End"], "Jumps to the first or last tab and selects it, unless activation is manual."),
                new(["Enter", "Space"], "Selects the focused tab.") { When = "With manual activation" },
                new(["Tab"], "Moves from the tab list, which is one tab stop, into the panel."),
                new(["Delete", "Backspace"], "Closes the focused tab.") { When = "On a tab with an `OnClose`" },
            ],
            Labeling =
            [
                "Give the list an `AriaLabel`.",
            ],
            Announcements =
            [
                "A `tablist` of `tab`s, each controlling a `tabpanel`. A `Vertical` list has "
                    + "`aria-orientation=\"vertical\"`.",
                "A tab that can close has `aria-keyshortcuts=\"Delete Backspace\"`. Its close mark is hidden from "
                    + "screen readers, since a tab can't hold a button.",
            ],
            Accessibility =
            [
                "Each panel is focusable, so keyboard users can reach content that has no controls.",
                "A screen reader on a touch screen can't reach a tab's close mark, and a double tap selects the tab, so "
                    + "when tabs can close, also offer a way to close one from its panel or a menu.",
                "In forced-colors mode, such as Windows high contrast, the selected tab keeps its underline in the "
                    + "system's highlight color, and a selected pill keeps an outline in it.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "Every tab's panel renders and the selected one shows, so each prerender includes every panel's "
                    + "content and anything it loads. A click selects a tab, as do the arrow keys with automatic "
                    + "activation. The browser's find selects the tab whose panel holds a match. A tab with no panel "
                    + "does nothing until the page is interactive. On a prerendered page, the selection goes back to "
                    + "`ActiveIndex` once the page is interactive. A tab with an `OnClose` shows its close mark, and tabs with "
                    + "an `OnAdd` show their add button, once the page is interactive.",
            ],
            Ready = new("[role=tablist]", "the tab list"),
            Target = new("tablist", "Report sections"),
            Blocks = ["tb-tabs"],
            DataParts =
            [
                new("list", "The row of tabs."),
                new("tab", "One tab."),
                new("tab-anchor", "A hidden marker that keeps a tab's place when tabs move."),
                new("icon", "A tab's icon."),
                new("close", "The close mark of a tab with an `OnClose`."),
                new("add", "The add button that `OnAdd` shows."),
                new("scroll-start", "The button that scrolls the row toward its start, with `ScrollButtons`."),
                new("scroll-end", "The button that scrolls the row toward its end, with `ScrollButtons`."),
                new("panel", "A tab's panel."),
            ],
            Anatomy = """
                <TbTabs AriaLabel="…" @bind-ActiveIndex="_index">
                    <TbTab Text="…">…</TbTab>
                    <TbTab Text="…">…</TbTab>
                </TbTabs>
                """,
            TestingTip = "Find tabs with `GetByRole(AriaRole.Tab, new() { Name = \"Summary\" })` and check "
                + "`aria-selected`; the shown panel is `GetByRole(AriaRole.Tabpanel)`. "
                + "`await cut.Tb().Tabs().SelectAsync(\"Summary\")` from `Trailblazor.Testing` picks a tab by its "
                + "text in bUnit, and `await page.Tb().Tabs(\"Report sections\").SelectAsync(\"Summary\")` from "
                + "`Trailblazor.Testing.Playwright` does in a browser, once the list is ready. On a static page, which "
                + "never becomes ready, click the tab instead. Both helpers also have `CloseAsync(\"Summary\")`, which "
                + "clicks the close mark of a tab with an `OnClose`, and `AddAsync()`, which clicks the add button.",
            Telemetry =
            [
                "A tab with a `TelemetryName`, such as `\"billing-tab\"`, counts `trailblazor.control.shown` as its "
                    + "header first renders and `trailblazor.control.used` each time the user selects it, or with "
                    + "`trailblazor.action` `close` when the user closes it.",
            ],
        },
        new("accordion", "Accordion", ExampleCategory.Navigation,
            "Sections that open and close under their headings.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for sections people read one or a few at a time, such as a list of questions.",
            ],
            WhenNotToUse =
            [
                "For switching between views of one thing, use [tabs](components/tabs).",
                "For a section nearly everyone needs, leave it on the page.",
            ],
            Related = ["tabs", "surface", "divider"],
            Sections =
            [
                new("items", "Items", "Each `TbAccordionItem` is a section with a `Title`. Opening one closes the "
                    + "others. `Expanded` opens one, and `Disabled` turns one off.", typeof(AccordionExample)),
                new("multiple", "Several open at once", "`Multiple` lets several items of an accordion stay open.",
                    typeof(AccordionMultipleExample)),
                new("from-code", "Opening from code", "`@bind-Expanded` opens or closes an accordion item from code "
                    + "and tells you when the user does.", typeof(AccordionBindExample)),
                new("actions", "Header actions", "`HeaderActions` sit at the end of an accordion item's header, beside "
                    + "its button and not in it, so they stay usable and out of the title's name.",
                    typeof(AccordionActionsExample)),
                new("title-content", "A richer title", "`TitleContent` shows content, such as a count, in place of an "
                    + "accordion item's `Title`, and it's read as part of the name.", typeof(AccordionTitleExample)),
                new("nested", "Nested items", "An item nested in another item's panel belongs only to an accordion "
                    + "inside that panel, or to none, so opening it never closes the item it sits in. An item's own "
                    + "`HeadingLevel` overrides the accordion's, so a nested one can sit below the item around it in "
                    + "the page's outline.", typeof(AccordionNestedExample)),
                new("on-its-own", "An item on its own", "A `TbAccordionItem` works outside an accordion, as a single "
                    + "section that opens and closes by itself.", typeof(AccordionItemExample)),
            ],
            Api = [typeof(TbAccordion), typeof(TbAccordionItem)],
            Keyboard =
            [
                new(["Tab"], "Moves between the titles, and reaches `HeaderActions` after their title."),
                new(["Enter", "Space"], "Opens or closes the focused section."),
                new(["Up", "Down"], "Moves between the titles, wrapping at the ends and skipping `HeaderActions`."),
                new(["Home", "End"], "Goes to the first or last title."),
            ],
            Labeling =
            [
                "Each panel is a `region` named by its title.",
            ],
            Announcements =
            [
                "Each title is a button with `aria-expanded` inside a heading, level 3 unless `HeadingLevel` says "
                    + "otherwise. Choose the level that fits the page's outline.",
            ],
            Accessibility =
            [
                "A closed panel is `inert`, so its content can't be reached until it opens. Opening and closing slide, "
                    + "except with reduced motion.",
                "When code closes a section while focus is in its panel, focus moves to that section's title, unless "
                    + "that title is disabled.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "Accordion sections show open or closed as `Expanded` sets them. A header opens and closes its "
                    + "section, and the browser's find opens the section holding a match. Tab, Enter and Space work from "
                    + "the start; Up, Down, Home and End move between headers only once the page is interactive. On a "
                    + "prerendered page, each section goes back to its `Expanded` value once the page is interactive.",
            ],
            Ready = new("[data-part=accordion]", "the accordion and each item's header"),
            Target = new("button", "Shipping"),
            Blocks = ["tb-accordion"],
            DataParts =
            [
                new("accordion", "The accordion itself."),
                new("item", "One section."),
                new("header", "A section's header, with its button and actions."),
                new("heading", "The heading that holds a section's button."),
                new("trigger", "The button that opens and closes a section."),
                new("chevron", "The arrow that shows whether a section is open."),
                new("actions", "The buttons beside a section's trigger."),
                new("panel", "A section's content."),
            ],
            Anatomy = """
                <TbAccordion>
                    <TbAccordionItem Title="…">…</TbAccordionItem>
                    <TbAccordionItem Title="…">…</TbAccordionItem>
                </TbAccordion>
                """,
            TestingTip = "`await page.Tb().AccordionItem(\"Sizing\").ExpandAsync()` from "
                + "`Trailblazor.Testing.Playwright` opens an item, in an accordion or on its own, once the page is "
                + "interactive, and hands back its panel. On a static page, click the item's header instead.",
        },
        new("tree-view", "Tree view", ExampleCategory.Navigation,
            "Shows nested items that open and close, with selection, checkboxes, a filter and children that load "
                + "when opened.")
        {
            FullWidth = true,
            RightToLeftKeys = "Left opens a row or moves into it, and Right closes it or moves to its parent. "
                + "Alt+Shift with Left or Right moves a row in or out the same way.",
            WhenToUse =
            [
                "Use it for items nested inside each other, such as folders or regions and their trails, where people "
                    + "open branches and pick items.",
            ],
            WhenNotToUse =
            [
                "To show and hide sections of content on a page, use an [accordion](components/accordion).",
                "For a flat list of options to pick from, use a [list box](components/listbox).",
                "For rows with several columns, use a [data grid](components/data-grid).",
            ],
            Related = ["accordion", "listbox", "data-grid", "menu"],
            Pattern = new("tree view", "https://www.w3.org/WAI/ARIA/apg/patterns/treeview/"),
            Sections =
            [
                new("filter-and-select", "Filter, select and open", "Rows come from `Items`. Typing in the field "
                    + "filters them and keeps each match's parents. Only trails can be selected, so a click on a region "
                    + "opens or closes it. Double-clicking a trail or pressing Enter on it opens it. `ItemEnd`, or a "
                    + "`TbTreeItem`'s `EndContent`, holds what sits at the end of a row, such as a count or a button. "
                    + "Clicks and keys there never select, open or activate the row.",
                    typeof(TreeViewExample)),
                new("multiple-selection", "Multiple selection", "`SelectionMode.Multiple` lets several rows be "
                    + "selected, and `SelectedKeys` holds their keys. A click selects one row, Ctrl or Cmd with a click "
                    + "adds or removes a row, and Shift with a click extends the selection. From code, "
                    + "`SelectAsync(key)` makes one row the only selected row and raises `SelectedKeysChanged`.",
                    typeof(TreeViewMultipleExample)),
                new("checkboxes", "Checkboxes", "Rows written as markup suit a tree that is part of the page. With "
                    + "`Checkable`, checking a parent checks its children, and a parent with only some of them "
                    + "checked shows as mixed. A `TbTreeItem` with no `Value`, such as Trips, opens, closes and is "
                    + "found by typing, but can't be selected or activated, and it is never in the bound keys.",
                    typeof(TreeViewCheckboxExample)),
                new("independent-checks", "Independent checks", "`CascadeChecks=\"false\"` checks only the row itself, "
                    + "so a parent and its children each keep their own check.",
                    typeof(TreeViewIndependentChecksExample)),
                new("disabled", "Disabled rows", "`ItemDisabled` turns off single rows, and `Disabled` turns off the "
                    + "whole tree.", typeof(TreeViewDisabledExample)),
                new("item-template", "Item template", "`ItemTemplate` draws a row's content. Keep `ItemText` so typing "
                    + "still finds the row.", typeof(TreeViewTemplateExample)),
                new("flat-data", "Flat data", "`ParentKeySelector` builds the tree from one list of every item, each "
                    + "naming its parent's key, with null for a root. Children keep the list's order.",
                    typeof(TreeViewFlatExample)),
                new("type-ahead", "Type-ahead", "Typing finds a row by its text. `TypeAhead=\"false\"` leaves letter "
                    + "keys to the page, for single-letter shortcuts that should work while the tree has focus.",
                    typeof(TreeViewTypeAheadExample)),
                new("load-on-open", "Children that load when opened", "`LoadChildren` loads a row's children the first "
                    + "time it opens, and `HasChildren` says which rows have a toggle before then. A loading row "
                    + "shows while they load. If the load fails, a row offers to try again, and `OnLoadFailed` gets the "
                    + "exception.", typeof(TreeViewLoadExample)),
                new("empty", "Empty", "With no rows, the tree says so. `EmptyContent` shows your own content, such as "
                    + "a way to add the first row.", typeof(TreeViewEmptyExample)),
                new("move-and-insert", "Move and insert", "With `OnMove`, rows move by dragging with a mouse, pen or "
                    + "finger, or with Alt+Shift and the arrows. A finger holds still for a moment first, so a swipe "
                    + "still scrolls. `OnMove` says what moved and where, and the app applies it, so it can be undone "
                    + "like any other change. A drag that rests on a closed row opens it. `CanMove` can refuse a move "
                    + "and say why. `InsertTemplate` puts content on each row's edge, such as the buttons here that "
                    + "add a row. It shows when the pointer reaches that edge or the content has keyboard focus, and "
                    + "on a touch screen for the focused row.",
                    typeof(TreeViewMoveExample)),
                new("many-rows", "Many rows", "With `Virtualize`, only the rows in view render, so ten thousand rows "
                    + "scroll smoothly. Every row is `ItemSize` pixels tall, and the rows scroll in a tree given a "
                    + "height or in the page's own scroll. Here `SelectionFollowsFocus`, which applies to single "
                    + "selection, selects each row the arrows, Home, End or typing move to. `Virtualize` needs `Items`.",
                    typeof(TreeViewVirtualExample)),
                new("content-instead-of-rows", "Content instead of rows", "A row shows its toggle once a `TbTreeItem` "
                    + "inside it exists. Other content doesn't count, so a row holding only other content needs "
                    + "`HasChildren`. With `HasChildren`, the content renders only while the row is open: none of it "
                    + "exists or runs until the row opens, and closing the row removes all of it. `@bind-Expanded` "
                    + "opens a `TbTreeItem` from code.", typeof(TreeViewContentExample)),
                new("open-state", "Open state kept on your items", "When your items keep their own open state, such as "
                    + "an `IsOpen` flag, give `ExpandedKeys` the keys of the open items and update the items from "
                    + "`ExpandedKeysChanged`.", typeof(TreeViewOpenStateExample)),
                new("labels", "Built-in text", "`Labels` replaces the words the tree shows and reads out while "
                    + "children load, when they fail and when it has no rows.", typeof(TreeViewLabelsExample)),
            ],
            Api =
            [
                typeof(TbTreeView<,>), typeof(TbTreeItem<>), typeof(TreeViewLabels), typeof(TreeMove<>),
                typeof(MoveCheck), typeof(TreeInsertContext<>), typeof(TreeViewClasses),
            ],
            Keyboard =
            [
                new(["Tab"], "Reaches the tree once. Each control at a row's end and each insert slot is a tab stop "
                    + "after its row.") { When = "Moving around" },
                new(["Up", "Down"], "Moves between rows.") { When = "Moving around" },
                new(["Home", "End"], "Goes to the first or last row.") { When = "Moving around" },
                new(["Right"], "Opens a row, or moves into it.") { When = "Moving around" },
                new(["Left"], "Closes a row, or moves to its parent.") { When = "Moving around" },
                new(["*"], "Opens the row's siblings.") { When = "Moving around" },
                new(["A letter"], "Finds a row by its text. Pressed again quickly, it moves to the next row that "
                    + "starts with that letter.") { When = "Moving around" },
                new(["Enter"], "Selects and activates the row and, like a double-click, opens or closes it if it has "
                    + "children. On a failed row, tries again.") { When = "Selecting" },
                new(["Space"], "Selects the row, or checks it in a `Checkable` tree. In a `Multiple` tree, selects or "
                    + "unselects it.") { When = "Selecting" },
                new(["Shift+Up", "Shift+Down", "Shift+Home", "Shift+End", "Shift+Space"], "Extends a `Multiple` "
                    + "selection from the row last chosen, or from the row focus leaves when there's none or it's "
                    + "hidden.") { When = "Selecting" },
                new(["Ctrl+A"], "In a `Multiple` tree, selects every row that can be selected and isn't inside a closed "
                    + "row.")
                    { When = "Selecting" },
                new(["Ctrl+Up", "Ctrl+Down", "Ctrl+Home", "Ctrl+End"], "With `SelectionFollowsFocus`, moves focus "
                    + "alone. Cmd does the same.") { When = "Selecting" },
                new(["Alt+Shift+Up", "Alt+Shift+Down"], "Moves the row past its neighbor.") { When = "Moving rows" },
                new(["Alt+Shift+Right"], "Moves the row into the row above.") { When = "Moving rows" },
                new(["Alt+Shift+Left"], "Moves the row out of its parent.") { When = "Moving rows" },
                new(["Escape"], "Cancels a drag.") { When = "Moving rows" },
            ],
            Labeling =
            [
                "It is a `tree` named by `AriaLabel`. With no rows, it is a `group` with that name, holding its "
                    + "message.",
                "A row is named by everything it shows, what's at its end included, so keep actions there short or "
                    + "give icon buttons an `AriaLabel`.",
            ],
            Announcements =
            [
                "A load that fails is read out.",
                "A move is read out, such as \"Lake camp moved before Pine Hollow.\"",
                "The reason `CanMove` gives for refusing a place is read out when the move is tried.",
            ],
            Accessibility =
            [
                "A drag held near the top or bottom of what scrolls keeps it scrolling, so a drop can reach any row.",
                "Each visible row is a `treeitem` carrying `aria-level`, `aria-setsize`, `aria-posinset`, "
                    + "`aria-expanded`, and `aria-selected` or `aria-checked`.",
                "The arrows reach the loading row and the failed row, and focus moves on to the children once they "
                    + "arrive.",
                "A `TbTreeItem` with no `Value` has no `aria-selected`.",
                "Each movable row is described by the keys that move it.",
                "With `Virtualize`, each row still says its level, size and place, the keys and typing reach rows that "
                    + "aren't rendered yet, and the focused row stays rendered.",
            ],
            AppDuties =
            [
                "The arrow keys move between rows and pass over other content in the tree, so someone using them can "
                    + "miss it. Keep what people need in the rows, or show it beside the tree for the selected row, as "
                    + "the [Master and detail recipe](recipes/master-detail) does.",
            ],
            Modes = new(ModeSupport.DisplayOnly, ModeSupport.DisplayOnly),
            BeforeInteractive =
            [
                "A `TbTreeItem` renders a plain row, open to `InitialExpandDepth`.",
                "With `Virtualize`, every row renders.",
            ],
            Notes =
            [
                new("After changing `Items` in place, call `RefreshAsync`. A new collection, or an `OnMove`, builds the "
                    + "rows again by itself.") { Pitfall = true },
                new("With `Items`, choosing, opening or checking a row draws again only the rows that change, so a "
                    + "long tree stays quick without `Virtualize`. Every row draws again when the tree's parent renders "
                    + "or `RefreshAsync` runs, so an `ItemTemplate` that shows state from outside the tree updates only "
                    + "then.") { Pitfall = true },
                new("Rows written as nested `TbTreeItem`s work from any component inside the tree. Each draws its row "
                    + "where it is written, followed by a `group` holding what is written inside it, so other content "
                    + "between rows shows in place and hides with its parent."),
                new("A tree inside another tree's row is a tree of its own: its rows take their own keys, focus, clicks "
                    + "and drags, the outer tree's arrows pass over them, and the outer row isn't selected by them."),
                new("With cascading checks, a `TbTreeItem` with no `Value` checks the rows inside it. Such a row is "
                    + "never in the bound keys, so several of them can share a text without needing keys."),
                new("`ExpandAllAsync` opens rows with `HasChildren` too, and the rows that appear in them."),
                new("Put a row's buttons and menus in `ItemEnd`, or a `TbTreeItem`'s `EndContent`, not in "
                    + "`ItemTemplate`. `ItemTemplate` renders in the row's content (`data-part=\"content\"`), where a "
                    + "click selects the row, or opens it when it can't be selected. The end (`data-part=\"end\"`) "
                    + "keeps clicks and keys to itself."),
                new("A row's `data-key` is its key's text for a string, number or date key. Any other key gets a "
                    + "made-up id such as `key-3`, and so does a key whose text is empty or that another key already "
                    + "has."),
            ],
            Ready = new(".tb-tree", "the tree"),
            Target = new("tree", "Trails by region"),
            Blocks = ["tb-tree"],
            DataParts =
            [
                new("item", "One row."),
                new("group", "A row's children."),
                new("spacer", "Space that stands in for rows a large tree leaves out of the page."),
                new("toggle", "The arrow that opens and closes a row."),
                new("check", "A checkable tree's checkbox."),
                new("content", "The row's text or template."),
                new("end", "Content at the end of the row, such as a count or a menu."),
                new("insert", "The slot between rows that holds InsertTemplate content, such as an add button."),
                new("loading", "The message shown while rows load."),
                new("failed", "The message and Retry button shown when rows fail to load."),
                new("empty", "The message shown when there are no rows."),
                new("drop-reason", "The reason shown while a dragged row is held over a place it may not go."),
            ],
            Anatomy = """
                <TbTreeView TItem="string" TKey="string" AriaLabel="…">
                    <TbTreeItem Value="…" Text="…">
                        <TbTreeItem Value="…" Text="…" />
                    </TbTreeItem>
                    <TbTreeItem Value="…" Text="…" />
                </TbTreeView>
                """,
            TestingTips =
            [
                "In Playwright, `page.Tb().Tree(\"Trails by region\")` from `Trailblazor.Testing.Playwright` follows "
                    + "rows by their path of texts: `ExpandAsync(\"River valley\", \"Fern Gully\")`, "
                    + "`SelectAsync(...)`, `CheckAsync(...)` and `ItemAsync(...)`.",
                "`ItemsAsync(...)` returns every row at a path, for rows that share their text. `ExpandAllAsync()` "
                    + "opens every row, waiting for rows that load or that a `TbTreeItem` renders once open.",
                "`DragBeforeAsync`, `DragAfterAsync` and `DragIntoAsync` drag one row's path onto another's.",
                "The helpers follow only the tree's own rows, not those of a tree inside a row.",
                "In a virtualized tree, the helpers scroll until the row renders, and a drag needs both rows in view "
                    + "together. Rows also carry `data-index` and `data-inside`, the number of rows in their branch.",
                "Every row carries `data-key`, `data-level` and `data-text`, and a row being dragged over carries "
                    + "`data-drop`.",
                "A tree of `TbTreeItem`s sets `aria-busy` while items it has just been given become rows.",
                "In bUnit, `cut.Tb().Tree(\"Trails by region\")` from `Trailblazor.Testing` has the same `ExpandAsync`, "
                    + "`ExpandAllAsync`, `SelectAsync`, `CheckAsync`, `DragBeforeAsync`, `DragAfterAsync` and "
                    + "`DragIntoAsync`, with `Texts` for the rows that show and `Item` and `Items` to find rows. A drag "
                    + "step moves what a drag in the browser would, selected rows included, and goes through `CanMove` "
                    + "and `OnMove`.",
            ],
            Telemetry =
            [
                "With a `TelemetryName`, such as `\"folders\"`, it counts `trailblazor.control.shown` as it first "
                    + "renders, and `trailblazor.control.used` with `trailblazor.action` `expand`, `collapse`, "
                    + "`select`, `check`, `activate` or `move` for each of those the user does, by pointer or key.",
                "Rows opened, selected or scrolled to from code aren't counted, and which rows were used is never "
                    + "recorded.",
            ],
        },
        new("data-grid", "Data grid", ExampleCategory.Content,
            "Shows items as rows of a table, with sortable and filterable columns, paging and row selection.")
        {
            RightToLeftKeys = "Left moves to the next cell and Right to the previous one.",
            FullWidth = true,
            WhenToUse =
            [
                "Use it for rows of data people sort, filter, page through, select or edit.",
            ],
            WhenNotToUse =
            [
                "For a few facts people only read, use a [table](components/table).",
                "For a short set of items that each have their own actions, use a [list](components/list).",
                "For items nested inside each other, use a [tree view](components/tree-view).",
            ],
            Related = ["table", "list", "tree-view", "pagination"],
            Sections =
            [
                new("sort-page-and-select", "Sort, page and select", "Click a header to sort by it: the next click "
                    + "reverses it, and the next stops sorting by it. Shift adds a column to the sorts. Pages come "
                    + "with a line such as \"1–5 of 7\" and a `TbPagination`. `ShowFirstLast` adds first and last "
                    + "page buttons to it, and `PageSizes` adds a list of page sizes. `Multiple` selection gives each "
                    + "row a checkbox and adds one to the header that selects every row on the page. `Single` uses "
                    + "radio buttons. A column's `Cell` template can hold buttons.",
                    typeof(DataGridExample)),
                new("row-click", "Clicking a row", "With a `SelectionMode` set, a click on a row selects it, unless the "
                    + "click lands on a button, link or field inside the row. `RowClickSelects=\"false\"` leaves "
                    + "selecting to the checkboxes, and `OnRowClick` reports the click. The second click of a "
                    + "double-click doesn't count, and neither does a drag that selects a row's text to copy it. In a "
                    + "`Multiple` grid, Shift with a click on a row or its checkbox selects the range from the row last "
                    + "chosen. In a virtualized grid with a `DataProvider`, the range covers only the rows it has loaded.",
                    typeof(DataGridRowClickExample)),
                new("columns", "Column options", "`Width` sets a column's width and `Align` lines up its cells. Without "
                    + "`Align`, numbers line up at the end and everything else at the start. `SortBy` sorts by "
                    + "something other than what the column shows, such as a rank, and `Sorts` sets the sort the grid "
                    + "starts with.", typeof(DataGridColumnsExample)),
                new("row-classes", "Row classes", "`RowClass` gives a row a class of your own, from its item. Say the "
                    + "same thing in words too, as Status does here, since color and strikethrough don't reach "
                    + "everyone.", typeof(DataGridRowClassExample)),
                new("filter", "Filter", "A `Filterable` column gets a filter under its header that suits its "
                    + "field: text it contains, a number it equals, a range of dates, a choice of an enum's values, "
                    + "or yes or no for a bool. A text filter applies when typing pauses, and the others as soon as "
                    + "they change. The grid then goes back to its first page. Bind `Filters` to read or set them.",
                    typeof(DataGridFilterExample)),
                new("filter-menu", "Filter menu", "With `FilterMode` set to `Menu`, each filterable header gets a "
                    + "filter button whose menu takes a condition and a value, or has checkboxes for the values of an "
                    + "enum, a bool or a column's `FilterChoices`. Nothing applies until you press Apply, or Enter in "
                    + "the value field. Clear removes the column's filter. A `FilterTemplate` draws a filter of your "
                    + "own.",
                    typeof(DataGridFilterMenuExample)),
                new("group-rows", "Group rows", "The rows can be grouped by any `Groupable` column, picked from the "
                    + "\"Group by\" list or set from code through `Groups`. Groups nest in the order they are picked, "
                    + "and each starts with a heading that shows its row count and opens or closes the group. "
                    + "`GroupTotals` adds a row under each open group with that group's totals.",
                    typeof(DataGridGroupExample)),
                new("totals", "Totals", "A column's `Aggregate` shows its `Sum`, `Average`, `Count`, `Min` or `Max` "
                    + "in a totals row under the rows. Each total covers every row the filters leave, not only the "
                    + "page shown. `AggregateBy` works out any other total from the rows in `Items`, and "
                    + "`FooterTemplate` draws the cell. "
                    + "A `DataProvider` is asked for the totals in `GridRequest.Aggregates` and gives them back in "
                    + "`GridResult.Totals`.", typeof(DataGridTotalsExample)),
                new("detail-rows", "Detail rows", "`DetailTemplate` starts each row with a button that opens a row of "
                    + "details under it, here another grid of the trail's stages. `@bind-ExpandedItems` "
                    + "tells you which rows are open and opens them from code, and `SingleDetail` keeps one open at "
                    + "a time. With cell navigation, the arrow keys reach the details as one wide cell: Enter goes "
                    + "into them and Escape comes back out. They keep their own Tab stops, so a grid inside them "
                    + "works as it would anywhere. A grid that virtualizes its rows can't show details.",
                    typeof(DataGridDetailExample)),
                new("export", "Export", "`Exportable` adds an Export menu that downloads the rows as a CSV file or an "
                    + "Excel workbook: the rows the filters leave, from every page and in the order they show, with "
                    + "the columns shown. The file is named for the grid's `AriaLabel`, or for `ExportFileName` when "
                    + "it's set. `ExportCsvAsync` and `ExportExcelAsync` return the same content to your own code, and "
                    + "with `GridExportRows.All` they return every row and column. A CSV holds each cell's text as the "
                    + "grid shows it; a workbook keeps numbers, dates and yes-or-no values as values a spreadsheet can "
                    + "sum and sort. In a CSV, text a spreadsheet would run as a formula gets an apostrophe in front, "
                    + "and a column drawn only by its `Cell` template has nothing to export.",
                    typeof(DataGridExportExample)),
                new("header-bands", "Header bands", "`TbColumnGroup` puts a heading over the columns inside it, in a "
                    + "row above their headers. Groups nest, and each level adds a row. Sorting, resizing, hiding and "
                    + "pinning work on the columns as usual; a heading spans the columns of its group that show, and "
                    + "splits where some of them are pinned and others aren't.", typeof(DataGridBandsExample)),
                new("pinned-columns", "Pinned columns", "A column's `Pinned` keeps it in view at the start or end "
                    + "of the row while the rows scroll sideways. A column pinned at the start keeps the selection "
                    + "column with it.", typeof(DataGridPinnedExample)),
                new("resize-columns", "Resize, move and hide columns", "With `ResizableColumns`, the user resizes "
                    + "a column by dragging its header's edge, or from its column menu, which the keys and a single "
                    + "click reach. The first resize fixes every column at its width, so the others hold still. A "
                    + "column's `MinWidth` is the narrowest it goes, and `Resizable=\"false\"` opts it out. With "
                    + "`ReorderableColumns`, dragging a header or choosing from its menu moves a column earlier or "
                    + "later; a pinned column moves only among the columns pinned to its side. With "
                    + "`HideableColumns`, the user hides and shows columns from a Columns menu above the rows, or "
                    + "hides one from its own menu; at least one column always shows, and `Hideable=\"false\"` keeps "
                    + "a column in view. `State` keeps the widths, the order and the hidden columns.",
                    typeof(DataGridResizeExample)),
                new("height", "Fixed height and scrolling to a row", "`Height` gives the rows a box of their own to "
                    + "scroll in, under headers that stay in view. Without cell navigation, Tab reaches the box when the rows "
                    + "overflow it, so the arrow keys can scroll it. `ScrollToAsync` goes to the page that holds a row "
                    + "and scrolls to it.",
                    typeof(DataGridHeightExample)),
                new("virtual-rows", "Virtual rows", "With `Virtualize` and a `Height`, only the rows in view are "
                    + "drawn, so the grid can hold thousands of rows without pages. `ScrollToAsync` scrolls to any "
                    + "row held in memory. With a `DataProvider`, rows load as they scroll into view, and "
                    + "`ScrollToAsync` finds only a loaded row.",
                    typeof(DataGridVirtualExample)),
                new("cell-navigation", "Cell navigation", "With `CellNavigation`, the grid is one stop for Tab, and "
                    + "the arrow keys move between its cells. A cell holding just a button, link, checkbox or radio "
                    + "button hands focus to it, and Enter goes into any other cell's fields.",
                    typeof(DataGridKeyboardExample)),
                new("edit-cells", "Edit cells", "An `Editable` column's cells are edited in place: double-click one, "
                    + "press Enter or F2 on it, or start typing. Enter saves, Escape cancels, Tab saves and opens the next "
                    + "editable cell, and leaving the field saves too. "
                    + "Each value is checked by its field's data annotations and the column's `Validate`, and "
                    + "`OnCellEdit` gets each saved edit. An `Editable` column turns on cell navigation.",
                    typeof(DataGridEditExample)),
                new("edit-rows", "Edit rows", "With `EditMode` set to `Row`, an edit opens every editable cell of "
                    + "the row: Tab moves between its fields and its Save and Cancel buttons, Enter saves the row, "
                    + "Escape cancels it, focus leaving the row saves it, and `OnRowEdit` gets all of its values at "
                    + "once.", typeof(DataGridRowEditExample)),
                new("save-state", "Save the view", "`State` holds the rows' sorts, filters, groups, page and page size "
                    + "in one `GridState`. Bind it to save how the user arranged the rows, such as to local storage "
                    + "as JSON, and set it to restore them; filter values read back from JSON take their columns' "
                    + "types.", typeof(DataGridStateExample)),
                new("links", "Sort and page links", "`StateHref` turns the sort headers and the pager into links to "
                    + "the view each one leads to, so sorting and paging work in static SSR and the grid's place is in "
                    + "the address, ready to share or reload. `GridState.ToQueryParameters` writes the sorts, page and "
                    + "page size for the address, and `GridState.FromQuery` reads them back; a prefix keeps two grids "
                    + "apart. A sort link sorts by its column alone, since a Shift- or Ctrl-click opens a link in a new "
                    + "window or tab instead of adding a sort.", typeof(DataGridLinksExample)),
                new("empty", "Empty", "With no rows, the grid says so. `EmptyContent` shows your own content, such as "
                    + "a way to add the first row.",
                    typeof(DataGridEmptyExample)),
                new("loading", "Loading", "Set `Loading` while the rows for `Items` are on their way. The table is "
                    + "`aria-busy`, and with no rows yet a message shows. A `DataProvider` does this by itself.",
                    typeof(DataGridLoadingExample)),
                new("failed-load", "A failed load", "When a `DataProvider` fails, the grid says so and offers to try "
                    + "again, and `OnLoadFailed` gets the exception. `RefreshAsync` loads again from code.",
                    typeof(DataGridFailedLoadExample)),
                new("server-data", "Rows from a server", "A `DataProvider` loads one page at a time, sorted by the "
                    + "server, which says how many rows there are in all. It receives the page, the sorts and the "
                    + "filters the grid asks for, and a new request cancels the last one. The [data grid from a "
                    + "database](recipes/grid-from-database) recipe does this with an EF Core query.",
                    typeof(DataGridServerExample)),
                new("reorder-rows", "Reorder rows", "With `OnRowMove`, rows held in `Items` can be put in order while "
                    + "the grid isn't sorted or filtered. Each row gets a handle in an \"Order\" column: drag it, press "
                    + "the arrow keys on it, or choose from its menu. Moves can cross pages, and the app applies each "
                    + "one. `CanMoveRow` can refuse a move and say why.", typeof(DataGridReorderExample)),
                new("labels", "Built-in text", "`Labels` replaces the words the grid shows and reads out, such as the "
                    + "header checkbox's name, the page size list's label and the count of rows shown.",
                    typeof(DataGridLabelsExample)),
            ],
            Api =
            [
                typeof(TbDataGrid<>), typeof(TbColumn<>), typeof(TbColumnGroup<>), typeof(DataGridLabels),
                typeof(DataGridClasses),
                typeof(GridRequest),
                typeof(GridResult<>), typeof(GridSort), typeof(GridFilter), typeof(FilterOperator),
                typeof(GridFilterMode), typeof(GridFilterContext), typeof(ColumnPin), typeof(GridCellEdit<>),
                typeof(GridCellEditContext<>), typeof(GridEditMode), typeof(GridRowEdit<>),
                typeof(GridState), typeof(ColumnAggregate), typeof(GridAggregate),
                typeof(SortableMove<>), typeof(MoveCheck),
            ],
            Keyboard =
            [
                new(["Tab"], "Reaches every control in the grid, each a native button, link or input. When the rows "
                    + "overflow their box, it also reaches that box.") { When = "Without cell navigation" },
                new(["Up", "Down", "Left", "Right"], "With focus on that box, scrolls the rows.")
                    { When = "Without cell navigation" },
                new(["Enter", "Space"], "On a sortable header, sorts by it alone, then reverses it, then stops sorting "
                    + "by it.") { When = "Sorting" },
                new(["Shift+Enter"], "On a sortable header, adds it to the sorts.") { When = "Sorting" },
                new(["Shift+Enter"], "On a sort link, opens the sorted view in a new window.")
                {
                    When = "With a `StateHref`",
                },
                new(["Space"], "On a row's checkbox, selects or unselects the row. On its radio button, selects "
                    + "the row.") { When = "Selecting" },
                new(["Shift+Up", "Shift+Down"], "Extends the selected rows, and moving back shrinks the range.")
                    { When = "With cell navigation and `SelectionMode.Multiple`" },
                new(["Ctrl+A", "Cmd+A"], "Selects every row on the page, the same rows as the header's checkbox. A "
                    + "virtualized grid with a `DataProvider` leaves these keys to the browser.")
                    { When = "With cell navigation and `SelectionMode.Multiple`" },
                new(["Tab"], "Reaches the grid once, and then the controls in any open details.")
                    { When = "With cell navigation" },
                new(["Up", "Down", "Left", "Right"], "Moves between cells.") { When = "With cell navigation" },
                new(["Home", "End"], "Goes to the ends of the row.") { When = "With cell navigation" },
                new(["Ctrl+Home", "Ctrl+End"], "Goes to the first or last cell.") { When = "With cell navigation" },
                new(["Page Up", "Page Down"], "Moves by a screenful, scrolling a virtualized grid to rows not yet "
                    + "drawn.") { When = "With cell navigation" },
                new(["Space"], "Selects or unselects the row of the cell in focus.") { When = "With cell navigation" },
                new(["Enter", "F2"], "Moves into the cell's fields, or starts editing an `Editable` cell.")
                    { When = "With cell navigation" },
                new(["A character"], "Starts editing an `Editable` cell, with what's typed replacing a text or "
                    + "number field's value; a select or checkbox just opens.") { When = "With cell navigation" },
                new(["Enter"], "On a cell with nothing to go into, clicks the row, raising `OnRowClick` and, with "
                    + "`RowClickSelects`, selecting or unselecting it.") { When = "With cell navigation" },
                new(["Enter", "Space"], "Opens or closes the row's details.") { When = "On a row's details button" },
                new(["Enter"], "On a row's details, moves into them.") { When = "With cell navigation and details" },
                new(["Escape"], "From anywhere in a row's details, even a grid inside them, comes back to the "
                    + "details.") { When = "With cell navigation and details" },
                new(["Tab"], "Cycles through the cell's fields.") { When = "Inside a cell" },
                new(["Escape"], "Comes back out to the cell.") { When = "Inside a cell" },
                new(["Enter"], "Saves the edit, and focus goes back to the cell.") { When = "Editing a cell" },
                new(["Escape"], "Cancels the edit, and focus goes back to the cell.") { When = "Editing a cell" },
                new(["Tab", "Shift+Tab"], "Saves the edit and opens the next or previous editable cell. Tab in the "
                    + "last one leaves the grid, and Shift+Tab in the first goes back to its cell.") { When = "Editing a cell" },
                new(["Tab"], "Moves between the row's fields and its Save and Cancel buttons.")
                    { When = "Editing a row" },
                new(["Enter"], "Saves the row.") { When = "Editing a row" },
                new(["Escape"], "Cancels the row's edit.") { When = "Editing a row" },
                new(["Enter"], "In the value field, applies the filter.") { When = "In a filter menu" },
                new(["Escape"], "Closes the menu.") { When = "In a filter menu" },
                new(["Up", "Down"], "Moves the row up or down, and focus stays on the handle.")
                    { When = "On a row's move handle" },
                new(["Home", "End"], "Moves the row to the start or end.") { When = "On a row's move handle" },
                new(["Enter", "Space"], "Opens its menu of moves.") { When = "On a row's move handle" },
                new(["Escape"], "Cancels a drag.") { When = "On a row's move handle" },
            ],
            Labeling =
            [
                "It is a native `table` named by `AriaLabel`, with a header cell for each column, so screen readers "
                    + "read each cell with its column.",
                "When the rows overflow their box and there is no cell navigation, the box is a tab stop, so keys can "
                    + "scroll it. With an `AriaLabel`, it's also a `region` with that name.",
                "A row's checkbox is named \"Select\" followed by the row's label, which is its `RowLabel` or else the "
                    + "text of the first column that has any.",
                "Each filter is a labeled field named \"Filter\" followed by the column's title. In `Menu` mode, its "
                    + "icon button has that name, with \", on\" added while the column is filtered, and the menu "
                    + "holds a labeled \"Condition\" select and \"Value\" field, a \"Value\" date range, or a checkbox "
                    + "group of values.",
                "A group's button is named by the column, the group's value and its row count, such as \"Region: "
                    + "North ridge Rows: 2\". The \"Group by\" list is a `TbMultiSelect`.",
                "An edit's field is named by its column, and a row's Save and Cancel buttons by the row, such as "
                    + "\"Save Cedar Flats\".",
                "A row's move handle is a menu `button` named \"Move\" followed by the row's label.",
                "The totals row starts with hidden text that screen readers read first: \"Totals\", or for a group's "
                    + "totals row \"Totals for\" and the group's heading, such as \"Totals for Region: North ridge\".",
            ],
            Announcements =
            [
                "Every change of sort is read out, such as \"Sorted by Name, ascending.\"",
                "After a filter changes, the count of rows shown is read out, such as \"Rows shown: 2.\"",
                "A failed load and a `DataProvider` load with no rows are read out. With `EmptyContent`, set "
                    + "`Labels.Empty` to what should be read out.",
                "An edit's error shows under its field and is read out.",
                "Each move of a row is read out with its new position, and so is the reason `CanMoveRow` refuses one.",
            ],
            Accessibility =
            [
                "A row drag held near the top or bottom of the scroll box keeps it scrolling, so a drop can reach any row.",
                "The first sort sets `aria-sort` on its header. With several sorts, each sorted header's button also "
                    + "says its place, such as \"sort 2 of 3, ascending\".",
                "The header checkbox shows \"mixed\" when only some rows are selected.",
                "A group's button reports its state with `aria-expanded`.",
                "A `TbColumnGroup`'s heading is a column header that spans its columns (`colspan`), so screen "
                    + "readers read it with the cells under it.",
                "A row's details button is named for the row, such as \"Details for Cedar Flats\", and reports its "
                    + "state with `aria-expanded`; while the details are open, its `aria-controls` points to the "
                    + "detail row.",
                "The totals row is the table's footer, so each total is read with its column's header. Each cell "
                    + "names its total before the value, such as \"Sum 49.9\". With cell navigation, the arrow keys "
                    + "reach the row like any other.",
                "A pinned column moves to its side of the table in the page's order too, so screen readers and Tab "
                    + "reach its cells in the order they show.",
                "With `Virtualize`, the table's `aria-rowcount` holds the number of rows, headers included, and "
                    + "each drawn row's `aria-rowindex` its place, so screen readers can say where a row is among "
                    + "rows that aren't drawn.",
                "While rows load the table is `aria-busy`.",
                "With `CellNavigation`, or an `Editable` column, the table takes the ARIA grid role, and rows say "
                    + "whether they are selected with `aria-selected`.",
                "With cell navigation, when the row holding focus is removed, focus moves to the row that takes its "
                    + "place, or to the new last row if it was the last. With `Virtualize`, removing the last row leaves "
                    + "focus on the page, and so does scrolling the focused row out of view.",
                "A cell holding just a menu's button or a row's move handle keeps focus until Enter, since those "
                    + "controls use the arrow keys.",
                "A resizable, movable or hideable column has a menu button, named \"Options for\" and the column's "
                    + "title, whose items resize it, move it earlier or later, or hide it with the keys or a single "
                    + "click, so dragging is never the only way. The edge itself is hidden from screen readers.",
                "The Columns menu lists every column as a checkbox item, in the grid's column order, and stays open "
                    + "while the user turns several on or off. The item for the last column shown, or for a column "
                    + "with `Hideable=\"false\"`, is disabled.",
                "After a column is hidden from its own menu, whose button goes with it, focus moves to the Columns "
                    + "menu's button, and the change is read out, such as \"Region hidden\".",
                "After a move from its menu, focus stays on the column's menu button in its new place, and the move "
                    + "is read out, such as \"Region moved to column 3 of 5\".",
            ],
            AppDuties =
            [
                "Without cell navigation, only a pointer clicks a row, so whatever `OnRowClick` does needs a button in "
                    + "a cell too, as [Clicking a row](components/data-grid#row-click) shows.",
                "Set `RowLabel` when a row's first column doesn't say which row it is, so its checkbox and move handle "
                    + "can.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "Overflow can't be measured yet, so the box the rows scroll in is a tab stop, and with an `AriaLabel` a "
                    + "`region`, whenever the grid has a `Height` and no cell navigation. Without a `Height`, the box "
                    + "isn't a tab stop, so on a static page a grid too wide for its box needs a `Height` or a control "
                    + "in its rows for keys to scroll it.",
                "With `Items` and no `Virtualize`, it renders its first page of rows as a plain table, so they can be "
                    + "read at once. Sorting, filtering, paging and selecting start working once the page is "
                    + "interactive. With a `StateHref`, sorting and paging work before that, as links; the page size "
                    + "picker still waits.",
            ],
            Notes =
            [
                new("A row draws again only when its item, its selection, its `RowClass`, its `RowLabel` or the "
                    + "columns change. When a `Cell` template shows something else, call `RenderRows` after it "
                    + "changes. After changing `Items` in place, or after the page changes a filter of its own, call "
                    + "`RefreshAsync`.") { Pitfall = true },
                new("Each move handle is a menu with a script of its own, so set a `PageSize` on a grid with many rows "
                    + "that can move."),
                new("A row's `data-key` is its key's text for a string, number or date key, and a made-up id such as "
                    + "`key-3` for any other key, when the text is empty, or when another key has it."),
            ],
            Ready = new("[data-part=grid]", "the grid"),
            Target = new("table", "Trails"),
            Blocks = ["tb-data-grid"],
            DataParts =
            [
                new("grid", "The grid itself."),
                new("scroll", "The box that scrolls the rows."),
                new("sort", "A column header's sort button, or its link with a `StateHref`."),
                new("sort-order", "The number that shows a column's place when sorting by several."),
                new("filters", "The row of filter fields under the headers."),
                new("filter", "One column's cell in the row of filter fields."),
                new("filter-menu", "A column's filter menu: its button in the header and the popup of conditions."),
                new("column-menu", "A column's menu, in its header, that resizes, moves or hides it."),
                new("resize", "The edge of a resizable column's header, which a drag moves."),
                new("filter-apply", "The filter menu's Apply button."),
                new("filter-clear", "The filter menu's Clear button."),
                new("toolbar", "The bar above the rows that holds the group-by list and the Columns menu."),
                new("group-by", "The list that chooses the columns to group by."),
                new("column-chooser", "The Columns menu, which hides and shows columns."),
                new("export", "The Export menu, which downloads the rows as a file."),
                new("group", "A group's heading row."),
                new("group-toggle", "The button that opens and closes a group."),
                new("select-all", "The checkbox that selects every row on the page."),
                new("totals", "The totals row, under the rows."),
                new("group-totals", "A group's totals row, under its rows."),
                new("bands", "A row of group headings above the column headers."),
                new("band", "A group's heading, across its columns."),
                new("detail-toggle", "A row's button that opens and closes its details."),
                new("detail", "The row of details under a row whose details are open."),
                new("detail-cell", "The one cell of a detail row, which holds the `DetailTemplate`."),
                new("total-label", "The name of a total, such as \"Sum\", before its value."),
                new("totals-name",
                    "A totals row's name for screen readers, hidden at the start of the first column's cell."),
                new("row", "One row."),
                new("placeholder", "A row a large grid hasn't loaded yet."),
                new("editor", "The field that edits a cell."),
                new("row-actions", "The Save and Cancel buttons of a row that's being edited."),
                new("row-error", "The message that says why a row could not be saved."),
                new("select", "The cell that holds a row's checkbox or radio, and its header cell."),
                new("order", "The header cell of the column that holds the rows' drag handles."),
                new("handle", "A row's drag handle."),
                new("drop-reason", "The reason shown while a dragged row is held over a place it may not go."),
                new("loading", "The message shown while rows load."),
                new("empty", "The message shown when there are no rows."),
                new("failed", "The message and Retry button shown when rows fail to load."),
                new("summary", "The text that says which rows the page shows, such as \"1–10 of 42\"."),
                new("pager", "The pagination under the grid."),
            ],
            Anatomy = """
                <TbDataGrid Items="_items" AriaLabel="…" KeySelector="item => item.Id">
                    <TbColumn Field="item => item.Name" Sortable Filterable />
                    <TbColumn Title="…">
                        <Cell>…</Cell>
                    </TbColumn>
                </TbDataGrid>
                """,
            TestingTips =
            [
                "In Playwright, `page.Tb().Grid(\"Trails\")` from `Trailblazor.Testing.Playwright` finds a row with "
                    + "`Row(\"Cedar Flats\")` and a cell with `await CellAsync(\"Cedar Flats\", \"Region\")`.",
                "`SortAsync(\"Name\", descending: true)`, `FilterAsync(\"Name\", \"ridge\")`, `SelectAsync(...)` and "
                    + "`GoToPageAsync(2)` drive it, and sorting, filtering and paging wait for a `DataProvider`'s rows.",
                "With a `StateHref`, the sort headers and the pager are links. In an interactive grid, `SortAsync` and "
                    + "`GoToPageAsync` still work; on a static page, click the header's link, and `GoToPageAsync` "
                    + "follows the pager's. To start a test at a view, open the page at an address with "
                    + "`GridState.ToQueryParameters` in its query.",
                "In a filter menu, `FilterAsync(\"Sites\", \"At least\", \"16\")` picks a condition too, and "
                    + "`FilterChoicesAsync(\"Region\", \"North ridge\")` leaves exactly those values picked, in a "
                    + "menu's checkboxes or the filter row's multiple select.",
                "In the filter row, a choice filter is a select named for its column: "
                    + "`page.Tb().Select(\"Filter Difficulty\").ChooseAsync(\"Easy\")`.",
                "`page.Tb().Select(\"Group by\").ChooseAsync(\"Region\")` groups the rows. Each group's heading is a "
                    + "`[data-part=group]` row whose `[data-part=group-toggle]` button opens or closes it.",
                "A row's details button is named for the row: `GetByRole(AriaRole.Button, new() { Name = \"Details "
                    + "for Cedar Flats\" })`. Its `aria-controls` names the `[data-part=detail]` row it opens.",
                "Without the helpers, wait for the table's `aria-busy` to clear before clicking a row.",
                "Rows carry `data-key` and `data-selected`, a filtered column's header `data-filtered`, and cells "
                    + "`data-column`, the field's path such as "
                    + "`Shipping.City` unless the column has an `Id`. A pinned column's header and cells also carry "
                    + "`data-pinned`, `start` or `end`, and a filter cell's `data-value` holds its applied values as "
                    + "invariant text, with a range's two dates separated by a space.",
                "In bUnit, `cut.Tb().Grid(\"Trails\")` from `Trailblazor.Testing` has the same `Row`, `SelectAsync` and "
                    + "`FilterAsync`.",
                "In bUnit, `await cut.Tb().Menu(\"Export\").ChooseAsync(\"CSV\")` downloads the file into "
                    + "`FakeBrowserService.Downloads`, where `Text` reads a CSV back, starting with its byte order "
                    + "mark. In Playwright, wrap the choice in "
                    + "`page.RunAndWaitForDownloadAsync`. `ExportCsvAsync` returns the text itself.",
                "`Total(\"Length (km)\")` reads the totals row's cell under a column, such as \"Sum 49.9\", or null "
                    + "when it shows no total; in Playwright, use `await TotalAsync(\"Length (km)\")`. "
                    + "`GroupTotals(\"Length (km)\")` reads each open group's totals cell under that column, in "
                    + "order; in Playwright, use `await GroupTotalsAsync(\"Length (km)\")`. Both leave out the row's "
                    + "hidden name.",
                "`ResizeColumnAsync(\"Region\", 200)` resizes the Region column to 200 pixels: in Playwright by "
                    + "dragging its edge, waiting for the grid to keep the width, and in bUnit as the grid's script "
                    + "reports it. A column's header carries the width the grid keeps in `data-width`.",
                "A column moves from its menu: `page.Tb().Menu(\"Options for Region\").ChooseAsync(\"Move Region "
                    + "later\")`, and the same in bUnit with `cut.Tb()`. In bUnit, `Columns` reads the column titles "
                    + "in the order they show; in Playwright, use `await ColumnsAsync()`.",
                "Hide or show a column from the Columns menu: `cut.Tb().Menu(\"Columns\").ChooseAsync(\"Region\")`, "
                    + "and the same in Playwright with `page.Tb()`.",
                "In bUnit, `ClickRowAsync(\"Cedar Flats\")` clicks a row as the browser reports it, raising "
                    + "`OnRowClick` and, with `RowClickSelects`, selecting it. A plain `Click()` on the row doesn't.",
                "Double-click a cell or press Enter on it to edit it. Its field is named for its column, so in bUnit "
                    + "`cut.Tb().NumberField(\"Length (km)\")` finds it, and Enter saves.",
                "A virtualized grid draws only the rows in view, so scroll its `[data-part=scroll]` box, or call "
                    + "`ScrollToAsync`, before looking for a row further down.",
            ],
            Telemetry =
            [
                "With a `TelemetryName`, such as `\"jobs\"`, it counts `trailblazor.control.shown` as it first "
                    + "renders, and `trailblazor.control.used` with `trailblazor.action` `sort`, `filter`, `page`, "
                    + "`page_size`, `select`, `move`, `group`, `expand`, `collapse`, `edit`, `resize`, `reorder`, "
                    + "`hide`, `show`, `open_detail`, `close_detail`, `export_csv` or `export_excel` for each of "
                    + "those the user does; a `move` also "
                    + "says whether a `pointer` or the "
                    + "`keyboard` made it.",
                "A page `ScrollToAsync` moves to, or a group it opens, isn't counted, and which column, filter "
                    + "value, page, group, rows or width were used, or what value an edit saved, is never recorded.",
            ],
        },
        new("table", "Table", ExampleCategory.Content,
            "A plain table written as markup, with the data grid's look.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for rows of facts that people only read, written as markup.",
            ],
            WhenNotToUse =
            [
                "For rows people sort, filter, page, select or edit, use a [data grid](components/data-grid).",
                "For items that each do something, or that don't share columns, use a [list](components/list).",
            ],
            Related = ["data-grid", "list"],
            Pattern = new("table", "https://www.w3.org/WAI/ARIA/apg/patterns/table/"),
            Sections =
            [
                new("basic", "Rows and headers", "Write the table's own markup inside it. `Striped` shades every other "
                    + "row.", typeof(TableExample)),
                new("sticky-header", "Sticky header and size", "`StickyHeader` keeps the header row in view as the rows "
                    + "scroll, inside a parent with a set height and `overflow: auto`, or as the page scrolls. "
                    + "`--tb-table-sticky-top` moves it below an app bar fixed to the top. `Size` sets how tall the "
                    + "rows are.", typeof(TableStickyExample)),
                new("empty", "Empty", "A table has no message of its own, so write one in a row with a `colspan` across "
                    + "the columns.", typeof(TableEmptyExample)),
                new("long-content", "Long content", "Long text in a data cell wraps, and the row grows to fit. Header "
                    + "cells stay on one line, so keep them short.", typeof(TableLongExample)),
                new("wide-tables", "Wide tables and phones", "A table with many columns doesn't fit a phone. Give it a "
                    + "`min-inline-size` and put it in an area with `overflow: auto`, so it scrolls sideways and the "
                    + "page doesn't.", typeof(TableWideExample)),
            ],
            Api = [typeof(TbTable)],
            Labeling =
            [
                "It is a native `table`, named by a `<caption>` or `aria-label`.",
            ],
            Accessibility =
            [
                "Screen readers read each cell with its headers, from the `<th>`s and their `scope`.",
                "Under a sticky header, a control in a row scrolls clear of the header when it takes focus. A "
                    + "header taller than one row needs a larger `scroll-margin-block-start` on those controls.",
            ],
            AppDuties =
            [
                "Name the table, and give each header a `scope`.",
                "If the table scrolls in an area of its own, give that area `tabindex=\"0\"`, `role=\"region\"` and a "
                    + "name, so keyboard users can reach and scroll it.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Full),
            BeforeInteractive =
            [
                PlainHtml,
            ],
            Target = new("table", "Opening hours"),
            Blocks = ["tb-table"],
            TestingTip = "Find it by role and name, `GetByRole(AriaRole.Table, new() { Name = \"Opening hours\" })`, "
                + "and its cells with `GetByRole(AriaRole.Cell, ...)`.",
        },
        new("breadcrumbs", "Breadcrumbs", ExampleCategory.Navigation,
            "A trail of links from the top of a site down to the current page, so people can see where they are and "
                + "go back up.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it on pages nested a few levels deep, such as a project's settings, so people can go back up to "
                    + "any level above.",
            ],
            WhenNotToUse =
            [
                "For moving between pages at the same level, use [tabs](components/tabs) or "
                    + "[pagination](components/pagination).",
                "For the steps of a task done in order, use a stepper.",
            ],
            Related = ["link", "tabs", "pagination"],
            Pattern = new("breadcrumb", "https://www.w3.org/WAI/ARIA/apg/patterns/breadcrumb/"),
            Sections =
            [
                new("basic", "Trail", "Each `TbBreadcrumb` with an `Href` is a link. `Current` marks the page people "
                    + "are on, which screen readers hear as the current page; set it on the last item when the trail "
                    + "ends there.", typeof(BreadcrumbsExample)),
                new("plain-text", "Plain text and labels", "An item without an `Href` is plain text, for a section "
                    + "with no page of its own. The current item can also have an `Href`, and stays a link. "
                    + "`AriaLabel` names the trail, \"Breadcrumb\" unless set; when a page has more than one trail, give "
                    + "each its own name.",
                    typeof(BreadcrumbsTextExample)),
                new("separator", "Separator and icons", "`Separator` changes what goes between the items, a chevron "
                    + "unless set; screen readers skip it. An item can hold an icon beside its text.",
                    typeof(BreadcrumbsSeparatorExample)),
            ],
            Api = [typeof(TbBreadcrumbs), typeof(TbBreadcrumb)],
            Anatomy = """
                <TbBreadcrumbs AriaLabel="…">
                    <Separator>…</Separator>
                    <ChildContent>
                        <TbBreadcrumb Href="…">…</TbBreadcrumb>
                        <TbBreadcrumb Current>…</TbBreadcrumb>
                    </ChildContent>
                </TbBreadcrumbs>
                """,
            Keyboard =
            [
                new(["Tab"], "Moves between the links, each its own tab stop."),
                new(["Enter"], "Follows the link."),
            ],
            Labeling =
            [
                "A `navigation` landmark named by `AriaLabel` (\"Breadcrumb\" by default), holding a list with one "
                    + "item for each step.",
                "Each link is named by its text.",
            ],
            Announcements =
            [
                "The current item has `aria-current=\"page\"`, so screen readers hear \"current page\". The separators "
                    + "are hidden from screen readers.",
            ],
            Accessibility =
            [
                "In right-to-left text the trail runs from right to left, and the chevrons point left.",
                "A trail too long for its line wraps onto the next, so every step stays visible.",
            ],
            AppDuties =
            [
                "Mark the page people are on with `Current`, on the last item when the trail ends at it.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Full),
            BeforeInteractive =
            [
                "It renders as plain HTML, and its links already work.",
            ],
            Target = new("navigation", "Breadcrumb"),
            Blocks = ["tb-breadcrumbs"],
            DataParts =
            [
                new("breadcrumbs", "The trail's navigation landmark."),
                new("breadcrumb", "One item in the trail."),
                new("link", "An item's link."),
                new("text", "An item without an address, shown as plain text."),
                new("separator", "What goes before each item; the stylesheet hides the first item's."),
            ],
            TestingTip = "Find the trail by its landmark, "
                + "`GetByRole(AriaRole.Navigation, new() { Name = \"Breadcrumb\" })`, and the page it marks as current "
                + "with `Locator(\"[aria-current=page]\")`.",
            Telemetry =
            [
                "With a `TelemetryName`, such as `\"project-breadcrumbs\"`, it counts `trailblazor.control.shown` as "
                    + "it first renders and `trailblazor.control.used` for each link followed. Which link was followed "
                    + "is never recorded.",
            ],
        },
        new("stepper", "Stepper", ExampleCategory.Navigation,
            "The steps of a task done in order, such as a sign-up or a checkout: which step people are on, which are "
                + "done, and a way back to a finished one.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for a task of three to six steps that people do once, in order, such as booking or setting up "
                    + "an account.",
            ],
            WhenNotToUse =
            [
                "For views people move between freely, use [tabs](components/tabs).",
                "For where a page sits in the site, use [breadcrumbs](components/breadcrumbs).",
                "For a task's progress with no steps to go back to, use [progress](components/progress).",
            ],
            Related = ["tabs", "breadcrumbs", "progress"],
            Sections =
            [
                new("basic", "Steps", "`@bind-ActiveKey` holds the `Key` of the step people are on, which defaults to "
                    + "the step's `Title`. In a horizontal stepper, the app shows the current step's content under it, "
                    + "and the app's Back and Next buttons change `ActiveKey`. `Completed` puts a check in place of a "
                    + "step's number, and people can go back to a completed step.", typeof(StepperExample)),
                new("vertical", "Vertical", "`Vertical` lays the steps out in a column, with the current step's "
                    + "content under it.", typeof(StepperVerticalExample)),
                new("states", "States and order", "`Error` shows a message under the title and a warning in place "
                    + "of the number or check, `Optional` marks a step people can skip, `Description` adds a line "
                    + "under the title, and `Disabled` keeps people from going to a step. With `Linear=\"false\"`, "
                    + "people can go to any step, done or not.",
                    typeof(StepperStatesExample)),
            ],
            Api = [typeof(TbStepper), typeof(TbStep)],
            Anatomy = """
                <TbStepper @bind-ActiveKey="_step">
                    <TbStep Title="…" Completed="…" />
                    <TbStep Title="…" Optional>…</TbStep>
                </TbStepper>
                """,
            Keyboard =
            [
                new(["Tab"], "Moves between the steps people can go to, each its own tab stop."),
                new(["Enter", "Space"], "Goes to the focused step."),
            ],
            Labeling =
            [
                "A list named by `AriaLabel` (\"Progress\" by default). Screen readers hear each step's place in it, "
                    + "as in \"2 of 4\".",
                "Each step's button is named by what it shows: its title, its \"Optional\" note, description and "
                    + "error, then \"Completed\" for a finished step.",
            ],
            Announcements =
            [
                "The current step has `aria-current=\"step\"`. When the step changes after the stepper first shows, "
                    + "screen readers hear \"Current step: Payment\".",
            ],
            Accessibility =
            [
                "A step people can't go to isn't a button, so Tab skips it.",
                "The current step stays a button, so focus stays on a step people just chose.",
                "In forced-colors mode, such as Windows high contrast, the current step's number takes the system's "
                    + "highlight.",
            ],
            AppDuties =
            [
                "Show the current step's content and move on with your own Back and Next buttons, marking each step "
                    + "`Completed` as people finish it.",
                "Give a step's content a heading, so people can find it after moving on.",
                "A horizontal stepper takes its container's width, so in a row give it `flex: 1` or a width.",
            ],
            Modes = new(ModeSupport.DisplayOnly, ModeSupport.DisplayOnly),
            BeforeInteractive =
            [
                "It renders as plain HTML. Its step buttons start working once the page is interactive.",
            ],
            Target = new("list", "Trip booking"),
            Blocks = ["tb-stepper"],
            DataParts =
            [
                new("stepper", "The list of steps."),
                new("step", "One step."),
                new("step-button", "The button that goes to a step."),
                new("step-header", "A step people can't go to."),
                new("indicator", "A step's number, or its check or warning."),
                new("title", "A step's title."),
                new("description", "The line under a step's title."),
                new("optional", "The note that a step can be skipped."),
                new("error", "A step's error message."),
                new("content", "The current step's content, in a vertical stepper."),
            ],
            TestingTips =
            [
                "Find the current step with `Locator(\"[aria-current=step]\")`, and a step by its title with "
                    + "`GetByRole(AriaRole.Button, new() { Name = \"Gear\" })`; a finished step's name ends in "
                    + "\"Completed\", as in \"Trip Completed\".",
                "In bUnit, set `ActiveKey` and check `[aria-current=step]`.",
            ],
        },
        new("pagination", "Pagination", ExampleCategory.Navigation,
            "Moves between the pages of a long list, with numbered page buttons or, in compact mode, a box for the "
                + "page number.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it to move through a long list one page at a time.",
            ],
            WhenNotToUse =
            [
                "For rows in a table, use the [data grid](components/data-grid)'s own paging.",
                "For sections of one page, use a [table of contents](components/table-of-contents).",
            ],
            Related = ["data-grid", "tabs", "table-of-contents"],
            Sections =
            [
                new("pages", "Pages", "`@bind-Page` is a pagination's current page, and `PageCount` is how many pages "
                    + "there are. The Previous and Next buttons show unless `ShowPreviousNext` is false.",
                    typeof(PaginationExample)),
                new("long-lists", "First, last and neighbors", "`ShowFirstLast` adds First and Last buttons to a "
                    + "pagination. `SiblingCount` sets how many pages show either side of the current "
                    + "one.", typeof(PaginationLongExample)),
                new("size", "Size", "`Size` sets the size of a pagination's buttons.", typeof(PaginationSizeExample)),
                new("boundary", "Pages at the ends", "`BoundaryCount` sets how many pages a pagination always shows at "
                    + "each end, 1 unless set.", typeof(PaginationBoundaryExample)),
                new("compact", "Compact", "`Compact` replaces a pagination's page buttons with a box for the page "
                    + "number. Typing a page and pressing Enter, or leaving the box, goes there.",
                    typeof(PaginationCompactExample)),
                new("links", "Links", "With `PageHref`, every item of a pagination is a link, so they work before the "
                    + "page is interactive and can open in a new tab while this one stays on its page.",
                    typeof(PaginationLinksExample)),
                new("disabled", "Disabled", "`Disabled` turns off every button of a pagination.",
                    typeof(PaginationDisabledExample)),
                new("labels", "Other languages", "All of a pagination's words come from `Labels`, such as `Previous`, "
                    + "`Next` and `Page`, whose `{0}` stands for the number.", typeof(PaginationLabelsExample)),
            ],
            Api = [typeof(TbPagination), typeof(PaginationLabels)],
            Keyboard =
            [
                new(["Tab"], "Moves between the controls, each its own tab stop, as in a list of links."),
                new(["Enter", "Space"], "Presses the focused button.") { When = "As buttons" },
                new(["Enter"], "Follows the link.") { When = "With `PageHref`" },
                new(["Enter"], "Goes to the page typed in the box, without submitting a form around it.")
                    { When = "In `Compact`" },
                new(["Up", "Down"], "Goes to the next or previous page.") { When = "In `Compact`" },
            ],
            Labeling =
            [
                "A `navigation` landmark named by `AriaLabel` (\"Pagination\" by default); name each one when a page "
                    + "has several.",
                "Each page is a button named \"Page 3\". The Previous, Next, First and Last buttons are named, and in "
                    + "compact mode the page box is named \"Page\".",
            ],
            Announcements =
            [
                "The current page has `aria-current=\"page\"`. The ellipses are hidden from screen readers.",
            ],
            Accessibility =
            [
                "The Previous, Next, First and Last buttons turn off at either end but stay focusable, so focus stays "
                    + "on Next after it reaches the last page. Focus stays on a chosen page as the pages around it "
                    + "shift.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "With `PageHref`, every item is already a link. Without it, the buttons do nothing until the page is "
                    + "interactive.",
            ],
            Ready = new(".tb-pagination", "the navigation"),
            Target = new("navigation", "Trail reports"),
            Blocks = ["tb-pagination"],
            DataParts =
            [
                new("first", "The button that goes to the first page."),
                new("previous", "The button that goes to the page before."),
                new("page", "A page button."),
                new("ellipsis", "The gap that stands in for pages not shown."),
                new("page-box", "The box for typing a page number to go to."),
                new("page-count", "The text after the box that says how many pages there are."),
                new("next", "The button that goes to the page after."),
                new("last", "The button that goes to the last page."),
            ],
            TestingTip = "To go to a page, `await page.Tb().Pagination(\"Photos\").GoToPageAsync(42)` from "
                + "`Trailblazor.Testing.Playwright` clicks it, types it into a compact pagination's box, or steps "
                + "with Next and Previous, then waits for it to be current.",
        },
        new("table-of-contents", "Table of contents", ExampleCategory.Navigation,
            "Links to the sections of a page that mark the section in view as the page scrolls.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it beside a long page, so people can see where they are and jump to a section.",
            ],
            WhenNotToUse =
            [
                "For moving between pages of a list, use [pagination](components/pagination).",
                "For switching what shows, use [tabs](components/tabs).",
            ],
            Related = ["link", "tabs", "accordion"],
            Sections =
            [
                new("sections", "Sections and nesting", "Each `TbTableOfContentsItem` links to the element with its "
                    + "`TargetId`, and an item inside another makes a nested list. The link to the section in view is "
                    + "marked as the page, or a scrolling panel, scrolls.", typeof(TableOfContentsExample)),
                new("active", "Knowing the section in view", "`ActiveChanged` gives you a section's id when a table "
                    + "of contents marks it as the section in view, and null when none "
                    + "is.", typeof(TableOfContentsActiveExample)),
            ],
            Api = [typeof(TbTableOfContents), typeof(TbTableOfContentsItem)],
            Anatomy = """
                <TbTableOfContents AriaLabel="…">
                    <TbTableOfContentsItem TargetId="…" Text="…">
                        <TbTableOfContentsItem TargetId="…" Text="…" />
                    </TbTableOfContentsItem>
                </TbTableOfContents>
                """,
            Keyboard =
            [
                new(["Tab"], "Moves between the links, each its own tab stop."),
                new(["Enter"], "Follows the link, and moves focus to the section, so Tab goes on from there."),
            ],
            Labeling =
            [
                "A `navigation` landmark named by `AriaLabel` (\"On this page\" by default) holding a list of links, "
                    + "with a nested list under an item for the sections inside it.",
            ],
            Announcements =
            [
                "The link to the section in view has `aria-current=\"location\"`, and shows it with a wider line "
                    + "beside it, not color alone.",
            ],
            Accessibility =
            [
                "A section is in view once its start passes the top quarter of what scrolls it, the page or a "
                    + "scrolling panel. At the top of the scroll the first section is in view, and at the bottom the "
                    + "last section that shows is in view. A followed link stays marked until the page or panel "
                    + "holding its section scrolls again, even where position alone would put another section in view.",
                "Following a link scrolls smoothly to the section, or jumps there when the user has asked for reduced "
                    + "motion.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "The links are plain addresses with the section's id, so they already work, and open in a new tab with "
                    + "Ctrl or a middle click. Marking the section in view starts once the page is interactive.",
            ],
            Ready = new(".tb-table-of-contents", "the navigation"),
            Target = new("navigation", "Trail guide"),
            Blocks = ["tb-table-of-contents"],
            DataParts =
            [
                new("link", "A link to one section."),
            ],
            TestingTip = "Scroll a section into view and check its link with `ToHaveAttributeAsync(\"aria-current\", "
                + "\"location\")`.",
            Telemetry =
            [
                "With a `TelemetryName`, such as `\"guide-contents\"`, it counts `trailblazor.control.shown` as it "
                    + "first renders and `trailblazor.control.used` for each link followed. Which link was followed "
                    + "is never recorded.",
            ],
        },
        new("link", "Link", ExampleCategory.Navigation,
            "An underlined text link to an address, or a link-styled button that runs an action.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it to go to another page, in a sentence or a list.",
            ],
            WhenNotToUse =
            [
                "For an action that stands on its own, use a [button](components/button).",
            ],
            Related = ["button", "pagination", "table-of-contents"],
            Sections =
            [
                new("basic", "Links", "`Href` is where the link goes. A link is always underlined, so it doesn't rely "
                    + "on color alone to stand out from the text around it.", typeof(LinkExample)),
                new("new-tab", "New tab", "`NewTab` opens the link in a new tab, with `target=\"_blank\"` and "
                    + "`rel=\"noopener noreferrer\"`, and screen readers hear \"opens in a new tab\" after its text. "
                    + "`NewTabHint` changes those words.", typeof(LinkNewTabExample)),
                new("button", "Without an address", "With `OnClick` and no `Href`, `TbLink` is a button that looks "
                    + "like a link, for an action in the middle of a sentence.", typeof(LinkButtonExample)),
                new("moving-focus", "Moving focus", "`FocusAsync` moves focus to the link from code, such as after an "
                    + "action finishes.", typeof(LinkFocusExample)),
                new("disabled", "Disabled", "`Disabled` keeps the text but drops the address, so the link can't be "
                    + "followed or focused. It loses its underline and turns gray.", typeof(LinkDisabledExample)),
            ],
            Api = [typeof(TbLink)],
            Keyboard =
            [
                new(["Enter"], "Follows the link.") { When = "With an `Href`" },
                new(["Enter", "Space"], "Presses it.") { When = "Without an `Href`" },
            ],
            Labeling =
            [
                "Named by its text. With `Href` it is a native `link`. Without `Href` it is a `button`, because an "
                    + "anchor with no address can't be focused.",
            ],
            Announcements =
            [
                "A disabled link is marked `aria-disabled` with the `link` role, so screen readers still read it as a "
                    + "disabled link. Without `Href` the button is `disabled`.",
            ],
            Accessibility =
            [
                "In forced-colors mode, such as Windows high contrast, a link takes the system's link color with or "
                    + "without an `Href`, and a disabled one the system's gray.",
            ],
            Modes = new(ModeSupport.Partial, ModeSupport.Partial),
            BeforeInteractive =
            [
                "A link with an `Href` already works. `OnClick` starts working once the page is interactive.",
            ],
            Target = new("link", "theming guide"),
            Blocks = ["tb-link"],
            TestingTip = "A link with an `Href` has the `link` role and one without has the `button` role, so find it "
                + "with the role that matches. `ToBeDisabledAsync` checks either kind is disabled.",
            Telemetry =
            [
                "With a `TelemetryName`, such as `\"trail-guide\"`, it counts `trailblazor.control.shown` as it first "
                    + "renders and `trailblazor.control.used` for each click it acts on, as a link or as a button.",
            ],
        },
        new("empty-state", "Empty state", ExampleCategory.Feedback,
            "A message that takes the place of content that isn't there yet, such as an empty list, with a way to add "
                + "it.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it where a list, table or page has nothing to show, to say why and what to do next.",
            ],
            WhenNotToUse =
            [
                "While content loads, use a [skeleton](components/skeleton).",
                "For an error, such as a failed load, use an [alert](components/alert).",
            ],
            Related = ["skeleton", "alert", "card"],
            Sections =
            [
                new("basic", "Empty state", "`Title` says what's missing and `Description` adds a sentence under it, "
                    + "or `ChildContent` takes its place. `Icon` shows a picture above, and `Actions` holds the "
                    + "buttons or links that add what's missing.", typeof(EmptyStateExample)),
                new("compact", "Compact", "`Compact` takes less space, for an empty list, table or panel rather than "
                    + "a whole page. `HeadingLevel` sets the title's heading level to fit the headings around it; here "
                    + "it's 4, under the panel's level 3 title. When a search empties the list, the example says so "
                    + "with the announcer, and its action puts focus back in the search.",
                    typeof(EmptyStateCompactExample)),
            ],
            Api = [typeof(TbEmptyState)],
            Accessibility =
            [
                "The title is a heading, at level 3 unless `HeadingLevel` says otherwise, so screen readers can find "
                    + "it. The icon is decoration and hidden from them.",
                "It handles no keys of its own; its actions are ordinary buttons and links.",
            ],
            AppDuties =
            [
                "When an empty state replaces content after the user acts, such as a search that finds nothing, tell "
                    + "screen readers too, with the [announcer](guides/announcements) or a status message.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Full),
            BeforeInteractive =
            [
                PlainHtml,
            ],
            Target = new("heading", "No trails saved yet"),
            Blocks = ["tb-empty-state"],
            DataParts =
            [
                new("icon", "The picture above the title."),
                new("title", "The heading that says what's missing."),
                new("description", "The sentence or content under the title."),
                new("actions", "The buttons or links that add what's missing."),
            ],
            TestingTip = "Find it by its title, as `GetByRole(AriaRole.Heading, new() { Name = \"No trails saved "
                + "yet\" })`, and its actions by their own roles and names.",
            Telemetry =
            [
                "With a `TelemetryName`, such as `\"no-saved-trails\"`, it counts `trailblazor.control.shown` as it "
                    + "first renders interactively, so the app can see how often people meet it.",
            ],
        },
        new("skeleton", "Skeleton", ExampleCategory.Feedback,
            "A gray shape that holds the place of content while it loads, with a pulse or a wave.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it while content loads into a shape you already know, so the page doesn't jump.",
            ],
            WhenNotToUse =
            [
                "For work with no shape to show, or when you know how much is done, use "
                    + "[progress](components/progress).",
            ],
            Related = ["progress", "surface"],
            Sections =
            [
                new("shapes", "Shapes", "`Shape` draws a skeleton as a line, circle or rectangle, sized by `Width` and "
                    + "`Height`. Mark the region that's loading with `aria-busy`.", typeof(SkeletonExample)),
                new("wrapping-content", "Wrapping content", "Wrapped around content, a skeleton takes that content's "
                    + "size. The content is hidden from sight, screen readers and the keyboard, and the wrapper has "
                    + "`aria-busy=\"true\"`, until `Loading` turns off.", typeof(SkeletonWrapExample)),
                new("animation", "Animation", "`Animation` sets how a skeleton moves: `Pulse` by default, `Wave` or "
                    + "`None`.", typeof(SkeletonAnimationExample)),
            ],
            Api = [typeof(TbSkeleton)],
            Accessibility =
            [
                "A skeleton is hidden from screen readers, because it has nothing to say.",
                "With reduced motion, the pulse and the wave stop and the shapes stay still.",
                "In forced-colors mode, such as Windows high contrast, each shape keeps an outline.",
            ],
            AppDuties =
            [
                "When a skeleton doesn't wrap the content, mark the region that is loading with `aria-busy=\"true\"`, "
                    + "and set it back to `false` when the content arrives.",
            ],
            Modes = new(ModeSupport.Full, ModeSupport.Full),
            BeforeInteractive =
            [
                PlainHtml,
            ],
            Target = null,
            Blocks = ["tb-skeleton"],
            DataParts =
            [
                new("content", "The content the skeleton stands in for, shown once it loads."),
            ],
            TestingTip = "Skeletons have no role, so wait for the loaded content rather than for them to go, or check "
                + "that the region's `aria-busy` is `false`. Wrapped content is in the page while it loads, but "
                + "hidden, so `ToBeVisibleAsync` waits for it to load.",
        },
        new("toast", "Toast", ExampleCategory.Feedback,
            "A brief message in a corner that doesn't interrupt, with an optional title, icon, content and action.")
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it for a brief update that needs no answer, such as a saved report.",
            ],
            WhenNotToUse =
            [
                "For a message that should stay in view beside what it's about, use an [alert](components/alert).",
                "For a decision people must make before going on, use a [dialog](components/dialog).",
            ],
            Related = ["alert", "dialog", "progress"],
            Sections =
            [
                new("tones", "Tones", "`IToastService.Show` shows a toast, and `Tone` says what kind of message it is. "
                    + "Toasts are read out as they appear: Danger and Warning interrupt, the others wait.",
                    typeof(ToastExample)),
                new("title", "Title", "`Title` adds a heading above a toast's message, read out before the message.",
                    typeof(ToastTitleExample)),
                new("action", "Action", "`Action` adds a button, such as Undo, to a toast. It's read out after the "
                    + "message, as in \"Undo available, press F8.\" Offer the same action somewhere else in the app "
                    + "too, since a toast can be dismissed before it is used.", typeof(ToastActionExample)),
                new("content", "Content and closing", "`Content`, such as a link, shows in place of a toast's "
                    + "message. The message is still what screen readers hear. `Closed`, on the toast that `Show` "
                    + "returns, says why it closed, and `DismissAll` closes every toast.", typeof(ToastContentExample)),
                new("duration", "How long it stays", "`Duration` sets how long a toast stays, and `StayOpen` keeps it "
                    + "until it's dismissed. Danger toasts and toasts with an `Action` stay until they are dismissed "
                    + "unless given a `Duration`, so the action can't time out unseen. Every toast's timer pauses while "
                    + "the pointer is over any toast or the page is hidden, and a toast's timer also pauses while it "
                    + "has focus.", typeof(ToastDurationExample)),
                new("key", "Updating a toast", "`Key` identifies a toast's message. Showing a toast with the key of "
                    + "one that's still open updates that toast instead of adding another.", typeof(ToastKeyExample)),
                new("icon", "Icon", "`Icon` replaces a toast's tone icon, and `ShowIcon = false` removes it. The "
                    + "tone's icon is decorative, since the text says the same.", typeof(ToastIconExample)),
            ],
            Api = [typeof(IToastService), typeof(ToastOptions), typeof(ToastSettings)],
            Keyboard =
            [
                new(["F8"], "Moves focus to the newest toast's first button."),
                new(["Escape"], "Sends focus from the toast back to where it was."),
            ],
            Labeling =
            [
                "The toasts sit in a region named \"Notifications\".",
            ],
            Accessibility =
            [
                "Toasts never take focus themselves.",
                "Toasts fade in and out; with reduced motion they appear and go at once.",
            ],
            Notes =
            [
                new("`options.Toasts` in `AddTrailblazor` sets, for the whole app, where toasts appear (`Position`, "
                    + "top end unless set), how many show at once (`MaxVisible`, 3 unless set) and how long they stay "
                    + "(`Duration`, 5 seconds unless set)."),
            ],
            Modes = new(ModeSupport.None, ModeSupport.None),
            BeforeInteractive =
            [
                "No toast shows until the page is interactive, because they're shown from code.",
            ],
            Ready = new("[data-part=toaster]", "the toast region"),
            Target = new("listitem") { Text = "Report saved", OpenedBy = new("button", "Save report") },
            Blocks = ["tb-toast", "tb-toaster"],
            DataParts =
            [
                new("toaster", "The region that holds the toasts."),
                new("toast", "One toast."),
                new("icon", "The icon that shows the toast's tone."),
                new("title", "The toast's title."),
                new("message", "The toast's text."),
                new("action", "The toast's action button, such as Undo."),
                new("close", "The button that dismisses the toast."),
            ],
            TestingTip = "Each toast is an item in the region named `Notifications`. A closed toast fades out with "
                + "`data-leaving` for a moment, so wait for it to go, as with `ToHaveCountAsync(0)`. "
                + "`await page.Tb().Toasts.WaitForAsync(\"Report archived\")` from `Trailblazor.Testing.Playwright` "
                + "returns the toast that shows the text, to click its action. In bUnit, `await toast.Closed` says "
                + "why it closed.",
            Telemetry =
            [
                "With `ToastOptions.TelemetryName`, such as `\"trail-saved\"`, it counts `trailblazor.control.shown` "
                    + "each time it appears, and `trailblazor.toast.closed` with `trailblazor.outcome`: `action`, "
                    + "`dismissed`, `timed_out`, or `closed` by the app. Its message is never recorded.",
            ],
        },
    ];

    public static Example? Find(string name) => All.FirstOrDefault(example => example.Name == name);
}
