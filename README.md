# Trailblazor

An MIT-licensed Blazor UI component library for .NET 10: accessible by default, styled with design tokens and
cascade layers, and built on the browser's native `<dialog>`, Popover API and anchor positioning.

Trailblazor is in early development (phase 0: foundations and design spikes). There are no packages yet.

## Repository layout

```text
src/
  Trailblazor/               styled components, CSS, JS module
  Trailblazor.Primitives/    headless behaviors (focus scope, dismissable layer, anchored positioning)
  Trailblazor.Analyzers/     Roslyn analyzers and code fixes
  Trailblazor.Testing/       bUnit and Playwright helpers for consumers
docs/Trailblazor.Docs/       WebAssembly docs site
tests/
  Trailblazor.Tests/         bUnit unit tests
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
