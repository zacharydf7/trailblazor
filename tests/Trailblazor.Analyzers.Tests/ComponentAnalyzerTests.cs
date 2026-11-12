using static Trailblazor.Analyzers.Tests.Analysis;

namespace Trailblazor.Analyzers.Tests;

public class ComponentAnalyzerTests
{
    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNoAccessibleName_WhenAnIconButtonsAriaLabelIsBlank()
    {
        var ids = await IdsAsync("""
            __builder.OpenComponent<TbIconButton>(0);
            __builder.AddComponentParameter(1, nameof(TbIconButton.Icon), default(Icon));
            __builder.AddComponentParameter(2, nameof(TbIconButton.AriaLabel), " ");
            __builder.CloseComponent();
            """);

        Assert.Equal(["TB001"], ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldLeaveTheNameToRunTime_WhenItIsNotALiteral()
    {
        var ids = await IdsAsync("""
            __builder.OpenComponent<TbIconButton>(0);
            __builder.AddComponentParameter(1, nameof(TbIconButton.Icon), default(Icon));
            __builder.AddComponentParameter(2, nameof(TbIconButton.AriaLabel), _name);
            __builder.CloseComponent();
            """, "private string _name = \"\";");

        Assert.Empty(ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldLeaveAMissingNameToRazor_WhenTheParameterIsEditorRequired()
    {
        var ids = await IdsAsync("""
            __builder.OpenComponent<TbIconButton>(0);
            __builder.AddComponentParameter(1, nameof(TbIconButton.Icon), default(Icon));
            __builder.CloseComponent();
            """);

        Assert.Empty(ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNoAccessibleName_WhenAMenuHasNeitherLabelNorTrigger()
    {
        var diagnostics = await RunAsync("""
            __builder.OpenComponent<TbMenu>(0);
            __builder.AddComponentParameter(1, nameof(TbMenu.Icon), default(Icon));
            __builder.CloseComponent();
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TB001", diagnostic.Id);
        Assert.Equal("TbMenu has no accessible name. Set Label, AriaLabel or Trigger.", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNothing_WhenAMenuHasATrigger()
    {
        var ids = await IdsAsync("""
            __builder.OpenComponent<TbMenu>(0);
            __builder.AddComponentParameter(1, nameof(TbMenu.Trigger),
                (RenderFragment<MenuTrigger>)(trigger => _ => { }));
            __builder.CloseComponent();
            """);

        Assert.Empty(ids);
    }

    [Theory]
    [InlineData(nameof(TbTreeView<string, int>.AriaLabel))]
    [InlineData("aria-labelledby")]
    public async Task ComponentAnalyzer_ShouldReportNothing_WhenATreeIsGivenAName(string name)
    {
        var ids = await IdsAsync($$"""
            __builder.OpenComponent<TbTreeView<string, int>>(0);
            __builder.AddComponentParameter(1, "{{name}}", "Trails");
            __builder.CloseComponent();
            """);

        Assert.Empty(ids);
    }

    // The tree's own aria-label, from AriaLabel, replaces one written on the component.
    [Theory]
    [InlineData(nameof(TbTreeView<string, int>.Items), "new[] { \"North\" }")]
    [InlineData("aria-label", "\"Trails\"")]
    public async Task ComponentAnalyzer_ShouldReportNoAccessibleName_WhenATreeHasNoNameItUses(string name, string value)
    {
        var ids = await IdsAsync($$"""
            __builder.OpenComponent<TbTreeView<string, int>>(0);
            __builder.AddComponentParameter(1, "{{name}}", {{value}});
            __builder.CloseComponent();
            """);

        Assert.Equal(["TB001"], ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNothing_WhenAttributesAreSplatted()
    {
        var ids = await IdsAsync("""
            __builder.OpenComponent<TbTreeView<string, int>>(0);
            __builder.AddMultipleAttributes(1, _attributes);
            __builder.CloseComponent();
            """, "private Dictionary<string, object> _attributes = new();");

        Assert.Empty(ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReadTheMarkupsValue_WhenAGenericComponentIsBuiltByATypeInferenceHelper()
    {
        var ids = await IdsAsync("""
            TypeInference.CreateTbTreeView_0<string, int>(__builder, 0, 1, "");
            """, """
            private static class TypeInference
            {
                public static void CreateTbTreeView_0<TItem, TKey>(RenderTreeBuilder __builder, int seq, int __seq0,
                    string __arg0) where TKey : notnull
                {
                    __builder.OpenComponent<TbTreeView<TItem, TKey>>(seq);
                    __builder.AddComponentParameter(__seq0, nameof(TbTreeView<TItem, TKey>.AriaLabel), __arg0);
                    __builder.CloseComponent();
                }
            }
            """);

        Assert.Equal(["TB001"], ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldPointAtTheMarkup_WhenRazorMapsTheParameterName()
    {
        var diagnostics = await RunAsync("""
            __builder.OpenComponent<TbIconButton>(0);
            __builder.AddComponentParameter(1, nameof(TbIconButton.
            #line (4,15)-(4,24) "Page.razor"
            AriaLabel
            #line default
            #line hidden
            ), "");
            __builder.CloseComponent();
            """);

        var span = Assert.Single(diagnostics).Location.GetMappedLineSpan();
        Assert.Equal(("Page.razor", 3, 14), (span.Path, span.StartLinePosition.Line, span.StartLinePosition.Character));
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNoDialogTitle_WhenATbDialogsTitleIsBlank()
    {
        var ids = await IdsAsync("""
            __builder.OpenComponent<TbDialog>(0);
            __builder.AddComponentParameter(1, nameof(TbDialog.Title), "");
            __builder.CloseComponent();
            """);

        Assert.Equal(["TB002"], ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNoDialogTitle_WhenTheDialogServiceIsGivenABlankTitle()
    {
        var ids = await IdsAsync("", """
            [Inject]
            private IDialogService Dialogs { get; set; } = default!;

            private Task<bool> DeleteAsync() => Dialogs.ConfirmAsync("", "Delete the trail?");
            """);

        Assert.Equal(["TB002"], ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNoDialogTitle_WhenADialogWithoutADefaultTitleIsOpenedWithoutOne()
    {
        var ids = await IdsAsync("", """
            [Inject]
            private IDialogService Dialogs { get; set; } = default!;

            private Task<bool> ShowAsync() => Dialogs.ShowAsync<NoticeDialog>();

            private sealed class NoticeDialog : TbDialogContent;
            """);

        Assert.Equal(["TB002"], ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNothing_WhenTheDialogsContentOverridesDefaultTitle()
    {
        var ids = await IdsAsync("", """
            [Inject]
            private IDialogService Dialogs { get; set; } = default!;

            private Task<bool> ShowAsync() => Dialogs.ShowAsync<NoticeDialog>();

            private sealed class NoticeDialog : TbDialogContent
            {
                protected override string DefaultTitle => "Trail notice";
            }
            """);

        Assert.Empty(ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNothing_WhenTheDialogTypeIsATypeParameter()
    {
        var ids = await IdsAsync("", """
            [Inject]
            private IDialogService Dialogs { get; set; } = default!;

            private Task<bool> ShowAsync<TNotice>() where TNotice : TbDialogContent => Dialogs.ShowAsync<TNotice>();
            """);

        Assert.Empty(ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportValueWithoutValueChanged_WhenAnInputIsGivenOnlyAValue()
    {
        var ids = await IdsAsync("""
            __builder.OpenComponent<TbTextField<string>>(0);
            __builder.AddComponentParameter(1, nameof(TbTextField<string>.Label), "Trail");
            __builder.AddComponentParameter(2, nameof(TbTextField<string>.Value), "Pine Hollow");
            __builder.CloseComponent();
            """);

        Assert.Equal(["TB003"], ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNothing_WhenAnInputsValueIsBound()
    {
        var ids = await IdsAsync("""
            __builder.OpenComponent<TbTextField<string>>(0);
            __builder.AddComponentParameter(1, nameof(TbTextField<string>.Label), "Trail");
            __builder.AddComponentParameter(2, nameof(TbTextField<string>.Value), _trail);
            __builder.AddComponentParameter(3, nameof(TbTextField<string>.ValueChanged),
                EventCallback.Factory.Create<string>(this, value => _trail = value));
            __builder.CloseComponent();
            """, "private string _trail = \"\";");

        Assert.Empty(ids);
    }

    [Theory]
    [InlineData("ReadOnly", "true")]
    [InlineData("Disabled", "_locked")]
    public async Task ComponentAnalyzer_ShouldReportNothing_WhenAnInputWithOnlyAValueCantBeChanged(string name,
        string value)
    {
        var ids = await IdsAsync($$"""
            __builder.OpenComponent<TbTextField<string>>(0);
            __builder.AddComponentParameter(1, nameof(TbTextField<string>.Label), "Trail");
            __builder.AddComponentParameter(2, nameof(TbTextField<string>.Value), "Pine Hollow");
            __builder.AddComponentParameter(3, "{{name}}", {{value}});
            __builder.CloseComponent();
            """, "private bool _locked = true;");

        Assert.Empty(ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportValueWithoutValueChanged_WhenDisabledIsFalse()
    {
        var ids = await IdsAsync("""
            __builder.OpenComponent<TbCheckbox<bool>>(0);
            __builder.AddComponentParameter(1, nameof(TbCheckbox<bool>.Label), "Share");
            __builder.AddComponentParameter(2, nameof(TbCheckbox<bool>.Value), true);
            __builder.AddComponentParameter(3, nameof(TbCheckbox<bool>.Disabled), false);
            __builder.CloseComponent();
            """);

        Assert.Equal(["TB003"], ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportValueWithoutValueChanged_WhenReadOnlyIsNotTheInputsParameter()
    {
        var diagnostics = await RunAsync("""
            __builder.OpenComponent<TbToggleGroup<string>>(0);
            __builder.AddComponentParameter(1, nameof(TbToggleGroup<string>.AriaLabel), "View");
            __builder.AddComponentParameter(2, nameof(TbToggleGroup<string>.Value), "list");
            __builder.AddComponentParameter(3, "ReadOnly", true);
            __builder.CloseComponent();
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TB003", diagnostic.Id);
        Assert.EndsWith("Use @bind-Value or handle ValueChanged.", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNothing_WhenAComponentOutsideTrailblazorIsGivenOnlyAValue()
    {
        var ids = await IdsAsync("""
            __builder.OpenComponent<Rating>(0);
            __builder.AddComponentParameter(1, nameof(Rating.Value), 3);
            __builder.CloseComponent();
            """, """
            public class Rating : ComponentBase
            {
                [Parameter]
                public int Value { get; set; }

                [Parameter]
                public EventCallback<int> ValueChanged { get; set; }
            }
            """);

        Assert.Empty(ids);
    }

    // An aria attribute written on a field goes on its wrapper, not its input.
    [Theory]
    [InlineData("""
        __builder.AddComponentParameter(1, nameof(TbTextField<string>.HelperText), "Where you start.");
        """)]
    [InlineData("""
        __builder.AddComponentParameter(1, nameof(TbTextField<string>.Label), "");
        """)]
    [InlineData("""
        __builder.AddComponentParameter(1, "aria-labelledby", "trailhead-label");
        """)]
    public async Task ComponentAnalyzer_ShouldReportNoFieldLabel_WhenAFieldHasNoLabel(string parameters)
    {
        var ids = await IdsAsync($$"""
            __builder.OpenComponent<TbTextField<string>>(0);
            {{parameters}}
            __builder.CloseComponent();
            """);

        Assert.Equal(["TB004"], ids);
    }

    [Theory]
    [InlineData("AriaLabel", "\"Trailhead\"")]
    [InlineData("InputAttributes", "new Dictionary<string, object> { [\"aria-label\"] = \"Trailhead\" }")]
    [InlineData("id", "\"trailhead\"")]
    public async Task ComponentAnalyzer_ShouldReportNothing_WhenAFieldIsNamedAnotherWay(string name, string value)
    {
        var ids = await IdsAsync($$"""
            __builder.OpenComponent<TbTextField<string>>(0);
            __builder.AddComponentParameter(1, "{{name}}", {{value}});
            __builder.CloseComponent();
            """);

        Assert.Empty(ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNoFieldLabel_WhenASelectIsGivenOnlyAnId()
    {
        var ids = await IdsAsync("""
            __builder.OpenComponent<TbSelect<string>>(0);
            __builder.AddComponentParameter(1, "id", "size");
            __builder.CloseComponent();
            """);

        Assert.Equal(["TB004"], ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNothing_WhenARadioGroupIsNamedByAnotherElement()
    {
        var ids = await IdsAsync("""
            __builder.OpenComponent<TbRadioGroup<string>>(0);
            __builder.AddComponentParameter(1, nameof(TbRadioGroup<string>.Value), _speed);
            __builder.AddComponentParameter(2, nameof(TbRadioGroup<string>.ValueChanged),
                EventCallback.Factory.Create<string>(this, value => _speed = value));
            __builder.AddComponentParameter(3, "aria-labelledby", "speed-label");
            __builder.CloseComponent();
            """, "private string _speed = \"standard\";");

        Assert.Empty(ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNothing_WhenACheckboxIsNamedByItsContent()
    {
        var ids = await IdsAsync("""
            __builder.OpenComponent<TbCheckbox<bool>>(0);
            __builder.AddComponentParameter(1, nameof(TbCheckbox<bool>.Value), _share);
            __builder.AddComponentParameter(2, nameof(TbCheckbox<bool>.ValueChanged),
                EventCallback.Factory.Create<bool>(this, value => _share = value));
            __builder.AddAttribute(3, "ChildContent", (RenderFragment)(content => content.AddContent(4, "Share")));
            __builder.CloseComponent();
            """, "private bool _share;");

        Assert.Empty(ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportTypeParameterAsAttribute_WhenAGenericComponentIsGivenT()
    {
        var diagnostics = await RunAsync("""
            __builder.OpenComponent<TbCheckbox<bool>>(0);
            __builder.AddComponentParameter(1, nameof(TbCheckbox<bool>.Label), "Agree");
            __builder.AddComponentParameter(2, "T", "bool");
            __builder.CloseComponent();
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TB005", diagnostic.Id);
        Assert.Equal("TbCheckbox has no T parameter, so T doesn't set its type. To set the type, write "
            + "TValue=\"bool\".", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldNameEachTypeParameter_WhenAComponentHasSeveral()
    {
        var diagnostics = await RunAsync("""
            __builder.OpenComponent<TbTreeView<string, int>>(0);
            __builder.AddComponentParameter(1, nameof(TbTreeView<string, int>.AriaLabel), "Trails");
            __builder.AddComponentParameter(2, "TId", "int");
            __builder.CloseComponent();
            """);

        Assert.EndsWith("write TItem=\"int\" or TKey=\"int\".", Assert.Single(diagnostics).GetMessage());
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNothing_WhenAGenericComponentOutsideTrailblazorIsGivenT()
    {
        var ids = await IdsAsync("""
            __builder.OpenComponent<Badge<int>>(0);
            __builder.AddComponentParameter(1, "T", "int");
            __builder.CloseComponent();
            """, """
            public class Badge<TValue> : ComponentBase
            {
                [Parameter(CaptureUnmatchedValues = true)]
                public Dictionary<string, object>? Attributes { get; set; }
            }
            """);

        Assert.Empty(ids);
    }

    [Theory]
    [InlineData("TItem")]
    [InlineData("TKey")]
    public async Task ComponentAnalyzer_ShouldReportTypeParameterAsAttribute_WhenASelectIsGivenAnotherTypeName(
        string name)
    {
        var ids = await IdsAsync($$"""
            __builder.OpenComponent<TbSelect<string>>(0);
            __builder.AddComponentParameter(1, nameof(TbSelect<string>.Label), "Size");
            __builder.AddComponentParameter(2, "{{name}}", "string");
            __builder.CloseComponent();
            """);

        Assert.Equal(["TB005"], ids);
    }

    [Theory]
    [InlineData("TbTreeView<string, int>", "TKey")]
    [InlineData("TbCheckbox<bool>", "Title")]
    [InlineData("TbCheckbox<bool>", "T-shirt")]
    [InlineData("TbButton", "T")]
    public async Task ComponentAnalyzer_ShouldReportNothing_WhenAnAttributeIsNotAMistypedTypeParameter(
        string component, string name)
    {
        var ids = await IdsAsync($$"""
            __builder.OpenComponent<{{component}}>(0);
            __builder.AddComponentParameter(1, "AriaLabel", "Trails");
            __builder.AddComponentParameter(2, "{{name}}", "string");
            __builder.CloseComponent();
            """);

        Assert.Empty(ids);
    }

    private const string Routed = "[Route(\"/days\")]";

    private const string TabInStack = """
        __builder.OpenComponent<TbStack>(0);
        __builder.AddAttribute(1, "ChildContent", (RenderFragment)((__builder2) => {
            __builder2.OpenComponent<TbTab>(2);
            __builder2.AddComponentParameter(3, nameof(TbTab.Text), "Day one");
            __builder2.CloseComponent();
        }));
        __builder.CloseComponent();
        """;

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportOutsideItsParent_WhenAPagesTabIsInsideAnotherComponentOnly()
    {
        var diagnostics = await RunAsync(TabInStack, attributes: Routed);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TB006", diagnostic.Id);
        Assert.Equal("TbTab isn't inside a TbTabs, so it throws as it renders", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNothing_WhenATabsContentHoldsTheTabThroughAnotherComponent()
    {
        var ids = await IdsAsync($$"""
            __builder.OpenComponent<TbTabs>(10);
            __builder.AddComponentParameter(11, nameof(TbTabs.AriaLabel), "Days");
            __builder.AddAttribute(12, "ChildContent", (RenderFragment)((__builder) => {
            {{TabInStack}}
            }));
            __builder.CloseComponent();
            """, attributes: Routed);

        Assert.Empty(ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNothing_WhenTheTabIsInsideAComponentOutsideTrailblazor()
    {
        var ids = await IdsAsync("""
            __builder.OpenComponent<Layout>(0);
            __builder.AddAttribute(1, "ChildContent", (RenderFragment)((__builder2) => {
                __builder2.OpenComponent<TbTab>(2);
                __builder2.AddComponentParameter(3, nameof(TbTab.Text), "Day one");
                __builder2.CloseComponent();
            }));
            __builder.CloseComponent();
            """, attributes: Routed, members: """
            // It may render its content inside a TbTabs of its own.
            private sealed class Layout : ComponentBase
            {
                [Parameter]
                public RenderFragment? ChildContent { get; set; }
            }
            """);

        Assert.Empty(ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNothing_WhenAComponentsOwnMarkupHasATabInAStack()
    {
        var ids = await IdsAsync(TabInStack);

        Assert.Empty(ids);
    }

    [Theory]
    [InlineData("TbDialog", "")]
    [InlineData("TbDrawer", "")]
    [InlineData("TbStack", "TB006")]
    public async Task ComponentAnalyzer_ShouldTakeADrawerAsADialog_WhenItHoldsADialogTitle(string holder,
        string expected)
    {
        var ids = await IdsAsync($$"""
            __builder.OpenComponent<{{holder}}>(0);
            __builder.AddComponentParameter(1, "Title", "Filters");
            __builder.AddAttribute(2, "ChildContent", (RenderFragment)((__builder2) => {
                __builder2.OpenComponent<TbDialogTitle>(3);
                __builder2.AddAttribute(4, "ChildContent", (RenderFragment)(content => content.AddContent(5, "x")));
                __builder2.CloseComponent();
            }));
            __builder.CloseComponent();
            """, attributes: Routed);

        Assert.Equal(expected == "" ? [] : [expected], ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNothing_WhenAComponentsOwnMarkupStartsWithATab()
    {
        var ids = await IdsAsync("""
            __builder.OpenComponent<TbTab>(0);
            __builder.AddComponentParameter(1, nameof(TbTab.Text), "Day one");
            __builder.CloseComponent();
            """);

        Assert.Empty(ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportOutsideItsParent_WhenAPageStartsWithATab()
    {
        var ids = await IdsAsync("""
            __builder.OpenComponent<TbTab>(0);
            __builder.AddComponentParameter(1, nameof(TbTab.Text), "Day one");
            __builder.CloseComponent();
            """, attributes: Routed);

        Assert.Equal(["TB006"], ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNothing_WhenTheTabComesFromARenderFragmentMember()
    {
        var ids = await IdsAsync("""
            __builder.OpenComponent<TbStack>(0);
            __builder.AddAttribute(1, "ChildContent", Day);
            __builder.CloseComponent();
            """, """
            private RenderFragment Day => __builder2 =>
            {
                __builder2.OpenComponent<TbTab>(2);
                __builder2.AddComponentParameter(3, nameof(TbTab.Text), "Day one");
                __builder2.CloseComponent();
            };
            """, Routed);

        Assert.Empty(ids);
    }

    [Fact]
    public async Task ComponentAnalyzer_ShouldReportNothing_WhenTheTabPassesThroughAMethodOnItsWay()
    {
        var ids = await IdsAsync("""
            __builder.OpenComponent<TbStack>(0);
            __builder.AddAttribute(1, "ChildContent", Wrap(__builder2 => {
                __builder2.OpenComponent<TbTab>(2);
                __builder2.AddComponentParameter(3, nameof(TbTab.Text), "Day one");
                __builder2.CloseComponent();
            }));
            __builder.CloseComponent();
            """, """
            // It may render the content inside a TbTabs of its own.
            private static RenderFragment Wrap(RenderFragment content) => content;
            """, Routed);

        Assert.Empty(ids);
    }

    // Razor opens a generic component whose types it infers in a helper, and passes the helper its content.
    [Theory]
    [InlineData("CreateTbTreeView_0", "")]
    [InlineData("CreateTbStack_0", "TB006")]
    public async Task ComponentAnalyzer_ShouldFollowTypeInferenceHelpers_WhenATreeItemsTypesAreInferred(
        string outer, string expected)
    {
        var ids = await IdsAsync($$"""
            TypeInference.{{outer}}(__builder, 0, 1, (__builder2) => {
                TypeInference.CreateTbTreeItem_1(__builder2, 2, 3, "pine");
            });
            """, """
            internal static class TypeInference
            {
                public static void CreateTbTreeView_0(RenderTreeBuilder __builder, int seq, int __seq0,
                    RenderFragment __arg0)
                {
                    __builder.OpenComponent<TbTreeView<string, string>>(seq);
                    __builder.AddComponentParameter(__seq0, "AriaLabel", "Trails");
                    __builder.AddComponentParameter(__seq0, "ChildContent", __arg0);
                    __builder.CloseComponent();
                }

                public static void CreateTbStack_0(RenderTreeBuilder __builder, int seq, int __seq0,
                    RenderFragment __arg0)
                {
                    __builder.OpenComponent<TbStack>(seq);
                    __builder.AddComponentParameter(__seq0, "ChildContent", __arg0);
                    __builder.CloseComponent();
                }

                public static void CreateTbTreeItem_1<TItem>(RenderTreeBuilder __builder, int seq, int __seq0,
                    TItem __arg0)
                {
                    __builder.OpenComponent<TbTreeItem<TItem>>(seq);
                    __builder.AddComponentParameter(__seq0, nameof(TbTreeItem<TItem>.Value), __arg0);
                    __builder.CloseComponent();
                }
            }
            """, Routed);

        Assert.Equal(expected == "" ? [] : [expected], ids);
    }

    [Fact]
    public void ComponentAnalyzer_ShouldNameOnlyComponentsThatExist_WhenItsHostTableIsRead()
    {
        var library = typeof(TbButton).Assembly;

        var missing = ComponentAnalyzer.Hosted.SelectMany(entry => new[] { entry.Child, entry.Host })
            .Where(name => library.GetType(name) is null);

        Assert.Empty(missing);
    }

    [Fact]
    public void ComponentAnalyzer_ShouldNameOnlyComponentsAndParametersThatExist_WhenItsTableIsRead()
    {
        var library = typeof(TbButton).Assembly;
        var tables = ComponentAnalyzer.Named.Concat(ComponentAnalyzer.OtherFields);

        var missing = tables.SelectMany(entry => library.GetType(entry.Type) is { } type
            ? entry.Names.Where(name => char.IsUpper(name[0]) && type.GetProperty(name) is null)
                .Select(name => $"{entry.Type}.{name}")
            : [entry.Type]);

        Assert.Empty(missing);
    }
}
