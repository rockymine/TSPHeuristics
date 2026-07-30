using System;
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
        public SwapInfo SwapInfo { get; set; }

        /* Read several times per iteration by every heuristic; a plain loop over the cached edge
         * lengths avoids the LINQ enumerator and delegate allocation on that path. */
        public double Costs {
            get {
                var sum = 0d;
                for (int i = 0; i < Edges.Count; i++)
                    sum += Edges[i].Distance;

                return sum;
            }
        }

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

            /* A single re-roll still left coincident nodes (which render on top of each other and
             * give zero length edges), so keep trying. The attempt cap keeps this terminating when
             * maxNodes approaches the number of distinct integer positions available. */
            var taken = new HashSet<Vector2>();

            for (int i = 0; i < maxNodes; i++) {
                var node = RandomNode(i, maxX, maxY);

                for (int attempt = 0; attempt < 100 && taken.Contains(node.Position); attempt++)
                    node = RandomNode(i, maxX, maxY);

                taken.Add(node.Position);
                graph.Nodes.Add(node);
            }

            return ConnectedGraphProblem(graph);
        }

        public static GraphProblem ConnectedGraphProblem(GraphProblem graph) {
            graph.ConnectAllNodes();
            PostProcess(graph);
            return graph;
        }

        /* Builds the closed starting tour. This used to alias graph.Nodes and append the closing
         * node to it, so the caller's graph permanently grew a duplicate node: the distance matrix
         * gained a bogus row, "Download Source" emitted n+1 nodes, and a Nearest Neighbour run
         * after a Hill Climbing run walked the duplicate. The node list is copied instead. */
        public static GraphProblem OrderedGraphProblem(GraphProblem graph) {
            var path = new List<Node>(graph.Nodes);
            if (path.Count > 0 && path[0] != path[^1])
                path.Add(path[0]);

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

        /* Walking the upper triangle yields each pair exactly once, so the Edges.Any() duplicate
         * check that made this O(n^4) is unnecessary. Building a 90 node instance took 193 ms. */
        public void ConnectAllNodes() {
            var existing = Edge.KeySet(Edges);

            for (int i = 0; i < Nodes.Count; i++) {
                for (int j = i + 1; j < Nodes.Count; j++) {
                    var n1 = Nodes[i];
                    var n2 = Nodes[j];

                    if (n1 == n2)
                        continue;
                    if (existing.Contains(Edge.KeyOf(n1, n2)))
                        continue;

                    Edges.Add(Edge.Between(n1, n2));
                }
            }
        }

        public void ConnectPathNodes() {
            Edges.Clear();
            for (int i = 0; i < Nodes.Count - 1; i++)
                Edges.Add(Edge.Between(Nodes[i], Nodes[i + 1], false));
        }

        public void Reset() {
            Nodes.ForEach(n => n.Visited = false);
            Edges.ForEach(e => e.Pheromone = 0);
        }

        /* Splitting on Environment.NewLine alone meant the accepted line ending depended on the
         * host: under WebAssembly it is "\n", so a CRLF file kept a trailing "\r" on every value
         * and only parsed by luck (char.IsWhiteSpace covers '\r' inside float.Parse). Accept both. */
        public static GraphProblem FromText(string text) {
            GraphProblem graph = new();
            foreach (var line in text.Split('\n'))
                ParseLine(graph, line.Trim('\r'));

            graph.ConnectAllNodes();
            PostProcess(graph);
            return graph;
        }

        public static GraphProblem FromFile(string path) {
            var graph = new GraphProblem();
            foreach (var line in File.ReadAllLines(path))
                ParseLine(graph, line);

            /* FromText always returns a complete graph; FromFile silently returned an edgeless one
             * for node-only files, which the heuristics cannot traverse. */
            if (graph.Edges.Count == 0)
                graph.ConnectAllNodes();

            PostProcess(graph);
            return graph;
        }

        private static void ParseLine(GraphProblem graph, string line) {
            if (string.IsNullOrWhiteSpace(line))
                return;
            if (line.StartsWith("#"))
                return;

            var splitted = line.Split(",");

            /* A short line used to throw IndexOutOfRangeException from deep inside the parser.
             * Reject it here so callers see a single, predictable FormatException. */
            if (splitted.Length < 4)
                throw new FormatException($"Expected 4 comma separated fields, got {splitted.Length}: '{line}'");

            if (splitted[0] == "n")
                graph.Nodes.Add(ParseNode(splitted));

            if (splitted[0] == "e")
                graph.Edges.Add(ParseEdge(splitted));
        }

        public static string SerializeGraph(GraphProblem graph) {
            var sb = new StringBuilder();
            sb.AppendLine($"#autogenerated: n = {graph.Nodes.Count}");

            foreach (var node in graph.Nodes) {
                sb.Append("n,").Append(node.Index).Append(",")
                    .Append(node.Position.X.ToString(CultureInfo.InvariantCulture))
                    .Append(",").Append(node.Position.Y.ToString(CultureInfo.InvariantCulture)).AppendLine();
            }

            foreach (var edge in graph.Edges) {
                sb.Append("e,").Append(edge.Node1Id).Append(",")
                    .Append(edge.Node2Id).Append(",").Append(edge.Value).AppendLine();
            }

            return sb.ToString();
        }

        /* Relinks edges to nodes by index. The two Nodes.Find() calls per edge made this O(V * E),
         * i.e. O(n^3) on a complete graph; an index map turns each lookup into O(1). The per-node
         * duplicate check is likewise a set lookup instead of a scan of the adjacency list. */
        public static void PostProcess(GraphProblem graph) {
            var byIndex = new Dictionary<int, Node>(graph.Nodes.Count);
            foreach (var node in graph.Nodes)
                byIndex[node.Index] = node;

            var registered = new Dictionary<Node, HashSet<(int, int)>>();

            foreach (var edge in graph.Edges) {
                if (edge == null)
                    continue;

                edge.Node1 = byIndex.GetValueOrDefault(edge.Node1Id);
                edge.Node2 = byIndex.GetValueOrDefault(edge.Node2Id);

                Register(registered, edge.Node1, edge);
                Register(registered, edge.Node2, edge);
            }
        }

        private static void Register(Dictionary<Node, HashSet<(int, int)>> registered, Node node, Edge edge) {
            if (node == null)
                return;

            if (!registered.TryGetValue(node, out var keys)) {
                keys = Edge.KeySet(node.Edges);
                registered[node] = keys;
            }

            if (keys.Add(edge.Key))
                node.Edges.Add(edge);
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

        public Vector2 FindMin() {
            var x = Nodes.Min(n => n.Position.X);
            var y = Nodes.Min(n => n.Position.Y);
            return new Vector2(x, y);
        }

        public Vector2 FindMax() {
            var x = Nodes.Max(n => n.Position.X);
            var y = Nodes.Max(n => n.Position.Y);
            return new Vector2(x, y);
        }

        public bool EqualPosition(Node node) => Nodes.Find(n => n.Position == node.Position) != null;
    }
}

public enum GraphSource {
    Text,
    Random,
    File
}