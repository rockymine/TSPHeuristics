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
        public NeighbourType NeighbourEnum { get; set; }

        private GraphProblem CurrentBest = new();

        private static readonly Random Random = new();

        public override IEnumerable<GraphState> FindPath(GraphProblem graph) {
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
                /* Create a neighbor y from N(x) */
                var y = CreateNeighbourSolution(x);
                state.Segments = y.Segments;

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

                var tu = "Temperature Update";
                Equations[tu] = new("$T_{k+1} = f(T(k)) = \\alpha \\cdot T_{k}$") {
                    Dummy = "$T_{?k+1?} = ?alpha? \\cdot ?current?$",
                    Variables = new Dictionary<string, object> {
                        { "k+1", state.Iteration + 1},
                        { "current", state.Temperature},
                        { "alpha", Alpha}
                    }
                };
                state.Iteration++;
                state.Temperature *= Alpha;

                Equations[tu].Result = $"$T_{{{state.Iteration}}} = {state.Temperature}$";
            }

            yield return UpdateState(state, true);
        }

        private GraphProblem CreateNeighbourSolution(GraphProblem x) {
            var y = new GraphProblem();
            switch (NeighbourEnum) {
                case NeighbourType.Swap:
                    y = NeighbourState.Swap(x);
                    break;
                case NeighbourType.TwoOpt:
                    y = NeighbourState.TwoOpt(x);
                    break;
                case NeighbourType.ThreeOpt:
                    y = NeighbourState.ThreeOpt(x);
                    break;
                case NeighbourType.FourOpt:
                    y = NeighbourState.DoubleBridgeFourOpt(x);
                    break;
            }

            return y;
        }

        private bool MetropolisRule(GraphProblem x, GraphState state) {
            var p = Math.Exp(-(x.Costs - state.Distance) / state.Temperature);
            var r = Random.NextDouble();
            bool condition = p > r;

            var mp = "Metropolis Rule";
            Equations[mp] = new("$\\text{exp}(\\frac{f(x) - f(y)}{T_{k}}) > \\text{rand}(0,1)$") {
                Dummy = "$\\text{exp}(\\frac{?f(x)? - ?f(y)?}{?temp?}) > ?rand?$",
                Variables = new Dictionary<string, object> {
                    { "f(x)", x.Costs },
                    { "f(y)", state.Distance },
                    { "temp", state.Temperature },
                    { "rand", r }
                },
                Result = $"$\\text{{{condition}}}$"
            };

            if (condition)
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
            state.Equations = Equations;

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