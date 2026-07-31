using System;
using System.Collections.Generic;
using System.Linq;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    /* The double bridge: cut the tour a|b|c|d|a into four pieces and reassemble it as a-d-c-b-a.
     * It has only one outcome per set of cut points, so unlike three-opt there is nothing to
     * choose - but it still needs to be a move object, because a manual run has to be able to name
     * its cut points and see what the exchange costs. */
    public class DoubleBridgeMove : ITourMove {
        public GraphProblem Graph { get; }
        public int I { get; }
        public int J { get; }
        public int K { get; }
        public int L { get; }

        public NeighbourType Type => NeighbourType.FourOpt;

        public static bool IsApplicable(GraphProblem graph, int i, int j, int k, int l) =>
            i > 0 && j > i && k > j && l > k && l < graph.Nodes.Count;

        public DoubleBridgeMove(GraphProblem graph, int i, int j, int k, int l) {
            Graph = graph;
            I = i;
            J = j;
            K = k;
            L = l;
        }

        /* The four edges the cuts remove, at the segment boundaries. */
        private Node A1End => Graph.Nodes[I - 1];
        private Node BStart => Graph.Nodes[I];
        private Node BEnd => Graph.Nodes[J - 1];
        private Node CStart => Graph.Nodes[J];
        private Node CEnd => Graph.Nodes[K - 1];
        private Node DStart => Graph.Nodes[K];
        private Node DEnd => Graph.Nodes[L - 1];
        private Node A2Start => Graph.Nodes[L];

        private (Node, Node)[] RemovedEdges => new[] {
            (A1End, BStart), (BEnd, CStart), (CEnd, DStart), (DEnd, A2Start)
        };

        private (Node, Node)[] AddedEdges => new[] {
            (A1End, DStart), (DEnd, CStart), (CEnd, BStart), (BEnd, A2Start)
        };

        public double Delta => Length(AddedEdges) - Length(RemovedEdges);

        private static double Length((Node, Node)[] edges) {
            var sum = 0d;
            foreach (var (from, to) in edges)
                sum += Edge.GetDistance(from, to);

            return sum;
        }

        public MoveInfo Describe() {
            var calculation = "$" +
                string.Join("+", AddedEdges.Select(e => Round(Edge.GetDistance(e.Item1, e.Item2)).ToString())) +
                string.Concat(RemovedEdges.Select(e => "-" + Round(Edge.GetDistance(e.Item1, e.Item2)))) +
                $"={Round(Delta)}$";

            return new MoveInfo(Type, new List<Node> { BStart, CStart, DStart, A2Start },
                new List<int> { I, J, K, L }, Delta, calculation);
        }

        private static double Round(double value) => Math.Round(value, 1);

        public GraphProblem Apply() {
            var tour = Graph.Nodes.ToArray();
            var a1 = tour[0..I];
            var b = tour[I..J];
            var c = tour[J..K];
            var d = tour[K..L];
            var a2 = tour[L..tour.Length];

            var swapped = new GraphProblem {
                Nodes = a1.Concat(d).Concat(c).Concat(b).Concat(a2).ToList()
            };

            swapped.ConnectPathNodes();
            return swapped;
        }
    }
}
