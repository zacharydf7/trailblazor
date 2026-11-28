# Validation summary

Pattern: none; a region with a heading and a list of links, like the error summaries on government and banking forms

Familiar apps: a sign-up form that shows "There is a problem" at its top after a failed submit, with each problem a link to its field

## Keyboard

Nothing of its own: its links are native links in the tab order, and Enter on one follows it.

## Pointer

- [x] Pressing a field's message moves focus to the field, and a group's link points at the group. `ValidationSummaryScenarioTests.SignUp_ShouldListWhatToFixAndLinkToIt_WhenSubmittedEmpty`, `TbValidationSummaryTests.Link_ShouldPointAtTheGroup_WhenTheFieldIsARadioGroup`

## Touch

Nothing of its own: a tap follows a link, as any link.

## Focus

- [x] It's a region named by a real heading, an `h2` unless `HeadingLevel` says otherwise. `TbValidationSummaryTests.TbValidationSummary_ShouldListEachFieldsMessageAsALinkToIt_WhenTheFormFailsToSubmit`, `TbValidationSummaryTests.HeadingLevel_ShouldSetTheHeadingsElement_WhenGiven`

## Rendering

- [x] It shows only while the form has messages, each field's in field order then the form's own, and drops a message once its field is fixed. `TbValidationSummaryTests.TbValidationSummary_ShouldListEachFieldsMessageAsALinkToIt_WhenTheFormFailsToSubmit`, `TbValidationSummaryTests.TbValidationSummary_ShouldDropAMessage_WhenItsFieldIsFixed`, `TbValidationSummaryTests.TbValidationSummary_ShouldListTheFormsOwnMessagesWithoutALink_WhenTheyBelongToNoField`
- [x] A form message with the same text as a field's still shows. `TbValidationSummaryTests.TbValidationSummary_ShouldKeepAFormMessage_WhenAFieldShowsTheSameText`
- [x] On a static page it lists the messages the page renders with. `TbValidationSummaryTests.Render_ShouldListTheMessages_WhenThePageIsStatic`
- [x] Outside a form it throws. `TbValidationSummaryTests.Render_ShouldThrow_WhenOutsideAForm`
- [x] In forced colors, the summary keeps its border. `ForcedColorsTests.Example_ShouldKeepEachPartsBorders_WhenColorsAreForced`

## Not applicable

- Announcements: a failed submit moves focus to the first field with a message, so a screen reader reads it.
- Taking focus on a failed submit: the form focuses the first field instead; build your own summary for that.
- Reduced motion: nothing moves.
