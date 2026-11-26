using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TravellingSalesmanProblem.Algorithms;
using TravellingSalesmanProblem.Graph;

namespace WebApplication.Utils {
    /* One figure of the tile row above the canvas: a plain label, the lecture's symbol, the value,
     * and a tone that colours it (good, bad, warn, accent). */
    public record StepTile(string Label, string Symbol, string Value, string Tone = null, string Hint = null);

    /* A chip of the canvas legend in the player: what a colour or line means for this heuristic. */
    public record LegendItem(string Kind, string Label);

    /* What the tile row and the player show for a step, per heuristic. It replaces the line of
     * "Key: value" pairs that used to sit above the canvases and wrapped over several lines; the
     * full list is still in the player's Step details. The choice of figures follows the readout
     * inventory: the values a student follows from step to step, never the tour order. */
    public static class StepView {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Len(double value) =>
            double.IsFinite(value) ? Math.Round(value, 2).ToString("0.00", Inv) : "–";

        /* Signed with a real minus sign. */
        public static string Signed(double value) {
            var v = Math.Round(value, 2) + 0d;
            return (v > 0 ? "+" : v < 0 ? "−" : "") + Math.Abs(v).ToString("0.00", Inv);
        }

        /* A share to one decimal, signed: "−9.7 %". */
        private static string Percent(double value) {
            var v = Math.Round(value, 1) + 0d;
            return (v > 0 ? "+" : v < 0 ? "\u2212" : "") + Math.Abs(v).ToString("0.0", Inv) + " %";
        }

        private static string Count(int value) => value.ToString("N0", Inv);

        private static double Parse(GraphState state, string key) =>
            state.Messages.TryGetValue(key, out var text) &&
            double.TryParse(text, NumberStyles.Float, Inv, out var value) ? value : double.NaN;

