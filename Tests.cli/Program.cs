using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TravellingSalesmanProblem.Algorithms;
using TravellingSalesmanProblem.Graph;

namespace Tests.cli {
    /* Checks the invariants the moves and the given starting tour have to hold, so that adding a
     * move type or a new way of driving them cannot quietly break the ones that exist. Exhaustive
     * where it can be: the move checks walk every combination of cut points on small tours rather
     * than sampling.
     *
     * Run with `dotnet run --project Tests.cli`; exit code 1 means something failed. */
    internal static class Program {
        private static int _failures;

        private static void Check(bool ok, string label, string detail = "") {
            Console.WriteLine($"{(ok ? "ok  " : "FAIL")} {label}{(detail == "" ? "" : "   " + detail)}");
            if (!ok)
                _failures++;
        }

        private static int Main() {
            MoveDeltas();
            ThreeOptReconnections();
            ApplicabilityGuards();
            GivenStartTour();

            Console.WriteLine();
            Console.WriteLine(_failures == 0 ? "ALL PASS" : $"{_failures} FAILED");
            return _failures == 0 ? 0 : 1;
        }

        private static GraphProblem Tour(int n, int seed) {
            var graph = new GraphProblem();
            var random = new Random(seed);

            for (int i = 0; i < n; i++) {
                graph.Nodes.Add(new Node {
                    Index = i,
                    Position = new Vector2(random.Next(0, 200), random.Next(0, 200))
                });
            }

            return GraphProblem.OrderedGraphProblem(GraphProblem.ConnectedGraphProblem(graph));
        }

        private static string Route(GraphProblem graph) =>
            string.Join("-", graph.Nodes.Select(n => n.Index));

        /* A move's delta is what the searches compare against the tour costs, so it has to be the
         * real change in length - not merely self consistent. This is the check that caught the
         * three-opt endpoints being read one position too far. */
        private static bool DeltaMatches(GraphProblem tour, ITourMove move) =>
            Math.Abs(move.Apply().Costs - tour.Costs - move.Delta) < 1e-9;

        private static void MoveDeltas() {
            var swapBad = 0;
            var twoBad = 0;
            var threeBad = 0;
            var fourBad = 0;
            var counted = 0;

            var tour = Tour(14, 7);
            var n = tour.Nodes.Count;

            for (int i = 1; i <= n - 3; i++) {
                for (int j = i + 1; j <= n - 2; j++) {
                    if (!DeltaMatches(tour, new SwapMove(tour, i, j)))
                        swapBad++;

                    counted++;
                }
            }

            for (int i = 0; i <= n - 3; i++) {
                for (int j = i + 1; j <= n - 2; j++) {
                    if (!DeltaMatches(tour, new TwoOptMove(tour, i, j)))
                        twoBad++;

                    counted++;
                }
            }

            for (int i = 1; i <= n - 3; i++) {
                for (int j = i + 1; j <= n - 2; j++) {
                    for (int k = j + 1; k <= n - 1; k++) {
                        if (!DeltaMatches(tour, ThreeOptMove.Best(tour, i, j, k)))
                            threeBad++;

                        counted++;

                        for (int l = k + 1; l <= n - 1; l++) {
                            if (!DeltaMatches(tour, new DoubleBridgeMove(tour, i, j, k, l)))
                                fourBad++;

                            counted++;
                        }
                    }
                }
            }

            Check(swapBad == 0, "swap delta equals the change in tour length", $"{swapBad} off");
            Check(twoBad == 0, "2-opt delta equals the change in tour length", $"{twoBad} off");
            Check(threeBad == 0, "3-opt delta equals the change in tour length", $"{threeBad} off");
            Check(fourBad == 0, "double bridge delta equals the change in tour length", $"{fourBad} off");
            Console.WriteLine($"     {counted} moves checked on a {n - 1} node tour");
        }

        private static void ThreeOptReconnections() {
            var tour = Tour(20, 3);
            var n = tour.Nodes.Count;
            var reached = new HashSet<ThreeOptReconnection>();
            var inconsistent = 0;
            var notBest = 0;

            for (int i = 1; i <= n - 3; i++) {
                for (int j = i + 1; j <= n - 2; j++) {
                    for (int k = j + 1; k <= n - 1; k++) {
                        var best = ThreeOptMove.Best(tour, i, j, k);
                        reached.Add(best.Reconnection);

                        foreach (ThreeOptReconnection reconnection in Enum.GetValues(typeof(ThreeOptReconnection))) {
                            var move = new ThreeOptMove(tour, i, j, k, reconnection);
                            if (!DeltaMatches(tour, move))
                                inconsistent++;
                            if (move.Delta < best.Delta - 1e-9)
                                notBest++;
                        }
                    }
                }
            }

            Check(reached.Count == 7, "every one of the seven reconnections is reachable", $"{reached.Count}/7");
            Check(inconsistent == 0, "each single reconnection agrees with its delta", $"{inconsistent} off");
            Check(notBest == 0, "Best really returns the shortest reconnection", $"{notBest} better ones missed");
        }

