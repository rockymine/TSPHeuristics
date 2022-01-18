﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class HillClimbing : Algorithm {
        public NeighbourType NeighbourEnum { get; set; }
        public DescentType DescentType { get; set; }
        private GraphProblem X = new();
        private GraphProblem XBest = new();
        private GraphProblem Y = new();

        private GraphState XState = new();
        private GraphState XBestState = new();
        public override LinkedList<GraphState> FindPath(GraphProblem graph) {
            var history = new LinkedList<GraphState>();
            X = GraphProblem.OrderedGraphProblem(graph);

            var state = new GraphState {
                Nodes = X.Nodes,
                PathEdges = X.Edges,
                Distance = double.MaxValue
            };

            XBest = X;
            var iteration = 0;
            history.AddLast(AdvanceState(state, iteration));

            while (true) {
                Y = NeighbourState.Create(X, NeighbourEnum, DescentType);

                if (Y.Costs >= X.Costs)
                    break;

                X = Y;
                XBest = X;
                iteration++;
                history.AddLast(AdvanceState(history.Last.Value, iteration));
            }

            return history;
        }

        public LinkedList<GraphState> MultiStart(GraphProblem graph) {
            var state = new GraphState { Nodes = graph.Nodes };
            var history = new LinkedList<GraphState>();
            var costs = double.MaxValue;
            history.AddLast(state);

            for (int i = 0; i < graph.Nodes.Count; i++) {
                XState = FindPath(graph).Last();

                if (XState.Distance < costs) {
                    costs = XState.Distance;
                    XBestState = XState;

                    history.AddLast(AdvanceMultiStartState(history.Last.Value));
                }
            }

            return history;
        }

        public override void UpdateStateMessages(GraphState state) {
            state.Messages["Iteration"] = state.Iteration.ToString();
            state.Messages["Route"] = string.Join("-", state.Path.Select(n => n.Index));
            state.Messages["Distance"] = state.Distance.ToString();
        }

        private GraphState AdvanceState(GraphState state, int iteration) {
            var newState = state.DeepCopy();

            newState.Distance = XBest.Costs;
            newState.Path = XBest.Nodes;
            newState.PathEdges = XBest.Edges;
            newState.SwapInfo = XBest.SwapInfo?.DeepCopy();
            newState.Iteration = iteration;

            UpdateStateMessages(newState);
            return newState;
        }

        private GraphState AdvanceMultiStartState(GraphState state) {
            var newState = state.DeepCopy();

            newState.Distance = XBestState.Distance;
            newState.Path = XBestState.Path;
            newState.PathEdges = XBestState.PathEdges;

            UpdateStateMessages(newState);
            return newState;
        }
    }
}