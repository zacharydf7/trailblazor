using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbSignaturePadTests : BunitContext
{
    public TbSignaturePadTests()
    {
        this.AddTrailblazor();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private Signature? Signed { get; set; }

    private IRenderedComponent<TbSignaturePad> RenderPad(bool disabled = false, bool readOnly = false) =>
        Render<TbSignaturePad>(parameters => parameters.Add(pad => pad.Label, "Signature")
            .Add(pad => pad.Value, Signed).Add(pad => pad.Disabled, disabled).Add(pad => pad.ReadOnly, readOnly)
            .Add(pad => pad.ValueChanged, (Signature signature) => Signed = signature));

    private static string? SurfaceLabel(IRenderedComponent<TbSignaturePad> cut) =>
        cut.Find("[data-part=surface]").GetAttribute("aria-label");

    [Fact]
    public void Pad_ShouldSayNothingIsDrawn_WhenItStartsEmpty()
    {
        var cut = RenderPad();

        Assert.Equal("Signature, nothing drawn", SurfaceLabel(cut));
        Assert.Equal("img", cut.Find("[data-part=surface]").GetAttribute("role"));
        Assert.True(cut.Find("[data-part=undo]").HasAttribute("disabled"));
        Assert.True(cut.Find("[data-part=clear]").HasAttribute("disabled"));
        Assert.False(cut.Tb().SignaturePad("Signature").IsSigned);
    }

    [Fact]
    public async Task DrawAsync_ShouldAddTheStrokeToTheValue_WhenAStrokeIsDrawn()
    {
        var cut = RenderPad();

        await cut.Tb().SignaturePad("Signature").DrawAsync((10, 20), (30, 40), (50.5, 60));

        Assert.Equal([new SignaturePoint(10, 20), new(30, 40), new(50.5, 60)], Signed!.Strokes.Single().Points);
        Assert.Equal("Signature, signed", SurfaceLabel(cut));
        Assert.Equal("M10 20 L30 40 L50.5 60", cut.Find("[data-part=surface] > svg > path").GetAttribute("d"));
        Assert.True(cut.Tb().SignaturePad("Signature").IsSigned);
    }

    [Fact]
    public async Task DrawAsync_ShouldKeepThePointsInsideThePad_WhenAStrokeRunsOffIt()
    {
        var cut = RenderPad();

        await cut.Tb().SignaturePad("Signature").DrawAsync((-5, 250), (700, 100));

        Assert.Equal([new SignaturePoint(0, 200), new(600, 100)], Signed!.Strokes.Single().Points);
    }

    [Fact]
    public async Task UndoAsync_ShouldTakeBackTheLastStroke_WhenThereAreSeveral()
    {
        var cut = RenderPad();
        var pad = cut.Tb().SignaturePad("Signature");
        await pad.DrawAsync((10, 20), (30, 40));
        await pad.DrawAsync((100, 20), (130, 40));

        await pad.UndoAsync();

        Assert.Equal(new SignaturePoint(10, 20), Signed!.Strokes.Single().Points[0]);
        await pad.UndoAsync();
        Assert.Null(Signed);
    }

    [Fact]
    public async Task ClearAsync_ShouldEmptyThePad_WhenSomethingIsDrawn()
    {
        var cut = RenderPad();
        var pad = cut.Tb().SignaturePad("Signature");
        await pad.DrawAsync((10, 20), (30, 40));

        await pad.ClearAsync();

        Assert.Null(Signed);
        Assert.Empty(cut.FindAll("[data-part=surface] > svg > path"));
    }

    [Fact]
    public async Task TypeNameAsync_ShouldSignWithTheTypedName_WhenTheNameIsTyped()
    {
        var cut = RenderPad();

        await cut.Tb().SignaturePad("Signature").TypeNameAsync("Ada Lovelace");

        Assert.Equal(("Ada Lovelace", 0), (Signed!.TypedName, Signed.Strokes.Count));
        Assert.Equal("true", cut.Find("[data-part=type-name]").GetAttribute("aria-pressed"));
        Assert.Empty(cut.FindAll("[data-part=surface]"));
    }

    [Fact]
    public async Task TypeNameAsync_ShouldLeaveThePadUnsigned_WhenTheNameIsOnlySpaces()
    {
        var cut = RenderPad();

        await cut.Tb().SignaturePad("Signature").TypeNameAsync("   ");

        Assert.Null(Signed);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void Validate_ShouldCountAnEmptySignatureAsMissing_WhenThePadIsRequired(bool signed, bool valid)
    {
        var context = new EditContext(new object());
        Render<EditForm>(parameters => parameters.Add(form => form.EditContext, context)
            .Add(form => form.ChildContent, (RenderFragment<EditContext>)(_ => builder =>
            {
                builder.OpenComponent<TbSignaturePad>(0);
                builder.AddAttribute(1, nameof(TbSignaturePad.Label), "Signature");
                builder.AddAttribute(2, nameof(TbSignaturePad.Required), true);
                builder.AddAttribute(3, nameof(TbSignaturePad.Value), signed ? new Signature([], "Ada") : Signature.Empty);
                builder.CloseComponent();
            })));

        Assert.Equal(valid, context.Validate());
    }

    [Fact]
    public async Task TypeName_ShouldStartTheSignatureOver_WhenTheToggleSwitchesBackToDrawing()
    {
        var cut = RenderPad();
        await cut.Tb().SignaturePad("Signature").TypeNameAsync("Ada Lovelace");

        await cut.Find("[data-part=type-name]").ClickAsync(new());

        Assert.Null(Signed);
        Assert.Equal("false", cut.Find("[data-part=type-name]").GetAttribute("aria-pressed"));
        Assert.Single(cut.FindAll("[data-part=surface]"));
    }

    [Fact]
    public void TypeName_ShouldShowTheNameField_WhenTheValueIsATypedName()
    {
        Signed = new Signature([], "Ada Lovelace");

        var cut = RenderPad();

        Assert.Equal("true", cut.Find("[data-part=type-name]").GetAttribute("aria-pressed"));
        Assert.Equal("Ada Lovelace", cut.Find("[data-part=typed-name] input").GetAttribute("value"));
        Assert.Empty(cut.FindAll("[data-part=surface]"));
    }

    [Fact]
    public async Task TypeName_ShouldSwitchToTheNameField_WhenTheAppKeepsAnEmptySignature()
    {
        Signed = Signature.Empty;
        var cut = RenderPad();

        await cut.Find("[data-part=type-name]").ClickAsync(new());
        cut.Render(parameters => parameters.Add(pad => pad.Value, Signature.Empty));

        Assert.Equal("true", cut.Find("[data-part=type-name]").GetAttribute("aria-pressed"));
        Assert.Single(cut.FindAll("[data-part=typed-name]"));
    }

    [Fact]
    public void Pad_ShouldAttachItsScriptAgain_WhenTheValueGoesFromTypedBackToDrawn()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        var module = JSInterop.SetupModule("./_content/Trailblazor/signature-pad.js");
        module.SetupModule("createSignaturePad", _ => true);
        var cut = RenderPad();

        cut.Render(parameters => parameters.Add(pad => pad.Value, new Signature([], "Ada Lovelace")));
        cut.Render(parameters => parameters.Add(pad => pad.Value, new Signature([new([new(1, 2), new(3, 4)])])));

        cut.WaitForAssertion(() => Assert.Equal(2, module.Invocations["createSignaturePad"].Count));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task DrawAsync_ShouldChangeNothing_WhenThePadIsDisabledOrReadOnly(bool disabled, bool readOnly)
    {
        var cut = RenderPad(disabled, readOnly);

        await cut.Tb().SignaturePad("Signature").DrawAsync((10, 20), (30, 40));

        Assert.Null(Signed);
        Assert.True(cut.Find("[data-part=surface]").HasAttribute("data-locked"));
        Assert.True(cut.Find("[data-part=type-name]").HasAttribute("disabled"));
    }

    [Fact]
    public void ToSvg_ShouldDrawEachStrokeAsARoundEndedPath_WhenCalled()
    {
        var signature = new Signature([new([new(1, 2), new(3, 4)]), new([new(5, 6)])]);

        var svg = signature.ToSvg("#123");

        Assert.StartsWith("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 600 200\"", svg);
        Assert.Contains("<path d=\"M1 2 L3 4\" fill=\"none\" stroke=\"#123\" stroke-width=\"3\" stroke-linecap=\"round\"", svg);
        Assert.Contains("<path d=\"M5 6 L5 6\"", svg);
    }

    [Fact]
    public void ToSvg_ShouldWriteTheTypedNameAsText_WhenTheNameIsTyped()
    {
        var svg = new Signature([], "Ada <Byron>").ToSvg();

        Assert.Contains(">Ada &lt;Byron&gt;</text>", svg);
        Assert.DoesNotContain("<path", svg);
    }

    [Fact]
    public void Equals_ShouldCompareStrokesPointByPoint_WhenCalled()
    {
        Signature Make(double x) => new([new([new(1, 2), new(x, 4)])]);

        Assert.Equal(Make(3), Make(3));
        Assert.Equal(Make(3).GetHashCode(), Make(3).GetHashCode());
        Assert.NotEqual(Make(3), Make(5));
        Assert.NotEqual(new Signature([], "Ada"), new Signature([], "Byron"));
    }

    [Fact]
    public void IsEmpty_ShouldBeTrue_WhenNothingIsDrawnOrTyped()
    {
        Assert.True(Signature.Empty.IsEmpty);
        Assert.True(new Signature([], "  ").IsEmpty);
        Assert.False(new Signature([], "Ada").IsEmpty);
    }
}
