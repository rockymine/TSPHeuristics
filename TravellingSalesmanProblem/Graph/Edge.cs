using GeoCoordinatePortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TravellingSalesmanProblem.Graph {
    public class Edge {
        private Node _node1;
        private Node _node2;
        private double _distance = -1;

        /* Distance is read in the innermost loops of every heuristic (GraphProblem.Costs alone
         * re-summed it thousands of times per run). Node positions never change once a graph is
         * built, so the euclidean length is computed once and cached until an endpoint is
         * reassigned - PostProcess and the ant colonies relink copied edges to their nodes. */
        public Node Node1 {
            get => _node1;
            set { _node1 = value; _distance = -1; }
        }

        public Node Node2 {
            get => _node2;
            set { _node2 = value; _distance = -1; }
        }

        public double Value { get; set; }
        public double Pheromone { get; set; }
        public double Distance => _distance >= 0 ? _distance : _distance = CalcDistance();

        internal int Node1Id;
        internal int Node2Id;

        public Edge Copy() {
            return new Edge {
                Value = Value,
                Pheromone = Pheromone,
                Node1Id = Node1Id,
                Node2Id = Node2Id
            };
        }

        public double CalcDistance() {
            return Vector2.Distance(Node1.Position, Node2.Position);
        }

        public double CalcDistance2D() {
            var xd = Node1.Position.X - Node2.Position.X;
            var yd = Node1.Position.Y - Node2.Position.Y;
            var dij = Math.Sqrt(xd * xd + yd * yd);
            
            return (int)(dij + 0.5);
        }

        public double CalcGeoDistance() {
            var rrr = 6378.388;

            var latitudePoint1 = CalcLatitudeOrLongitude(Node1.Position.X);
            var longitudePoint1 = CalcLatitudeOrLongitude(Node1.Position.Y);
            var latitudePoint2 = CalcLatitudeOrLongitude(Node2.Position.X);
            var longitudePoint2 = CalcLatitudeOrLongitude(Node2.Position.Y);

            var q1 = Math.Cos(longitudePoint1 - longitudePoint2);
            var q2 = Math.Cos(latitudePoint1 - latitudePoint2);
            var q3 = Math.Cos(latitudePoint1 + latitudePoint2);

            return (int)(rrr * Math.Acos(0.5 * ((1.0 + q1) * q2 - (1.0 - q1) * q3)) + 1.0);
        }

        private static double CalcLatitudeOrLongitude(double coordinate) {
            var deg = (int)coordinate;
            var min = coordinate - deg;

            return Math.PI * (deg + 5.0 * min / 3.0) / 180.0;
        }

        public bool IsBetween(Node node1, Node node2) {
            if (Node1 == node1 && Node2 == node2)
                return true;
            if (Node2 == node1 && Node1 == node2)
                return true;

            return false;
        }

        public bool IsBetweenId(int node1id, int node2id) {
            if (Node1Id == node1id && Node2Id == node2id)
                return true;
            if (Node2Id == node1id && Node1Id == node2id)
                return true;

            return false;
        }

        public bool IsEqual(Edge edge) {
            if (Node1 == edge.Node1 && Node2 == edge.Node2)
                return true;
            if (Node1 == edge.Node2 && Node2 == edge.Node1)
                return true;

            return false;
        }

        public bool IsInside(List<Edge> edges) {
            return edges.Any(edge => IsEqual(edge));
        }

        /* Order independent identity of an edge, for dictionary/set lookups. Scanning a list with
         * IsEqual turned the pheromone updates into an O(E * tour) sweep per ant per iteration. */
        internal (int, int) Key => Node1Id < Node2Id ? (Node1Id, Node2Id) : (Node2Id, Node1Id);

        internal static (int, int) KeyOf(Node node1, Node node2) =>
            node1.Index < node2.Index ? (node1.Index, node2.Index) : (node2.Index, node1.Index);

        internal static HashSet<(int, int)> KeySet(IEnumerable<Edge> edges) {
            var set = new HashSet<(int, int)>();
            foreach (var edge in edges)
                set.Add(edge.Key);

            return set;
        }

        public Node Opposite(Node node) {
            if (node == Node1)
                return Node2;
            if (node == Node2)
                return Node1;

            throw new ArgumentException("No opposite node.");
        }

        public static Edge Between(Node node1, Node node2) => Between(node1, node2, true);

        /* register=false skips the adjacency bookkeeping. Only NearestNeighbour walks Node.Edges,
         * and it does so on the complete graph; tours and ant paths keep their edges in the
         * GraphProblem itself, so registering them cost two O(degree) scans per edge for nothing. */
        public static Edge Between(Node node1, Node node2, bool register) {
            var edge = new Edge {
                Node1 = node1, Node2 = node2 ,
                Node1Id = node1.Index, Node2Id = node2.Index
            };

            if (!register)
                return edge;

            if (!node1.Edges.Any(e => e.IsEqual(edge)))
                node1.Edges.Add(edge);
            if (!node2.Edges.Any(e => e.IsEqual(edge)))
                node2.Edges.Add(edge);

            return edge;
        }

        /* Tour cost is the exact euclidean length, so move deltas have to be measured the same way.
         * Evaluating them with GetDistanceRounded disagreed with the acceptance test on the sign of
         * the delta often enough to matter: hill climbing would generate a move that looked like an
         * improvement at one decimal, find it was not an improvement exactly, and stop the descent.
         * GetDistanceRounded is kept for the numbers shown in the UI. */
        public static double GetDistance(Node n1, Node n2) {
            return Vector2.Distance(n1.Position, n2.Position);
        }

        public static double GetDistanceRounded(Node n1, Node n2) {
            return Math.Round(Vector2.Distance(n1.Position, n2.Position), 1);
        }

        public static Edge FindShortest(Node node) {
            Edge closest = null;
            var closestDistance = double.MaxValue;

            foreach (var edge in node.Edges) {
                if (edge.Opposite(node).Visited)
                    continue;

                if (edge.Distance < closestDistance) {
                    closestDistance = edge.Distance;
                    closest = edge;
                }
            }

            return closest;
        }

        public static Edge FindLongest(Node node) {
            Edge longest = null;
            var longestDistance = double.MinValue;

            foreach (var edge in node.Edges) {
                if (edge.Opposite(node).Visited)
                    continue;

                if (edge.Distance > longestDistance) {
                    longestDistance = edge.Distance;
                    longest = edge;
                }
            }

            return longest;
        }

        public Vector2 FindCenter() => Vector2.Add(Node1.Position, Node2.Position) / 2;
    }
}