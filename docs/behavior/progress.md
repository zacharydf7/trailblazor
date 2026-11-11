# Progress

Pattern: none in the Authoring Practices; it uses the [progressbar role](https://www.w3.org/TR/wai-aria-1.2/#progressbar)

Familiar apps: a browser's download bar, File Explorer's copy dialog

## Keyboard

Nothing of its own. It only shows how far along work is, so it has no keys.

## Pointer

Nothing of its own. A slider is for setting a value.

## Touch

Nothing of its own, for the same reason.

## Focus

- [x] It is never a tab stop. `TbProgress.razor`
- [x] Screen readers find it by its name, from `Label` or `AriaLabel`. `TbProgressTests.Render_ShouldBeNamedByItsLabel_WhenItHasNoAriaLabel`
- [x] They read its value, or the formatted text in its place. `TbProgressTests.Render_ShouldShowAndAnnounceTheFormattedValue_WhenGivenAValueFormatter`
- [x] Work with no known end has no value to read. `TbProgressTests.Render_ShouldBeAnIndeterminateLinearBar_WhenNoValueIsSet`
- [x] Zero total work, with `Min` equal to `Max`, shows and reads as finished. `TbProgressTests.Render_ShouldShowTheWorkAsFinished_WhenMinEqualsMax`

## Not applicable

- Saying each new value aloud: it would interrupt everything else, so screen readers read the value when they reach the bar, and the app announces when work ends.
