using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Playwright;
using Trailblazor.Testing;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.Tests;

// Both testing packages find each interactive component with the same helper and drive it with the same members; a
// bUnit read such as Text matches its Playwright twin TextAsync.
public class DriverParityTests
{
    private const string InMarkup = "bUnit's markup holds the list whether it's open or not";
    private const string NotInteractive = "not interactive";
    private const string InTestFile = "each package takes its own file made in the test: Playwright's FilePayload and "
        + "bUnit's InputFileContent";

    // The helper in both packages that finds each component.
    internal static readonly Dictionary<string, string> Helpers = new()
    {
        ["TbAccordionItem"] = "AccordionItem", ["TbCalendar"] = "Calendar", ["TbCommandPalette"] = "CommandPalette",
        ["TbContextMenu"] = "ContextMenu", ["TbDataGrid"] = "Grid", ["TbDatePicker"] = "DatePicker",
        ["TbDateRangePicker"] = "DateRangePicker", ["TbDateTimePicker"] = "DateTimePicker", ["TbHotkey"] = "Hotkey",
        ["TbListBox"] = "ListBox", ["TbMultiListBox"] = "ListBox", ["TbMenu"] = "Menu", ["TbCheckbox"] = "Checkbox",
        ["TbSwitch"] = "Switch", ["TbCheckboxGroup"] = "CheckboxGroup", ["TbRadioGroup"] = "RadioGroup",
        ["TbToggleGroup"] = "ToggleGroup", ["TbMultiToggleGroup"] = "ToggleGroup",
        ["TbThemePicker"] = "ToggleGroup", ["TbList"] = "List", ["TbLink"] = "Link",
        ["TbTextField"] = "TextField", ["TbNumberField"] = "NumberField", ["TbPagination"] = "Pagination",
        ["TbPopconfirm"] = "Popconfirm", ["TbPropertyGrid"] = "PropertyGrid", ["TbQueryBuilder"] = "QueryBuilder",
        ["TbRichTextEditor"] = "RichTextEditor", ["TbSelect"] = "Select", ["TbMultiSelect"] = "Select",
        ["TbSignaturePad"] = "SignaturePad", ["TbSortable"] = "Sortable", ["TbTabs"] = "Tabs",
        ["TbTagInput"] = "TagInput", ["TbTransferList"] = "TransferList", ["TbTreeView"] = "Tree",
        ["TbTreeSelect"] = "TreeSelect", ["TbMultiTreeSelect"] = "TreeSelect", ["TbViewport"] = "Viewport",
        ["TbAutocomplete"] = "Autocomplete", ["TbColorPicker"] = "ColorPicker", ["TbDialog"] = "Dialog",
        ["TbFileUpload"] = "FileUpload", ["TbSlider"] = "Slider", ["TbRangeSlider"] = "RangeSlider",
        ["TbRating"] = "Rating", ["TbSplitter"] = "Splitter", ["TbPopover"] = "Popover", ["TbAlert"] = "Alert",
        ["TbBanner"] = "Banner", ["TbBreadcrumbs"] = "Breadcrumbs", ["TbStepper"] = "Stepper",
        ["TbVirtualList"] = "VirtualList", ["TbOverlay"] = "Overlay", ["TbHotkeysHelp"] = "Dialog",
        ["TbFab"] = "Button",
        ["TbTimePicker"] = "TimePicker", ["TbChip"] = "Chip", ["TbChipGroup"] = "ChipGroup",
        ["TbCarousel"] = "Carousel", ["TbChat"] = "Chat", ["TbAiPrompt"] = "AiPrompt", ["TbKanban"] = "Kanban",
        ["TbCascader"] = "Cascader", ["TbMenubar"] = "Menubar", ["TbLightbox"] = "Lightbox", ["TbForm"] = "Form",
        ["TbMarkdownEditor"] = "MarkdownEditor", ["TbMentionInput"] = "MentionInput", ["TbTour"] = "Tour",
        ["TbTooltip"] = "Tooltip", ["TbDrawer"] = "Dialog",
        ["TbSpeedDial"] = "Menu", ["TbToolbar"] = "Toolbar", ["TbInsertDivider"] = "InsertDivider",
        ["TbBackToTop"] = "BackToTop", ["TbSpeechToText"] = "SpeechToText", ["TbSmartPaste"] = "SmartPaste",
        ["TbInPlaceEditor"] = "InPlaceEditor",
        ["TbNavMenu"] = "NavMenu", ["TbCodeField"] = "TextField", ["TbButton"] = "Button",
        ["TbIconButton"] = "Button",
    };

