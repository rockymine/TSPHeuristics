﻿using System;
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

        private GraphProblem CurrentBest = new();

        private static readonly Random Random = new();

        public override IEnumerable<GraphState> FindPath(GraphProblem graph) {
            var x = GraphProblem.OrderedGraphProblem(graph.DeepCopy());
            var state = new GraphState {
                Nodes = x.Nodes,
                PathEdges = x.Edges,
                Temperature = CalculateInitialTemperature(x)
            };

            CurrentBest = x;
            UpdateStateMessages(state);
            yield return state;
            
            while (state.Temperature >= MinTemp) {
                for (int i = 0; i < PhaseLength; i++) {
                    /* Generate Neighbor */
                    x.Reset();
                    var y = NeighbourState.Create(x, NeighbourEnum);
                    state.SwapInfo = y.SwapInfo;
                    state.Segments = y.Segments;

                    if (y.Costs <= x.Costs) {
                        x = y;

                        /* Update Current Best */
                        if (x.Costs < CurrentBest.Costs) {
                            CurrentBest = x;
                            yield return UpdateState(state);
                        }

                    } else if (MetropolisRule(y, state)) {
                        x = y;
                    }
                }

                Equations["Temperature Update"] = MathString.UpdateTemperature(state, Alpha);
                state.Iteration++;
                state.Temperature *= Alpha;                
            }

            state.Finished = true;
            UpdateStateMessages(state);
            //yield return UpdateState(state, true);
        }

        private static double CalculateInitialTemperature(GraphProblem graph) {            
            var nearestNeighbor = new NearestNeighbour { Start = graph.Nodes[Random.Next(0, graph.Nodes.Count)] };

            /* Calculate Best Nearest Neighbor Solution */
            var best = nearestNeighbor.MultiStart(graph).Last().Distance;

            /* Calculate Worst Nearest Neighbor Solution */
            var worst = nearestNeighbor.MultiStartLongest(graph).Last().Distance;

            return worst - best;
        }

        private bool MetropolisRule(GraphProblem x, GraphState state) {
            var p = Math.Exp(-(x.Costs - state.Distance) / state.Temperature);
            Console.WriteLine($"x:" + string.Join(",", x.Nodes.Select(n => n.Index)));
            Console.WriteLine($"x.Costs: {x.Costs}");
            var r = Random.NextDouble();
            bool condition = p > r;

            Equations["Metropolis Rule"] = MathString.MetropolisRule(x, state, r, condition);

            return condition;
        }

        public override IEnumerable<GraphState> MultiStart(GraphProblem graph) {
            throw new NotImplementedException();
        }

        public override GraphState UpdateState(GraphState state) {
            state.Distance = CurrentBest.Costs;
            state.Path = CurrentBest.Nodes;
            state.PathEdges = CurrentBest.Edges;
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