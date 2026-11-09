using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class ApiDocsTests
{
    public static TheoryData<string> Types => new(ExampleCatalog.All.SelectMany(example => example.Api)
        .Select(type => type.AssemblyQualifiedName!));

    [Fact]
    public void Describe_ShouldListAnInterfacesPropertiesEventsAndIndexers_WhenGivenAnInterface()
    {
        var theme = ApiDocs.Describe(typeof(IThemeService)).Members;
        var localizer = ApiDocs.Describe(typeof(ITbLocalizer)).Members;

        Assert.Equal(["Mode { get; }", "Palette { get; }", "Density { get; }", "event Changed", "LoadAsync()",
            "SetModeAsync(ThemeMode mode)", "SetPaletteAsync(Palette palette)", "SetDensityAsync(Density density)"],
            theme.Select(member => member.Name));
        Assert.Equal(["this[string key] { get; }"], localizer.Select(member => member.Name));
        Assert.All(theme.Concat(localizer), member => Assert.NotEmpty(member.Description));
    }

    [Fact]
    public void Describe_ShouldListAHandlesPropertiesAndGenericMethods_WhenGivenATypeWithoutAConstructor()
    {
        var reference = ApiDocs.Describe(typeof(DialogReference<>)).Members;
        var dialogs = ApiDocs.Describe(typeof(IDialogService)).Members;

        Assert.Equal(["Title { get; set; }", "Options { get; }", "Modal { get; set; }", "Size { get; set; }",
            "Position { get; set; }", "WindowState { get; set; }", "ShowHeader { get; set; }",
            "CloseButton { get; set; }", "ContentType { get; }", "Parameters { get; }",
            "Result { get; }", "CancelAsync()",
            "CloseAsync(TResult? value)", "Update<TDialog>(Action<DialogParameters<TDialog>> parameters)"],
            reference.Select(member => member.Name));
        Assert.Contains(dialogs, member => member.Name.StartsWith("Show<TDialog, TResult>(string? title = null,",
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

        Assert.Equal(["Methods"], api.Groups.Select(group => group.Name));
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
    public void Describe_ShouldListPublicMethodsButNotThoseEveryComponentHas_WhenGivenAComponent()
    {
        var grid = ApiDocs.Describe(typeof(TbDataGrid<>)).Groups.Single(group => group.Name == "Methods").Members;
        var button = ApiDocs.Describe(typeof(TbButton)).Groups.Single(group => group.Name == "Methods").Members;

        Assert.Contains(grid, member => member is { Name: "RefreshAsync()", Type: "Task" });
        Assert.Contains(grid, member => member is { Name: "RenderRows()", Type: "void" });
        Assert.Contains(grid, member => member.Name.StartsWith("ScrollToAsync(", StringComparison.Ordinal)
            && member.Id == "tbdatagrid-scrolltoasync");
        Assert.Contains(button, member => member.Name.StartsWith("FocusAsync(", StringComparison.Ordinal));
        Assert.DoesNotContain(grid.Concat(button),
            member => member.Name.StartsWith("SetParametersAsync", StringComparison.Ordinal)
                || member.Name.StartsWith("Dispose", StringComparison.Ordinal));
    }

    [Fact]
    public void Describe_ShouldGroupValuesContentEventsAndMethods_WhenGivenAComponent()
    {
        var groups = ApiDocs.Describe(typeof(TbButton)).Groups;

        Assert.Equal(["Parameters", "Content", "Events", "Methods"], groups.Select(group => group.Name));
        Assert.Equal([true, false, false, false], groups.Select(group => group.Defaults));
        Assert.Contains(groups[1].Members, member => member.Name == "ChildContent");
        Assert.Contains(groups[2].Members,
            member => member is { Name: "OnClick", Type: "EventCallback<MouseEventArgs>" });
        Assert.DoesNotContain(groups[0].Members,
            member => member.Type.StartsWith("EventCallback", StringComparison.Ordinal)
                || member.Type.StartsWith("RenderFragment", StringComparison.Ordinal));
        Assert.Contains(ApiDocs.Describe(typeof(TbChip)).Groups.Single(group => group.Name == "Events").Members,
            member => member is { Name: "OnClose", Type: "EventCallback" });
    }

    [Fact]
    public void Describe_ShouldListEnumValuesAndMarkTheDefault_WhenAParameterIsAnEnum()
    {
        var variant = ApiDocs.Describe(typeof(TbButton)).Members.Single(member => member.Name == "Variant");

        Assert.Equal(Enum.GetNames<Variant>(), variant.Values.Select(value => value.Name));
        Assert.Equal(["Filled"], variant.Values.Where(value => value.Default).Select(value => value.Name));
        Assert.Empty(ApiDocs.Describe(typeof(TbButton)).Members.Single(member => member.Name == "Disabled").Values);
    }

    [Theory, MemberData(nameof(Types))]
    public void Describe_ShouldGiveEveryMemberItsOwnAnchorUnderTheType_WhenGivenATypeTheDocsTable(string type)
    {
        var api = ApiDocs.Describe(Type.GetType(type)!);

        Assert.All(api.Members, member => Assert.StartsWith($"{api.Id}-", member.Id));
        Assert.Equal(api.Members.Count, api.Members.Select(member => member.Id).Distinct().Count());
    }

    [Fact]
    public void Describe_ShouldLinkACrossReference_WhenItsTargetHasATable()
    {
        var items = ApiDocs.Describe(typeof(TbDataGrid<>)).Members.Single(member => member.Name == "Items");

        Assert.Contains("[TbDataGrid.RefreshAsync](components/data-grid/api#tbdatagrid-refreshasync)",
            items.Description);
    }

    [Fact]
    public void Describe_ShouldLinkToTheOverloadsOwnRow_WhenOverloadsDifferOnlyInTypeParameters()
    {
        var members = ApiDocs.Describe(typeof(IDialogService)).Members;
        var show = members.Single(member => member.Name.StartsWith("Show<TDialog>(", StringComparison.Ordinal));

        Assert.Equal("idialogservice-show-1", show.Id);
        Assert.Contains("(components/dialog/api#idialogservice-show-2)", show.Description);
    }

    [Fact]
    public void Describe_ShouldLinkACrossReferenceToAGuide_WhenItsTargetIsTabledThere()
    {
        var check = ApiDocs.Describe(typeof(IBrowserService)).Members
            .Single(member => member.ShortName == "CanPickColorAsync");

        Assert.Contains("[IBrowserService.PickColorAsync](guides/browser#ibrowserservice-pickcolorasync)",
            check.Description);
    }

    [Fact]
    public void Describe_ShouldWriteACrossReferenceAsItsShortName_WhenASummaryHasOne()
    {
        var size = ApiDocs.Describe(typeof(DialogOptions)).Members.Single(member => member.Name == "Size");

        Assert.Contains("DialogSize.Medium", size.Description);
    }
}
