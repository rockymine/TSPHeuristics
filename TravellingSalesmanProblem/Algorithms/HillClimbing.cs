using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class HillClimbing : Algorithm {
        public NeighbourType NeighbourType { get; set; }
        private GraphProblem CurrentBest = new();
        public override IEnumerable<GraphState> FindPath(GraphProblem graph) {
            var x = GraphProblem.OrderedGraphProblem(graph);
            var y = new GraphProblem();
            var state = new GraphState {
                Nodes = graph.Nodes,
                Path = x.Nodes,
                PathEdges = x.Edges,
                Distance = double.MaxValue
            };

            yield return state;

            while (true) {
                switch (NeighbourType) {
                    case NeighbourType.Swap:
                        y = NeighbourState.Swap(x);
                        break;
                    case NeighbourType.TwoOpt:
                        y = NeighbourState.TwoOptFull(x);
                        break;
                    case NeighbourType.ThreeOpt:
                        y = NeighbourState.ThreeOpt(x);
                        break;
                    case NeighbourType.FourOpt:
                        y = NeighbourState.DoubleBridgeFourOpt(x);
                        break;
                }

                if (y.Costs >= state.Distance) {
                    state.Finished = true;
                    UpdateStateMessages(state);
                    yield break;
                } 

                x = y;
                CurrentBest = y;
                yield return UpdateState(state);
            }            
        }

        public override IEnumerable<GraphState> MultiStart(GraphProblem graph) {
            var best = new GraphState { Nodes = graph.Nodes };
            var costs = double.MaxValue;

            for (int i = 0; i < graph.Nodes.Count; i++) {
                var state = FindPath(graph).Last();

                if (state.CalcCosts() < costs) {
                    costs = state.CalcCosts();
                    best.Path = state.Path;
                    best.PathEdges = state.PathEdges;
                    best.Distance = state.Distance;
                    yield return UpdateState(best);
                }
            }
        }

        public override void UpdateStateMessages(GraphState state) {
            state.Messages["Iteration"] = state.Iteration.ToString();
            state.Messages["Route"] = string.Join("-", state.Path.Select(n => n.Index));
            state.Messages["Distance"] = state.Distance.ToString();
        }

        private GraphState UpdateState(GraphState state, bool finished = false) {
            state.Iteration++;
            state.Distance = CurrentBest.Costs;
            state.Path = CurrentBest.Nodes;
            state.PathEdges = CurrentBest.Edges;

            if (finished)
                state.Finished = true;

            UpdateStateMessages(state);
            return state;
        }
    }
}