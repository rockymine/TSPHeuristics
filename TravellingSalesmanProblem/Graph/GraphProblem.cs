using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Globalization;
using System.IO;

namespace TravellingSalesmanProblem.Graph {
    public class GraphProblem {
        public List<Node> Nodes { get; set; }
        public List<Edge> Edges { get; set; }

        private static readonly Random Random = new();
        public GraphProblem() {
            Nodes = new List<Node>();
            Edges = new List<Edge>();
        }

        public static GraphProblem RandomGraphProblem(int maxX, int maxY, int maxNodes) {
            var graph = new GraphProblem();

            for (int i = 0; i < maxNodes; i++) {
                Node node = RandomNode(i, maxX, maxY);

                if (graph.EqualPosition(node))
                    node = RandomNode(i, maxX, maxY);

                graph.Nodes.Add(node);
            }

            graph.ConnectAllNodes();
            return graph;
        }

        public static GraphProblem OrderedGraphProblem(GraphProblem graph) {
            var ordered = new GraphProblem();
            var path = graph.Nodes;

            //close graph
            path.Add(graph.Nodes[0]);
            ordered.Nodes = path;
            ordered.ConnectPathNodes();

            return ordered;
        }

        private static Node RandomNode(int index, int maxX, int maxY) {
            return new Node {
                Index = index,
                Position = new Vector2((int)(Random.NextDouble() * maxX), (int)(Random.NextDouble() * maxY))
            };
        }

        public void ConnectAllNodes() {
            foreach (var n1 in Nodes) {
                foreach (var n2 in Nodes) {
                    if (n1 == n2)
                        continue;
                    if (Edges.Any(e => e.IsBetween(n1, n2)))
                        continue;

                    var edge = Edge.Between(n1, n2);
                    n1.NeighbourCache.Add(n2, edge);
                    Edges.Add(edge);
                }
            }
        }

        public void ConnectPathNodes() {
            for (int i = 0; i < Nodes.Count - 1; i++) {
                var edge = Edge.Between(Nodes[i], Nodes[i + 1]);
                Edges.Add(edge);
            }
        }

        public void Reset() {
            Nodes.ForEach(n => n.Visited = false);
        }

        public static GraphProblem FromText(string text) {
            GraphProblem graph = new();
            foreach (var line in text.Split(Environment.NewLine))
                ParseLine(graph, line);

            PostProcess(graph);
            graph.ConnectAllNodes();
            return graph;
        }

        public static GraphProblem FromFile(string path) {
            var graph = new GraphProblem();
            foreach (var line in File.ReadAllLines(path))
                ParseLine(graph, line);

            PostProcess(graph);
            return graph;
        }

        private static void ParseLine(GraphProblem graph, string line) {
            if (string.IsNullOrWhiteSpace(line))
                return;
            if (line.StartsWith("#"))
                return;

            var splitted = line.Split(",");

            if (splitted[0] == "n")
                graph.Nodes.Add(ParseNode(splitted));

            if (splitted[0] == "e")
                graph.Edges.Add(ParseEdge(splitted));
        }

        private static void PostProcess(GraphProblem graph) {
            foreach (var edge in graph.Edges) {
                edge.Node1 = graph.Nodes.Find(n => n.Index == edge.Node1Id);
                edge.Node2 = graph.Nodes.Find(n => n.Index == edge.Node2Id);

                edge.Node1.Edges.Add(edge);
                edge.Node2.Edges.Add(edge);
            }
        }

        public static Node ParseNode(string[] s) {
            return new Node {
                Index = int.Parse(s[1]),
                Position = new Vector2(float.Parse(s[2], CultureInfo.InvariantCulture),
                    float.Parse(s[3], CultureInfo.InvariantCulture))
            };
        }

        public static Edge ParseEdge(string[] s) {
            return new Edge {
                Node1Id = int.Parse(s[1]),
                Node2Id = int.Parse(s[2]),
                Value = double.Parse(s[3], CultureInfo.InvariantCulture)
            };
        }

        public Vector2 FindMax() {
            var position = new Vector2();
            foreach (var node in Nodes) {
                var maxX = Math.Max(position.X, node.Position.X);
                var maxY = Math.Max(position.Y, node.Position.Y);
                position = new Vector2(maxX, maxY);
            }

            return position;
        }

        public bool EqualPosition(Node node) => Nodes.Find(n => n.Position == node.Position) != null;

        public double CalcCosts() => Edges.Sum(e => e.Distance);
    }
}
