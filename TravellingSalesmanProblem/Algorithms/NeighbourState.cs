using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class NeighbourState {
        private static readonly Random Random = new();
        public static GraphProblem Create(GraphProblem graph, NeighbourType type, DescentType descent) {
            graph.Reset();
            var neighbor = new GraphProblem();
            switch (type) {
                case NeighbourType.Swap:
                    neighbor = Swap(graph, descent);
                    break;
                case NeighbourType.TwoOpt:
                    neighbor = TwoOpt(graph, descent);
                    break;
                case NeighbourType.ThreeOpt:
                    neighbor = ThreeOpt(graph);
                    break;
                case NeighbourType.FourOpt:
                    neighbor = DoubleBridgeFourOpt(graph);
                    break;
            }

            return neighbor;
        }

        public static GraphProblem TwoOpt(GraphProblem graph, DescentType descent) {
            TwoOptMove move;
            if (descent == DescentType.Random) {
                var n = graph.Nodes.Count;
                if (n < 4)
                    return graph;

                var pos1 = Random.Next(0, n - 2);
                var pos2 = Random.Next(pos1 + 1, n - 1);
                move = new TwoOptMove(graph, pos1, pos2);
            } else {
                move = TwoOptLoop(graph, descent);
            }

            if (move == null)
                return graph;

            var best = move.SwapEdges();
            best.SwapInfo = move.SwapInfo;
            return best;
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

        public static GraphProblem Swap(GraphProblem graph, DescentType descent) {
            SwapMove move;
            if (descent == DescentType.Random) {
                var n = graph.Nodes.Count;

                /* SwapMove reads Nodes[I-1] and Nodes[J+1], so it needs an interior i < j with a
                 * predecessor and a successor. Below five tour entries no such pair exists and the
                 * old bounds threw ArgumentOutOfRangeException out of Random.Next. */
                if (n < 5)
                    return graph;

                var pos1 = Random.Next(1, n - 2);
                var pos2 = Random.Next(pos1 + 1, n - 1);
                move = new SwapMove(graph, pos1, pos2);
            } else {
                move = SwapLoop(graph, descent);
            }

            if (move == null)
                return graph;

            var best = move.SwapNodes();
            best.SwapInfo = move.SwapInfo;
            return best;
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

        public static GraphProblem ThreeOpt(GraphProblem graph) {
            var tour = graph.Nodes.ToArray();
            var n = tour.Length;

            /* Needs three cut points with a node before the first and at the last, so the smallest
             * workable tour has four entries. Below that Random.Next threw. */
            if (n < 4)
                return graph;

            //random indexes for split
            var i = Random.Next(1, n - 3);
            var j = Random.Next(i + 1, n - 2);
            var k = Random.Next(j + 1, n - 1);

            //tour segments
            var start = tour[0..i];
            var BC = tour[i..j];
            var CB = BC.Reverse().ToArray();
            var DE = tour[j..k];
            var ED = DE.Reverse().ToArray();
            var end = tour[k..n];

            /* The six endpoints have to be the actual boundaries of the segments above: A is the
             * last node of start, B/C the first/last of BC, D/E the first/last of DE and F the
             * first of end. They were each read one position too far, so all seven candidate
             * lengths described a different reconnection than the one that was applied - the
             * "minimum" perturbation lengthened the tour in about a third of all draws, and
             * reading tour[k + 1] overran the array on short tours. */
            var A = tour[i - 1];
            var B = BC[0];
            var C = BC[^1];
            var D = DE[0];
            var E = DE[^1];
            var F = end[0];

            //edge distances
            var dAB = Edge.GetDistance(A, B);
            var dAC = Edge.GetDistance(A, C);
            var dAD = Edge.GetDistance(A, D);
            var dCD = Edge.GetDistance(C, D);
            var dEF = Edge.GetDistance(E, F);
            var dBD = Edge.GetDistance(B, D);
            var dCE = Edge.GetDistance(C, E);
            var dDF = Edge.GetDistance(D, F);
            var dEB = Edge.GetDistance(E, B);
            var dCF = Edge.GetDistance(C, F);
            var dFB = Edge.GetDistance(F, B);
            var dAE = Edge.GetDistance(A, E);

            /* Reconnections paired with their length, so the applied one is always the scored one.
             * The old chain of independent "if (min == dN)" tests could assign twice on ties and
             * relied on exact floating point equality to recover the winner. */
            var candidates = new (double Length, Node[] First, Node[] Second)[] {
                (dAC + dBD + dEF, CB, DE), //A CB DE F
                (dAB + dCE + dDF, BC, ED), //A BC ED F
                (dAD + dCE + dFB, DE, CB), //A DE CB F
                (dAD + dEB + dCF, DE, BC), //A DE BC F
                (dAE + dCD + dFB, ED, CB), //A ED CB F
                (dAE + dBD + dCF, ED, BC), //A ED BC F
                (dAC + dEB + dDF, CB, ED)  //A CB ED F
            };

            var bestIndex = 0;
            for (int c = 1; c < candidates.Length; c++) {
                if (candidates[c].Length < candidates[bestIndex].Length)
                    bestIndex = c;
            }

            var best = candidates[bestIndex];
            var final = start.Concat(best.First).Concat(best.Second).Concat(end);

            var swapped = new GraphProblem {
                Nodes = final.ToList()
            };
            swapped.ConnectPathNodes();
            return swapped;
        }

        public static GraphProblem DoubleBridgeFourOpt(GraphProblem graph) {
            var tour = graph.Nodes.ToArray();
            var n = tour.Length;

            /* Four cut points need at least five tour entries; Random.Next threw below that. */
            if (n < 5)
                return graph;

            var i = Random.Next(1, n - 4);
            var j = Random.Next(i + 1, n - 3);
            var k = Random.Next(j + 1, n - 2);
            var l = Random.Next(k + 1, n - 1);

            //tour is split into 5 segments: a-b-c-d-a
            var a1 = tour[0..i];
            var b = tour[i..j];
            var c = tour[j..k];
            var d = tour[k..l];
            var a2 = tour[l..n];

            //tour is recombined as: a-d-c-b-a
            var final = a1.Concat(d).Concat(c).Concat(b).Concat(a2);
            var swapped = new GraphProblem {
                Nodes = final.ToList()
            };
            swapped.ConnectPathNodes();
            return swapped;
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