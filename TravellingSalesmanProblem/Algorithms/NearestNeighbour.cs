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
        public override LinkedList<GraphState> FindPath(GraphProblem graph) {
            graph.Reset(); //mark nodes as unvisited
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

                Equations["Hello World"] = new MathString("Hello", "World", Edge.Distance.ToString());
                history.AddLast(AdvanceState(history.Last.Value));
            }

            return history;
        }

        private GraphState AdvanceState(GraphState state) {
            var newState = state.DeepCopy();
            var opposite = Edge.Opposite(Current);

            Current = opposite;
            Current.Visited = true;

            //add nodes and edges
            newState.Path.Add(opposite);
            newState.PathEdges.Add(Edge);
            newState.Distance += Edge.Distance;
            newState.Equations = Equations?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DeepCopy());

            UpdateStateMessages(newState);
            return newState;
        }

        private LinkedList<GraphState> FindLongestPath(GraphProblem graph) {
            graph.Reset(); //mark nodes as unvisited
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

        public IEnumerable<GraphState> MultiStart(GraphProblem graph, bool shortest = true) {
            graph.Reset();
            var best = new GraphState { Nodes = graph.Nodes };
            var costs = double.MaxValue;            

            foreach (var node in graph.Nodes) {
                Start = node;
                var current = shortest ? FindPath(graph).Last() : FindLongestPath(graph).Last();

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

        public override void UpdateStateMessages(GraphState state) {
            state.Messages["Route"] = string.Join('-', state.Path.Select(n => n.Index));
            state.Messages["Distance"] = Math.Round(state.Distance, 1).ToString();
        }
    }
}