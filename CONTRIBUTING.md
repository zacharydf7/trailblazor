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

## Roadmap

The docs site's roadmap is made from the issues: their phase milestones, the `component` label and the `in progress`
label. Add `in progress` to an issue when work on it starts and close the issue when it ships. The Docs workflow
refreshes the roadmap on every publish; `docs/update-roadmap.sh` refreshes the copy in git.
