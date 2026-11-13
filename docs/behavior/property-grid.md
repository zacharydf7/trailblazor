# Property grid

Pattern: none; each property is a labeled field, and each group a [disclosure](https://www.w3.org/WAI/ARIA/apg/patterns/disclosure/)

Familiar apps: Visual Studio's and Figma's properties panels, a game engine's inspector

## Keyboard

- [x] Tab moves through the editors and the group headings, in the order the properties come. `PropertyGridTests.Tab_ShouldVisitEachEditorAndGroupHeadingInOrder_WhenPressedThroughTheGrid`
- [x] Enter or Space on a group heading folds the group away and opens it again. `PropertyGridTests.Group_ShouldFoldAwayAndOpenAgain_WhenItsHeadingIsPressedWithTheKeyboard`
- [x] Each editor is the same field component used elsewhere, so it takes the same keys, such as the arrows in a number field or a select. `TbPropertyGrid.cs`

## Pointer

- [x] A change in any editor sets the property and raises `OnChange` with it. `TbPropertyGridTests.Change_ShouldSetThePropertyAndRaiseOnChange_WhenTheUserEditsIt`
- [x] A property that can't be set, an init-only one, a `[Flags]` enum, or a type with no editor shows read-only. `TbPropertyGridTests.Render_ShouldMakeThePropertyReadOnly_WhenItHasNoSetterIsMarkedReadOnlyOrHasNoEditor`
- [x] Clearing an editor whose property can't be empty leaves the property as it was. `TbPropertyGridTests.Change_ShouldSetADateButLeaveIt_WhenAnEditorForAValueThatCantBeEmptyIsCleared`

## Touch

- [x] Each editor works by touch as it does on its own. `TbPropertyGrid.cs`

## Focus

- [x] Each editor is labeled by its property's display name, or its name split into words, with its description as help text. `TbPropertyGridTests.Render_ShouldListEachVisiblePropertyWithAnEditorForItsTypeUnderItsGroup_WhenGivenAModel`

## Not applicable

- Arrow keys between rows, as in a grid: the properties are a form of fields, which Tab moves through.
- Searching the properties: a grid lists one object's properties, usually few enough to scan.
