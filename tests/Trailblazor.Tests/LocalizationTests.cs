using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests;

public class LocalizationTests : BunitContext
{
    public LocalizationTests()
    {
        this.AddTrailblazor(options =>
        {
            options.Strings[TbStrings.AlertClose] = "Schließen";
            options.Strings[TbStrings.PaginationPrevious] = "Zurück";
            options.Strings[TbStrings.NumberFieldNotANumber] = "{0} muss eine Zahl sein.";
            options.Strings[TbStrings.FieldUnnamed] = "Das Feld";
            options.Strings[TbStrings.DialogConfirm] = "Ja";
            options.Strings[TbStrings.SplitterPane] = "Bereich {0}";
            options.Strings[TbStrings.ChipRemoveNamed] = "{0} entfernen";
            options.Strings[TbStrings.LabelInfo] = "Infos zu {0}";
            options.Strings[TbStrings.FileUploadKilobytes] = "{0} Ko";
        });
    }

    public static TheoryData<string> Keys => new(typeof(TbStrings).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(key => key.IsLiteral).Select(key => (string)key.GetRawConstantValue()!));

    [Theory]
    [MemberData(nameof(Keys))]
    public void English_ShouldHaveText_WhenGivenAKey(string key)
    {
        Assert.False(string.IsNullOrWhiteSpace(TbStrings.English(key)));
    }

    [Fact]
    public void English_ShouldThrow_WhenGivenAnUnknownKey()
    {
        var failure = Assert.Throws<KeyNotFoundException>(() => TbStrings.English("Alert.Shut"));

        Assert.Equal("Alert.Shut isn't a Trailblazor string; the keys are in TbStrings.", failure.Message);
    }

    [Fact]
    public void TbLocalizer_ShouldGiveTheAppsTextOrElseEnglish_WhenAsked()
    {
        var localizer = Services.GetRequiredService<ITbLocalizer>();

        Assert.Equal(("Schließen", "Next page"),
            (localizer[TbStrings.AlertClose], localizer[TbStrings.PaginationNext]));
    }

    [Fact]
    public void Plural_ShouldGiveTheOneFormForOneAndTheKeyForOtherCounts_WhenTheLocalizerKeepsTheDefault()
    {
        var localizer = Services.GetRequiredService<ITbLocalizer>();

        Assert.Equal(["1 file chosen.", "{0} files chosen.", "{0} files chosen.", "{0} items"],
            [localizer.Plural(TbStrings.FileUploadChosen, 1), localizer.Plural(TbStrings.FileUploadChosen, 0),
                localizer.Plural(TbStrings.FileUploadChosen, 21), localizer.Plural(TbStrings.TreeViewItemCount, 1)]);
    }

    [Fact]
    public void Rating_ShouldNameEachStarInTheLocalizersPluralForm_WhenItsLanguageHasThreeForms()
    {
        using var context = new BunitContext();
        context.Services.AddScoped<ITbLocalizer, Russian>();
        context.AddTrailblazor();

        var cut = context.Render<TbRating>(parameters => parameters.Add(rating => rating.Label, "Оценка")
            .Add(rating => rating.Max, 5));

        Assert.Equal(["1 звезда", "2 звезды", "3 звезды", "4 звезды", "5 звёзд"],
            cut.FindAll("[data-part=star] .tb-visually-hidden").Select(name => name.TextContent));
    }

