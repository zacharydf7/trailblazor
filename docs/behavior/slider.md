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
- [x] In right-to-left text, Left raises the value and Right lowers it, in WebKit too. `SliderTests.Slider_ShouldRaiseTheValueWithTheLeftArrow_WhenTheTextIsRightToLeft`, `SliderTests.RangeSlider_ShouldRaiseAThumbWithTheLeftArrow_WhenTheTextIsRightToLeft`

## Pointer

- [x] Clicking the track moves the thumb and fill there. `SliderTests.Slider_ShouldMoveItsThumbAndFillToThePointer_WhenTheTrackIsClicked`
- [x] Dragging updates the value as the thumb moves and commits on release. `SliderExampleTests.SliderExample_ShouldFollowTheThumbAndSaveOnlyOnCommit_WhenItMoves`
- [x] Clicking the label focuses the thumb without changing the value. `TbSliderTests.Render_ShouldBeARangeInputNamedByItsLabel_WhenCalled`, `TbSliderBase.razor`
- [x] Each thumb of a range slider can be dragged. `SliderDragTests.RangeSlider_ShouldLetEachThumbBeDragged_WhenTheyShareATrack`
- [x] A thumb dragged past the other stops at it. `TbSliderTests.Input_ShouldStopAThumbAtTheOther_WhenItIsDraggedPastIt`
- [x] With both thumbs on one value, dragging down moves the start thumb and dragging up the end thumb, at the ends of the track too. A press that doesn't move changes nothing. `SliderDragTests.RangeSlider_ShouldPartItsThumbsTheWayThePointerGoes_WhenTheyShareAValue`, `SliderDragTests.RangeSlider_ShouldChangeNothing_WhenAPressOnSharedThumbsDoesntMove`
- [x] Clicking a range slider's track moves the nearer thumb there and focuses it, in right-to-left text too. Holding the press and moving the pointer drags that thumb. `SliderDragTests.RangeSlider_ShouldMoveTheNearerThumbToThePointer_WhenTheTrackIsClicked`, `SliderDragTests.RangeSlider_ShouldFollowThePointer_WhenATrackPressIsHeldAndDragged`, `SliderDragTests.RangeSlider_ShouldMoveTheNearerThumb_WhenTheTrackIsClickedInRightToLeftText`
- [x] Clicking the track away from thumbs on one value moves the one on that side. `SliderDragTests.RangeSlider_ShouldMoveTheThumbOnThatSide_WhenTheTrackIsClickedAwayFromSharedThumbs`
- [x] A drag from thumbs on one value can change direction while they still share it, so a wobble doesn't lock in the wrong thumb. `SliderDragTests.RangeSlider_ShouldLetADragChangeDirection_WhileItsThumbsShareAValue`
- [x] A drag from thumbs on different values that overlap on screen moves the one on the side it heads to, rather than stopping at the other, and a wobble to the other side puts back the thumb it picked. `SliderDragTests.RangeSlider_ShouldMoveTheThumbOnTheSideThePointerHeadsTo_WhenItsThumbsOverlap`, `SliderDragTests.RangeSlider_ShouldReturnTheThumbAWobblePicked_WhenADragFromOverlappingThumbsTurnsBack`
- [x] A thumb dragged into the other and back keeps moving. `SliderDragTests.RangeSlider_ShouldKeepDraggingAThumb_WhenItMeetsTheOtherAndComesBack`
- [x] A press on a read-only or disabled range slider's track changes nothing. `SliderDragTests.RangeSlider_ShouldChangeNothing_WhenItsTrackIsPressedWhileReadOnlyOrDisabled`
- [x] Dragging a read-only slider changes nothing, on either kind. `ReadOnlyTests.ReadOnlySlider_ShouldKeepItsValue_WhenDraggedOrMovedWithTheKeys`, `ReadOnlyTests.ReadOnlyRangeSlider_ShouldKeepItsThumb_WhenDragged`

## Touch

- [x] Touch on a thumb is the browser's own, since each thumb is a native range input, unless a press lands on both of a range slider's thumbs. `TbSlider.razor`, `range-slider.js`
- [x] On a range slider, a tap on the track moves the nearer thumb, and a finger sliding across drags it. A finger sliding from thumbs on one value separates them, and so does one sliding from thumbs that overlap, in Chromium. `SliderDragTests.RangeSlider_ShouldMoveTheNearerThumb_WhenTheTrackIsTapped`, `SliderDragTests.RangeSlider_ShouldFollowAFinger_WhenItSlidesAcrossTheTrack`, `SliderDragTests.RangeSlider_ShouldSeparateItsThumbs_WhenAFingerSlidesFromThem`, `SliderDragTests.RangeSlider_ShouldMoveTheThumbOnTheSideAFingerHeadsTo_WhenItsThumbsOverlap`
- [x] A finger scrolling the page from a range slider's track changes nothing and takes no focus, in Chromium. From thumbs that overlap, the page doesn't scroll, since their own touch handling is held back, but the swipe still changes nothing and takes no focus. `SliderDragTests.RangeSlider_ShouldStayPut_WhenAFingerStartsScrollingOnTheTrack`, `SliderDragTests.RangeSlider_ShouldStayPut_WhenAFingerStartsScrollingOnOverlappingThumbs`
- [ ] A finger scrolling the page from a range slider's track changes nothing on phones, which relies on the browser canceling the press. UNCONFIRMED
- [ ] Two fingers on one range slider can each drag a thumb. UNCONFIRMED
- [ ] A finger sliding from thumbs that overlap separates them on phones, which relies on the browser letting the page hold back a thumb's own touch handling. UNCONFIRMED
- [ ] Tapping a single slider's track moves the thumb there on phones. UNCONFIRMED
- [ ] A range slider's thumbs can be dragged on phones, though they're just 18px across. UNCONFIRMED

## Focus

- [x] The thumb shows a focus ring for keyboard focus. `trailblazor.css`
- [x] A click on a range slider's track focuses the thumb without the ring, and the ring appears once a key is pressed, or when focus comes back. `SliderDragTests.RangeSlider_ShouldHoldBackTheRingUntilAKey_WhenTheTrackIsClicked`, `SliderDragTests.RangeSlider_ShouldStopHoldingBackTheRing_WhenFocusLeavesTheThumb`
- [x] A read-only slider stays focusable. `ReadOnlyTests.ReadOnlySlider_ShouldKeepItsValue_WhenDraggedOrMovedWithTheKeys`
- [x] A disabled slider can't take focus. `SliderExampleTests.StatesExample_ShouldMarkOneReadOnlySoItPostsAndDisableTheOther_WhenRendered`
- [x] `FocusAsync` moves focus to the thumb, or to a range slider's start thumb. `FieldFocusTests.FocusAsync_ShouldMoveFocusToTheFieldsInput_WhenCalled`
- [x] Moving between a range slider's thumbs doesn't raise `OnFocus` or `OnBlur`. `TbFieldBase.cs`

## Not applicable

- A vertical slider: there is no orientation setting, and a slider here always runs along the reading direction.
- The mouse wheel: the pattern doesn't ask for it, and it would catch page scrolling.
- Clicking a mark or its label: marks are decoration, hidden from screen readers.
