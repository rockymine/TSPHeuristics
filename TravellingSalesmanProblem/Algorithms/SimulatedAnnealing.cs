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

        public override LinkedList<GraphState> FindPath(GraphProblem graph) {
            graph.Reset();
            var history = new LinkedList<GraphState>();
            X = GraphProblem.OrderedGraphProblem(graph);

            var state = new GraphState {
                Nodes = X.Nodes,
                PathEdges = X.Edges,
                Temperature = CalculateInitialTemperature(X)
            };

            XBest = X;
            UpdateStateMessages(state);
            history.AddLast(state);
            var iteration = 0;
            var temperature = state.Temperature;

            while (temperature >= MinTemp) {
                for (int i = 0; i < PhaseLength; i++) {
                    Y = NeighbourState.Create(X, NeighbourEnum);

                    if (Y.Costs <= X.Costs) {
                        X = Y;

                        if (X.Costs < XBest.Costs) {
                            XBest = X;
                            history.AddLast(AdvanceState(history.Last.Value, iteration, temperature));
                            //yield return UpdateState(state);
                        }
                    } else if (MetropolisRule(history.Last.Value)) {
                        X = Y;
                    }
                }

                Equations["Temperature Update"] = MathString.UpdateTemperature(history.Last.Value, Alpha);
                iteration++;
                temperature *= Alpha;
            }

            return history;
        }

        private static double CalculateInitialTemperature(GraphProblem graph) {            
            var nearestNeighbor = new NearestNeighbour { Start = graph.Nodes[Random.Next(0, graph.Nodes.Count)] };
            var best = nearestNeighbor.MultiStart(graph).Last().Distance;
            var worst = nearestNeighbor.MultiStart(graph, false).Last().Distance;

            return worst - best;
        }

        private bool MetropolisRule(GraphState state) {
            var r = Random.NextDouble();
            bool condition = Math.Exp(X.Costs - Y.Costs / state.Temperature) > r;

            Equations["Metropolis Rule"] = MathString.MetropolisRule(X, Y, state, r, condition);
            return condition;
        }

        private GraphState AdvanceState(GraphState state, int iteration, double temperature) {
            var newState = state.DeepCopy();

            newState.Distance = XBest.Costs;
            newState.Path = XBest.Nodes;
            newState.PathEdges = XBest.Edges;
            newState.Equations = Equations;
            newState.SwapInfo = XBest.SwapInfo;
            //newState.Path.Clear();
            //newState.Path.AddRange(XBest.Nodes);
            //newState.PathEdges.Clear();
            //newState.PathEdges.AddRange(XBest.Edges);
            //newState.Equations = Equations?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DeepCopy());
            newState.Temperature = temperature;
            newState.Iteration = iteration;
            //newState.SwapInfo = Y.SwapInfo?.DeepCopy();
            //newState.Segments = Y.Segments;

            UpdateStateMessages(newState);
            return newState;
        }

        public override void UpdateStateMessages(GraphState state) {
            state.Messages["Iteration"] = state.Iteration.ToString();
            state.Messages["Route"] = string.Join("-", state.Path.Select(n => n.Index));
            state.Messages["Distance"] = state.Distance.ToString();
            state.Messages["Temperature"] = state.Temperature.ToString();
        }
    }}