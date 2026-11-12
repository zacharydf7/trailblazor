# App shell

Pattern: [Disclosure navigation menu](https://www.w3.org/WAI/ARIA/apg/patterns/disclosure/examples/disclosure-navigation/), in a page of banner, navigation and main landmarks

Familiar apps: the sidebar and top bar of a mail app, and of a cloud provider's console

## Keyboard

- [x] The skip link shows when it has focus, and Enter on it moves focus to the page's content, where Tab goes on. `AppShellTests.SkipLink_ShouldShowAndMoveFocusToTheContent_WhenItIsUsed`
- [x] Tab moves between the links and group buttons, each its own tab stop, since they are real links and buttons. `TbNavLink.razor`, `TbNavGroup.razor`
- [x] Enter or Space on a group's button opens or closes it, even a group holding the current page. `TbNavMenuTests.Group_ShouldOpenItsLinks_WhenItsButtonIsPressed`, `TbNavMenuTests.Group_ShouldClose_WhenItsButtonIsPressedWhileItHoldsTheCurrentPage`
- [x] Escape closes the navigation drawer and gives focus back to the menu button, as any drawer does. `DrawerTests.Drawer_ShouldCloseAndGiveFocusBack_WhenEscapeIsPressed`

## Pointer

- [x] Following a link marks it as the current page and unmarks the one before. `AppShellTests.AppShell_ShouldMarkTheLinkToThePageItIsOn_WhenALinkIsFollowed`, `TbNavMenuTests.Render_ShouldMoveTheCurrentMark_WhenTheAddressChanges`
- [x] A link matches the address as Blazor's `NavLink` does: under its address by default, or exactly with `NavLinkMatch.All`. `TbNavMenuTests.IsCurrent_ShouldMatchAsBlazorsNavLinkDoes_WhenGivenAnAddress`
- [x] A group holding the current page is open as the page arrives, before it is interactive. `AppShellTests.AppShell_ShouldOpenTheGroupHoldingThePage_WhenThePageLoads`, `TbNavMenuTests.Group_ShouldOpenByItself_WhenItHoldsTheCurrentPage`
- [x] On a wide window, the menu button narrows the navigation to a rail of icons and labels. `AppShellTests.MenuButton_ShouldNarrowTheNavigationToARail_WhenTheWindowIsWide`, `TbAppShellTests.CollapseButton_ShouldNarrowTheNavigationToARail_WhenPressed`
- [x] On a narrow window, the navigation leaves the page, the menu button opens it in a drawer, and following a link closes the drawer. `AppShellTests.MenuButton_ShouldOpenTheNavigationInADrawer_WhenTheWindowIsNarrow`, `TbAppShellTests.Navigation_ShouldCloseTheDrawer_WhenTheAddressChanges`
- [x] The app bar and the navigation stay in view as the page scrolls. `AppShellTests.AppShell_ShouldKeepTheBarAndNavigationInView_WhenThePageScrolls`
- [x] A control focused under the sticky app bar, such as by Shift+Tab, scrolls out from under it, also when an app sets `--tb-app-bar-height` on the root. `AppShellTests.AppShell_ShouldScrollAFocusedControlOutFromUnderTheBar_WhenTheBarCoversIt`, `AppShellTests.AppShell_ShouldScrollAFocusedControlOutFromUnderATallerBar_WhenTheRootSetsItsHeight`

## Touch

- [x] Tapping does what clicking does, and nothing depends on hover. `TbNavLink.razor`

## Focus

- [x] Screen readers hear a banner, a navigation landmark named "Main" and the main content, plus a skip link before them. `TbAppShellTests.Render_ShouldLayOutTheLandmarksAndASkipLink_WhenGivenEachPart`
- [x] The current page's link has `aria-current="page"`, and group and menu buttons say whether they're open with `aria-expanded`. `TbNavMenuTests.Render_ShouldListItsLinksAndMarkTheCurrentPage_WhenCalled`, `TbAppShellTests.MenuButton_ShouldOpenTheNavigationInADrawer_WhenPressed`
- [x] In forced-colors mode, the current link takes the system's highlight. `AppShellTests.AppShell_ShouldHighlightTheCurrentLink_WhenColorsAreForced`

## Not applicable

- Arrow keys between the links: the disclosure navigation pattern leaves them optional, and Tab reaches every link.
- Remembering whether the navigation was collapsed: the app binds `NavigationCollapsed` and keeps it where it likes.
- Choosing the breakpoint at run time: it is 768px, and an app's own stylesheet can override the rules that use it.
