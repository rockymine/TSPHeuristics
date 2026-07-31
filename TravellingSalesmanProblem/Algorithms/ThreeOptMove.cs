using System;
using System.Collections.Generic;
using System.Linq;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    /* Cutting a tour in three places leaves the six endpoints A|B ... C|D ... E|F and seven ways to
     * put the pieces back together that are not the original. Which one is applied used to be
     * decided inside NeighbourState.ThreeOpt and could not be stated from outside, so the move was
     * only ever available as "draw three positions, take the best of the seven". Both halves are
     * separate here: the constructor takes the reconnection, Best scores them. */
    public enum ThreeOptReconnection {
        ReverseFirst = 0,        // A CB DE F
        ReverseSecond = 1,       // A BC ED F
        SwapReverseFirst = 2,    // A DE CB F
        Swap = 3,                // A DE BC F
        SwapReverseBoth = 4,     // A ED CB F
        SwapReverseSecond = 5,   // A ED BC F
        ReverseBoth = 6          // A CB ED F
    }

    public class ThreeOptMove : ITourMove {
        public GraphProblem Graph { get; }
        public int I { get; }
        public int J { get; }
        public int K { get; }
        public ThreeOptReconnection Reconnection { get; }

        public NeighbourType Type => NeighbourType.ThreeOpt;

        /* Three cut points with a node before the first and one at the last. */
        public static bool IsApplicable(GraphProblem graph, int i, int j, int k) =>
            i > 0 && j > i && k > j && k < graph.Nodes.Count;

        public ThreeOptMove(GraphProblem graph, int i, int j, int k, ThreeOptReconnection reconnection) {
            Graph = graph;
            I = i;
            J = j;
            K = k;
            Reconnection = reconnection;
        }

        /* The seven reconnections all remove the same three edges, so ranking them by the length of
         * the edges they add is the same ranking as by delta - which is what the original code did,
         * and it is kept exactly so the tours it produced are reproducible. */
        public static ThreeOptMove Best(GraphProblem graph, int i, int j, int k) {
            if (!IsApplicable(graph, i, j, k))
                return null;

            var bestIndex = 0;
            var bestLength = double.MaxValue;

            for (int c = 0; c < 7; c++) {
                var length = new ThreeOptMove(graph, i, j, k, (ThreeOptReconnection)c).AddedLength;
                if (length < bestLength) {
                    bestLength = length;
                    bestIndex = c;
                }
            }

            return new ThreeOptMove(graph, i, j, k, (ThreeOptReconnection)bestIndex);
        }

        private Node A => Graph.Nodes[I - 1];
        private Node B => Graph.Nodes[I];
        private Node C => Graph.Nodes[J - 1];
        private Node D => Graph.Nodes[J];
        private Node E => Graph.Nodes[K - 1];
        private Node F => Graph.Nodes[K];

        private double RemovedLength =>
            Edge.GetDistance(A, B) + Edge.GetDistance(C, D) + Edge.GetDistance(E, F);

        /* The three edges this reconnection introduces, in the order A-?, ?-?, ?-F. */
        private (Node, Node)[] AddedEdges => Reconnection switch {
            ThreeOptReconnection.ReverseFirst      => new[] { (A, C), (B, D), (E, F) },
            ThreeOptReconnection.ReverseSecond     => new[] { (A, B), (C, E), (D, F) },
            ThreeOptReconnection.SwapReverseFirst  => new[] { (A, D), (C, E), (F, B) },
            ThreeOptReconnection.Swap              => new[] { (A, D), (E, B), (C, F) },
            ThreeOptReconnection.SwapReverseBoth   => new[] { (A, E), (C, D), (F, B) },
            ThreeOptReconnection.SwapReverseSecond => new[] { (A, E), (B, D), (C, F) },
            _                                      => new[] { (A, C), (E, B), (D, F) }
        };

        private double AddedLength {
            get {
                var sum = 0d;
                foreach (var (from, to) in AddedEdges)
                    sum += Edge.GetDistance(from, to);

                return sum;
            }
        }

        public double Delta => AddedLength - RemovedLength;

        public MoveInfo Describe() {
            var added = AddedEdges;
            var calculation = "$" +
                string.Join("+", added.Select(e => Round(Edge.GetDistance(e.Item1, e.Item2)).ToString())) +
                $"-{Round(Edge.GetDistance(A, B))}-{Round(Edge.GetDistance(C, D))}-{Round(Edge.GetDistance(E, F))}" +
                $"={Round(Delta)}$";

            return new MoveInfo(Type, new List<Node> { B, D, F }, new List<int> { I, J, K },
                Delta, calculation);
        }

        private static double Round(double value) => Math.Round(value, 1);

        public GraphProblem Apply() {
            var tour = Graph.Nodes.ToArray();
            var start = tour[0..I];
            var bc = tour[I..J];
            var cb = bc.Reverse().ToArray();
            var de = tour[J..K];
            var ed = de.Reverse().ToArray();
            var end = tour[K..tour.Length];

            var (first, second) = Reconnection switch {
                ThreeOptReconnection.ReverseFirst      => (cb, de),
                ThreeOptReconnection.ReverseSecond     => (bc, ed),
                ThreeOptReconnection.SwapReverseFirst  => (de, cb),
                ThreeOptReconnection.Swap              => (de, bc),
                ThreeOptReconnection.SwapReverseBoth   => (ed, cb),
                ThreeOptReconnection.SwapReverseSecond => (ed, bc),
                _                                      => (cb, ed)
            };

            var swapped = new GraphProblem {
                Nodes = start.Concat(first).Concat(second).Concat(end).ToList()
            };

            swapped.ConnectPathNodes();
            return swapped;
        }
    }
}
