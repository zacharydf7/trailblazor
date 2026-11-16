using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Playwright;
using Trailblazor.Testing;
using Trailblazor.Testing.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Trailblazor.Docs.Recipes;

// The Plain bUnit and Playwright guide shows the body of each method here as one cell of its comparison, and the tests
// run every one against TripPlanner, so the code the guide shows can't drift from the API.
public static class PlainBunit
{
    public static async Task ClickAButton(IRenderedComponent<TripPlanner> cut)
    {
        var button = cut.FindAll("button").Single(button => button.TextContent.Trim() == "Add a stop");
        await button.ClickAsync(new MouseEventArgs());
    }

    public static async Task TypeAndLeave(IRenderedComponent<TripPlanner> cut)
    {
        // The field is found through its label, whose text ends in the required mark. It commits on change, which a
        // browser sends when the field loses focus, and has no blur handler of its own, so sending the change is all
        // that leaving the field does.
        var label = cut.FindAll("label").Single(label => label.TextContent.Trim().TrimEnd('*') == "Trip name");
        var input = cut.Find($"[id='{label.GetAttribute("for")}']");
        await input.ChangeAsync(new ChangeEventArgs { Value = "Lake loop" });
    }

    public static async Task<(bool Invalid, IReadOnlyList<string> Descriptions)> ReadAnError(
        IRenderedComponent<TripPlanner> cut)
    {
        var label = cut.FindAll("label").Single(label => label.TextContent.Trim().TrimEnd('*') == "Trip name");
        var input = cut.Find($"[id='{label.GetAttribute("for")}']");
        await input.ChangeAsync(new ChangeEventArgs { Value = "" });

        // The input is found again, since the change rendered it anew. Its error is one of the elements its
        // aria-describedby points to.
        input = cut.Find($"[id='{label.GetAttribute("for")}']");
        var described = (input.GetAttribute("aria-describedby") ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return (input.GetAttribute("aria-invalid") == "true",
            [.. described.Select(id => cut.Find($"[id='{id}']").TextContent.Trim())]);
    }

    public static async Task ChooseAnOption(IRenderedComponent<TripPlanner> cut)
    {
        // The select is a combobox named by the element its aria-labelledby points to.
        var combobox = cut.FindAll("[role=combobox]").Single(box =>
            cut.Find($"[id='{box.GetAttribute("aria-labelledby")}']").TextContent.Trim() == "Season");
        // The select's script opens the list in a browser. bUnit runs no script, so the option is clicked in the
        // closed list.
        var options = cut.Find($"[id='{combobox.GetAttribute("aria-controls")}']");
        await options.QuerySelectorAll("[role=option]").Single(option => option.TextContent.Trim() == "Summer")
            .ClickAsync(new MouseEventArgs());
    }

    public static async Task AnswerADialog(IRenderedComponent<TripPlanner> cut, IRenderedComponent<TbRoot> root)
    {
        // The click's task finishes only once the dialog is answered, so it's awaited last.
        var click = cut.FindAll("button").Single(button => button.TextContent.Trim() == "Delete trip")
            .ClickAsync(new MouseEventArgs());
        var dialog = root.FindAll("[role=alertdialog]").Single(dialog =>
            root.Find($"[id='{dialog.GetAttribute("aria-labelledby")}']").TextContent.Trim() == "Delete trip?");
        await dialog.QuerySelectorAll("button").Single(button => button.TextContent.Trim() == "Delete")
            .ClickAsync(new MouseEventArgs());
        await click;
    }

    public static async Task SelectATreeRow(IRenderedComponent<TripPlanner> cut)
    {
        IElement Row(string text) =>
            cut.FindAll("[role=treeitem]").Single(row => row.GetAttribute("data-text") == text);

        await Row("North").QuerySelector("[data-part=toggle]")!.ClickAsync(new MouseEventArgs());
        await Row("Ridge loop").ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    public static async Task<IReadOnlyList<string>> SelectAGridRow(IRenderedComponent<TripPlanner> cut)
    {
        // A row's selection box is its first cell, so the row's own cells come after it.
        var grid = cut.FindAll("table").Single(table => table.GetAttribute("aria-label") == "Hikes");
        var rows = grid.QuerySelectorAll("tbody tr");
        await rows.Single(row => row.QuerySelectorAll("td")[1].TextContent.Trim() == "Lake path")
            .QuerySelector("td input")!.ChangeAsync(new ChangeEventArgs());
        return [.. rows.Select(row => row.QuerySelectorAll("td")[1].TextContent.Trim())];
    }

    public static async Task<string> ReadATabPanel(IRenderedComponent<TripPlanner> cut)
    {
        var tab = cut.FindAll("[role=tab]").Single(tab => tab.TextContent.Trim() == "Route");
        await tab.ClickAsync(new MouseEventArgs { Detail = 1 });
        tab = cut.FindAll("[role=tab]").Single(tab => tab.TextContent.Trim() == "Route");
        return cut.Find($"[id='{tab.GetAttribute("aria-controls")}']").TextContent.Trim();
    }

    public static void UploadAFile(IRenderedComponent<TripPlanner> cut) =>
        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("<gpx />", "route.gpx"));

    public static string? ReadAControlWithNoHelper(IRenderedComponent<TripPlanner> cut) =>
        cut.FindAll("[role=progressbar]").Single(bar =>
            cut.Find($"[id='{bar.GetAttribute("aria-labelledby")}']").TextContent.Trim() == "Packed")
            .GetAttribute("aria-valuenow");
}

public static class PlainPlaywright
{
    public static async Task ClickAButton(IPage page)
    {
        // A click before the page is interactive does nothing, so wait for the app's own mark that it is.
        await page.Locator("[data-trip-ready]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        var stops = await page.GetByLabel("Stops", new() { Exact = true }).TextContentAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Add a stop", Exact = true }).ClickAsync();