        /* A manual move takes its positions from whoever is driving it, so the guards are the only
         * thing between a chosen position and an IndexOutOfRangeException. */
        private static void ApplicabilityGuards() {
            var tour = Tour(10, 11);
            var n = tour.Nodes.Count;

            Check(!SwapMove.IsApplicable(tour, 0, 3), "swap rejects position 0, which has no predecessor");
            Check(!SwapMove.IsApplicable(tour, 1, n - 1), "swap rejects the closing node");
            Check(SwapMove.IsApplicable(tour, 1, n - 2), "swap accepts the last interior pair");

            Check(TwoOptMove.IsApplicable(tour, 0, 3), "2-opt accepts position 0");
            Check(!TwoOptMove.IsApplicable(tour, 0, n - 1), "2-opt rejects the closing node");
            Check(!TwoOptMove.IsApplicable(tour, 3, 3), "2-opt rejects i == j");

            Check(!ThreeOptMove.IsApplicable(tour, 0, 2, 4), "3-opt rejects a first cut at position 0");
            Check(ThreeOptMove.IsApplicable(tour, 1, 2, 3), "3-opt accepts adjacent cuts");
            Check(ThreeOptMove.Best(tour, 0, 2, 4) == null, "3-opt returns nothing for cuts it cannot apply");

            Check(!DoubleBridgeMove.IsApplicable(tour, 1, 2, 3, n), "double bridge rejects a cut past the tour");
            Check(DoubleBridgeMove.IsApplicable(tour, 1, 2, 3, 4), "double bridge accepts adjacent cuts");
        }

        private const string Square = "n,0,0,0\nn,1,10,0\nn,2,10,10\nn,3,0,10\n";

        private static void GivenStartTour() {
            var implicitOrder = GraphProblem.FromText(Square);
            Check(implicitOrder.StartTour == null, "without a t line no tour is set");
            Check(Route(GraphProblem.OrderedGraphProblem(implicitOrder)) == "0-1-2-3-0",
                "without a t line the node order is still the starting tour");

            var given = GraphProblem.FromText(Square + "t,0,2,1,3\n");
            Check(Route(GraphProblem.OrderedGraphProblem(given)) == "0-2-1-3-0",
                "a t line sets the starting tour");

            var closed = GraphProblem.FromText(Square + "t,0,2,1,3,0\n");
            Check(Route(GraphProblem.OrderedGraphProblem(closed)) == "0-2-1-3-0",
                "a tour written closed means the same cycle");

            /* The crossed square is the textbook exercise: 2-opt has to untangle it. */
            var history = new HillClimbing { NeighbourEnum = NeighbourType.TwoOpt, DescentType = DescentType.Steepest }
                .FindPath(GraphProblem.FromText(Square + "t,0,2,1,3\n"));

            Check(string.Join("-", history.First.Value.Path.Select(n => n.Index)) == "0-2-1-3-0",
                "the search starts on the given tour",
                string.Join("-", history.First.Value.Path.Select(n => n.Index)));
            Check(Math.Abs(history.Last.Value.Distance - 40) < 1e-9,
                "and descends from there to the square of perimeter 40",
                history.Last.Value.Distance.ToString("F3"));

            Check(Rejected(Square + "t,0,1,2").Contains("tour lists"), "a tour that is too short is rejected");
            Check(Rejected(Square + "t,0,1,1,3").Contains("more than once"), "a repeated node is rejected");
            Check(Rejected(Square + "t,0,1,2,9").Contains("not defined"), "an unknown node is rejected");
            Check(Rejected(Square + "t,0,x,2,3").Contains("not a node index"), "a non numeric entry is rejected");

            var serialized = GraphProblem.SerializeGraph(given);
            Check(serialized.Contains("t,0,2,1,3"), "the starting tour survives serialization");
            Check(Route(GraphProblem.OrderedGraphProblem(GraphProblem.FromText(serialized))) == "0-2-1-3-0",
                "and reading the result back yields the same tour");

            Check(Route(GraphProblem.OrderedGraphProblem(given.DeepCopy())) == "0-2-1-3-0",
                "a deep copy keeps the tour, remapped onto its own nodes");
        }

        private static string Rejected(string text) {
            try {
                GraphProblem.FromText(text);
                return "(not rejected)";
            } catch (FormatException e) {
                return e.Message;
            }
        }
    }
}
