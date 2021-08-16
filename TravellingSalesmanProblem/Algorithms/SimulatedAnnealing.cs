using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class SimulatedAnnealing : Algorithm {
        public int MaxIter1 { get; set; }
        public int MaxIter2 { get; set; }
        public double StartTemp { get; set; }
        public double MinTemp { get; set; }
        public double Alpha { get; set; }
        private GraphProblem CurrentBest = new();

        private static readonly Random Random = new();

        public override IEnumerable<GraphState> FindPath(GraphProblem graph) {
            /* TODO: Create tour using tour constructive heuristic.
             This tour will be improved by the Simulated Annealing heuristic. */
            var x = GraphProblem.OrderedGraphProblem(graph);
            CurrentBest = x;

            var state = new GraphState {
                Nodes = graph.Nodes,
                Path = x.Nodes,
                PathEdges = x.Edges,
                Distance = x.Costs,
                Temperature = StartTemp
            };
            yield return state;
            
            while (state.Temperature >= MinTemp) {
                /* Create a neighbor y from N(x) and check if it is better than x */
                var y = NeighbourState.TwoOpt(x);
                if (y.Costs <= x.Costs) {
                    x = y;

                    /* update current best tour */
                    if (x.Costs < CurrentBest.Costs) {
                        CurrentBest = x;
                        yield return UpdateState(state);
                    }

                } else if (MetropolisRule(graph, state)) {
                    x = y;
                }
                state.Iteration++;
                state.Temperature *= Alpha;
            }

            yield return UpdateState(state, true);
        }

        private static bool MetropolisRule(GraphProblem x, GraphState s) {
            var p = Math.Exp(-(x.Costs - s.Distance) / s.Temperature);
            var r = Random.NextDouble();

            if (p > r)
                return true;

            return false;
        }

        public override IEnumerable<GraphState> MultiStart(GraphProblem graph) {
            throw new NotImplementedException();
        }

        private GraphState UpdateState(GraphState state, bool finished = false) {
            state.Distance = CurrentBest.Costs;
            state.Path = CurrentBest.Nodes;
            state.PathEdges = CurrentBest.Edges;

            if (finished)
                state.Finished = true;

            UpdateStateMessages(state);
            return state;
        }

        public override void UpdateStateMessages(GraphState state) {
            state.Messages["Iteration"] = state.Iteration.ToString();
            state.Messages["Route"] = string.Join("-", state.Path.Select(n => n.Index));
            state.Messages["Distance"] = state.Distance.ToString();
            state.Messages["Temperature"] = state.Temperature.ToString();
        }
    }
}