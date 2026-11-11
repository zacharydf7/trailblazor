using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbTagInputTests : BunitContext
{
    private readonly BunitJSModuleInterop _handle;

    public TbTagInputTests()
    {
        this.AddTrailblazor();
        var module = JSInterop.SetupModule("./_content/Trailblazor/tag-input.js");
        _handle = module.SetupModule("createTagInput", _ => true);
        _handle.SetupVoid("restore", _ => true).SetVoidResult();
        _handle.SetupVoid("focusTag", _ => true).SetVoidResult();
    }

    private List<IReadOnlyList<string>> Changes { get; } = [];

    private IRenderedComponent<TbTagInput> RenderTags(IReadOnlyList<string> value,
        Action<ComponentParameterCollectionBuilder<TbTagInput>>? configure = null) => Render<TbTagInput>(parameters =>
    {
        parameters.Add(field => field.Label, "Features").Add(field => field.Value, value)
            .Add(field => field.ValueChanged, (IReadOnlyList<string> next) => Changes.Add(next));
        configure?.Invoke(parameters);
    });

    private IReadOnlyList<string> Announced(IRenderedComponent<AnnouncerHost> host) =>
        [.. host.FindAll("[role=status] p").Select(p => p.TextContent)];

    [Fact]
    public void Render_ShouldListEachTagWithANamedRemoveButtonOutOfTheTabOrder_WhenGivenTags()
    {
        var cut = RenderTags(["Shade", "Water"]);

        var list = cut.Find("[data-part=tags]");
        Assert.Equal("list", list.GetAttribute("role"));
        Assert.Equal(cut.Find("label").Id, list.GetAttribute("aria-labelledby"));
        Assert.Equal(["Remove Shade", "Remove Water"],
            cut.FindAll("[data-part=remove]").Select(button => button.GetAttribute("aria-label")));
        Assert.All(cut.FindAll("[data-part=remove]"), button => Assert.Equal("-1", button.GetAttribute("tabindex")));
        Assert.Equal(["Shade", "Water"], cut.Tb().TagInput("Features").Tags);
    }

    [Fact]
    public void Render_ShouldDescribeTheInputWithHowToAddAndReachTags_WhenEditable()
    {
        var cut = RenderTags([], parameters => parameters.Add(field => field.HelperText, "Up to five."));

        var described = cut.Find("input").GetAttribute("aria-describedby")!.Split(' ');
        Assert.Equal(["Up to five.", "Enter adds a tag. Left Arrow moves to the tags."],
            described.Select(id => cut.Find($"[id='{id}']").TextContent.Trim()));
    }

    [Fact]
    public async Task AddAsync_ShouldAddTheTrimmedTextAndAnnounceIt_WhenTextIsEntered()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderTags(["Shade"]);

        await cut.Tb().TagInput("Features").AddAsync("  Water ");

        Assert.Equal(["Shade", "Water"], Changes.Single());
        host.WaitForAssertion(() => Assert.Equal(["Water added"], Announced(host)));
    }

    [Fact]
    public async Task AddAsync_ShouldRefuseTheTagWithAMessageAndPutItBack_WhenItIsAlreadyThereInAnotherCase()
    {
        var cut = RenderTags(["Shade"]);

        await cut.Tb().TagInput("Features").AddAsync("shade");

        Assert.Empty(Changes);
        Assert.Equal("shade is already added.", cut.Find("[data-part=error]").TextContent.Trim());
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
        Assert.Equal(["shade"], (IEnumerable<string>)_handle.VerifyInvoke("restore").Arguments[0]!);
    }

    [Fact]
    public async Task AddAsync_ShouldAddATagDifferingOnlyInCase_WhenTheComparerIsOrdinal()
    {
        var cut = RenderTags(["Shade"], parameters => parameters.Add(field => field.Comparer, StringComparer.Ordinal));

        await cut.Tb().TagInput("Features").AddAsync("shade");

        Assert.Equal(["Shade", "shade"], Changes.Single());
    }

    [Fact]
    public async Task AddAsync_ShouldRefuseTheTag_WhenTheFieldHasMaxTags()
    {
        var cut = RenderTags(["Shade", "Water"], parameters => parameters.Add(field => field.MaxTags, 2));

        await cut.Tb().TagInput("Features").AddAsync("Views");

        Assert.Empty(Changes);
        Assert.Equal("You can add up to 2 tags.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public async Task AddAsync_ShouldAskTheLocalizerForTheCountedWords_WhenTheFieldIsFull()
    {
        Services.AddScoped<ITbLocalizer, PluralRecorder>();
        var cut = RenderTags(["Shade", "Water"], parameters => parameters.Add(field => field.MaxTags, 2));

        await cut.Tb().TagInput("Features").AddAsync("Views");

        Assert.Equal("TagInput.TooMany for 2", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public async Task AddAsync_ShouldSayOnlyOneTagFits_WhenTheFieldTakesOneTag()
    {
        var cut = RenderTags(["Shade"], parameters => parameters.Add(field => field.MaxTags, 1));

        await cut.Tb().TagInput("Features").AddAsync("Views");

        Assert.Equal("You can add only one tag.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public async Task AddAsync_ShouldShowTheChecksMessage_WhenTagValidationRefusesTheTag()
    {
        var cut = RenderTags([], parameters => parameters.Add(field => field.TagValidation,
            (Func<string, string?>)(tag => tag.Length > 5 ? "Keep a feature to five letters." : null)));

        await cut.Tb().TagInput("Features").AddAsync("Waterfall");

        Assert.Empty(Changes);
        Assert.Equal("Keep a feature to five letters.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Theory]
    [InlineData(",", "Water, shade\n , Views")]
    [InlineData(";", "Water; shade\r\nViews;")]
    public async Task AddEvent_ShouldSplitTheTextAndPutBackTheRefusedParts_WhenItHoldsSeveralTags(string separators,
        string text)
    {
        var cut = RenderTags(["Shade"], parameters => parameters.Add(field => field.Separators, separators));

        await cut.Find("input").TriggerEventAsync("ontbtagadd", new TagAddEventArgs { Text = text });

        Assert.Equal(["Shade", "Water", "Views"], Changes.Single());
        Assert.Equal(["shade"], (IEnumerable<string>)_handle.VerifyInvoke("restore").Arguments[0]!);
        Assert.Equal("shade is already added.", cut.Find("[data-part=error]").TextContent.Trim());
    }

    [Fact]
    public async Task AddEvent_ShouldTakeBackTheMessage_WhenNothingIsLeftToAdd()
    {
        var cut = RenderTags(["Shade"]);
        await cut.Tb().TagInput("Features").AddAsync("Shade");

        await cut.Find("input").TriggerEventAsync("ontbtagadd", new TagAddEventArgs { Text = "" });

        Assert.Empty(cut.FindAll("[data-part=error]"));
        Assert.Null(cut.Find("input").GetAttribute("aria-invalid"));
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveTheTagAnnounceItAndFocusTheInput_WhenItsButtonIsClicked()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderTags(["Shade", "Water"]);

        await cut.Tb().TagInput("Features").RemoveAsync("Shade");

        Assert.Equal(["Water"], Changes.Single());
        host.WaitForAssertion(() => Assert.Equal(["Shade removed"], Announced(host)));
        cut.WaitForAssertion(() => Assert.Equal(-1, _handle.VerifyInvoke("focusTag").Arguments[0]));
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveTheTagClicked_WhenTheValueRepeatsATag()
    {
        var cut = RenderTags(["Shade", "Water", "Shade"]);

        await cut.FindAll("[data-part=remove]")[2].ClickAsync(new());

        Assert.Equal(["Shade", "Water"], Changes.Single());
    }

    [Theory]
    [InlineData(0, "previous", 0)]
    [InlineData(1, "previous", 0)]
    [InlineData(1, "next", 1)]
    [InlineData(2, "next", -1)]
    [InlineData(0, "input", -1)]
    public async Task RemoveEvent_ShouldFocusTheTagThatTakesItsPlace_WhenATagIsRemovedByKey(int index, string focus,
        int focused)
    {
        var cut = RenderTags(["Shade", "Water", "Views"]);
        var key = cut.FindAll("[data-part=remove]")[index].GetAttribute("data-key")!;

        await cut.Find("input").TriggerEventAsync("ontbtagremove", new TagRemoveEventArgs { Key = key, Focus = focus });

        Assert.Equal(2, Changes.Single().Count);
        cut.WaitForAssertion(() => Assert.Equal(focused, _handle.VerifyInvoke("focusTag").Arguments[0]));
    }

    [Fact]
    public async Task RemoveEvent_ShouldRemoveOnlyTheNamedTag_WhenTheSameKeyArrivesTwice()
    {
        var cut = RenderTags(["Shade", "Water", "Views"]);
        var key = cut.FindAll("[data-part=remove]")[1].GetAttribute("data-key")!;

        await cut.Find("input").TriggerEventAsync("ontbtagremove", new TagRemoveEventArgs { Key = key, Focus = "next" });
        await cut.Find("input").TriggerEventAsync("ontbtagremove", new TagRemoveEventArgs { Key = key, Focus = "next" });

        Assert.Equal(["Shade", "Views"], Changes.Single());
    }

    [Fact]
    public async Task RemoveEvent_ShouldRemoveOnlyTheNamedCopy_WhenTheValueRepeatsATagAndItsKeyArrivesTwice()
    {
        var cut = RenderTags(["Shade", "Water", "Shade"]);
        var key = cut.FindAll("[data-part=remove]")[0].GetAttribute("data-key")!;

        await cut.Find("input").TriggerEventAsync("ontbtagremove", new TagRemoveEventArgs { Key = key });
        await cut.Find("input").TriggerEventAsync("ontbtagremove", new TagRemoveEventArgs { Key = key });

        Assert.Equal(["Water", "Shade"], Changes.Single());
    }

    [Fact]
    public async Task RemoveEvent_ShouldFindTheLaterCopy_WhenAnEarlierCopyOfARepeatedTagWasRemovedFirst()
    {
        var cut = RenderTags(["Shade", "Water", "Shade"]);
        var keys = cut.FindAll("[data-part=remove]").Select(button => button.GetAttribute("data-key")!).ToList();

        await cut.Find("input").TriggerEventAsync("ontbtagremove", new TagRemoveEventArgs { Key = keys[0] });
        await cut.Find("input").TriggerEventAsync("ontbtagremove", new TagRemoveEventArgs { Key = keys[2] });

        Assert.Equal(["Water"], Changes.Last());
    }

    [Fact]
    public async Task RemoveEvent_ShouldFindTheLaterCopy_WhenTheAppPassesBackACopyOfTheValue()
    {
        var cut = RenderTags(["Shade", "Water", "Shade"]);
        var keys = cut.FindAll("[data-part=remove]").Select(button => button.GetAttribute("data-key")!).ToList();
        await cut.Find("input").TriggerEventAsync("ontbtagremove", new TagRemoveEventArgs { Key = keys[0] });
        cut.Render(parameters => parameters.Add(field => field.Value, Changes.Last().ToList()));

        await cut.Find("input").TriggerEventAsync("ontbtagremove", new TagRemoveEventArgs { Key = keys[2] });

        Assert.Equal(["Water"], Changes.Last());
    }

    [Fact]
    public async Task RemoveEvent_ShouldRemoveNothingAndRenderTheNewTag_WhenTheAppChangedTheListInPlace()
    {
        var tags = new List<string> { "Shade", "Water" };
        var cut = RenderTags(tags);
        var key = cut.FindAll("[data-part=remove]")[0].GetAttribute("data-key")!;
        tags.RemoveAt(0);
        tags.Add("Views");
        cut.Render(parameters => parameters.Add(field => field.Value, tags));

        await cut.Find("input").TriggerEventAsync("ontbtagremove", new TagRemoveEventArgs { Key = key });

        Assert.Empty(Changes);
        Assert.Equal(["Water", "Views"], cut.FindAll(".tb-chip__label").Select(label => label.TextContent));
    }

    [Fact]
    public async Task RemoveEvent_ShouldRemoveNothing_WhenTheAppReplacedTheValueSinceTheKeyWasSent()
    {
        var cut = RenderTags(["Shade", "Water"]);
        var key = cut.FindAll("[data-part=remove]")[0].GetAttribute("data-key")!;
        cut.Render(parameters => parameters.Add(field => field.Value, ["Shade", "Views"]));

        await cut.Find("input").TriggerEventAsync("ontbtagremove", new TagRemoveEventArgs { Key = key });

        Assert.Empty(Changes);
    }

    [Fact]
    public async Task RemoveAsync_ShouldMoveOnToTheNextTag_WhenTheButtonIsPressedFromTheKeyboard()
    {
        var cut = RenderTags(["Shade", "Water"]);

        await cut.FindAll("[data-part=remove]")[0].ClickAsync(new MouseEventArgs { Detail = 0 });

        cut.WaitForAssertion(() => Assert.Equal(0, _handle.VerifyInvoke("focusTag").Arguments[0]));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Render_ShouldOfferNoWayToChangeTheTags_WhenDisabledOrReadOnly(bool disabled, bool readOnly)
    {
        var cut = RenderTags(["Shade"], parameters => parameters.Add(field => field.Disabled, disabled)
            .Add(field => field.ReadOnly, readOnly));

        Assert.Empty(cut.FindAll("[data-part=remove]"));
        Assert.Null(cut.Find("input").GetAttribute("aria-describedby"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().TagInput("Features").AddAsync("Water"));
        await cut.Find("input").TriggerEventAsync("ontbtagremove", new TagRemoveEventArgs { Key = "0" });
        Assert.Empty(Changes);
    }

    [Fact]
    public void Render_ShouldPostEachTagWithoutRemoveButtons_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var trip = new Trip();

        var cut = Render<TbTagInput>(parameters => parameters.Add(field => field.Label, "Features")
            .Add(field => field.Value, trip.Features).Add(field => field.ValueExpression, () => trip.Features));

        var posted = cut.FindAll("input[type=hidden]");
        Assert.Equal(["Shade", "Water"], posted.Select(input => input.GetAttribute("value")));
        Assert.Single(posted.Select(input => input.GetAttribute("name")).Distinct());
        Assert.Empty(cut.FindAll("[data-part=remove]"));
    }

    [Fact]
    public async Task Labels_ShouldReplaceTheBuiltInWords_WhenGiven()
    {
        var cut = RenderTags(["Shade"], parameters => parameters.Add(field => field.Labels, new TagInputLabels
        {
            Remove = "Delete {0}",
            Duplicate = "You already have {0}.",
            Hint = "Type a feature and press Enter.",
        }));

        await cut.Tb().TagInput("Features").AddAsync("Shade");

        Assert.Equal("Delete Shade", cut.Find("[data-part=remove]").GetAttribute("aria-label"));
        Assert.Equal("You already have Shade.", cut.Find("[data-part=error]").TextContent.Trim());
        Assert.Contains("Type a feature and press Enter.", cut.Markup);
    }

    [Fact]
    public void Render_ShouldGiveTheScriptItsSeparators_WhenTheyAreSet()
    {
        var cut = RenderTags([], parameters => parameters.Add(field => field.Separators, ",;"));

        Assert.Equal(",;", cut.Find("input").GetAttribute("data-separators"));
    }

    private sealed class Trip
    {
        public IReadOnlyList<string> Features { get; set; } = ["Shade", "Water"];
    }
}
