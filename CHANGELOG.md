# Changelog

Trailblazor hasn't had a release yet. Until it does, two lists below run by date, newest first: renamed and removed
public API, and changes to markup and behavior that tests, styles or scripts can depend on. After the first release
they run by version. The docs site's Upgrading page is built from both.

## Unreleased

### Renamed and removed

#### 2026-11-16

- TbViewportLocator.FitAsync() and TbViewportDriver.Fit, which read the fit → CurrentFitAsync() and CurrentFit; FitAsync(ViewportFit) still sets the fit
- DialogOptions.Attributes and DialogAttributes → DialogOptions.WindowButtonAttributes and DialogWindowButtonAttributes

#### 2026-11-14

- TbViewportLocator.FitPageAsync() and FitWidthAsync() → TbViewportLocator.FitAsync(ViewportFit)
- page.Tb().ContextMenu(element) and new TbContextMenuLocator(…) → page.Tb().ContextMenu().On(key or element)
- TbQueryBuilderLocator.JoinAsync(string option, …) → JoinAsync(QueryJoin join, …)
- new TbTextFieldLocator, TbNumberFieldLocator or TbAutocompleteLocator with a label → page.Tb().TextField(label), NumberField(label) or Autocomplete(label)
- TbCommandPalette.DisposeAsync() → Nothing to call: Blazor disposes the component

#### 2026-11-12

- TbStepper.Active and ActiveChanged → TbStepper.ActiveKey and ActiveKeyChanged
- TbCarousel.Index and IndexChanged → TbCarousel.ActiveIndex and ActiveIndexChanged
- TbCarousel.AutoPlay → TbCarousel.AutoPlayInterval
- TbSortable.ItemKey → TbSortable.KeySelector
- TbSortable.ItemText → TbSortable.ItemLabel
- TbTreeView.IsDisabled and TbTreeSelect.IsDisabled → ItemDisabled
- TbTreeView.CanDrag → TbTreeView.CanMoveItem
- TbTreeView.CanDrop → TbTreeView.CanMove
- TbTreeView.OnDrop → TbTreeView.OnMove
- TreeDrop\<TItem> and TreeDropPosition → TreeMove\<TItem> and TreeMovePosition
- TbTreeView.ItemEnd → TbTreeView.ItemEndTemplate
- TbTreeItem.EndContent → TbTreeItem.End
- TbColumn.Cell → TbColumn.CellTemplate
- TbCalendar.DayContent → TbCalendar.DayTemplate
- TbChip.OnClose → TbChip.OnRemove
- TbSlider.ValueCommitted and TbRangeSlider.ValueCommitted → OnValueCommitted
- TbAlert.OnClose and TbBanner.OnClose taking MouseEventArgs → OnClose with no arguments
- TbCheckboxItem's TItem type parameter → TValue
- TbTreeSelect.Searchable and TbTransferList.Searchable → Filterable
- TbMultiTreeSelect.MoreLabel and TbTreeSelect.SearchLabel → Labels.More and Labels.Filter
- The text keys TreeSelect.Search and TransferList.Search (TbStrings.TreeSelectSearch and TransferListSearch) → TreeSelect.Filter and TransferList.Filter: a translation still set under an old key no longer shows
- TbTreeSelectDriver.SearchAsync and TbTreeSelectLocator.SearchAsync → FilterAsync
- TbAppShell.MenuLabel, NavigationLabel and SkipLinkText → TbAppShell.Labels.Menu, Navigation and SkipLink
- TbTabs.AddLabel → TbTabs.Labels.Add
- TbTextField.ClearLabel → TbTextField.Labels.Clear
- TbRangeSlider.StartLabel and EndLabel → TbRangeSlider.Labels.Start and End
- PaginationLabels.Navigation → TbPagination.AriaLabel
- The components' event argument types, such as RowClickEventArgs and TagRemoveEventArgs → Moved to Trailblazor.Internal.Events and hidden from IntelliSense; they're for the components' own use

#### 2026-11-11

- TbOverlay.Visible and VisibleChanged → TbOverlay.Open and OpenChanged

#### 2026-11-10

- TbDialogContent.CloseDialogAsync() → Dialog.ConfirmAsync()
- TbDialogContent\<TResult>.CloseDialogAsync(value) → Dialog.CloseAsync(value)
- TbDialogContent\<TResult>.CancelDialogAsync() → Dialog.CancelAsync()
- TbToolbarButton.Text → TbToolbarButton.Label, or AriaLabel for an icon-only button
- TbTooltip.DisposeAsync() → Nothing to call: Blazor disposes the component

#### 2026-11-09

- TbDialogContent's CloseButton, Modal, Position, ShowHeader, Size and WindowState → The same members on Dialog, the content's DialogReference
- new TrailblazorPage(page) and the Playwright locators' constructors taking an IPage → page.Tb() and its methods

#### 2026-11-02

- TbAlert.Dense and TbList.Dense → The theme's Density, set with IThemeService.SetDensityAsync
- Palette as an enum, such as (int)Palette.Clay or a switch over its values → Palette as a record: Palette.Clay and the others still name the built-ins, and Name tells them apart

#### 2026-11-01

