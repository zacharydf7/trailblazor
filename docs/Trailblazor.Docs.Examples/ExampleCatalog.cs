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

public sealed record Example(string Name, string Title, ExampleCategory Category, string Summary, Type Component)
{
    // More files the example's code is split into, such as the dialogs it opens.
    public IReadOnlyList<Type> Parts { get; init; } = [];

    // The types whose parameters or options the example's page tables.
    public required IReadOnlyList<Type> Api { get; init; }

    // What the component does for people using assistive technology, such as its roles and states; text in backticks
    // shows as code, and [text](path) links to another page.
    public required IReadOnlyList<string> Accessibility { get; init; }

    // When the component fits, and when another does instead; both can link to the other component.
    public IReadOnlyList<string> WhenToUse { get; init; } = [];

    public IReadOnlyList<string> WhenNotToUse { get; init; } = [];

    // The catalog names of related components, linked at the end of the page.
    public IReadOnlyList<string> Related { get; init; } = [];

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
    public IReadOnlyList<string> BeforeInteractive { get; init; } = [];

    // Behavior, limitations and known issues that belong to no other section; a pitfall shows as a warning.
    public IReadOnlyList<ExampleNote> Notes { get; init; } = [];

    // How a test finds the component in the example; null for a layout component with no role of its own.
    public required TestTarget? Target { get; init; }

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

    // The accessibility section splits into headings once the entry fills any part beyond what the component does.
    public bool HasAccessibilityParts => Keyboard.Count > 0 || LabelingNotes.Count > 0 || Announcements.Count > 0
        || AppDuties.Count > 0 || BeforeInteractive.Count > 0;

    private const string FieldLabeling = "With no room for a label, an `AriaLabel` names the field through a label "
        + "only screen readers see. A field with neither gets build warning TB004.";

    private const string FieldNote = "`OnFocus` and `OnBlur` report focus moving into the field and out of it, not "
        + "between its own parts, so they suit checking or saving when the user moves on. `AutoFocus` moves focus to "
        + "it as it appears on an interactive page, unless the user is typing in another field by then, or, in a "
        + "dialog or focus scope, as that opens. A `Value` set without `ValueChanged` gets build warning TB003. The "
        + "[forms and validation guide](guides/forms) covers messages, server errors and when validation runs.";

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

    // One section per feature for a component with many; without them, the page shows the example as one section.
    public IReadOnlyList<ExampleSection> Sections { get; init; } = [];

    public IReadOnlyList<ExampleSection> Shown =>
        Sections.Count > 0 ? Sections : [new("example", "Example", null, Component, [.. Parts])];

