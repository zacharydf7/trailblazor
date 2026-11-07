# Color picker

Pattern: none for the whole control; its parts follow [Radio group](https://www.w3.org/WAI/ARIA/apg/patterns/radio/) and [Slider](https://www.w3.org/WAI/ARIA/apg/patterns/slider/)

Familiar apps: a browser's color input, the color pickers in design tools and browser developer tools

## Keyboard

- [x] Enter in the text box takes a typed color and writes it in full. `ColorPickerTests.Typing_ShouldTakeAShortHexAsTheFullColor_WhenEnterIsPressed`
- [x] Text that isn't a color stays in the box with a message, and the value stays as it was. `ColorPickerTests.Typing_ShouldExplainAndKeepTheText_WhenItIsntAColor`
- [x] Enter or Space on the picker button opens the picker. `PopoverTests.Popover_ShouldTakeFocusAndGiveItBack_WhenOpenedAndClosedWithTheKeyboard`
- [x] The arrows move between swatches and choose one, as native radios do. `ColorPickerTests.Swatches_ShouldBeRadiosChosenWithTheArrows_WhenThePickerIsOpen`
- [x] The arrows, Home, End, Page Up and Page Down change a slider, since they're native. `ColorPickerTests.Sliders_ShouldChangeTheColorAndKeepTheHue_WhenTheBrightnessGoesToBlack`
- [x] Up and Down step the Red, Green and Blue boxes, since they're native number inputs. `TbColorPicker.razor`
- [x] Escape closes the picker and returns focus to its button. `PopoverTests.Popover_ShouldTakeFocusAndGiveItBack_WhenOpenedAndClosedWithTheKeyboard`
- [x] Tabbing out of the picker closes it. `PopoverTests.Popover_ShouldClose_WhenTabMovesFocusOutOfThePanel`

## Pointer

- [x] Clicking around the text box, such as on the field's padding, focuses it, unless it lands on a button. `FieldBoxTests.Box_ShouldFocusItsInput_WhenItsPaddingIsPressed`
- [x] Clicking the pad sets saturation and brightness, and the sliders follow. `ColorPickerPadTests.Pad_ShouldSetSaturationAndBrightness_WhenClicked`
- [x] Dragging keeps following the pointer outside the pad, since it captures the pointer. `color-picker.js`
- [x] Right-clicking the pad leaves the color alone. `ColorPickerPadTests.Pad_ShouldLeaveTheColorAlone_WhenRightClickedAfterTheColorWasTyped`
- [x] Clicking a swatch chooses it. `TbColorPickerTests.Swatch_ShouldChooseItsColor_WhenChanged`
- [x] The opacity slider shows and sets the alpha. `ColorPickerTests.Opacity_ShouldShowAndSetTheAlpha_WhenAlphaIsOn`
- [x] The eyedropper takes a color from the screen, and only shows where the browser has one. `ColorPickerTests.Eyedropper_ShouldTakeTheColorFromTheScreen_WhenTheBrowserHasOne`
- [x] Canceling the eyedropper keeps the color. `browser.js`
- [x] Clicking elsewhere closes the picker. `PopoverTests.Popover_ShouldCloseFromCodeAndOnAClickElsewhere_WhenItsOpenIsBound`

## Touch

- [ ] A tap on the field's padding focuses the text box. UNCONFIRMED
- [x] Dragging on the pad works by pointer events, and `touch-action: none` keeps the page from scrolling instead. `trailblazor.css`
- [x] A pointer canceled mid-drag ends the drag and keeps the last color. `color-picker.js`

## Focus

- [ ] Opening puts focus on the chosen swatch, as Tab would, so the arrows move from it. #299
- [x] Opening with no palette puts focus on the first control. `popover.js`
- [x] Clearing moves focus to the text box. `ColorPickerTests.Clear_ShouldEmptyTheColorAndFocusTheBox_WhenClicked`
- [x] A read-only field turns off the picker's controls and its buttons. `TbColorPickerTests.Render_ShouldTurnOffEveryControlInThePanel_WhenReadOnly`

## Not applicable

- Keys on the pad: it's for the pointer and hidden from screen readers, and the sliders give the keyboard the same control.
- Clicking the preview beside the box to open the picker: it's decorative, and the picker button opens it.
- Choosing a swatch closing the picker: it stays open, so someone can compare colors.
