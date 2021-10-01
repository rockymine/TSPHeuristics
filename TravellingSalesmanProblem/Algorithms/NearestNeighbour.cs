using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class NearestNeighbour : Algorithm {
        public Node Start { get; set; }
        public bool Closed { get; set; } = true;
        public override IEnumerable<GraphState> FindPath(GraphProblem graph) {
            var state = new GraphState { Nodes = graph.Nodes };
            graph.Reset();

            var current = Start;
            Start.Visited = true;
            state.Path.Add(current);

            UpdateStateMessages(state);
            yield return state;

            while (true) {
                var edge = Edge.FindShortest(current);

                if (edge == null) {
                    var isClosed = true;

                    if (Closed) {
                        edge = current.Edges.Find(e => e.IsBetween(current, Start));
                        isClosed = edge != null;

                        if (edge != null) {
                            state.Path.Add(edge.Opposite(current));
                            state.PathEdges.Add(edge);
                            state.Distance += edge.Distance;
                        }
                    }

                    state.Finished = true;
                    state.Success = graph.Nodes.All(n => n.Visited) && isClosed;
                    UpdateStateMessages(state);
                    yield break;
                }

                state.Distance += edge.Distance;
                current = edge.Opposite(current);
                current.Visited = true;

                state.Path.Add(current);
                state.PathEdges.Add(edge);
                UpdateStateMessages(state);
                yield return state;
            }
        }

        public IEnumerable<GraphState> FindLongestPath(GraphProblem graph) {
            var state = new GraphState { Nodes = graph.Nodes };
            graph.Reset();

            var current = Start;
            Start.Visited = true;
            state.Path.Add(current);

            UpdateStateMessages(state);
            yield return state;

            while (true) {
                var edge = Edge.FindLongest(current);

                if (edge == null) {
                    var isClosed = true;

                    if (Closed) {
                        edge = current.Edges.Find(e => e.IsBetween(current, Start));
                        isClosed = edge != null;

                        if (edge != null) {
                            state.Path.Add(edge.Opposite(current));
                            state.PathEdges.Add(edge);
                            state.Distance += edge.Distance;
                        }
                    }

                    state.Finished = true;
                    state.Success = graph.Nodes.All(n => n.Visited) && isClosed;
                    UpdateStateMessages(state);
                    yield break;
                }

                state.Distance += edge.Distance;
                current = edge.Opposite(current);
                current.Visited = true;

                state.Path.Add(current);
                state.PathEdges.Add(edge);
                UpdateStateMessages(state);
                yield return state;
            }
        }

        public override IEnumerable<GraphState> MultiStart(GraphProblem graph) {
            var best = new GraphState { Nodes = graph.Nodes };
            var costs = double.MaxValue;

            graph.Reset();            

            foreach (var node in graph.Nodes) {
                Start = node;
                var current = FindPath(graph).Last();

                if (current.CalcCosts() < costs) {
                    costs = current.CalcCosts();
                    best.Path = current.Path;
                    best.PathEdges = current.PathEdges;
                    best.Distance = current.Distance;

                    UpdateStateMessages(best);
                    yield return best;
                }                    
            }
        }

        public IEnumerable<GraphState> MultiStartLongest(GraphProblem graph) {
            var worst = new GraphState { Nodes = graph.Nodes };
            var costs = double.MinValue;

            graph.Reset();

            foreach (var node in graph.Nodes) {
                Start = node;
                var current = FindLongestPath(graph).Last();

                if (current.CalcCosts() > costs) {
                    costs = current.CalcCosts();
                    worst.Path = current.Path;
                    worst.PathEdges = current.PathEdges;
                    worst.Distance = current.Distance;
                    
                    yield return worst;
                }
            }
        }

        public override void UpdateStateMessages(GraphState state) {
            state.Messages["Route"] = string.Join('-', state.Path.Select(n => n.Index));
            state.Messages["Distance"] = Math.Round(state.Distance, 1).ToString();
        }
    }
}