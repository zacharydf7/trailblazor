using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Trailblazor.Tests;

public class TbContractTests
{
    // An app's own field, built on a Trailblazor one, which keeps every rule.
    private sealed class NameField : ComponentBase
    {
        [Parameter, EditorRequired]
        public string Label { get; set; } = "";

        [Parameter]
        public string? Value { get; set; }

        [Parameter]
        public EventCallback<string?> ValueChanged { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TbTextField<string>>(0);
            builder.AddComponentParameter(1, nameof(TbTextField<string>.Label), Label);
            builder.AddComponentParameter(2, nameof(TbTextField<string>.Value), Value);
            builder.AddComponentParameter(3, nameof(TbTextField<string>.ValueChanged), ValueChanged);
            builder.AddComponentParameter(4, nameof(TbTextField<string>.Disabled), Disabled);
            builder.CloseComponent();
        }
    }

    // A field that breaks the rules: it reports the value it's given, keeps listening after it's gone, and gives its
    // buttons new handlers on every render.
    private sealed class CarelessField : ComponentBase
    {
        [Inject]
        private NavigationManager Navigation { get; set; } = default!;

        [Parameter]
        public string? Value { get; set; }

        [Parameter]
        public EventCallback<string?> ValueChanged { get; set; }

        protected override void OnInitialized() => Navigation.LocationChanged += (_, _) => { };