    // Why every other component has no helper of its own.
    private static readonly Dictionary<string, string> Exempt = new()
    {
        ["TbAccordion"] = "its items have the driver", ["TbBreadcrumb"] = "part of TbBreadcrumbs",
        ["TbCarouselSlide"] = "part of TbCarousel", ["TbCheckboxItem"] = "part of TbCheckboxGroup",
        ["TbColumn"] = "part of TbDataGrid", ["TbColumnGroup"] = "part of TbDataGrid",
        ["TbDialogActions"] = "part of TbDialog", ["TbDialogDescription"] = "part of TbDialog",
        ["TbDialogHeader"] = "part of TbDialog", ["TbDialogHeaderActions"] = "part of TbDialog",
        ["TbDialogTitle"] = "part of TbDialog", ["TbGridItem"] = "part of TbGrid", ["TbListItem"] = "part of TbList",
        ["TbMenuCheckboxItem"] = "part of TbMenu", ["TbMenuGroup"] = "part of TbMenu",
        ["TbMenuItem"] = "part of TbMenu",
        ["TbMenuRadioGroup"] = "part of TbMenu", ["TbMenuRadioItem"] = "part of TbMenu",
        ["TbMenuSeparator"] = "part of TbMenu", ["TbSubMenu"] = "part of TbMenu",
        ["TbModelField"] = "draws nothing; it changes a TbModelFields field",
        ["TbModelFields"] = "its fields have drivers",
        ["TbNavGroup"] = "part of TbNavMenu", ["TbNavLink"] = "part of TbNavMenu", ["TbOption"] = "part of TbSelect",
        ["TbOptionGroup"] = "part of TbSelect", ["TbRadio"] = "part of TbRadioGroup",
        ["TbSplitterPane"] = "part of TbSplitter", ["TbStep"] = "part of TbStepper", ["TbTab"] = "part of TbTabs",
        ["TbTableOfContentsItem"] = "part of TbTableOfContents", ["TbTimelineItem"] = "part of TbTimeline",
        ["TbToggleItem"] = "part of TbToggleGroup", ["TbToolbarButton"] = "part of TbToolbar",
        ["TbToolbarSeparator"] = "part of TbToolbar",
        ["TbTreeItem"] = "part of TbTreeView", ["TbTreeItemEnd"] = "part of TbTreeView",

        ["TbAppShell"] = "its menu button is a plain button found by its role, and whether the navigation is open "
            + "or collapsed shows on the page",
        ["TbImage"] = "its OnLoad and OnError report the image loading or failing, not anything a user does",
        ["TbTableOfContents"] = "its entries are plain links found by their role, and ActiveChanged follows scrolling",

        ["TbAppBar"] = NotInteractive, ["TbAvatar"] = NotInteractive,
        ["TbAvatarGroup"] = NotInteractive, ["TbBadge"] = NotInteractive, ["TbBarcode"] = NotInteractive,
        ["TbButtonGroup"] = NotInteractive,
        ["TbCard"] = NotInteractive, ["TbContainer"] = NotInteractive, ["TbDivider"] = NotInteractive,
        ["TbEmptyState"] = NotInteractive, ["TbFieldRow"] = NotInteractive,
        ["TbFocusScope"] = NotInteractive,
        ["TbGrid"] = NotInteractive, ["TbIcon"] = NotInteractive,
        ["TbMasonry"] = NotInteractive,
        ["TbInteractiveMark"] = NotInteractive, ["TbKbd"] = NotInteractive,
        ["TbLabel"] = NotInteractive,
        ["TbProgress"] = NotInteractive, ["TbQrCode"] = NotInteractive, ["TbRoot"] = NotInteractive,
        ["TbSkeleton"] = NotInteractive,
        ["TbSpacer"] = NotInteractive, ["TbStack"] = NotInteractive, ["TbStyles"] = NotInteractive,
        ["TbSurface"] = NotInteractive,
        ["TbTable"] = NotInteractive, ["TbText"] = NotInteractive,
        ["TbHighlight"] = NotInteractive,
        ["TbThemeHead"] = NotInteractive, ["TbTimeline"] = NotInteractive, ["TbWatermark"] = NotInteractive,
    };

