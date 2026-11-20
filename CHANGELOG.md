# Changelog

Trailblazor hasn't had a release yet. Until it does, two lists below run by date, newest first: renamed and removed
public API, and changes to markup and behavior that tests, styles or scripts can depend on. After the first release
they run by version. The docs site's Upgrading page is built from both.

## Unreleased

### Renamed and removed

#### 2026-11-16

- new TbSliderLocator, TbRangeSliderLocator, TbTimePickerLocator, TbDatePickerLocator, TbCheckboxGroupLocator, TbRadioGroupLocator, TbRatingLocator, TbToggleGroupLocator, TbTagInputLocator, TbRichTextEditorLocator, TbListBoxLocator, TbTransferListLocator, TbFileUploadLocator, TbDateRangePickerLocator, TbCalendarLocator, TbDateTimePickerLocator or TbTreeSelectLocator with a label → page.Tb().Slider(label) and the like, or new TbSliderLocator(scope, locator) and the like, given the element the field's id goes on
- new TbMenuLocator(scope, trigger name) or TbAccordionItemLocator(scope, header text) → page.Tb().Menu(name) and AccordionItem(text), or new TbMenuLocator(scope, locator) with the trigger and TbAccordionItemLocator(scope, locator) with the item or its header's button
- new TbTreeLocator, TbTabsLocator or TbGridLocator with a name → page.Tb().Tree(name) and the like, or new TbTreeLocator(scope, locator) and the like, given the tree, the tabs or the grid
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

#### 2026-11-20

- A `TbRibbon` can collapse to its tabs. `Collapsible` adds a button after the tabs (`data-part="collapse"`, with `aria-expanded`) and turns on Ctrl+F1 in the ribbon and a double-click on a tab; `Collapsed` binds the state, and the ribbon has `data-collapsed` while it is collapsed. Choosing a tab then shows that tab's commands over the page, marked by `data-peek`, until a command is chosen, Escape is pressed or focus leaves the ribbon. `Labels.Collapse` names the button, and `TbStrings.RibbonCollapse` is its text to translate. The ribbon's root sets `data-ready` once Ctrl+F1 and a collapsed ribbon's tabs work, and carries `data-static` on a static page. `page.Tb().Ribbon(name)` and `cut.Tb().Ribbon(name)` gain `CollapseAsync()` and `ExpandAsync()`, and `IsCollapsedAsync()` and `IsCollapsed` read the state.
- `TbTabs` has `Start` and `End` slots for content before the tabs and after them, in their row; `End` follows the add button. `TbRibbon` has the same two, for an app button or a File menu before the tabs and quick actions after them. The tabs' row is now one grid whenever it holds anything beside the tab list, with a cell each for `.tb-tabs__start`, the scroll buttons and `.tb-tabs__actions`.
- On a static page, and before the page is interactive, a `TbTabs` panel that starts with a control is no longer a tab stop, as it already wasn't once the page was interactive. The startup script now makes the interactive script's check as it loads, after an enhanced navigation and as a tab is selected.
- In a `TbTreeView`, and the tree of a `TbTreeSelect`, Enter and a double-click no longer open or close a disabled row that has children, as a click never did. The row's chevron and the Left and Right arrow keys still open and close it.

#### 2026-11-19

