# Banner

Pattern: none; it is a named region holding a message, with ordinary buttons and links

Familiar apps: GitHub's site-wide notices, a cloud console's "planned maintenance" strip, an app's "You're offline" bar

## Keyboard

- [x] The close button is a native button, so Enter on it dismisses the banner; the actions are the app's own buttons and links. `BannerTests.Banner_ShouldClose_WhenItsCloseButtonIsActivatedByKeyboard`

## Pointer

- [x] Clicking the close button raises `OnClose`; the page hides the banner in its handler. `TbBannerTests.Close_ShouldRaiseOnClose_WhenItsButtonIsClicked`
- [x] The icon, message, actions and close button share one row, centered while they fit, and a message too long for it wraps beside the icon. In a banner narrower than 30rem the actions go under the message, and the icon and close button keep to its first line. `BannerTests.Banner_ShouldWrapItsMessageBesideTheIcon_WhenThePartsDontFitOneRow`

## Touch

Nothing of its own: tapping its buttons and links does what clicking does.

## Focus

- [x] Screen readers find it as a region named by its title, or by `AriaLabel`, or "Notice". `TbBannerTests.Render_ShouldBeARegionNamedByItsTitle_WhenGivenATitle`, `TbBannerTests.Render_ShouldNameTheRegion_WhenItHasNoTitle`
- [x] The close button is named "Dismiss", or by `CloseLabel`. `TbBannerTests.Close_ShouldRaiseOnClose_WhenItsButtonIsClicked`
- [x] In forced colors, the banner keeps the border under it, which sets it off from the page once its tint is dropped. `ForcedColorsTests.Example_ShouldKeepEachPartsBorders_WhenColorsAreForced`

## Not applicable

- Announcing itself as it appears: a banner is often on the page from the start, so announcing a new one is the app's job, with the announcer.
- Moving focus after it is dismissed: the page decides where focus belongs once the banner is gone.
- Closing with Escape: a banner isn't a popup, and Escape belongs to whatever has focus.