        protected override Task OnParametersSetAsync() => ValueChanged.InvokeAsync(Value);

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            foreach (var choice in new[] { "Pine", "Cedar" })
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, () => Value = choice));
                builder.AddContent(2, choice);
                builder.CloseElement();
            }
        }
    }

    // A field that takes no notice of Disabled.
    private sealed class StubbornField : ComponentBase
    {
        [Parameter]
        public string? Value { get; set; }

        [Parameter]
        public EventCallback<string?> ValueChanged { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, Choose));
            builder.CloseElement();
        }

        private Task Choose() => ValueChanged.InvokeAsync("Cedar");
    }

    // A watch that keeps its timer and its browser callback when it's gone.
    private sealed class LingeringWatch : ComponentBase
    {
        // Held in a field like a real component's timer: one nothing refers to can be garbage-collected and never fire.
        private ITimer? _timer;

        [Inject]
        private TimeProvider Time { get; set; } = default!;

        [Inject]
        private IJSRuntime Script { get; set; } = default!;

        [Parameter]
        public EventCallback OnTick { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _timer = Time.CreateTimer(_ => InvokeAsync(OnTick.InvokeAsync), null, TimeSpan.FromMinutes(1),
                    Timeout.InfiniteTimeSpan);
                await Script.InvokeVoidAsync("watch", DotNetObjectReference.Create(this));
            }
        }
    }

    // A list whose rows each have a checkbox and a remove button, with handlers kept from render to render.
    private sealed class TrailList : ComponentBase
    {
        private readonly List<string> _trails = ["Pine", "Cedar"];
        private readonly Dictionary<string, Action> _removes = [];
        private readonly Action _check = () => { };

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            foreach (var trail in _trails)
            {
                builder.OpenElement(0, "li");
                builder.SetKey(trail);
                builder.OpenElement(1, "input");
                builder.AddAttribute(2, "type", "checkbox");
                builder.AddAttribute(3, "onchange", EventCallback.Factory.Create(this, _check));
                builder.CloseElement();
                builder.OpenElement(4, "button");
                builder.AddAttribute(5, "aria-label", $"Remove {trail}");
                builder.AddAttribute(6, "onclick", EventCallback.Factory.Create(this, RemoveOf(trail)));
                builder.CloseElement();
                builder.CloseElement();
            }
        }

        private Action RemoveOf(string trail) =>
            _removes.TryGetValue(trail, out var remove) ? remove : _removes[trail] = () => _trails.Remove(trail);
    }

    private sealed class LinkField : ComponentBase
    {
        [Parameter]
        public Uri? Link { get; set; }

        [Parameter]
        public EventCallback<Uri?> LinkChanged { get; set; }
    }

    private sealed class LinksField : ComponentBase
    {
        [Parameter]
        public IReadOnlyList<Uri>? Links { get; set; }

        [Parameter]
        public EventCallback<IReadOnlyList<Uri>?> LinksChanged { get; set; }
    }

    private sealed class DistanceField : ComponentBase
    {
        [Parameter]
        public SliderRange<double> Distance { get; set; }

        [Parameter]
        public EventCallback<SliderRange<double>> DistanceChanged { get; set; }
    }

    private sealed class TrailNames
    {
        public string First => "Pine";
    }

    private sealed class TrailNameText : ComponentBase
    {
        [Inject]
        private TrailNames Names { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, Names.First);
    }

    [Fact]
    public async Task VerifyAsync_ShouldPass_WhenTheComponentKeepsEveryRule()
    {
        await TbContract.VerifyAsync<NameField>(setup => setup.Parameter(nameof(NameField.Label), "Name"));
    }

    [Fact]
    public async Task VerifyAsync_ShouldListEachRuleItBreaks_WhenTheComponentBreaksThem()
    {
        var error = await Assert.ThrowsAsync<TbContractException>(() => TbContract.VerifyAsync<CarelessField>());

        Assert.Contains(error.Failures, failure => failure.Contains("raised ValueChanged"));
        Assert.Contains(error.Failures, failure => failure.Contains("left a handler behind"));
        Assert.Contains(error.Failures, failure => failure.Contains("lost its onclick handler"));
    }

    [Fact]
    public async Task VerifyAsync_ShouldAskForSamples_WhenABoundTypeHasNone()
    {
        var error = await Assert.ThrowsAsync<TbContractException>(() => TbContract.VerifyAsync<LinkField>());

        Assert.Equal("Call Sample with two Uri values, so Link can be set from the parent.",
            Assert.Single(error.Failures));
    }

    [Fact]
    public async Task VerifyAsync_ShouldSetTheSamplesGiven_WhenABoundTypeHasThem()
    {
        await TbContract.VerifyAsync<LinkField>(setup =>
            setup.Sample(new Uri("https://example.com/pine"), new Uri("https://example.com/cedar")));
    }

    [Fact]
    public async Task VerifyAsync_ShouldSetSamplesOfATypeTheBoundOneAccepts_WhenGivenThem()
    {
        await TbContract.VerifyAsync<LinksField>(setup =>
            setup.Sample(new[] { new Uri("https://example.com/pine") },
                new[] { new Uri("https://example.com/cedar") }));
    }

    [Fact]
    public async Task VerifyAsync_ShouldNeedNoSamples_WhenTheBoundTypeIsARangeOfATypeWithSamples()
    {
        await TbContract.VerifyAsync<DistanceField>();
    }

    [Fact]
    public async Task VerifyAsync_ShouldPass_WhenRemovingARowMovesTheNextRowsPartsIntoItsPlace()
    {
        await TbContract.VerifyAsync<TrailList>();
    }

    [Fact]
    public async Task VerifyAsync_ShouldReportAChangedValue_WhenTheFieldChangesItWhileDisabled()
    {
        var error = await Assert.ThrowsAsync<TbContractException>(() => TbContract.VerifyAsync<StubbornField>());

        Assert.Contains("StubbornField changed its value while Disabled.", error.Failures);
    }

    [Fact]
    public async Task VerifyAsync_ShouldReportWhatKeepsRunning_WhenTheComponentIsDisposed()
    {
        var error = await Assert.ThrowsAsync<TbContractException>(() => TbContract.VerifyAsync<LingeringWatch>(
            setup => setup.Context(context => context.JSInterop.SetupVoid("watch", _ => true))));

        Assert.Equal(["LingeringWatch left a reference to LingeringWatch that the browser can still call.",
            "LingeringWatch called its parent after it was disposed."], error.Failures);
    }

    [Fact]
    public async Task VerifyAsync_ShouldRenderWithTheServicesGiven_WhenTheComponentNeedsThem()
    {
        await TbContract.VerifyAsync<TrailNameText>(setup =>
            setup.Services(services => services.AddSingleton<TrailNames>()));
    }
}
