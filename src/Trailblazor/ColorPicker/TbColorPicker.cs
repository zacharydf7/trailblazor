using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>Chooses a color: typed as hex, picked from swatches or a custom picker, or taken from the screen with the
/// browser's eyedropper. Bind it with <c>@bind-Value</c> to a hex string such as <c>#336699</c>.</summary>
public partial class TbColorPicker : IAsyncDisposable
{
    private ElementReference _input;
    private ElementReference _pad;
    private IJSObjectReference? _handle;
    private DotNetObjectReference<ColorPickerEvents>? _events;
    private string? _shownValue;
    private double _hue;
    private double _saturation;
    private double _brightness;
    private byte _alpha = 255;
    private bool _canPickColor;
    private bool _disposed;

    /// <summary>Colors offered as swatches, such as the brand's or the document's, as <c>#rrggbb</c>, <c>#rgb</c> or
    /// <c>rgb()</c>.</summary>
    [Parameter]
    public IReadOnlyList<string>? Palette { get; set; }

    /// <summary>Names a swatch for screen readers, such as "Ocean blue". Defaults to the color's hex.</summary>
    [Parameter]
    public Func<string, string>? SwatchName { get; set; }

    /// <summary>Offers only the swatches, without the custom picker.</summary>
    [Parameter]
    public bool PaletteOnly { get; set; }

    /// <summary>Adds an opacity slider and allows eight-digit hex, such as <c>#33669980</c>. A color that isn't fully
    /// opaque is written with its opacity.</summary>
    [Parameter]
    public bool Alpha { get; set; }

    /// <summary>Shows a button that takes a color from the screen, in browsers that have an eyedropper. Defaults to
    /// true.</summary>
    [Parameter]
    public bool Eyedropper { get; set; } = true;

    /// <summary>Shows a button that clears the color.</summary>
    [Parameter]
    public bool Clearable { get; set; }

    /// <summary>Text shown while there is no color. It is not a label.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>The words the picker shows and says to screen readers.</summary>
    [Parameter]
    public ColorPickerLabels Labels { get; set; } = new();

    /// <summary>Extra CSS classes for the field's parts.</summary>
    [Parameter]
    public FieldClasses? Classes { get; set; }

    [Inject]
    private IBrowserService Browser { get; set; } = default!;

    [Inject]
    private JsModuleLoader Modules { get; set; } = default!;

    private ColorValue? Color => ColorValue.TryParse(Value, Alpha, out var color) ? color : null;

    private ColorValue Picked => ColorValue.FromHsv(_hue, _saturation, _brightness, _alpha);

