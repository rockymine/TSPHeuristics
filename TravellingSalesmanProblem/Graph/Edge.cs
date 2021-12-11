using GeoCoordinatePortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TravellingSalesmanProblem.Graph {
    public class Edge {
        public Node Node1 { get; set; }
        public Node Node2 { get; set; }
        public double Value { get; set; }
        public double Pheromone { get; set; }
        public double Distance => CalcDistance();

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

        public double CalcGeoDistance() {
            var point1 = new GeoCoordinate(Node1.Position.X, Node1.Position.Y);
            var point2 = new GeoCoordinate(Node2.Position.X, Node2.Position.Y);
            return point1.GetDistanceTo(point2);
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

        public Node Opposite(Node node) {
            if (node == Node1)
                return Node2;
            if (node == Node2)
                return Node1;

            throw new ArgumentException("No opposite node.");
        }

        public static Edge Between(Node node1, Node node2) {
            var edge = new Edge { 
                Node1 = node1, Node2 = node2 , 
                Node1Id = node1.Index, Node2Id = node2.Index
            };

            if (!node1.Edges.Any(e => e.IsEqual(edge)))
                node1.Edges.Add(edge);
            if (!node2.Edges.Any(e => e.IsEqual(edge)))
                node2.Edges.Add(edge);

            return edge;
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