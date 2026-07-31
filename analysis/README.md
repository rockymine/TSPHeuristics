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

`results/fixed` is the current code. `results/prefix` is the same machine running `387eb5a`, the
tip of the branch before any of the fixes, and `results/baseline` is `d8c0488`, an earlier commit
measured first. The two pre-fix runs agree within noise - 69.659 s against 74.058 s for the
slowest `eil76` configuration - so either can serve as the "before" column; `prefix` is the exact
predecessor and is the one the report uses. Having a pre-fix run at all is what separates "the
machine got faster" from "the code got faster": of the measured speedup, a factor of 5.7 is the
machine and 452 is the code.

`results/fixed/selection.json` compares the two Ant System transition rules over four budgets
(see below).

## Instance format and the starting tour

```
n,index,x,y          a node
e,index,index,value  an edge (optional; the complete graph is built anyway)
t,index,index,…      the starting tour, in visiting order (optional)
```

Simulated Annealing and Hill Climbing improve a tour, so they need one to start from, and they have
always taken it from the order the nodes are listed in - implicitly, unchecked, and with no way to
state a different order for the same points. A `t` line makes it explicit. It may be written open or
closed (`t,0,2,1,3` and `t,0,2,1,3,0` are the same cycle) and is rejected with a specific message if
it is not a permutation of the nodes. Without the line the node order is used exactly as before, so
every existing instance file is unaffected. `SerializeGraph` writes the line back, so "Download
Source" round trips it.

This matters for exercises: a task usually hands out both the points and a tour to improve, and the
tour is half of it.

## Moves

A move - a swap, a 2-opt exchange, a 3-opt reconnection, a double bridge - is an object
(`ITourMove`) that knows its exact `Delta`, can `Describe()` itself as a rendered calculation, and
can `Apply()` itself to the tour it was built on. `NeighbourState` only *selects* one according to
the descent type; it no longer applies it as well.

Before, selection and application were the same call, so the positions never left the method and
nothing outside could ask for a specific move. Three-opt had no move type at all: it drew three cut
points, scored the seven reconnections and applied the winner inside one static method. The seven
are now a `ThreeOptReconnection` enum, and `ThreeOptMove.Best` is the policy that ranks them - the
same ranking as before, so the tours are unchanged.

`Tests.cli` pins this down: it walks every combination of cut points on a small tour and checks that
each move's delta equals the actual change in tour length, that all seven reconnections are
reachable, and that the position guards hold. Run it with `dotnet run --project Tests.cli`.

## Manual search

`ManualSearch` is what the move objects were for. It is not a heuristic and decides nothing: its
`FindPath` returns a history holding only the starting tour, and `Apply` extends it with a move
chosen from outside. Everything downstream - the animation, the tables, the canvases - reads the
same `LinkedList<GraphState>` it always did; only the producer differs.

In the page it is the "Manual Search" heuristic. Pick a move type, pick the nodes it applies to, and
the panel states what the move would cost before it is applied, so a calculation done on paper can
be checked against it. Move types can be chained in any order - a swap, then a 3-opt reconnection,
then a double bridge - which is why the move history table takes the rule from each row rather than
from a run level setting, and why the distance matrix is on screen next to it.

Applying a move to a state that is not the last one drops the entries after it. A history that kept
both would no longer match what is drawn, and the point of stepping back is to try a different line
of play, not to accumulate abandoned ones.

Nodes are chosen by their own index rather than by their position in the tour, because that is how
an exercise is written down - "swap 3 and 7", not "swap the second and the fifth entry". A
combination the move cannot be built from is refused with the rule that forbids it: a swap needs the
neighbours on both sides, so the first and last node of the tour cannot take part, while a 2-opt
move only needs a successor and can start at the first.

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

## What the web application can handle now

The old ceiling of 90 nodes, 50 iterations and one ant per node was set by what the page survived,
not by the heuristics. Two things kept it there, and both are fixed:

- the full distance matrix behind "Show Full Table" was part of every render whether the dialog was
  open or not - 40 382 cells at 200 nodes, each one through a markdown parser. Clicking Execute for
  a Nearest Neighbour run whose `FindPath` took 56 ms froze the page for 310 s. The dialog now
  builds its table when it is opened and drops it when it is closed, and cells without LaTeX skip
  the parser: the same run is 1.2 s, and opening the dialog at 90 nodes costs 1.7 s.
