# Behavior checklists

Each file here lists what one component should do with the keyboard, the pointer, touch and focus: what the matching
WAI-ARIA Authoring Practices pattern asks for, and what the same control does in the apps people already know. A tooltip
that stays open after a click isn't a missing feature, but it is a gap in behavior people expect, and a checklist finds
gaps like that before a user does.

Write the checklist before building a new component, and keep it up to date as the component changes. A component
with no behavior of its own, such as a layout helper, still has a file that says so.

## Format

Name the file after the component's catalog name, such as `tree-view.md`.

```markdown
# Tree view

Pattern: [Tree view](https://www.w3.org/WAI/ARIA/apg/patterns/treeview/)

Familiar apps: File Explorer, VS Code's file tree

## Keyboard

- [x] Down moves to the next visible item. `TreeViewTests.Tree_ShouldMoveFocus_WhenArrowKeysArePressed`
- [ ] Typing a letter jumps to the next item that starts with it. #123

## Pointer

## Touch

## Focus

## Not applicable

- Context menus: the tree has no menu of its own, and an app can attach one through a row's template or attributes.
```

- `[x]` means the component does it. Name a test that shows it, or the source file when only the code shows it.
- `[ ]` means it doesn't, and links the issue that tracks the gap.
- `[ ]` with `UNCONFIRMED` means reading the code couldn't settle it: it needs a browser run or a real touch device.
  #332 lists these items. Tick one or link an issue once someone has tried it.
- A section with nothing to check says "Nothing of its own." and why, in a few words.
- `Not applicable` records what the pattern or familiar apps do that this component deliberately doesn't, and why.
- With no matching pattern or familiar app, write `none;` and a few words on why.
- What screen readers hear goes under Focus.
