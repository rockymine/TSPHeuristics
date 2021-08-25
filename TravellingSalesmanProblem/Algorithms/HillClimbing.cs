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
            Console.WriteLine("HillClimbing: FindPath(GraphProblem graph)");
            var x = GraphProblem.OrderedGraphProblem(graph);
            var y = new GraphProblem();

            var state = new GraphState {
                Nodes = graph.Nodes,
                Distance = double.MaxValue
            };

            yield return state;

            while (true) {
                switch (NeighbourType) {
                    case NeighbourType.Swap:
                        y = NeighbourState.Swap(x);
                        Console.WriteLine("Swap");
                        break;
                    case NeighbourType.TwoOpt:
                        y = NeighbourState.TwoOpt(x);
                        Console.WriteLine("TwoOpt");
                        break;
                    case NeighbourType.ThreeOpt:
                        y = NeighbourState.ThreeOpt(x);
                        Console.WriteLine("ThreeOpt");
                        break;
                    case NeighbourType.FourOpt:
                        y = NeighbourState.DoubleBridgeFourOpt(x);
                        Console.WriteLine("FourOpt");
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
            var best = new GraphState {
                Nodes = graph.Nodes
            };
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

            Console.WriteLine(string.Join("-", state.Path.Select(n => n.Index)));

            if (finished)
                state.Finished = true;

            UpdateStateMessages(state);
            return state;
        }
    }
}