- TbDialogContent\<TResult>.CloseAsync(value) and CancelAsync() → Dialog.CloseAsync(value) and Dialog.CancelAsync()
- TbDialogContent\<TResult>.CanDismissAsync(reason) → OnDismissAsync(reason); the dialog stays open unless the override closes it
- new TbSelectLocator or TbColorPickerLocator with a label → page.Tb().Select(label) or ColorPicker(label)

### Markup and behavior

#### 2026-11-16

- An interactive overlay counts finished clicks in `data-clicks` on its root; a click counts once its `OnClick`, and any close it brings, have run
- Every component takes the font of the text around it, as fields and tabs already did, rather than `--tb-font-family`; headings keep `--tb-font-family-heading`, and code and keys keep `--tb-font-family-mono`. A `--tb-font-family` that a stylesheet rule sets on a region no longer reaches its components; set `font-family` there, or the token in its `style` attribute
- An interactive chip that isn't only a link counts its finished clicks, toggles and removals in `data-commits` on its root, and a list item with an `OnClick` counts its finished clicks on its `li`
- A list item's `li` gets `data-static` on a static page and `data-ready` once it's interactive
- An interactive splitter counts the collapses and restores it has made in `data-commits` on its root, once `CollapsedChanged` has run, and each separator names the pane that Enter on it collapses and restores in `data-toggles`
- An interactive accordion item's header and nav group's button count their toggles in `data-commits` once `ExpandedChanged` has run
- An interactive stepper's list counts the step clicks that move it, in `data-commits`, once `ActiveKeyChanged` has run
- An interactive carousel's picker counts the picks that move the slides, in `data-commits`, once `ActiveIndexChanged` has run
- A calendar's root counts the days picked in `data-commits`, once `ValueChanged` has run, as other fields' roots count their changes
- A file upload's input no longer carries `data-choices`; its field's root counts each choice in `data-commits` once `ValueChanged` has run
- TbRoot's dialog host renders a hidden element with `data-tb-open-dialogs`, the number of open dialogs that aren't closing
- A static form marks its submit button busy during an enhanced post too, and drops a second submit until the answer updates the page
- A number field too narrow for its step buttons hides them on static and prerendered pages too
- A masked text field's input gets `data-ready` once its mask has attached, not as soon as the field is interactive, and keeps it when the mask is taken away
- A tab list's `data-commits` counts tab clicks, not just adds and closes; a click counts once the tab's `OnClick` and the selection's callbacks have run

#### 2026-11-15

- The data grid's `data-commits` counts a sort, filter, page, page size, selection, expansion or row click once the app has answered it, not just row moves and batch saves; a static grid leaves `data-commits` off
- A pagination counts the user's finished page changes in `data-commits` on its `nav`, so a test can wait for a `PageChanged` that runs after the page shows
- A tree view counts the user's finished clicks, toggles, checks and drops in `data-commits` on the tree, so a test can wait for a callback that runs after the row shows the change
- Menus and context menus count finished item handlers in `data-commits` on their list, so a test can wait for a handler that runs after the menu closes
- The toaster counts finished toast actions in `data-commits`, so a test can wait for an action that runs after its toast starts to fade
- A canceled dialog's `trailblazor.outcome` is `canceled`, not `cancelled`; update dashboards and alerts that filter on the old value
- When interactivity is set page by page, TbRoot's `data-tb-interactive` mark carries `data-tb-session`, and the page's root gets `data-tb-navigated` once an enhanced navigation leaves the first page. From then on `page.Tb().InteractiveAsync()` passes over TbRoot's mark, even one that first renders after the navigation
- A text field's show-password button works on static pages too, through the page's script
- Items in a `Variant.Text` toggle group carry `tb-button--quiet`. A pressed one takes the tone's light fill and text color, with a bar in that color along its bottom, in place of the deeper tint and body text it took before
- In forced-colors mode, a pressed toggle keeps the system highlight while hovered or pressed down, in every variant
- The page's root carries `data-tb-script` once Trailblazor's script has loaded, on static pages too
- A transfer list's root counts the commits the app has answered in `data-commits`
- An autocomplete's list carries `data-results-for`, the text its suggestions answer
- Leaving an autocomplete whose typed text isn't a suggestion restores the value's text, and that now counts as a commit in `data-commits`
- A checkbox's root carries `data-indeterminate` while it shows its mixed state
- TbRoot's `data-tb-interactive` mark comes off on an enhanced navigation when interactivity is set page by page; put a `TbInteractiveMark` in each page's interactive part to mark the page
- `page.Tb().InteractiveAsync()` waits for the first `data-tb-interactive` mark, so a page with several marks no longer fails Playwright's strict mode

#### 2026-11-14

- A clickable TbListItem's `id` is on its action, the button or link, not on the item
- Every pressed outlined or soft TbButton takes the solid fill, not only a toggle in a group
- IDialogService.ConfirmAsync and AlertAsync take a `DialogOptions`, which breaks mock setups of the old signatures and custom IDialogService implementations

#### 2026-11-13

- A loading TbButton no longer has the `tb-button--loading` class; style and test `aria-busy="true"` instead
- A tooltip's requested side is in `data-placement`; `data-side` holds the side the script placed it on
- Setting DialogReference.Size clears a Width set earlier
