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

public sealed record Example(string Name, string Title, ExampleCategory Category, string Summary, Type Component,
    params Type[] Parts)
{
    // The types whose parameters or options the example's page tables.
    public required IReadOnlyList<Type> Api { get; init; }

    // Keyboard, roles and the names a developer must give; text in backticks shows as code.
    public required IReadOnlyList<string> Accessibility { get; init; }

    // How a test finds the component in the example; null for a layout component with no role of its own.
    public required TestTarget? Target { get; init; }

    // The CSS blocks the component is styled under, such as tb-button, which its design tokens are read from.
    public required IReadOnlyList<string> Blocks { get; init; }

    // Extra testing advice for the page; text in backticks shows as code.
    public string? TestingTip { get; init; }

    // What the component reports through the Trailblazor meter; text in backticks shows as code.
    public IReadOnlyList<string> Telemetry { get; init; } = [];

    // Every field takes a TelemetryName, so a field's page says what that counts before its own notes.
    public IReadOnlyList<string> TelemetryNotes => Api.Any(IsField) ? [FieldTelemetry, .. Telemetry] : Telemetry;

    // Every field takes AriaLabel, OnFocus, OnBlur and AutoFocus, so a field's page ends its notes with them.
    public IReadOnlyList<string> AccessibilityNotes => Api.Any(IsField) ? [.. Accessibility, FieldAccessibility]
        : Accessibility;

    private const string FieldAccessibility = "With no room for a label, an `AriaLabel` names the field through a "
        + "label only screen readers see. `OnFocus` and `OnBlur` report focus moving into the field and out of it, not "
        + "between its own parts, so they suit checking or saving when the user moves on. `AutoFocus` moves focus to "
        + "it as it appears on an interactive page, unless the user is typing in another field by then, or, in a "
        + "dialog or focus scope, as that opens.";

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

    // The data-part names a test can find inside the component, in the order they appear.
    public IReadOnlyList<string> DataParts { get; init; } = [];

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
    public static ExampleSource Of(Type component)
    {
        var file = $"{component.Name}.razor";
        using var stream = typeof(ExampleSource).Assembly.GetManifestResourceStream($"Source/{file}")
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
    private static readonly string[] FieldParts = ["label", "control", "start", "end", "helper", "error"];

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
            TestingTip = "Text cut off by `MaxLines` still holds all of it, so text assertions and `GetByText` match "
                + "the whole string. Whether it is cut off shows only in a browser, not in bUnit.",
        },
        new("image", "Image", ExampleCategory.Content,
            "An image with required alternative text, that can scale to fit its container.", typeof(ImageExample))
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
            "A line that separates content, across or down.", typeof(DividerExample))
        {
            Api = [typeof(TbDivider)],
            Accessibility =
            [
                "A horizontal divider is an `<hr>`, which screen readers treat as a separator. A vertical one is "
                    + "a `div` with `role=\"separator\"` and `aria-orientation=\"vertical\"`.",
                "It takes no focus and has no name.",
                "A divider with content, such as \"or\", is a plain `div`: a separator's children aren't read aloud, "
                    + "so the content is read as text and the lines are only drawn.",
            ],
            Target = null,
            Blocks = ["tb-divider"],
            DataParts = ["content"],
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
                    + "themes. Color alone doesn't carry the meaning, so pair it with a label or text.",
            ],
            Target = new("img", "Failed"),
            Blocks = ["tb-icon"],
        },
        new("button", "Button", ExampleCategory.Actions,
            "Starts an action, in filled, outlined or text styles and every tone.", typeof(ButtonExample))
        {
            Api = [typeof(TbButton), typeof(ButtonClasses)],
            Accessibility =
            [
                "A native `<button>`, so Enter and Space press it. It defaults to `type=\"button\"`, so it never "
                    + "submits a form by accident.",
                "Its text is its name; a decorative icon, from `StartIcon`, `EndIcon` or the `Start` and `End` "
                    + "slots, adds nothing to it.",
                "`Disabled` takes it out of the tab order. With `FocusableWhenDisabled` it stays a tab stop, marked "
                    + "`aria-disabled`, so its tooltip can say why it is off. `Loading` keeps it focusable too, marks "
                    + "it `aria-disabled` and `aria-busy`, and ignores clicks.",
                "With `Href` it is a `link` drawn as a button, so it can open in a new tab and is announced as a link. "
                    + "A disabled one drops its address.",
                "With `@bind-Pressed` it is a toggle button with `aria-pressed`. Keep its text the same when it is "
                    + "pressed, or change the text instead of using a toggle, so the state isn't announced twice.",
                "With reduced motion, the busy spinner stops turning. In forced-colors mode a pressed button takes "
                    + "the system highlight.",
            ],
            Target = new("button", "Add item"),
            Blocks = ["tb-button"],
            DataParts = ["start", "icon", "label", "spinner", "end"],
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
                "`AriaLabel` is required and names the button, since it shows no text; a blank label throws when "
                    + "it renders.",
                "The icon itself is hidden from screen readers. Set `ShowTooltip` so sighted users can see the "
                    + "name too, on hover and on focus. The tooltip repeats the name, so it isn't added as a "
                    + "description, and screen readers hear the name once.",
                "Keys, `Disabled`, `FocusableWhenDisabled`, `Loading`, `Href` and `@bind-Pressed` work as they do "
                    + "for a button. A toggle keeps the same `AriaLabel` either way; `aria-pressed` tells whether it "
                    + "is on.",
            ],
            Target = new("button", "Add"),
            Blocks = ["tb-button", "tb-icon-button"],
            DataParts = ["icon", "spinner"],
            TestingTip = "Find it by its name: `GetByRole(AriaRole.Button, new() { Name = \"Delete\" })`. With "
                + "`ShowTooltip`, focusing it shows a `tooltip` role with the same text.",
            Telemetry = ButtonTelemetry("TbIconButton"),
        },
        new("button-group", "Button group", ExampleCategory.Actions,
            "Joins related buttons into one control, in a row or a column, with settings shared by all of them.",
            typeof(ButtonGroupExample))
        {
            Api = [typeof(TbButtonGroup)],
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
            Api = [typeof(TbToggleGroup<>), typeof(TbMultiToggleGroup<>), typeof(TbToggleItem<>)],
            Accessibility =
            [
                "A `group` named by its required `AriaLabel`, holding toggle buttons with `aria-pressed`. An item "
                    + "with only an icon needs an `AriaLabel` of its own.",
                "The group is one tab stop, landing on the pressed item, or the first one. Left and Right move "
                    + "between the items (Up and Down when `Vertical`), Home and End go to the first and last, and "
                    + "disabled items are skipped. Space or Enter presses the focused item.",
                "In a single group, pressing the pressed item keeps it pressed unless the group is `Clearable`.",
                "In forced-colors mode a pressed item takes the system highlight.",
            ],
            Target = new("group", "Map view"),
            Blocks = ["tb-toggle-group", "tb-button"],
            DataParts = ["item"],
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
            Api = [typeof(TbToolbar), typeof(TbToolbarButton)],
            Accessibility =
            [
                "A `toolbar` named by its required `AriaLabel`, and one tab stop. Left and Right (Up and Down when "
                    + "`Vertical`) move between every control in it, toggle group items included; Home and End go to "
                    + "the first and last. A text field keeps its arrow keys.",
                "A `TbToolbarButton` with an `Icon` is an icon button named by its `Text`, which its tooltip shows "
                    + "too. A tooltip that repeats a button's name isn't read a second time as its description.",
                "Toolbar buttons with no room move into the menu button named by `OverflowLabel` (\"More\"), where "
                    + "each is a `menuitem` with the same text and action. Other content never moves.",
            ],
            Target = new("toolbar", "Trail notes"),
            Blocks = ["tb-toolbar"],
            DataParts = ["overflow"],
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
            DataParts = ["insert"],
            TestingTip = "Click the insert button by its name, as `GetByRole(AriaRole.Button, new() { Name = "
                + "\"Add a step after Pack the tent\" })`; Playwright hovers it first, which shows it.",
        },
        new("stack", "Stack", ExampleCategory.Layout,
            "Lays its children out in a row or a column, evenly spaced.", typeof(StackExample))
        {
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
            Api = [typeof(TbGrid), typeof(TbGridItem)],
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
            "Lays out panes side by side or stacked, with separators that resize them by dragging or with the "
                + "keyboard.", typeof(SplitterExample))
        {
            Api = [typeof(TbSplitter), typeof(TbSplitterPane)],
            Accessibility =
            [
                "Each separator is a focusable `separator` named \"Resize\" and the `Label` of the pane before it, "
                    + "with `aria-valuenow` giving that pane's size in percent and `aria-controls` pointing to it.",
                "On a separator, the arrows resize by 1% (10% with Shift), Home and End take the pane before it to "
                    + "its smallest and largest, and Enter collapses or restores a `Collapsible` pane.",
                "A collapsed pane keeps its content but hides it from sight and from the tab order.",
                "Dragging has the keyboard as its alternative (WCAG 2.5.7), and works right to left.",
            ],
            Target = new("separator", "Resize Trails"),
            Blocks = ["tb-splitter"],
            DataParts = ["splitter", "pane", "separator"],
            TestingTip = "Resize with the keys, as `var separator = page.GetByRole(AriaRole.Separator, new() { Name "
                + "= \"Resize Trails\" }); await separator.FocusAsync(); await page.Keyboard.PressAsync(\"End\");`, "
                + "then check `ToHaveAttributeAsync(\"aria-valuenow\", \"40\")`. Wait first for `data-ready` "
                + "on the splitter's root, `[data-part=splitter]`.",
        },
        new("container", "Container", ExampleCategory.Layout,
            "Centers content and caps its width at a readable size.", typeof(ContainerExample))
        {
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
            DataParts = ["icon", "title", "message", "actions", "close"],
        },
        new("chip", "Chip", ExampleCategory.Content,
            "A compact label for a tag, a filter or a choice, which can be clicked, toggled or removed.",
            typeof(ChipExample))
        {
            Api = [typeof(TbChip), typeof(TbChipGroup)],
            Accessibility =
            [
                "A `TbChipGroup` is a `list` named by its required `AriaLabel`, and each chip in it is a `listitem`.",
                "A chip with `OnClick` is a `button`, with `Href` a `link`, and with `@bind-Selected` a toggle button "
                    + "with `aria-pressed`. Other chips are plain text.",
                "A removable chip has a remove button named \"Remove\" and the chip's `Text`; Delete or Backspace on "
                    + "the chip removes it too. In a group, focus then moves to the chip that takes its place, or "
                    + "to the group when none is left.",
                "In forced-colors mode, such as Windows high contrast, every chip keeps an outline and a selected "
                    + "one is outlined in the system's highlight color.",
            ],
            Target = new("list", "Selected tags"),
            Blocks = ["tb-chip", "tb-chip-group"],
            DataParts = ["check", "icon", "label", "action", "remove"],
            TestingTip = "Remove a chip with `GetByRole(AriaRole.Button, new() { Name = \"Remove Archive\" })`, and "
                + "check a filter chip with `ToHaveAttributeAsync(\"aria-pressed\", \"true\")`.",
            Telemetry =
            [
                "With a `TelemetryName`, such as `\"filter-open\"`, it counts `trailblazor.control.shown` as it first "
                    + "renders, `trailblazor.control.used` for each click or toggle, and `trailblazor.control.used` "
                    + "with `trailblazor.action` `remove` when it is removed. Leave it off chips that only show "
                    + "something.",
            ],
        },
        new("list", "List", ExampleCategory.Content,
            "A list of items, each with optional leading content, a description and trailing content. Items can be "
                + "links or buttons, and one can be marked as the current item.", typeof(ListExample))
        {
            Api = [typeof(TbList), typeof(TbListItem)],
            Accessibility =
            [
                "A `list` of `listitem`s; name it with `aria-label` when the page has several lists.",
                "An item with `Href` is a `link` and one with `OnClick` a `button`, each a stop in the tab order. "
                    + "It is named by its text and then its `Description`, so start the text with what the item is.",
                "The `End` content sits outside the link or button, so its own buttons are tab stops of their own, "
                    + "reached right after their item.",
                "`Selected` sets `aria-current`: `page` on a link and `true` on a button or plain item.",
            ],
            Target = new("list", "Campsites"),
            Blocks = ["tb-list"],
            DataParts = ["start", "text", "description", "content", "end", "action"],
            TestingTip = "Find an item by the start of its name, as `GetByRole(AriaRole.Button, new() { NameRegex = "
                + "new Regex(\"^Pine Hollow\") })`, and check the current one with "
                + "`ToHaveAttributeAsync(\"aria-current\", \"true\")`.",
        },
        new("sortable", "Sortable", ExampleCategory.Content,
            "A list the user puts in order, by dragging an item's handle or with the arrow keys, such as rules whose "
                + "order sets which runs first.", typeof(SortableExample))
        {
            Api = [typeof(TbSortable<>), typeof(SortableMove<>), typeof(MoveCheck)],
            Accessibility =
            [
                "A `list` named by its `Label`. Each item has a handle, a menu `button` named \"Move\" and the item's "
                    + "text, described by how its keys work.",
                "On a handle, the up and down arrows move the item one place, and Home and End move it to the start "
                    + "or end; focus stays on the handle as the item moves. Enter, Space or a click opens its menu: "
                    + "Move up, Move down, Move to the start and Move to the end. The menu is the one-click way that "
                    + "replaces dragging (WCAG 2.5.7), and it works where a screen reader keeps the arrow keys for "
                    + "itself.",
                "Each move is announced with the item's new position, such as \"Storm warning moved to position 1 of "
                    + "4.\", and a move `CanMove` refuses is announced with its reason, which also shows beside the "
                    + "drop.",
                "A drop lands before or after the item under the pointer, shown by a line; a place it can't go shows "
                    + "dashed, so it doesn't rely on color.",
                "`MoveAsync(from, to)` moves an item from the app's own buttons, such as \"Raise\" here, with the same "
                    + "check and announcement.",
            ],
            Target = new("list", "Trail alerts, first match wins"),
            Blocks = ["tb-sortable"],
            DataParts = ["list", "item", "handle", "drop-reason"],
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
                    + "count of errors that reads well at zero.",
                "In forced-colors mode, such as Windows high contrast, the badge keeps an outline and a dot keeps its "
                    + "fill.",
            ],
            Target = null,
            Blocks = ["tb-badge"],
            DataParts = ["badge"],
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
                "`AriaLabel` is required and names it; a blank label throws when it renders.",
                "`Label` and `ShowValue` add visible text. With `ValueFormatter`, screen readers announce that "
                    + "text, such as \"3 of 10 files\", in place of the number. Show a ring's value at `Size.Large`, "
                    + "where it is big enough to read.",
                "With reduced motion, the moving bar and the spinning ring pulse instead.",
                "In forced-colors mode, such as Windows high contrast, the fill takes the system's highlight color.",
            ],
            Target = new("progressbar", "Upload"),
            Blocks = ["tb-progress"],
            DataParts = ["bar", "ring", "indicator", "value", "label"],
            TestingTip = "Find it with `GetByRole(AriaRole.Progressbar, new() { Name = \"Upload\" })` and check "
                + "`aria-valuenow`, or `aria-valuetext` when it has a `ValueFormatter`. The shown text is in "
                + "`[data-part=label]` and `[data-part=value]`.",
        },
        new("label", "Label", ExampleCategory.Inputs,
            "A label for a control or a group of controls, with an optional info button that explains the setting.",
            typeof(LabelExample))
        {
            Api = [typeof(TbLabel)],
            Accessibility =
            [
                "With `For` it is a `<label>` tied to that control. Without it, it is plain text: give a group of "
                    + "controls `aria-labelledby` pointing at its `TextId`, which leaves out the info button.",
                "The info button is a toggletip: a button named \"Information about\" and the label's `Text`, which "
                    + "opens a small panel on click, Enter, Space or hover, and closes on Escape.",
                "The `Required` marker is only visual; mark the control itself as required too.",
            ],
            Target = new("button", "Information about Project code"),
            Blocks = ["tb-label"],
            DataParts = ["text", "info"],
            TestingTip = "Open the info with "
                + "`await page.Tb().Popover(\"Information about Project code\").OpenAsync()`, which returns its panel. "
                + "The info button's name contains the label, so find the control by role with an exact name, as "
                + "`GetByRole(AriaRole.Textbox, new() { Name = \"Project code\", Exact = true })`.",
        },
        new("form", "Form", ExampleCategory.Inputs,
            "A form for Trailblazor fields that reports whether it is valid as the fields change and moves focus to "
                + "the first error when a submit fails.", typeof(FormExample))
        {
            Api = [typeof(TbForm)],
            Accessibility =
            [
                "A `form` element (built on `EditForm`); give it an `aria-label` to make it a landmark, as here.",
                "When a submit fails, every field shows its messages, which are linked to it with `aria-describedby` "
                    + "and announced, and focus moves to the first field with `aria-invalid`, or into a radio or "
                    + "checkbox group where its `FocusAsync()` would.",
                "`@bind-IsValid` checks the fields' `Validation`, and with `DataAnnotations` the model's annotations, "
                    + "without showing messages, so the user isn't told off before they start; it is for turning a "
                    + "button on and off. When the app calls `AddValidation` and marks the model "
                    + "`[ValidatableType]`, it checks nested objects too, as the submit does.",
                "A field's `Error`, such as a server's answer, keeps the form from submitting and `IsValid` false "
                    + "until the parent clears it.",
                "Every field has `FocusAsync()`, to move focus from code, such as to the field a server's answer is "
                    + "about. A disabled field doesn't take focus.",
                "It posts as a static SSR form when given a `FormName`.",
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
            Api = [typeof(TbTextField<>), typeof(FieldClasses)],
            Accessibility =
            [
                "The `Label` is a real `<label>` and names the field; give every field one. A placeholder is not "
                    + "a label.",
                "Helper text and validation messages are linked to the field with `aria-describedby`, an error "
                    + "sets `aria-invalid`, and `Required` sets `aria-required`; the asterisk it adds is hidden from "
                    + "screen readers.",
                "A validation message that appears as someone edits the field is read out once. Checking the whole "
                    + "form on submit reads none out, so a form with several errors doesn't speak them all at once.",
                "An `Error` from the parent, such as a server's answer, is linked the same way and read out once when "
                    + "it appears, even after a submit; it keeps its form from submitting until the parent clears it.",
                "After a static-SSR form post, text that doesn't convert, such as letters in a number, stays in the "
                    + "field with the form's error, so nothing typed is lost. Blazor reports these errors for an "
                    + "`EditForm` whose model is supplied by `[SupplyParameterFromForm]` without its own `FormName`.",
                "On a static page a number or date shows in the invariant culture, as 1.5 and 07/02/2026, because "
                    + "Blazor reads a static form's post that way; for numbers typed in the user's own format, use a "
                    + "number field.",
                "`@ref` the field and call `FocusAsync()` to move focus to it, or `SelectTextAsync()` to also select "
                    + "its text so typing replaces it.",
                "`Type` sets the role: a text box, a search box for `Search`, and none for `Password`, as "
                    + "browsers expose them.",
                "`ReadOnly` keeps the field focusable and posted with its form, and screen readers hear it as read "
                    + "only; `Disabled` takes it out of the tab order and the post.",
                "`Clearable` adds a button named \"Clear\" (or `ClearLabel`) and described by the field's label while "
                    + "the field holds text it can change; pressing it empties the field and puts focus back in it. A "
                    + "number needs a nullable type, such as `int?`.",
                "`UpdateDelay` waits for typing to pause before updating the value, so a search doesn't run on "
                    + "every keystroke; Enter in a single-line field, or leaving the field, updates it at once.",
                "`MaxLines` lets a text area grow with its text from `Lines` up to that many lines, and then scroll, "
                    + "in browsers with CSS `field-sizing`.",
                "`Mask` fits typing to a pattern such as `(000) 000-0000` (`0` a digit, `a` a letter, `*` either), "
                    + "adding its brackets and dashes as you type and keeping the caret in place; Backspace after one "
                    + "removes the character before it. Empty slots make the field invalid, the placeholder shows the "
                    + "pattern, and a pattern of digits brings up a number keyboard. The value holds what was typed, "
                    + "or the whole text with `IncludeMaskLiterals`. A static form post binds the text as it was "
                    + "sent, so check it on the model too, with a `[RegularExpression]` for example.",
                "`Variant` and `Size` work the same on every boxed field: text, number, select, autocomplete, color "
                    + "and date range. `Filled` keeps a 3:1 line along the box's bottom, red when the field is "
                    + "invalid and dashed when it's read only. Each size is set from the theme's density, as a "
                    + "button's is.",
            ],
            Target = new("textbox", "Name"),
            Blocks = ["tb-field"],
            DataParts = [.. FieldParts, "input", "clear"],
            TestingTip = "A field with `UpdateDelay` takes its value once typing pauses, so assert with Playwright's "
                + "`Expect`, which waits, or press Enter to update it at once. In bUnit, register a `FakeTimeProvider` "
                + "as the `TimeProvider` and advance it past the delay.",
        },
        new("number-field", "Number field", ExampleCategory.Inputs,
            "A labeled number input with step buttons and limits.", typeof(NumberFieldExample))
        {
            Api = [typeof(TbNumberField<>), typeof(FieldClasses)],
            Accessibility =
            [
                "`Variant` (`Outlined` or `Filled`) and `Size` work as on the text field.",
                "A native number input, a `spinbutton`: Up and Down step the value by `Step`.",
                "With a `Format`, such as `C2` or `P0`, it is a text box acting as a `spinbutton` instead, since a "
                    + "number box can't show grouping or symbols. It reads and shows numbers in its `Culture`, gives "
                    + "screen readers the number as `aria-valuenow` and the formatted text as `aria-valuetext`, and "
                    + "Up and Down still step it.",
                "The Decrease and Increase buttons are left out of the tab order, since the arrow keys do the "
                    + "same, and they turn off at `Min` and `Max`.",
                "A value out of range, or one that isn't a number, shows as an error with `aria-invalid`, unless "
                    + "`ClampToRange` moves it to the nearer limit. `Decimals` rounds what is typed.",
                "The `Label` names the field. Helper text and validation messages are linked to the field with "
                    + "`aria-describedby`, an error sets `aria-invalid`, and `Required` sets `aria-required`; the "
                    + "asterisk it adds is hidden from screen readers.",
                "`ReadOnly` keeps the value from changing by typing, the arrow keys or the step buttons, which turn "
                    + "off.",
                "Before the page is interactive, a field with a `Format` is a plain number box, since a static form "
                    + "posts numbers in no culture.",
            ],
            Target = new("spinbutton", "Quantity"),
            Blocks = ["tb-field"],
            DataParts = [.. FieldParts, "input", "decrement", "increment"],
            TestingTip = "Without a `Format` the field reads what was typed through its own change event, which "
                + "bUnit's `Change` doesn't reach; `await cut.Tb().NumberField(\"Quantity\").EnterAsync(\"3\")` "
                + "from `Trailblazor.Testing` types into either kind. In Playwright, "
                + "`await page.Tb().NumberField(\"Quantity\").EnterAsync(\"3\")` from "
                + "`Trailblazor.Testing.Playwright` waits for the field's `data-ready`, types and leaves it.",
        },
        new("checkbox", "Checkbox", ExampleCategory.Inputs,
            "Turns an option on or off, or shows that only some of a group are on.", typeof(CheckboxExample))
        {
            Api = [typeof(TbCheckbox<>), typeof(ChoiceClasses)],
            Accessibility =
            [
                "A native checkbox inside its label, so the `Label` names it, clicking the text toggles it, and "
                    + "Space toggles it from the keyboard.",
                "A `bool?` value of null shows the mixed state, which screen readers read as partly checked.",
                "Helper text and validation messages are linked to the field with `aria-describedby`, an error "
                    + "sets `aria-invalid`, and `Required` sets `aria-required`; the asterisk it adds is hidden from "
                    + "screen readers.",
                "In forced-colors mode it falls back to the system's own control, so it stays visible in every "
                    + "high-contrast theme.",
                "`ReadOnly` keeps the box focusable and posted, but neither a click nor Space changes it; screen "
                    + "readers hear it as read only.",
                "`Tone` colors the checked box, standing out from the page by at least 3:1 in every tone; warning "
                    + "takes its text color for that. `Size` scales the box and its label.",
                "Rich content as `ChildContent` takes the place of `Label` and names the box, link text included, so "
                    + "keep it short. A link inside opens without ticking the box.",
            ],
            Target = new("checkbox", "Email me updates"),
            Blocks = ["tb-checkbox"],
            DataParts = ["input", "label", "helper", "error"],
            Telemetry = CheckboxTelemetry,
        },
        new("checkbox-group", "Checkbox group", ExampleCategory.Inputs,
            "Picks any number of options from a short list shown all at once.", typeof(CheckboxGroupExample))
        {
            Api = [typeof(TbCheckboxGroup<>), typeof(TbCheckboxItem<>), typeof(ChoiceClasses)],
            Accessibility =
            [
                "A `fieldset` named by its `Label` as the legend, with a native checkbox per option, each a tab stop "
                    + "that Space toggles.",
                "Helper text and messages describe the whole group. An error marks the group and every checkbox "
                    + "invalid. ARIA has no required state for a group, so `Required` shows the marker; say what's "
                    + "needed, such as \"Pick at least one\", in the helper text.",
                "`Disabled` on the group turns off every option, and an option can be disabled on its own. "
                    + "`ReadOnly` keeps each box focusable and posted, but a click or Space doesn't change it.",
                "`FocusAsync()` moves focus to the first option that isn't disabled.",
                "In forced-colors mode it falls back to the system's own controls, so it stays visible in every "
                    + "high-contrast theme.",
            ],
            Target = new("group", "Trail days"),
            Blocks = ["tb-checkbox-group", "tb-checkbox"],
            DataParts = ["label", "helper", "error", "options", "input"],
            TestingTip = "Each option is a native checkbox, so Playwright's `CheckAsync()` and `UncheckAsync()` work "
                + "on `GetByRole(AriaRole.Checkbox, ...)` inside the group. In bUnit, `Change(true)` or "
                + "`Change(false)` on an option's `input` checks or unchecks it.",
        },
        new("switch", "Switch", ExampleCategory.Inputs,
            "Turns a setting on or off right away.", typeof(SwitchExample))
        {
            Api = [typeof(TbSwitch), typeof(ChoiceClasses)],
            Accessibility =
            [
                "A native checkbox with `role=\"switch\"`, read as on or off; the `Label` names it and Space "
                    + "toggles it.",
                "Use it for a setting that takes effect at once. For a choice saved with a form, use a checkbox.",
                "In forced-colors mode it falls back to the system's own control, so it stays visible in every "
                    + "high-contrast theme.",
                "`ReadOnly` keeps the switch focusable and posted, but neither a click nor Space changes it; "
                    + "screen readers hear it as read only.",
                "`Tone` colors the track when on, standing out from the page by at least 3:1 in every tone; warning "
                    + "takes its text color for that. `Size` scales the track and its label.",
                "Rich content as `ChildContent` takes the place of `Label` and names the switch, so keep it short.",
            ],
            Target = new("switch", "Auto-save"),
            Blocks = ["tb-switch"],
            DataParts = ["input", "label", "helper", "error"],
            Telemetry = CheckboxTelemetry,
        },
        new("slider", "Slider", ExampleCategory.Inputs,
            "Chooses a number, or a range, by dragging a thumb along a track or with the arrow keys.",
            typeof(SliderExample))
        {
            Api = [typeof(TbSlider<>), typeof(TbRangeSlider<>), typeof(SliderMark), typeof(SliderRange<>),
                typeof(ChoiceClasses)],
            Accessibility =
            [
                "Each thumb is the browser's own range input, a `slider` named by the `Label`. The arrows step, Page "
                    + "Up and Page Down jump, and Home and End go to the ends.",
                "`ValueFormatter` sets the text screen readers hear for the value (`aria-valuetext`), and the text "
                    + "`ShowValue` shows above the thumb.",
                "A range slider's two thumbs are named by the label and \"Minimum\" or \"Maximum\" (`StartLabel` "
                    + "and `EndLabel`), each a tab stop of its own, and neither can pass the other. Its "
                    + "`FocusAsync()` focuses the start thumb.",
                "It posts in a static form, as a number or as `Start` and `End`. In forced-colors mode the track and "
                    + "its fill take system colors.",
                "`ReadOnly` keeps the thumbs focusable and posted, but neither the pointer nor the keys move them; "
                    + "screen readers hear them as read only.",
            ],
            Target = new("slider", "Map brightness"),
            Blocks = ["tb-slider"],
            DataParts = ["label", "helper", "error", "value", "track", "fill", "mark", "input", "start-thumb",
                "end-thumb"],
            TestingTip = "Set a slider with `await page.GetByRole(AriaRole.Slider, new() { Name = \"Map brightness\" "
                + "}).FillAsync(\"80\")`, which Playwright supports for range inputs. Then check what the app "
                + "did with it, such as `Expect(page.GetByText(\"Saved at\")).ToHaveTextAsync(\"Saved at 80%\")`: "
                + "`ToHaveValueAsync(\"80\")` passes as soon as the box changes, before the app hears of it. A range "
                + "slider's thumbs are named \"Elevation Minimum\" and \"Elevation Maximum\".",
        },
        new("radio-group", "Radio group", ExampleCategory.Inputs,
            "Picks one option from a short list shown all at once.", typeof(RadioGroupExample))
        {
            Api = [typeof(TbRadioGroup<>), typeof(TbRadio<>), typeof(ChoiceClasses)],
            Accessibility =
            [
                "A `fieldset` with `role=\"radiogroup\"`, named by its `Label` as the legend. Required, error and "
                    + "helper text apply to the whole group.",
                "The group is one tab stop, and the arrow keys move between options and choose them, as browsers "
                    + "do for native radios.",
                "`Disabled` on the group turns off every option.",
                "`FocusAsync()` moves focus where Tab would: to the checked radio, or else the first radio that "
                    + "isn't disabled.",
                "In forced-colors mode it falls back to the system's own control, so it stays visible in every "
                    + "high-contrast theme.",
                "`ReadOnly` turns off every option but the chosen one, which stays a tab stop and still posts; "
                    + "with nothing chosen, every option is off. Screen readers hear the group as read only.",
                "`Tone` colors the chosen radio, standing out from the page by at least 3:1 in every tone; warning "
                    + "takes its text color for that. `Size` scales the radios and their labels.",
            ],
            Target = new("radiogroup", "Paper size"),
            Blocks = ["tb-radio-group", "tb-radio"],
            DataParts = ["label", "helper", "error", "options", "input"],
        },
        new("select", "Select", ExampleCategory.Inputs,
            "Picks one or several options from a list that opens under the field.", typeof(SelectExample))
        {
            Api = [typeof(TbSelect<>), typeof(TbMultiSelect<>), typeof(TbOption<>), typeof(TbOptionGroup),
                typeof(SelectLabels), typeof(FieldClasses)],
            Accessibility =
            [
                "`Variant` (`Outlined` or `Filled`) and `Size` work as on the text field.",
                "A `combobox` named by its `Label` that opens a `listbox` of `option`s. Focus stays on the "
                    + "combobox, which points to the active option with `aria-activedescendant`.",
                "While closed: Down, Up, Enter or Space opens it, Home and End open it on the first or last "
                    + "option, and typing jumps to an option that starts with what was typed.",
                "While open: the arrows, Home and End move; Enter or Space chooses; Escape closes. In a single "
                    + "select, Tab chooses the active option and moves on. A multi-select stays open as options are "
                    + "chosen.",
                "Before the page is interactive it renders as the browser's own `<select>`, so it still works.",
                "The clear button is named \"Clear\" and described by the field's label, so a screen reader says "
                    + "which field it clears, and it sends focus back to the field. An option with content of its own, "
                    + "such as an image and a caption, is named by its `Text`, chosen or not; one with neither text "
                    + "nor a name gets a warning in the browser console. A select bound to a value type, such as "
                    + "`Guid`, clears to its default, which shows the placeholder.",
                "A value no option has, such as one that has left the list, still shows: as its text, or as "
                    + "`UnmatchedText` makes it. Before the page is interactive, the native select keeps it chosen. "
                    + "Options that load as the list opens aren't there yet, so give such a select `UnmatchedText`, "
                    + "such as a lookup of the value's name, rather than showing a key such as a `Guid`.",
                "With `SelectAll`, a multi-select's list starts with a \"Select all\" option, named by "
                    + "`Labels.SelectAll`, that the arrows reach like any other. It chooses every enabled option, and is "
                    + "selected once they all are, when choosing it takes them out again. It hides while the filter "
                    + "has text.",
                "A `TbOptionGroup` is a `group` named by its `Label`; the arrows and typing move through every group.",
                "With `Chips`, each chosen option shows as a chip. Backspace removes the last one and each chip has a "
                    + "remove button for the pointer; a removal is announced, and the list still deselects options.",
                "With `Filterable`, opening the list moves focus to a filter box above it, a `combobox` with "
                    + "`aria-autocomplete=\"list\"` named by `Labels.Filter`. Typing on the closed select starts the "
                    + "filter. Matching ignores case and accents, groups with no matches hide, and `Labels.NoResults` "
                    + "is announced when nothing matches. Up and Down move, Enter chooses, Escape closes and returns "
                    + "focus to the select.",
                "`ReadOnly` keeps the select focusable and posted, but it doesn't open and its chips can't be "
                    + "removed; screen readers hear it as read only. Before the page is interactive, the native select "
                    + "puts its choice back whenever the user changes it.",
            ],
            Target = new("combobox", "Shipping"),
            Blocks = ["tb-field", "tb-select"],
            DataParts = [.. FieldParts, "input", "placeholder", "value",
                "chips", "chip", "chip-remove", "clear", "chevron", "popup", "filter", "listbox", "group",
                "group-label", "option", "select-all", "no-results"],
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
            Api = [typeof(TbAutocomplete<>), typeof(AutocompleteLabels), typeof(FieldClasses)],
            Accessibility =
            [
                "`Variant` (`Outlined` or `Filled`) and `Size` work as on the text field.",
                "The input is a `combobox` with `aria-autocomplete=\"list\"`, named by its `Label`, that controls a "
                    + "`listbox` of `option`s. Focus stays in the input; the active option is pointed to with "
                    + "`aria-activedescendant`.",
                "Down opens the list and moves through it, Up moves back, Enter chooses, and Escape closes. Alt+Down "
                    + "opens without moving. The arrow keys never take the caret out of the text.",
                "While a search runs the list is `aria-busy` and says \"Searching…\"; when it finishes, the number "
                    + "of suggestions is announced. Every word can be changed.",
                "A search that fails says \"Couldn't load suggestions.\" in the list and reads it out, raises "
                    + "`OnSearchFailed`, and leaves the field usable: typing again searches again.",
                "Before the page is interactive it is a plain text input, so a static form still posts what was "
                    + "typed.",
                "`ReadOnly` keeps the list closed and hides the clear button.",
                "The clear button is named \"Clear\" and described by the field's label, so a screen reader says "
                    + "which field it clears.",
            ],
            Target = new("combobox", "Trailhead"),
            Blocks = ["tb-field", "tb-select", "tb-autocomplete"],
            DataParts = [.. FieldParts, "input", "clear", "listbox", "option",
                "status"],
            TestingTip = "To choose a suggestion, `await page.Tb().Autocomplete(\"Trailhead\").ChooseAsync(\"pine\", "
                + "\"Pine Hollow North ridge\")` from `Trailblazor.Testing.Playwright` types the text, waits for the "
                + "search, and clicks the option by its name. In bUnit, `Search` runs once `DebounceDelay` has passed "
                + "on the app's `TimeProvider`, so advance a `FakeTimeProvider` past it, or set it to `TimeSpan.Zero`.",
        },
        new("color-picker", "Color picker", ExampleCategory.Inputs,
            "Chooses a color by typing it, picking a swatch, using the custom picker, or taking it from the screen.",
            typeof(ColorPickerExample))
        {
            Api = [typeof(TbColorPicker), typeof(ColorPickerLabels), typeof(FieldClasses)],
            Accessibility =
            [
                "`Variant` (`Outlined` or `Filled`) and `Size` work as on the text field.",
                "The text box is named by the `Label` and takes `#rgb`, `#rrggbb` or `rgb()`; a color it can't read "
                    + "says the format to use and marks the box `aria-invalid`.",
                "The picker is a `dialog`. Swatches are native radios in a `radiogroup`, named by `SwatchName`, so the "
                    + "arrow keys move between them and choose.",
                "The saturation and brightness pad is for the pointer; the Hue, Saturation, Brightness and Opacity "
                    + "sliders and the Red, Green and Blue boxes give keyboard and screen reader users the same "
                    + "control.",
                "The eyedropper button appears only in browsers that have one.",
                "Before the page is interactive it is a plain text box that only takes `#rrggbb` (or `#rrggbbaa` "
                    + "with `Alpha`), so a static form posts a hex color, as typed.",
                "`ReadOnly` keeps the color from changing: the text can't be edited, the picker buttons turn off and "
                    + "the clear button hides.",
                "The clear button, named \"Clear\" and described by the field's label, empties the color and puts "
                    + "focus back in the text box.",
            ],
            Target = new("textbox", "Trail marker"),
            Blocks = ["tb-field", "tb-color-picker"],
            DataParts = [.. FieldParts, "input", "swatch-preview", "clear",
                "eyedropper", "pad", "hue", "saturation", "brightness", "alpha", "red", "green", "blue", "swatches",
                "swatch"],
            TestingTip = "`await page.Tb().ColorPicker(\"Trail marker\").ChooseAsync(\"#d1242f\")` from "
                + "`Trailblazor.Testing.Playwright` types the color and waits until the field has taken it; for "
                + "pickers that share a label, pass a locator, as `ColorPicker(row.GetByLabel(\"Color\"))`. In bUnit, "
                + "set `FakeBrowserService.PickedColor` to test the eyedropper.",
        },
        new("file-upload", "File upload", ExampleCategory.Inputs,
            "Chooses files by clicking or dropping them on a drop zone, and lists them with a way to remove each.",
            typeof(FileUploadExample))
        {
            Api = [typeof(TbFileUpload), typeof(FileUploadLabels), typeof(FieldClasses)],
            Accessibility =
            [
                "It is the browser's own file input, named by the `Label`, laid over the drop zone: Enter or Space "
                    + "opens the file dialog, and screen readers announce it as a file button.",
                "Files of another type, too large, or beyond `MaxFiles` are left out, and the reasons are linked to "
                    + "the input and announced with the number of files chosen.",
                "The chosen files are a `list`; each has a \"Remove\" button named with its file, and focus moves back "
                    + "to the input when a file is removed.",
                "Each new choice replaces the last, as the browser's file input does, so read the files before the "
                    + "user chooses again.",
                "`ReadOnly` turns off the picker and the remove and clear buttons, and leaves the chosen files "
                    + "listed. A file input can't be read only, so the picker can't be focused.",
                "Before the page is interactive it is the same file input, named for its form, so a static form "
                    + "posts the chosen files; the list of files and the reasons for any left out show once it is "
                    + "interactive.",
                "`Hidden` keeps the field out of sight, the tab order and the reading order, apart from the reasons "
                    + "any files were left out and its messages, so your own button can call `OpenAsync()` from its "
                    + "click; browsers open a file dialog only shortly after a user's click. `ClearAsync()` empties the "
                    + "field from code.",
            ],
            Target = new("button", "Trail maps"),
            Blocks = ["tb-field", "tb-file-upload"],
            DataParts = ["label", "helper", "error", "zone", "input", "files", "file", "remove", "rejected", "clear"],
            TestingTip = "`await page.Tb().FileUpload(\"Trail maps\").ChooseAsync(\"map.pdf\")` from "
                + "`Trailblazor.Testing.Playwright` takes file paths or `FilePayload`s and waits until the field has "
                + "taken them. In bUnit, `FindComponent<InputFile>().UploadFiles(...)` chooses files.",
        },
        new("date-range-picker", "Date range picker", ExampleCategory.Inputs,
            "Chooses a range of dates, typed or picked in a two-month calendar, with ready-made ranges.",
            typeof(DateRangePickerExample))
        {
            Api = [typeof(TbDateRangePicker<>), typeof(DateRangePickerLabels), typeof(FieldClasses)],
            Accessibility =
            [
                "`Variant` (`Outlined` or `Filled`) and `Size` work as on the text field.",
                "The two date boxes and the calendar button sit in a `group` named by the `Label`; each box is "
                    + "named by the label and \"Start date\" or \"End date\", and shows the format as its placeholder. "
                    + "`FocusAsync()` focuses the start box.",
                "The calendar is a `dialog` with a `grid` for each month. Arrows move by a day or a week, Home and End "
                    + "go to the start or end of the week, Page Up and Page Down change the month, and with Shift the "
                    + "year. Enter or Space picks a day; Escape closes and returns focus to the button.",
                "Days in the range are `aria-selected`, today is `aria-current=\"date\"`, and days that can't be "
                    + "chosen are `aria-disabled` but can still be reached, so the user hears why.",
                "A date that can't be read says which box and the format to use, and marks that box `aria-invalid`. "
                    + "Picking the start announces that the end comes next.",
                "Before the page is interactive it shows the browser's own date boxes, so a static form still posts "
                    + "both dates.",
                "`ReadOnly` keeps the dates from changing: the boxes can't be edited, the calendar button turns off "
                    + "and the clear button hides.",
                "The clear button is named \"Clear\" and described by the field's label, so a screen reader says "
                    + "which field it clears.",
            ],
            Target = new("group", "Trip dates"),
            Blocks = ["tb-field", "tb-date-range"],
            DataParts = [.. FieldParts, "start-input", "end-input", "trigger",
                "clear", "panel", "presets", "preset", "previous-month", "next-month", "month", "day"],
            TestingTip = "`await page.Tb().DateRangePicker(\"Trip dates\").ChooseAsync(start, end)` from "
                + "`Trailblazor.Testing.Playwright` types both dates in the field's own format and waits until the "
                + "field has taken them. Its boxes have `data-part=\"start-input\"` and `data-part=\"end-input\"`.",
        },
        new("listbox", "List box", ExampleCategory.Inputs,
            "Picks one or several options from a list that is always shown.", typeof(ListBoxExample))
        {
            Api = [typeof(TbListBox<>), typeof(TbMultiListBox<>), typeof(TbOption<>), typeof(TbOptionGroup),
                typeof(FieldClasses)],
            Accessibility =
            [
                "A `listbox` of `option`s named by its `Label`. It is one tab stop; focus stays on the listbox, which "
                    + "points to the active option with `aria-activedescendant`.",
                "Down and Up move, Home and End go to the first and last option, and typing jumps to an option that "
                    + "starts with what was typed. Disabled options are skipped.",
                "In a single listbox, moving chooses the option, as in the browser's own list. In a multiple one, "
                    + "Space chooses or clears the active option, and Shift with Down or Up also chooses the next.",
                "Before the page is interactive it renders as the browser's own `<select>` list, so it still works.",
                "A `TbOptionGroup` is a `group` named by its `Label`; the arrows and typing move through every group.",
                "An option with content of its own, such as an image and a caption, is named by its `Text`; one with "
                    + "neither text nor a name gets a warning in the browser console.",
                "`ReadOnly` keeps the list focusable, and the arrow keys still move through its options so they can "
                    + "be read, but nothing gets chosen; screen readers hear it as read only.",
            ],
            Target = new("listbox", "Trail"),
            Blocks = ["tb-field", "tb-listbox"],
            DataParts = ["label", "helper", "error", "input", "group", "group-label", "option"],
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
            "A short description that appears on hover or focus.", typeof(TooltipExample))
        {
            Api = [typeof(TbTooltip), typeof(TooltipSettings)],
            Accessibility =
            [
                "The tooltip describes its anchor: its text goes into the `aria-describedby` of the first "
                    + "focusable element inside. It's a description, not a name, so that element still needs a name of "
                    + "its own. A tooltip that only repeats that element's `aria-label` isn't added, so it isn't read "
                    + "twice.",
                "It shows at once on focus and after a short delay on hover. Moving the pointer onto the tooltip "
                    + "keeps it open, and Escape hides it, without also closing a dialog it is in. `ShowDelay` and "
                    + "`HideDelay` change the delays for one tooltip, and `options.Tooltips` in `AddTrailblazor` for "
                    + "the whole app.",
                "`TextContent` can hold formatted text, such as a shortcut in `<kbd>`, which is read as part of the "
                    + "description. The tooltip can't be reached with the keyboard, so it holds no links or buttons; "
                    + "use a popover for those.",
                "The arrow from `ShowArrow` is decoration and isn't read out.",
            ],
            Target = new("tooltip", "Add an item") { OpenedBy = new("button", "Add"), OpenOnHover = true },
            Blocks = ["tb-tooltip", "tb-tooltip-anchor"],
            DataParts = ["tooltip", "arrow"],
            TestingTip = "Focus or hover the trigger, then find the tooltip with `GetByRole(AriaRole.Tooltip)`; "
                + "`ToHaveAccessibleDescriptionAsync` on the trigger checks what screen readers hear. In bUnit the "
                + "tooltip's text is in the markup from the start, with `role=\"tooltip\"`.",
        },
        new("menu", "Menu", ExampleCategory.Overlays,
            "A list of actions, options and links that opens from a button, with groups and submenus.",
            typeof(MenuExample))
        {
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
                    + "next item that starts with it. Right opens a submenu and Left closes it, mirrored on "
                    + "right-to-left pages.",
                "Escape closes the menu and returns focus to the trigger; Tab closes it and moves on. Disabled "
                    + "items stay focusable, marked `aria-disabled`, so people can still find them.",
                "`Description` adds text that screen readers read after the trigger's name, such as a hint about "
                    + "other keys it takes.",
                "A custom `Trigger` gets the same attributes through `context.Attributes`; put them on a button, and "
                    + "give it a name, such as an `aria-label` on an avatar. `Disabled` turns either trigger off.",
                "Checkbox items are `menuitemcheckbox`es and radio items `menuitemradio`s, with `aria-checked`. A "
                    + "`TbMenuGroup` or radio group is a `group` named by its `Label`, and `TbMenuSeparator` is a "
                    + "`separator`.",
                "An item with `Href` is a link; Enter or Space follows it. With `NewTab` it opens in a new tab, and "
                    + "screen readers hear that it does.",
                "Resting the pointer on a submenu's item opens it without moving focus. On the way there the pointer "
                    + "can cross other items without the submenu closing.",
                "A menu too long for the window opens on the side with more room and scrolls, so every item stays "
                    + "reachable; a popover's panel and a date range picker's panel do the same.",
            ],
            Target = new("button", "Actions"),
            Blocks = ["tb-menu"],
            DataParts = ["trigger", "menu", "icon", "label", "indicator", "chevron", "submenu-trigger", "group-label"],
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
            typeof(DialogExample), typeof(RenameDialog), typeof(ExportDialog), typeof(NotesDialog))
        {
            Api =
            [
                typeof(IDialogService), typeof(DialogReference<>), typeof(DialogOptions), typeof(DialogSettings),
                typeof(TbDialog), typeof(TbDialogActions), typeof(TbDialogDescription),
            ],
            Accessibility =
            [
                "A native `<dialog>` named by its title. A modal one makes the rest of the page inert, so focus "
                    + "and screen readers stay inside; a non-modal one leaves the page usable.",
                "On opening, focus goes to an element marked `autofocus`, or else the first field, or the dialog "
                    + "itself with `InitialFocus = DialogFocus.Dialog`. On closing, it goes back to whatever opened "
                    + "the dialog.",
                "Escape cancels the topmost dialog unless `DismissOnEscape` is false; a menu, select, autocomplete "
                    + "list or tooltip open inside it, or a drag, takes the key first. A click that starts and ends "
                    + "on the backdrop cancels a modal unless `DismissOnBackdrop` is false.",
                "`ConfirmAsync` and `AlertAsync` open an `alertdialog` described by its message. The alert's tone icon "
                    + "is decorative, so say what happened in the message.",
                "A `TbDialogDescription` describes any dialog with `aria-describedby`, so screen readers read it with "
                    + "the title as the dialog opens.",
                "Escape, the backdrop and the close button ask the dialog first: override `CanDismissAsync` in a "
                    + "`TbDialogContent`, or set `CanDismiss` on a `TbDialog`, to keep it open, as the rename dialog "
                    + "does once its name has changed. Closing from code doesn't ask.",
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
                    + "itself from a `TbDialogContent`. Turning modeless keeps focus where it was; turning modal "
                    + "moves focus into the dialog unless it is already there, and makes the page inert again.",
                "A `Minimizable` modal leaves the page usable while it is minimized, with its toasts and "
                    + "announcements back on the page, and blocks it again once restored; focus stays on the button "
                    + "that restored it.",
            ],
            Target = new("dialog", "Rename report") { OpenedBy = new("button", "Rename report") },
            Blocks = ["tb-dialog"],
            DataParts = ["dialog", "header", "icon", "move", "minimize", "maximize", "close", "description", "body",
                "actions", "resize"],
            TestingTip = "In bUnit, `this.ShowDialogAsync<TDialog, TResult>(title)` from `Trailblazor.Testing` hosts "
                + "and opens a dialog in one call, as the Testing guide shows. "
                + "`IDialogService.OpenDialogs` lists the open dialogs, newest last, and `CloseAllAsync()` "
                + "cancels them all, which resets a page between tests. `Show` returns a reference whose `Result` "
                + "finishes when the dialog closes. In Playwright, `page.Tb().Dialog(\"Delete report?\")` from "
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
            DataParts = ["focus-scope"],
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
                "A custom `Trigger` gets the same attributes through `context.Attributes`; put them on a button.",
            ],
            Target = new("dialog", "Filter the report") { OpenedBy = new("button", "Filters") },
            Blocks = ["tb-popover"],
            DataParts = ["trigger", "panel", "title"],
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
                "It blocks the pointer, not the keyboard. Mark what it covers `inert` while it is shown, as the "
                    + "loading report does, so Tab can't reach controls behind it.",
                "With `CloseOnClick`, Escape closes it too, unless a menu or popover above it takes the key first.",
                "For a message that needs an answer, use a dialog, which moves focus and traps it.",
            ],
            Target = null,
            Blocks = ["tb-overlay"],
            DataParts = ["overlay", "scrim", "content"],
            TestingTip = "The overlay's element has `data-part=\"overlay\"`; click its `data-part=\"scrim\"` to "
                + "close one with `CloseOnClick`. Before pressing Escape, wait for the overlay's `data-ready` "
                + "attribute, which appears once Escape is wired.",
        },
        new("tabs", "Tabs", ExampleCategory.Navigation,
            "Switches between panels of related content, one at a time.", typeof(TabsExample))
        {
            Api = [typeof(TbTabs), typeof(TbTab)],
            Accessibility =
            [
                "A `tablist` of `tab`s, each controlling a `tabpanel`. Give the list an `AriaLabel`. A tab's "
                    + "`TextContent` or `Text` is its name, so a count in it is read with it; its `Icon` isn't "
                    + "read.",
                "`Variant` marks the selected tab: `Text` (and, for now, `Outlined`) underlines it, and `Soft` and "
                    + "`Filled` make it a pill, in vertical lists too. `--tb-tabs-indicator-color` and "
                    + "`--tb-tabs-indicator-size` restyle the underline. In forced colors, the selected pill keeps a "
                    + "Highlight outline.",
                "The tab list is one tab stop. Left and Right move between tabs and wrap, mirrored on "
                    + "right-to-left pages, and Home and End jump to the ends. A `Vertical` list has "
                    + "`aria-orientation=\"vertical\"` and moves with Up and Down instead. With automatic activation, "
                    + "moving also selects; with manual activation, Enter or Space selects.",
                "Each panel is focusable, so keyboard users can reach content that has no controls.",
                "In forced-colors mode, such as Windows high contrast, the selected tab keeps its underline in the "
                    + "system's highlight color.",
            ],
            Target = new("tablist", "Report sections"),
            Blocks = ["tb-tabs"],
            DataParts = ["list", "tab", "tab-anchor", "icon", "panel"],
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
            ],
            Target = new("button", "Shipping"),
            Blocks = ["tb-accordion"],
            DataParts = ["accordion", "item", "heading", "trigger", "chevron", "panel"],
            TestingTip = "The arrow keys, Home and End work once the accordion's script has attached, which "
                + "`data-ready` on its root, `[data-part=accordion]`, marks; wait for it before pressing them.",
        },
        new("tree-view", "Tree view", ExampleCategory.Navigation,
            "Shows nested items that open and close, with selection, checkboxes, a filter and children that load "
                + "when opened.", typeof(TreeViewExample))
        {
            Api =
            [
                typeof(TbTreeView<,>), typeof(TbTreeItem<>), typeof(TreeViewLabels), typeof(TreeDrop<>),
                typeof(MoveCheck), typeof(TreeInsertContext<>),
            ],
            Accessibility =
            [
                "It is a `tree` named by `AriaLabel`, with one `treeitem` per visible row carrying `aria-level`, "
                    + "`aria-setsize`, `aria-posinset`, `aria-expanded`, and `aria-selected` or `aria-checked`.",
                "The tree is one tab stop. Up and Down move between rows, Home and End go to the ends, Right opens a "
                    + "row or moves into it, Left closes it or moves to its parent, `*` opens its siblings, and typing "
                    + "finds a row by its text.",
                "Enter selects and activates a row; Space selects it, or checks it in a `Checkable` tree. Shift with "
                    + "the arrows extends a `Multiple` selection, and Ctrl+A selects every row.",
                "A row is named by everything it shows, its `ItemEnd` content included, so keep actions there short "
                    + "or give icon buttons an `AriaLabel`.",
                "Children that load show a loading row, and a load that fails is read out and shows a row that "
                    + "offers to try again. The arrows reach both rows; Enter on the failed one tries again, and focus "
                    + "moves on to the children once they arrive.",
                "In a right-to-left layout, Right and Left swap, as the chevrons do.",
                "Rows written as nested `TbTreeItem`s work the same, from any component inside the tree. Each draws "
                    + "its row where it is written, followed by a `group` holding what is written inside it, so other "
                    + "content between rows shows in place and hides with its parent. Before the page is interactive, "
                    + "each renders a plain row, open to `InitialExpandDepth`. `Virtualize` needs `Items`.",
                "A `TbTreeItem` with no `Value`, such as the Trips heading, opens, closes and is found by typing, but "
                    + "can't be selected or activated, so it has no `aria-selected`. With cascading checks it checks "
                    + "the rows inside it, and it is never in the bound keys, so texts that repeat need no keys.",
                "A tree inside another tree's row is a tree of its own: its rows take their own keys, focus, clicks and "
                    + "drags, the outer tree's arrows pass over them, and the outer row isn't selected by them.",
                "With `OnDrop`, rows move by dragging with a mouse, pen or finger (a finger holds still for a moment "
                    + "first, so a swipe still scrolls), or with Alt+Shift and the arrows: Up and Down past a "
                    + "neighbor, Right into the row above, Left out of the parent. Each movable row is described by "
                    + "those keys, and Escape cancels a drag.",
                "A move is read out, such as \"Lake camp moved before Pine Hollow.\" A place `CanDrop` refuses "
                    + "shows its reason beside the row, in the danger color, and the reason is read out when the "
                    + "move is tried.",
                "The tree never moves rows itself: `OnDrop` says what moved and where, and the app applies it, so it "
                    + "can be undone like any other change.",
                "`InsertTemplate` content sits on each row's edge and shows on hover or focus; it is a tab stop "
                    + "after its row, like `ItemEnd` content.",
                "`Virtualize` renders only the rows in view, every one `ItemSize` pixels tall, in a tree given a "
                    + "height or in the page's own scroll. Each row still says its level, size and place, the keys "
                    + "and typing reach rows that aren't rendered yet, and the focused row stays rendered. Before "
                    + "the page is interactive every row renders.",
            ],
            Target = new("tree", "Trails by region"),
            Blocks = ["tb-tree"],
            DataParts = ["items", "item", "spacer", "toggle", "check", "content", "end", "insert", "loading", "failed",
                "empty", "drop-reason"],
            TestingTip = "`page.Tb().Tree(\"Trails by region\")` from `Trailblazor.Testing.Playwright` follows rows by "
                + "their path of texts: `ExpandAsync(\"River valley\", \"Fern Gully\")`, `SelectAsync(...)`, "
                + "`CheckAsync(...)` and `ItemAsync(...)`; `DragBeforeAsync`, `DragAfterAsync` and `DragIntoAsync` "
                + "drag one row's path onto another's. Every row also carries `data-key`, `data-level` and "
                + "`data-text`, and a row being dragged over carries `data-drop`. In a virtualized tree the helpers "
                + "scroll until the row renders (a drag needs both rows in view together), and rows also carry "
                + "`data-index` and `data-inside`, the number of rows in their branch. A row's `data-key` is its "
                + "key's text for a string, number or date key, and a made-up id such as `key-3` otherwise, when "
                + "the text is empty or when another key has it. Before pressing keys or dragging without the "
                + "helpers, wait for the tree's `data-ready`. In bUnit, a test that changes `Items` in place calls "
                + "`RefreshAsync`, as the app does; a new collection, or an `OnDrop`, builds the rows again by "
                + "itself.",
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
            "Shows items as rows of a table, with columns that sort, pages, and rows that can be selected.",
            typeof(DataGridExample))
        {
            Api =
            [
                typeof(TbDataGrid<>), typeof(TbColumn<>), typeof(DataGridLabels), typeof(GridRequest),
                typeof(GridResult<>), typeof(GridSort), typeof(SortableMove<>), typeof(MoveCheck),
            ],
            Accessibility =
            [
                "It is a native `table` named by `AriaLabel`, with a header cell for each column, so screen readers "
                    + "read each cell with its column.",
                "A sortable column's header is a button. A click sorts by it alone, then turns it around, then stops; "
                    + "Shift adds it to the sorts. The first sort sets `aria-sort` on its header, later ones say "
                    + "their place, and every change is read out, such as \"Sorted by Name, ascending.\"",
                "`Multiple` selection gives each row a checkbox named \"Select\" and the row's label, and the header "
                    + "a box for every row on the page that shows \"mixed\" when only some are. `Single` uses radio "
                    + "buttons. Clicking a row selects it too, unless the click lands on a button, link or field in "
                    + "it.",
                "Pages come with a line such as \"1–5 of 7\" and a `TbPagination`; `PageSizes` adds a list of page "
                    + "sizes.",
                "While rows load the table is `aria-busy`. A `DataProvider` gets the page and sorts it asks for, and "
                    + "a new request cancels the last one. A failed load is read out and offers to try again, a load "
                    + "with no rows is read out too, and `RefreshAsync` loads again after a filter changes. With "
                    + "`EmptyContent`, set `Labels.Empty` to what screen readers should hear instead.",
                "Every control in it is a native button, link or input, reached with Tab. Only a pointer clicks a "
                    + "row, so whatever `OnRowClick` does needs a button in a cell too.",
                "With `OnRowMove`, rows held in `Items` can be put in order while the grid isn't sorted. Each row "
                    + "gains a handle in an \"Order\" column, a menu `button` named \"Move\" and the row's label: drag "
                    + "it, press the up and down arrows, Home or End on it, or choose from its menu. Moves cross "
                    + "pages, focus stays on the handle, and each is read out with the row's new position. `CanMoveRow` can "
                    + "refuse a move, with a reason shown beside the drop and read out. Each handle is a menu with a "
                    + "script of its own, so give a grid whose rows move a `PageSize` when it holds many rows.",
            ],
            Target = new("table", "Trails"),
            Blocks = ["tb-data-grid"],
            DataParts = ["grid", "scroll", "sort", "sort-order", "select-all", "row", "select", "order", "handle",
                "drop-reason", "loading", "empty", "failed", "summary", "pager"],
            TestingTip = "`page.Tb().Grid(\"Trails\")` from `Trailblazor.Testing.Playwright` finds a row by a "
                + "cell's text with `Row(\"Cedar Flats\")`, and a cell with "
                + "`await CellAsync(\"Cedar Flats\", \"Region\")`; "
                + "`SortAsync(\"Name\", descending: true)`, `SelectAsync(...)` and `GoToPageAsync(2)` drive "
                + "it; sorting and paging wait for a `DataProvider`'s rows. Without the helpers, wait for the "
                + "table's `aria-busy` to clear, and for `data-ready` on the grid's root, `[data-part=grid]`, "
                + "before clicking a row. Rows carry `data-key` and `data-selected`, and cells `data-column`, the "
                + "field's path such as `Shipping.City` unless the column has an `Id`. A row's `data-key` is its "
                + "key's text for a string, number or date key, and a made-up id such as `key-3` otherwise, when "
                + "the text is empty or when another key has it. In bUnit, find rows with `tr[data-part=row]` and "
                + "their key. A row draws again only when its item, its selection or the columns change, so a test "
                + "that changes what a `Cell` template shows calls `RenderRows`, as the app does, and one that "
                + "changes `Items` in place calls `RefreshAsync`.",
            Telemetry =
            [
                "With a `TelemetryName`, such as `\"jobs\"`, it counts `trailblazor.control.shown` as it first "
                    + "renders, and `trailblazor.control.used` with `trailblazor.action` `sort`, `page`, `page_size`, "
                    + "`select` or `move` for each of those the user does; a move also says whether a `pointer` or "
                    + "the `keyboard` made it.",
                "A page `ScrollToAsync` moves to isn't counted, and which column, page or rows were used is never "
                    + "recorded.",
            ],
        },
        new("table", "Table", ExampleCategory.Content,
            "A plain table written as markup, with the data grid's look.", typeof(TableExample))
        {
            Api = [typeof(TbTable)],
            Accessibility =
            [
                "It is a native `table`: name it with a `<caption>` or `aria-label`, and give headers `scope`.",
                "For rows that sort, page or select, use the data grid.",
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
            DataParts = ["first", "previous", "page", "ellipsis", "page-box", "page-count", "next", "last"],
            TestingTip = "To go to a page, `await page.Tb().Pagination(\"Photos\").GoToPageAsync(42)` from "
                + "`Trailblazor.Testing.Playwright` clicks it, types it into a compact pagination's box, or steps "
                + "with Next and Previous, then waits for it to be current.",
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
            DataParts = ["content"],
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
            DataParts = ["toaster", "toast", "icon", "title", "message", "action", "close"],
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
