using System;
using System.Collections.Generic;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    /* Picks a move out of a neighbourhood. Applying it is the move's own job - see ITourMove.
     *
     * This used to do both at once and return the resulting tour, which is why nothing outside
     * could say "apply this move at these positions": the positions never left the method. The
     * selection rules are unchanged, so the tours the searches walk are the same. */
    public class NeighbourState {
        private static readonly Random Random = new();

        /* Returns null when the neighbourhood holds nothing to apply: too few nodes for the move,
         * or - for the descending rules - no move that shortens the tour. */
        public static ITourMove Select(GraphProblem graph, NeighbourType type, DescentType descent) {
            return type switch {
                NeighbourType.Swap => SelectSwap(graph, descent),
                NeighbourType.TwoOpt => SelectTwoOpt(graph, descent),
                NeighbourType.ThreeOpt => SelectThreeOpt(graph),
                NeighbourType.FourOpt => SelectDoubleBridge(graph),
                _ => null
            };
        }

        public static GraphProblem Create(GraphProblem graph, NeighbourType type, DescentType descent) {
            graph.Reset();

            var move = Select(graph, type, descent);
            if (move == null)
                return graph;

            var neighbour = move.Apply();
            neighbour.MoveInfo = move.Describe();
            return neighbour;
        }

        private static ITourMove SelectTwoOpt(GraphProblem graph, DescentType descent) {
            if (descent != DescentType.Random)
                return TwoOptLoop(graph, descent);

            var n = graph.Nodes.Count;
            if (n < 4)
                return null;

            var pos1 = Random.Next(0, n - 2);
            var pos2 = Random.Next(pos1 + 1, n - 1);
            return new TwoOptMove(graph, pos1, pos2);
        }

        /* i started at 1, so the edge leaving the first tour node was never a candidate for
         * removal and steepest/next descent explored a strictly smaller neighbourhood than the
         * random variant (which draws pos1 from 0). Closed tours have no privileged first edge. */
        private static TwoOptMove TwoOptLoop(GraphProblem graph, DescentType descent) {
            var n = graph.Nodes.Count;
            var savings = 0d;
            TwoOptMove move = null;

            for (int i = 0; i <= n - 3; i++) {
                for (int j = i + 1; j <= n - 2; j++) {
                    var temp = new TwoOptMove(graph, i, j);
                    if (temp.Costs < savings) {
                        savings = temp.Costs;
                        move = temp;

                        if (descent == DescentType.Next)
                            return move;
                    }
                }
            }

            return move;
        }

        private static ITourMove SelectSwap(GraphProblem graph, DescentType descent) {
            if (descent != DescentType.Random)
                return SwapLoop(graph, descent);

            var n = graph.Nodes.Count;

            /* SwapMove reads Nodes[I-1] and Nodes[J+1], so it needs an interior i < j with a
             * predecessor and a successor. Below five tour entries no such pair exists and the
             * old bounds threw ArgumentOutOfRangeException out of Random.Next. */
            if (n < 5)
                return null;

            var pos1 = Random.Next(1, n - 2);
            var pos2 = Random.Next(pos1 + 1, n - 1);
            return new SwapMove(graph, pos1, pos2);
        }

        private static SwapMove SwapLoop(GraphProblem graph, DescentType descent) {
            var n = graph.Nodes.Count;
            var savings = 0d;
            SwapMove move = null;

            for (int i = 1; i <= n - 3; i++) {
                for (int j = i + 1; j <= n - 2; j++) {
                    var temp = new SwapMove(graph, i, j);
                    if (temp.Costs < savings) {
                        savings = temp.Costs;
                        move = temp;

                        if (descent == DescentType.Next)
                            return move;
                    }
                }
            }

            return move;
        }

        /* Both perturbations draw their cut points; three-opt then takes the shortest of the seven
         * reconnections. Neither compares the result against the current tour - they are kicks, and
         * the caller decides whether to accept a longer one. */
        private static ITourMove SelectThreeOpt(GraphProblem graph) {
            var n = graph.Nodes.Count;

            /* Needs three cut points with a node before the first and at the last, so the smallest
             * workable tour has four entries. Below that Random.Next threw. */
            if (n < 4)
                return null;

            var i = Random.Next(1, n - 3);
            var j = Random.Next(i + 1, n - 2);
            var k = Random.Next(j + 1, n - 1);

            return ThreeOptMove.Best(graph, i, j, k);
        }

        private static ITourMove SelectDoubleBridge(GraphProblem graph) {
            var n = graph.Nodes.Count;

            /* Four cut points need at least five tour entries; Random.Next threw below that. */
            if (n < 5)
                return null;

            var i = Random.Next(1, n - 4);
            var j = Random.Next(i + 1, n - 3);
            var k = Random.Next(j + 1, n - 2);
            var l = Random.Next(k + 1, n - 1);

            return new DoubleBridgeMove(graph, i, j, k, l);
        }
    }

    public enum NeighbourType {
        Swap,
        TwoOpt,
        ThreeOpt,
        FourOpt
    }

    public enum DescentType {
        Random,
        Next,
        Steepest
    }
}