- the canvas is sized `scale x coordinate range`, so a wide range asked the browser for a buffer
  hundreds of megabytes large. It is capped at 3000 px per edge, which shrinks the drawing instead
  of clipping it.

Measured in the browser afterwards, click to responsive page:

| | 150 nodes | 200 nodes | 300 nodes |
| --- | --- | --- | --- |
| Nearest Neighbour | 1.9 s | 1.5 s | 2.9 s |
| Simulated Annealing, phase length 10 | 4.0 s | 4.7 s | 7.2 s |
| Hill Climbing, 2-opt, steepest | 3.3 s | 4.8 s | 12.7 s |
| Hill Climbing, swap, steepest | 5.5 s | 10.6 s | 36.6 s |
| Ant System, 50 ants, 10 iterations | 3.8 s | 22.5 s | 31.2 s |

The new limits follow those numbers: 300 nodes, a coordinate range of 300, and up to 500 ants. The
colony is no longer tied to the node count - under the greedy rule two ants from the same start
walked the same tour, so more ants than nodes were pointless, but under the proportional rule they
diverge. Ants cost 0.0047 s each at 100 nodes and carry no memory of their own.

Iterations are capped at 500 but additionally against the instance, because each iteration keeps
its own pheromone snapshot - one `Edge` per edge of the complete graph, about 95 bytes. Measured:
200 nodes at 100 iterations holds 167 MB, and the formula predicts 189 MB. The budget is 250 MB,
which leaves the full 500 iterations up to about 130 nodes and 61 at 300. Entering more snaps the
field to the ceiling rather than discarding the input.

## Ant System: which transition rule

The Ant System picks the next node with a probability proportional to its pheromone-visibility
weight. This implementation always took the largest weight instead, which makes an ant's tour a
function of its starting node alone: a colony of *m* ants explores exactly *m* tours, and further
iterations only move pheromone around. That is the reason table 7.8 shows almost no benefit from
doubling the colony, and it is visible in `selection.json` - greedy `eil51` sits at 464 (8.9 %
above the optimum) at 10 ants/20 iterations and is still at 464 at 50 ants/300 iterations.

Both rules are available; `AntSystem.SelectionRule` and the "State Transition Rule" control select
between them, and `PerformanceAnalysis.cli --selection greedy|proportional` does the same. The
tables of chapter 7 were produced with `greedy`, which is what `run_analysis.py` passes for the
`as` grid so those numbers stay reproducible.

Mean tour length over 5 runs, as a percentage above the known optimum:

| Budget | | burma14 | ulysses16 | eil51 | berlin52 | st70 | eil76 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 10 ants, 20 it. | greedy | 1.7 | 0.7 | **8.9** | **15.3** | **18.4** | **13.2** |
| | proportional | **1.5** | 1.5 | 26.9 | 27.4 | 36.1 | 38.3 |
| 25 ants, 50 it. | greedy | 1.7 | 0.7 | 8.9 | 13.2 | **13.2** | **15.2** |
| | proportional | **0.3** | **0.4** | **6.2** | **8.4** | 15.1 | 18.6 |
| 50 ants, 100 it. | greedy | 1.7 | 0.7 | 8.9 | 6.9 | 13.2 | 10.0 |
| | proportional | **0.2** | **0.0** | **3.6** | **4.5** | **7.4** | **6.8** |
| 50 ants, 300 it. | greedy | 1.7 | 0.7 | 8.9 | 6.9 | 13.2 | 10.0 |
| | proportional | **0.3** | **0.3** | **4.8** | **5.4** | **6.4** | **5.6** |

Greedy is the better choice at the small budgets the thesis used - it exploits immediately, while
the proportional rule is still exploring. From roughly 25 ants and 50 iterations onwards that
reverses, and only the proportional rule ever reaches an optimum (`burma14` 3323, `ulysses16`
6859). The greedy column stops improving entirely.

## Reproducing

```bash
# .NET 6 is out of support; the SDK 8 build runs on the 8.0 runtime via roll-forward.
export DOTNET_ROLL_FORWARD=Major
dotnet build -c Release

python3 convert.py                       # only needed to rebuild the instance files
python3 run_analysis.py <cli-bin-dir> fixed all      # hc, sa, as, selection, timings
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
