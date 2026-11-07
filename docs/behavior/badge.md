# Badge

Pattern: none; it's a label with no behavior of its own

Familiar apps: the unread count on a mail app's folder or a phone's app icon

## Keyboard

Nothing of its own. A badge has no keys, and what it sits on keeps its own.

## Pointer

- [x] A click on the badge reaches the button under it. `BadgeTests.Badge_ShouldLetClicksReachTheButton_WhenThePointerIsOnTheBadge`
- [x] Hiding the badge leaves what it sits on. `BadgeTests.Badge_ShouldHideAndLeaveTheButton_WhenItIsNoLongerVisible`

## Touch

Nothing of its own. A tap passes through the badge as a click does.

## Focus

- [x] A badge is never a tab stop. `TbBadge.razor`
- [x] On a button, screen readers skip the badge and hear the button's name, or its `Label` when it has one. `TbBadgeTests.Render_ShouldHideTheBadgeFromScreenReaders_WhenItSitsOnAChildWithoutALabel`

## Not applicable

- Saying a new count aloud: the count belongs in the button's name, or the app announces it.
- A click on the badge doing anything itself: a chip is for that.
