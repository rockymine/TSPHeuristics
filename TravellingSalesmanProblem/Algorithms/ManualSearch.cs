using ChartData;
using System;
using System.Collections.Generic;
using System.Linq;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    /* Playing the search by hand: the moves are chosen by whoever is driving, one at a time.
     *
     * This is not a heuristic and decides nothing. It exists because everything downstream - the
     * animation, the tables, the canvases - reads a LinkedList<GraphState>, which is already a move
     * log; only the producer differs. FindPath therefore returns a history holding nothing but the
     * starting tour, and Apply extends it.
     *
     * Applying a move to a state that is not the last one branches: the entries after it are
     * dropped, so stepping back and trying something else replaces the old continuation instead of
     * leaving a history that no longer matches what is drawn. */
    public class ManualSearch : Algorithm {
        public override LinkedList<GraphState> FindPath(GraphProblem graph) {
            var history = new LinkedList<GraphState>();
            if (graph.Nodes.Count == 0)
                return history;

            var tour = GraphProblem.OrderedGraphProblem(graph);

            var chartInfo = new ChartInfo {
                Title = "Distance Progress",
                XAxis = new ChartSet { Title = "Move" },
                YAxis = new List<ChartSet> {
                    new ChartSet { Title = "Distance" }
                }
            };

            var state = new GraphState {
                Nodes = graph.Nodes,
                Path = tour.Nodes,
                PathEdges = tour.Edges,
                Distance = tour.Costs,
                Iteration = 0,
                ChartInfo = new List<ChartInfo> { chartInfo }
            };

            state.ChartInfo[0].XAxis.Add(0, "red");
            state.ChartInfo[0].YAxis[0].Add(state.Distance, "blue");

            UpdateStateMessages(state);
            history.AddLast(state);
            return history;
        }

        /* The tour of a history entry, in the shape the moves expect. */
        public static GraphProblem TourOf(GraphState state) => state.ToGraphProblem();

        /* Where a node sits in the tour. The closing entry repeats the first node, and IndexOf
         * finds the opening one, which is the position every move means. */
        public static int PositionOf(GraphProblem tour, int nodeIndex) =>
            tour.Nodes.FindIndex(n => n.Index == nodeIndex);

        /* Builds the requested move, or explains why those positions do not admit one. The message
         * is for the person choosing, so it names the rule rather than the index arithmetic. */
        public static ITourMove Build(GraphProblem tour, NeighbourType type, IReadOnlyList<int> positions,
                                      ThreeOptReconnection reconnection, out string error) {
            error = null;

            var needed = type switch {
                NeighbourType.Swap => 2,
                NeighbourType.TwoOpt => 2,
                NeighbourType.ThreeOpt => 3,
                _ => 4
            };

            if (positions == null || positions.Count != needed || positions.Any(p => p < 0)) {
                error = $"This move needs {needed} distinct nodes of the tour.";
                return null;
            }

            var sorted = positions.OrderBy(p => p).ToList();
            if (sorted.Distinct().Count() != needed) {
                error = "The chosen nodes have to be distinct.";
                return null;
            }

            switch (type) {
                case NeighbourType.Swap:
                    if (!SwapMove.IsApplicable(tour, sorted[0], sorted[1])) {
                        error = "A swap exchanges a node with its neighbours on both sides, so the "
                              + "first and the last node of the tour cannot take part.";
                        return null;
                    }

                    return new SwapMove(tour, sorted[0], sorted[1]);

                case NeighbourType.TwoOpt:
                    if (!TwoOptMove.IsApplicable(tour, sorted[0], sorted[1])) {
                        error = "A 2-opt move removes the edge leaving each of the two nodes, so the "
                              + "closing node of the tour cannot take part.";
                        return null;
                    }

                    return new TwoOptMove(tour, sorted[0], sorted[1]);

                case NeighbourType.ThreeOpt:
                    if (!ThreeOptMove.IsApplicable(tour, sorted[0], sorted[1], sorted[2])) {
                        error = "The three cuts are made in front of the chosen nodes, so the first "
                              + "node of the tour cannot be one of them.";
                        return null;
                    }

                    return new ThreeOptMove(tour, sorted[0], sorted[1], sorted[2], reconnection);

                default:
                    if (!DoubleBridgeMove.IsApplicable(tour, sorted[0], sorted[1], sorted[2], sorted[3])) {
                        error = "The four cuts are made in front of the chosen nodes, so the first "
                              + "node of the tour cannot be one of them.";
                        return null;
                    }

                    return new DoubleBridgeMove(tour, sorted[0], sorted[1], sorted[2], sorted[3]);
            }
        }

        /* Appends the result of the move after `from`, dropping whatever followed it. */
        public LinkedListNode<GraphState> Apply(LinkedList<GraphState> history,
                                                LinkedListNode<GraphState> from, ITourMove move) {
            if (history == null || from == null || move == null)
                return from;

            while (history.Last != from)
                history.RemoveLast();

            var tour = move.Apply();
            var state = from.Value.DeepCopy();

            state.Path = tour.Nodes;
            state.PathEdges = tour.Edges;
            state.Distance = tour.Costs;
            state.MoveInfo = move.Describe();
            state.Iteration = from.Value.Iteration + 1;

            RebuildChart(state, history, tour.Costs);
            UpdateStateMessages(state);
            return history.AddLast(state);
        }

        /* Removes the last move. Reset goes back to the starting tour in one step. */
        public static LinkedListNode<GraphState> Undo(LinkedList<GraphState> history) {
            if (history == null || history.Count < 2)
                return history?.First;

            history.RemoveLast();
            return history.Last;
        }

        public static LinkedListNode<GraphState> Reset(LinkedList<GraphState> history) {
            if (history == null)
                return null;

            while (history.Count > 1)
                history.RemoveLast();

            return history.First;
        }

        /* The chart is derived from the history rather than appended to, so undoing a move drops
         * its point as well - a curve that keeps abandoned attempts would not match the table. */
        private static void RebuildChart(GraphState state, LinkedList<GraphState> history, double distance) {
            if (state.ChartInfo == null || state.ChartInfo.Count == 0)
                return;

            var chart = state.ChartInfo[0];
            chart.XAxis.Values.Clear();
            chart.XAxis.Colors.Clear();
            chart.YAxis[0].Values.Clear();
            chart.YAxis[0].Colors.Clear();

            var move = 0;
            foreach (var previous in history) {
                chart.XAxis.Add(move++, "red");
                chart.YAxis[0].Add(previous.Distance, "red");
            }

            chart.XAxis.Add(move, "red");
            chart.YAxis[0].Add(distance, "blue");
        }

        public override void UpdateStateMessages(GraphState state) {
            state.Messages["Move"] = state.Iteration.ToString();
            state.Messages["Route"] = string.Join("-", state.Path.Select(n => n.Index));
            state.Messages["Distance"] = Math.Round(state.Distance, 3).ToString();

            if (state.MoveInfo != null) {
                state.Messages["Last Move"] =
                    $"{Label(state.MoveInfo.Type)} on {string.Join(", ", state.MoveInfo.Nodes.Select(n => n.Index))}";
                state.Messages["Change"] = Math.Round(state.MoveInfo.Delta, 3).ToString();
            } else {
                state.Messages.Remove("Last Move");
                state.Messages.Remove("Change");
            }
        }

        public static string Label(NeighbourType type) => type switch {
            NeighbourType.Swap => "Swap",
            NeighbourType.TwoOpt => "2-opt",
            NeighbourType.ThreeOpt => "3-opt",
            _ => "Double bridge"
        };
    }
}