- A `TbRibbon` too narrow for a tab's groups now collapses them one at a time, from the last, each into a button (`data-part="group-button"`, with `aria-expanded`) that opens the group's controls in a popup. While a group is collapsed, it has `data-collapsed` and its `items` part carries the `popover` attribute. The row has `data-fitting` while a fit is still to come. Every group renders its button from the start, `hidden` until the group collapses, with the group's name as its text, so a test that finds a group's name by its text, or counts a group's buttons, now finds one more. `TbRibbonGroup.CollapseOrder`, which renders as `data-collapse-order` on the group, picks the groups that go first, and `TbRibbonGroup.Icon` gives the button an icon. The controls stay the same elements, so they keep what they hold. A row too narrow even for the buttons scrolls sideways, as does any row before the page is interactive. The Playwright helper's `ClickAsync` opens a collapsed group to reach a button in it
- A `TbPopover` or a `TbDateRangePicker`'s calendar that closes no longer takes focus back from a control that took focus right after it closed, such as a field the app focuses as the panel closes, or another control a test presses a key on as soon as it closes the panel. When nothing else has taken it, focus still returns to the trigger. The components built on `TbPopover` behave the same way, such as `TbDatePicker`, `TbDateTimePicker`, `TbColorPicker`, `TbTreeSelect` and `TbPopconfirm`
- A `TbTreeView` row template's `data-part="text"` now keeps its ellipsis and a filter match's heavier weight when the template puts it in one wrapper of its own, such as a `div` that carries an id. It used to have to sit directly in the template, and a wrapped name ran under the row's end content. The wrapper gets `min-width: 0`, and the wrapped text becomes an `inline-block` no wider than its wrapper, so make a wrapper that holds other content beside the text, such as an icon, a flex container, or a long name drops to a line of its own. Text marked directly in the template is styled as before. Another component's `data-part="text"` in a wrapper, such as a label's, is left alone
- `IDialogService.ConfirmAsync` now returns false when its dialog is closed from code with `CloseAsync(false)`. It used to return true for any close that wasn't a cancel, so a test that answered `LastConfirm().CloseAsync(false)` sent the app down its yes branch. The confirm's own buttons answer as before, as do `ConfirmAsync()`, `CloseAsync(true)` and `CancelAsync()`, and `FakeDialogService` follows the same rule. With a `telemetryName`, a confirm closed with false still counts as `confirmed`, since the outcome records how the dialog closed and never its answer
- The buttons in `TbDialogActions` now stay in view at the bottom of every dialog whose body scrolls, as they already did in a full or maximized dialog and a drawer. In a sized dialog they used to scroll with the body, so long content kept them out of sight until the body was scrolled to the end. A field behind them that Tab moves to scrolls clear of them, as long as the actions fit on one row. Library content that sits on a layer of its own, such as a grid's pinned cells, now scrolls under them instead of drawing over them, in a full dialog and a drawer too. A contained `TbOverlay` still covers them: its `z-index` went from 1 to 5, which also puts it over a grid's sticky header. A dialog that doesn't scroll looks as before, unless it has a background of its own: the actions now paint the default surface color behind them, so give its `.tb-dialog__actions` the dialog's background too. A dialog with `TopLayer` off now clips its content to its rounded corners, as the browser does for one in the top layer. To let one dialog's actions scroll with its body again, give its `TbDialogActions` a `Class` and set `position: static` on that class, in CSS outside any cascade layer
- In a `TbTreeView` with an `OnActivate`, Enter and a double-click no longer open or close a row that can be selected: they run the handler and leave the row as it is, so they no longer count `expand` or `collapse` either. The row used to toggle after the handler finished, so a handler that waited on a dialog saw the row open or close as the dialog closed. The chevron and the arrow keys still open and close rows, and `*` still opens them. A parent row that can't be selected still opens or closes on Enter, now as the key is pressed rather than after the handler runs. A tree with no `OnActivate` behaves as before
- `TbDockManager` panels can be put away on auto-hide strips: Auto-hide in a panel's menus (`data-move-hide`) puts it on a strip along an edge of the dock (`data-part="strip"`, with a `data-part="strip-button"` for each panel), and its button slides the panel out over the dock and back; Escape, or a press or the focus anywhere but it and the strips, slides it back too. Slid out, it is drawn as a window in front of the others, with `data-edge` and the classes `tb-dock__group--revealed` and `tb-dock__panel--revealed`. `DockLayout.AutoHide(panel, edge)`, `AutoHidden`, `Revealed` and `Conceal()` do it from code, with the new `DockAutoHidden` and the constructor's new `autoHidden` and `revealed` arguments; `Open` slides an auto-hidden panel out, and `ToJson` keeps the strips. `DockLabels.AutoHide`, `Strip`, `RevealedGroup` and `AutoHidden` word it, telemetry counts `auto_hide` and `reveal`, and `page.Tb().Dock()` and `cut.Tb().Dock()` gain `AutoHideAsync`, `RevealAsync`, `ConcealAsync` and `AutoHiddenAsync()` (Playwright) or `AutoHidden` (bUnit); their `Floating` leaves out the panels on strips, and `Panels` leaves out all but the one that is slid out, which `Showing` lists too
- A `TbDockManager` panel that is slid out from a strip resizes by its inner edge, a focusable separator (`data-part="slide-separator"`, class `tb-dock__slide-separator`) with a grip menu on it (`data-part="slide-grip"`): a drag, the arrow keys (1% of the dock, or 10% with Shift; Home and End for the smallest and the largest) or the grip's two steps, between a tenth and nine tenths of the dock and no less than `MinGroupSize`. `DockLayout.ResizeAutoHidden(panel, size)` does it from code, telemetry counts `resize_auto_hidden`, and the helpers' `Size`/`SizeAsync` and `ResizeAsync` take the edge's name, such as "Resize Notes"
- A `TbDockManager` tab dropped on an auto-hide strip puts its panel away there, and a strip's button drags into the dock, which puts its panel where the mark shows, or onto another strip. With nothing docked, all of the dock's empty area takes a dropped tab or button, where only the band along its edges used to. The strip a drop would go to has `data-drop`, a dragged strip button has `data-dragging`, and the dock has `data-moving` during either drag
- `TbDockManager`'s outermost element is now a frame (`data-part="frame"`, class `tb-dock-frame`) around its strips and the area its groups share; that area keeps `data-part="dock"` and the class `tb-dock`. `Class`, `Style` and other attributes go on the frame, which is the element they size; a dock without strips is laid out as before, and the dock's border and background stay on `.tb-dock`, so restyle them there
- `TbDockManager` no longer loses the focus when a window goes with its last tab while a group fills the dock: the focus goes to a tab that shows, not to one the maximized group covers
- `TbDockManager` separators now state how far they can go in `aria-valuemin` and `aria-valuemax`, and a slid-out panel's edge states the real least size where `MinGroupSize` is more than a tenth of the dock. The Playwright helper's `ResizeAsync` throws `ArgumentOutOfRangeException` for a percent outside those limits, where it used to time out. A share exactly halfway between two whole percents now rounds up in `aria-valuenow`, as the script rounds it
- New `TbRibbon`: a tabbed band of commands (class `tb-ribbon`, `data-part="ribbon"`), built on `TbTabs`. Each `TbRibbonTab` shows a `toolbar` (`data-part="commands"`, named by the tab's `Title`) of `TbRibbonGroup`s, each a `group` named by its `Label` (`data-part="group"`, with `data-part="items"` and `data-part="group-label"`). A tab's commands are one tab stop that the arrow keys move through, across its groups. `TbRibbonButton` is a small button with its icon beside its text, `Large` with the icon above it, or `IconOnly` with a tooltip, and `@bind-Pressed` makes it a toggle. Testing helpers: `Ribbon(name)` in both testing packages
- A `TbTabs` panel that starts with a control is no longer a tab stop of its own, so Tab goes from the selected tab to that control with no stop on the panel in between. A panel that starts with text or a picture is still a stop, and so is the shown panel on a static page, or before the page is interactive. The new `FocusablePanels` can override this: `TabPanelFocus.Always` makes every panel a stop, and `TabPanelFocus.Never`, meant for tabs whose panels all start with a control, makes none a stop
- A `TbMenu` or `TbContextMenu` that closes no longer takes focus back from a control that took focus right after it closed, such as a field an item's handler focuses, or another control a test presses a key on as soon as it closes the menu. When nothing else has taken it, focus still returns to the menu's trigger, or, for a context menu, to what had focus when it opened
- `TbContract.VerifyAsync` no longer reports a trigger with `AttachOnFirstUse`, such as a move handle, for losing its focus and click handlers once it's used: the trigger listens for its first use only and drops those handlers together with its `data-attach-on-use` mark. A component of your own that holds a `TbSortable`, a `TbKanban`, a `TbDataGrid` with row move handles, or a menu, popover or select with `AttachOnFirstUse` no longer fails the check because of those triggers; any other handler such a trigger loses is still reported
- `TbDockManager`'s menus of single steps now turn off a step that has nowhere left to go, as `TbSplitter`'s grip menu does: when the menu opens, the item gets `aria-disabled="true"`, and choosing it does nothing and leaves the menu open. That goes for Larger and Smaller on the grip of a separator or of a slid-out panel's edge, and for Left, Right, Up, Down, Wider, Narrower, Taller and Shorter on a floating window. A step that can go part of the way stays on. The Playwright helper's `Menu(...).ChooseAsync` throws an `InvalidOperationException` for a step that is turned off
- A slow tap on a `TbSortable` handle, or on a `TbDataGrid` row's move handle, now opens its menu: a finger that rested on the handle for a moment and lifted in place had its click swallowed as the end of a drag
- In a `TbMentionInput`, Enter or Tab pressed while suggestions show and newer ones are on their way puts the first of the newer ones in once they arrive, where the key used to do nothing

#### 2026-11-18

- New `TbDockManager` and `TbDockPanel`: a dock (`tb-dock`, `role="group"`, `data-part="dock"`) whose children are each group's row of tabs (`data-part="group"` around a `tablist`), each rendered panel (`role="tabpanel"`, `data-part="panel"`, `data-panel` with its id) and a `separator` between groups, placed by `--tb-dock-x`, `--tb-dock-y`, `--tb-dock-w` and `--tb-dock-h` in percent of the dock; a `DockLayout` of `DockSplit`s and `DockGroup`s says where each panel is and saves as JSON; `page.Tb().Dock()` and `cut.Tb().Dock()` drive it in tests
- `TbMarkdownEditor` fills its field's box: its toolbar, text and preview were squeezed into a strip at the box's start
- `TbDockManager` moves panels: each group has a menu after its tabs (`data-part="group-menu"`, class `tb-dock__menu`) that moves the showing panel along its row, to another group, or to a new group beside its own or along an edge of the dock, and closes it; its button is a new Tab stop between a group's tabs and its panel; `DockLayout.Move` and `MoveToEdge` do the same from code, with the new `DockPosition`; focus follows the moved tab and screen readers hear where it went; telemetry counts the action `move`
- Each `TbDockManager` separator has a grip on its line (`data-part="grip"`, class `tb-dock__grip`, a child of the dock after its separator); dragging the grip moves the separator, and its menu resizes in steps of 10%, the single-pointer alternative to dragging (WCAG 2.5.7)
- A right-click on a `TbDockManager` tab, Shift+F10 or the context menu key on it, or a finger resting on it opens a menu named "Tab options" that moves or closes that tab's panel, whether or not it shows; each group's tab list now sits inside a `TbContextMenu` (`data-part="target"`, class `tb-dock__tab-area`), so on a page with a dock, a test should find its own context menu by name; `DockLabels.TabMenu` words the menu's name, and `page.Tb().Dock()` and `cut.Tb().Dock()` gain `ChooseFromTabMenuAsync(panel, path)`
- Dragging a `TbDockManager` tab moves its panel: along its own row of tabs or another group's, to the end of a group, to a new group on one side of a group, or to one along an edge of the dock; a mark (`data-part="drop-mark"`, class `tb-dock__drop`, a child of the dock) shows where it would land, Escape cancels, and with a finger the drag starts only once the tab has been held still for a moment; the dock has `data-moving` and the tab `data-dragging` while the drag runs; after the drop, focus is on the moved tab, screen readers hear where the panel went, and telemetry counts the action `move`, as after a move from the menu
- A finger that picks up something to drag inside a `TbContextMenu`, such as a `TbDockManager` tab or a `TbTreeView` row that can be moved, has half a second from the pickup to start moving before the menu opens: the menu's wait starts over when the item is picked up (a bubbling `tb-drag-pickup` event), not when the finger comes down, and a context menu the browser asks for in that time is ignored
- A `TbDockManager` group can fill the dock: a double click on a tab, or Maximize in the group's menu or the tab's own (`data-move="maximize"`), covers the other groups (class `tb-dock__group--covered`) and marks the group `data-maximized`, and a second double click, Restore in those menus (`data-move="restore"`) or the new Restore button beside the group's menu (`data-part="restore"`, class `tb-dock__restore`) shows every group again; the button is a new Tab stop between the tabs and the menu button of a group that fills the dock; screen readers hear "Panel group 2 fills the dock." and "All panel groups show again.", and telemetry counts the actions `maximize` and `restore`; `DockLayout.Maximize`, `Restore` and `Maximized`, with `DockGroup.Maximized`, do the same from code, `ToJson` keeps it, `DockLabels.Maximize`, `Restore`, `Maximized` and `Restored` word it, and `page.Tb().Dock()` and `cut.Tb().Dock()` gain `MaximizeAsync(panel)` and `RestoreAsync()`; while a group fills the dock, `ShowingAsync()` and `Showing` leave out the panels it covers, and a helper that clicks a covered panel's tab throws
- `TbDockManager` panels can float: Float in a group's menu or a tab's own (`data-move="float"`) takes the panel out into a window above the docked groups, drawn as a group is, with `data-floating`, the classes `tb-dock__group--floating` and `tb-dock__panel--floating` and its layer in `--tb-dock-z`, and named "Floating panel group" with its number; Dock in a window's menus (`data-move="dock"`) puts the panel back, and tabs drop onto and out of windows; a press or the focus in a window behind another brings it forward, which raises `LayoutChanged` and is saved but isn't counted in `data-commits`; each group's row now carries its number in `data-group`; screen readers hear "Notes floats in a window.", and telemetry counts the actions `float` and `dock`; `DockLayout.Float`, `FloatAt` and `Floating`, with the new `DockFloat`, do the same from code, `GroupOf` finds a floating panel's group too, while `Groups` stays the docked ones, the `DockLayout` constructor takes the windows as a third argument, `ToJson` keeps them, `DockLabels.Float`, `Dock`, `Floated` and `FloatingGroup` word it, and `page.Tb().Dock()` and `cut.Tb().Dock()` gain `FloatAsync(panel)`, `DockAsync(panel)` and `FloatingAsync()` (Playwright) or `Floating` (bUnit); `ShowingAsync()` and `Showing` list each window's showing panel too, even while a group fills the dock; the dock now keeps its stacking order to itself (`isolation: isolate`), so a `z-index` inside a panel no longer lifts content above page elements outside the dock
- A `TbDockManager` tab dropped on the middle of its own group, which changed nothing before, pulls its panel out into a floating window under the pointer, half as wide and as tall as the dock and whole inside it, unless the tab is alone in a window; a tab alone in its docked group floats when it is dropped anywhere on its panel, apart from the dock's edge while other panels are docked; the drop mark shows the window, and telemetry counts `float`. Float is also in the menus of a window that holds other panels, where it gives the panel a window of its own
- A `TbDockManager` floating window moves and resizes: a Move menu starts its row (`data-part="window-move"`, class `tb-dock__window-move`, a new Tab stop before the window's tabs) and a grip menu sits in the corner where its box ends (`data-part="window-grip"`, class `tb-dock__window-grip`, a child of the dock and a new Tab stop after the window's panel); dragging either, or the empty part of the row, moves or resizes the window, the arrow keys on either do so by 1% of the dock or 10% with Shift, and their items (`data-step`) by a tenth of the dock; the dock has `data-resizing` during the drag; telemetry counts the actions `move_window` and `resize_window`; screen readers hear where the window is, or how big, after each change, in words that `DockLabels.MovedWindow` and `ResizedWindow` replace; `DockLayout.MoveWindow(panel, x, y, width, height)` gives a window another box from code; `DockLabels.MoveWindow`, `MoveWindowHint`, `ResizeWindow`, `ResizeWindowHint`, `Left`, `Right`, `Up`, `Down`, `Wider`, `Narrower`, `Taller` and `Shorter` word it; `page.Tb().Dock()` and `cut.Tb().Dock()` gain `MoveWindowAsync(panel, right, down)`, `ResizeWindowAsync(panel, wider, taller)` and `WindowAsync(panel)` (Playwright) or `Window(panel)` (bUnit)
- `TbDockManager` counts a finished action in `data-commits` only after the focus has moved to where the action puts it, one render later than before, so a test that waits for the count can use the keyboard at once
- A slow tap on a draggable `TbTreeView` row or a `TbDataGrid` column header that drags is a tap again: a finger that rested for a moment and lifted in place had its click swallowed as the end of a drag, and on a header could move the column
- New `TbDockManager` `PersistenceKey`: saves the layout in the browser's local storage after each change and restores it on the next visit
- A `TbDockManager` panel that holds an `<iframe>` now stays in place while other parts move around it, so the frame stays loaded in every browser, unless two panels with frames trade places
- `DockLabels` gains the words of the menus and of what is said after a move
- `page.Tb().Dock()` and `cut.Tb().Dock()` gain `MoveTabAsync`, `MoveToGroupAsync`, `MoveToNewGroupAsync` and `MoveToEdgeAsync`
- `TbSplitter` reads and saves its layout under its current `PersistenceKey` when the key arrives or changes after the first render, where it used to keep the first key, or save under `null` when it had none at first
- `page.Tb().Splitter(name)` and `cut.Tb().Splitter(name)` find only a `TbSplitter`'s separators, so a `TbDockManager`'s separator with the same name isn't taken for one

#### 2026-11-17

- With `RichTextBlocks.Tables` in `Blocks`, `TbRichTextEditor`'s toolbar gets a Table button (`data-command="table"`) that puts in a table of three columns with a header row, and a table menu (`data-part="table-menu"`) that adds rows above or below and columns left or right, deletes the row, column or table, and turns the header row on or off; a table, rule or image chosen from the toolbar while the caret is in a table goes in after it
- `TbRichTextEditor` edits tables where `Blocks` includes `RichTextBlocks.Tables`: cells are `<td>` and `<th>` elements in a `<table>`, Tab and Shift+Tab move between cells (Tab in the last cell adds a row), Enter breaks the line in a cell, and cells never merge; deleting inside a table empties cells, and a selection reaching outside it takes the whole table
- When reading HTML, text and blocks such as paragraphs, headings and rules inside a table but outside its cells go before the table, in the order a browser shows them, whether or not the schema allows tables; a row, cell, caption or column ends whatever else is open in the table, such as bold text, which no longer carries into the cells
- `RichTextDocument` reads and writes tables where the schema allows the new `RichTextBlocks.Tables`: `ToHtml` writes a `<table>` with its first row in a `<thead>` of `<th scope="col">` cells when they're all header cells, and the other rows in a `<tbody>`; markdown reads and writes pipe tables, so `TbMarkdownEditor`'s preview shows them when its `Schema` allows tables
- When reading HTML, a cell or row that starts while another is open ends it, as in a browser, so formatting left open in one cell no longer carries into the next
- New `TbSmartPaste`: a button (`tb-smart-paste`, `data-part="button"`) inside a `TbForm` that fills the form's fields from the clipboard through the app's `Extract` model, with the result beside it (`data-part="status"`) and a dialog (`tb-smart-paste__box`) that asks for the text where the clipboard can't be read
- `TbRichTextEditor` takes an `UploadImage` function: with `RichTextBlocks.Images`, the toolbar gets an Image button (`data-part="image"`, `aria-haspopup="dialog"`) that asks for a file and its alt text (or Decorative, for none), stores the file through the app and puts the image in at the caret; `MaxImageSize` caps the file's size
- `TbRichTextEditor` shows images where `Blocks` includes `RichTextBlocks.Images`: each is an `<img>` between paragraphs that the caret steps over, a click selects whole, and Backspace or Delete removes; the new `ImageSchemes` sets which sources an image may use
- New `TbSpeechToText`: a toggle button (`tb-speech-to-text`, `data-part="button"`, `aria-pressed` while listening) that shows only where the browser recognizes speech and hands each phrase it hears to the app
- New `TbInPlaceEditor`: a value (`tb-in-place-editor`, with `data-editing` while editing) shown in a button (`data-part="display"`) that turns into its editor (`data-part="editor"`) with save and cancel buttons
- New `TbBackToTop`: a floating button (`tb-back-to-top`, with `data-shown` while it shows, and `data-part="button"`) that appears once the page has scrolled down and takes it back to the top, moving focus there
- New `TbHighlight`: text (`tb-highlight`) with each match of a search in a `mark` (`data-part="match"`)
- `TbAccordionItem`, `TbAlert`, `TbBanner`, `TbAppShell`, `TbCarousel`, `TbOverlay`, `TbPagination`, `TbSplitter`, `TbStepper`, `TbThemePicker` and `TbInsertDivider` take a `TelemetryName` and, when given one, count `trailblazor.control.shown` and each use as `trailblazor.control.used`, with what was done as `trailblazor.action`; `TbHotkeysHelp` takes one too and counts as a dialog does
- New `TbMasonry`: cards of different heights in columns (`tb-masonry`, with `data-ready` once they're placed), each under the shortest column in the order given
- `TbQrCode`, `TbBarcode`, `TbImage`, `TbTimeline`, `TbTable`, `TbVirtualList`, `TbProgress`, `TbAvatar`, `TbAvatarGroup` and `TbBadge` take a `TelemetryName` and, when given one, count `trailblazor.control.shown` as they first render
- New `TbWatermark`: content with tiled marks over it (`data-part="marks"`, `aria-hidden`, taking no clicks) and a visually hidden copy of what they say (`data-part="label"`) before the content
- New `TbTour`: a full-page layer (`tb-tour`, with `data-step`) whose shade (`data-part="shade"`) dims the page around the step's target, and whose step is a modal `dialog` (`data-part="step"` with `data-target`) named by its title and described by its body, with Back, Next (Done on the last step) and Skip tour buttons, and the step's position; the page behind it is inert while it shows
- New `TbMentionInput`: a field whose text area or input (`data-part="input"`, with `aria-autocomplete="list"`, `aria-controls` and `data-triggers`; a one-line input is a `combobox` with `aria-expanded`) opens a listbox of suggestions (`data-part="listbox"`, with `data-results-for`; each `data-part="option"` with `data-insert`) under the trigger being typed; the field marks the active suggestion with `aria-activedescendant`, and the count of suggestions is read out
- New `TbMarkdownEditor`: a field whose `textarea` (`data-part="input"`) holds markdown, with a toolbar of marks (each button has `data-command` and `aria-keyshortcuts`), a Preview toggle (`data-part="preview-toggle"`, `aria-pressed`) and a preview region (`data-part="preview"`) of the rich text editor's HTML; `data-ready` on `data-part="editor"` once the toolbar works
- New `RichTextDocument.FromMarkdown` and `ToMarkdown` read and write markdown
- `TbCarousel`'s previous and next chevrons mirror in right-to-left text (their icons get `tb-icon--mirrored`), so next points toward the end of the line and previous toward the start
- New `TbMenubar`: a `menubar` whose `TbMenu` children's triggers become `menuitem`s, shown as text without a chevron, with one tab stop between them once the page is interactive (`tabindex` 0 on one, -1 on the rest)
- New `TbLightbox`: a full-window modal dialog (`tb-lightbox`) whose header holds zoom and rotation buttons (`data-part="tools"`), and whose body shows one image in a scrolling stage (`data-part="stage"` with `data-zoom` and `data-turns`) with previous and next buttons over it; under the image are a caption and a position, and a polite live region reads out each new image
- New `TbCascader`: a combobox whose popup (`data-part="popup"`) holds one listbox for each level (`data-part="column"` with `data-level`), or a list of matching paths while the user types (`data-part="results"`); a choice with more under it has `data-more` and is named with "more choices"
- New `TbKanban`: a region of columns (`role="group"`, `data-part="column"` with `data-key` and `data-full`), each with a title, a count and a sortable list of its cards; the columns' lists share one group, so cards move between columns; a move into a full column is refused and read out
- A `TbAutocomplete` with `Columns` shows its suggestions as cells (`data-part="cell"`) under a row of titles (`data-part="columns"`, each `data-part="column-title"`, hidden from screen readers), and names each option by its columns' titles and values
- New `TbQrCode` and `TbBarcode` (Code 128 and EAN-13): SVG images (`role="img"`) named by their value, which `data-value` holds; a QR code's modules are `data-part="modules"`, and a barcode's image is `data-part="bars"` with its printed value in `data-part="text"`
- New `TbAiPrompt`: a region named by its visible label, with a prompt box, suggestions (`data-part="suggestion"`), a generate button that turns into Stop, and responses newest first (`data-part="output"`, `data-status` of generating, done, stopped or failed); a response's start, end, stop or failure is read out through the announcer

#### 2026-11-16

- New `TbChat`: a `feed` of `article` messages (`data-part="message"`, `data-own` on the user's own), who is typing, attachments, replies that stream in and a composer; new messages from others are read out through the announcer
- A field with `ShowValidationStatus`, or every field with `TrailblazorOptions.Fields.ShowValidationStatus`, shows its `ValidationAsync` check's status under it in `data-part="validation-status"` (`data-status="checking"` or `"valid"`), marks its input `aria-busy="true"` while the check runs, and puts an icon in each error message; the input's `aria-describedby` now includes the status while it shows. New with it: a field's `ValidationStatus` and `ValidationStatusChanged`, `ValidMessage`, `CheckingMessage`, `StatusIcons` and `StatusTemplate`, `TbForm.IsValidating`, `TrailblazorOptions.Fields.StatusIcons`, and `FieldClasses.Status` and `ChoiceClasses.Status`
- A `TbTreeItemEnd` in a tree's `ItemTemplate` or a `TbTreeItem`'s `TextContent` fills that row's end slot (`data-part="end"`), after its `ItemEndTemplate` or `End`, so one component can draw a row's text and its end controls
- A carousel's turn moves the slides by default (`Transition`): during a move its slides carry `data-move`, `data-direction` and `data-transition`, and the slide moving out stays drawn with `data-leaving`, `aria-hidden="true"` and `inert` until the move ends; `CarouselTransition.None` keeps the instant turn, and someone who asks for reduced motion always gets it
- An interactive carousel with a transition, not only one whose slides turn by themselves, now waits for its script to load before it carries `data-ready`, and the Playwright carousel helpers wait until a move has ended
- A popover closes when Tab moves focus past its last control and out of the page, as it already did when Tab moved focus to another control
- Pressing a splitter's separator focuses it without the focus highlight, which now shows only for the keyboard; the separator carries `data-pointer-focus` from the press until a key is pressed or focus leaves it, as a range slider's thumbs do
- In Chromium, turning the mouse wheel over a focused number field no longer steps its value; the page scrolls as it does over any other field
- From a value off the step, a number field's step buttons, and a formatted field's arrow keys, now land on a whole number of steps from `Min`, or from zero without one, as a number box's own arrows do: 1.5 with a step of 1 goes to 2 or 1, where the buttons used to give 2.5 or 0.5. A `Step` of 0 or less now throws an `InvalidOperationException`
- The data grid leaves out the edit and focus-leave handlers of rows and cells that can't be edited, where it used to render empty ones that made the server log an error each time the browser sent their events
- A field in a `TbFieldRow` shrinks to its column, where its box used to stay as wide as its input's `size` and spill into the next column
- A masked text field no longer commits on the Enter that ends an input method's composition, matching the code field
- The bUnit typing drivers (text, number, date, time, color and autocomplete) and the tag input's `AddAsync` refuse a field inside a disabled `fieldset` or marked `aria-readonly="true"`, as the choice drivers already did, and the choice drivers now also refuse one with `readonly`
- `AccordionItem(header)` in both testing packages also finds an item whose header shows a badge by any run of text in its title outside the badge, so a header that shows a count is found by its title alone
- The bUnit `Grid(name).WaitForRowsAsync()` fails with the grid lookup's own message when no grid has the name, rather than inside bUnit's wait
- A required field leaves its required message out of the `EditContext` when a validator already reports the model's `[Required]` for it, so `GetValidationMessages()` and a `ValidationSummary` hold the message once; the form's `Errors` and the field still show the field's wording, now also for a `[Required]` that sets its own message
- The Playwright tree select's `OpenAsync`, and the helpers that open its tree, wait until its trigger carries `aria-expanded="true"`, so `IsOpenAsync()` returns true straight after
- An interactive dialog counts its window state changes in `data-window-commits` once `TbDialogHeader.WindowStateChanged` has run, and the dialog's `MinimizeAsync`, `MaximizeAsync` and `RestoreAsync` wait for it in both testing packages
- `TbRoot`'s hidden dialog element counts answered dialogs in `data-tb-answered-dialogs` on an interactive page: a `TbDialog` or `TbDrawer` once its `OpenChanged` has run, and a dialog from `IDialogService` once its result is set. The dialog's `ChooseAsync` and `CancelAsync` wait for it in both testing packages
- The data grid's `data-commits` also counts a column the user resizes, once `StateChanged` has run; a width the grid measures for itself isn't counted
- An interactive toolbar carries `data-fitting` while a fit is still to come, such as after a resize
- A theme picker on an interactive or prerendered page carries `aria-busy="true"` on its group until it has read the theme the page shows
- The bUnit `Button(name).ClickAsync()` leaves the test's `NavigationManager` alone for a link button with `target="_blank"`, as the list and menu drivers do for a link that opens in a new tab
- The bUnit `List(name).ClickAsync(item)`, `Menu(trigger).ChooseAsync(path)` and `ContextMenu(name).ChooseAsync(path)` take the test's `NavigationManager` to a link item's address, as a browser follows it, unless the link opens in a new tab
- The Playwright select's `ChooseAsync` and `OptionAsync`, and a static list box's `ChooseAsync`, throw for a name the field doesn't have, listing the options it does have, as the bUnit driver does; `ChooseAsync` no longer times out on the click, except in a long list that shows only the options in view
- The bUnit `List(name)` throws for a name no list has, listing the names the lists do have, rather than saying no list is in the markup
- An interactive overlay counts finished clicks in `data-clicks` on its root; a click counts once its `OnClick`, and any close it brings, have run
- Every component takes the font of the text around it, as fields and tabs already did, rather than `--tb-font-family`; headings keep `--tb-font-family-heading`, and code and keys keep `--tb-font-family-mono`. A `--tb-font-family` that a stylesheet rule sets on a region no longer reaches its components; set `font-family` there, or the token in its `style` attribute
- An interactive chip that isn't only a link counts its finished clicks, toggles and removals in `data-commits` on its root, and a list item with an `OnClick` counts its finished clicks on its `li`
- A list item's `li` gets `data-static` on a static page and `data-ready` once it's interactive
- An interactive select's combobox gets `data-open` while its list is open
- The bUnit select driver's `ChooseAsync` and `UnchooseAsync` open the list before choosing and close it after, running `OpenChanged`, and refuse a read-only select, as the Playwright helper does
- An interactive splitter counts the collapses and restores it has made in `data-commits` on its root, once `CollapsedChanged` has run, and each separator names the pane that Enter on it collapses and restores in `data-toggles`
- An interactive accordion item's header and nav group's button count their toggles in `data-commits` once `ExpandedChanged` has run
- An interactive stepper's list counts the step clicks that move it, in `data-commits`, once `ActiveKeyChanged` has run
- An interactive carousel's picker counts the picks that move the slides, in `data-commits`, once `ActiveIndexChanged` has run
- A calendar's root counts the days picked in `data-commits`, once `ValueChanged` has run, as other fields' roots count their changes
- A file upload's input no longer carries `data-choices`; its field's root counts each choice in `data-commits` once `ValueChanged` has run
- An interactive hotkey's marker counts its presses in `data-commits` once `OnPress` has run
- An interactive link counts its clicks in `data-clicks` once `OnClick` has run, as a button does, and a link on a static page carries `data-static`
- An interactive autocomplete's list counts in `data-handled` each time someone types, chooses, leaves or clears the field, once the handlers, `OnTextChanged` included, have run
- A signature pad's root counts each stroke, undo, clear, typed name and switch between drawing and typing in `data-commits` once `ValueChanged` has run
- A slider's and range slider's root counts each move in `data-commits` once `ValueChanged` has run, as other fields' roots do, and each release of a thumb once `OnValueCommitted` has run
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
