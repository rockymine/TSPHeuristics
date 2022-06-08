using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class NearestNeighbour : Algorithm {
        public Node Start { get; set; }
        private Node Current = new();
        private Edge Edge = new();
        private GraphState X = new();
        private GraphState XBest = new();
        public override LinkedList<GraphState> FindPath(GraphProblem graph) {
            graph.Reset();
            var history = new LinkedList<GraphState>();
            var state = new GraphState { Nodes = graph.Nodes };

            Current = Start;
            Start.Visited = true;
            state.Path.Add(Current);
            history.AddLast(state);

            while (true) {
                Edge = Edge.FindShortest(Current);

                if (Edge == null) {
                    Edge = Current.Edges.Find(e => e.IsBetween(Current, Start));
                    history.AddLast(AdvanceState(history.Last.Value));
                    break;
                }

                history.AddLast(AdvanceState(history.Last.Value));
            }

            return history;
        }

        private GraphState AdvanceState(GraphState state) {
            var newState = state.DeepCopy();
            var opposite = Edge.Opposite(Current);

            Current = opposite;
            Current.Visited = true;

            newState.Path.Add(opposite);
            newState.PathEdges.Add(Edge);
            newState.Distance += Edge.Distance;

            UpdateStateMessages(newState);
            return newState;
        }

        private LinkedList<GraphState> FindLongestPath(GraphProblem graph) {
            graph.Reset();
            var history = new LinkedList<GraphState>();
            var state = new GraphState { Nodes = graph.Nodes };

            Current = Start;
            Start.Visited = true;
            state.Path.Add(Current);
            history.AddLast(state);

            while (true) {
                Edge = Edge.FindLongest(Current);

                if (Edge == null) {
                    Edge = Current.Edges.Find(e => e.IsBetween(Current, Start));
                    history.AddLast(AdvanceState(history.Last.Value));
                    break;
                }

                history.AddLast(AdvanceState(history.Last.Value));
            }

            return history;
        }

        public LinkedList<GraphState> MultiStart(GraphProblem graph, bool shortest = true) {
            graph.Reset();

            var state = new GraphState { Nodes = graph.Nodes };
            var history = new LinkedList<GraphState>();
            var costs = double.MaxValue;

            history.AddLast(state);

            foreach (var node in graph.Nodes) {
                Start = node;
                X = shortest ? FindPath(graph).Last() : FindLongestPath(graph).Last();

                if (X.CalcCosts() < costs) {
                    costs = X.Distance;
                    XBest = X;

                    history.AddLast(AdvanceMultiStartState(history.Last.Value));
                }                    
            }

            return history;
        }

        private GraphState AdvanceMultiStartState(GraphState state) {
            var newState = state.DeepCopy();

            newState.Distance = XBest.Distance;
            newState.Path = XBest.Path;
            newState.PathEdges = XBest.PathEdges;

            UpdateStateMessages(newState);
            return newState;
        }

        public override void UpdateStateMessages(GraphState state) {
            state.Messages["Start Node"] = Start.Index.ToString();
            state.Messages["Route"] = string.Join('-', state.Path.Select(n => n.Index));
            state.Messages["Distance"] = Math.Round(state.Distance, 3).ToString();
        }
    }
}