        // The click returns before the app has handled it, so wait for what the handler changes.
        await Expect(page.GetByLabel("Stops", new() { Exact = true })).Not.ToHaveTextAsync(stops!);
    }

    public static async Task TypeAndLeave(IPage page)
    {
        await page.Locator("[data-trip-ready]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        var field = page.GetByRole(AriaRole.Textbox, new() { Name = "Trip name", Exact = true });
        await field.FillAsync("Lake loop");
        await field.BlurAsync();
        await Expect(page.GetByLabel("Trip", new() { Exact = true })).ToContainTextAsync("Lake loop");
    }

    public static async Task<string?> ReadAnError(IPage page)
    {
        await page.Locator("[data-trip-ready]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        var field = page.GetByRole(AriaRole.Textbox, new() { Name = "Trip name", Exact = true });
        await field.FillAsync("");
        await field.BlurAsync();

        // The error describes the field once the app has validated the change.
        await Expect(field).ToHaveAttributeAsync("aria-invalid", "true");
        return await field.EvaluateAsync<string>("""
            input => input.getAttribute('aria-describedby').split(' ')
                .map(id => document.getElementById(id).textContent.trim()).join(' ')
            """);
    }

    public static async Task ChooseAnOption(IPage page)
    {
        await page.Locator("[data-trip-ready]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        // The select's own script opens the list, and listens only once it marks the combobox data-ready.
        var combobox = page.GetByRole(AriaRole.Combobox, new() { Name = "Season", Exact = true });
        await Expect(combobox).ToHaveAttributeAsync("data-ready", "");
        await combobox.ClickAsync();
        await page.GetByRole(AriaRole.Option, new() { Name = "Summer", Exact = true }).ClickAsync();
        await Expect(page.GetByLabel("Trip", new() { Exact = true })).ToContainTextAsync("Summer");
    }

    public static async Task AnswerADialog(IPage page)
    {
        await page.Locator("[data-trip-ready]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        await page.GetByRole(AriaRole.Button, new() { Name = "Delete trip", Exact = true }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Delete trip?", Exact = true });
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true }).ClickAsync();

        // The dialog fades out before the app gets the answer, so wait for it to leave the page, not just to hide.
        await Expect(page.Locator("dialog[open], dialog[data-leaving]")).ToHaveCountAsync(0);
        await Expect(page.GetByLabel("Deleted", new() { Exact = true })).ToHaveTextAsync("yes");
    }

