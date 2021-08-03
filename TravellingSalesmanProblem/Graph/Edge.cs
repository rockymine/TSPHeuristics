using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static TravellingSalesmanProblem.Algorithms.AntAlgorithm;

namespace TravellingSalesmanProblem.Graph {
    public class Edge {
        public Node Node1 { get; set; }
        public Node Node2 { get; set; }
        public double Value { get; set; }
        public string Color { get; set; }
        public double Pheromone { get; set; } = 0;
        public double Visibility => 1 / Distance;
        public double Distance => Vector2.Distance(Node1.Position, Node2.Position);

        internal int Node1Id;
        internal int Node2Id;

        public bool IsBetween(Node node1, Node node2) {
            if (Node1 == node1 && Node2 == node2)
                return true;
            if (Node2 == node1 && Node1 == node2)
                return true;

            return false;
        }

        public Node Opposite(Node node) {
            if (node == Node1)
                return Node2;
            if (node == Node2)
                return Node1;

            throw new ArgumentException();
        }

        public static Edge Between(Node node1, Node node2) {
            var edge = new Edge { Node1 = node1, Node2 = node2 };

            node1.Edges.Add(edge);
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
                //ignore already visited nodes
                if (edge.Opposite(node).Visited)
                    continue;

                if (edge.Distance < closestDistance) {
                    closestDistance = edge.Distance;
                    closest = edge;
                }
            }

            return closest;
        }

        public Vector2 FindCenter() => Vector2.Add(Node1.Position, Node2.Position) / 2;
    }
}