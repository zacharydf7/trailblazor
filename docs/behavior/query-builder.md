# Query builder

Pattern: none; a form of labeled fields in nested `group`s, with buttons that add and remove conditions and groups

Familiar apps: Jira's advanced search builder, Notion's and Airtable's filter groups, Outlook's search rules

## Keyboard

- [x] Tab moves through each group's Match list, its conditions' fields and remove buttons, and its Add buttons, in order. `TbQueryBuilder.razor`
- [x] Enter on Add condition adds a condition and moves focus to its Field list. `QueryBuilderTests.Focus_ShouldFollowAddedAndRemovedConditions_WhenTheButtonsArePressedWithTheKeyboard`
- [x] Enter on a condition's remove button removes it and moves focus to its group's Add condition button. `QueryBuilderTests.Focus_ShouldFollowAddedAndRemovedConditions_WhenTheButtonsArePressedWithTheKeyboard`
- [x] Each field takes the keys it takes anywhere else, since it is the same select, number field, date picker or text field. `TbQueryBuilder.razor`

## Pointer

- [x] A condition filters the items once its field, comparison and value are filled in. `QueryBuilderTests.Condition_ShouldFilterTheItems_WhenItsFieldComparisonAndValueAreFilledIn`
- [x] A group nests with its own join, so a query can match all of some conditions and any of others. `QueryBuilderTests.Group_ShouldJoinItsConditionsItsOwnWay_WhenNestedInTheQuery`
- [x] Each field offers the comparisons that suit it: text ones for text, Before and After for a date, and Is any of for a choice. `TbQueryBuilderTests.Condition_ShouldOfferDayComparisonsAndAChoiceList_WhenItsFieldIsADateOrAChoice`
- [x] A condition drops its value when its field changes or its comparison takes none. `TbQueryBuilderTests.Condition_ShouldDropItsValue_WhenItsFieldChangesOrItsComparisonTakesNone`
- [x] Groups nest no deeper than `MaxDepth`. `TbQueryBuilderTests.AddGroupAsync_ShouldThrow_WhenTheGroupIsAtTheDeepestLevel`
- [x] A query read back from JSON shows as it was saved. `TbQueryBuilderTests.Value_ShouldShowAQueryReadBackFromJson_WhenTheParentSetsIt`

## Touch

- [x] Each button and field works by touch as it does on its own. `TbQueryBuilder.razor`

## Focus

- [x] A new group's Match list takes focus. `TbQueryBuilder.razor`
- [x] Removing a group moves focus to its parent group's Add condition button. `TbQueryBuilderTests.Remove_ShouldTakeOutTheConditionOrGroupAndFocusItsGroupsAddButton_WhenClicked`

## Not applicable

- Dragging conditions between groups: a condition is quick to remove and add again, and the keyboard reaches every button.
- Free-text query syntax: the builder writes the query from fields, so nothing has to be typed in a query language.
