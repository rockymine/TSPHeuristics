# Re-run of the chapter 7 performance analysis

Everything needed to reproduce the runtime and solution-quality tables of chapter 7, plus the
numbers produced by re-running them on the current code.

## What is here

| Path | Purpose |
| --- | --- |
| `instances/` | The six TSPLIB problems of table 7.1 and `random10`..`random80`, in the project's node format |
| `convert.py` | Rebuilds `instances/*.txt` from the original TSPLIB `.tsp` files |
| `run_analysis.py` | Drives `PerformanceAnalysis.cli` over the full parameter grid |
| `web_timings.js` | Measures the same configurations inside the browser via Playwright |
| `thesis_values.py` | The 2020 numbers, transcribed from tables 7.3, 7.4, 7.6, 7.7, 7.8 and 7.9 |
| `report.py` | Builds the comparison tables |
| `report.txt` | The generated comparison |
| `results/` | Raw `SimulationResult` aggregates per run |
| `web_timings*.json`, `sa_mintemp8.json` | Browser runtimes and the matching script runtimes |

`results/fixed` is the current code; `results/baseline` is the same machine running commit
`d8c0488`, i.e. the code as it stood for the thesis. Having both separates "the machine got
faster" from "the algorithms got faster".

## Distance metrics

TSPLIB fixes an edge weight type per instance, and the analysis depends on it: `burma14` and
`ulysses16` are `GEO`, the other four are `EUC_2D`. `Edge.CalcGeoDistance` and
`Edge.CalcDistance2D` implemented both but nothing ever called them - `Edge.Distance` was always
the plain euclidean length. `Edge.Metric` now selects between them and
`PerformanceAnalysis.cli --metric geo|euc_2d|euclidean` sets it. The default is unchanged, so the
web application still behaves exactly as before.

The metrics check out against the published optima: the optimal `burma14` tour quoted in the
thesis measures 3323 under GEO, `berlin52` measures 7542 and `eil51` measures 426 under EUC_2D.
Under plain euclidean the same tours measure 30.878, 7544.37 and 429.12.

## Reproducing

```bash
# .NET 6 is out of support; the SDK 8 build runs on the 8.0 runtime via roll-forward.
export DOTNET_ROLL_FORWARD=Major
dotnet build -c Release

python3 convert.py                       # only needed to rebuild the instance files
python3 run_analysis.py <cli-bin-dir> fixed all
python3 report.py

# Browser runtimes: serve the app first, then
node web_timings.js 5199 web_timings.json
```

`run_analysis.py` runs strictly sequentially - the runtimes are the measurement, so parallel
execution would distort them.

## Caveats

- The thesis does not state the coordinate range used for `random10`..`random80`; these were
  regenerated at 100x100. Only the runtime comparison uses them, never solution quality.
- Phase lengths 1 and 5 for Simulated Annealing live in appendix B of the thesis, which was not
  part of the supplied excerpt, so those rows have no 2020 column.
- The web page cannot be set below `MinTemp` 1e-7 and defaults to 1e-8, while the script used the
  thesis value 1e-9. The 2020 web-vs-script factor therefore compared two different amounts of
  work; `sa_mintemp8.json` holds script runtimes at 1e-8 so the factor is like for like.
- Chart.js is loaded from a CDN. Where that is unreachable the page raises "Chart is not defined";
  `web_timings.js` stubs it so its absence cannot mask a real failure.
