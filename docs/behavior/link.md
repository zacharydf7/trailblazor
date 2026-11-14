# Link

Pattern: [Link](https://www.w3.org/WAI/ARIA/apg/patterns/link/)

Familiar apps: any link in a browser

## Keyboard

- [x] Enter on a link follows it, since it is a real anchor. `TbLink.razor`
- [x] Enter on a link with no address runs its action. `LinkTests.Link_ShouldRunItsAction_WhenItHasNoAddressAndEnterIsPressed`
- [x] Space on a link with no address runs its action too, since it is a real button. `TbLink.razor`

## Pointer

- [x] Clicking a link goes to its address. `TbLinkTests.Render_ShouldRenderAnAnchorToTheAddress_WhenHrefIsSet`
- [x] Ctrl, Cmd, Shift, middle and right clicks do what they do on any link, since nothing cancels them. `TbLink.razor`
- [x] With `ForceLoad`, a link loads a whole new page, from a static or interactive page alike, while modified clicks and links that open elsewhere still go to the browser. `LinkTests.Link_ShouldLoadAWholeNewPageOnlyWithForceLoad_WhenFollowed`, `LinkTests.Link_ShouldStillOpenANewTab_WhenItHasForceLoadAndNewTab`, `TbLinkTests.Render_ShouldMarkTheAnchorForAWholePageLoadOnlyWithForceLoad_WhenGivenForceLoad`
- [x] Clicking a link with no address runs its action. `TbLinkTests.Click_ShouldRunOnClickFromAButton_WhenHrefIsMissing`
- [x] A link with no address doesn't submit a form around it. `TbLink.razor`
- [x] `NewTab` opens the address in a new tab and tells screen readers so. `TbLinkTests.Render_ShouldOpenANewTabWithoutTheOpenerAndSaySo_WhenNewTabIsSet`
- [x] A disabled link goes nowhere and runs nothing. `LinkTests.Link_ShouldBeAnnouncedDisabledAndGoNowhere_WhenDisabled`, `TbLinkTests.Click_ShouldNotRunOnClick_WhenDisabled`
- [x] Hovering thickens the underline, and a disabled link has neither the underline nor the hand cursor. `trailblazor.css`

## Touch

- [x] Tapping does what clicking does, and nothing depends on hover. `TbLink.razor`

## Focus

- [x] A focused link keeps its underline and shows a focus ring. `LinkTests.Link_ShouldStayUnderlinedAndShowAFocusRing_WhenFocused`
- [x] A disabled link is out of the tab order, with or without an address. `TbLinkTests.Render_ShouldDropTheAddressAndSayItIsDisabled_WhenADisabledLinkHasAnHref`, `TbLinkTests.Render_ShouldDisableTheButton_WhenADisabledLinkHasNoHref`
- [x] `FocusAsync` moves focus to the link. `LinkTests.FocusAsync_ShouldFocusTheLink_WhenCalledFromCode`

## Not applicable

- Staying focusable while disabled: a disabled link is plain text, unlike a button with `FocusableWhenDisabled`.
