# Trailblazor

Personal, MIT-licensed Blazor UI component library for .NET 10. The design spec is a Claude Doc and is the source of
truth: https://claude.ai/artifact/ApVjmrfSeiukXSfD59DCeT. Work follows its roadmap phase by phase; don't build
ahead of the current phase.

## Names

- Component prefix `Tb`: `TbButton`, `TbRoot`, `AddTrailblazor()`.
- CSS: custom properties `--tb-*`, classes `tb-*` (BEM modifiers), cascade layers `tb.*`, stable `data-part` hooks.
- Analyzer diagnostic IDs `TB001`, `TB002`, ...

## Clean-room rule

Trailblazor is written from scratch. The clean-room rule and the names it covers are in the untracked
`CLAUDE.local.md`; none of those names may appear in tracked files, commits, issues or pull requests.

## Conventions

- American English everywhere (color, behavior, gray).
- Code a typical .NET developer can read: no god classes, no clever abstractions, familiar idioms.
- Comments only for real constraints, one line each.
- Don't put one parameter per line; fill lines to about 120 characters.
- Test names: `SUT_Should<Expectation>_When<Condition>`, with `_WhenCalled` when unconditional. Never omit the
  When clause.
- Commit as often as useful, without asking. Commit messages are one short line with no Co-Authored-By or Claude
  attribution.
- Commits use `zacharydf7@gmail.com` (set in this repo's local git config, not the global work email).
- On Windows Git Bash, never name a shell variable `TMP` or `TEMP`.

## Independent review

The global review process applies, with one exception for this repo: the single follow-up round is not a hard
limit. Run further rounds of code-reviewer and slop-reviewer whenever another review is warranted, for example
after fixes that change behavior.

## Build and test

```shell
dotnet build
dotnet test
```

Warnings are errors. Package versions live in `Directory.Packages.props` (central package management).
