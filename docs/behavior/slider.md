# Slider

Pattern: [Slider](https://www.w3.org/WAI/ARIA/apg/patterns/slider/), [Multi-thumb slider](https://www.w3.org/WAI/ARIA/apg/patterns/slider-multithumb/)

Familiar apps: the volume slider in an OS or video player, the price range filter on shopping sites

## Keyboard

- [x] Right raises the value by `Step`. `SliderTests.Slider_ShouldStepJumpAndReachTheEnds_WhenTheKeysAreUsed`
- [x] Up raises the value, and Left and Down lower it, since each thumb is a native range input. `TbSlider.razor`
- [x] Page Up and Page Down move by a larger amount, which the browser picks. `TbSlider.razor`
- [x] Home and End go to `Min` and `Max`. `SliderTests.Slider_ShouldStepJumpAndReachTheEnds_WhenTheKeysAreUsed`
- [x] `ValueCommitted` is raised once a key has moved the thumb. `SliderTests.Slider_ShouldStepJumpAndReachTheEnds_WhenTheKeysAreUsed`
- [x] On a range slider, Tab moves between the thumbs, and each stops at the other. `SliderTests.RangeSlider_ShouldMoveEachThumbAndStopItAtTheOther_WhenUsedWithTheKeys`
- [x] A read-only slider's keys change nothing, on either kind. `ReadOnlyTests.ReadOnlySlider_ShouldKeepItsValue_WhenDraggedOrMovedWithTheKeys`, `ReadOnlyTests.ReadOnlySwitchAndRangeSlider_ShouldKeepTheirValues_WhenTheUserTriesToChangeThem`
- [ ] In right-to-left text, Left raises the value and Right lowers it. #287

## Pointer

- [x] Clicking the track moves the thumb and fill there. `SliderTests.Slider_ShouldMoveItsThumbAndFillToThePointer_WhenTheTrackIsClicked`
- [x] Dragging updates the value as the thumb moves and commits on release. `SliderExampleTests.SliderExample_ShouldFollowTheThumbAndSaveOnlyOnCommit_WhenItMoves`
- [x] Clicking the label focuses the thumb without changing the value. `TbSliderTests.Render_ShouldBeARangeInputNamedByItsLabel_WhenCalled`, `TbSliderBase.razor`
- [x] Each thumb of a range slider can be dragged. `SliderDragTests.RangeSlider_ShouldLetEachThumbBeDragged_WhenTheyShareATrack`
- [x] A thumb dragged past the other stops at it. `TbSliderTests.Input_ShouldStopAThumbAtTheOther_WhenItIsDraggedPastIt`
- [x] With both thumbs at the top, the start thumb can be pulled down. `TbSliderTests.Render_ShouldLiftTheStartThumb_WhenBothThumbsAreAtTheTop`
- [ ] Clicking a range slider's track moves the nearer thumb there. #316
- [ ] With both thumbs on one value below the top, dragging down moves a thumb. #316
- [x] Dragging a read-only slider changes nothing, on either kind. `ReadOnlyTests.ReadOnlySlider_ShouldKeepItsValue_WhenDraggedOrMovedWithTheKeys`, `ReadOnlyTests.ReadOnlyRangeSlider_ShouldKeepItsThumb_WhenDragged`

## Touch

- [x] Each thumb is a native range input, so touch is the browser's own and the slider adds no touch handling. `TbSlider.razor`
- [ ] Tapping a single slider's track moves the thumb there on phones. UNCONFIRMED
- [ ] A range slider's thumbs can be dragged on phones, though they're the only parts that take the pointer and just 18px across. UNCONFIRMED

## Focus

- [x] The thumb shows a focus ring for keyboard focus. `trailblazor.css`
- [x] A read-only slider stays focusable. `ReadOnlyTests.ReadOnlySlider_ShouldKeepItsValue_WhenDraggedOrMovedWithTheKeys`
- [x] A disabled slider can't take focus. `SliderExampleTests.StatesExample_ShouldMarkOneReadOnlySoItPostsAndDisableTheOther_WhenRendered`
- [x] `FocusAsync` moves focus to the thumb, or to a range slider's start thumb. `FieldFocusTests.FocusAsync_ShouldMoveFocusToTheFieldsInput_WhenCalled`
- [x] Moving between a range slider's thumbs doesn't raise `OnFocus` or `OnBlur`. `TbFieldBase.cs`

## Not applicable

- A vertical slider: there is no orientation setting, and a slider here always runs along the reading direction.
- The mouse wheel: the pattern doesn't ask for it, and it would catch page scrolling.
- Clicking a mark or its label: marks are decoration, hidden from screen readers.
