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

## Upcoming components

The docs site's navigation lists each component still to come under its category. `UpcomingCatalog.All` names each one
with its category and the issue that tracks it, and the issue sets its badge: "Coming soon" when the issue is in
progress or in the current or next phase milestone, "Planned" otherwise. Add the `in progress` label to an issue when
work on it starts and close it when the component ships; remove the component's entry from `All` in the change that adds
its page. The Docs workflow fetches the issues again on every publish; `docs/update-roadmap.sh` refreshes the committed
copy, `docs/Trailblazor.Docs.Examples/roadmap.json`.
