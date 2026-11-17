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

#### 2026-11-17

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
