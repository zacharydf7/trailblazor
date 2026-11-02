# Contributing

## Definition of done

An issue is done when all of these hold:

- The change has bUnit tests and Playwright tests in chromium, firefox and webkit, and the new tests fail when the
  change is undone.
- `dotnet build` passes with warnings as errors, and `dotnet test` passes locally in every browser. CI runs the
  browser tests in chromium on each push to save minutes, and in all three every Monday or when run by hand;
  `TRAILBLAZOR_BROWSERS=chromium` narrows a local run the same way. If five minutes pass with no test finishing, the run
  stops and writes a hang dump and a `TestResults/*_hang.log` that lists the tests still running.
- The docs site is updated: a new or changed component, parameter or behavior has its page, interactive example, API
  table, accessibility notes and testing notes brought up to date, and the docs examples pass the axe checks.
- A docs review reads the component's page and API page against the [docs checklist](#docs-checklist), alongside the
  code and prose reviews, and every item is ticked or marked "n/a" with a reason.
- The change follows the conventions: American English, comments only for real constraints, test names
  `SUT_Should<Expectation>_When<Condition>`.
- A change to the public API is listed in its project's `PublicAPI.Unshipped.txt`. The build flags each public member
  that isn't listed (RS0016), and the IDE's code fix adds it; a release moves the lines to `PublicAPI.Shipped.txt`.

## Component pages

Each component's page comes from its entry in `ExampleCatalog.All`. A component with several features gives its entry
`Sections`, one per feature, each with an id, a title, a sentence or two and its own example component. The page shows
each section as a heading, its demo and its code on request, and lists the sections under "On this page". An entry
without sections shows its single example under the heading "Example". Demos sit centered in their frame; set
`FullWidth` on an entry whose demos need the frame's width, such as a grid or a layout component.

## Docs checklist

A docs reviewer reads the component's page and API page against this list before the issue closes. Tick an item, or
mark it "n/a" with a reason (a layout component has no keys, for example). Some items need page and API slots that
#264 and #266 add; until those land, check what the page can already show.

### Overview

- [ ] The summary says in one sentence what the component is for, in words a reader without the code would follow.
- [ ] **When to use** and **When not to use** are stated. Each look-alike component is named and linked with the
      reason to pick it instead ("Use an autocomplete when ..."), and related components are linked.

### Examples

- [ ] Each feature has its own section: a heading, a sentence or two on what it does and when to use it, and a demo
      of that feature alone. No section stacks unrelated demos.
- [ ] Every main parameter (the ones a typical app sets) is used in at least one section, and every parameter whose
      effect isn't obvious from its name has a demo.
- [ ] The edge cases that apply are shown, not only described: empty, loading, error or failed load, invalid, disabled,
      read-only, long content (a long label, value or option wrapping or truncating), and many items.
- [ ] An input shows its invalid state without the reader having to trigger it, and a readout shows the bound value
      (its type, and what clearing gives).
- [ ] At least one section shows the component in a realistic composition (in a form, a dialog, a toolbar or beside
      the components it's used with).
- [ ] Example code is what a reader should copy: no `data-testid`, no outputs only tests read, layout through `TbStack`,
      `TbGrid` or `tb-*` classes rather than inline styles, and every field labeled. Labels and messages are realistic.
- [ ] Text that refers to a demo names its section.

### API

- [ ] Every public parameter, event, content slot and method has a description that says what it does, not just its
      type, and states the unit, range or format where one applies.
- [ ] Enum values and defaults are visible, and required parameters are marked.
- [ ] A component made of parts has an anatomy showing how the parts nest, and each `data-part` is described.

### Accessibility

- [ ] Every key the component handles is in a keyboard table, by mode or state where they differ, with keys that swap
      in right-to-left layouts noted.
- [ ] Labeling says how the component gets its accessible name, what happens without one, and when a hidden label
      (`AriaLabel`) is acceptable.
- [ ] What screen readers announce is stated: on focus, on change, on error and for live updates.
- [ ] What the app must still do (name it, choose heading levels, return focus, word messages) is listed apart from
      what the component does, and the WAI-ARIA pattern it follows is linked.
- [ ] Forced colors, reduced motion and touch or single-pointer alternatives are covered where they apply.
- [ ] The page says what renders before it's interactive, what already works then, and what a static form posts.
- [ ] The Accessibility section holds only accessibility: feature behavior, styling and caveats are in their sections
      or in Notes.

### Testing, telemetry and styling

- [ ] Testing gives the role-based locator, the `Trailblazor.Testing` helpers for bUnit and Playwright where they
      exist, and any wait the component needs, as short items rather than one paragraph.
- [ ] Telemetry says what a `TelemetryName` makes the component count and when, and that values are never recorded.
- [ ] Design tokens list what the component reads, and the page says how to restyle one instance (the `Classes`
      parameter, parts and state attributes) or links to where that is explained.

### Writing

- [ ] American English, short sentences, one idea per note, and no bullet longer than about four lines.
- [ ] Terms, section names and order match the other component pages; no time-bound wording ("for now", "currently").
- [ ] Every statement matches the code: parameter names, defaults, keys and announcements were checked against the
      component and its tests, and every demo and link works on a phone at 390 px and in both themes.

## Upcoming components

The docs site's navigation lists each component still to come under its category. `UpcomingCatalog.All` names each one
with its category and the issue that tracks it, and the issue sets its badge: "Coming soon" when the issue is in
progress or in the current or next phase milestone, "Planned" otherwise. Add the `in progress` label to an issue when
work on it starts and close it when the component ships; remove the component's entry from `All` in the change that adds
its page. The Docs workflow fetches the issues again on every publish; `docs/update-roadmap.sh` refreshes the committed
copy, `docs/Trailblazor.Docs.Examples/roadmap.json`.
