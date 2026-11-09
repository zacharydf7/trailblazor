# Segmented control

Pattern: [Radio group](https://www.w3.org/WAI/ARIA/apg/patterns/radio/), a `TbRadioGroup` with `Segmented`

Familiar apps: the unit switch (°C or °F) in a weather app's settings, and the size choice on a shop's product page

## Keyboard

- [x] The control is one tab stop, landing on the chosen segment, since its segments are native radios that share a name. `TbRadioGroupTests.Render_ShouldBeALabeledRadioGroupWhoseRadiosShareAName_WhenCalled`, `SegmentedControlTests.SegmentedControl_ShouldRingTheFocusedSegment_WhenItHasKeyboardFocus`
- [x] The arrow keys choose the next or previous segment, and focus moves with them. `SegmentedControlTests.SegmentedControl_ShouldMoveTheChoice_WhenAnArrowKeyIsPressed`
- [x] The focused segment shows a focus ring around it, since its radio is hidden. `SegmentedControlTests.SegmentedControl_ShouldRingTheFocusedSegment_WhenItHasKeyboardFocus`
- [ ] In Safari, arrows wrap at the ends of the control too. UNCONFIRMED
- [ ] In right-to-left text, Left moves to the next segment and Right to the previous one. UNCONFIRMED

## Pointer

- [x] Clicking anywhere on a segment chooses it. `SegmentedControlTests.SegmentedControl_ShouldChooseASegment_WhenItIsClicked`
- [x] The chosen segment is filled and its radio's circle is hidden. `SegmentedControlTests.SegmentedControl_ShouldFillTheChosenSegmentAndHideTheRadios_WhenRendered`
- [x] Pointing at a segment shades it, unless it is chosen or can't be chosen. `SegmentedControlTests.SegmentedControl_ShouldShadeOnlySegmentsItCanChoose_WhenHovered`
- [x] A read-only control can't be changed, and only its chosen segment stays focusable. `TbRadioGroupTests.Render_ShouldTurnOffEveryRadioButTheCheckedOne_WhenReadOnly`
- [x] In forced-colors mode, the chosen segment takes the system's highlight. `SegmentedControlTests.SegmentedControl_ShouldHighlightTheChosenSegment_WhenColorsAreForced`

## Touch

- [x] Tapping a segment does what clicking does, since its radio covers the whole segment. `trailblazor.css`

## Focus

- [x] Screen readers hear a group named by its label, and each segment as a radio named by its text. `RadioGroupTests.RadioGroup_ShouldBeNamedByItsLegend_WhenLabelIsSet`
- [x] A static form posts the chosen segment's value. `RadioGroupTests.RadioGroup_ShouldPostItsValue_WhenChosenInAStaticForm`

## Not applicable

- Choosing several segments at once: use a toggle group, whose items each stay pressed.
- A sliding thumb that animates between segments: the fill jumps straight to the chosen segment, with no animation.