    public static async Task SelectATreeRow(IPage page)
    {
        await page.Locator("[data-trip-ready]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        var tree = page.GetByRole(AriaRole.Tree, new() { Name = "Stops by region", Exact = true });
        var north = tree.GetByRole(AriaRole.Treeitem, new() { Name = "North" });
        await north.Locator("[data-part=toggle]").ClickAsync();
        await Expect(north).ToHaveAttributeAsync("aria-expanded", "true");
        var ridge = tree.GetByRole(AriaRole.Treeitem, new() { Name = "Ridge loop", Exact = true });
        await ridge.ClickAsync();
        await Expect(ridge).ToHaveAttributeAsync("aria-selected", "true");
    }

    public static async Task<IReadOnlyList<string>> SelectAGridRow(IPage page)
    {
        await page.Locator("[data-trip-ready]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        var grid = page.GetByRole(AriaRole.Table, new() { Name = "Hikes", Exact = true });
        // A grid that loads its rows marks itself busy until they're in.
        await Expect(grid).Not.ToHaveAttributeAsync("aria-busy", "true");
        var row = grid.GetByRole(AriaRole.Row).Filter(new() { Has = page.GetByRole(AriaRole.Cell,
            new() { Name = "Lake path", Exact = true }) });
        await row.GetByRole(AriaRole.Radio).CheckAsync();
        // The browser checks the box at once; the page shows the hike once the app has selected it.
        await Expect(page.GetByLabel("Hike", new() { Exact = true })).ToHaveTextAsync("Lake path");
        return [.. (await grid.Locator("tbody tr td:nth-child(2)").AllTextContentsAsync()).Select(text => text.Trim())];
    }

    public static async Task<string> ReadATabPanel(IPage page)
    {
        await page.Locator("[data-trip-ready]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        var tab = page.GetByRole(AriaRole.Tab, new() { Name = "Route", Exact = true });
        await tab.ClickAsync();
        await Expect(tab).ToHaveAttributeAsync("aria-selected", "true");
        return (await page.GetByRole(AriaRole.Tabpanel, new() { Name = "Route", Exact = true }).TextContentAsync())!
            .Trim();
    }

    public static async Task UploadAFile(IPage page)
    {
        await page.Locator("[data-trip-ready]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        // The field's script hands it the files, and listens only once it marks the input data-ready.
        var input = page.GetByLabel("Route map", new() { Exact = true });
        await Expect(input).ToHaveAttributeAsync("data-ready", "");
        await input.SetInputFilesAsync(
            new FilePayload { Name = "route.gpx", MimeType = "application/gpx+xml", Buffer = "<gpx />"u8.ToArray() });
        await Expect(page.GetByLabel("Trip", new() { Exact = true })).ToContainTextAsync("route.gpx");
    }

    public static async Task WaitUntilInteractive(IPage page) =>
        await page.Locator("[data-trip-ready]").WaitForAsync(new() { State = WaitForSelectorState.Attached });

    public static async Task ReadAControlWithNoHelper(IPage page) =>
        await Expect(page.GetByRole(AriaRole.Progressbar, new() { Name = "Packed", Exact = true }))
            .ToHaveAttributeAsync("aria-valuenow", "40");
}

public static class BunitHelpers
{
    public static async Task ClickAButton(IRenderedComponent<TripPlanner> cut) =>
        await cut.Tb().Button("Add a stop").ClickAsync();

    public static async Task TypeAndLeave(IRenderedComponent<TripPlanner> cut) =>
        await cut.Tb().TextField("Trip name").EnterAsync("Lake loop");

    public static async Task<(bool Invalid, string? Error)> ReadAnError(IRenderedComponent<TripPlanner> cut)
    {
        await cut.Tb().TextField("Trip name").EnterAsync("");
        return (cut.Tb().TextField("Trip name").Invalid, cut.Tb().TextField("Trip name").Error);
    }

    public static async Task ChooseAnOption(IRenderedComponent<TripPlanner> cut) =>
        await cut.Tb().Select("Season").ChooseAsync("Summer");

    public static async Task AnswerADialog(IRenderedComponent<TripPlanner> cut, IRenderedComponent<TbRoot> root)
    {
        var click = cut.Tb().Button("Delete trip").ClickAsync();
        await root.Tb().Dialog("Delete trip?").ChooseAsync("Delete");
        await click;
    }

    public static async Task SelectATreeRow(IRenderedComponent<TripPlanner> cut) =>
        await cut.Tb().Tree("Stops by region").SelectAsync("North", "Ridge loop");

    public static async Task<IReadOnlyList<IReadOnlyList<string>>> SelectAGridRow(IRenderedComponent<TripPlanner> cut)
    {
        await cut.Tb().Grid("Hikes").WaitForRowsAsync();
        await cut.Tb().Grid("Hikes").SelectAsync("Lake path");
        return cut.Tb().Grid("Hikes").Rows;
    }

    public static async Task<string> ReadATabPanel(IRenderedComponent<TripPlanner> cut)
    {
        await cut.Tb().Tabs("Trip details").SelectAsync("Route");
        return cut.Tb().Tabs("Trip details").PanelElement("Route").TextContent.Trim();
    }

    public static async Task UploadAFile(IRenderedComponent<TripPlanner> cut) =>
        await cut.Tb().FileUpload("Route map").ChooseAsync(InputFileContent.CreateFromText("<gpx />", "route.gpx"));
}

public static class PlaywrightHelpers
{
    public static async Task ClickAButton(IPage page) => await page.Tb().Button("Add a stop").ClickAsync();

    public static async Task TypeAndLeave(IPage page) => await page.Tb().TextField("Trip name").EnterAsync("Lake loop");

    public static async Task<(bool Invalid, string? Error)> ReadAnError(IPage page)
    {
        await page.Tb().TextField("Trip name").EnterAsync("");
        var field = page.Tb().TextField("Trip name");
        return (await field.InvalidAsync(), await field.ErrorAsync());
    }

    public static async Task ChooseAnOption(IPage page) => await page.Tb().Select("Season").ChooseAsync("Summer");

    public static async Task AnswerADialog(IPage page)
    {
        await page.Tb().Button("Delete trip").ClickAsync();
        await page.Tb().Dialog("Delete trip?").ChooseAsync("Delete");
    }

    public static async Task SelectATreeRow(IPage page) =>
        await page.Tb().Tree("Stops by region").SelectAsync("North", "Ridge loop");

    public static async Task<IReadOnlyList<IReadOnlyList<string>>> SelectAGridRow(IPage page)
    {
        await page.Tb().Grid("Hikes").WaitForRowsAsync();
        await page.Tb().Grid("Hikes").SelectAsync("Lake path");
        return await page.Tb().Grid("Hikes").RowsAsync();
    }

    public static async Task<string> ReadATabPanel(IPage page)
    {
        await page.Tb().Tabs("Trip details").SelectAsync("Route");
        return (await page.Tb().Tabs("Trip details").PanelElement("Route").TextContentAsync())!.Trim();
    }

    public static async Task UploadAFile(IPage page) => await page.Tb().FileUpload("Route map").ChooseAsync(
        new FilePayload { Name = "route.gpx", MimeType = "application/gpx+xml", Buffer = "<gpx />"u8.ToArray() });

    public static async Task WaitUntilInteractive(IPage page) => await page.Tb().InteractiveAsync();
}
