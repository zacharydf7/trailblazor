using System.Text;
using Deque.AxeCore.Playwright;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class FileUploadTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static FilePayload File(string name, string text = "trail", string type = "application/pdf") =>
        new() { Name = name, MimeType = type, Buffer = Encoding.UTF8.GetBytes(text) };

    private static ILocator Field(IPage page, string label) =>
        page.Locator(".tb-file-upload").Filter(new() { Has = page.GetByLabel(label, new() { Exact = true }) });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task DropZone_ShouldWriteItsPromptInThePrimaryText_WhenHovered(string browser)
    {
        var page = await OpenAsync(browser, "/file-upload");
        var zone = Field(page, "Documents").Locator(".tb-file-upload__zone");

        // Muted text on the selection fill falls below 4.5:1 in the ink palettes.
        await zone.HoverAsync();

        var colors = await zone.EvaluateAsync<string[]>("""
            zone => {
                const probe = document.createElement('span');
                probe.style.color = 'var(--tb-color-primary-text)';
                zone.append(probe);
                const colors = [getComputedStyle(zone).color, getComputedStyle(probe).color];
                probe.remove();
                return colors;
            }
            """);
        Assert.Equal(colors[1], colors[0]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Clear_ShouldLetTheSameFileBeChosenAgain_WhenAFileWasCleared(string browser)
    {
        var page = await OpenAsync(browser, "/file-upload");
        var input = page.Locator("[data-part=input]").First;
        await Expect(input).ToHaveAttributeAsync("data-ready", "");
        await input.SetInputFilesAsync(File("trail.pdf", "x", "application/pdf"));

        await page.Locator("[data-part=clear]").First.ClickAsync();

        await Expect(page.Locator("[data-part=file]")).ToHaveCountAsync(0);
        Assert.Equal("", await input.InputValueAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Open_ShouldOpenThePickerFromCodeAndLetItBeCleared_WhenButtonsAsk(string browser)
    {
        var page = await OpenAsync(browser, "/file-upload");
        var imported = page.GetByTestId("imported");
        var import = page.GetByRole(AriaRole.Button, new() { Name = "Import settings" });
        await Expect(page.Locator(".tb-visually-hidden [data-part=input]")).ToHaveAttributeAsync("data-ready", "");

        var chooser = await page.RunAndWaitForFileChooserAsync(() => import.ClickAsync());
        await chooser.SetFilesAsync(File("trail.json", "{}", "application/json"));
        await Expect(imported).ToHaveTextAsync("trail.json");
        chooser = await page.RunAndWaitForFileChooserAsync(() => import.ClickAsync());
        // A browser reports no change for the file the input already holds, so opening empties it first.
        Assert.Equal("", await chooser.Element.InputValueAsync());
        await page.GetByRole(AriaRole.Button, new() { Name = "Forget the import" }).ClickAsync();

        await Expect(imported).ToHaveTextAsync("none");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Choose_ShouldListTheFilesAndKeepThemReadable_WhenTheyAreChosen(string browser)
    {
        var page = await OpenAsync(browser, "/file-upload");

        await page.Tb().FileUpload("Documents").ChooseAsync(File("trail.pdf", "12345"), File("map.pdf", "123"));
        await page.GetByRole(AriaRole.Button, new() { Name = "Read" }).ClickAsync();

        await Expect(page.GetByTestId("documents")).ToHaveTextAsync("trail.pdf, map.pdf");
        await Expect(Field(page, "Documents").GetByRole(AriaRole.Listitem)).ToHaveCountAsync(2);
        await Expect(page.GetByTestId("read")).ToHaveTextAsync("8 bytes");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Choose_ShouldLeaveOutAndExplainOtherTypes_WhenDroppedFilesDontMatchAccept(string browser)
    {
        var page = await OpenAsync(browser, "/file-upload");

        await page.Tb().FileUpload("Documents").ChooseAsync(File("trail.pdf"),
            File("notes.docx", type: "application/msword"));

        await Expect(page.GetByTestId("documents")).ToHaveTextAsync("trail.pdf");
        await Expect(page.GetByLabel("Documents", new() { Exact = true }))
            .ToHaveAccessibleDescriptionAsync("notes.docx isn't one of .pdf.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Remove_ShouldDropTheFileAndFocusTheInput_WhenClicked(string browser)
    {
        var page = await OpenAsync(browser, "/file-upload");
        await page.Tb().FileUpload("Documents").ChooseAsync(File("trail.pdf"), File("map.pdf"));

        await page.GetByRole(AriaRole.Button, new() { Name = "Remove trail.pdf" }).ClickAsync();

        await Expect(page.GetByTestId("documents")).ToHaveTextAsync("map.pdf");
        await Expect(page.GetByLabel("Documents", new() { Exact = true })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Remove_ShouldEmptyTheInputAndKeepTheOtherFilesReadable_WhenOneOfSeveralIsRemoved(string browser)
    {
        var page = await OpenAsync(browser, "/file-upload");
        var input = page.GetByLabel("Documents", new() { Exact = true });
        await page.Tb().FileUpload("Documents").ChooseAsync(File("trail.pdf", "12345"), File("map.pdf", "123"));

        await page.GetByRole(AriaRole.Button, new() { Name = "Remove trail.pdf" }).ClickAsync();

        await Expect(page.GetByTestId("documents")).ToHaveTextAsync("map.pdf");
        await Expect(input).ToHaveValueAsync("");
        await page.GetByRole(AriaRole.Button, new() { Name = "Read" }).ClickAsync();
        await Expect(page.GetByTestId("read")).ToHaveTextAsync("3 bytes");
    }

    // Only Chromium's debugging protocol can send a trusted drop, which is what fills the input by itself.
    [Fact]
    public async Task Drop_ShouldTakeTheFileAgain_WhenItWasRemoved()
    {
        var page = await OpenAsync("chromium", "/file-upload");
        var input = page.GetByLabel("Documents", new() { Exact = true });
        await Expect(input).ToHaveAttributeAsync("data-ready", "");
        var folder = Directory.CreateTempSubdirectory("trailblazor-drop");
        try
        {
            var path = Path.Combine(folder.FullName, "trail.pdf");
            await System.IO.File.WriteAllTextAsync(path, "trail", TestContext.Current.CancellationToken);

            await DropAsync(page, input, path);
            await Expect(page.GetByTestId("documents")).ToHaveTextAsync("trail.pdf");
            await page.GetByRole(AriaRole.Button, new() { Name = "Remove trail.pdf" }).ClickAsync();
            await Expect(page.GetByTestId("documents")).ToHaveTextAsync("none");
            await DropAsync(page, input, path);

            await Expect(page.GetByTestId("documents")).ToHaveTextAsync("trail.pdf");
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Zone_ShouldRefuseADrop_WhenTheFieldIsDisabled(string browser)
    {
        var page = await OpenAsync(browser, "/file-upload");

        Assert.Equal(["zone", "refused", "refused"], await DragOverAndDropAsync(page, "Archive"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Zone_ShouldRefuseADrop_WhenTheFieldIsReadOnly(string browser)
    {
        var page = await OpenAsync(browser, "/read-only");

        Assert.Equal(["zone", "refused", "refused"], await DragOverAndDropAsync(page, "Photos"));
    }

    // A constructed DataTransfer ignores a new drop effect, so checking for the no-drop cursor needs a trusted drag.
    [Fact]
    public async Task Zone_ShouldShowNoDropAndGetNoDrop_WhenAFileIsDraggedOverADisabledField()
    {
        var page = await OpenAsync("chromium", "/file-upload");
        await page.EvaluateAsync("""
            () => {
                window.seen = { effect: '', drops: 0 };
                window.addEventListener('dragover', event => window.seen.effect = event.dataTransfer.dropEffect);
                window.addEventListener('drop', () => window.seen.drops++);
            }
            """);
        var path = Path.GetTempFileName();

        try
        {
            await DropAsync(page, Field(page, "Archive").Locator("[data-part=zone]"), path);

            Assert.Equal("none", await page.EvaluateAsync<string>("() => window.seen.effect"));
            Assert.Equal(0, await page.EvaluateAsync<int>("() => window.seen.drops"));
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Zone_ShouldRefuseADrop_WhenTheDragTargetsTheTextOfItsPrompt(string browser)
    {
        var page = await OpenAsync(browser, "/file-upload");

        var allowed = await Field(page, "Archive").Locator("[data-part=zone]").EvaluateAsync<bool>("""
            zone => document.createTreeWalker(zone, NodeFilter.SHOW_TEXT).nextNode().dispatchEvent(
                new DragEvent('dragover', { bubbles: true, cancelable: true, dataTransfer: new DataTransfer() }))
            """);

        Assert.False(allowed);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Zone_ShouldRefuseADrop_WhenADisabledFieldIsOnAStaticPage(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-file-upload");
        var zone = await Field(page, "Archive").Locator("[data-part=zone]").ElementHandleAsync();

        // No component loads the script on a static page, and it arrives with Blazor's own, so this waits for it.
        await page.WaitForFunctionAsync("""
            zone => !zone.dispatchEvent(
                new DragEvent('dragover', { bubbles: true, cancelable: true, dataTransfer: new DataTransfer() }))
            """, zone);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Zone_ShouldLeaveADropToTheInput_WhenTheFieldIsEnabled(string browser)
    {
        var page = await OpenAsync(browser, "/file-upload");

        var result = await DragOverAndDropAsync(page, "Documents");

        Assert.Equal(["input", "left alone", "left alone"], result);
    }

    // Dispatches on what lies at the middle of the zone, as a drag would, and reads what the page did with each event.
    private static Task<string[]> DragOverAndDropAsync(IPage page, string label) =>
        Field(page, label).Locator("[data-part=zone]").EvaluateAsync<string[]>("""
            zone => {
                zone.scrollIntoView({ block: 'center' });
                const box = zone.getBoundingClientRect();
                const target = document.elementFromPoint(box.x + box.width / 2, box.y + box.height / 2);
                const send = (type, dataTransfer) => target.dispatchEvent(
                    new DragEvent(type, { bubbles: true, cancelable: true, dataTransfer })) ? 'left alone' : 'refused';
                const data = new DataTransfer();
                return [target.localName === 'input' ? 'input' : 'zone', send('dragover', data), send('drop', data)];
            }
            """);

    private static async Task DropAsync(IPage page, ILocator target, string path)
    {
        var box = (await target.BoundingBoxAsync())!;
        var session = await page.Context.NewCDPSessionAsync(page);
        foreach (var type in new[] { "dragEnter", "dragOver", "drop" })
        {
            await session.SendAsync("Input.dispatchDragEvent", new Dictionary<string, object>
            {
                ["type"] = type, ["x"] = box.X + box.Width / 2, ["y"] = box.Y + box.Height / 2,
                ["data"] = new Dictionary<string, object>
                {
                    ["items"] = Array.Empty<object>(), ["files"] = new[] { path }, ["dragOperationsMask"] = 1,
                },
            });
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Zone_ShouldShowItIsReady_WhenAFileIsDraggedOverIt(string browser)
    {
        var page = await OpenAsync(browser, "/file-upload");
        var zone = Field(page, "Documents").Locator("[data-part=zone]");
        var input = page.GetByLabel("Documents", new() { Exact = true });
        // Firefox and WebKit dispatch a drag event without data unless the test gives it some.
        var data = await page.EvaluateHandleAsync("() => new DataTransfer()");

        await input.DispatchEventAsync("dragenter", new { dataTransfer = data });
        await Expect(zone).ToHaveAttributeAsync("data-dragging", "");
        await input.DispatchEventAsync("dragleave", new { dataTransfer = data });

        await Expect(zone).Not.ToHaveAttributeAsync("data-dragging", "");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Zone_ShouldKeepTheInputUnseen_WhenTheFieldIsDisabled(string browser)
    {
        var page = await OpenAsync(browser, "/file-upload");

        var input = page.GetByLabel("Archive", new() { Exact = true });

        await Expect(input).ToBeDisabledAsync();
        await Expect(input).ToHaveCSSAsync("opacity", "0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldTakeOneFile_WhenThereIsNoDropZone(string browser)
    {
        var page = await OpenAsync(browser, "/file-upload");

        await page.Tb().FileUpload("Settings").ChooseAsync(File("settings.json", "{}", "application/json"));

        await Expect(page.GetByTestId("settings")).ToHaveTextAsync("settings.json");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldWaitForTheInteractiveField_WhenThePageIsStillPrerendered(string browser)
    {
        var page = await OpenStaticAsync(browser, "/file-upload");

        await page.Tb().FileUpload("Documents").ChooseAsync(File("trail.pdf"));

        await Expect(page.GetByTestId("documents")).ToHaveTextAsync("trail.pdf");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Upload_ShouldPostTheFiles_WhenUsedInAStaticForm(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-file-upload");

        await page.Tb().FileUpload("Documents").ChooseAsync(File("trail.pdf"));
        await Expect(page.GetByLabel("Documents", new() { Exact = true })).ToHaveCSSAsync("opacity", "1");
        await Expect(Field(page, "Documents").Locator(".tb-file-upload__prompt")).ToBeHiddenAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("trail.pdf");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Upload_ShouldHaveNoAccessibilityViolations_WhenFilesAreListed(string browser)
    {
        var page = await OpenAsync(browser, "/file-upload");
        await page.Tb().FileUpload("Documents").ChooseAsync(File("trail.pdf"));

        var result = await page.RunAxe(AxeTests.WcagAA);

        Assert.Empty(result.Violations.Select(violation =>
            $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target))}"));
    }
}