    public IReadOnlyList<ExampleSource> Sources => [.. Shown.SelectMany(section => section.Sources)];
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
public sealed record ExampleSection(string Id, string Title, string? Description, Type Component, params Type[] Parts)
{
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
            "Headings, body copy and labels from the type scale, rendered as the HTML element you choose.",
            typeof(TextExample))
        {
            Api = [typeof(TbText)],
            Accessibility =
            [
                "It renders the element `As` names, so a heading is a real heading. Choose the level that fits "
                    + "the page's outline, whatever `TextStyle` it looks like.",
                "`TextStyle` changes only the look; it adds no meaning for screen readers.",
                "Text cut off by `MaxLines` is all still there for screen readers, but sighted users can't read the "
                    + "rest, so don't cut off text they need without showing it somewhere else.",
            ],
            Target = new("heading", "Display"),
            Blocks = ["tb-text"],
            TestingTip = "Text cut off by `MaxLines` is still all in the markup, so text assertions and `GetByText` "
                + "match the whole string. Whether it is cut off shows only in a browser, not in bUnit.",
        },
        new("image", "Image", ExampleCategory.Content,
            "An image that requires alternative text and can scale to fit its container.", typeof(ImageExample))
        {
            Api = [typeof(TbImage)],
            Accessibility =
            [
                "`Alt` is required and describes what the image shows; leaving it out throws when it renders. Set "
                    + "it to an empty string for an image that is only decoration, which screen readers then skip.",
                "Say what the image shows in its context, without \"image of\": screen readers already say it's an "
                    + "image.",
                "`Width` and `Height` reserve the image's space before it loads, so the page doesn't jump.",
            ],
            Target = new("img", "A trail winding between two hills"),
            Blocks = ["tb-image"],
        },
        new("divider", "Divider", ExampleCategory.Layout,
            "A horizontal or vertical line that separates content.", typeof(DividerExample))
        {
            Api = [typeof(TbDivider)],
            Accessibility =
            [
                "A horizontal divider is an `<hr>`, which screen readers treat as a separator. A vertical one is "
                    + "a `div` with `role=\"separator\"` and `aria-orientation=\"vertical\"`.",
                "It takes no focus and has no name.",
                "A divider with content, such as \"or\", is a plain `div` rather than a separator, whose children "
                    + "screen readers skip, so the content is read as text and the lines are only drawn.",
            ],
            Target = null,
            Blocks = ["tb-divider"],
            DataParts =
            [
                new("content", "The content between the two lines, such as \"or\"."),
            ],
        },
        new("surface", "Surface", ExampleCategory.Layout,
            "A raised panel with the app's surface color, an optional border and a shadow, for grouping content.",
            typeof(SurfaceExample))
        {
            Api = [typeof(TbSurface)],
            Accessibility =
            [
                "A plain `div` by default. `As` renders it as another element, such as `LayoutElement.Article` for a "
                    + "card that stands on its own, or `LayoutElement.Section` with an `aria-label` for a region "
                    + "screen readers can jump to.",
                "Screen readers and the keyboard follow the order in the markup, so keep it the same as the order "
                    + "on screen.",
            ],
            Target = null,
            Blocks = ["tb-surface"],
        },
        new("icon", "Icon", ExampleCategory.Content,
            "An SVG icon from any icon set, sized to the text around it and hidden from screen readers unless named.",
            typeof(IconExample))
        {
            Api = [typeof(TbIcon)],
            Accessibility =
            [
                "Without a `Label` the icon is decorative and hidden from screen readers with `aria-hidden`.",
                "With a `Label` it's an `img` named by the label. Give one whenever the icon means something that "
                    + "no text next to it says.",
                "`Tone` colors it by meaning, with the same text colors as `TbText`, which meet contrast in both "
                    + "themes. Color alone can't carry the meaning, so pair it with a label or text.",
            ],
            Target = new("img", "Failed"),
            Blocks = ["tb-icon"],
        },
        new("button", "Button", ExampleCategory.Actions,
            "Starts an action, in filled, outlined or text styles and every tone.", typeof(ButtonExample))
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
                new("variants-and-tones", "Variants and tones", "`Variant` sets how much weight a button has, and "
                    + "`Tone` what kind of action it is. Use `Danger` for what can't be undone.", typeof(ButtonExample)),
                new("clicks", "Clicks", "`OnClick` runs when the button is pressed by a pointer, Enter or Space.",
                    typeof(ButtonClickExample)),
                new("sizes", "Sizes", "`Size` is `Small`, `Medium` or `Large`.", typeof(ButtonSizeExample)),
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
            BeforeInteractive =
            [
                "It renders as the same native element. A link already works, and a submit button posts its form. "
                    + "`OnClick` starts working once the page is interactive.",
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
            "A button that shows only an icon, with the accessible name it needs.", typeof(IconButtonExample))
        {
            Api = [typeof(TbIconButton), typeof(IconButtonClasses)],
            Accessibility =
            [
                "`AriaLabel` is required and names the button, since it shows no text. A blank label written as "
                    + "text in markup gets build warning TB001, and any blank label throws when it renders.",
                "The icon itself is hidden from screen readers. Set `ShowTooltip` so sighted users can see the "
                    + "name too, on hover and on keyboard focus. The tooltip repeats the name, so it isn't added as a "
                    + "description, and screen readers hear the name once.",
                "Keys, `Disabled`, `FocusableWhenDisabled`, `Loading`, `Href` and `@bind-Pressed` work as they do "
                    + "for a button. A toggle keeps the same `AriaLabel` either way; `aria-pressed` tells whether it "
                    + "is on.",
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
            "Joins related buttons into one control, in a row or a column, with settings shared by all of them.",
            typeof(ButtonGroupExample))
        {
            Api = [typeof(TbButtonGroup)],
            Anatomy = """
                <TbButtonGroup aria-label="…">
                    <TbButton>…</TbButton>
                    <TbButton>…</TbButton>
                    <TbMenu Label="…">…</TbMenu>
                </TbButtonGroup>
                """,
            Accessibility =
            [
                "A `div` with `role=\"group\"`. Give it an `aria-label` that says what the buttons do together.",
                "Each button keeps its own tab stop; the group adds no arrow-key navigation.",
                "`Disabled` on the group turns every button off, as each button's own `Disabled` would. `Variant`, "
                    + "`Tone` and `Size` apply to every button that doesn't set its own.",
                "A menu, popover or tooltip around a button joins the group with it, so a split button is a button "
                    + "and a menu side by side. Give the menu's icon trigger a `Label` that says what it opens.",
            ],
            Target = new("group", "Text alignment"),
            Blocks = ["tb-button-group"],
        },
        new("toggle-group", "Toggle group", ExampleCategory.Actions,
            "A row of toggle buttons where one, or any number, can be pressed.", typeof(ToggleGroupExample))
        {
            RightToLeftKeys = "Left moves to the next item and Right to the previous one.",
            Api = [typeof(TbToggleGroup<>), typeof(TbMultiToggleGroup<>), typeof(TbToggleItem<>)],
            Anatomy = """
                <TbToggleGroup AriaLabel="…" @bind-Value="_value">
                    <TbToggleItem Value="…">…</TbToggleItem>
                    <TbToggleItem Value="…">…</TbToggleItem>
                </TbToggleGroup>

                <TbMultiToggleGroup AriaLabel="…" @bind-Value="_values">
                    <TbToggleItem Value="…">…</TbToggleItem>
                </TbMultiToggleGroup>
                """,
            Accessibility =
            [
                "A `group` named by its required `AriaLabel`, holding toggle buttons with `aria-pressed`. An item "
                    + "with only an icon needs an `AriaLabel` of its own.",
                "The group is one tab stop, landing on the pressed item, or the first one. Left and Right move "
                    + "between the items (Up and Down when `Vertical`), Home and End go to the first and last, and "
                    + "disabled items are skipped. Space or Enter presses the focused item.",
                "In a `TbToggleGroup`, pressing the pressed item again keeps it pressed unless the group is "
                    + "`Clearable`.",
                "In forced-colors mode a pressed item takes the system highlight.",
            ],
            Target = new("group", "Map view"),
            Blocks = ["tb-toggle-group", "tb-button"],
            DataParts =
            [
                new("item", "One toggle button in the group."),
            ],
            TestingTip = "Press an item by its name, as `GetByRole(AriaRole.Button, new() { Name = \"Satellite\" })`, "
                + "and check it with `ToHaveAttributeAsync(\"aria-pressed\", \"true\")`. Wait for the group's "
                + "`data-ready` attribute before testing the arrow keys.",
            Telemetry =
            [
                "With a `TelemetryName` on an item, such as `\"view-as-list\"`, the item counts "
                    + "`trailblazor.control.shown` as it first renders and `trailblazor.control.used` each time it is "
                    + "pressed.",
            ],
        },
        new("theme-picker", "Theme picker", ExampleCategory.Actions,
            "Lets people choose a light or dark page, or follow their device, and remembers the choice.",
            typeof(ThemePickerExample))
        {
            Api = [typeof(TbThemePicker), typeof(IThemeService), typeof(ThemeSettings)],
            Accessibility =
            [
                "A `group` named \"Theme\", holding toggle buttons for Light, Dark and Match the device, each named "
                    + "by the localizer and marked with `aria-pressed`.",
                "The group is one tab stop. Left and Right move between the choices, and Space or Enter picks one, as "
                    + "in a toggle group.",
                "`TbThemeHead` in `<head>` puts a remembered choice back before the page paints, so a dark page "
                    + "never flashes light first.",
            ],
            Target = new("group", "Theme"),
            Blocks = ["tb-toggle-group", "tb-button"],
            TestingTip = "Pick a mode with `GetByRole(AriaRole.Button, new() { Name = \"Dark\" })`, and check the "
                + "page with `ToHaveAttributeAsync(\"data-tb-theme\", \"dark\")` on `html`. A test that needs one "
                + "theme can set `data-tb-theme` on `html` itself.",
        },
        new("toolbar", "Toolbar", ExampleCategory.Actions,
            "A row of controls that is one tab stop. When it is too narrow, its toolbar buttons move into a menu at "
                + "its end.", typeof(ToolbarExample))
        {
            RightToLeftKeys = "Left moves to the next control and Right to the previous one.",
            Api = [typeof(TbToolbar), typeof(TbToolbarButton)],
            Accessibility =
            [
                "A `toolbar` named by its required `AriaLabel`, and one tab stop. Left and Right (Up and Down when "
                    + "`Vertical`) move between every control in it, toggle group items included; Home and End go to "
                    + "the first and last. A text field keeps its arrow keys.",
                "A `TbToolbarButton` with an `Icon` is an icon button named by its `Text`, which its tooltip shows "
                    + "too. A tooltip that repeats a button's name isn't read a second time as its description.",
                "Toolbar buttons with no room move into the overflow menu named by `OverflowLabel` (\"More\" by "
                    + "default), where each is a `menuitem` with the same text and action. Other content never moves.",
            ],
            Target = new("toolbar", "Trail notes"),
            Blocks = ["tb-toolbar"],
            DataParts =
            [
                new("overflow", "The More menu that holds the items a narrow toolbar can't fit."),
            ],
            Anatomy = """
                <TbToolbar AriaLabel="…">
                    <TbToolbarButton Text="…" Icon="…" OnClick="…" />
                    <TbToolbarButton Text="…" Icon="…" OnClick="…" />
                </TbToolbar>
                """,
            TestingTip = "Find a toolbar button by its `Text`, as `GetByRole(AriaRole.Button, new() { Name = \"Add a "
                + "note\" })`. To test the overflow, set the page's viewport narrow, wait for the toolbar's "
                + "`data-ready`, open the menu button named \"More\" and choose the `menuitem`.",
        },
        new("insert-divider", "Insert divider", ExampleCategory.Actions,
            "A line between two items that reveals an insert button on hover or focus, for adding an item in that "
                + "place.", typeof(InsertDividerExample))
        {
            Api = [typeof(TbInsertDivider)],
            Accessibility =
            [
                "The insert button is a real button in the tab order, named by the required `Label`. Say where the "
                    + "new item goes, such as \"Add a step after Pack the tent\".",
                "It shows when the pointer rests on the line or when the button has focus. On a device with a touch "
                    + "screen, which can't hover, it stays in view.",
                "Every divider is a tab stop, so a long list gets many. For a long list, consider one Add button "
                    + "that inserts after the focused item instead.",
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
            "Lays its children out in a row or a column, evenly spaced.", typeof(StackExample))
        {
            FullWidth = true,
            Api = [typeof(TbStack)],
            Accessibility =
            [
                "A plain `div` by default; it only lays out what it holds. `As` renders it as another element, such as "
                    + "`LayoutElement.Nav` for a group of links or `LayoutElement.Ul` for a list, whose items are "
                    + "`li` elements. A list keeps `role=\"list\"`, since Safari stops reading a list without "
                    + "bullets as one.",
                "`RowFrom` keeps the items in a column on narrow screens and lays them in a row from a breakpoint; "
                    + "the reading order stays the same either way.",
                "Screen readers and the keyboard follow the order in the markup, so keep it the same as the order "
                    + "on screen.",
            ],
            Target = null,
            Blocks = ["tb-stack"],
        },
        new("spacer", "Spacer", ExampleCategory.Layout,
            "Takes up the free space in a stack, pushing what follows to the end.", typeof(SpacerExample))
        {
            FullWidth = true,
            Api = [typeof(TbSpacer)],
            Accessibility =
            [
                "A plain `div` with no role and no content; screen readers skip it.",
            ],
            Target = null,
            Blocks = ["tb-spacer"],
        },
        new("grid", "Grid", ExampleCategory.Layout,
            "A responsive 12-column grid for page layouts.", typeof(GridExample))
        {
            FullWidth = true,
            Api = [typeof(TbGrid), typeof(TbGridItem)],
            Anatomy = """
                <TbGrid>
                    <TbGridItem SpanMd="6">…</TbGridItem>
                    <TbGridItem SpanMd="6">…</TbGridItem>
                </TbGrid>
                """,
            Accessibility =
            [
                "Plain `div`s by default; they only lay out what they hold. Render the grid `As` `LayoutElement.Ul` "
                    + "and its items as `LayoutElement.Li` for a list of cards, which screen readers count.",
                "`Offset` leaves columns empty before an item but doesn't change the reading order. Screen readers "
                    + "and the keyboard follow the order in the markup, so keep it the same as the order on screen.",
                "An item's `Span` can be `GridSpan.Grow`, which takes what's left of its row, or `GridSpan.Auto`, "
                    + "which sizes to its content; an item that doesn't fit starts the next row.",
            ],
            Target = null,
            Blocks = ["tb-grid", "tb-grid-item"],
        },
        new("splitter", "Splitter", ExampleCategory.Layout,
            "Lays out panes side by side or stacked, with separators that resize them by dragging, with the "
                + "keyboard or from a menu.", typeof(SplitterExample))
        {
            RightToLeftKeys = "Left and Right move the separator the way they point, so Left grows the start pane, "
                + "now on the right.",
            FullWidth = true,
            Api = [typeof(TbSplitter), typeof(TbSplitterPane)],
            Accessibility =
            [
                "Each separator is a focusable `separator` named \"Resize\" followed by the `Label` of the pane before "
                    + "it, with `aria-valuenow` giving that pane's size in percent and `aria-controls` pointing to it.",
                "On a separator, the arrows resize by 1% (10% with Shift), Home and End take the pane before it to "
                    + "its smallest and largest, and Enter collapses or restores a `Collapsible` pane.",
                "A collapsed pane keeps its content but hides it from sight and from the tab order.",
                "Each separator has a grip in its middle, a menu `button` named \"Size of\" followed by the `Label` "
                    + "of the pane before it. Dragging the grip resizes like dragging the separator, and a click or "
                    + "tap opens its menu: Make larger and Make smaller (by 10%), plus Collapse or Restore for a "
                    + "`Collapsible` pane. The menu is the single-pointer alternative to dragging (WCAG 2.5.7). The "
                    + "grip stays out of the tab order, since the separator's keys do the same.",
                "Dragging works in right-to-left layouts.",
            ],
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
                + "then check `ToHaveAttributeAsync(\"aria-valuenow\", \"40\")`. Wait first for `data-ready` "
                + "on the splitter's root, `[data-part=splitter]`.",
        },
        new("container", "Container", ExampleCategory.Layout,
            "Centers content and caps its width at a readable size.", typeof(ContainerExample))
        {
            FullWidth = true,
            Api = [typeof(TbContainer)],
            Accessibility =
            [
                "A plain `div` by default; it only limits the width of what it holds. Render it `As` "
                    + "`LayoutElement.Main` to make it the page's main landmark.",
            ],
            Target = null,
            Blocks = ["tb-container"],
        },
        new("alert", "Alert", ExampleCategory.Feedback,
            "A message in the page, such as a warning or an error, that can be closed.", typeof(AlertExample))
        {
            Api = [typeof(TbAlert), typeof(AlertClasses)],
            Accessibility =
            [
                "Danger and Warning alerts have `role=\"alert\"`, which screen readers read out at once. The "
                    + "other tones have `role=\"status\"`, read out when the reader is idle.",
                "Screen readers may not read a status alert that is added to the page with its message already in "
                    + "it. Add it empty and then set the message, or use `IAnnouncer`.",
                "The icon is decorative, so the `Title` and message must carry the meaning. Success, Warning, Danger "
                    + "and Info each have their own icon shape, so those tones don't rest on color alone. The close "
                    + "button is named by `CloseLabel`, \"Close\" by default.",
                "In a `Filled` alert, buttons and the focus ring take the alert's text color, so they stay readable "
                    + "on the fill.",
                "`Actions` sit after the message, so screen readers reach them after hearing it.",
            ],
            Target = new("alert") { Text = "A danger alert." },
            Blocks = ["tb-alert"],
            DataParts =
            [
                new("icon", "The icon that shows the alert's tone."),
                new("title", "The alert's title."),
                new("message", "The alert's text."),
                new("actions", "The buttons the alert offers."),
                new("close", "The button that dismisses the alert."),
            ],
        },
        new("chip", "Chip", ExampleCategory.Content,
            "A compact label for a tag, a filter or a choice, which can be clicked, toggled or removed.",
            typeof(ChipExample))
        {
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
            Accessibility =
            [
                "A `TbChipGroup` is a `list` named by its required `AriaLabel`, and each chip in it is a `listitem`.",
                "A chip with `OnClick` is a `button`, with `Href` a `link`, and with `@bind-Selected` a toggle button "
                    + "with `aria-pressed`. Other chips are plain text.",
                "A removable chip has a remove button named \"Remove\" followed by the chip's `Text`; Delete or "
                    + "Backspace on the chip removes it too. In a group, focus then moves to the chip that takes its "
                    + "place, or to the group when none is left.",
                "In forced-colors mode, such as Windows high contrast, every chip keeps an outline and a selected "
                    + "one is outlined in the system's highlight color.",
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
                + "links or buttons, and one can be marked as the current item.", typeof(ListExample))
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
                + "order sets which runs first.", typeof(SortableExample))
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it when the order of a short list is what people are deciding.",
            ],
            WhenNotToUse =
            [
                "To reorder rows with columns, use a [data grid](components/data-grid) with `OnRowMove`.",
                "To reorder nested items, use a [tree view](components/tree-view) with `OnDrop`.",
            ],
            Related = ["data-grid", "tree-view", "list"],
            Sections =
            [
                new("order", "Put items in order", "`OnMove` gets each move and the app applies it. `CanMove` can "
                    + "refuse one and say why, as for the last rule here. `MoveAsync(from, to)` moves an item from the "
                    + "app's own buttons, such as Raise, with the same check and announcement.",
                    typeof(SortableExample)),
                new("content", "Your own content", "`ItemTemplate` draws each item, and long text wraps inside it. "
                    + "`ItemText` names the handle when items aren't strings, and `ItemKey` tells items apart as they "
                    + "move.", typeof(SortableContentExample)),
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
            ],
            BeforeInteractive =
            [
                "The items render in their order. Dragging and the handles start working once the page is interactive.",
            ],
            Target = new("list", "Trail alerts, first match wins"),
            Blocks = ["tb-sortable"],
            DataParts =
            [
                new("list", "The list that holds the items."),
                new("item", "One item."),
                new("handle", "The item's drag handle, which also moves it with the keyboard or its menu."),
                new("drop-reason", "The reason shown while a dragged item is held over a place it may not go."),
            ],
            TestingTip = "Wait for `data-ready` on the list's root, then press keys on a handle, found as "
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
            "A small count or dot on an icon or button, or in a line of text, that draws the eye to something new.",
            typeof(BadgeExample))
        {
            Api = [typeof(TbBadge)],
            Accessibility =
            [
                "A badge on an icon or button is hidden from screen readers unless it has a `Label`, and Tab goes to "
                    + "the button and skips it. So put what the badge says in the button's own name, such as "
                    + "`AriaLabel=\"Messages, 120 unread\"` or `AriaLabel=\"Filters, some applied\"`, and give a badge "
                    + "on an icon that isn't a button a `Label`.",
                "A badge in a line of text is read in its place. Give it a `Label`, such as \"3 unsaved changes\", "
                    + "which screen readers hear instead of the bare number; a dot with no `Label` isn't read out.",
                "It takes no focus and never catches clicks, which reach the icon or button under it.",
                "A count of 0 hides the badge, since there is nothing new; `ShowZero` shows it, such as for a "
                    + "count of errors, where zero is good news.",
                "In forced-colors mode, such as Windows high contrast, the badge keeps an outline and a dot keeps its "
                    + "fill.",
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
            "Shows how far along work is, as a bar or a circle, or that it is still going.", typeof(ProgressExample))
        {
            Api = [typeof(TbProgress)],
            Accessibility =
            [
                "A `progressbar` with its range and current value. Leaving `Value` null makes it indeterminate, "
                    + "with no current value.",
                "A `Label` names it through `aria-labelledby`, as \"Importing trails\" does. Without one, give an "
                    + "`AriaLabel`. Also give one when the label alone doesn't say what is in progress, such as a file "
                    + "name; the `AriaLabel` then names it instead. With neither, it throws when it renders.",
                "`Label` and `ShowValue` add visible text. With `ValueFormatter`, screen readers announce that "
                    + "text, such as \"3 of 10 files\", in place of the number. Show a ring's value at `Size.Large`, "
                    + "where it is big enough to read.",
                "With reduced motion, the moving bar and the spinning ring pulse instead.",
                "In forced-colors mode, such as Windows high contrast, the fill takes the system's highlight color.",
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
            "A label for a control or a group of controls, with an optional info button that explains the setting.",
            typeof(LabelExample))
        {
            FullWidth = true,
            WhenToUse =
            [
                "Use it to name a control or a group that has no label of its own, or to add an info button for help "
                    + "most people don't need every time.",
            ],
            WhenNotToUse =
            [
                "For a Trailblazor field with no info button, set the field's own `Label`.",
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
                + "the first error when a submit fails.", typeof(FormExample))
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
                new("model-and-annotations", "Model and data annotations", "`Model` and `DataAnnotations` check a "
                    + "class's annotations alongside the fields' own checks. Here each field's `Required` covers "
                    + "empty values, and the annotations cover the rest.", typeof(FormModelExample)),
                new("your-own-edit-context", "Your own edit context", "An `EditContext` of your own tells you what "
                    + "has changed, and can clear that after a save. `ResetValidationAsync` hides the messages again "
                    + "after an undo.", typeof(FormEditContextExample)),
                new("server-errors", "Server errors", "A field's `Error` shows what the server said, and blocks the "
                    + "next submit until it is cleared, so clear it when the value changes. `Loading` shows the save "
                    + "is running and ignores more clicks.", typeof(FormServerExample)),
                new("error-summary", "Error summary", "For a long form, list the errors above it, and let each one "
                    + "move focus to its field. `OnSubmit` leaves focus to you, so the summary can take it, and the "
                    + "page title starts with \"Error:\" while there are errors.", typeof(FormErrorSummaryExample)),
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
            ],
            Accessibility =
            [
                "It renders a `form` element, built on `EditForm`.",
                "When a submit fails, every field shows its messages, linked to it with `aria-describedby`, and focus "
                    + "moves to the first field with an error, or into a radio or checkbox group where its "
                    + "`FocusAsync()` would.",
                "With `OnSubmit`, focus stays where it is, so your handler can move it.",
            ],
            AppDuties =
            [
                "Leave the submit button on, so a submit can show what to fix. [The guide]"
                    + "(guides/forms#leave-the-submit-button-on) explains why.",
                "Word each message so it says how to fix the problem, as [the guide](guides/forms#wording) describes.",
                "Say when a submit succeeds, with an `output` element as in these examples, or the [announcer]"
                    + "(guides/announcements).",
            ],
            BeforeInteractive =
            [
                "With a `FormName`, it posts as a static form. The fields check themselves on the server, and the "
                    + "page comes back with their messages. With no script running, focus doesn't move and nothing "
                    + "is read out.",
            ],
            Notes =
            [
                new("`@bind-IsValid` checks the fields' `Validation`, and with `DataAnnotations` the model's "
                    + "annotations, without showing any message. When the app calls `AddValidation` and marks the "
                    + "model `[ValidatableType]`, it checks nested objects too, as a submit does."),
                new("A field's `Error`, like a `Required` field left empty, keeps the form from submitting and "
                    + "`IsValid` false. `Invalid` marks a field without a message and doesn't stop a submit."),
                new("A field's `Required` and a model's `[Required]` each add a message, so use one of them for a "
                    + "field. The [guide](guides/forms#model-and-data-annotations) says which.") { Pitfall = true },
                new("A form with `OnSubmit` isn't counted in `trailblazor.form.submitted`, since it doesn't validate "
                    + "the submit itself."),
                new("Every field has `FocusAsync()`, to move focus from code. A disabled field doesn't take focus."),
            ],
            Target = new("form", "Trip sign-up"),
            Blocks = ["tb-form"],
            TestingTip = "Submit with the button, then find errors through the fields: "
                + "`ToHaveAttributeAsync(\"aria-invalid\", \"true\")` on the input, and its message with "
                + "`ToHaveAccessibleDescriptionAsync`.",
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
            "A labeled text input with help text and validation messages.", typeof(TextFieldExample))
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
                new("label-and-help", "Label and help text", "`Label` names the field, and `HelperText` says what people "
                    + "need before they type. A long label wraps. The value stays `null` until something is typed.",
                    typeof(TextFieldExample)),
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
                new("While a `Required` message shows, the field's own `Validation` doesn't run, so it is the only "
                    + "message. `Invalid` marks the field invalid without a message, for a form that lists its errors "
                    + "elsewhere."),
                new("An `Error` keeps its form from submitting until the parent clears it."),
                new("`@ref` the field and call `FocusAsync()` to move focus to it, or `SelectTextAsync()` to also select "
                    + "its text so typing replaces it."),
                new("`Clearable` on a number needs a nullable type, such as `int?`."),
                new("With `UpdateDelay`, Enter in a single-line field, or leaving the field, updates the value at once."),
                new("`MaxLines` needs a browser with CSS `field-sizing`."),
                new("With a `Mask`, empty slots make the field invalid, the placeholder shows the pattern, and a pattern "
                    + "of digits brings up a number keyboard."),
                new("Each `Size` is set from the theme's density, as a button's is."),
            ],
            Target = new("textbox", "Name"),
            Blocks = ["tb-field"],
            DataParts =
            [
                .. FieldParts,
                new("input", "The text input."),
                new("clear", "The button that clears the text."),
            ],
            TestingTip = "A field with `UpdateDelay` takes its value once typing pauses, so assert with Playwright's "
                + "`Expect`, which waits, or press Enter to update it at once. In bUnit, register a `FakeTimeProvider` "
                + "as the `TimeProvider` and advance it past the delay.",
        },
        new("number-field", "Number field", ExampleCategory.Inputs,
            "A labeled number input with step buttons and limits.", typeof(NumberFieldExample))
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
                    + "doesn't change. `Step` sets how far the arrow keys and buttons move the value.",
                    typeof(NumberFieldExample)),
                new("values-set-by-code", "Values set by code", "`Min`, `Max` and `ClampToRange` don't apply to a value "
                    + "your code sets, such as one loaded from a saved trip, so it can sit outside the limits and still "
                    + "submit. `Validation` checks it, as here.", typeof(NumberFieldCheckedExample)),
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
                    + "they turn off at `Min` and `Max`.",
                "Helper text and validation messages are linked to the field with `aria-describedby`, an error sets "
                    + "`aria-invalid`, and `Required` sets `aria-required`; the asterisk it adds is hidden from screen "
                    + "readers.",
            ],
            BeforeInteractive =
            [
                "Before the page is interactive, a field with a `Format` is a plain number box, since a static form "
                    + "posts numbers in the invariant culture.",
                "A static form's post isn't checked against `Min` and `Max`, so use `Validation` for a limit the server "
                    + "must enforce.",
            ],
            Notes =
            [
                new("What the user types stays as typed until they leave the field or press Enter, and an error waits "
                    + "until then too."),
                new("`ClampToRange` also moves a number too large or small for the value's type to the nearer limit."),
            ],
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
            "Turns an option on or off, or shows that only some of a group are on.", typeof(CheckboxExample))
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
            "Picks any number of options from a short list shown all at once.", typeof(CheckboxGroupExample))
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
            "Turns a setting on or off right away.", typeof(SwitchExample))
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
                    + "high-contrast theme.",
                "Every `Tone` stands out from the page by at least 3:1 when on; warning uses its text color to reach "
                    + "that contrast.",
            ],
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
            ],
            TestingTip = "It's a native checkbox, so Playwright's `CheckAsync()` and `UncheckAsync()` work on "
                + "`GetByRole(AriaRole.Switch, ...)`. In bUnit, `Change(true)` on the `input` turns it on, but a "
                + "browser script is what stops a `ReadOnly` switch from changing, so a `Change` on one still changes "
                + "the value. Test read-only behavior in Playwright.",
            Telemetry = CheckboxTelemetry,
        },
        new("slider", "Slider", ExampleCategory.Inputs,
            "Chooses a number, or a range, by dragging a thumb along a track or with the arrow keys.",
            typeof(SliderExample))
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
            RightToLeftKeys = "Left raises the value and Right lowers it, so the thumb moves the way the key points. "
                + "Safari doesn't swap them yet.",
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
                    + "other. `StartLabel` and `EndLabel` name the thumbs.", typeof(SliderRangeExample)),
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
                new("`Min` must be below `Max`, both must fit the number type, and `Step` must be above 0. A whole number "
                    + "type also needs a whole `Min`, `Max` and `Step`. Anything else throws when it renders."),
                new("`@ref` the slider and call `FocusAsync()` to move focus to it. A range slider focuses its start "
                    + "thumb."),
            ],
            BeforeInteractive =
            [
                "It posts in a static form, as a number or as `Start` and `End`.",
                "Before the page is interactive, a thumb moves, but the fill, the shown value and the text screen "
                    + "readers hear stay where they were until the next render.",
                "Nothing stops a range slider's thumbs from passing each other until the page is interactive, so on a "
                    + "static post, check that `Start` isn't above `End`.",
                "`ReadOnly` already works on a static page.",
            ],
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
            TestingTip = "Set a slider with `await page.GetByRole(AriaRole.Slider, new() { Name = \"Map brightness\" "
                + "}).FillAsync(\"80\")`, which Playwright supports for range inputs. Then check what the app "
                + "did with it, such as `Expect(page.GetByText(\"Saved at\")).ToHaveTextAsync(\"Saved at 80%\")`: "
                + "`ToHaveValueAsync(\"80\")` passes as soon as the input changes, before the app hears of it. A range "
                + "slider's thumbs are named \"Elevation Minimum\" and \"Elevation Maximum\".",
        },
        new("radio-group", "Radio group", ExampleCategory.Inputs,
            "Picks one option from a short list shown all at once.", typeof(RadioGroupExample))
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
            Related = ["select", "toggle-group", "checkbox-group", "switch", "form"],
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
                new("`@ref` the group and call `FocusAsync()` to move focus where Tab would: to the chosen radio, or "
                    + "else the first radio that isn't disabled."),
            ],
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
        new("select", "Select", ExampleCategory.Inputs,
            "Picks one or several options from a list that opens under the field.", typeof(SelectExample))
        {
            FullWidth = true,
            Sections =
            [
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
                new("empty-list", "Empty list", "A select with no options opens an empty list, so add a disabled "
                    + "option that says why.", typeof(SelectEmptyExample)),
                new("many-options", "Many options", "A long list scrolls, and `Filterable` lets people search it. A "
                    + "long option widens the list, and once chosen it's cut off with an ellipsis. When the options "
                    + "come from a search as someone types, use an "
                    + "[autocomplete](components/autocomplete).", typeof(SelectManyExample)),
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
                new(["A letter"], "Moves to an option that starts with what was typed. A Space typed mid-search adds "
                    + "to the search rather than choosing.")
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
            BeforeInteractive =
            [
                "It renders as the browser's own `<select>`, so it still works and posts with its form.",
                "In a single select, a value no option has stays chosen in the native select.",
                "A `ReadOnly` native select puts its choice back whenever the user changes it, and posts. A `Disabled` "
                    + "one doesn't post.",
                "An option whose content has no words has no name until the page is interactive, even with `Text`.",
            ],
            Notes =
            [
                new("A select bound to a value type, such as `Guid`, clears to its default, which shows the option "
                    + "with that value or else the placeholder, and `Required` never fires on it. Bind the nullable "
                    + "form, such as `Guid?`.") { Pitfall = true },
            ],
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
                + "\"Select all\" is an option like the others, so `ChooseAsync(\"Select all\")` chooses it.",
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
        new("autocomplete", "Autocomplete", ExampleCategory.Inputs,
            "A text field that suggests values as the user types, from a list or an async search.",
            typeof(AutocompleteExample))
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
                new("many-items", "Many items", "A long list scrolls. Press Down to browse it, or type to narrow it "
                    + "down.", typeof(AutocompleteManyExample)),
                new("free-text", "Free text", "With `FreeText`, typed text that isn't a suggestion becomes the value, "
                    + "converted to the value's type. Without it, leaving the field puts back the chosen value's text.",
                    typeof(AutocompleteFreeTextExample)),
                new("search-states", "Searching, no results and failure", "The list says when a search is running, when "
                    + "nothing matches, and when a search fails, which also raises `OnSearchFailed`. The field stays "
                    + "usable, and typing again searches again. Type a campsite's name, then something that matches "
                    + "nothing. To see a failure, check the box and type again.",
                    typeof(AutocompleteStatesOfSearchExample)),
                new("limits", "Minimum characters and maximum items", "`MinCharacters` sets how much someone must type "
                    + "before suggestions show, and `MaxItems` caps how many the list shows.",
                    typeof(AutocompleteLimitsExample)),
                new("debounce", "Search delay", "`DebounceDelay` sets how long someone must stop typing before `Search` "
                    + "runs. It defaults to 200 milliseconds. Set it to zero to search immediately.",
                    typeof(AutocompleteDelayExample)),
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
                new("read-only-and-disabled", "Read-only and disabled", "`ReadOnly` keeps the list closed and hides the "
                    + "clear button. `Disabled` takes the field out of the tab order and the post.",
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
                new(["Escape"], "Closes the list.") { When = "While open" },
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
            Accessibility =
            [
                "The input has `aria-autocomplete=\"list\"` and controls a `listbox` of `option`s. Up and Down move "
                    + "through the list, not the caret.",
            ],
            BeforeInteractive =
            [
                "It is a plain text input, so a static form still posts what was typed.",
            ],
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
                + "search, and clicks the option by its name. In bUnit, `Search` runs once `DebounceDelay` has passed "
                + "on the app's `TimeProvider`, so advance a `FakeTimeProvider` past it, or set it to `TimeSpan.Zero`.",
        },
        new("color-picker", "Color picker", ExampleCategory.Inputs,
            "Chooses a color by typing it, picking a swatch, using the custom picker, or taking it from the screen.",
            typeof(ColorPickerExample))
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
                + "Left raises the value and Right lowers it. Safari doesn't swap them.",
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
            Accessibility =
            [
                "The saturation and brightness pad is for the pointer; the Hue, Saturation, Brightness and Opacity "
                    + "sliders and the Red, Green and Blue boxes give keyboard and screen reader users the same "
                    + "control.",
            ],
            BeforeInteractive =
            [
                "It is a plain text box that only takes `#rrggbb` (or `#rrggbbaa` with `Alpha`), so a static form posts "
                    + "a hex color, as typed.",
            ],
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
                + "set `FakeBrowserService.PickedColor` to test the eyedropper.",
        },
        new("file-upload", "File upload", ExampleCategory.Inputs,
            "Chooses files by clicking or dropping them on a drop zone, and lists them with a way to remove each.",
            typeof(FileUploadExample))
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
                    + "the field disables it: the picker can't be focused and its files don't post. `Disabled` behaves "
                    + "and looks the same on this field.",
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
        new("date-range-picker", "Date range picker", ExampleCategory.Inputs,
            "Chooses a range of dates, typed or picked in a two-month calendar, with ready-made ranges.",
            typeof(DateRangePickerExample))
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
                new("format-and-culture", "Format and culture", "`Format` sets how the boxes show and read dates. "
                    + "`Culture` sets the month and day names, the first day of the week, and the format when `Format` "
                    + "isn't set. It defaults to the current culture.", typeof(DateRangePickerFormatExample)),
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
                    + "the label and \"Start date\" or \"End date\", and shows the format as its placeholder.",
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
                "In forced-colors mode, such as Windows high contrast, the range's days take the system's highlight, "
                    + "its first and last days add an outline, and the range previewed under the pointer has only a "
                    + "dashed outline, so it can't be taken for a chosen range.",
            ],
            Notes =
            [
                new("`@ref` the picker and call `FocusAsync()` to move focus to the start box."),
            ],
            BeforeInteractive =
            [
                "It shows the browser's own date boxes, so a static form still posts both dates.",
            ],
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
                + "field has taken them. Its boxes have `data-part=\"start-input\"` and `data-part=\"end-input\"`.",
        },
        new("listbox", "List box", ExampleCategory.Inputs,
            "Picks one or several options from a list that is always shown.", typeof(ListBoxExample))
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
                new("multiple", "Multiple", "`TbMultiListBox` binds a list of the chosen values.",
                    typeof(ListBoxMultipleExample)),
                new("groups", "Groups", "`TbOptionGroup` groups options under a heading. The arrows and typing move "
                    + "through every group.", typeof(ListBoxGroupsExample)),
                new("rich-options", "Options with more than text", "An option's content can hold more than text, such "
                    + "as a chip, and its words name the option. `Text` is for content without words, such as an "
                    + "image, and replaces that name, so include any words the option shows.",
                    typeof(ListBoxContentExample)),
                new("empty-list", "Empty list", "A list box with no options shows an empty box, so add a disabled "
                    + "option that says why.", typeof(ListBoxEmptyExample)),
                new("many-options", "Many options", "A long list scrolls, and a long option wraps. When people would "
                    + "rather search than scroll, use a filterable [select](components/select) or an "
                    + "[autocomplete](components/autocomplete).",
                    typeof(ListBoxManyExample)),
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
                typeof(FieldClasses)],
            Keyboard =
            [
                new(["Down", "Up"], "Moves to the next or previous option that isn't disabled, and chooses it.")
                    { When = "In a `TbListBox`" },
                new(["Home", "End"], "Moves to the first or last option that isn't disabled, and chooses it.")
                    { When = "In a `TbListBox`" },
                new(["A letter"], "Moves to an option that starts with what was typed, and chooses it.")
                    { When = "In a `TbListBox`" },
                new(["Space"], "Chooses the active option. A Space typed mid-search adds to the search instead.")
                    { When = "In a `TbListBox`" },
                new(["Down", "Up"], "Moves to the next or previous option that isn't disabled.")
                    { When = "In a `TbMultiListBox`" },
                new(["Home", "End"], "Moves to the first or last option that isn't disabled.")
                    { When = "In a `TbMultiListBox`" },
                new(["A letter"], "Moves to an option that starts with what was typed.")
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
            ],
            Accessibility =
            [
                "It is one tab stop.",
            ],
            BeforeInteractive =
            [
                "It renders as the browser's own `<select>` list, so it still works and posts with its form.",
                "A `ReadOnly` native list puts its choice back whenever the user changes it, and posts. A `Disabled` "
                    + "one doesn't post.",
                "An option whose content has no words has no name until the page is interactive, even with `Text`.",
            ],
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
                    + "time it is chosen. A disabled or read-only list box chooses nothing, so it counts nothing; the "
                    + "value is never recorded.",
            ],
        },
        new("tooltip", "Tooltip", ExampleCategory.Overlays,
            "A short description that appears on hover or keyboard focus.", typeof(TooltipExample))
        {
            Api = [typeof(TbTooltip), typeof(TooltipSettings)],
            Accessibility =
            [
                "The tooltip describes its anchor: its text goes into the `aria-describedby` of the first "
                    + "focusable element inside. It's a description, not a name, so that element still needs a name of "
                    + "its own. A tooltip that only repeats that element's `aria-label` isn't added, so it isn't read "
                    + "twice.",
                "It shows at once on keyboard focus and after a short delay on hover. A click or tap on a button "
                    + "hides it, and neither the focus that click leaves nor the focus handed back by a dialog closed "
                    + "with the mouse shows it, so it doesn't cover what someone is working on. A text field shows it "
                    + "on any focus, since its user is about to type. Moving the pointer onto the tooltip keeps it "
                    + "open, and Escape hides it, without also closing a dialog it is in. `ShowDelay` and `HideDelay` "
                    + "change the delays for one tooltip, and `options.Tooltips` in `AddTrailblazor` for the whole "
                    + "app.",
                "`TextContent` can hold formatted text, such as a shortcut in `<kbd>`, which is read as part of the "
                    + "description. The tooltip can't be reached with the keyboard, so it holds no links or buttons; "
                    + "use a popover for those.",
                "The arrow from `ShowArrow` is decoration and isn't read out.",
                "`Disabled`, or `Text` left blank, shows no tooltip and adds no description, so a tooltip that only "
                    + "sometimes applies needs no `@if`; the switch here turns the delete button's off. `BubbleClass` "
                    + "styles the bubble, `MaxWidth` sets how wide it grows before wrapping (18rem by default), and "
                    + "`Class` styles the anchor.",
            ],
            Target = new("tooltip", "Add an item") { OpenedBy = new("button", "Add"), OpenOnHover = true },
            Blocks = ["tb-tooltip", "tb-tooltip-anchor"],
            DataParts =
            [
                new("tooltip", "The tooltip bubble."),
                new("arrow", "The arrow that points at the trigger."),
            ],
            TestingTip = "Tab to or hover the trigger, since `FocusAsync` after a click isn't keyboard focus, then "
                + "find the tooltip with `GetByRole(AriaRole.Tooltip)`; "
                + "`ToHaveAccessibleDescriptionAsync` on the trigger checks what screen readers hear. In bUnit the "
                + "tooltip's text is in the markup from the start, with `role=\"tooltip\"`.",
        },
        new("menu", "Menu", ExampleCategory.Overlays,
            "A list of actions, options and links that opens from a button, with groups and submenus.",
            typeof(MenuExample))
        {
            RightToLeftKeys = "Left opens a submenu and Right closes it.",
            Api =
            [
                typeof(TbMenu), typeof(MenuTrigger), typeof(TbMenuItem), typeof(TbMenuCheckboxItem),
                typeof(TbMenuRadioGroup<>), typeof(TbMenuRadioItem<>), typeof(TbMenuGroup), typeof(TbMenuSeparator),
                typeof(TbSubMenu),
            ],
            Accessibility =
            [
                "The trigger is a button with `aria-haspopup=\"menu\"` and `aria-expanded`, named by `Label`; "
                    + "with an `Icon` it becomes an icon button named by the label. The list is a `menu` of "
                    + "`menuitem`s.",
                "Enter, Space or Down opens the menu on its first item; Up opens it on the last.",
                "In the menu, Up and Down move and wrap, Home and End jump to the ends, and a letter jumps to the "
                    + "next item that starts with it. Right opens a submenu and Left closes it.",
                "Escape closes the menu and returns focus to the trigger; Tab closes it and moves on. Disabled "
                    + "items stay focusable, marked `aria-disabled`, so people can still find them.",
                "`Description` adds text that screen readers read after the trigger's name, such as a hint about "
                    + "other keys it takes.",
                "A custom `Trigger` gets the same attributes through `context.Attributes`; put them on a button, and "
                    + "give it a name, such as an `aria-label` on an avatar. `Disabled` turns either trigger off.",
                "Checkbox items are `menuitemcheckbox`es and radio items `menuitemradio`s, with `aria-checked`. A "
                    + "`TbMenuGroup` or radio group is a `group` named by its `Label`, and `TbMenuSeparator` is a "
                    + "`separator`.",
                "Choosing an item closes the menu, unless it is a checkbox or radio item with `KeepOpen`, such as "
                    + "the filters here: a click, Enter or Space sets it and focus stays on it, so several can be set "
                    + "in a row. Escape, Tab or a click outside closes the menu.",
                "`@bind-Open` opens or closes the menu from code and tells you when the user does. The items are drawn "
                    + "again each time the menu opens, so one whose text or `Disabled` depends on state that changed "
                    + "since the page last rendered, such as a file that finished loading, is up to date.",
                "An item with `Href` is a link; Enter or Space follows it. With `NewTab` it opens in a new tab, and "
                    + "screen readers hear that it does.",
                "Resting the pointer on a submenu's item opens it without moving focus. On the way there the pointer "
                    + "can cross other items without the submenu closing.",
                "A menu too long for the window opens on the side with more room and scrolls, so every item stays "
                    + "reachable; a popover's panel and a date range picker's panel do the same.",
            ],
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
                + "and clicks the item.",
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
            "A window over the page, opened from code or written in markup, that asks for input or a decision.",
            typeof(DialogExample))
        {
            Parts = [typeof(RenameDialog), typeof(ExportDialog), typeof(NotesDialog)],
            Api =
            [
                typeof(IDialogService), typeof(DialogReference<>), typeof(DialogOptions), typeof(DialogSettings),
                typeof(TbDialog), typeof(TbDialogActions), typeof(TbDialogTitle), typeof(TbDialogDescription),
            ],
            Accessibility =
            [
                "A native `<dialog>` named by its title. A modal one makes the rest of the page inert, so focus "
                    + "and screen readers stay inside; a non-modal one leaves the page usable. A blank title, in "
                    + "markup or passed to `IDialogService`, gets build warning TB002.",
                "On opening, focus goes to an element marked `autofocus`, or else the first field, or the dialog "
                    + "itself with `InitialFocus = DialogFocus.Dialog`. On closing, it goes back to whatever opened "
                    + "the dialog.",
                "Escape cancels the topmost dialog, or a modeless one that has focus, unless `DismissOnEscape` is "
                    + "false; a menu, select, autocomplete list or tooltip open inside it, or a drag, takes the key "
                    + "first. A click that starts and ends on the backdrop cancels a modal unless `DismissOnBackdrop` "
                    + "is false.",
                "`ConfirmAsync` and `AlertAsync` open an `alertdialog` described by its message. The alert's tone icon "
                    + "is decorative, so say what happened in the message.",
                "A `TbDialogDescription` describes any dialog with `aria-describedby`, so screen readers read it with "
                    + "the title as the dialog opens.",
                "Escape, the backdrop and the close button call a `TbDialogContent`'s `OnDismissAsync`, which "
                    + "cancels by default: override it to run its own cancel, as the rename dialog does to ask before "
                    + "discarding a changed name, whichever way the user leaves. On a `TbDialog` they ask "
                    + "`CanDismiss`, which can keep it open. Closing from code doesn't ask.",
                "A `TbDialogTitle` shows richer content, such as the export dialog's icon, in place of the title "
                    + "in the header. It names the dialog, so keep the title's text in it; an icon in it is hidden "
                    + "from screen readers.",
                "`Size` sets the widest a dialog grows, in rem so it follows the user's text size: `Small` 24rem "
                    + "(384px), `Medium` 32rem (512px), `Large` 48rem (768px), `ExtraLarge` 64rem (1024px) and "
                    + "`Full`. Pick the step nearest the content's need, or set `Width` to a CSS length for one "
                    + "between the steps; no dialog grows past the window.",
                "A dialog opens in the browser's top layer, above everything on the page, and a modal makes the page "
                    + "inert. A popup another library adds to the page's body, such as an editor's menus or a date "
                    + "picker, then opens beneath a modal and can't be used. Set `TopLayer = false` on the dialog, or "
                    + "`DialogSettings.TopLayer` for the app, to keep it on the page: it sits at "
                    + "`--tb-dialog-z-index` (1100), draws its own backdrop, stops the page scrolling, is marked "
                    + "`aria-modal` and makes the rest of the page inert itself, so popups added later open above it "
                    + "and work, and keys pressed in them stay there. A library that makes its popup's element "
                    + "before the dialog opens, such as a date picker's calendar made as the page loads, needs its "
                    + "selector in `DialogSettings.OutsidePopups`; Blazor's reconnect dialog and error bar always "
                    + "stay usable. The trade-offs: a popup with a lower z-index still opens beneath it, and a parent "
                    + "with a transform or a stacking context of its own can hold it beneath the page. A dialog "
                    + "opened from a modal in the top layer opens there too.",
                "A dialog fades in and out with the motion tokens; reduced motion opens and closes it at once. As it "
                    + "starts to fade out, the page takes input again and focus goes back to its opener, unless focus "
                    + "had already left the dialog.",
                "A dialog that isn't modal can be `Draggable` by its header and `Resizable` by its bottom-end grip. "
                    + "Neither needs dragging: the header gains a Move button whose menu picks a place, and the grip "
                    + "is a Resize button whose menu picks a size. While either button has focus, the arrow keys "
                    + "move or resize the dialog instead of opening the menu, and its description says so.",
                "Toasts and announcements made while a modal is open appear inside it, so the inert page doesn't "
                    + "hide them.",
                "An open dialog can change: set `Modal`, `Size`, `Position` or `WindowState` on its reference, or on "
                    + "itself from a `TbDialogContent`. Becoming non-modal keeps focus where it was; becoming modal "
                    + "moves focus into the dialog unless it is already there, and makes the page inert again.",
                "A `Minimizable` modal leaves the page usable while it is minimized, with its toasts and "
                    + "announcements back on the page, and blocks it again once restored; focus stays on the button "
                    + "that restored it.",
            ],
            Target = new("dialog", "Rename report") { OpenedBy = new("button", "Rename report") },
            Blocks = ["tb-dialog"],
            DataParts =
            [
                new("dialog", "The dialog window."),
                new("header", "The title bar, with the title and the window buttons."),
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
                    <TbButton>…</TbButton>
                </TbDialogActions>

                @* A dialog written in the page's markup *@
                <TbDialog Title="…" @bind-Open="_open">
                    <TbDialogTitle>…</TbDialogTitle>
                    …
                    <TbDialogActions>…</TbDialogActions>
                </TbDialog>
                """,
            TestingTip = "In bUnit, `this.ShowDialogAsync<TDialog, TResult>(title)` from `Trailblazor.Testing` hosts "
                + "and opens a dialog in one call. "
                + "`IDialogService.OpenDialogs` lists the open dialogs, newest last, and `CloseAllAsync()` "
                + "cancels them all, which resets a page between tests. `Show` returns a reference whose `Result` "
                + "finishes when the dialog closes, and whose `ContentType` and `Parameters` say what it shows. In "
                + "bUnit, `FakeDialogService` records the dialogs opened through it, finds one with `Last<TDialog, "
                + "TResult>()` or `LastConfirm()`, "
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
        new("focus-scope", "Focus scope", ExampleCategory.Overlays,
            "Manages focus for a custom overlay or panel: it moves focus in, can keep Tab inside, and gives focus "
                + "back when it goes.", typeof(FocusScopeExample))
        {
            Api = [typeof(TbFocusScope)],
            Accessibility =
            [
                "When it appears, focus moves to the element marked `autofocus`, or else its first control, unless "
                    + "focus is already inside. Turn this off with `AutoFocus=\"false\"`.",
                "`Trapped` keeps Tab and Shift+Tab inside and brings focus back if it leaves. When scopes nest, only "
                    + "the newest trapped one traps.",
                "When it goes, focus returns to where it was, unless the user has moved it elsewhere.",
                "It doesn't make the page behind inert or close on Escape; for a modal window, use a dialog.",
                "Its options apply when it first renders; render it conditionally, as the example does, to start "
                    + "it again.",
            ],
            Target = new("group", "Find a trail") { OpenedBy = new("button", "Find a trail") },
            Blocks = ["tb-focus-scope"],
            DataParts =
            [
                new("focus-scope", "The element that keeps focus inside it."),
            ],
        },
        new("popover", "Popover", ExampleCategory.Overlays,
            "A panel of content that opens beside a trigger, such as filters or a share form, and closes on Escape or "
                + "a click elsewhere.", typeof(PopoverExample))
        {
            Api = [typeof(TbPopover), typeof(PopoverTrigger)],
            Accessibility =
            [
                "The trigger is a button with `aria-haspopup=\"dialog\"`, `aria-controls` and `aria-expanded`. The "
                    + "panel is a non-modal `dialog`, named by its `Title`, or by the trigger without one.",
                "Enter or Space on the trigger opens the panel and moves focus into it. Escape, a click elsewhere, or "
                    + "Tab past its last control closes it, and focus goes back to the trigger if it was in the panel.",
                "With `OpenOnHover`, the panel also opens when the pointer rests on the trigger, without taking focus, "
                    + "and stays open while the pointer is over it (WCAG 1.4.13). Escape still closes it. A click on "
                    + "the trigger, or working in the panel, keeps it open after the pointer leaves, and resting on "
                    + "the trigger doesn't open it over another popover that is already open.",
                "A custom `Trigger` gets the same attributes through `context.Attributes`; put them on a button. "
                    + "`Disabled` turns either trigger off, so neither a click, the keyboard nor the pointer resting "
                    + "on it opens the panel.",
            ],
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
                + "ready.",
        },
        new("overlay", "Overlay", ExampleCategory.Overlays,
            "A scrim over the page or over one region, which blocks clicks behind it and can hold a message or a "
                + "spinner in its center.", typeof(OverlayExample))
        {
            Api = [typeof(TbOverlay)],
            Accessibility =
            [
                "The scrim is hidden from screen readers; the content in its center is read as usual.",
                "It blocks the pointer. With `Blocking`, which both examples here set, it blocks the keyboard and "
                    + "screen readers too: what it covers turns inert while it shows, so Tab can't reach controls "
                    + "behind it, and if focus was behind it, it goes back there when the overlay closes. Dialogs "
                    + "opened through `IDialogService`, toasts and announcements stay usable; content added beside it "
                    + "after it opens isn't blocked.",
                "With `CloseOnClick`, Escape closes it too, unless a menu or popover above it takes the key first.",
                "For a message that needs an answer, use a dialog, which moves focus and traps it.",
            ],
            Target = null,
            Blocks = ["tb-overlay"],
            DataParts =
            [
                new("overlay", "The overlay itself."),
                new("scrim", "The shade over the page behind the content."),
                new("content", "The overlay's content."),
            ],
            TestingTip = "The overlay's element has `data-part=\"overlay\"`; click its `data-part=\"scrim\"` to "
                + "close one with `CloseOnClick`. Before pressing Escape, wait for the overlay's `data-ready` "
                + "attribute, which appears once Escape is wired.",
        },
        new("tabs", "Tabs", ExampleCategory.Navigation,
            "Switches between panels of related content, one at a time.", typeof(TabsExample))
        {
            RightToLeftKeys = "Left moves to the next tab and Right to the previous one.",
            FullWidth = true,
            Api = [typeof(TbTabs), typeof(TbTab)],
            Accessibility =
            [
                "A `tablist` of `tab`s, each controlling a `tabpanel`. Give the list an `AriaLabel`. A tab's "
                    + "`TextContent` or `Text` is its name, so a count inside it is read as part of the name; its "
                    + "`Icon` isn't read.",
                "`Variant` marks the selected tab: `Text` (and, for now, `Outlined`) underlines it, and `Soft` and "
                    + "`Filled` make it a pill, in vertical lists too. `--tb-tabs-indicator-color` and "
                    + "`--tb-tabs-indicator-size` restyle the underline. In forced-colors mode, the selected pill "
                    + "keeps an outline in the system's highlight color.",
                "The tab list is one tab stop. Left and Right move between tabs and wrap, and Home and End jump "
                    + "to the ends. A `Vertical` list has `aria-orientation=\"vertical\"` and moves with Up and Down "
                    + "instead. With automatic activation, moving also selects; with manual activation, Enter or Space "
                    + "selects.",
                "Each panel is focusable, so keyboard users can reach content that has no controls.",
                "Only the selected panel renders, unless `KeepAlive` keeps each one once shown or "
                    + "`RenderAllPanels` renders them all from the start, hidden, so a form across the tabs, like "
                    + "the trip plan, validates and saves every field. A hidden panel's fields can't take focus, so "
                    + "name the tab that needs attention, as the trip plan does with `Text` and a `HeaderClass`; a "
                    + "class alone changes only the look, which screen readers don't announce.",
                "A tab's `OnClick` is raised each time its header is clicked, or Enter or Space is pressed on it, "
                    + "even if it was already selected. It is raised after the tab is selected, and the arrow keys "
                    + "select without raising it.",
                "In forced-colors mode, such as Windows high contrast, the selected tab keeps its underline in the "
                    + "system's highlight color.",
            ],
            Target = new("tablist", "Report sections"),
            Blocks = ["tb-tabs"],
            DataParts =
            [
                new("list", "The row of tabs."),
                new("tab", "One tab."),
                new("tab-anchor", "A hidden marker that keeps a tab's place when tabs move."),
                new("icon", "A tab's icon."),
                new("panel", "A tab's panel."),
            ],
            Anatomy = """
                <TbTabs AriaLabel="…" @bind-ActiveIndex="_index">
                    <TbTab Text="…">…</TbTab>
                    <TbTab Text="…">…</TbTab>
                </TbTabs>
                """,
            TestingTip = "Find tabs with `GetByRole(AriaRole.Tab, new() { Name = \"Summary\" })` and check "
                + "`aria-selected`; the shown panel is `GetByRole(AriaRole.Tabpanel)`. Wait for the tab list's "
                + "`data-ready` attribute before testing the arrow keys. "
                + "`await cut.Tb().Tabs().SelectAsync(\"Summary\")` from `Trailblazor.Testing` picks a tab by its "
                + "text in bUnit, and `await page.Tb().Tabs(\"Report sections\").SelectAsync(\"Summary\")` from "
                + "`Trailblazor.Testing.Playwright` does in a browser, once the list is ready.",
            Telemetry =
            [
                "A tab with a `TelemetryName`, such as `\"billing-tab\"`, counts `trailblazor.control.shown` as its "
                    + "header first renders and `trailblazor.control.used` each time the user selects it.",
            ],
        },
        new("accordion", "Accordion", ExampleCategory.Navigation,
            "Sections that open and close under their headings.", typeof(AccordionExample))
        {
            Api = [typeof(TbAccordion), typeof(TbAccordionItem)],
            Accessibility =
            [
                "Each title is a button with `aria-expanded` inside a heading, level 3 unless `HeadingLevel` says "
                    + "otherwise. Choose the level that fits the page's outline.",
                "Each panel is a `region` named by its title.",
                "Tab moves between the titles, and Enter or Space opens and closes them. Up and Down also move "
                    + "between the titles, wrapping at the ends, and Home and End go to the first and last.",
                "A closed panel is `inert`, so its content can't be reached until it opens. Opening and closing "
                    + "slide, except with reduced motion.",
                "`HeaderActions`, such as Returns' print button, sit at the end of the header beside its button, "
                    + "not in it, so they stay usable and out of the title's name. Tab reaches them after the title; "
                    + "Up and Down skip them.",
                "An item works on its own, outside an accordion, as Care instructions does. One nested in another "
                    + "item's panel, as Exchanges is, belongs only to an accordion inside that panel, or to none, "
                    + "so opening it never closes the item it sits in. An item's own `HeadingLevel` overrides the "
                    + "accordion's, so a nested one, like Exchanges at level 4, sits below the item around it in the "
                    + "page's outline.",
            ],
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
            TestingTip = "The arrow keys, Home and End work once the accordion's script has attached, which "
                + "is marked by `data-ready` on its root, `[data-part=accordion]`; wait for it before pressing them.",
        },
        new("tree-view", "Tree view", ExampleCategory.Navigation,
            "Shows nested items that open and close, with selection, checkboxes, a filter and children that load "
                + "when opened.", typeof(TreeViewExample))
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
                    + "filters them and keeps each match's parents. Only trails can be selected, and double-clicking "
                    + "a trail or pressing Enter on it opens it. `ItemEnd`, or a `TbTreeItem`'s `EndContent`, holds "
                    + "what sits at the end of a row, such as a count or a button. Clicks and keys there never select, "
                    + "open or activate the row.",
                    typeof(TreeViewExample)),
                new("multiple-selection", "Multiple selection", "`SelectionMode.Multiple` lets several rows be "
                    + "selected, and `SelectedKeys` holds their keys. A click selects one row, Ctrl or Cmd with a click "
                    + "adds or removes a row, and Shift with a click extends the selection.",
                    typeof(TreeViewMultipleExample)),
                new("checkboxes", "Checkboxes", "Rows written as markup suit a tree that is part of the page. With "
                    + "`Checkable`, checking a parent checks its children, and a parent with only some of them "
                    + "checked shows as mixed. A `TbTreeItem` with no `Value`, such as Trips, opens, closes and is "
                    + "found by typing, but can't be selected or activated, and it is never in the bound keys.",
                    typeof(TreeViewCheckboxExample)),
                new("independent-checks", "Independent checks", "`CascadeChecks=\"false\"` checks only the row itself, "
                    + "so a parent and its children each keep their own check.",
                    typeof(TreeViewIndependentChecksExample)),
                new("disabled", "Disabled rows", "`IsDisabled` turns off single rows, and `Disabled` turns off the "
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
                new("move-and-insert", "Move and insert", "With `OnDrop`, rows move by dragging with a mouse, pen or "
                    + "finger, or with Alt+Shift and the arrows. A finger holds still for a moment first, so a swipe "
                    + "still scrolls. `OnDrop` says what moved and where, and the app applies it, so it can be undone "
                    + "like any other change. `CanDrop` can refuse a move and say why. `InsertTemplate` puts content on "
                    + "each row's edge, such as the buttons here that add a row. It shows when the pointer reaches that "
                    + "edge or the content has focus, and on a touch screen for the focused row.",
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
                new("labels", "Built-in text", "`Labels` replaces the words the tree shows and reads out while "
                    + "children load, when they fail and when it has no rows.", typeof(TreeViewLabelsExample)),
            ],
            Api =
            [
                typeof(TbTreeView<,>), typeof(TbTreeItem<>), typeof(TreeViewLabels), typeof(TreeDrop<>),
                typeof(MoveCheck), typeof(TreeInsertContext<>),
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
                new(["A letter"], "Finds a row by its text.") { When = "Moving around" },
                new(["Enter"], "Selects and activates the row and, like a double-click, opens or closes it if it has "
                    + "children. On a failed row, tries again.") { When = "Selecting" },
                new(["Space"], "Selects the row, or checks it in a `Checkable` tree. In a `Multiple` tree, selects or "
                    + "unselects it.") { When = "Selecting" },
                new(["Shift+Up", "Shift+Down", "Shift+Space"], "Extends a `Multiple` selection.")
                    { When = "Selecting" },
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
                "The reason `CanDrop` gives for refusing a place is read out when the move is tried.",
            ],
            Accessibility =
            [
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
            BeforeInteractive =
            [
                "A `TbTreeItem` renders a plain row, open to `InitialExpandDepth`.",
                "With `Virtualize`, every row renders.",
            ],
            Notes =
            [
                new("After changing `Items` in place, call `RefreshAsync`. A new collection, or an `OnDrop`, builds the "
                    + "rows again by itself.") { Pitfall = true },
                new("Rows written as nested `TbTreeItem`s work from any component inside the tree. Each draws its row "
                    + "where it is written, followed by a `group` holding what is written inside it, so other content "
                    + "between rows shows in place and hides with its parent."),
                new("A tree inside another tree's row is a tree of its own: its rows take their own keys, focus, clicks "
                    + "and drags, the outer tree's arrows pass over them, and the outer row isn't selected by them."),
                new("With cascading checks, a `TbTreeItem` with no `Value` checks the rows inside it. Such a row is "
                    + "never in the bound keys, so several of them can share a text without needing keys."),
                new("`ExpandAllAsync` opens rows with `HasChildren` too, and the rows that appear in them."),
                new("A row's `data-key` is its key's text for a string, number or date key. Any other key gets a "
                    + "made-up id such as `key-3`, and so does a key whose text is empty or that another key already "
                    + "has."),
            ],
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
                "Without the helpers, wait for the tree's `data-ready` before pressing keys or dragging. A tree of "
                    + "`TbTreeItem`s also sets `aria-busy` while items it has just been given become rows.",
                "In bUnit, `cut.Tb().Tree(\"Trails by region\")` from `Trailblazor.Testing` has the same `ExpandAsync`, "
                    + "`ExpandAllAsync`, `SelectAsync` and `CheckAsync`, with `Texts` for the rows that show and `Item` "
                    + "and `Items` to find rows.",
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
            "Shows items as rows of a table, with sortable and filterable columns, paging and row selection.",
            typeof(DataGridExample))
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
                    + "with a line such as \"1–5 of 7\" and a `TbPagination`, and `PageSizes` adds a list of page "
                    + "sizes. `Multiple` selection gives each row a checkbox and adds one to the header that selects "
                    + "every row on the page. `Single` uses radio buttons. A column's `Cell` template can hold buttons.",
                    typeof(DataGridExample)),
                new("row-click", "Clicking a row", "With a `SelectionMode` set, a click on a row selects it, unless the "
                    + "click lands on a button, link or field inside the row. `RowClickSelects=\"false\"` leaves "
                    + "selecting to the checkboxes, and `OnRowClick` reports the click.",
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
                    + "and each starts with a heading that shows its row count and opens or closes the group.",
                    typeof(DataGridGroupExample)),
                new("pinned-columns", "Pinned columns", "A column's `Pinned` keeps it in view at the start or end "
                    + "of the row while the rows scroll sideways. A column pinned at the start keeps the selection "
                    + "column with it.", typeof(DataGridPinnedExample)),
                new("height", "Fixed height and scrolling to a row", "`Height` gives the rows a box of their own to "
                    + "scroll in, under headers that stay in view. Without cell navigation, Tab reaches the box so the "
                    + "arrow keys can scroll it. `ScrollToAsync` goes to the page that holds a row and scrolls to it.",
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
                    + "or press Enter or F2 on it, then Enter saves, Escape cancels, and leaving the field saves too. "
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
                    + "header checkbox's name and the page size list's label.", typeof(DataGridLabelsExample)),
            ],
            Api =
            [
                typeof(TbDataGrid<>), typeof(TbColumn<>), typeof(DataGridLabels), typeof(GridRequest),
                typeof(GridResult<>), typeof(GridSort), typeof(GridFilter), typeof(FilterOperator),
                typeof(GridFilterMode), typeof(GridFilterContext), typeof(ColumnPin), typeof(GridCellEdit<>),
                typeof(GridCellEditContext<>), typeof(GridEditMode), typeof(GridRowEdit<>),
                typeof(GridState),
                typeof(SortableMove<>), typeof(MoveCheck),
            ],
            Keyboard =
            [
                new(["Tab"], "Reaches every control in the grid, each a native button, link or input. With a "
                    + "`Height`, it also reaches the box the rows scroll in.") { When = "Without cell navigation" },
                new(["Up", "Down"], "With a `Height` and focus on that box, scrolls the rows.")
                    { When = "Without cell navigation" },
                new(["Enter", "Space"], "On a sortable header, sorts by it alone, then reverses it, then stops sorting "
                    + "by it.") { When = "Sorting" },
                new(["Shift+Enter"], "On a sortable header, adds it to the sorts.") { When = "Sorting" },
                new(["Space"], "On a row's checkbox, selects or unselects the row. On its radio button, selects "
                    + "the row.") { When = "Selecting" },
                new(["Tab"], "Reaches the grid once.") { When = "With cell navigation" },
                new(["Up", "Down", "Left", "Right"], "Moves between cells.") { When = "With cell navigation" },
                new(["Home", "End"], "Goes to the ends of the row.") { When = "With cell navigation" },
                new(["Ctrl+Home", "Ctrl+End"], "Goes to the first or last cell.") { When = "With cell navigation" },
                new(["Page Up", "Page Down"], "Moves by a screenful, scrolling a virtualized grid to rows not yet "
                    + "drawn.") { When = "With cell navigation" },
                new(["Space"], "Selects or unselects the row of the cell in focus.") { When = "With cell navigation" },
                new(["Enter", "F2"], "Moves into the cell's fields, or starts editing an `Editable` cell.")
                    { When = "With cell navigation" },
                new(["Tab"], "Cycles through the cell's fields.") { When = "Inside a cell" },
                new(["Escape"], "Comes back out to the cell.") { When = "Inside a cell" },
                new(["Enter"], "Saves the edit, and focus goes back to the cell.") { When = "Editing a cell" },
                new(["Escape"], "Cancels the edit, and focus goes back to the cell.") { When = "Editing a cell" },
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
                "With a `Height` and no cell navigation, the box the rows scroll in is a `region` with the same name.",
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
                "The first sort sets `aria-sort` on its header. With several sorts, each sorted header's button also "
                    + "says its place, such as \"sort 2 of 3, ascending\".",
                "The header checkbox shows \"mixed\" when only some rows are selected.",
                "A group's button reports its state with `aria-expanded`.",
                "A pinned column moves to its side of the table in the page's order too, so screen readers and Tab "
                    + "reach its cells in the order they show.",
                "With `Virtualize`, the table's `aria-rowcount` holds the number of rows, headers included, and "
                    + "each drawn row's `aria-rowindex` its place, so screen readers can say where a row is among "
                    + "rows that aren't drawn.",
                "While rows load the table is `aria-busy`.",
                "With `CellNavigation`, or an `Editable` column, the table takes the ARIA grid role, and rows say "
                    + "whether they are selected with `aria-selected`.",
                "A cell holding just a menu's button or a row's move handle keeps focus until Enter, since those "
                    + "controls use the arrow keys.",
            ],
            AppDuties =
            [
                "Only a pointer clicks a row, so whatever `OnRowClick` does needs a button in a cell too, as "
                    + "[Clicking a row](components/data-grid#row-click) shows.",
                "Set `RowLabel` when a row's first column doesn't say which row it is, so its checkbox and move handle "
                    + "can.",
            ],
            BeforeInteractive =
            [
                "With `Items` and no `Virtualize`, it renders its first page of rows as a plain table, so they can be "
                    + "read at once. Sorting, filtering, paging and selecting start working once the page is "
                    + "interactive.",
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
            Target = new("table", "Trails"),
            Blocks = ["tb-data-grid"],
            DataParts =
            [
                new("grid", "The grid itself."),
                new("scroll", "The box that scrolls the rows."),
                new("sort", "A column header's sort button."),
                new("sort-order", "The number that shows a column's place when sorting by several."),
                new("filters", "The row of filter fields under the headers."),
                new("filter", "One column's cell in the row of filter fields."),
                new("filter-menu", "A column's filter menu: its button in the header and the popup of conditions."),
                new("filter-apply", "The filter menu's Apply button."),
                new("filter-clear", "The filter menu's Clear button."),
                new("group-by", "The bar that chooses the columns to group by."),
                new("group", "A group's heading row."),
                new("group-toggle", "The button that opens and closes a group."),
                new("select-all", "The checkbox that selects every row on the page."),
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
                "In a filter menu, `FilterAsync(\"Sites\", \"At least\", \"16\")` picks a condition too, and "
                    + "`FilterChoicesAsync(\"Region\", \"North ridge\")` leaves exactly those values picked, in a "
                    + "menu's checkboxes or the filter row's multiple select.",
                "In the filter row, a choice filter is a select named for its column: "
                    + "`page.Tb().Select(\"Filter Difficulty\").ChooseAsync(\"Easy\")`.",
                "`page.Tb().Select(\"Group by\").ChooseAsync(\"Region\")` groups the rows. Each group's heading is a "
                    + "`[data-part=group]` row whose `[data-part=group-toggle]` button opens or closes it.",
                "Without the helpers, wait for the table's `aria-busy` to clear, and for `data-ready` on "
                    + "`[data-part=grid]` before clicking a row.",
                "Rows carry `data-key` and `data-selected`, a filtered column's header `data-filtered`, and cells "
                    + "`data-column`, the field's path such as "
                    + "`Shipping.City` unless the column has an `Id`. A pinned column's header and cells also carry "
                    + "`data-pinned`, `start` or `end`, and a filter cell's `data-value` holds its applied values as "
                    + "invariant text, with a range's two dates separated by a space.",
                "In bUnit, `cut.Tb().Grid(\"Trails\")` from `Trailblazor.Testing` has the same `Row`, `SelectAsync` and "
                    + "`FilterAsync`.",
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
                    + "`page_size`, `select`, `move`, `group`, `expand`, `collapse` or `edit` for each of those the "
                    + "user does; a move also says whether a "
                    + "`pointer` or the `keyboard` made it.",
                "A page `ScrollToAsync` moves to, or a group it opens, isn't counted, and which column, filter "
                    + "value, page, group or rows were used, or what value an edit saved, is never recorded.",
            ],
        },
        new("table", "Table", ExampleCategory.Content,
            "A plain table written as markup, with the data grid's look.", typeof(TableExample))
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
            ],
            AppDuties =
            [
                "Name the table, and give each header a `scope`.",
                "If the table scrolls in an area of its own, give that area `tabindex=\"0\"`, `role=\"region\"` and a "
                    + "name, so keyboard users can reach and scroll it.",
            ],
            BeforeInteractive =
            [
                "It is plain HTML, so it all renders and reads the same before the page is interactive.",
            ],
            Target = new("table", "Opening hours"),
            Blocks = ["tb-table"],
            TestingTip = "Find it by role and name, `GetByRole(AriaRole.Table, new() { Name = \"Opening hours\" })`, "
                + "and its cells with `GetByRole(AriaRole.Cell, ...)`.",
        },
        new("pagination", "Pagination", ExampleCategory.Navigation,
            "Moves between the pages of a long list, with numbered page buttons or, in compact mode, a box for the "
                + "page number.", typeof(PaginationExample))
        {
            Api = [typeof(TbPagination), typeof(PaginationLabels)],
            Accessibility =
            [
                "A `navigation` landmark named by `AriaLabel` (\"Pagination\" by default); name each one when a page "
                    + "has several.",
                "Each page is a button named \"Page 3\", and the current one has `aria-current=\"page\"`. The "
                    + "Previous, Next, First and Last buttons are named, and turn off at either end but stay "
                    + "focusable, so focus stays on Next after it reaches the last page. Focus stays on a chosen page "
                    + "as the pages around it shift. The ellipses are hidden from screen readers.",
                "Every control is its own tab stop, as in a list of links. In compact mode the page box is named "
                    + "\"Page\"; typing a page and pressing Enter, or leaving the box, goes there.",
                "With `PageHref` every item is a link, so the pages work before the page is interactive. All the "
                    + "words come from `Labels`.",
            ],
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
            "Links to the sections of a page that mark the section in view as the page scrolls.",
            typeof(TableOfContentsExample))
        {
            Api = [typeof(TbTableOfContents), typeof(TbTableOfContentsItem)],
            Anatomy = """
                <TbTableOfContents AriaLabel="…">
                    <TbTableOfContentsItem TargetId="…" Text="…">
                        <TbTableOfContentsItem TargetId="…" Text="…" />
                    </TbTableOfContentsItem>
                </TbTableOfContents>
                """,
            Accessibility =
            [
                "A `navigation` landmark named by `AriaLabel` (\"On this page\" by default) holding a list of links, "
                    + "with a nested list under an item for the sections inside it.",
                "The link to the section in view has `aria-current=\"location\"`, and shows it with a wider line "
                    + "beside it, not color alone. A section is in view once its start passes the top quarter of what "
                    + "scrolls it, the page or a scrolling panel. At the top of the scroll the first section is in "
                    + "view, and at the bottom the last section that shows is in view.",
                "Following a link scrolls smoothly to the section, or jumps there when the user has asked for reduced "
                    + "motion, and moves focus to the section, so Tab goes on from there.",
                "Each link is its own tab stop. The links are plain addresses with the section's id, so they work "
                    + "before the page is interactive, and open in a new tab with Ctrl or a middle click.",
            ],
            Target = new("navigation", "Trail guide"),
            Blocks = ["tb-table-of-contents"],
            DataParts =
            [
                new("link", "A link to one section."),
            ],
            TestingTip = "Scroll a section into view and check its link with `ToHaveAttributeAsync(\"aria-current\", "
                + "\"location\")`. Wait first for `data-ready` on the `navigation`, which marks that its script is "
                + "tracking the scroll.",
            Telemetry =
            [
                "With a `TelemetryName`, such as `\"guide-contents\"`, it counts `trailblazor.control.shown` as it "
                    + "first renders and `trailblazor.control.used` for each link followed. Which link was followed "
                    + "is never recorded.",
            ],
        },
        new("link", "Link", ExampleCategory.Navigation,
            "An underlined text link to an address, or a link-styled button that runs an action.", typeof(LinkExample))
        {
            Api = [typeof(TbLink)],
            Accessibility =
            [
                "With `Href` it is a native `link`, followed with Enter. Without `Href` it is a `button` that looks "
                    + "like a link, run with Enter or Space, because an anchor with no address can't be focused.",
                "It is always underlined, so it doesn't rely on color alone to stand out from the text around it.",
                "`NewTab` adds `target=\"_blank\"` and `rel=\"noopener noreferrer\"`, and screen readers hear \"opens "
                    + "in a new tab\" after its text. `NewTabHint` changes those words.",
                "`Disabled` keeps the text but drops the address, so the link can't be followed or focused, and "
                    + "marks it `aria-disabled` with the `link` role, so screen readers still read it as a disabled "
                    + "link. Without `Href` the button is `disabled`. Both lose their underline and turn gray.",
                "In forced-colors mode, such as Windows high contrast, both kinds take the system's link color, and a "
                    + "disabled one the system's gray.",
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
        new("skeleton", "Skeleton", ExampleCategory.Feedback,
            "A gray shape that holds the place of content while it loads, with a pulse or a wave.",
            typeof(SkeletonExample))
        {
            Api = [typeof(TbSkeleton)],
            Accessibility =
            [
                "A skeleton is hidden from screen readers, because it has nothing to say. Mark the region that is "
                    + "loading with `aria-busy=\"true\"`, and set it back to `false` when the content arrives.",
                "Wrapped around content, it does that itself: the content is hidden from sight, screen readers and "
                    + "the keyboard, and the wrapper has `aria-busy=\"true\"`, until `Loading` turns off.",
                "With reduced motion, the pulse and the wave stop and the shapes stay still.",
                "In forced-colors mode, such as Windows high contrast, each shape keeps an outline.",
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
            "A brief message in a corner that doesn't interrupt, with an optional title, icon, content and action.",
            typeof(ToastExample))
        {
            Api = [typeof(IToastService), typeof(ToastOptions), typeof(ToastSettings)],
            Accessibility =
            [
                "Toasts are read out as they appear: Danger and Warning interrupt, the others wait. They never "
                    + "take focus themselves.",
                "F8 moves focus to the newest toast's first button, and Escape sends it back to where it was.",
                "A toast's timer pauses while the pointer is over it or it has focus. Danger toasts and toasts "
                    + "with an `Action` stay until they are dismissed unless given a `Duration`, so the action can't "
                    + "time out unseen.",
                "A toast's `Action` is read out after its message, as in \"Undo available, press F8.\" Offer the "
                    + "same action somewhere else in the app too, since a toast can be dismissed before it is used.",
                "The toasts sit in a region named \"Notifications\".",
                "A `Title` is read out before the message. The tone's icon is decorative, since the text says the "
                    + "same; `Content` shows in place of the message, which is still what screen readers hear.",
                "Toasts fade in and out; with reduced motion they appear and go at once.",
            ],
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
