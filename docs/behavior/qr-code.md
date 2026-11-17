# QR code and barcode

Pattern: none; each code is an image (`role="img"`)

Familiar apps: a boarding pass or event ticket in a phone's wallet, the "scan to open" code on a sign or a login page

## Keyboard

Nothing of its own: a code is an image, with nothing to focus or press.

## Pointer

Nothing of its own. To make a code clickable, put it inside a link or button.

## Touch

Nothing of its own: it is an image.

## Focus

- [x] It takes no focus, and screen readers read it as an image named by its value, or by `AriaLabel`. `TbQrCodeTests.Render_ShouldDrawAnImageNamedByItsValueWithAQuietZone_WhenGivenAValue`, `TbQrCodeTests.Render_ShouldNameTheImageWithTheAriaLabel_WhenOneIsGiven`
- [x] A barcode's printed value is hidden from screen readers, so it isn't read twice. `TbBarcodeTests.Render_ShouldDrawAnImageNamedByItsValueAndPrintTheValueHidden_WhenGivenAValue`
- [x] Codes stay dark on light in dark mode and in forced colors, so a scanner can still read them. `QrCodeTests.Codes_ShouldStayDarkOnLight_WhenTheThemeIsDark`, `QrCodeTests.Codes_ShouldStayDarkOnLight_WhenColorsAreForced`
- [x] A code wider than its container shrinks to fit rather than widening the page. `DocsSiteTests.ComponentPage_ShouldFitThePageWidth_WhenTheScreenIsAPhone`
- [x] A barcode's printed value wraps in a container narrower than the text. `trailblazor.css`
- [x] The symbols match independent encoders, mask choice included. `BarcodeEncoderTests.QrEncode_ShouldMatchAReferenceEncoder_WhenGivenLongerText`, `BarcodeEncoderTests.Code128Encode_ShouldMatchAReferenceEncoder_WhenGivenText`, `BarcodeEncoderTests.Ean13Encode_ShouldMatchAReferenceEncoder_WhenGivenDigits`

## Not applicable

- Scanning: reading a code with the camera is a different control.
- Kanji mode, mixed modes and ECI for a QR code: a value is encoded in a single mode, the most compact of numeric, alphanumeric and UTF-8 byte, which every scanner reads.
