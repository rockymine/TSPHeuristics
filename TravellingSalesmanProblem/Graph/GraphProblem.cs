﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Globalization;
using System.IO;
using TravellingSalesmanProblem.Algorithms;
using Newtonsoft.Json;

namespace TravellingSalesmanProblem.Graph {
    public class GraphProblem {
        public List<Node> Nodes { get; set; } = new();
        public List<Edge> Edges { get; set; } = new();
        public List<GraphSegment> Segments { get; set; } = new();
        public SwapInfo SwapInfo { get; set; }
        public double Costs => Edges.Sum(e => e.Distance);
        private static readonly Random Random = new();
        public GraphProblem() {
            Nodes = new List<Node>();
            Edges = new List<Edge>();
        }

        public GraphProblem DeepCopy() {
            var graph = new GraphProblem {
                SwapInfo = SwapInfo?.DeepCopy(),
                Nodes = Nodes.Select(n => n.Copy()).ToList(),
                Edges = Edges.Select(e => e.Copy()).ToList()
            };

            PostProcess(graph);
            return graph;
        }

        public static GraphProblem RandomGraphProblem(int maxX, int maxY, int maxNodes) {
            var graph = new GraphProblem();

            for (int i = 0; i < maxNodes; i++) {
                var node = RandomNode(i, maxX, maxY);

                if (graph.EqualPosition(node))
                    node = RandomNode(i, maxX, maxY);

                graph.Nodes.Add(node);
            }
            
            return ConnectedGraphProblem(graph);
        }

        public static GraphProblem ConnectedGraphProblem(GraphProblem graph) {
            graph.ConnectAllNodes();
            PostProcess(graph);
            return graph;
        }

        public static GraphProblem OrderedGraphProblem(GraphProblem graph) {
            var path = graph.Nodes;
            if (path[0] != path.Last())
                path.Add(graph.Nodes[0]);

            var ordered = new GraphProblem { Nodes = path };
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
                    Edges.Add(edge);
                }
            }
        }

        public void ConnectPathNodes() {
            Edges.Clear();
            for (int i = 0; i < Nodes.Count - 1; i++) {
                var edge = Edge.Between(Nodes[i], Nodes[i + 1]);
                Edges.Add(edge);
            }
        }

        public void Reset() {
            Nodes.ForEach(n => n.Visited = false);
            Edges.ForEach(e => e.Pheromone = 0);
        }

        public static GraphProblem FromText(string text) {
            GraphProblem graph = new();
            foreach (var line in text.Split(Environment.NewLine))
                ParseLine(graph, line);
            
            graph.ConnectAllNodes();
            PostProcess(graph);
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

        public static void PostProcess(GraphProblem graph) {
            foreach (var edge in graph.Edges) {
                if (edge != null) {
                    edge.Node1 = graph.Nodes.Find(n => n.Index == edge.Node1Id);
                    edge.Node2 = graph.Nodes.Find(n => n.Index == edge.Node2Id);

                    if (edge.Node1 != null) {
                        var node1edges = edge.Node1.Edges;
                        if (!node1edges.Any(e => e.IsEqual(edge))) {
                            node1edges.Add(edge);
                        }
                    }

                    if (edge.Node2 != null) {
                        var node2edges = edge.Node2.Edges;
                        if (!node2edges.Any(e => e.IsEqual(edge))) {
                            node2edges.Add(edge);
                        }
                    }
                }
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
            var x = Nodes.Max(n => n.Position.X);
            var y = Nodes.Max(n => n.Position.Y);
            return new Vector2(x, y);
        }

        public bool EqualPosition(Node node) => Nodes.Find(n => n.Position == node.Position) != null;
    }
}