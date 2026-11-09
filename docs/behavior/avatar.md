# Avatar

Pattern: none; it's an image with no behavior of its own

Familiar apps: the profile pictures in a chat app, and the row of faces on a shared document

## Keyboard

Nothing of its own. An avatar takes no focus; one inside a button or a menu's trigger leaves the keys to it.

## Pointer

- [x] A picture that fails to load is taken away, and the initials show in its place. `AvatarTests.Avatar_ShouldShowItsPictureOrItsInitials_WhenThePictureLoadsOrFails`, `TbAvatarTests.Render_ShouldDropThePicture_WhenItFailsToLoad`
- [x] Before the page is interactive, an avatar whose picture fails looks just like one with no picture. `AvatarTests.Avatar_ShouldLookAsIfItHadNoPicture_WhenItsPictureFailsBeforeThePageIsInteractive`
- [x] Once a picture loads, the initials go, so none show through its transparent parts. `TbAvatarTests.Render_ShouldDropTheInitials_WhenThePictureLoads`
- [x] In forced-colors mode, each avatar keeps an outline. `AvatarTests.Avatar_ShouldKeepAnOutline_WhenColorsAreForced`

## Touch

Nothing of its own.

## Focus

- [x] An avatar is never a tab stop. `TbAvatar.razor`
- [x] Screen readers hear an avatar once, by its name, and skip a decorative one. `TbAvatarTests.Render_ShouldNameTheImageByTheName_WhenGivenAName`, `TbAvatarTests.Render_ShouldHideTheAvatarFromScreenReaders_WhenDecorative`
- [x] A group is named, and its count of the rest is heard as "3 more". `TbAvatarTests.Group_ShouldShowAndNameTheCountOfTheRest_WhenMoreIsSet`

## Not applicable

- Choosing a color from the name: the app picks `Tone` from something that stays the same for each person.
- Working out which avatars fit in a group: the app renders the ones it wants shown and sets `More`.
