using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravellingSalesmanProblem.Graph {
    public class GraphState {
        public List<Node> Nodes { get; set; } = new();
        public List<Node> Path { get; set; } = new();
        public List<Edge> PathEdges { get; set; } = new();
        public bool Finished { get; set; } = false;
        public bool Success { get; set; }
        public double Distance { get; set; }
        public double Temperature { get; set; }
        public int Iteration { get; set; }
        public Dictionary<string, string> Messages { get; set; } = new();

        public GraphProblem ToGraphProblem() {
            return new GraphProblem {
                Nodes = Nodes,
                Edges = PathEdges
            };
        }

        public double CalcCosts() => PathEdges.Sum(e => e.Distance);

        public GraphState DeepCopy() {
            var state = new GraphState();
            state.Nodes.AddRange(Nodes);
            state.Path.AddRange(Path);
            state.PathEdges.AddRange(PathEdges);
            state.Finished = Finished;
            state.Success = Success;
            state.Distance = Distance;
            state.Temperature = Temperature;
            state.Iteration = Iteration;
            state.Messages = Messages;
            return state;
        }

        public void ComparePathEdges(GraphState state) {
            //if "state" does not have edge of THIS color edge of "THIS" red
            SetEdgeColors(state.PathEdges, PathEdges, "red");

            //if "state" has edge that is not in THIS color edge of "state" green
            SetEdgeColors(PathEdges, state.PathEdges, "green");
        }

        private static void SetEdgeColors(List<Edge> current, List<Edge> compareAgainst, string color) {
            foreach (var edge in compareAgainst) {
                var other = current.Find(e => e == edge);
                if (other == null)
                    other.Color = color;
            }
        }
    }
}