using ChartData;
using System;
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
        private static readonly Random Random = new();

        public override LinkedList<GraphState> FindPath(GraphProblem graph) {
            multiStartRun = false;
            return FindPath(graph, false);
        }

        private bool multiStartRun;

        private LinkedList<GraphState> FindPath(GraphProblem graph, bool randomizeStart) {
            var history = new LinkedList<GraphState>();
            X = GraphProblem.OrderedGraphProblem(randomizeStart ? Shuffled(graph) : graph);

            var chartInfo = new ChartInfo {
                Title = "Tour length per improving move",
                XAxis = new ChartSet { Title = "Improving move" },
                YAxis = new List<ChartSet> {
                    new ChartSet { Title = "Tour length" }
                }
            };

            var state = new GraphState {
                Nodes = X.Nodes,
                PathEdges = X.Edges,
                Distance = double.MaxValue,
                ChartInfo = new List<ChartInfo>() { chartInfo }
            };

            XBest = X;
            var iteration = 0;
            history.AddLast(AdvanceState(state, iteration));

            while (true) {
                Y = DescentType == DescentType.Random ?
                    NeighbourState.CreateRandomImproving(X, NeighbourEnum) :
                    NeighbourState.Create(X, NeighbourEnum, DescentType);

                if (Y.Costs >= X.Costs)
                    break;

                var fx = X.Costs;
                X = Y;
                XBest = X;
                iteration++;
                history.AddLast(AdvanceState(history.Last.Value, iteration, fx));

                history.Last.Value.ChartInfo[0].XAxis.Add(iteration, "red");
                history.Last.Value.ChartInfo[0].YAxis[0].Add(X.Costs, "red");
            }

            return history;
        }

        /* Every restart called FindPath(graph), which always builds its initial tour from the same
         * node ordering. A deterministic descent from a fixed start is a deterministic descent, so
         * all n restarts produced the identical local optimum and multi start was a plain single
         * run repeated n times. Each restart now begins from its own random permutation. */
        public LinkedList<GraphState> MultiStart(GraphProblem graph) {
            graph.Reset();
            multiStartRun = true;

            var state = new GraphState { Nodes = graph.Nodes };
            var history = new LinkedList<GraphState>();
            var costs = double.MaxValue;
            var iteration = 0;

            history.AddLast(state);

            for (int i = 0; i < graph.Nodes.Count; i++) {
                XState = FindPath(graph, true).Last();

                if (XState.Distance < costs) {
                    costs = XState.Distance;
                    XBestState = XState;
                    iteration++;

                    history.AddLast(AdvanceMultiStartState(history.Last.Value, iteration));
                }
            }

            return history;
        }

        private static GraphProblem Shuffled(GraphProblem graph) {
            /* Copies the node list: shuffling graph.Nodes in place would reorder the instance the
             * rest of the application (distance matrix, serialization) is still displaying. */
            var nodes = new List<Node>(graph.Nodes);
            if (nodes.Count > 1 && nodes[0] == nodes[^1])
                nodes.RemoveAt(nodes.Count - 1);

            nodes.Shuffle();
            return new GraphProblem { Nodes = nodes, Edges = graph.Edges };
        }

        private string DescentCaption() => DescentType switch {
            DescentType.Steepest => "Steepest descent: every neighbour is checked and the shortest one is taken.",
            DescentType.Next => "Next descent: neighbours are checked in order and the first shorter one is taken.",
            _ => "Random descent: neighbours are drawn at random until one is shorter."
        };

        public override void UpdateStateMessages(GraphState state) {
            /* One counter, named for what it counts: improving moves in a run, new bests in a
             * multi-start run. Both used to be "Iteration". */
            state.Messages[multiStartRun ? "New best" : "Improving move"] = state.Iteration.ToString();
            state.Messages["Tour"] = string.Join("-", state.Path.Select(n => n.Index));
            state.Messages["Tour length"] = Math.Round(state.Distance, 3).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private GraphState AdvanceState(GraphState state, int iteration, double? fx = null) {
            var newState = state.DeepCopy();

            newState.Distance = XBest.Costs;
            newState.Path = XBest.Nodes;
            newState.PathEdges = XBest.Edges;
            newState.MoveInfo = XBest.MoveInfo?.DeepCopy();
            newState.Iteration = iteration;

            var card = fx is { } before
                ? Derivations.Move(newState.MoveInfo, before, Derivations.MoveContext(newState.MoveInfo), DescentCaption())
                : null;
            newState.Derivations = card == null ? new List<Derivation>() : new List<Derivation> { card };

            for (int i = 0; i < newState.ChartInfo[0].YAxis[0].Values.Count; i++) {
                newState.ChartInfo[0].YAxis[0].Colors[i] = (i == iteration - 1) ? "blue" : "red";
            }

            UpdateStateMessages(newState);
            return newState;
        }

        private GraphState AdvanceMultiStartState(GraphState state, int iteration) {
            var newState = state.DeepCopy();

            newState.Iteration = iteration;
            newState.Distance = XBestState.Distance;
            newState.Path = XBestState.Path;
            newState.PathEdges = XBestState.PathEdges;

            UpdateStateMessages(newState);
            return newState;
        }
    }
}