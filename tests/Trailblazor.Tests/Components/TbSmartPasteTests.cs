using System.Diagnostics.Metrics;
using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbSmartPasteTests : BunitContext
{
    private const string Signature = "Robin Vale, Ridge Outfitters, robin@ridge.example";

    private readonly Dictionary<string, object?> _values = [];
    private readonly List<SmartPasteRequest> _requests = [];
    private bool _fewerOptions;
    private Func<SmartPasteRequest, CancellationToken, Task<IReadOnlyDictionary<string, string>>> _model =
        (_, _) => Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());
    private bool _dirty;

    public TbSmartPasteTests() => this.AddTrailblazor();

    private void Finds(Dictionary<string, string> values) =>
        _model = (_, _) => Task.FromResult<IReadOnlyDictionary<string, string>>(values);

    private Task<IReadOnlyDictionary<string, string>> Extract(SmartPasteRequest request, CancellationToken token)
    {
        _requests.Add(request);
        return _model(request, token);
    }

    // A field whose changes land in _values under its label, as @bind-Value would put them in the app's model.
    private void Field<TField, TValue>(RenderTreeBuilder builder, string label, params (string Name, object? Value)[] more)
        where TField : IComponent
    {
        builder.OpenComponent<TField>(0);
        builder.AddComponentParameter(1, "Label", label);
        builder.AddComponentParameter(2, "ValueChanged",
            EventCallback.Factory.Create<TValue>(this, value => _values[label] = value));
        foreach (var (name, value) in more)
        {
            builder.AddComponentParameter(3, name, value);
        }

        builder.CloseComponent();
    }

    private void Contact(RenderTreeBuilder builder)
    {
        Field<TbTextField<string>, string>(builder, "Name");
        Field<TbTextField<string>, string>(builder, "Email", ("HelperText", "Where we send the permit"));
        Field<TbNumberField<int?>, int?>(builder, "Hikers");
        Field<TbNumberField<decimal?>, decimal?>(builder, "Fee");
        Field<TbDatePicker<DateOnly>, DateOnly?>(builder, "Start");
        Field<TbCheckbox<bool>, bool>(builder, "Guide");
    }

    private IRenderedComponent<TbForm> RenderForm(Action<RenderTreeBuilder>? fields = null,
        params (string Name, object? Value)[] paste) =>
        Render<TbForm>(parameters => parameters.Add(form => form.IsDirtyChanged, (bool dirty) => _dirty = dirty)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                (fields ?? Contact)(builder);
                builder.OpenComponent<TbSmartPaste>(10);
                builder.AddComponentParameter(11, nameof(TbSmartPaste.Extract),
                    (Func<SmartPasteRequest, CancellationToken, Task<IReadOnlyDictionary<string, string>>>)Extract);
                foreach (var (name, value) in paste)
                {
                    builder.AddComponentParameter(12, name, value);
                }

                builder.CloseComponent();
            })));

    // The model's values are invariant whatever the user's culture, which writes 12,5 and 17.11.2026 in German.
    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    public async Task PasteAsync_ShouldFillEachFieldTheModelFound_WhenGivenText(string culture)
    {
        var current = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new(culture);
        Finds(new()
        {
            ["Name"] = "Robin Vale", ["Email"] = "robin@ridge.example", ["Hikers"] = "4", ["Fee"] = "12.5",
            ["Start"] = "2026-11-17", ["Guide"] = "true",
        });
        var cut = RenderForm();

        try
        {
            await cut.Tb().SmartPaste().PasteAsync(Signature);
        }
        finally
        {
            CultureInfo.CurrentCulture = current;
        }

        Assert.Equal(new Dictionary<string, object?>
        {
            ["Name"] = "Robin Vale", ["Email"] = "robin@ridge.example", ["Hikers"] = 4, ["Fee"] = 12.5m,
            ["Start"] = new DateOnly(2026, 11, 17), ["Guide"] = true,
        }, _values);
        Assert.Equal(Signature, _requests.Single().Text);
        Assert.True(_dirty);
        Assert.Equal("Filled 6 fields", cut.Tb().SmartPaste().Status);
        Assert.Equal(["Filling the form", "Filled 6 fields"], Services.GetRequiredService<FakeAnnouncer>().Polite);
    }

    [Fact]
    public async Task Extract_ShouldGetEachFieldItCanFill_WhenCalled()
    {
        var cut = RenderForm(builder =>
        {
            Contact(builder);
            Field<TbTextField<string>, string>(builder, "Password", ("Type", TextFieldType.Password));
            Field<TbTextField<string>, string>(builder, "Permit", ("Disabled", true));
            Field<TbTextField<string>, string>(builder, "Ranger", ("ReadOnly", true));
            Field<TbSelect<string>, string>(builder, "Trail");
            Field<TbListBox<string>, string>(builder, "Campsite");
            Field<TbRadioGroup<string>, string>(builder, "Pace");
            Field<TbRating, int>(builder, "Stars");
            Field<TbSlider<int>, int>(builder, "Distance");
            Field<TbCalendar<DateOnly>, DateOnly?>(builder, "Day");
            Field<TbAutocomplete<string>, string>(builder, "Ranger station");
            Field<TbAutocomplete<string>, string>(builder, "Trailhead", ("FreeText", true));
            Field<TbTimePicker<TimeOnly>, TimeOnly?>(builder, "Meet");
            Field<TbDateTimePicker<DateTime>, DateTime?>(builder, "Return");
            Field<TbTextField<string>, string>(builder, "Email");
        });

        await cut.Tb().SmartPaste().PasteAsync(Signature);

        Assert.Equal(
        [
            new("Name", null, typeof(string)), new("Email", "Where we send the permit", typeof(string)),
            new("Hikers", null, typeof(int?)), new("Fee", null, typeof(decimal?)),
            new("Start", null, typeof(DateOnly?)), new("Guide", null, typeof(bool)),
            new SmartPasteField("Trailhead", null, typeof(string)), new("Meet", null, typeof(TimeOnly?)),
            new("Return", null, typeof(DateTime?)), new("Email (2)", null, typeof(string)),
        ], _requests.Single().Fields);
    }

    [Fact]
    public async Task PasteAsync_ShouldKeepToEachFieldsOwnLimits_WhenAValueGoesPastThem()
    {
        Finds(new()
        {
            ["Start"] = "2026-11-17", ["Fee"] = "12.345", ["Tip"] = "12.6", ["Share"] = "0.155",
            ["Arrive"] = "2026-11-17T23:30:00-11:00", ["Leave"] = "2026-11-17T08:00", ["Return"] = "2026-11-18T14:30",
            ["Back"] = "2026-11-19T16:45:00-11:00",
        });
        var plusFive = TimeZoneInfo.CreateCustomTimeZone("Plus five", TimeSpan.FromHours(5), "Plus five", "Plus five");
        var cut = RenderForm(builder =>
        {
            Field<TbDatePicker<DateOnly>, DateOnly?>(builder, "Start", ("Min", new DateOnly(2026, 12, 1)));
            Field<TbNumberField<decimal?>, decimal?>(builder, "Fee", ("Decimals", 2));
            Field<TbNumberField<decimal?>, decimal?>(builder, "Tip", ("Format", "N0"));
            Field<TbNumberField<decimal?>, decimal?>(builder, "Share", ("Format", "P0"));
            Field<TbDateTimePicker<DateTime>, DateTime?>(builder, "Leave",
                ("IsDateDisabled", (Func<DateOnly, bool>)(date => date.DayOfWeek == DayOfWeek.Tuesday)));
            Field<TbDateTimePicker<DateTimeOffset>, DateTimeOffset?>(builder, "Return", ("TimeZone", plusFive));
            Field<TbDateTimePicker<DateTime>, DateTime?>(builder, "Back");
            Field<TbDatePicker<DateTime>, DateTime?>(builder, "Arrive");
        });

        await cut.Tb().SmartPaste().PasteAsync(Signature);

        // A display format rounds only what it shows, as when typed, and a date-time keeps the time as written.
        Assert.Equal(new Dictionary<string, object?>
        {
            ["Fee"] = 12.35m, ["Tip"] = 12.6m, ["Share"] = 0.155m,
            ["Return"] = new DateTimeOffset(2026, 11, 18, 14, 30, 0, TimeSpan.FromHours(5)),
            ["Back"] = new DateTime(2026, 11, 19, 16, 45, 0), ["Arrive"] = new DateTime(2026, 11, 17),
        }, _values);
        Assert.Equal("Filled 6 fields", cut.Tb().SmartPaste().Status);
    }

    [Fact]
    public async Task Click_ShouldOpenTheBoxAndLogWhy_WhenReadingTheClipboardFails()
    {
        var logs = new FakeLogCollector();
        Services.AddSingleton<ILogger<TbSmartPaste>>(new FakeLogger<TbSmartPaste>(logs));
        JSInterop.SetupModule("./_content/Trailblazor/smart-paste.js").Setup<string?>("readClipboard")
            .SetException(new Microsoft.JSInterop.JSException("The module didn't load."));
        var cut = RenderForm();

        await cut.Find(".tb-smart-paste [data-part=button]").ClickAsync(new());

        Assert.Single(cut.FindAll("dialog.tb-smart-paste__box"));
        Assert.Single(logs.GetSnapshot(), log => log.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Click_ShouldIgnoreAnotherPress_WhenPressedWhileTheClipboardIsRead()
    {
        var reading = JSInterop.SetupModule("./_content/Trailblazor/smart-paste.js").Setup<string?>("readClipboard");
        var cut = RenderForm();
        var button = () => cut.Find(".tb-smart-paste [data-part=button]");

        var pasting = button().ClickAsync(new());
        Assert.NotNull(button().QuerySelector("[data-part=spinner]"));
        await button().ClickAsync(new());
        await cut.InvokeAsync(() => reading.SetResult(null));
        await pasting;

        Assert.Single(reading.Invocations);
        Assert.Single(cut.FindAll("dialog.tb-smart-paste__box"));
    }

    [Fact]
    public async Task PasteAsync_ShouldSkipAValueNoFieldTakes_WhenTheModelReturnsOne()
    {
        Finds(new() { ["hikers"] = "a few", ["name"] = "Robin Vale", ["Phone"] = "555-0100", ["Email"] = " " });
        var cut = RenderForm();

        await cut.Tb().SmartPaste().PasteAsync(Signature);

        Assert.Equal(new Dictionary<string, object?> { ["Name"] = "Robin Vale" }, _values);
        Assert.Equal("Filled 1 field", cut.Tb().SmartPaste().Status);
    }

    private readonly record struct Choice(object? Value, string? Text = null, string? Content = null,
        bool Disabled = false);

    // A field's options: TbOption in a select or a list box, TbRadio or TbCheckboxItem in a group.
    private static (string Name, object? Value) Options<TOption>(params Choice[] choices) where TOption : IComponent =>
        ("ChildContent", (RenderFragment)(builder =>
        {
            foreach (var choice in choices)
            {
                builder.OpenComponent<TOption>(0);
                builder.AddComponentParameter(1, "Value", choice.Value);
                builder.AddComponentParameter(2, "Text", choice.Text);
                builder.AddComponentParameter(3, "Disabled", choice.Disabled);
                if (choice.Content is not null)
                {
                    builder.AddComponentParameter(4, "ChildContent",
                        (RenderFragment)(content => content.AddContent(0, choice.Content)));
                }

                builder.CloseComponent();
            }
        }));

    // One field of each kind that picks from options, with options smart paste can't offer among them.
    private void Trip(RenderTreeBuilder builder)
    {
        Field<TbSelect<string>, string>(builder, "Trail", Options<TbOption<string>>(new("ridge", "Ridge loop"),
            new("creek"), new("spur", "Summit spur", Content: "Summit spur, 4 km up"), new("bog", Content: "Bog walk"),
            new("closed", "Closed trail", Disabled: true), new("ridge-2", " ridge LOOP ")));
        Field<TbSelect<int?>, int?>(builder, "Guide", ("Items", new int?[] { 0, 1, 2, 3 }),
            ("ItemText", (Func<int?, string>)(guide => guide switch { 0 => " ", 1 => "Ana", 2 => "Bo", _ => "Cy" })),
            ("ItemDisabled", (Func<int?, bool>)(guide => guide == 3)));
        Field<TbListBox<string>, string>(builder, "Campsite", Options<TbOption<string>>(new("lake", "Lake shore"),
            new("pines", "Under the pines")));
        Field<TbMultiSelect<string>, IReadOnlyList<string>>(builder, "Gear", ("HelperText", "What you carry"),
            Options<TbOption<string>>(new("tent", "Tent"), new("stove", "Stove"), new("map", "Map")));
        Field<TbMultiListBox<int>, IReadOnlyList<int>>(builder, "Meals", Options<TbOption<int>>(new(1, "Breakfast"),
            new(2, "Lunch"), new(3, "Dinner")));
        Field<TbRadioGroup<int?>, int?>(builder, "Pace", Options<TbRadio<int>>(new(1, "Easy"), new(2, "Steady"),
            new(3, Content: "Fast"), new(4, "Closed", Disabled: true)));
        Field<TbCheckboxGroup<string>, IReadOnlyList<string>>(builder, "Days", _fewerOptions
            ? Options<TbCheckboxItem<string>>(new Choice("sat", "Saturday"))
            : Options<TbCheckboxItem<string>>(new("sat", "Saturday"), new("sun", "Sunday", Content: "Sunday, if dry")));
        Field<TbRadioGroup<string>, string>(builder, "Mood", _fewerOptions
            ? Options<TbRadio<string>>(new Choice("calm", "Calm"))
            : Options<TbRadio<string>>(new("calm", "Calm"), new("keen", "Keen")));
        Field<TbRadioGroup<string>, string>(builder, "Weather", Options<TbRadio<string>>(new("dry", Content: "Dry"),
            new("wet", Content: "Wet")));
    }

    private IEnumerable<(string Name, bool Multiple, string Choices)> Offered() =>
        _requests[^1].Fields.Select(field => (field.Name, field.Multiple, string.Join('|', field.Choices ?? [])));

    [Fact]
    public async Task Extract_ShouldGetEachOptionsTextAsAChoice_WhenAFieldPicksFromOptions()
    {
        var cut = RenderForm(Trip);

        await cut.Tb().SmartPaste().PasteAsync(Signature);

        // An option with content alone, a disabled one, one with blank text and a second with the same text aren't
        // offered, and Weather, with nothing to offer, is left out.
        Assert.Equal(
        [
            ("Trail", false, "Ridge loop|creek|Summit spur"), ("Guide", false, "Ana|Bo"),
            ("Campsite", false, "Lake shore|Under the pines"), ("Gear", true, "Tent|Stove|Map"),
            ("Meals", true, "Breakfast|Lunch|Dinner"), ("Pace", false, "Easy|Steady"),
            ("Days", true, "Saturday|Sunday"), ("Mood", false, "Calm|Keen"),
        ], Offered());
        var gear = _requests.Single().Fields[3];
        Assert.Equal(("What you carry", typeof(IReadOnlyList<string>)), (gear.Description, gear.Type));
    }

    [Fact]
    public void Equals_ShouldCompareTheChoicesOneByOne_WhenTwoFieldsHaveChoices()
    {
        var pace = new SmartPasteField("Pace", null, typeof(int?)) { Choices = ["Easy", "Steady"] };

        Assert.Equal(pace, new SmartPasteField("Pace", null, typeof(int?)) { Choices = ["Easy", "Steady"] });
        Assert.Equal(pace.GetHashCode(), (pace with { Choices = ["Easy", "Steady"] }).GetHashCode());
        Assert.NotEqual(pace, pace with { Choices = ["Steady", "Easy"] });
        Assert.NotEqual(pace, pace with { Choices = null });
        Assert.NotEqual(pace with { Choices = null }, pace);
        Assert.NotEqual(pace, pace with { Multiple = true });
        Assert.NotEqual(pace, pace with { Name = "Speed" });
        Assert.NotEqual(pace, pace with { Description = "How fast" });
        Assert.NotEqual(pace, pace with { Type = typeof(int) });
    }

    [Fact]
    public async Task Extract_ShouldGetTheOptionsThereNow_WhenAGroupsOptionWasRemoved()
    {
        var cut = RenderForm(Trip);
        _fewerOptions = true;
        cut.Render();

        await cut.Tb().SmartPaste().PasteAsync(Signature);

        Assert.Equal([("Days", true, "Saturday"), ("Mood", false, "Calm")], Offered().Skip(6));
    }

    [Fact]
    public async Task PasteAsync_ShouldChooseTheOptionsNamed_WhenTheModelAnswersWithTheirTexts()
    {
        Finds(new()
        {
            ["Trail"] = "RIDGE LOOP", ["Guide"] = "bo", ["Campsite"] = "under the pines",
            ["Gear"] = "Map\nSnorkel\ntent\nMAP", ["Meals"] = "Dinner\r\n\r\nBreakfast", ["Pace"] = "steady",
            ["Days"] = " sunday ", ["Mood"] = "Keen",
        });
        var cut = RenderForm(Trip);

        await cut.Tb().SmartPaste().PasteAsync(Signature);

        // Several choices come one to a line, and are chosen in the order named, each once.
        Assert.Equal("ridge", _values["Trail"]);
        Assert.Equal(2, _values["Guide"]);
        Assert.Equal("pines", _values["Campsite"]);
        Assert.Equal(["map", "tent"], Assert.IsAssignableFrom<IReadOnlyList<string>>(_values["Gear"]));
        Assert.Equal([3, 1], Assert.IsAssignableFrom<IReadOnlyList<int>>(_values["Meals"]));
        Assert.Equal(2, _values["Pace"]);
        Assert.Equal(["sun"], Assert.IsAssignableFrom<IReadOnlyList<string>>(_values["Days"]));
        Assert.Equal("keen", _values["Mood"]);
        Assert.Equal("Filled 8 fields", cut.Tb().SmartPaste().Status);
        Assert.True(_dirty);
    }

    [Fact]
    public async Task PasteAsync_ShouldSkipAnAnswer_WhenItNamesNoOptionTheFieldOffered()
    {
        // Content alone, a disabled option, two options for a field that takes one, and texts no option has.
        Finds(new()
        {
            ["Trail"] = "Bog walk", ["Guide"] = "Cy", ["Campsite"] = "Lake shore\nUnder the pines",
            ["Gear"] = "Snorkel\nKayak", ["Pace"] = "Closed", ["Days"] = "Saturday",
        });
        var cut = RenderForm(Trip);

        await cut.Tb().SmartPaste().PasteAsync(Signature);

        Assert.Equal("Days", Assert.Single(_values).Key);
        Assert.Equal("Filled 1 field", cut.Tb().SmartPaste().Status);
    }

    [Fact]
    public async Task PasteAsync_ShouldLeaveWhatItDidNotOfferAsItWas_WhenAFieldTakesSeveral()
    {
        Finds(new() { ["Gear"] = "Map", ["Meals"] = "Lunch", ["Days"] = "Sunday" });
        var cut = RenderForm(builder =>
        {
            Field<TbMultiSelect<string>, IReadOnlyList<string>>(builder, "Gear",
                ("Value", new[] { "permit", "stove", "whistle" }), Options<TbOption<string>>(
                    new("permit", "Permit", Disabled: true), new("stove", "Stove"), new("map", "Map")));
            Field<TbMultiListBox<int>, IReadOnlyList<int>>(builder, "Meals", ("Value", new[] { 1, 3 }),
                Options<TbOption<int>>(new(1, "Breakfast", Disabled: true), new(2, "Lunch"), new(3, "Dinner")));
            Field<TbCheckboxGroup<string>, IReadOnlyList<string>>(builder, "Days", ("Value", new[] { "fri", "sat" }),
                Options<TbCheckboxItem<string>>(new("fri", "Friday", Disabled: true), new("sat", "Saturday"),
                    new("sun", "Sunday")));
        });

        await cut.Tb().SmartPaste().PasteAsync(Signature);

        // A disabled option keeps its state, and so does a value no option has; the rest give way to those named.
        Assert.Equal(["permit", "whistle", "map"], Assert.IsAssignableFrom<IReadOnlyList<string>>(_values["Gear"]));
        Assert.Equal([1, 2], Assert.IsAssignableFrom<IReadOnlyList<int>>(_values["Meals"]));
        Assert.Equal(["fri", "sun"], Assert.IsAssignableFrom<IReadOnlyList<string>>(_values["Days"]));
    }

    private sealed record Place(string Key, string Name, params Place[] Inside)
    {
        public bool Heading { get; init; }

        public bool Closed { get; init; }
    }

    private static readonly Place[] Places =
    [
        new("north", "North", new Place("ridge", "Ridge"), new Place("tarn", "Tarn") { Closed = true })
        {
            Heading = true,
        },
        new("south", "South", new Place("dunes", "Dunes"), new Place("cove", "Cove", new Place("cave", "Cave"))),
    ];

    private static readonly CascaderItem<string>[] Routes =
    [
        new("Coast", "coast") { Children = [new("Cliff walk", "cliff"), new("Beach", "beach") { Disabled = true }] },
        new("Hills", "hills") { Children = [new("Cliff walk", "hill-cliff")] },
        new("Town loop", "town"),
    ];

    private static readonly (string Name, object? Value)[] TreeOfPlaces =
    [
        ("Items", Places), ("KeySelector", (Func<Place, string>)(place => place.Key)),
        ("ItemText", (Func<Place, string>)(place => place.Name)),
        ("ItemDisabled", (Func<Place, bool>)(place => place.Closed)),
    ];

    private static (string Name, object? Value)[] Tree(params (string Name, object? Value)[] more) =>
        [.. TreeOfPlaces, ("ChildrenSelector", (Func<Place, IEnumerable<Place>?>)(place => place.Inside)), .. more];

    // The same places, with each one's stops loaded as it opens.
    private static readonly (string Name, object? Value)[] LoadingTree =
    [
        .. TreeOfPlaces, ("HasChildren", (Func<Place, bool>)(place => place.Inside.Length > 0)),
        ("LoadChildren", (Func<Place, CancellationToken, Task<IEnumerable<Place>>>)((place, _) =>
            Task.FromResult<IEnumerable<Place>>(place.Inside))),
    ];

    private static List<AngleSharp.Dom.IElement> Rows(IRenderedComponent<TbForm> cut, string name) =>
        [.. cut.FindAll("[role=treeitem]").Where(row =>
            row.QuerySelector("[data-part=content]")!.TextContent.Trim() == name)];

    private static readonly (string Name, object? Value)[] Hikers =
        [("Items", new[] { "Ana", "Bo", "Cy", "Di" }), ("ItemText", (Func<string, string>)(hiker => $"{hiker} V."))];

    private static Task<IEnumerable<string>> FindNothing(string text, CancellationToken token) =>
        Task.FromResult<IEnumerable<string>>([]);

    // The other fields that choose from a list the page holds, and a tag input, which takes several texts.
    private void Outing(RenderTreeBuilder builder)
    {
        Field<TbAutocomplete<string>, string>(builder, "Ranger", ("Items", new[] { "Ana", "Bo" }));
        Field<TbAutocomplete<string>, string>(builder, "Station", ("Items", new[] { "East", "West" }),
            ("Search", (Func<string, CancellationToken, Task<IEnumerable<string>>>)FindNothing));
        Field<TbAutocomplete<string>, string>(builder, "Trailhead", ("Items", new[] { "Gate" }), ("FreeText", true));
        Field<TbTransferList<string>, IReadOnlyList<string>>(builder, "Hikers", Hikers);
        Field<TbTreeSelect<Place, string>, string>(builder, "Camp",
            Tree(("IsSelectable", (Func<Place, bool>)(place => !place.Heading))));
        Field<TbMultiTreeSelect<Place, string>, IReadOnlyList<string>>(builder, "Stops", Tree());
        Field<TbCascader<string>, string>(builder, "Route", ("Items", Routes));
        Field<TbTagInput, IReadOnlyList<string>>(builder, "Tags");
    }

    [Fact]
    public async Task Extract_ShouldGetTheChoicesOfEachFieldThatHoldsItsList_WhenCalled()
    {
        var cut = RenderForm(Outing);

        await cut.Tb().SmartPaste().PasteAsync(Signature);

        // An autocomplete with a search is left out, one with free text takes any text, a heading and a disabled
        // item aren't offered, and a cascader offers whole paths.
        Assert.Equal(
        [
            ("Ranger", false, "Ana|Bo"), ("Trailhead", false, ""), ("Hikers", true, "Ana V.|Bo V.|Cy V.|Di V."),
            ("Camp", false, "Ridge|South|Dunes|Cove|Cave"), ("Stops", true, "North|Ridge|South|Dunes|Cove|Cave"),
            ("Route", false, "Coast / Cliff walk|Hills / Cliff walk|Town loop"), ("Tags", true, ""),
        ], Offered());
        Assert.Null(_requests.Single().Fields[^1].Choices);
    }

    [Fact]
    public async Task PasteAsync_ShouldChooseFromEachFieldsList_WhenTheModelAnswersWithItsTexts()
    {
        Finds(new()
        {
            ["Ranger"] = "bo", ["Hikers"] = "di v.\nAna V.", ["Camp"] = "dunes", ["Stops"] = "South\nNorth",
            ["Route"] = "hills / cliff walk", ["Tags"] = "steep, loop\nSteep\nshaded",
        });
        var cut = RenderForm(Outing);

        await cut.Tb().SmartPaste().PasteAsync(Signature);

        // A transfer list keeps its items' order. With cascading checks, a stop brings the stops inside it that
        // aren't disabled, and North, with a stop left unchecked, isn't in the value, as after a check in the tree.
        // The stops come in tree order. A tag given twice is added once.
        Assert.Equal("Bo", _values["Ranger"]);
        Assert.Equal(["Ana", "Di"], Assert.IsAssignableFrom<IReadOnlyList<string>>(_values["Hikers"]));
        Assert.Equal("dunes", _values["Camp"]);
        Assert.Equal(["ridge", "south", "dunes", "cove", "cave"],
            Assert.IsAssignableFrom<IReadOnlyList<string>>(_values["Stops"]));
        Assert.Equal("hill-cliff", _values["Route"]);
        Assert.Equal(["steep", "loop", "shaded"], Assert.IsAssignableFrom<IReadOnlyList<string>>(_values["Tags"]));
        Assert.Equal("Filled 6 fields", cut.Tb().SmartPaste().Status);
    }

    [Fact]
    public async Task PasteAsync_ShouldSkipAnAnswer_WhenAFieldsListHasNoSuchItem()
    {
        // A heading, a disabled item, a path that ends at a disabled choice, a step of a path, and tags all refused.
        Finds(new()
        {
            ["Ranger"] = "Cy", ["Camp"] = "North", ["Stops"] = "Tarn", ["Route"] = "Coast / Beach", ["Way"] = "Coast",
            ["Codes"] = "a, b", ["Hikers"] = "Bo V.",
        });
        var cut = RenderForm(builder =>
        {
            Outing(builder);
            Field<TbCascader<string>, string>(builder, "Way", ("Items", Routes));
            Field<TbTagInput, IReadOnlyList<string>>(builder, "Codes",
                ("TagValidation", (Func<string, string?>)(_ => "Codes are closed.")));
        });

        await cut.Tb().SmartPaste().PasteAsync(Signature);

        Assert.Equal("Hikers", Assert.Single(_values).Key);
        Assert.Equal("Filled 1 field", cut.Tb().SmartPaste().Status);
        // The refused tags leave no message behind, unlike a typed tag that is refused.
        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public async Task PasteAsync_ShouldKeepToEachListFieldsOwnRules_WhenItHoldsAValueAlready()
    {
        Finds(new()
        {
            ["Hikers"] = "Cy V.", ["Stops"] = "South", ["Camps"] = "South", ["Way"] = "coast", ["Tags"] = "a\nb\nc",
            ["Rests"] = "Dunes\nCove", ["Naps"] = "Dunes\nCove", ["Nooks"] = "Cave",
        });
        var cut = RenderForm(builder =>
        {
            Field<TbTransferList<string>, IReadOnlyList<string>>(builder, "Hikers",
                [.. Hikers, ("Value", new[] { "Zed", "Bo" })]);
            Field<TbMultiTreeSelect<Place, string>, IReadOnlyList<string>>(builder, "Stops",
                Tree(("Value", new[] { "tarn", "elsewhere", "ridge" })));
            Field<TbMultiTreeSelect<Place, string>, IReadOnlyList<string>>(builder, "Camps",
                Tree(("CascadeChecks", false)));
            Field<TbMultiTreeSelect<Place, string>, IReadOnlyList<string>>(builder, "Rests", Tree());
            Field<TbMultiTreeSelect<Place, string>, IReadOnlyList<string>>(builder, "Nooks", Tree());
            Field<TbMultiTreeSelect<Place, string>, IReadOnlyList<string>>(builder, "Naps",
                Tree(("IsSelectable", (Func<Place, bool>)(place => place.Key != "south"))));
            Field<TbCascader<string>, string>(builder, "Way", ("Items", Routes), ("AllowParents", true));
            Field<TbTagInput, IReadOnlyList<string>>(builder, "Tags", ("Value", new[] { "old" }), ("MaxTags", 2));
        });

        await cut.Tb().SmartPaste().PasteAsync(Signature);

        // A chosen value with no item stays, last. A disabled stop stays checked, and one that was offered and isn't
        // named goes. Without cascading checks, South comes alone. The new tags replace the old, up to the limit.
        Assert.Equal(["Cy", "Zed"], Assert.IsAssignableFrom<IReadOnlyList<string>>(_values["Hikers"]));
        Assert.Equal(["tarn", "south", "dunes", "cove", "cave", "elsewhere"],
            Assert.IsAssignableFrom<IReadOnlyList<string>>(_values["Stops"]));
        Assert.Equal(["south"], Assert.IsAssignableFrom<IReadOnlyList<string>>(_values["Camps"]));
        // Every stop inside South is named, so South is checked too, unless it can't be chosen.
        Assert.Equal(["south", "dunes", "cove", "cave"],
            Assert.IsAssignableFrom<IReadOnlyList<string>>(_values["Rests"]));
        Assert.Equal(["dunes", "cove", "cave"], Assert.IsAssignableFrom<IReadOnlyList<string>>(_values["Naps"]));
        // Cave is all there is inside Cove, so Cove is checked, though Dunes, beside it, isn't.
        Assert.Equal(["cove", "cave"], Assert.IsAssignableFrom<IReadOnlyList<string>>(_values["Nooks"]));
        Assert.Equal("coast", _values["Way"]);
        Assert.Equal(["a", "b"], Assert.IsAssignableFrom<IReadOnlyList<string>>(_values["Tags"]));
    }

    [Fact]
    public async Task PasteAsync_ShouldCheckLoadedItemsAsTheTreeDoes_WhenAMultipleTreeSelectLoadsItsItems()
    {
        Finds(new() { ["Trips"] = "North", ["Walks"] = "Dunes\nCove", ["Hikes"] = "South" });
        var cut = RenderForm(builder =>
        {
            Field<TbMultiTreeSelect<Place, string>, IReadOnlyList<string>>(builder, "Trips", LoadingTree);
            Field<TbMultiTreeSelect<Place, string>, IReadOnlyList<string>>(builder, "Walks", LoadingTree);
            Field<TbMultiTreeSelect<Place, string>, IReadOnlyList<string>>(builder, "Hikes", LoadingTree);
        });
        // North opens in Trips and South in Walks, so the stops inside them load. Nothing loads in Hikes.
        Rows(cut, "North")[0].QuerySelector("[data-part=toggle]")!.Click();
        Rows(cut, "South")[1].QuerySelector("[data-part=toggle]")!.Click();
        cut.WaitForAssertion(() => Assert.Equal((1, 1), (Rows(cut, "Ridge").Count, Rows(cut, "Dunes").Count)));

        await cut.Tb().SmartPaste().PasteAsync(Signature);

        // North brings Ridge and stays out, since Tarn, disabled, isn't checked. Dunes and Cove put South in; what
        // is inside Cove hasn't loaded.
        Assert.Equal(["ridge"], Assert.IsAssignableFrom<IReadOnlyList<string>>(_values["Trips"]));
        Assert.Equal(["south", "dunes", "cove"], Assert.IsAssignableFrom<IReadOnlyList<string>>(_values["Walks"]));
        Assert.Equal(["south"], Assert.IsAssignableFrom<IReadOnlyList<string>>(_values["Hikes"]));
    }

    [Fact]
    public async Task PasteAsync_ShouldLeaveAHeadingChecked_WhenItWasCheckedBeforeItsItemsLoaded()
    {
        Finds(new() { ["Tours"] = "Dunes" });
        // The field is bound: each render gives it the value it last raised.
        var cut = RenderForm(builder => Field<TbMultiTreeSelect<Place, string>, IReadOnlyList<string>>(builder, "Tours",
            [.. LoadingTree, ("IsSelectable", (Func<Place, bool>)(place => !place.Heading)),
                ("Value", _values.GetValueOrDefault("Tours"))]));
        Rows(cut, "South")[0].QuerySelector("[data-part=toggle]")!.Click();
        cut.WaitForAssertion(() => Assert.Single(Rows(cut, "Dunes")));
        // North can't be chosen and nothing inside it has loaded, so checking it changes no value.
        Rows(cut, "North")[0].QuerySelector("[data-part=check]")!.Click();
        Assert.Empty(_values);

        await cut.Tb().SmartPaste().PasteAsync(Signature);
        cut.Render();

        Assert.Equal(["dunes"], Assert.IsAssignableFrom<IReadOnlyList<string>>(_values["Tours"]));
        Assert.Equal("true", Rows(cut, "North")[0].GetAttribute("aria-checked"));
        // The stops inside North come checked as they load, apart from the disabled one.
        Rows(cut, "North")[0].QuerySelector("[data-part=toggle]")!.Click();
        cut.WaitForAssertion(() => Assert.Equal(["dunes", "ridge"],
            Assert.IsAssignableFrom<IReadOnlyList<string>>(_values["Tours"])));
    }

    [Fact]
    public async Task PasteAsync_ShouldAskTheLocalizerToWordTheCount_WhenFieldsAreFilled()
    {
        Services.AddScoped<ITbLocalizer, PluralRecorder>();
        Finds(new() { ["Name"] = "Robin Vale", ["Email"] = "robin@ridge.example" });
        var cut = RenderForm();

        await cut.Tb().SmartPaste().PasteAsync(Signature);

        Assert.Equal("SmartPaste.Filled for 2", cut.Tb().SmartPaste().Status);
    }

    [Fact]
    public async Task PasteAsync_ShouldSayItFoundNothing_WhenTheModelFindsNoValues()
    {
        var cut = RenderForm();

        await cut.Tb().SmartPaste().PasteAsync(Signature);

        Assert.Empty(_values);
        Assert.Equal("Found nothing to fill", cut.Tb().SmartPaste().Status);
    }

    [Fact]
    public async Task PasteAsync_ShouldSayItFoundNothingWithoutAskingTheModel_WhenNoFieldCanBeFilled()
    {
        var cut = RenderForm(builder =>
            Field<TbTextField<string>, string>(builder, "Password", ("Type", TextFieldType.Password)));

        await cut.Tb().SmartPaste().PasteAsync(Signature);

        Assert.Empty(_requests);
        Assert.Equal("Found nothing to fill", cut.Tb().SmartPaste().Status);
    }

    [Fact]
    public async Task PasteAsync_ShouldSayItFoundNothingWithoutAskingTheModel_WhenGivenBlankText()
    {
        var cut = RenderForm();

        await cut.Tb().SmartPaste().PasteAsync("  ");

        Assert.Empty(_requests);
        Assert.Equal("Found nothing to fill", cut.Tb().SmartPaste().Status);
    }

    [Fact]
    public async Task PasteAsync_ShouldShowItFailed_WhenTheModelThrows()
    {
        _model = (_, _) => throw new HttpRequestException("The model is down.");
        var cut = RenderForm();

        await cut.Tb().SmartPaste().PasteAsync(Signature);

        Assert.Empty(_values);
        Assert.Equal("Couldn't fill the form", cut.Tb().SmartPaste().Status);
        Assert.False(cut.Find("[data-part=button]").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Click_ShouldFillFromTheClipboardWithoutTheBox_WhenTheBrowserSharesIt()
    {
        JSInterop.SetupModule("./_content/Trailblazor/smart-paste.js").Setup<string?>("readClipboard")
            .SetResult(Signature);
        Finds(new() { ["Name"] = "Robin Vale" });
        var cut = RenderForm();

        await cut.Find(".tb-smart-paste [data-part=button]").ClickAsync(new());

        Assert.Equal(Signature, _requests.Single().Text);
        Assert.Equal("Robin Vale", _values["Name"]);
        Assert.Empty(cut.FindAll("dialog.tb-smart-paste__box"));
    }

    [Fact]
    public async Task Cancel_ShouldCloseTheBoxWithoutFillingOrKeepingTheText_WhenPressed()
    {
        var cut = RenderForm();
        await cut.Find(".tb-smart-paste [data-part=button]").ClickAsync(new());
        var box = cut.Find("dialog.tb-smart-paste__box");
        Assert.Equal("Paste text to fill the form", cut.Tb().Dialog().Title);
        Assert.Equal("Text to fill from", box.QuerySelector("label[for]")!.TextContent.Trim());
        await box.QuerySelector("[data-part=text]")!.ChangeAsync(new() { Value = Signature });

        await cut.Find("dialog.tb-smart-paste__box [data-part=cancel]").ClickAsync(new());

        await cut.Tb().Dialog().WaitForClosedAsync();
        Assert.Empty(_requests);
        Assert.Equal("", cut.Tb().SmartPaste().Status);
        await cut.Find(".tb-smart-paste [data-part=button]").ClickAsync(new());
        Assert.Equal("", cut.Find("dialog.tb-smart-paste__box [data-part=text]").GetAttribute("value") ?? "");
    }

    [Fact]
    public async Task Click_ShouldShowItsFillingAndIgnoreAnotherPress_WhenPressedWhileTheModelReads()
    {
        var reading = new TaskCompletionSource<IReadOnlyDictionary<string, string>>();
        _model = (_, _) => reading.Task;
        JSInterop.SetupModule("./_content/Trailblazor/smart-paste.js").Setup<string?>("readClipboard")
            .SetResult(Signature);
        var cut = RenderForm();
        var button = () => cut.Find(".tb-smart-paste [data-part=button]");

        var pasting = button().ClickAsync(new());
        Assert.Equal("Filling the form", cut.Tb().SmartPaste().Status);
        Assert.NotNull(button().QuerySelector("[data-part=spinner]"));
        await button().ClickAsync(new());
        await cut.InvokeAsync(() => reading.SetResult(new Dictionary<string, string> { ["Name"] = "Robin Vale" }));
        await pasting;

        Assert.Single(_requests);
        Assert.Equal("Filled 1 field", cut.Tb().SmartPaste().Status);
        Assert.Null(button().QuerySelector("[data-part=spinner]"));
    }

    [Fact]
    public async Task Dispose_ShouldCancelTheModel_WhenTheButtonGoesAwayWhileItReads()
    {
        var token = CancellationToken.None;
        _model = async (_, given) =>
        {
            token = given;
            await Task.Delay(Timeout.Infinite, given);
            return new Dictionary<string, string>();
        };
        JSInterop.SetupModule("./_content/Trailblazor/smart-paste.js").Setup<string?>("readClipboard")
            .SetResult(Signature);
        var cut = RenderForm();
        var pasting = cut.Find(".tb-smart-paste [data-part=button]").ClickAsync(new());

        cut.Render(parameters => parameters.Add(form => form.ChildContent, (RenderFragment)Contact));
        await pasting;

        Assert.True(token.IsCancellationRequested);
        Assert.Equal(["Filling the form"], Services.GetRequiredService<FakeAnnouncer>().Polite);
    }

    [Fact]
    public void Render_ShouldNeedAForm_WhenRenderedOutsideOne()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbSmartPaste>(parameters =>
            parameters.Add(paste => paste.Extract, Extract)));

        Assert.Equal("TbSmartPaste goes inside a TbForm.", thrown.Message);
    }

    [Fact]
    public void Render_ShouldTurnTheButtonOff_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderForm();

        Assert.True(cut.Find(".tb-smart-paste [data-part=button]").HasAttribute("disabled"));
    }

    [Fact]
    public void Render_ShouldTurnTheButtonOff_WhenTheFormIsDisabled()
    {
        var cut = RenderForm();

        cut.Render(parameters => parameters.Add(form => form.Disabled, true));

        Assert.True(cut.Find(".tb-smart-paste [data-part=button]").HasAttribute("disabled"));
    }

    [Fact]
    public async Task PasteAsync_ShouldThrow_WhenTheButtonIsDisabled()
    {
        var cut = RenderForm(paste: ("Disabled", true));

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cut.Tb().SmartPaste().PasteAsync(Signature));

        Assert.Equal("The smart paste button is disabled.", thrown.Message);
    }

    [Fact]
    public async Task Labels_ShouldWordTheButtonBoxAndStatus_WhenGiven()
    {
        Finds(new() { ["Name"] = "Robin Vale", ["Email"] = "robin@ridge.example" });
        var cut = RenderForm(paste:
        [
            ("Label", "Paste a signature"),
            ("Labels", new SmartPasteLabels { Title = "Paste a signature", Text = "Signature", Fill = "Go",
                Filling = "Reading", Filled = "{0} filled" }),
        ]);

        await cut.Find(".tb-smart-paste [data-part=button]").ClickAsync(new());
        Assert.Equal("Paste a signature", cut.Tb().Dialog().Title);
        Assert.Equal("Signature", cut.Find("dialog label[for]").TextContent.Trim());
        Assert.Equal("Go", cut.Find("dialog [data-part=fill]").TextContent.Trim());
        await cut.Find("dialog [data-part=text]").ChangeAsync(new() { Value = Signature });
        await cut.Find("dialog [data-part=fill]").ClickAsync(new());

        Assert.Equal("2 filled", cut.Tb().SmartPaste("Paste a signature").Status);
        Assert.Equal(["Reading", "2 filled"], Services.GetRequiredService<FakeAnnouncer>().Polite);
    }

    [Fact]
    public async Task TelemetryName_ShouldCountItShownAndEachPaste_WhenGiven()
    {
        var meters = Services.GetRequiredService<IMeterFactory>();
        using var shown = new MetricCollector<long>(meters, "Trailblazor", "trailblazor.control.shown");
        using var used = new MetricCollector<long>(meters, "Trailblazor", "trailblazor.control.used");
        var cut = RenderForm(paste: ("TelemetryName", "permit-paste"));

        await cut.Tb().SmartPaste().PasteAsync(Signature);

        Assert.Equal(["permit-paste"], shown.GetMeasurementSnapshot().Select(measurement =>
            measurement.Tags["trailblazor.telemetry_name"]));
        Assert.Equal(["paste"], used.GetMeasurementSnapshot()
            .Where(measurement => Equals(measurement.Tags["trailblazor.telemetry_name"], "permit-paste"))
            .Select(measurement => measurement.Tags["trailblazor.action"]));
    }
}
