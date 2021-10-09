﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Algorithms;

namespace TravellingSalesmanProblem.Graph {
    public class GraphState {
        public List<Node> Nodes { get; set; } = new();
        public List<Node> Path { get; set; } = new();
        public List<Edge> PathEdges { get; set; } = new();
        public List<GraphSegment> Segments { get; set; } = new();
        public SwapInfo SwapInfo { get; set; }
        public bool Finished { get; set; } = false;
        public double Distance { get; set; }
        public double Temperature { get; set; }
        public int Iteration { get; set; }
        public Dictionary<string, string> Messages { get; set; } = new();
        public Dictionary<string, MathString> Equations { get; set; } = new();

        public GraphProblem ToGraphProblem() {
            return new GraphProblem {
                Nodes = Path,
                Edges = PathEdges
            };
        }

        public double CalcCosts() => PathEdges.Sum(e => e.Distance);

        public GraphState DeepCopy() {
            var state = new GraphState();
            state.Nodes.AddRange(Nodes);
            state.Path.AddRange(Path);
            state.PathEdges.AddRange(PathEdges);
            state.Segments.AddRange(Segments);
            state.SwapInfo = SwapInfo;
            state.Finished = Finished;
            state.Distance = Distance;
            state.Temperature = Temperature;
            state.Iteration = Iteration;
            state.Messages = Messages;
            state.Equations = Equations;

            return state;
        }

        public void ComparePathEdges(GraphState state) {
            //an edge was removed
            SetEdgeColors(state.PathEdges, PathEdges, "red");

            //an edge was added
            SetEdgeColors(PathEdges, state.PathEdges, "green");
        }

        private static void SetEdgeColors(List<Edge> current, List<Edge> compareAgainst, string color) {
            foreach (var edge in compareAgainst) {
                var condition = current.Find(e => e.IsEqual(edge)) == null;
                edge.Color = condition ? color : "black";
            }
        }
    }
}