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


        private static TwoOptMove TwoOptLoop(GraphProblem graph, DescentType descent) {
            var n = graph.Nodes.Count;
            var savings = 0d;
            TwoOptMove move = null;

            for (int i = 1; i <= n - 3; i++) {
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

            //random indexes for split
            var i = Random.Next(1, n - 3);
            var j = Random.Next(i + 1, n - 2);
            var k = Random.Next(j + 1, n - 1);

            //tour segments
            var start = tour[0..i];
            var BC = tour[i..j];
            var CB = BC.Reverse();
            var DE = tour[j..k];
            var ED = DE.Reverse();
            var end = tour[k..n];

            //relevant nodes
            var A = tour[i];
            var B = tour[i + 1];
            var C = tour[j];
            var D = tour[j + 1];
            var E = tour[k];
            var F = tour[k + 1];

            //edge distances
            var dAB = Edge.GetDistanceRounded(A, B);
            var dAC = Edge.GetDistanceRounded(A, C);
            var dAD = Edge.GetDistanceRounded(A, D);
            var dCD = Edge.GetDistanceRounded(C, D);
            var dEF = Edge.GetDistanceRounded(E, F);
            var dBD = Edge.GetDistanceRounded(B, D);
            var dCE = Edge.GetDistanceRounded(C, E);
            var dDF = Edge.GetDistanceRounded(D, F);
            var dEB = Edge.GetDistanceRounded(E, B);
            var dCF = Edge.GetDistanceRounded(C, F);
            var dFB = Edge.GetDistanceRounded(F, B);
            var dAE = Edge.GetDistanceRounded(A, E);

            IEnumerable<Node> final = null;
            var distances = new List<double>();

            //var d0 = dAB + dCD + dEF; //A BC DE F
            //pertubations
            var d1 = dAC + dBD + dEF; //A CB DE F
            distances.Add(d1);
            var d2 = dAB + dCE + dDF; //A BC ED F
            distances.Add(d2);
            var d3 = dAD + dCE + dFB; //A DE CB F
            distances.Add(d3);
            var d4 = dAD + dEB + dCF; //A DE BC F
            distances.Add(d4);
            var d5 = dAE + dCD + dFB; //A ED CB F
            distances.Add(d5);
            var d6 = dAE + dBD + dCF; //A ED BC F
            distances.Add(d6);
            var d7 = dAC + dEB + dDF; //A CB ED F
            distances.Add(d7);

            var min = distances.Min();

            if (min == d1)
                final = start.Concat(CB).Concat(DE);

            if (min == d2)
                final = start.Concat(BC).Concat(ED);

            if (min == d3)
                final = start.Concat(DE).Concat(CB);

            if (min == d4)
                final = start.Concat(DE).Concat(BC);

            if (min == d5)
                final = start.Concat(ED).Concat(CB);

            if (min == d6)
                final = start.Concat(ED).Concat(BC);

            if (min == d7)
                final = start.Concat(CB).Concat(ED);

            final = final.Concat(end);

            var swapped = new GraphProblem {
                Nodes = final.ToList()
            };
            swapped.ConnectPathNodes();
            return swapped;
        }

        public static GraphProblem DoubleBridgeFourOpt(GraphProblem graph) {
            var tour = graph.Nodes.ToArray();
            var n = tour.Length;

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