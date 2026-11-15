# Form from a model

Pattern: none; each property is a labeled field of a form, and each group a `fieldset` named by its `legend`

Familiar apps: an admin site's generated edit page, a scaffolded record form

## Keyboard

- [x] Tab moves through the fields in the order they show, row by row, then on to the rest of the form. `ModelFieldsTests.Tab_ShouldVisitEachFieldInTheOrderItShows_WhenPressedThroughTheForm`
- [x] Each field is the same component used elsewhere, so it takes the same keys, such as the arrows in a number field or a select. `PropertyEditor.cs`

## Pointer

- [x] A change in any field sets the model's property. `TbModelFieldsTests.Fields_ShouldSetTheModel_WhenTheUserChangesAField`
- [x] A failed submit shows each annotation's message under its field, including an enum's list and a date or time that can't be empty, and focuses the first field with an error. `ModelFieldsTests.Submit_ShouldFocusTheFirstFieldWithAnError_WhenARequiredPropertyIsEmpty`, `TbModelFieldsTests.Fields_ShouldShowTheAnnotationsMessages_WhenTheFormIsSubmitted`, `TbModelFieldsTests.Fields_ShouldShowAndPointToAnAnnotationsMessage_WhenAFieldThatCantBindToItsPropertyFails`, `TbModelFieldsTests.Fields_ShouldClearAnAnnotationsMessage_WhenAnEnumFieldThatFailedIsFixed`

## Touch

- [x] Each field works by touch as it does on its own, and the fields stack in one column when the form is too narrow for two. `ModelFieldsTests.Fields_ShouldStackInOneColumn_WhenTheFormIsTooNarrowForTwo`

## Focus

- [x] Each field is labeled by its property's display name, or its name split into words, with its description as help text, and a `[Required]` one that can be left empty is marked required. `TbModelFieldsTests.Fields_ShouldFollowTheAnnotations_WhenTheyNameDescribeAndLimitAProperty`, `TbModelFieldsTests.Fields_ShouldShowTheAnnotationsMessages_WhenTheFormIsSubmitted`
- [x] A `TbModelField` changes a field's label, place, width or editor, or hides it. `TbModelFieldsTests.Fields_ShouldApplyAnOverride_WhenAModelFieldChangesALabelPlaceOrWidth`, `TbModelFieldsTests.Fields_ShouldDrawTheOverridesEditor_WhenAModelFieldGivesOne`

## Not applicable

- Keys of its own: the fields are a form's fields, which Tab moves through.
- Folding groups away: a form shows every field it asks for.
