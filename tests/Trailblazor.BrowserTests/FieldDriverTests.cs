using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// Every field's locator reads the field's messages and whether it's marked invalid, the same way.
public class FieldDriverTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static readonly string[] Helpers =
    [
        "Select", "TreeSelect", "ListBox", "TransferList", "DatePicker", "DateTimePicker", "DateRangePicker",
        "TextField", "Checkbox", "Switch", "CheckboxGroup", "RadioGroup", "NumberField", "Slider", "RangeSlider",
        "Autocomplete",
        "ColorPicker", "FileUpload", "TagInput", "SignaturePad", "Calendar", "RichTextEditor",
    ];

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Locators_ShouldReadEachFieldsMessageAndInvalidMark_WhenTheFieldsShowAnError(string browser)
    {
        var page = await OpenAsync(browser, "/field-errors");
        var failures = new List<string>();

        // Each field is labeled by its helper's name.
        foreach (var helper in Helpers)
        {
            var locator = (TbFieldLocator)typeof(TrailblazorPage).GetMethod(helper, [typeof(string)])!
                .Invoke(page.Tb(), [helper])!;
            var (error, errors, invalid) =
                (await locator.ErrorAsync(), await locator.ErrorsAsync(), await locator.InvalidAsync());
            if (error != "That name is taken." || !errors.SequenceEqual(["That name is taken."]) || !invalid)
            {
                failures.Add($"{helper}: {error}, [{string.Join(", ", errors)}], {invalid}");
            }
        }

        var valid = page.Tb().TextField("Valid");
        var marked = page.Tb().DateRangePicker("Marked");
        Assert.Empty(failures);
        Assert.Equal((null, 0, false), (await valid.ErrorAsync(), (await valid.ErrorsAsync()).Count,
            await valid.InvalidAsync()));
        // The group itself carries no mark, only its boxes do.
        Assert.Equal((null, 0, true), (await marked.ErrorAsync(), (await marked.ErrorsAsync()).Count,
            await marked.InvalidAsync()));
    }
}
