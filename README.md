<img src="assets/icon.svg" alt="" width="72" height="72">

# Trailblazor

An MIT-licensed Blazor UI component library for .NET 10: accessible by default, styled with design tokens and
cascade layers, and built on the browser's native `<dialog>`, Popover API and anchor positioning.

## Why Trailblazor

- **Everything an app needs, with nothing given up.** Fields, pickers and file uploads, a data grid that sorts, pages
  and selects, a virtualized tree view, dialogs, menus, popovers, tabs, toasts and more, each accessible and themed,
  with fields and forms that work before the page is interactive.
- **Easy to test.** `Trailblazor.Testing` sets up bUnit with fakes for the browser, time and JavaScript, and
  `Trailblazor.Testing.Playwright` drives components by their labels, the way users do:
  `await page.Tb().Select("Size").ChooseAsync("Medium")`.
- **Built-in telemetry.** Label a control with `TelemetryName` and the components count how often people see it and
  use it. The counts go through OpenTelemetry to the dashboard you already have, so features that are shown but never
  used stand out. Nothing is recorded until the app listens, and nothing about the user is recorded at all.

Trailblazor is in development (phase 2: parity with the components a large app needs). There are no packages yet. Try
the components on the documentation site: https://zacharydf7.github.io/trailblazor/. For a coding assistant, every page
is also published as Markdown, listed in https://zacharydf7.github.io/trailblazor/llms.txt (or all in one file,
llms-full.txt).

## Repository layout

```text
src/
  Trailblazor/               styled components, CSS, JS module
  Trailblazor.Primitives/    headless behaviors (focus scope, dismissable layer, anchored positioning)
  Trailblazor.Analyzers/     build warnings for unnamed controls and fields, untitled dialogs and stuck values
  Trailblazor.Icons.Material/  Google's Material Symbols as icons for TbIcon (paths under Apache 2.0)
  Trailblazor.Testing/       bUnit setup for consumers: this.AddTrailblazor()
  Trailblazor.Testing.Playwright/  Playwright helpers for consumers: page.Tb().Select("Size").ChooseAsync("Medium")
docs/Trailblazor.Docs/       WebAssembly docs site
tools/Trailblazor.Icons.Generator/  writes the icon pack's classes from the Material Symbols npm package
tests/
  Trailblazor.Tests/         bUnit unit tests
  Trailblazor.Analyzers.Tests/  analyzer tests, on generated code and on a real Razor build
  Trailblazor.BrowserTests/  Playwright browser tests
```

## Building

Requires the .NET 10 SDK.

```shell
dotnet build
dotnet test
```

## License

[MIT](LICENSE)
