using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class HillClimbing : Algorithm {
        public NeighbourType NeighbourEnum { get; set; }
        public DescentType DescentType { get; set; }
        private GraphProblem X = new();
        private GraphProblem XBest = new();
        private GraphProblem Y = new();
        public override LinkedList<GraphState> FindPath(GraphProblem graph) {
            graph.Reset();
            var history = new LinkedList<GraphState>();
            X = GraphProblem.OrderedGraphProblem(graph);

            var state = new GraphState {
                Nodes = X.Nodes,
                PathEdges = X.Edges,
                Distance = double.MaxValue
            };

            XBest = X;
            var iteration = 0;
            history.AddLast(AdvanceState(state, iteration));

            while (true) {
                //Console.WriteLine("New Hill Climbing iteration.");
                Y = NeighbourState.Create(X, NeighbourEnum, DescentType);

                if (Y.Costs >= X.Costs)
                    break;

                X = Y;
                XBest = X;
                iteration++;
                history.AddLast(AdvanceState(history.Last.Value, iteration));
            }

            return history;
        }

        //public IEnumerable<GraphState> MultiStart(GraphProblem graph) {
        //    var best = new GraphState { Nodes = graph.Nodes };
        //    var costs = double.MaxValue;

        //    for (int i = 0; i < graph.Nodes.Count; i++) {
        //        var state = FindPath(graph).Last();

        //        if (state.CalcCosts() < costs) {
        //            costs = state.CalcCosts();
        //            best.Path = state.Path;
        //            best.PathEdges = state.PathEdges;
        //            best.Distance = state.Distance;
        //            yield return AdvanceState(best);
        //        }
        //    }
        //}

        public override void UpdateStateMessages(GraphState state) {
            state.Messages["Iteration"] = state.Iteration.ToString();
            state.Messages["Route"] = string.Join("-", state.Path.Select(n => n.Index));
            state.Messages["Distance"] = state.Distance.ToString();
        }

        private GraphState AdvanceState(GraphState state, int iteration) {
            var newState = state.DeepCopy();

            //newState.Path.Clear();
            //newState.Path.AddRange(XBest.Nodes);
            //newState.PathEdges.Clear();
            //newState.PathEdges.AddRange(XBest.Edges);
            newState.Distance = XBest.Costs;
            newState.Path = XBest.Nodes;
            newState.PathEdges = XBest.Edges;
            newState.SwapInfo = XBest.SwapInfo;
            newState.Iteration = iteration;

            UpdateStateMessages(newState);
            return newState;
        }
    }
}