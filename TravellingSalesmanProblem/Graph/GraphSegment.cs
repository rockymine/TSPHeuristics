using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravellingSalesmanProblem.Graph {
    public class GraphSegment {
        public string Identifier { get; set; }
        public SegmentType Type { get; set; }
        public List<Node> Nodes => GetNodes();
        public List<Edge> Edges { get; set; } = new();
        public Dictionary<string, string> Info { get; set; } = new();

        public GraphSegment(string identifier, SegmentType type, Edge[] edges) {
            Identifier = identifier;
            Type = type;
            Edges = edges.ToList();
        }

        public static List<GraphSegment> Split(List<Edge> edges, int i, int j) {
            var n = edges.Count;
            var array = edges.ToArray();            

            var first = new GraphSegment("Seg. A: ", SegmentType.Normal, array[0..i]);
            var second = new GraphSegment("Seg. B': ", SegmentType.Reversed, array[i..j]);
            var third = new GraphSegment("Seg. C: ", SegmentType.Normal, array[j..n]);

            return new List<GraphSegment> {
                first, second, third
            };
        }

        private List<Node> GetNodes() {
            GenerateInfo();
            var nodes = new List<Node>();
            foreach (var edge in Edges) {
                if (!nodes.Contains(edge.Node1))
                    nodes.Add(edge.Node1);
                if (!nodes.Contains(edge.Node2))
                    nodes.Add(edge.Node2);
            }
            return nodes;
        }

        private void GenerateInfo() {
            Info["Edges"] = string.Join(',', Edges.Select(n => "(" + n.Node1.Index + "<->" + n.Node2.Index + ")"));
            Info["Distance"] = Edges.Sum(e => e.Distance).ToString();
        }
    }

    public enum SegmentType {
        Normal = 0,
        Reversed = 1
    }
}