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

    public IReadOnlyList<ExampleSource> Sources => [.. new[] { Component }.Concat(Parts).Select(ExampleSource.Of)];
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
        },
        new("divider", "Divider", ExampleCategory.Layout,
            "A line that separates content, across or down.", typeof(DividerExample))
        {
            Api = [typeof(TbDivider)],
        },
        new("surface", "Surface", ExampleCategory.Layout,
            "A raised panel with the app's surface color, an optional border and a shadow, for grouping content.",
            typeof(SurfaceExample))
        {
            Api = [typeof(TbSurface)],
        },
        new("icon", "Icon", ExampleCategory.Content,
            "An SVG icon from any icon set, sized to the text around it and hidden from screen readers unless named.",
            typeof(IconExample))
        {
            Api = [typeof(TbIcon)],
        },
        new("button", "Button", ExampleCategory.Actions,
            "Starts an action, in filled, outlined or text styles and every tone.", typeof(ButtonExample))
        {
            Api = [typeof(TbButton)],
        },
        new("icon-button", "Icon button", ExampleCategory.Actions,
            "A button that shows only an icon, with the accessible name it needs.", typeof(IconButtonExample))
        {
            Api = [typeof(TbIconButton)],
        },
        new("button-group", "Button group", ExampleCategory.Actions,
            "Joins related buttons into one control.", typeof(ButtonGroupExample))
        {
            Api = [typeof(TbButtonGroup)],
        },
        new("stack", "Stack", ExampleCategory.Layout,
            "Lays its children out in a row or a column, evenly spaced.", typeof(StackExample))
        {
            Api = [typeof(TbStack)],
        },
        new("spacer", "Spacer", ExampleCategory.Layout,
            "Takes up the free space in a stack, pushing what follows to the end.", typeof(SpacerExample))
        {
            Api = [typeof(TbSpacer)],
        },
        new("grid", "Grid", ExampleCategory.Layout,
            "A responsive 12-column grid for page layouts.", typeof(GridExample))
        {
            Api = [typeof(TbGrid), typeof(TbGridItem)],
        },
        new("container", "Container", ExampleCategory.Layout,
            "Centers content and caps its width at a readable size.", typeof(ContainerExample))
        {
            Api = [typeof(TbContainer)],
        },
        new("alert", "Alert", ExampleCategory.Feedback,
            "A message in the page, such as a warning or an error, that can be closed.", typeof(AlertExample))
        {
            Api = [typeof(TbAlert)],
        },
        new("progress", "Progress", ExampleCategory.Feedback,
            "Shows how far along work is, as a bar or a circle, or that it is still going.", typeof(ProgressExample))
        {
            Api = [typeof(TbProgress)],
        },
        new("text-field", "Text field", ExampleCategory.Inputs,
            "A labeled text input with help text and validation messages.", typeof(TextFieldExample))
        {
            Api = [typeof(TbTextField<>)],
        },
        new("number-field", "Number field", ExampleCategory.Inputs,
            "A labeled number input with step buttons and limits.", typeof(NumberFieldExample))
        {
            Api = [typeof(TbNumberField<>)],
        },
        new("checkbox", "Checkbox", ExampleCategory.Inputs,
            "Turns an option on or off, or shows that only some of a group are on.", typeof(CheckboxExample))
        {
            Api = [typeof(TbCheckbox<>)],
        },
        new("switch", "Switch", ExampleCategory.Inputs,
            "Turns a setting on or off right away.", typeof(SwitchExample))
        {
            Api = [typeof(TbSwitch)],
        },
        new("radio-group", "Radio group", ExampleCategory.Inputs,
            "Picks one option from a short list shown all at once.", typeof(RadioGroupExample))
        {
            Api = [typeof(TbRadioGroup<>), typeof(TbRadio<>)],
        },
        new("select", "Select", ExampleCategory.Inputs,
            "Picks one or several options from a list that opens under the field.", typeof(SelectExample))
        {
            Api = [typeof(TbSelect<>), typeof(TbMultiSelect<>), typeof(TbOption<>)],
        },
        new("tooltip", "Tooltip", ExampleCategory.Overlays,
            "A short description that appears on hover or focus.", typeof(TooltipExample))
        {
            Api = [typeof(TbTooltip)],
        },
        new("menu", "Menu", ExampleCategory.Overlays,
            "A list of actions that opens from a button, with submenus.", typeof(MenuExample))
        {
            Api = [typeof(TbMenu), typeof(TbMenuItem), typeof(TbSubMenu)],
        },
        new("dialog", "Dialog", ExampleCategory.Overlays,
            "A window over the page, opened from code, that asks for input or a decision.",
            typeof(DialogExample), typeof(RenameDialog))
        {
            Api = [typeof(DialogOptions), typeof(DialogSettings), typeof(TbDialogActions)],
        },
        new("tabs", "Tabs", ExampleCategory.Navigation,
            "Switches between panels of related content, one at a time.", typeof(TabsExample))
        {
            Api = [typeof(TbTabs), typeof(TbTab)],
        },
        new("accordion", "Accordion", ExampleCategory.Navigation,
            "Sections that open and close under their headings.", typeof(AccordionExample))
        {
            Api = [typeof(TbAccordion), typeof(TbAccordionItem)],
        },
        new("toast", "Toast", ExampleCategory.Feedback,
            "A brief message in a corner that doesn't interrupt, with an optional action.", typeof(ToastExample))
        {
            Api = [typeof(ToastOptions), typeof(ToastSettings)],
        },
    ];

    public static Example? Find(string name) => All.FirstOrDefault(example => example.Name == name);
}
