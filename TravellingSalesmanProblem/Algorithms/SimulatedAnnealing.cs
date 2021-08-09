using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class SimulatedAnnealing : Algorithm {
        public int MaxIter { get; set; } = 100_000;
        public int MaxIter1 { get; set; } = 10;
        public int MaxIter2 { get; set; } = 150;
        public double StartTemp { get; set; } = 100;
        public double MinTemp { get; set; } = 0.00001;
        public double Alpha { get; set; } = 0.95;
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

            int i = 0;
            for (; i < 400; i++) {
                state.Iteration++;
                /* TODO: If necessary, break when a certain temperature is reached */

                /* MaxIter1 stands for the maximum amount of iterations without temperature change */
                for (int j = 0; j < 10; j++) {
                    /* Create a neighbor y from N(x) and check if it is better than x */
                    GraphProblem y = NeighbourState.TwoOpt(x);
                    if (y.Costs <= x.Costs) {
                        x = y;

                        /* update current best tour */
                        if (x.Costs < CurrentBest.Costs) {
                            CurrentBest = x;
                            yield return UpdateState(state);
                        }

                    } else if (ExpCoinFlip(graph, state.ToGraphProblem(), state.Temperature)) {
                        x = y;
                    }
                }
                state.Temperature *= Alpha;
            }

            yield return UpdateState(state, true);
        }

        private static bool ExpCoinFlip(GraphProblem x, GraphProblem y, double t) {
            var p = Math.Exp(-(x.CalcCosts() - y.CalcCosts()) / t);
            var r = Random.NextDouble();

            if (p > r)
                return true;

            return false;
        }

        public override IEnumerable<GraphState> MultiStart(GraphProblem graph) {
            throw new NotImplementedException();
        }

        private GraphState UpdateState(GraphState state, bool finished = false) {
            state.Distance = CurrentBest.CalcCosts();
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