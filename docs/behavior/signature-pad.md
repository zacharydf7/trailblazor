# Signature pad

Pattern: none in the APG; a group holding a drawing area, which is an image with a label, and buttons, with a typed name as the way to sign without a pointer

Familiar apps: the signature box of a parcel delivery app, and of an e-signing service

## Keyboard

- [x] Tab reaches Undo, Clear and Type your name, and Type your name is a toggle that switches the pad to a field for the name. `SignaturePadTests.TypeName_ShouldSignFromTheKeyboard_WhenTheToggleIsPressed`, `TbSignaturePadTests.TypeNameAsync_ShouldSignWithTheTypedName_WhenTheNameIsTyped`
- [x] Switching between drawing and typing starts the signature over. `TbSignaturePadTests.TypeName_ShouldStartTheSignatureOver_WhenTheToggleSwitchesBackToDrawing`

## Pointer

- [x] Pressing on the pad and moving draws a stroke, kept in the pad's own space, whatever size it's shown at. `SignaturePadTests.Draw_ShouldKeepPointsInTheSignaturesSpace_WhenThePadIsShownSmaller`, `SignaturePadTests.Draw_ShouldAddAStrokeAndUndoTakesItBack_WhenDrawnWithTheMouse`, `TbSignaturePadTests.DrawAsync_ShouldAddTheStrokeToTheValue_WhenAStrokeIsDrawn`
- [x] A stroke that runs off the pad stays inside it. `TbSignaturePadTests.DrawAsync_ShouldKeepThePointsInsideThePad_WhenAStrokeRunsOffIt`
- [x] Undo takes back the last stroke, and Clear empties the pad. `TbSignaturePadTests.UndoAsync_ShouldTakeBackTheLastStroke_WhenThereAreSeveral`, `TbSignaturePadTests.ClearAsync_ShouldEmptyThePad_WhenSomethingIsDrawn`
- [x] A disabled or read-only pad can't be drawn on or changed. `TbSignaturePadTests.DrawAsync_ShouldChangeNothing_WhenThePadIsDisabledOrReadOnly`

## Touch

- [x] A press on the pad draws rather than scrolling the page. `trailblazor.css`

## Focus

- [x] Screen readers hear the pad's label, and whether anything is drawn. `TbSignaturePadTests.Pad_ShouldSayNothingIsDrawn_WhenItStartsEmpty`, `TbSignaturePadTests.DrawAsync_ShouldAddTheStrokeToTheValue_WhenAStrokeIsDrawn`

## Output

- [x] `ToSvg` draws the strokes as round-ended lines, or the typed name as text, and `ToPngAsync` has the browser make a PNG. `TbSignaturePadTests.ToSvg_ShouldDrawEachStrokeAsARoundEndedPath_WhenCalled`, `TbSignaturePadTests.ToSvg_ShouldWriteTheTypedNameAsText_WhenTheNameIsTyped`, `SignaturePadTests.ToPngAsync_ShouldMakeAPng_WhenSigned`
