using ChartData;
using System;
using System.Collections.Generic;
using System.Linq;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class SimulatedAnnealing : Algorithm {
        public int PhaseLength { get; set; }
        public int MaxPhases { get; set; }
        public double StartTemp { get; set; }
        public double MinTemp { get; set; }
        public double Alpha { get; set; }
        public NeighbourType NeighbourEnum { get; set; }
        public bool CalculateTemperature { get; set; }

        private GraphProblem X = new();
        private GraphProblem XBest = new();
        private GraphProblem Y = new();

        private static readonly Random Random = new();

        public override LinkedList<GraphState> FindPath(GraphProblem graph) {
            graph.Reset();
            var history = new LinkedList<GraphState>();
            X = GraphProblem.OrderedGraphProblem(graph);

            var distanceInfo = new ChartInfo {
                Title = "Distance Progress",
                XAxis = new ChartSet { Title = "Iteration" },
                YAxis = new List<ChartSet> {
                    new ChartSet { Title = "Distance" }
                }
            };

            var temperatureInfo = distanceInfo.DeepCopy();
            temperatureInfo.Title = "Temperature Progress";
            temperatureInfo.YAxis[0].Title = "Temperature";

            var state = new GraphState {
                Nodes = X.Nodes,
                PathEdges = X.Edges,
                Temperature = CalculateTemperature ? CalculateInitialTemperature(X) : StartTemp,
                ChartInfo = new List<ChartInfo>() { temperatureInfo, distanceInfo }
            };

            XBest = X;
            var iteration = 0;
            var temperature = state.Temperature;
            history.AddLast(AdvanceState(state, iteration, temperature));

            while (temperature >= MinTemp) {
                for (int i = 0; i < PhaseLength; i++) {
                    Y = NeighbourState.Create(X, NeighbourEnum, DescentType.Random);

                    if (Y.Costs <= X.Costs) {
                        X = Y;

                        if (X.Costs < XBest.Costs) {
                            XBest = X;
                            history.AddLast(AdvanceState(history.Last.Value, iteration, temperature));
                        }
                    } else if (MetropolisRule(history.Last.Value)) {
                        X = Y;
                    }
                }

                Equations["Temperature Update"] = MathString.UpdateTemperature(history.Last.Value, Alpha);

                iteration++;
                temperature *= Alpha;

                history.Last.Value.ChartInfo[0].XAxis.Add(iteration, "red");
                history.Last.Value.ChartInfo[0].YAxis[0].Add(temperature, "red");

                history.Last.Value.ChartInfo[1].XAxis.Add(iteration, "red");
                history.Last.Value.ChartInfo[1].YAxis[0].Add(X.Costs, "red");
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
            bool condition = r < Math.Exp(-(Y.Costs - X.Costs) / state.Temperature);

            Equations["Metropolis Rule"] = MathString.MetropolisRule(X, Y, state, r, condition);
            return condition;
        }

        private GraphState AdvanceState(GraphState state, int iteration, double temperature) {
            var newState = state.DeepCopy();

            newState.Distance = XBest.Costs;
            newState.Path = XBest.Nodes;
            newState.PathEdges = XBest.Edges;
            newState.Equations = Equations;
            newState.SwapInfo = XBest.SwapInfo?.DeepCopy();
            newState.Equations = Equations?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DeepCopy());
            newState.Temperature = temperature;
            newState.Iteration = iteration;

            for (int i = 0; i < newState.ChartInfo[0].YAxis[0].Values.Count; i++) {
                newState.ChartInfo[0].YAxis[0].Colors[i] = (i == iteration - 1) ? "blue" : "red";
            }

            UpdateStateMessages(newState);
            return newState;
        }

        public override void UpdateStateMessages(GraphState state) {
            state.Messages["Iteration"] = state.Iteration.ToString();
            state.Messages["Route"] = string.Join("-", state.Path.Select(n => n.Index));
            state.Messages["Distance"] = Math.Round(state.Distance, 3).ToString();
            state.Messages["Temperature"] = state.Temperature.ToString();
        }
    }}