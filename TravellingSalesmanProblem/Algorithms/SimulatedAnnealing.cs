using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class SimulatedAnnealing : Algorithm {
        public int PhaseLength { get; set; }
        public int MaxPhases { get; set; }
        public double StartTemp { get; set; }
        public double MinTemp { get; set; }
        public double Alpha { get; set; }
        public NeighbourType NeighbourEnum { get; set; }

        private GraphProblem X = new();
        private GraphProblem XBest = new();
        private GraphProblem Y = new();

        private static readonly Random Random = new();

        public override IEnumerable<GraphState> FindPath(GraphProblem graph) {
            graph.Reset();
            X = GraphProblem.OrderedGraphProblem(graph);

            var state = new GraphState {
                Nodes = X.Nodes,
                PathEdges = X.Edges,
                Temperature = CalculateInitialTemperature(X)
            };

            XBest = X;
            UpdateStateMessages(state);
            yield return state;
            
            while (state.Temperature >= MinTemp) {
                for (int i = 0; i < PhaseLength; i++) {
                    GenerateRandomNeighbour(state);

                    if (Y.Costs <= X.Costs) {
                        X = Y;

                        if (X.Costs < XBest.Costs) {
                            XBest = X;
                            yield return UpdateState(state);
                        }
                    } else if (MetropolisRule(state)) {
                        X = Y;
                    }
                }

                Equations["Temperature Update"] = MathString.UpdateTemperature(state, Alpha);
                state.Iteration++;
                state.Temperature *= Alpha;
            }

            state.Finished = true;
            yield return UpdateState(state);
        }

        private void GenerateRandomNeighbour(GraphState state) {
            Y = NeighbourState.Create(X, NeighbourEnum);
            state.SwapInfo = Y.SwapInfo;
            state.Segments = Y.Segments;
        }

        private static double CalculateInitialTemperature(GraphProblem graph) {            
            var nearestNeighbor = new NearestNeighbour { Start = graph.Nodes[Random.Next(0, graph.Nodes.Count)] };

            /* Calculate Best Nearest Neighbor Solution */
            var best = nearestNeighbor.MultiStart(graph).Last().Distance;

            /* Calculate Worst Nearest Neighbor Solution */
            var worst = nearestNeighbor.MultiStartLongest(graph).Last().Distance;

            return worst - best;
        }

        private bool MetropolisRule(GraphState state) {
            var r = Random.NextDouble();
            bool condition = Math.Exp(X.Costs - Y.Costs / state.Temperature) > r;

            Equations["Metropolis Rule"] = MathString.MetropolisRule(X, Y, state, r, condition);
            return condition;
        }

        public override IEnumerable<GraphState> MultiStart(GraphProblem graph) {
            throw new NotImplementedException();
        }

        private GraphState UpdateState(GraphState state) {
            state.Distance = XBest.Costs;
            state.Path = XBest.Nodes;
            state.PathEdges = XBest.Edges;
            state.Equations = Equations;

            UpdateStateMessages(state);
            return state;
        }

        public override void UpdateStateMessages(GraphState state) {
            state.Messages["Iteration"] = state.Iteration.ToString();
            state.Messages["Route"] = string.Join("-", state.Path.Select(n => n.Index));
            state.Messages["Distance"] = state.Distance.ToString();
            state.Messages["Temperature"] = state.Temperature.ToString();
        }
    }}