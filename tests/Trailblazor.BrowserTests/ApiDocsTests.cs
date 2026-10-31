using Trailblazor.Docs.Examples;

namespace Trailblazor.BrowserTests;

public class ApiDocsTests
{
    public static TheoryData<string> Types => new(ExampleCatalog.All.SelectMany(example => example.Api)
        .Select(type => type.AssemblyQualifiedName!));

    [Fact]
    public void Describe_ShouldListAnInterfacesPropertiesEventsAndIndexers_WhenGivenAnInterface()
    {
        var theme = ApiDocs.Describe(typeof(IThemeService)).Members;
        var localizer = ApiDocs.Describe(typeof(ITbLocalizer)).Members;

        Assert.Equal(["Mode { get; }", "Palette { get; }", "event Changed", "LoadAsync()",
            "SetModeAsync(ThemeMode mode)", "SetPaletteAsync(Palette palette)"], theme.Select(member => member.Name));
        Assert.Equal(["this[string key] { get; }"], localizer.Select(member => member.Name));
        Assert.All(theme.Concat(localizer), member => Assert.NotEmpty(member.Description));
    }

    [Fact]
    public void Describe_ShouldListAHandlesPropertiesAndGenericMethods_WhenGivenATypeWithoutAConstructor()
    {
        var reference = ApiDocs.Describe(typeof(DialogReference<>)).Members;
        var dialogs = ApiDocs.Describe(typeof(IDialogService)).Members;

        Assert.Equal(["Title { get; set; }", "Options { get; }", "Modal { get; set; }", "Size { get; set; }",
            "Position { get; set; }", "WindowState { get; set; }", "Result { get; }", "CancelAsync()",
            "CloseAsync(TResult? value)", "Update<TDialog>(Action<DialogParameters<TDialog>> parameters)"],
            reference.Select(member => member.Name));
        Assert.Contains(dialogs, member => member.Name.StartsWith("Show<TDialog, TResult>(string title,",
            StringComparison.Ordinal));
        Assert.All(reference.Concat(dialogs), member => Assert.NotEmpty(member.Description));
    }

    [Theory, MemberData(nameof(Types))]
    public void Describe_ShouldGiveEveryMemberADescription_WhenGivenATypeTheDocsTable(string type)
    {
        var api = ApiDocs.Describe(Type.GetType(type)!);

        Assert.All(api.Members, member => Assert.NotEmpty(member.Description));
    }

    [Theory, MemberData(nameof(Types))]
    public void Describe_ShouldGiveTheTypeASummary_WhenGivenATypeTheDocsTable(string type)
    {
        var api = ApiDocs.Describe(Type.GetType(type)!);

        Assert.NotEmpty(api.Summary);
    }

    [Fact]
    public void Describe_ShouldListAParameterWithItsTypeDefaultAndDescription_WhenGivenAComponent()
    {
        var variant = ApiDocs.Describe(typeof(TbButton)).Members.Single(member => member.Name == "Variant");

        Assert.Equal(("Variant", "Variant.Filled"), (variant.Type, variant.Default));
        Assert.StartsWith("How strongly the button is drawn", variant.Description);
    }

    [Fact]
    public void Describe_ShouldShowTypeParametersAndInheritedParameters_WhenGivenAGenericField()
    {
        var api = ApiDocs.Describe(typeof(TbSelect<>));

        Assert.Equal("TbSelect<TValue>", api.Name);
        Assert.Equal("Value", api.Members[0].Name);
        Assert.Contains(api.Members, member => member is { Name: "Value", Type: "TValue?" });
        Assert.Contains(api.Members, member => member is { Name: "Label", Type: "string?" });
        Assert.DoesNotContain(api.Members, member => member.Name is "Class" or "Style");
    }

    [Fact]
    public void Describe_ShouldMarkARequiredParameter_WhenTheComponentRequiresIt()
    {
        var label = ApiDocs.Describe(typeof(TbChipGroup)).Members.Single(member => member.Name == "AriaLabel");

        Assert.True(label.Required);
    }

    [Fact]
    public void Describe_ShouldListTheSettableProperties_WhenGivenAnOptionsType()
    {
        var api = ApiDocs.Describe(typeof(DialogOptions));

        Assert.Contains(api.Members, member => member is { Name: "Modal", Type: "bool", Default: "true" });
        Assert.Contains(api.Members, member => member is { Name: "DismissOnEscape", Type: "bool?", Default: "" });
    }

    [Fact]
    public void Describe_ShouldListEachMethodWithItsSignatureAndDescription_WhenGivenAServiceInterface()
    {
        var api = ApiDocs.Describe(typeof(IBrowserService));

        Assert.True(api.Methods);
        Assert.NotEmpty(api.Summary);
        Assert.All(api.Members, member => Assert.NotEmpty(member.Description));
        Assert.Contains(api.Members, member => member is
        {
            Name: "SaveAsync(Stream content, string fileName, string? contentType = null)", Type: "Task<bool>",
        });
        Assert.Contains(api.Members, member => member is { Name: "PickColorAsync()", Type: "Task<string?>" });
        Assert.Contains(api.Members, member => member.Name == "ScrollIntoViewAsync(ElementReference element, "
            + "ScrollAlignment alignment = ScrollAlignment.Nearest, bool smooth = false)");
        Assert.Contains(api.Members, member => member is
        {
            Name: "ObserveViewportAsync(Func<Viewport, Task> onChange)", Type: "Task<IAsyncDisposable>",
        });
    }

    [Fact]
    public void Describe_ShouldWriteACrossReferenceAsItsShortName_WhenASummaryHasOne()
    {
        var size = ApiDocs.Describe(typeof(DialogOptions)).Members.Single(member => member.Name == "Size");

        Assert.Contains("DialogSize.Medium", size.Description);
    }
}
