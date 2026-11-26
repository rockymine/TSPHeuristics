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
            initialTemperature = temperature;
            lastUphill = null;
            history.AddLast(AdvanceState(state, iteration, temperature));

            while (temperature >= MinTemp) {
                phase = iteration;
                for (int i = 0; i < PhaseLength; i++) {
                    Y = NeighbourState.Create(X, NeighbourEnum, DescentType.Random);

                    if (Y.Costs <= X.Costs) {
                        var fx = X.Costs;
                        X = Y;

                        if (X.Costs < XBest.Costs) {
                            XBest = X;
                            history.AddLast(AdvanceState(history.Last.Value, iteration, temperature,
                                Explain(fx, iteration, temperature)));
                        }
                    } else if (MetropolisRule(temperature)) {
                        X = Y;
                    }
                }

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

        /* The acceptance probability has to use the temperature of the phase being run. It used to
         * read history.Last.Value.Temperature, and a history entry is only appended when a new best
         * tour is found - so the schedule cooled while the acceptance test stayed frozen at
         * whatever the temperature was when the last improvement happened. With a long plateau the
         * search kept accepting uphill moves at the initial temperature and never converged. */
        private bool MetropolisRule(double temperature) {
            var r = Random.NextDouble();
            bool condition = r < Math.Exp(-(Y.Costs - X.Costs) / temperature);

            lastUphill = (phase, X.Costs, Y.Costs, temperature, r);
            return condition;
        }

        /* The rules behind a new best tour, in the order they happened: the move (shorter, so taken
         * without a draw), the last longer tour tested since the previous state, and the cooling at
         * the end of this phase. These used to be whatever the Equations dictionary held - the
         * Metropolis line of some later draw next to the temperature update of an earlier state,
         * so two different temperatures sat side by side and the order flipped between states. */
        private int phase;
        private double initialTemperature;
        private (int Phase, double Fx, double Fy, double T, double R)? lastUphill;

        private List<Derivation> Explain(double fx, int iteration, double temperature) {
            var cards = new List<Derivation>();
            var move = Derivations.Move(XBest.MoveInfo, fx, Derivations.MoveContext(XBest.MoveInfo),
                "A shorter neighbour is always taken, without a random draw. This one is also the best tour so far.");
            if (move != null)
                cards.Add(move);

            if (lastUphill is { } u) {
                cards.Add(Derivations.Metropolis(u.Phase, u.Fx, u.Fy, u.T, u.R,
                    $"last longer tour tested, phase {u.Phase}"));
                lastUphill = null;
            }

            cards.Add(Derivations.Cooling(iteration, temperature, Alpha, initialTemperature, MinTemp, PhaseLength));
            return cards;
        }

        private GraphState AdvanceState(GraphState state, int iteration, double temperature, List<Derivation> derivations = null) {
            var newState = state.DeepCopy();

            newState.Distance = XBest.Costs;
            newState.Path = XBest.Nodes;
            newState.PathEdges = XBest.Edges;
            newState.MoveInfo = XBest.MoveInfo?.DeepCopy();
            newState.Derivations = derivations ?? new List<Derivation>();
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
            /* Four significant digits: the schedule spans ten orders of magnitude, so a fixed number of
             * decimals either prints sixteen digits early on or rounds to 0 late in the run. */
            state.Messages["Temperature"] = state.Temperature.ToString("G4");
        }
    }}