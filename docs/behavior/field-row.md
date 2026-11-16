# Field row

Pattern: none; it lays out other controls

Familiar apps: none; it isn't a control

## Keyboard

Nothing of its own. Tab moves through the fields and buttons in the order of the markup.

## Pointer

Nothing of its own.

## Touch

Nothing of its own.

## Focus

- [x] It takes no focus and has nothing to read. `TbFieldRow.cs`

## Layout

- [x] Every field's box sits on one line, whether or not the fields show helper text or errors. `LayoutTests.FieldRow_ShouldPutEveryBoxOnOneLineAndCenterTheButtons_WhenFieldsShowDifferentMessages`
- [x] A button beside the fields is centered on the boxes' line. `LayoutTests.FieldRow_ShouldPutEveryBoxOnOneLineAndCenterTheButtons_WhenFieldsShowDifferentMessages`
- [x] A taller box, such as a field with several lines, keeps the other boxes at their own height. `LayoutTests.FieldRow_ShouldKeepEachBoxItsOwnHeightAndPutABoxlessFieldOnTheirLine_WhenOneBoxIsTaller`
- [x] A field with no box, such as a file upload, sits on the boxes' line like a button. `LayoutTests.FieldRow_ShouldKeepEachBoxItsOwnHeightAndPutABoxlessFieldOnTheirLine_WhenOneBoxIsTaller`
- [x] Labels share the line above the boxes, so a field with no visible label or a label on two lines still lines up with the others. `LayoutTests.FieldRow_ShouldPutEveryBoxOnOneLineAndCenterTheButtons_WhenFieldsShowDifferentMessages`

## Not applicable

- Everything else: all it does is line up what it holds.