        /* The tiles for a step; with no run yet, the same labels with dashes. */
        public static List<StepTile> Tiles(AlgorithmEnum algorithm, bool multiStart, LinkedListNode<GraphState> node) {
            var dash = "–";
            var s = node?.Value;
            var p = node?.Previous?.Value;
            var last = node != null && node.Next == null;

            switch (algorithm) {
                case AlgorithmEnum.SA: {
                    var move = s?.MoveInfo;
                    var best = s == null ? double.NaN : Parse(s, "Best");
                    var newBest = p != null && best < Parse(p, "Best") - 1e-9;
                    var (word, tone) = s?.Outcome switch {
                        DrawOutcome.Shorter => newBest ? ("New best", "accent") : ("Taken", "good"),
                        DrawOutcome.LongerAccepted => ("Taken", "warn"),
                        DrawOutcome.LongerRejected => ("Rejected", "bad"),
                        _ => s == null ? (dash, null) : last && p != null ? ("Finished", "accent") : ("Start", null)
                    };
                    return new() {
                        new("Temperature", "T", s == null ? dash : s.Temperature.ToString("G4", Inv)),
                        new("Tour length", "f(x)", s == null ? dash : Len(s.Distance)),
                        new("Change", "Δ", move == null ? dash : Signed(move.Delta), move == null ? null : move.Delta < 0 ? "good" : "bad",
                            move == null ? null : Derivations.MoveContext(move)),
                        new("Decision", null, word, tone),
                        new("Best so far", "f(x*)", s == null ? dash : Len(best))
                    };
                }

                case AlgorithmEnum.NN when !multiStart: {
                    var n = s?.Nodes.Count ?? 0;
                    var closed = s != null && s.Path.Count > n;
                    var hasEdge = s != null && p != null && s.Path.Count >= 2;
                    var a = hasEdge ? s.Path[^2].Index : 0;
                    var b = hasEdge ? s.Path[^1].Index : 0;
                    return new() {
                        new("Start node", null, s == null || s.Path.Count == 0 ? dash : s.Path[0].Index.ToString()),
                        new(closed ? "Back to the start" : "Added node", null, hasEdge ? b.ToString() : dash),
                        new("Edge length", hasEdge ? $"d({a}, {b})" : "d", hasEdge ? Len(s.Distance - p.Distance) : dash),
                        new("Visited", null, s == null ? dash : $"{Math.Min(s.Path.Count, n)} of {n}"),
                        new("Length so far", "f(x)", s == null ? dash : Len(s.Distance))
                    };
                }

                case AlgorithmEnum.HC when !multiStart: {
                    var move = s?.MoveInfo;
                    var start = node?.List?.First?.Next?.Value.Distance ?? double.NaN;
                    start = node?.List?.First?.Value is { } first && double.IsFinite(first.Distance) ? first.Distance : start;
                    var gain = s != null && double.IsFinite(start) && start > 0 ? (s.Distance - start) / start * 100 : double.NaN;
                    return new() {
                        new("Move", null, move == null ? dash : Derivations.MoveContext(move)),
                        new("Change", "Δ", move == null ? dash : Signed(move.Delta), move == null ? null : "good"),
                        new("Tour length", "f(x)", s == null || !double.IsFinite(s.Distance) ? dash : Len(s.Distance)),
                        new("Gain since the start", null, double.IsFinite(gain) && p != null ? Percent(gain) : dash,
                            double.IsFinite(gain) && gain < 0 ? "good" : null),
                        new("Status", null, s == null ? dash : last ? "Local optimum" : "Improving", last && s != null ? "accent" : null)
                    };
                }

                case AlgorithmEnum.NN or AlgorithmEnum.HC: {
                    var count = node?.List?.Count - 1 ?? 0;
                    return new() {
                        new("New best", null, s == null ? dash : $"{s.Iteration} of {count}"),
                        new("Starts at", null, s == null || s.Path.Count == 0 ? dash : s.Path[0].Index.ToString()),
                        new("Tour length", "f(x)", s == null || s.Path.Count == 0 ? dash : Len(s.Distance), s != null && last ? "accent" : null)
                    };
                }

                case AlgorithmEnum.AS: {
                    var lengths = s != null && s.Messages.TryGetValue("Ant tour lengths", out var text) && !string.IsNullOrWhiteSpace(text)
                        ? text.Split(';').Select(v => double.TryParse(v.Trim(), NumberStyles.Float, Inv, out var d) ? d : double.NaN)
                              .Where(double.IsFinite).ToList()
                        : new List<double>();
                    var any = s != null && p != null && lengths.Count > 0;
                    var bestSoFar = double.PositiveInfinity;
                    if (node != null)
                        for (var at = node.List.First.Next; at != null; at = at.Next) {
                            bestSoFar = Math.Min(bestSoFar, at.Value.Distance);
                            if (at == node)
                                break;
                        }
                    var foundNow = any && s.Distance <= bestSoFar + 1e-9;
                    var tau = s == null || s.PathEdges.Count == 0 ? 0 : s.PathEdges.Max(e => e.Pheromone);
                    return new() {
                        new("Iteration best", null, any ? Len(s.Distance) : dash, null, "the tour drawn on top"),
                        new("Best so far", null, any ? Len(bestSoFar) : dash, foundNow ? "accent" : null),
                        new("Mean ant", null, any ? Len(lengths.Average()) : dash),
                        new("Worst ant", null, any ? Len(lengths.Max()) : dash),
                        new("Distinct tours", null, any ? $"{lengths.Select(v => Math.Round(v, 3)).Distinct().Count()} of {lengths.Count}" : dash,
                            null, "counted by length"),
                        new("Max pheromone", null, tau > 0 ? tau.ToString("0.0000", Inv) : dash, null, "the highest pheromone level τ on any edge")
                    };
                }

                default: {
                    var move = s?.MoveInfo;
                    var best = double.PositiveInfinity;
                    if (node != null)
                        for (var at = node.List.First; at != null; at = at.Next) {
                            best = Math.Min(best, at.Value.Distance);
                            if (at == node)
                                break;
                        }
                    return new() {
                        new("Last move", null, move == null ? dash : Derivations.MoveContext(move)),
                        new("Change", "Δ", move == null ? dash : Signed(move.Delta), move == null ? null : move.Delta < 0 ? "good" : move.Delta > 0 ? "bad" : null),
                        new("Tour length", "f(x)", s == null ? dash : Len(s.Distance)),
                        new("Best so far", null, s == null ? dash : Len(best))
                    };
                }
            }
        }

