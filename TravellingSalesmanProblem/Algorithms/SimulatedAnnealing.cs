using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class SimulatedAnnealing : Algorithm {
        public int MaxIter { get; set; } = 100_000;
        public double StartTemp { get; set; } = 10_000;
        public double MinTemp { get; set; } = 0.00001;
        public double Alpha { get; set; } = 0.999;

        private static readonly Random Random = new();

        public override IEnumerable<GraphState> FindPath(GraphProblem graph) {
            var problem = GraphProblem.OrderedGraphProblem(graph);

            var state = new GraphState() {
                Nodes = graph.Nodes,
                Path = problem.Nodes,
                PathEdges = problem.Edges,
                Distance = problem.CalcCosts(),
                Temperature = StartTemp
            };
            yield return state;

            //record initial tour as best so far
            double min = state.Distance;
            graph = problem;
            GraphProblem mintour = graph;

            int iteration = 0;
            for (; iteration < MaxIter; iteration++) {
                state.Iteration++;

                if (state.Temperature < MinTemp) {
                    state.Finished = true;
                    UpdateStateMessages(state);
                    yield return state;
                    yield break;
                }
                //randomly select a neighbour
                var neighbour = NeighbourState.DoubleBridgeFourOpt(graph);
                var neighbourCosts = neighbour.CalcCosts();

                //if neighbour is better, jump to it
                if (neighbourCosts < graph.CalcCosts()) {
                    graph = neighbour;

                    //check if tour is best so far
                    if (neighbourCosts < min) {
                        min = neighbourCosts;
                        mintour = neighbour;

                        state.Distance = graph.CalcCosts();
                        state.Path = graph.Nodes;
                        state.PathEdges = graph.Edges;
                        UpdateStateMessages(state);
                        yield return state;
                    }
                } else if (ExpCoinFlip(graph, mintour, state.Temperature)) {
                    //jump to neighbour even if it is worse
                    graph = neighbour;
                }
                //update temperature and iteration
                state.Temperature *= Alpha;
            }

            state.Distance = mintour.CalcCosts();
            state.Path = mintour.Nodes;
            state.PathEdges = mintour.Edges;
            state.Finished = true;
            UpdateStateMessages(state);
            yield return state;
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

        public override void UpdateStateMessages(GraphState state) {
            state.Messages["Iteration"] = state.Iteration.ToString();
            state.Messages["Distance"] = state.Distance.ToString();
            state.Messages["Temperature"] = state.Temperature.ToString();
        }
    }
}