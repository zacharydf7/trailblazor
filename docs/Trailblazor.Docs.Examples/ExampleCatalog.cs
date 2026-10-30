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
            ],
            Target = new("heading", "Display"),
            Blocks = ["tb-text"],
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
            ],
            Target = null,
            Blocks = ["tb-divider"],
        },
        new("surface", "Surface", ExampleCategory.Layout,
            "A raised panel with the app's surface color, an optional border and a shadow, for grouping content.",
            typeof(SurfaceExample))
        {
            Api = [typeof(TbSurface)],
            Accessibility =
            [
                "A plain `div` with no role. To make it a landmark, give it one with a name, such as "
                    + "`role=\"region\"` and an `aria-label`.",
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
                "Its text is its name; a decorative icon in the `Start` or `End` slot adds nothing to it.",
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
            TestingTip = "Check a toggle with `ToHaveAttributeAsync(\"aria-pressed\", \"true\")`, and a disabled "
                + "button with `ToBeDisabledAsync()`, which also passes for `aria-disabled`. To move focus to a "
                + "button from code, `@ref` it and call `FocusAsync()`.",
        },
        new("icon-button", "Icon button", ExampleCategory.Actions,
            "A button that shows only an icon, with the accessible name it needs.", typeof(IconButtonExample))
        {
            Api = [typeof(TbIconButton), typeof(IconButtonClasses)],
            Accessibility =
            [
                "`AriaLabel` is required and names the button, since it shows no text; a blank label throws when "
                    + "it renders.",
                "The icon itself is hidden from screen readers. Add a `TbTooltip` with the same words so sighted "
                    + "users can see the name too.",
                "Keys, `Disabled`, `FocusableWhenDisabled`, `Loading`, `Href` and `@bind-Pressed` work as they do "
                    + "for a button. A toggle keeps the same `AriaLabel` either way; `aria-pressed` tells whether it "
                    + "is on.",
            ],
            Target = new("button", "Add"),
            Blocks = ["tb-button", "tb-icon-button"],
        },
        new("button-group", "Button group", ExampleCategory.Actions,
            "Joins related buttons into one control.", typeof(ButtonGroupExample))
        {
            Api = [typeof(TbButtonGroup)],
            Accessibility =
            [
                "A `div` with `role=\"group\"`. Give it an `aria-label` that says what the buttons do together.",
                "Each button keeps its own tab stop; the group adds no arrow-key navigation.",
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
            TestingTip = "Press an item by its name, as `GetByRole(AriaRole.Button, new() { Name = \"Satellite\" })`, "
                + "and check it with `ToHaveAttributeAsync(\"aria-pressed\", \"true\")`. Wait for the group's "
                + "`data-ready` attribute before testing the arrow keys.",
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
            TestingTip = "Click the insert button by its name, as `GetByRole(AriaRole.Button, new() { Name = "
                + "\"Add a step after Pack the tent\" })`; Playwright hovers it first, which shows it.",
        },
        new("stack", "Stack", ExampleCategory.Layout,
            "Lays its children out in a row or a column, evenly spaced.", typeof(StackExample))
        {
            Api = [typeof(TbStack)],
            Accessibility =
            [
                "A plain `div` with no role; it only lays out what it holds.",
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
                "Plain `div`s with no roles; they only lay out what they hold.",
                "`Start` moves an item on screen but not in the reading order. Screen readers and the keyboard "
                    + "follow the order in the markup, so keep it the same as the order on screen.",
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
            TestingTip = "Resize with the keys, as `var separator = page.GetByRole(AriaRole.Separator, new() { Name "
                + "= \"Resize Trails\" }); await separator.FocusAsync(); await page.Keyboard.PressAsync(\"End\");`, "
                + "then check `ToHaveAttributeAsync(\"aria-valuenow\", \"40\")`. Wait for the splitter's "
                + "`data-ready` first.",
        },
        new("container", "Container", ExampleCategory.Layout,
            "Centers content and caps its width at a readable size.", typeof(ContainerExample))
        {
            Api = [typeof(TbContainer)],
            Accessibility =
            [
                "A plain `div` with no role; it only limits the width of what it holds.",
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
                "The icon is decorative. The close button is named by `CloseLabel`, \"Close\" by default.",
            ],
            Target = new("alert") { Text = "A danger alert." },
            Blocks = ["tb-alert"],
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
            TestingTip = "Remove a chip with `GetByRole(AriaRole.Button, new() { Name = \"Remove Archive\" })`, and "
                + "check a filter chip with `ToHaveAttributeAsync(\"aria-pressed\", \"true\")`.",
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
            TestingTip = "Find an item by the start of its name, as `GetByRole(AriaRole.Button, new() { NameRegex = "
                + "new Regex(\"^Pine Hollow\") })`, and check the current one with "
                + "`ToHaveAttributeAsync(\"aria-current\", \"true\")`.",
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
                "In forced-colors mode, such as Windows high contrast, the badge keeps an outline and a dot keeps its "
                    + "fill.",
            ],
            Target = null,
            Blocks = ["tb-badge"],
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
                "With reduced motion, the moving bar and the spinning ring pulse instead.",
                "In forced-colors mode, such as Windows high contrast, the fill takes the system's highlight color.",
            ],
            Target = new("progressbar", "Upload"),
            Blocks = ["tb-progress"],
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
            TestingTip = "Open the info with "
                + "`await page.Tb().Popover(\"Information about Project code\").OpenAsync()`, which returns its panel. "
                + "The info button's name contains the label, so find the control by role with an exact name, as "
                + "`GetByRole(AriaRole.Textbox, new() { Name = \"Project code\", Exact = true })`.",
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
                "`Type` sets the role: a text box, a search box for `Search`, and none for `Password`, as "
                    + "browsers expose them.",
            ],
            Target = new("textbox", "Name"),
            Blocks = ["tb-field"],
        },
        new("number-field", "Number field", ExampleCategory.Inputs,
            "A labeled number input with step buttons and limits.", typeof(NumberFieldExample))
        {
            Api = [typeof(TbNumberField<>), typeof(FieldClasses)],
            Accessibility =
            [
                "A native number input, a `spinbutton`: Up and Down step the value by `Step`.",
                "The Decrease and Increase buttons are left out of the tab order, since the arrow keys do the "
                    + "same, and they turn off at `Min` and `Max`.",
                "A value out of range, or one that isn't a number, shows as an error with `aria-invalid`.",
                "The `Label` names the field. Helper text and validation messages are linked to the field with "
                    + "`aria-describedby`, an error sets `aria-invalid`, and `Required` sets `aria-required`; the "
                    + "asterisk it adds is hidden from screen readers.",
            ],
            Target = new("spinbutton", "Quantity"),
            Blocks = ["tb-field"],
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
            ],
            Target = new("checkbox", "Email me updates"),
            Blocks = ["tb-checkbox"],
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
            ],
            Target = new("switch", "Auto-save"),
            Blocks = ["tb-switch"],
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
                    + "and `EndLabel`), each a tab stop of its own, and neither can pass the other.",
                "It posts in a static form, as a number or as `Start` and `End`. In forced-colors mode the track and "
                    + "its fill take system colors.",
            ],
            Target = new("slider", "Map brightness"),
            Blocks = ["tb-slider"],
            TestingTip = "Set a slider with `await page.GetByRole(AriaRole.Slider, new() { Name = \"Map brightness\" "
                + "}).FillAsync(\"80\")`, which Playwright supports for range inputs, and check it with "
                + "`ToHaveValueAsync(\"80\")`. A range slider's thumbs are named \"Elevation Minimum\" and "
                + "\"Elevation Maximum\".",
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
                "In forced-colors mode it falls back to the system's own control, so it stays visible in every "
                    + "high-contrast theme.",
            ],
            Target = new("radiogroup", "Paper size"),
            Blocks = ["tb-radio-group", "tb-radio"],
        },
        new("select", "Select", ExampleCategory.Inputs,
            "Picks one or several options from a list that opens under the field.", typeof(SelectExample))
        {
            Api = [typeof(TbSelect<>), typeof(TbMultiSelect<>), typeof(TbOption<>), typeof(FieldClasses)],
            Accessibility =
            [
                "A `combobox` named by its `Label` that opens a `listbox` of `option`s. Focus stays on the "
                    + "combobox, which points to the active option with `aria-activedescendant`.",
                "While closed: Down, Up, Enter or Space opens it, Home and End open it on the first or last "
                    + "option, and typing jumps to an option that starts with what was typed.",
                "While open: the arrows, Home and End move; Enter or Space chooses; Escape closes. In a single "
                    + "select, Tab chooses the active option and moves on. A multi-select stays open as options are "
                    + "chosen.",
                "Before the page is interactive it renders as the browser's own `<select>`, so it still works.",
                "The clear button is named \"Clear\" and sends focus back to the field.",
            ],
            Target = new("combobox", "Shipping"),
            Blocks = ["tb-field", "tb-select"],
            TestingTip = "To choose options, `await page.Tb().Select(\"Shipping\").ChooseAsync(\"Express (1 day)\")` "
                + "from `Trailblazor.Testing.Playwright` opens the list, picks them and waits for the page, and it "
                + "works on the native `<select>` too.",
        },
        new("listbox", "List box", ExampleCategory.Inputs,
            "Picks one or several options from a list that is always shown.", typeof(ListBoxExample))
        {
            Api = [typeof(TbListBox<>), typeof(TbMultiListBox<>), typeof(TbOption<>), typeof(FieldClasses)],
            Accessibility =
            [
                "A `listbox` of `option`s named by its `Label`. It is one tab stop; focus stays on the listbox, which "
                    + "points to the active option with `aria-activedescendant`.",
                "Down and Up move, Home and End go to the first and last option, and typing jumps to an option that "
                    + "starts with what was typed. Disabled options are skipped.",
                "In a single listbox, moving chooses the option, as in the browser's own list. In a multiple one, "
                    + "Space chooses or clears the active option, and Shift with Down or Up also chooses the next.",
                "Before the page is interactive it renders as the browser's own `<select>` list, so it still works.",
            ],
            Target = new("listbox", "Trail"),
            Blocks = ["tb-field", "tb-listbox"],
            TestingTip = "To choose options, `await page.Tb().ListBox(\"Gear to pack\").ChooseAsync(\"Stove\")` from "
                + "`Trailblazor.Testing.Playwright` picks them and waits for the page, and it works on the native "
                + "`<select>` too.",
        },
        new("tooltip", "Tooltip", ExampleCategory.Overlays,
            "A short description that appears on hover or focus.", typeof(TooltipExample))
        {
            Api = [typeof(TbTooltip)],
            Accessibility =
            [
                "The tooltip describes its anchor: its text goes into the `aria-describedby` of the first "
                    + "focusable element inside. It's a description, not a name, so that element still needs a name of "
                    + "its own. A tooltip that only repeats that element's `aria-label` isn't added, so it isn't read "
                    + "twice.",
                "It shows at once on focus and after a short delay on hover. Moving the pointer onto the tooltip "
                    + "keeps it open, and Escape hides it.",
            ],
            Target = new("tooltip", "Add an item") { OpenedBy = new("button", "Add"), OpenOnHover = true },
            Blocks = ["tb-tooltip", "tb-tooltip-anchor"],
        },
        new("menu", "Menu", ExampleCategory.Overlays,
            "A list of actions that opens from a button, with submenus.", typeof(MenuExample))
        {
            Api = [typeof(TbMenu), typeof(TbMenuItem), typeof(TbSubMenu)],
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
            ],
            Target = new("button", "Actions"),
            Blocks = ["tb-menu"],
        },
        new("dialog", "Dialog", ExampleCategory.Overlays,
            "A window over the page, opened from code, that asks for input or a decision.",
            typeof(DialogExample), typeof(RenameDialog))
        {
            Api = [typeof(DialogOptions), typeof(DialogSettings), typeof(TbDialogActions)],
            Accessibility =
            [
                "A native `<dialog>` named by its title. A modal one makes the rest of the page inert, so focus "
                    + "and screen readers stay inside; a non-modal one leaves the page usable.",
                "On opening, focus goes to an element marked `autofocus`, or else the first field, or the dialog "
                    + "itself with `InitialFocus = DialogFocus.Dialog`. On closing, it goes back to whatever opened "
                    + "the dialog.",
                "Escape cancels the topmost dialog unless `DismissOnEscape` is false; a menu or select open "
                    + "inside it closes first. A click that starts and ends on the backdrop cancels a modal unless "
                    + "`DismissOnBackdrop` is false.",
                "`ConfirmAsync` opens an `alertdialog` described by its message.",
                "A dialog that isn't modal can be `Draggable` by its header and `Resizable` by its bottom-end grip. "
                    + "Neither needs dragging: the header gains a Move button whose menu picks a place, and the grip "
                    + "is a Resize button whose menu picks a size. While either button has focus, the arrow keys "
                    + "move or resize the dialog instead of opening the menu, and its description says so.",
                "Toasts and announcements made while a modal is open appear inside it, so the inert page doesn't "
                    + "hide them.",
            ],
            Target = new("dialog", "Rename report") { OpenedBy = new("button", "Rename report") },
            Blocks = ["tb-dialog"],
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
                "A `tablist` of `tab`s, each controlling a `tabpanel`. Give the list an `AriaLabel`.",
                "The tab list is one tab stop. Left and Right move between tabs and wrap, mirrored on "
                    + "right-to-left pages, and Home and End jump to the ends. With automatic activation, moving also "
                    + "selects; with manual activation, Enter or Space selects.",
                "Each panel is focusable, so keyboard users can reach content that has no controls.",
                "In forced-colors mode, such as Windows high contrast, the selected tab keeps its underline in the "
                    + "system's highlight color.",
            ],
            Target = new("tablist", "Report sections"),
            Blocks = ["tb-tabs"],
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
                "Tab moves between the titles, and Enter or Space opens and closes them.",
            ],
            Target = new("button", "Shipping"),
            Blocks = ["tb-accordion"],
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
                    + "Previous, Next, First and Last buttons are named, and turn off at either end. The ellipses are "
                    + "hidden from screen readers.",
                "Every control is its own tab stop, as in a list of links. In compact mode the page box is named "
                    + "\"Page\"; typing a page and pressing Enter, or leaving the box, goes there.",
                "With `PageHref` every item is a link, so the pages work before the page is interactive. All the "
                    + "words come from `Labels`.",
            ],
            Target = new("navigation", "Trail reports"),
            Blocks = ["tb-pagination"],
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
                "In forced-colors mode, such as Windows high contrast, both kinds take the system's link color.",
            ],
            Target = new("link", "theming guide"),
            Blocks = ["tb-link"],
            TestingTip = "A link with an `Href` has the `link` role and one without has the `button` role, so find it "
                + "with the role that matches.",
        },
        new("skeleton", "Skeleton", ExampleCategory.Feedback,
            "A pulsing gray shape that holds the place of content while it loads.", typeof(SkeletonExample))
        {
            Api = [typeof(TbSkeleton)],
            Accessibility =
            [
                "A skeleton is hidden from screen readers, because it has nothing to say. Mark the region that is "
                    + "loading with `aria-busy=\"true\"`, and set it back to `false` when the content arrives.",
                "With reduced motion, the pulse stops and the shapes stay still.",
                "In forced-colors mode, such as Windows high contrast, each shape keeps an outline.",
            ],
            Target = null,
            Blocks = ["tb-skeleton"],
            TestingTip = "Skeletons have no role, so wait for the loaded content rather than for them to go, or check "
                + "that the region's `aria-busy` is `false`.",
        },
        new("toast", "Toast", ExampleCategory.Feedback,
            "A brief message in a corner that doesn't interrupt, with an optional action.", typeof(ToastExample))
        {
            Api = [typeof(ToastOptions), typeof(ToastSettings)],
            Accessibility =
            [
                "Toasts are read out as they appear: Danger and Warning interrupt, the others wait. They never "
                    + "take focus themselves.",
                "F8 moves focus to the newest toast's first button, and Escape sends it back to where it was.",
                "A toast's timer pauses while the pointer is over it or it has focus, and Danger toasts stay "
                    + "until they are dismissed unless given a `Duration`.",
                "The toasts sit in a region named \"Notifications\".",
            ],
            Target = new("listitem") { Text = "Report saved", OpenedBy = new("button", "Save report") },
            Blocks = ["tb-toast", "tb-toaster"],
            TestingTip = "Each toast is an item in the region named `Notifications`.",
        },
    ];

    public static Example? Find(string name) => All.FirstOrDefault(example => example.Name == name);
}
