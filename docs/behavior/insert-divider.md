# Insert divider

Pattern: its insert button follows [Button](https://www.w3.org/WAI/ARIA/apg/patterns/button/)

Familiar apps: the add-block button in Notion, the insert handles between rows in Confluence tables

## Keyboard

- [x] Tab reaches each divider's button, shown or not. `trailblazor.css`
- [x] Enter presses the button. `InsertDividerTests.InsertDivider_ShouldAddTheItemInItsPlace_WhenItsButtonIsPressed`
- [x] Space presses the button. `TbInsertDivider.razor`

## Pointer

- [x] Resting on the line shows the line and button, and leaving hides them. `InsertDividerTests.InsertDivider_ShouldShowItsButtonOnlyOnHoverOrFocus_WhenThePointerCanHover`
- [x] A click asks the app to insert here. `TbInsertDividerTests.Click_ShouldAskToInsert_WhenTheButtonIsChosen`
- [x] The divider keeps its height shown or hidden, so the list doesn't jump. `InsertDividerTests.InsertDivider_ShouldCenterItsLineOnTheButton_WhenShown`
- [ ] After a click, the line and button hide once the pointer leaves. #312
- [ ] A disabled divider's button stays hidden at rest and looks off when the line is hovered. #312

## Touch

- [x] Where the pointer can't hover, the button stays in view. `InsertDividerTests.InsertDivider_ShouldKeepItsButtonInView_WhenThePointerCannotHover`
- [x] A tap presses it through the same click a mouse sends, at a target of at least 26px. `TbInsertDivider.razor`

## Focus

- [x] Keyboard focus shows the line and button. `InsertDividerTests.InsertDivider_ShouldShowItsButtonOnlyOnHoverOrFocus_WhenThePointerCanHover`
- [x] A disabled divider's button is out of the tab order. `TbInsertDividerTests.Render_ShouldTurnTheButtonOff_WhenDisabled`

## Not applicable

- Moving focus to the new item: the app adds it, and only the app knows where it lands.
- A tooltip: a plus on a line says what it does, and `Label` gives screen readers the place.
- One tab stop across the dividers: each is a separate button, and the docs steer long lists to a single button instead.