        /* The player's label: where in the run this step is, in the heuristic's own terms. */
        public static string Label(AlgorithmEnum algorithm, bool multiStart, LinkedListNode<GraphState> node, int index, int count) {
            if (node == null)
                return "";
            var s = node.Value;
            var first = index == 0;
            var last = node.Next == null;

            if (multiStart && algorithm is AlgorithmEnum.NN or AlgorithmEnum.HC)
                return first ? "Before the first restart" : $"New best {index} of {count - 1}";

            return algorithm switch {
                AlgorithmEnum.SA => first ? "Start" : last ? $"Best tour after {Count(count - 2)} draws" : $"Draw {Count(index)} · phase {s.Iteration}",
                AlgorithmEnum.NN => first ? $"Start at node {s.Path.FirstOrDefault()?.Index}" : s.Path.Count > s.Nodes.Count ? "Tour closed" : $"Node {s.Path.Count} of {s.Nodes.Count} added",
                AlgorithmEnum.HC => first ? "Start tour" : $"Improving move {index} of {count - 1}",
                AlgorithmEnum.AS => first ? "Initial pheromone" : $"Iteration {index} of {count - 1}",
                _ => first ? "Start tour" : $"Move {index}"
            };
        }

        public static List<LegendItem> Legend(AlgorithmEnum algorithm, bool multiStart) => algorithm switch {
            AlgorithmEnum.AS => new() { new("pheromone", "pheromone"), new("tour", "best tour") },
            AlgorithmEnum.NN when !multiStart => new() { new("added", "edge just added") },
            _ => new() { new("added", "added"), new("removed", "removed"), new("i", "i"), new("j", "j") }
        };

        /* The tour length over the run as an SVG path in a 1000 x 20 box, and the steps where the
         * best so far improved. The scrubber draws it behind its track. */
        public static (string Path, List<double> Ticks) Sparkline(LinkedList<GraphState> history, AlgorithmEnum algorithm) {
            var values = history.Select(s => s.Distance).ToList();
            /* The first state of some runs has no tour yet (0, or MaxValue as a start marker). */
            for (var i = values.Count - 1; i >= 0; i--)
                if (!double.IsFinite(values[i]) || values[i] <= 0 || values[i] >= double.MaxValue / 2)
                    values[i] = i + 1 < values.Count ? values[i + 1] : 0;

            var ticks = new List<double>();
            if (values.Count < 2)
                return ("", ticks);

            var lo = values.Where(v => v > 0).DefaultIfEmpty(0).Min();
            var hi = values.Max();
            var span = hi - lo > 0 ? hi - lo : 1;
            var n = values.Count - 1;
            double X(int i) => Math.Round(i * 1000.0 / n, 1);
            double Y(double v) => Math.Round(18 - (v - lo) / span * 16, 1);

            var sb = new StringBuilder(values.Count * 12);
            for (var i = 0; i < values.Count; i++)
                sb.Append(i == 0 ? 'M' : 'L').Append(X(i).ToString(Inv)).Append(' ').Append(Y(values[i]).ToString(Inv));

            /* New bests are only meaningful where the tour can get longer again. */
            if (algorithm is AlgorithmEnum.SA or AlgorithmEnum.AS or AlgorithmEnum.Manual) {
                var best = double.PositiveInfinity;
                var i = 0;
                foreach (var state in history) {
                    var b = algorithm == AlgorithmEnum.SA ? Parse(state, "Best") : values[i];
                    if (double.IsFinite(b) && b > 0 && b < best - 1e-9) {
                        if (i > 0)
                            ticks.Add(X(i));
                        best = b;
                    }
                    i++;
                }
            }

            return (sb.ToString(), ticks);
        }
    }
}
