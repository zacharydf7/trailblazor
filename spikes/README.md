# Phase 0 spikes

Each spike answers one design question before the design is frozen. Its pass/fail criteria are written here before
any spike code exists, and its results are recorded next to them afterward. The phase 0 gate is all three spikes
meeting their criteria, or a failed criterion being written up with the fallback design it forces.

| Spike | Question |
| --- | --- |
| [Overlays](overlays/README.md) | Can native `<dialog>`, the Popover API and anchored positioning carry every overlay without a portal or z-index stack? |
| [Dock keep-alive host](dock-keep-alive/README.md) | Can docked panels move between groups without losing component state or re-rendering? |
| [Grid virtualization](grid-virtualization/README.md) | Can a Blazor data grid scroll 100,000 rows at 60 fps? |

## Where spikes run

Spike code lives in `spikes/Trailblazor.Spikes`, a Blazor Web App that serves each spike page in Interactive Server
and in WebAssembly. Measurement scripts are Playwright tests in `spikes/Trailblazor.Spikes.Tests`. Spike code is
throwaway: it is not packed, and whatever survives the gate moves into `src/` with real tests.

Spike measurements run locally, not in CI, because timing on shared CI runners is too noisy to gate on.

## Measurement environment

- **Correctness criteria** run on Chromium, Firefox and WebKit through Playwright.
- **Performance criteria** run on Chromium with CPU throttling at 4× (Chrome DevTools Protocol
  `Emulation.setCPUThrottlingRate`), to stand in for a mid-range laptop. Each result records the machine, browser
  version and render mode it was measured on.
- **Server latency**, where a criterion names it, is simulated with Chrome DevTools Protocol network conditions.