    /// <inheritdoc />
    public override ValueTask FocusAsync(bool preventScroll = false) => _input.FocusAsync(preventScroll);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Palette?.FirstOrDefault(swatch => !ColorValue.TryParse(swatch, Alpha, out _)) is { } unreadable)
        {
            throw new InvalidOperationException(
                $"{nameof(TbColorPicker)} can't read the palette color {unreadable}; give it as #rrggbb.");
        }

        if (Value != _shownValue)
        {
            _shownValue = Value;
            ShowColor(Color);
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || !RendererInfo.IsInteractive)
        {
            return;
        }

        try
        {
            if (!PaletteOnly)
            {
                await AttachPadAsync();
            }

            if (Eyedropper && await Browser.CanPickColorAsync())
            {
                _canPickColor = true;
                StateHasChanged();
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the picker went with it.
        }
    }

    private async Task AttachPadAsync()
    {
        var module = await Modules.ImportAsync("./_content/Trailblazor/color-picker.js");
        if (_disposed)
        {
            return;
        }

        _events = DotNetObjectReference.Create(new ColorPickerEvents(OnPadAsync));
        var handle = await module.InvokeAsync<IJSObjectReference?>("createColorPad", _pad, _events);
        if (_disposed && handle is not null)
        {
            await handle.DisposeHandleAsync();
            return;
        }

        _handle = handle;
    }

    // The sliders keep their hue while the color is gray or black, where the hex alone can't say what it was.
    private void ShowColor(ColorValue? color)
    {
        if (color is not { } shown)
        {
            return;
        }

        var (hue, saturation, brightness) = shown.ToHsv();
        _hue = saturation == 0 || brightness == 0 ? _hue : hue;
        _saturation = brightness == 0 ? _saturation : saturation;
        _brightness = brightness;
        _alpha = shown.A;
    }

    private Task SetColorAsync(ColorValue color)
    {
        var hex = color.ToHex(Alpha);
        _shownValue = hex;
        return SetValueAsync(hex);
    }

    // The pad's script isn't a Blazor event, so the field renders itself once the new value and its messages are set.
    private async Task OnPadAsync(double saturation, double brightness)
    {
        _saturation = saturation;
        _brightness = brightness;
        await SetColorAsync(Picked);
        StateHasChanged();
    }

    private Task SetHueAsync(string? text) => SetFromSliderAsync(text, value => _hue = value);

    private Task SetSaturationAsync(string? text) => SetFromSliderAsync(text, value => _saturation = value / 100);

    private Task SetBrightnessAsync(string? text) => SetFromSliderAsync(text, value => _brightness = value / 100);

    private Task SetAlphaAsync(string? text) =>
        SetFromSliderAsync(text, value => _alpha = (byte)Math.Round(value / 100 * 255));

    private Task SetFromSliderAsync(string? text, Action<double> set)
    {
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return Task.CompletedTask;
        }

        set(value);
        return SetColorAsync(Picked);
    }

    private Task SetChannelAsync(string? text, Func<ColorValue, byte, ColorValue> with)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return Task.CompletedTask;
        }

        var color = with(Color ?? Picked, (byte)Math.Clamp(value, 0, 255));
        ShowColor(color);
        return SetColorAsync(color);
    }

    private Task ChooseSwatchAsync(string swatch) => ChooseAsync(Read(swatch));

    private Task ChooseAsync(ColorValue color)
    {
        ShowColor(color);
        return SetColorAsync(color);
    }

    private async Task PickFromScreenAsync()
    {
        if (await Browser.PickColorAsync() is { } picked && ColorValue.TryParse(picked, Alpha, out var color))
        {
            await ChooseAsync(color);
        }
    }

    // The Clear button goes away with the color, so focus moves to the box rather than dropping to the page.
    private async Task ClearAsync()
    {
        _shownValue = null;
        await SetValueAsync(null);
        if (!_disposed)
        {
            await _input.FocusAsync();
        }
    }

    private bool IsChosen(string swatch) => Color == Read(swatch);

    // Every palette color reads, since OnParametersSet checks them.
    private ColorValue Read(string swatch)
    {
        ColorValue.TryParse(swatch, Alpha, out var color);
        return color;
    }

    private string NameOf(string swatch) => SwatchName?.Invoke(swatch) ?? swatch;

    private static string Invariant(double value) => Math.Round(value).ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    private protected override string? FormatValue(string? value) =>
        ColorValue.TryParse(value, Alpha, out var color) ? color.ToHex(Alpha) : value;

    /// <inheritdoc />
    private protected override bool TryParse(string? text, out string? value, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            value = null;
            return true;
        }

        // The sliders follow a typed color at once, so they keep its hue rather than working it out again.
        if (ColorValue.TryParse(text, Alpha, out var color))
        {
            value = color.ToHex(Alpha);
            ShowColor(color);
            _shownValue = value;
            return true;
        }

        value = null;
        error = BadColorLabel;
        return false;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Blazor calls only DisposeAsync when both exist, so the field's own cleanup has to run from here.
        Dispose();
        _disposed = true;
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }

        _events?.Dispose();
    }

    private string OpenLabel => Labels.Open ?? Localizer[TbStrings.ColorPickerOpen];
    private string EyedropperLabel => Labels.Eyedropper ?? Localizer[TbStrings.ColorPickerEyedropper];
    private string ClearLabel => Labels.Clear ?? Localizer[TbStrings.ColorPickerClear];
    private string SwatchesLabel => Labels.Swatches ?? Localizer[TbStrings.ColorPickerSwatches];
    private string HueLabel => Labels.Hue ?? Localizer[TbStrings.ColorPickerHue];
    private string SaturationLabel => Labels.Saturation ?? Localizer[TbStrings.ColorPickerSaturation];
    private string BrightnessLabel => Labels.Brightness ?? Localizer[TbStrings.ColorPickerBrightness];
    private string AlphaLabel => Labels.Alpha ?? Localizer[TbStrings.ColorPickerAlpha];
    private string RedLabel => Labels.Red ?? Localizer[TbStrings.ColorPickerRed];
    private string GreenLabel => Labels.Green ?? Localizer[TbStrings.ColorPickerGreen];
    private string BlueLabel => Labels.Blue ?? Localizer[TbStrings.ColorPickerBlue];
    private string BadColorLabel => Labels.BadColor ?? Localizer[TbStrings.ColorPickerBadColor];
}