    // Members only one package has, by type and member signature, and why.
    private static readonly Dictionary<string, string> OneSided = new()
    {
        ["TrailblazorMarkup.Within(Func`1)"] = "Playwright scopes with new TrailblazorPage(locator)",
        ["TrailblazorPage.Interactive()"] = "bUnit renders interactively from the start",
        ["TrailblazorPage.Announcements()"] = "bUnit tests read FakeAnnouncer",
        ["TbGridLocator.Copy()"] = "pressing Ctrl+C needs the browser's copy event; bUnit reads the same text with "
            + "SelectedCells",
        ["TbFileUploadLocator.Choose(FilePayload[])"] = InTestFile,
        ["TbFileUploadDriver.Choose(InputFileContent[])"] = InTestFile,
        ["TbChatLocator.Attach(FilePayload[])"] = InTestFile,
        ["TbChatDriver.Attach(InputFileContent[])"] = InTestFile,
        ["TbTreeSelectLocator.Open()"] = InMarkup, ["TbTreeSelectLocator.Close()"] = InMarkup,
        ["TbTreeSelectLocator.IsOpen()"] = InMarkup,
        ["TbMarkdownEditorLocator.Press(String)"] = "the editor's script adds the marks, which only a browser runs; bUnit "
            + "enters the markdown with EnterAsync",
        ["TbSpeechToTextDriver.Say(String)"] = "a test browser has no microphone to hear with; Playwright tests "
            + "replace window.SpeechRecognition with a fake",
        ["TbBackToTopLocator.IsShown()"] = "it shows once the page scrolls, which only a browser does",
    };

    [Fact]
    public void Components_ShouldEachHaveAHelperOrAReason_WhenTheLibraryIsScanned()
    {
        var components = typeof(TbButton).Assembly.GetExportedTypes()
            .Where(type => typeof(IComponent).IsAssignableFrom(type) && !type.IsAbstract && type.Name.StartsWith("Tb"))
            .Select(Name).ToHashSet();

        // Each one listed once, in one place or the other, and nothing listed that isn't a component.
        var unsorted = components.Where(component => Helpers.ContainsKey(component) == Exempt.ContainsKey(component));
        var unknown = Helpers.Keys.Concat(Exempt.Keys).Except(components);

        // An exempt component that now has a helper in both packages belongs in Helpers.
        var helped = Exempt.Keys.Where(component => !Helpers.ContainsValue(component[2..])
            && HasHelper(typeof(TrailblazorMarkup), component[2..])
            && HasHelper(typeof(TrailblazorPage), component[2..]));

        Assert.Equal(([], [], []), (unsorted.Order().ToArray(), unknown.Order().ToArray(), helped.Order().ToArray()));
    }

    [Fact]
    public void Exemptions_ShouldNotCallAComponentWithCallbacksNotInteractive_WhenTheLibraryIsScanned()
    {
        var interactive = typeof(TbButton).Assembly.GetExportedTypes()
            .Where(type => Exempt.GetValueOrDefault(Name(type)) == NotInteractive)
            .Where(type => type.GetProperties().Any(property =>
                property.GetCustomAttribute<ParameterAttribute>() is not null
                && property.PropertyType.Name.StartsWith("EventCallback")))
            .Select(Name);

        Assert.Empty(interactive);
    }

    [Fact]
    public void Helpers_ShouldTakeTheSameArguments_WhenComparedAcrossThePackages()
    {
        var missing = new List<string>();
        foreach (var helper in Helpers.Values.Distinct())
        {
            var markup = Signatures(typeof(TrailblazorMarkup), helper);
            var page = Signatures(typeof(TrailblazorPage), helper);
            if (markup.Count == 0 || !markup.SetEquals(page))
            {
                missing.Add($"{helper}: bUnit [{string.Join("; ", markup)}], Playwright [{string.Join("; ", page)}]");
            }
        }

        Assert.True(missing.Count == 0, string.Join(Environment.NewLine, missing));
    }

    [Fact]
    public void Drivers_ShouldHaveTheSameMembers_WhenComparedAcrossThePackages()
    {
        var differences = new List<string>();
        var excused = new HashSet<string>();
        Compare(typeof(TrailblazorMarkup), typeof(TrailblazorPage), new HashSet<(Type, Type)>(), differences, excused);
        // An excuse for a member that both packages now have, or that neither does, is stale.
        differences.AddRange(OneSided.Keys.Except(excused).Select(key => $"{key} is excused but not one-sided"));

        Assert.True(differences.Count == 0, string.Join(Environment.NewLine, differences));
    }

