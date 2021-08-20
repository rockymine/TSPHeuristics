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