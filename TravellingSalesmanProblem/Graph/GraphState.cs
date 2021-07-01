using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravellingSalesmanProblem.Graph {
    public class GraphState {
        public List<Node> Nodes { get; set; }
        public List<Node> Path { get; set; } = new();
        public List<Edge> PathEdges { get; set; } = new();
        public bool Finished { get; set; }
        public bool Success { get; set; }
        public double Distance { get; set; }
        public double Temperature { get; set; }
        public int Iteration { get; set; }
        public Dictionary<string, string> Messages { get; set; } = new();
    }
}