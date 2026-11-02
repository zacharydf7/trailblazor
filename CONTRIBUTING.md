# Contributing

## Definition of done

An issue is done when all of these hold:

- The change has bUnit tests and Playwright tests in chromium, firefox and webkit, and the new tests fail when the
  change is undone.
- `dotnet build` passes with warnings as errors, and `dotnet test` passes locally in every browser. CI runs the
  browser tests in chromium on each push to save minutes, and in all three every Monday or when run by hand;
  `TRAILBLAZOR_BROWSERS=chromium` narrows a local run the same way. Five minutes with no test finishing ends the run,
  with a dump and a `TestResults/*_hang.log` that lists the tests still running.
- The docs site is updated: a new or changed component, parameter or behavior has its page, interactive example, API
  table, accessibility notes and testing notes brought up to date, and the docs examples pass the axe checks.
- The change follows the conventions: American English, comments only for real constraints, test names
  `SUT_Should<Expectation>_When<Condition>`.
- A change to the public API is listed in its project's `PublicAPI.Unshipped.txt`. The build names each public member
  that isn't (RS0016), and the IDE's fix adds it; a release moves the lines to `PublicAPI.Shipped.txt`.

## Upcoming components

The docs site's navigation lists each component still to come under its category. `UpcomingCatalog.All` names each
one with its category and the issue that tracks it, and the issue sets its badge: "Coming soon" when the issue is in
progress or in the current or next phase milestone, "Planned" otherwise. Add `in progress` to an issue when work on it
starts and close it when the component ships; remove the component's entry from `All` in the change that adds its page.
The Docs workflow fetches the issues again on every publish; `docs/update-roadmap.sh` refreshes the copy in git.
