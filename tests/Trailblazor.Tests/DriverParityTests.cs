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
    private const string FollowUp = "a driver in both packages follows in #516";
    private const string NotInteractive = "not interactive, or a plain button or link found by its role";

    // The helper in both packages that finds each component.
    internal static readonly Dictionary<string, string> Helpers = new()
    {
        ["TbAccordionItem"] = "AccordionItem", ["TbCalendar"] = "Calendar", ["TbCommandPalette"] = "CommandPalette",
        ["TbContextMenu"] = "ContextMenu", ["TbDataGrid"] = "Grid", ["TbDatePicker"] = "DatePicker",
        ["TbDateRangePicker"] = "DateRangePicker", ["TbDateTimePicker"] = "DateTimePicker", ["TbHotkey"] = "Hotkey",
        ["TbListBox"] = "ListBox", ["TbMultiListBox"] = "ListBox", ["TbMenu"] = "Menu", ["TbCheckbox"] = "Checkbox",
        ["TbSwitch"] = "Switch", ["TbCheckboxGroup"] = "CheckboxGroup", ["TbRadioGroup"] = "RadioGroup",
        ["TbToggleGroup"] = "ToggleGroup", ["TbMultiToggleGroup"] = "ToggleGroup", ["TbList"] = "List",
        ["TbTextField"] = "TextField", ["TbNumberField"] = "NumberField", ["TbPagination"] = "Pagination",
        ["TbPopconfirm"] = "Popconfirm", ["TbPropertyGrid"] = "PropertyGrid", ["TbQueryBuilder"] = "QueryBuilder",
        ["TbRichTextEditor"] = "RichTextEditor", ["TbSelect"] = "Select", ["TbMultiSelect"] = "Select",
        ["TbSignaturePad"] = "SignaturePad", ["TbSortable"] = "Sortable", ["TbTabs"] = "Tabs",
        ["TbTagInput"] = "TagInput", ["TbTransferList"] = "TransferList", ["TbTreeView"] = "Tree",
        ["TbTreeSelect"] = "TreeSelect", ["TbMultiTreeSelect"] = "TreeSelect", ["TbViewport"] = "Viewport",
        ["TbAutocomplete"] = "Autocomplete", ["TbColorPicker"] = "ColorPicker", ["TbDialog"] = "Dialog",
        ["TbFileUpload"] = "FileUpload", ["TbSlider"] = "Slider",
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
        ["TbTreeItem"] = "part of TbTreeView",

        ["TbAppBar"] = NotInteractive, ["TbAppShell"] = NotInteractive, ["TbAvatar"] = NotInteractive,
        ["TbAvatarGroup"] = NotInteractive, ["TbBadge"] = NotInteractive, ["TbButtonGroup"] = NotInteractive,
        ["TbCard"] = NotInteractive, ["TbContainer"] = NotInteractive, ["TbDivider"] = NotInteractive,
        ["TbEmptyState"] = NotInteractive, ["TbFab"] = NotInteractive, ["TbFocusScope"] = NotInteractive,
        ["TbGrid"] = NotInteractive, ["TbHotkeysHelp"] = NotInteractive, ["TbIcon"] = NotInteractive,
        ["TbIconButton"] = NotInteractive, ["TbImage"] = NotInteractive, ["TbKbd"] = NotInteractive,
        ["TbLabel"] = NotInteractive, ["TbLink"] = NotInteractive, ["TbOverlay"] = NotInteractive,
        ["TbProgress"] = NotInteractive, ["TbRoot"] = NotInteractive, ["TbSkeleton"] = NotInteractive,
        ["TbSpacer"] = NotInteractive, ["TbStack"] = NotInteractive, ["TbStyles"] = NotInteractive,
        ["TbSurface"] = NotInteractive,
        ["TbTable"] = NotInteractive, ["TbTableOfContents"] = NotInteractive, ["TbText"] = NotInteractive,
        ["TbThemeHead"] = NotInteractive, ["TbTimeline"] = NotInteractive,

        ["TbAlert"] = FollowUp, ["TbBanner"] = FollowUp, ["TbBreadcrumbs"] = FollowUp, ["TbButton"] = FollowUp,
        ["TbCarousel"] = FollowUp, ["TbChip"] = FollowUp, ["TbChipGroup"] = FollowUp, ["TbCodeField"] = FollowUp,
        ["TbDrawer"] = FollowUp, ["TbForm"] = FollowUp, ["TbInsertDivider"] = FollowUp, ["TbNavMenu"] = FollowUp,
        ["TbPopover"] = FollowUp, ["TbRangeSlider"] = FollowUp, ["TbRating"] = FollowUp, ["TbSpeedDial"] = FollowUp,
        ["TbSplitter"] = FollowUp, ["TbStepper"] = FollowUp, ["TbThemePicker"] = FollowUp,
        ["TbTimePicker"] = FollowUp, ["TbToolbar"] = FollowUp, ["TbTooltip"] = FollowUp,
        ["TbVirtualList"] = FollowUp,
    };

    // Members only one package has, by type and member name, and why.
    private static readonly Dictionary<string, string> OneSided = new()
    {
        ["TrailblazorMarkup.Within"] = "Playwright scopes with new TrailblazorPage(locator)",
        ["TrailblazorPage.Interactive"] = "bUnit renders interactively from the start",
        ["TrailblazorPage.Toasts"] = FollowUp, ["TrailblazorPage.Announcements"] = "bUnit tests read FakeAnnouncer",
        ["TrailblazorPage.Popover"] = FollowUp, ["TrailblazorPage.RangeSlider"] = FollowUp,
        ["TrailblazorPage.Splitter"] = FollowUp,
        ["TbGridLocator.Copy"] = "pressing Ctrl+C needs the browser's copy event; bUnit reads the same text with "
            + "SelectedCells",
        ["TbFileUploadLocator.Choose"] =
            "a FilePayload is Playwright's file made in the test; bUnit picks files by path",
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
            var excuse = $"{(bunit.ContainsKey(member) ? markup : page).Name}.{member.Split('(')[0]}";
            if (bunit.TryGetValue(member, out var markupType) && playwright.TryGetValue(member, out var pageType))
            {
                if (IsDriver(markupType) && IsDriver(pageType))
                {
                    Compare(markupType, pageType, compared, differences, excused);
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
        .DistinctBy(member => member.Key).ToDictionary(member => member.Key, member => member.Type);

    private static string Stripped(string name) => name.EndsWith("Async") ? name[..^5] : name;

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
