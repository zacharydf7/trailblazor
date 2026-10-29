namespace Trailblazor.Docs.Examples;

public sealed record Example(string Name, string Title, Type Component);

// Every docs example. The docs site shows them, and the browser tests run axe against each one.
public static class ExampleCatalog
{
    public static IReadOnlyList<Example> All { get; } =
    [
        new("text", "Text", typeof(TextExample)),
        new("divider", "Divider", typeof(DividerExample)),
        new("surface", "Surface", typeof(SurfaceExample)),
        new("icon", "Icon", typeof(IconExample)),
        new("button", "Button", typeof(ButtonExample)),
        new("icon-button", "Icon button", typeof(IconButtonExample)),
        new("button-group", "Button group", typeof(ButtonGroupExample)),
        new("stack", "Stack", typeof(StackExample)),
        new("spacer", "Spacer", typeof(SpacerExample)),
        new("grid", "Grid", typeof(GridExample)),
        new("container", "Container", typeof(ContainerExample)),
        new("alert", "Alert", typeof(AlertExample)),
        new("progress", "Progress", typeof(ProgressExample)),
        new("text-field", "Text field", typeof(TextFieldExample)),
        new("number-field", "Number field", typeof(NumberFieldExample)),
        new("checkbox", "Checkbox", typeof(CheckboxExample)),
        new("switch", "Switch", typeof(SwitchExample)),
        new("radio-group", "Radio group", typeof(RadioGroupExample)),
        new("tooltip", "Tooltip", typeof(TooltipExample)),
    ];

    public static Example? Find(string name) => All.FirstOrDefault(example => example.Name == name);
}