    [Fact]
    public void Alert_ShouldNameItsCloseButtonWithItsOwnLabelOrElseTheLocalizers_WhenGivenOneOrNot()
    {
        var localized = RenderAlert(closeLabel: null);
        var own = RenderAlert(closeLabel: "Ausblenden");

        Assert.Equal("Schließen", localized.Find("[data-part=close]").GetAttribute("aria-label"));
        Assert.Equal("Ausblenden", own.Find("[data-part=close]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Pagination_ShouldKeepTheLabelsTheAppSetAndLocalizeTheRest_WhenGivenSomeLabels()
    {
        var cut = Render<TbPagination>(parameters => parameters.Add(pagination => pagination.PageCount, 5)
            .Add(pagination => pagination.Page, 3)
            .Add(pagination => pagination.Labels, new PaginationLabels { Next = "Weiter" }));

        Assert.Equal(("Zurück", "Weiter"), (cut.Find("[data-part=previous]").GetAttribute("aria-label"),
            cut.Find("[data-part=next]").GetAttribute("aria-label")));
    }

    [Theory]
    [InlineData("Menge", "Menge muss eine Zahl sein.")]
    [InlineData(null, "Das Feld muss eine Zahl sein.")]
    public void NumberField_ShouldSayItsMessagesWithTheLocalizersText_WhenTheTextIsntANumber(string? label,
        string message)
    {
        var cut = Render<TbNumberField<int>>(parameters => parameters.Add(field => field.Label, label));

        cut.Find("input").TriggerEvent("ontbnumberchange", new NumberChangeEventArgs { Value = "", BadInput = true });

        Assert.Equal(message, cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public void ConfirmAsync_ShouldLabelItsButtonsWithTheLocalizersText_WhenGivenNoText()
    {
        var host = Render<DialogHost>();

        _ = Services.GetRequiredService<IDialogService>().ConfirmAsync("Löschen?", "Das geht nicht rückgängig.");

        var buttons = host.WaitForElements("[data-part=actions] button", 2);
        Assert.Equal(["Cancel", "Ja"], buttons.Select(button => button.TextContent.Trim()));
    }

    [Fact]
    public void Splitter_ShouldNameASeparatorWithTheLocalizersText_WhenThePaneBeforeHasNoLabel()
    {
        var cut = Render<TbSplitter>(parameters => parameters.Add(splitter => splitter.ChildContent, builder =>
        {
            builder.OpenComponent<TbSplitterPane>(0);
            builder.CloseComponent();
            builder.OpenComponent<TbSplitterPane>(1);
            builder.CloseComponent();
        }));

        Assert.Equal("Resize Bereich 1", cut.Find("[role=separator]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Chip_ShouldPutItsTextWhereTheLocalizersTextSays_WhenItHasText()
    {
        var named = Render<TbChip>(parameters => parameters.Add(chip => chip.Text, "Rechnungen")
            .Add(chip => chip.OnClose, () => { }));
        var unnamed = Render<TbChip>(parameters => parameters.Add(chip => chip.OnClose, () => { })
            .AddChildContent("Rechnungen"));

        Assert.Equal("Rechnungen entfernen", named.Find("[data-part=remove]").GetAttribute("aria-label"));
        Assert.Equal("Remove", unnamed.Find("[data-part=remove]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Label_ShouldPutItsTextWhereTheLocalizersTextSays_WhenItHasInfo()
    {
        var cut = Render<TbLabel>(parameters => parameters.Add(label => label.Text, "Projektcode")
            .Add(label => label.Info, "Steht auf der Rechnung."));

        Assert.Equal("Infos zu Projektcode",
            cut.Find("[data-part=info] [data-part=trigger]").GetAttribute("aria-label"));
    }

    [Fact]
    public void FileUpload_ShouldSayItsSizesWithTheLocalizersText_WhenFilesAreChosen()
    {
        var cut = Render<TbFileUpload>(parameters => parameters.Add(upload => upload.Label, "Dokumente"));

        cut.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromBinary(new byte[1_500], "weg.pdf", contentType: "application/pdf"));

        Assert.Equal($"{1.5.ToString(System.Globalization.CultureInfo.CurrentCulture)} Ko",
            cut.Find(".tb-file-upload__size").TextContent);
    }

    [Fact]
    public void AddTrailblazor_ShouldKeepTheAppsOwnLocalizer_WhenTheAppRegistersOne()
    {
        using var context = new BunitContext();
        context.Services.AddScoped<ITbLocalizer, Shouting>();
        context.AddTrailblazor();

        var cut = context.Render<TbAlert>(parameters => parameters.Add(alert => alert.OnClose, () => { }));

        Assert.Equal("ALERT.CLOSE", cut.Find("[data-part=close]").GetAttribute("aria-label"));
    }

    private IRenderedComponent<TbAlert> RenderAlert(string? closeLabel) =>
        Render<TbAlert>(parameters => parameters.Add(alert => alert.OnClose, () => { })
            .Add(alert => alert.CloseLabel, closeLabel).AddChildContent("Saved"));

    // Russian's plural rule: one for 1, 21 and 31; few for 2–4 and 22–24; many for 5–20 and the rest.
    private sealed class Russian : ITbLocalizer
    {
        public string this[string key] => TbStrings.English(key);

        public string Plural(string key, int count) => key != TbStrings.RatingStars ? this[key]
            : (count % 10, count % 100) switch
            {
                (1, not 11) => "{0} звезда",
                (>= 2 and <= 4, < 12 or > 14) => "{0} звезды",
                _ => "{0} звёзд",
            };
    }

    // An app's own localizer, which answers every key with the key itself in capitals.
    private sealed class Shouting : ITbLocalizer
    {
        public string this[string key] => key.ToUpperInvariant();
    }
}
