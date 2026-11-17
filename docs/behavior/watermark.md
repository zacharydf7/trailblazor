# Watermark

Pattern: none; decorative marks over content, with their meaning in text

Familiar apps: the "Draft" and "Confidential" marks on Google Docs and Word printouts

## Keyboard

- [x] Nothing about the marks takes focus; the content's controls keep their own tab order. `WatermarkTests.Watermark_ShouldLetClicksAndFocusThrough_WhenOverContent`

## Pointer

- [x] Clicks pass through the marks to the content. `WatermarkTests.Watermark_ShouldLetClicksAndFocusThrough_WhenOverContent`
- [x] The marks cover the whole content and show as a tiled mask. `WatermarkTests.Watermark_ShouldCoverTheContentWithTiledMarks_WhenGivenText`

## Touch

- [x] Taps pass through the marks, and nothing needs hover. `TbWatermark.razor`

## Focus

- [x] Screen readers skip the marks and hear what they say once, before the content. `TbWatermarkTests.Render_ShouldTileTheTextOverTheContentAndSayItOnce_WhenGivenText`
- [x] Decorative marks say nothing. `TbWatermarkTests.Render_ShouldSayNothing_WhenDecorative`
- [x] Text in the marks is escaped in their image. `TbWatermarkTests.Render_ShouldEscapeTheTextInTheTile_WhenItHasMarkup`
- [x] In forced colors, text marks use the system's gray. `WatermarkTests.Watermark_ShouldUseTheSystemGray_WhenColorsAreForced`
