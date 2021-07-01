using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class NearestNeighbour : Algorithm {
        public Node Node { get; set; }
        public bool Closed { get; set; } = true;
        public override IEnumerable<GraphState> FindPath(GraphProblem graph) {
            var state = new GraphState {
                Nodes = graph.Nodes
            };
            graph.Reset();

            var current = Node;
            Node.Visited = true;
            state.Path.Add(current);

            UpdateStateMessages(state);
            yield return state;

            while (true) {
                var edge = FindShortest(current);

                if (edge == null) {
                    var isClosed = true;

                    if (Closed) {
                        edge = current.Edges.Find(e => e.IsBetween(current, Node));
                        isClosed = edge != null;

                        if (edge != null) {
                            state.Path.Add(edge.Opposite(current));
                            state.PathEdges.Add(edge);
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

        public override void UpdateStateMessages(GraphState state) {
            state.Messages["Route"] = string.Join('-', state.Path.Select(n => n.Index));
            state.Messages["Path Edges"] = string.Join(',', state.PathEdges.Select(n => n.Node1.Index + " <-> " + n.Node2.Index));
            state.Messages["Distance"] = Math.Round(state.Distance, 1).ToString();
        }

        private static Edge FindShortest(Node node) {
            Edge closest = null;
            var closestDistance = double.MaxValue;

            foreach (var edge in node.Edges) {
                //ignore already visited nodes
                if (edge.Opposite(node).Visited)
                    continue;

                if (edge.Distance < closestDistance) {
                    closestDistance = edge.Distance;
                    closest = edge;
                }
            }

            return closest;
        }
    }
}