    // Pairs each member by its name without Async and its parameters, a read having none, then compares the drivers
    // the paired members return.
    private static void Compare(Type markup, Type page, HashSet<(Type, Type)> compared, List<string> differences,
        HashSet<string> excused)
    {
        if (!compared.Add((markup, page)))
        {
            return;
        }

        var bunit = Members(markup);
        var playwright = Members(page);
        foreach (var member in bunit.Keys.Union(playwright.Keys).Order())
        {
            var excuse = $"{(bunit.ContainsKey(member) ? markup : page).Name}.{member}";
            if (bunit.TryGetValue(member, out var markupType) && playwright.TryGetValue(member, out var pageType))
            {
                (markupType, pageType) = (Unwrapped(markupType), Unwrapped(pageType));
                if (IsDriver(markupType) && IsDriver(pageType))
                {
                    if (DriverName(markupType) != DriverName(pageType))
                    {
                        differences.Add($"{markup.Name} / {page.Name}: {member} returns {Name(markupType)} in "
                            + $"bUnit and {Name(pageType)} in Playwright");
                    }

                    Compare(markupType, pageType, compared, differences, excused);
                }
                else if (Element(markupType) != Element(pageType))
                {
                    differences.Add($"{markup.Name} / {page.Name}: {member} returns {TypeName(markupType)} in bUnit "
                        + $"and {TypeName(pageType)} in Playwright");
                }
            }
            // A Playwright part such as an input, handed out to assert on with Expect, needs no bUnit twin.
            else if (OneSided.ContainsKey(excuse))
            {
                excused.Add(excuse);
            }
            else if (playwright.GetValueOrDefault(member) != typeof(ILocator))
            {
                differences.Add($"{markup.Name} / {page.Name}: {member} is only in "
                    + (bunit.ContainsKey(member) ? "bUnit" : "Playwright"));
            }
        }
    }

    // A Playwright member that takes a locator points at an element the Playwright way, and has no bUnit twin. Members
    // a field's driver inherits, such as its errors, count too.
    private static Dictionary<string, Type> Members(Type type) => type
        .GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(member => IsDriver(member.DeclaringType!))
        .Where(member => member is PropertyInfo || member is MethodInfo { IsSpecialName: false } method
            && method.GetParameters().All(parameter => parameter.ParameterType != typeof(ILocator)))
        .Select(member => member is PropertyInfo property ? (Key: $"{property.Name}()", Type: property.PropertyType)
            : (Key: $"{Stripped(member.Name)}({string.Join(", ", ((MethodInfo)member).GetParameters()
                .Select(parameter => parameter.ParameterType.Name))})", Type: ((MethodInfo)member).ReturnType))
        // A Playwright read such as TagsAsync() pairs with its bUnit twin ahead of a locator of the same name.
        .OrderBy(member => member.Type == typeof(ILocator)).DistinctBy(member => member.Key)
        .ToDictionary(member => member.Key, member => member.Type);

    private static string Stripped(string name) => name.EndsWith("Async") ? name[..^5] : name;

    // An element bUnit hands out is a locator in Playwright, alone or in a list.
    private static Type Element(Type type) => type == typeof(AngleSharp.Dom.IElement) ? typeof(ILocator)
        : type == typeof(IReadOnlyList<AngleSharp.Dom.IElement>) ? typeof(IReadOnlyList<ILocator>) : type;

    // A Playwright member returns a task where its bUnit twin may not.
    private static Type Unwrapped(Type type) =>
        type == typeof(Task) ? typeof(void) : type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>)
            ? type.GetGenericArguments()[0] : type;

    // TbXDriver pairs with TbXLocator, and the markup with the page.
    private static string DriverName(Type type) => Name(type) switch
    {
        "TrailblazorMarkup" or "TrailblazorPage" => "Trailblazor",
        var name when name.EndsWith("Driver") => name[..^6],
        var name when name.EndsWith("Locator") => name[..^7],
        var name => name,
    };

    private static string TypeName(Type type) => type.IsGenericType
        ? $"{Name(type)}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>" : type.Name;

    private static bool HasHelper(Type type, string name) =>
        type.GetMember(name, BindingFlags.Public | BindingFlags.Instance).Length > 0;

    private static bool IsDriver(Type type) => type.Namespace?.StartsWith("Trailblazor.Testing") == true;

    // An overload that takes a locator is the Playwright way to point at an element, and has no bUnit twin.
    private static HashSet<string> Signatures(Type type, string helper) => type
        .GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(method => method.Name == helper
            && method.GetParameters().All(parameter => parameter.ParameterType != typeof(ILocator)))
        .Select(method => string.Join(", ", method.GetParameters().Select(parameter =>
            $"{parameter.ParameterType.Name} {parameter.Name}{(parameter.IsOptional ? " = default" : "")}")))
        .ToHashSet();

    private static string Name(Type type) => type.Name.Split('`')[0];
}
