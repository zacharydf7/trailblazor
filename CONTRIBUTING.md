# Contributing

## Definition of done

An issue is done when all of these hold:

- The change has bUnit tests and Playwright tests in chromium, firefox and webkit, and the new tests fail when the
  change is undone.
- `dotnet build` passes with warnings as errors, and `dotnet test` passes.
- The docs site is updated: a new or changed component, parameter or behavior has its page, interactive example, API
  table, accessibility notes and testing notes brought up to date, and the docs examples pass the axe checks.
- The change follows the conventions: American English, comments only for real constraints, test names
  `SUT_Should<Expectation>_When<Condition>`.

## Roadmap

The docs site's roadmap is made from the issues: their phase milestones, the `component` label and the `in progress`
label. Add `in progress` to an issue when work on it starts and close the issue when it ships. The Docs workflow
refreshes the roadmap on every publish; `docs/update-roadmap.sh` refreshes the copy in git.
