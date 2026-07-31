# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and run

Everything targets `net10.0` and builds with the plain SDK; no workloads are needed.

```bash
dotnet build TSPHeuristics.sln -c Release
dotnet run --project Tests.cli -c Release          # exits 1 if anything failed
dotnet run --project WebApplication -c Release     # Blazor WASM dev server
```

**Output paths are per operating system.** `Directory.Build.props` sets
`BaseIntermediateOutputPath` and `BaseOutputPath` to `obj/$(OS)/` and `bin/$(OS)/`, so a build lands
in `bin/Unix/Release/net10.0` or `bin/Windows_NT/Release/net10.0`. This is not cosmetic: the
repository is often opened from two operating systems at once through a shared folder, and a single
`obj/` cannot hold both. The generated `obj/*.nuget.g.props` record an absolute `NuGetPackageRoot`
and use the host's path separator, so whichever side restored last leaves the other with imports it
cannot resolve — seen as a NuGet restore loop in Visual Studio, and as
`libhostpolicy.so not found` from `dotnet run` on Linux, because the Blazor DevServer targets fail
to import and the SDK falls back to executing the browser-wasm assembly directly. Do not remove the
props file, and do not hardcode `bin/Release/net10.0` anywhere.

`dotnet run --no-build` does not work for `WebApplication`; let it build.

### Tests

`Tests.cli` is a plain console program, not a test framework. `Main` calls five suites in sequence
(`MoveDeltas`, `ThreeOptReconnections`, `ApplicabilityGuards`, `GivenStartTour`, `ManualMoves`) and
there is no filter argument — to run one, comment out the others in `Main`. It covers the move
invariants: every move's `Delta` must equal the real change in tour length, all seven three-opt
reconnections must be reachable, `Best` must return the shortest, and the position guards must hold.

### Serving the app so another machine can reach it

The dev server binds to localhost by default. To reach it from outside the VM:

```bash
dotnet run --project WebApplication -c Release --urls "http://0.0.0.0:5199"
```

### Analysis pipeline

`analysis/` reproduces chapter 7 of the thesis and documents its own workflow in
`analysis/README.md`. `PerformanceAnalysis.cli` drives it and takes short/long flags via
`Utility.CommandLine.Arguments` — `-h|--heuristic` (required), plus `--metric geo|euc_2d|euclidean`
and `--selection greedy|proportional`, which exist so published numbers stay reproducible.

## Architecture

Three layers: `TravellingSalesmanProblem` (the heuristics, no UI dependency), `WebApplication`
(Blazor WebAssembly), and the CLIs (`PerformanceAnalysis.cli`, `GraphGenerator.cli`, `Tests.cli`).
`ChartData` holds the chart DTOs shared between core and UI.

### A run is a history, and that is the only contract

Every heuristic returns `LinkedList<GraphState>`. Everything downstream — the animation, the
transport controls, the tables, both canvases, the charts — reads that list and nothing else. This
is what lets `ManualSearch` exist: it is not a heuristic and decides nothing, it just returns a
history holding the starting tour and appends to it as moves are applied from outside. When adding a
mode, produce a history; do not add a parallel channel to the UI.

### Moves are objects, selection is separate from application

`ITourMove` knows its exact `Delta`, can `Describe()` itself as a rendered calculation, and can
`Apply()` itself to the tour it was built on. `NeighbourState` only *selects* one according to the
descent type. Keeping these apart is what allows a caller to name a specific move at specific
positions, which the manual mode needs.

`Delta` is exact. Tour cost is the exact euclidean length, so a delta computed from rounded
distances can disagree with the acceptance test on its own sign; `GetDistanceRounded` is for display
only.

### Distances and metrics

`Edge.Distance` is cached and invalidated when an endpoint is reassigned — it is read in the
innermost loop of every heuristic. `Edge.Metric` selects the TSPLIB edge weight
(`Euclidean`, `Euclidean2D`, `Geo`) and is **process wide**: an instance file fixes one metric for a whole run, so a CLI
sets it once at startup before any graph is built. Because distances are cached per edge, changing
it after graphs exist leaves those edges on the previous metric.

### The web shell

`MainLayout` is a pass-through; the page owns the shell. `AppShell` supplies three regions — topbar,
sidebar, workspace — and the layout lives in `wwwroot/css/shell.css`. The shell is pinned to `100vh`
with each region scrolling itself; `min-height: 0` on the flex children is load-bearing, because a
flex item defaults to `min-height: auto` and will not shrink below its content, letting overflow
escape.

Stylesheets load in order: `tokens.css` (custom properties only), `controls.css` (buttons, form
controls, tables), `shell.css` (layout), `app.css` (what is left). **There is no Bootstrap** — no
CSS, no JS, no icon font. Overlays and disclosure widgets are Blazor components with C# state
(`Help`, `Disclosure`, `ResultsDock`), and icons are inline SVG in `Icon.razor`.

Themes are token overrides under `:root[data-theme="light"]`. An inline script in `index.html` sets
the attribute before any stylesheet loads, so there is no flash on boot.

### The canvas

Three things are separate and easy to conflate:

- **Logical space** — `CanvasSettings.Width`/`Height`, in CSS pixels. All drawing code works here.
- **The buffer** — `BufferWidth`/`BufferHeight`, the logical box times a clamped
  `DevicePixelRatio`. The transform is pre-multiplied by that ratio at the start of each frame, which
  is why the renderer needs no knowledge of it. It must be re-applied per frame: assigning a canvas
  `width`/`height` resets the context state, transform included.
- **Scale** — derived, not an input. The graph is fitted to the measured stage and `Zoom` is applied
  on top. The stage is measured from JS (`ObserveCanvasStage`) with a `ResizeObserver` on
  `.canvas-area`, never on the canvas itself, which would be a feedback loop.

Canvas colours come from `--canvas-*` tokens, read through `GetCanvasColors` into `CanvasPalette`. A
canvas is painted rather than laid out, so it inherits nothing; adding a colour means adding it in
`tokens.css`, `GetCanvasColors` and `CanvasPalette` together. Do not branch drawing behaviour on a
colour string — `Brush.Dashed` exists because that was previously decided by comparing the colour to
`"red"`.

Drawing goes through one `Batch2D` per frame, flushed as a single JS interop call. `Context2D`
issues one round trip per call, so per-primitive drawing is thousands of round trips per frame.
Text measurement (`MeasureTextAsync`) needs a value back and therefore cannot happen inside a batch —
`GraphView` measures annotations first, in the font they will be drawn in.

## Conventions

**Comments describe the code, not its history.** Present tense, stating what the code does or the
constraint it satisfies. No accounts of what a function used to do, which bug a guard was written
for, or before-and-after measurements — that belongs in the commit message. Plain `/* */` blocks;
no XML doc comments (`/// <summary>`).

**Do not reference other projects or repositories in comments.** This project stands alone.

**Verify UI by looking at it.** DOM assertions are not sufficient and have produced false passes
here: a Help panel that rendered completely blank satisfied every `textContent` check because the
text was present but hidden, and table highlighting silently stopped when a stylesheet was removed
without anything erroring or 404ing. Take a screenshot and read it, or compare computed styles —
`getBoundingClientRect` reports an element's own box even when an ancestor clips it, and
`overflow: hidden` blocks user scrolling while still permitting programmatic scrolling, so both make
poor containment checks.

Chart.js is loaded from a CDN in `index.html` and raises "Chart is not defined" where that is
unreachable; browser tests stub `window.Chart` so its absence cannot mask a real failure.
