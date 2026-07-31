using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class AntSystem : Algorithm {
        public List<Ant> Colony { get; set; }
        public int AntCount { get; set; }
        public double Beta { get; set; }
        public double Alpha { get; set; }
        public int Iterations { get; set; }
        public bool BreakWhenPathsAreEqual { get; set; }
        public GlobalUpdatingRule GlobalUpdatingRule { get; set; }
        public SelectionRule SelectionRule { get; set; } = SelectionRule.Proportional;
        public double InitialPheromone { get; set; }
        private static readonly Random Random = new();
        private GraphProblem XBest = new();
        private GraphProblem X = new();

        /* Length of XBest, cached for the iteration so the pheromone update does not re-sum the
         * tour once per edge of the complete graph. */
        private double BestCosts;

        public override LinkedList<GraphState> FindPath(GraphProblem graph) {
            var history = new LinkedList<GraphState>();

            if (graph.Nodes.Count == 0 || AntCount < 1)
                return history;

            var nearestNeighbor = new NearestNeighbour { Start = graph.Nodes[Random.Next(0, graph.Nodes.Count)] };
            var initialSolution = nearestNeighbor.FindPath(graph).Last();
            
            InitialPheromone = Math.Pow((initialSolution.Nodes.Count) * initialSolution.Distance, -1);

            X = GraphProblem.ConnectedGraphProblem(graph);

            /* Assignment, not accumulation: re-running on an already seeded graph must start from
             * the initial deposit rather than stack onto the previous run's trail. */
            X.Edges.ForEach(e => e.Pheromone = InitialPheromone);

            /* Give each ant a starting node. Starts wrap around, since AntCount may exceed the
             * node count. */
            Colony = Enumerable.Range(0, AntCount)
                .Select(i => new Ant { Start = graph.Nodes[i % graph.Nodes.Count] }).ToList();

            var state = new GraphState {
                Nodes = initialSolution.Nodes,
                PathEdges = X.Edges
            };

            history.AddLast(state);
            var iteration = 0;

            for (int i = 0; i < Iterations; i++) {
                BuildAntPaths(graph);

                /* Costs re-sums the whole tour on every read, so the best ant is found in one
                 * pass with one read per ant. */
                var costs = new double[Colony.Count];
                var bestAnt = 0;
                for (int a = 0; a < Colony.Count; a++) {
                    costs[a] = Colony[a].Path.Costs;
                    if (costs[a] < costs[bestAnt])
                        bestAnt = a;
                }

                XBest = Colony[bestAnt].Path;
                BestCosts = costs[bestAnt];

                X = GlobalUpdating(X);

                iteration++;
                history.AddLast(AdvanceState(history.Last.Value, iteration));

                if (BreakWhenPathsAreEqual && costs.All(c => c == BestCosts))
                    break;
            }

            return history;
        }

        private GraphState AdvanceState(GraphState state, int iteration) {
            var newState = state.DeepCopy();

            newState.Distance = XBest.Costs;
            newState.Path = XBest.Nodes;
            newState.PathEdges = X.Edges;
            newState.Iteration = iteration;
            newState.Equations = Equations?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DeepCopy());

            UpdateStateMessages(newState);
            return newState;
        }

        /* Each history entry needs its own snapshot of the pheromone levels. The edges are copied
         * and pointed at the existing nodes; cloning the nodes as well would be discarded by the
         * lookups below. */
        private GraphProblem GlobalUpdating(GraphProblem graph) {
            var edges = new List<Edge>(graph.Edges.Count);
            foreach (var source in graph.Edges) {
                var edge = source.Copy();
                edge.Node1 = source.Node1;
                edge.Node2 = source.Node2;
                edges.Add(edge);
            }

            /* Each tour is hashed once, so edge membership is a set lookup rather than a scan of
             * the tour. */
            var deposits = GlobalUpdatingRule == GlobalUpdatingRule.AS
                ? Colony.Select(a => (Keys: Edge.KeySet(a.Path.Edges), Amount: 1 / a.Path.Costs)).ToList()
                : null;
            var bestKeys = GlobalUpdatingRule == GlobalUpdatingRule.AS ? null : Edge.KeySet(XBest.Edges);
            var lgbInversed = GlobalUpdatingRule == GlobalUpdatingRule.AS ? 0 : Math.Pow(BestCosts, -1);

            /* The equation shown in the UI is the one for the last updated edge, built once
             * rather than per edge. The pheromone is captured before the deposit because that is
             * what the formula displays. */
            Edge lastEdge = null;
            var lastSum = 0d;

            foreach (var edge in edges) {
                edge.Pheromone = (1 - Alpha) * edge.Pheromone;

                if (GlobalUpdatingRule == GlobalUpdatingRule.AS) {
                    var key = edge.Key;
                    var sum = 0d;
                    foreach (var deposit in deposits) {
                        if (deposit.Keys.Contains(key))
                            sum += deposit.Amount;
                    }

                    lastEdge = Snapshot(edge);
                    lastSum = sum;
                    edge.Pheromone += sum;
                } else if (bestKeys.Contains(edge.Key)) {
                    lastEdge = Snapshot(edge);
                    edge.Pheromone += Alpha * lgbInversed;
                }
            }

            if (lastEdge != null) {
                Equations["Global Updating Rule"] = GlobalUpdatingRule == GlobalUpdatingRule.AS
                    ? MathString.GlobalUpdatingRuleAS(lastEdge, Alpha, lastSum)
                    : MathString.GlobalUpdatingRule(lastEdge, Alpha, lgbInversed);
            }

            return new GraphProblem {
                Nodes = graph.Nodes,
                Edges = edges
            };
        }

        private static Edge Snapshot(Edge edge) {
            var copy = edge.Copy();
            copy.Node1 = edge.Node1;
            copy.Node2 = edge.Node2;
            return copy;
        }

        /* Edge lookup table for the current pheromone snapshot, so the innermost selection loop
         * indexes rather than scans. */
        private Dictionary<(int, int), Edge> EdgeLookup = new();

        private void BuildAntPaths(GraphProblem graph) {
            EdgeLookup = new Dictionary<(int, int), Edge>(X.Edges.Count);
            foreach (var edge in X.Edges)
                EdgeLookup[edge.Key] = edge;

            Colony.ForEach(a => a.Initialize(graph));
            for (int j = 0; j < graph.Nodes.Count; j++) {
                foreach (var ant in Colony) {
                    var next = ant.Unvisited.Any() ? StateTransitionRule(ant) : ant.Start;
                    ant.UpdatePath(ant.Current, next);
                }
            }
        }

        /* Scratch buffer for the candidate weights, grown as needed and reused across steps so
         * the selection does not allocate once per ant per step. */
        private double[] Weights = new double[0];

        /* Picks the next node for an ant. Proportional is the random proportional rule that
         * defines the Ant System: a candidate is drawn with a probability proportional to its
         * pheromone-visibility weight. Greedy always takes the largest weight, which makes an
         * ant's tour a function of its starting node alone - a colony of m ants then explores
         * exactly m tours per iteration and further iterations only shift the pheromone. Both are
         * selectable so the thesis results stay reproducible. Exponents: Beta weights the
         * visibility. Alpha is this project's evaporation parameter (rho in most of the
         * literature) rather than the pheromone exponent, so the pheromone enters with exponent
         * one. */
        private Node StateTransitionRule(Ant ant) {
            var candidates = ant.Unvisited;
            if (candidates.Count == 0)
                return null;

            var current = ant.Current;

            if (SelectionRule == SelectionRule.Greedy) {
                Node best = null;
                var bestWeight = double.NegativeInfinity;

                foreach (var node in candidates) {
                    var weight = PheromoneVisibility(current, node);
                    if (weight > bestWeight) {
                        bestWeight = weight;
                        best = node;
                    }
                }

                return best;
            }

            if (Weights.Length < candidates.Count)
                Weights = new double[candidates.Count];

            var sum = 0d;
            for (int i = 0; i < candidates.Count; i++) {
                Weights[i] = PheromoneVisibility(current, candidates[i]);
                sum += Weights[i];
            }

            /* Also catches NaN: a weight can overflow for a very large Beta on a very short edge,
             * and an undrawable distribution should still yield a legal move. */
            if (!(sum > 0) || double.IsInfinity(sum))
                return candidates[Random.Next(candidates.Count)];

            var target = Random.NextDouble() * sum;
            var cumulative = 0d;

            for (int i = 0; i < candidates.Count; i++) {
                cumulative += Weights[i];
                if (target < cumulative)
                    return candidates[i];
            }

            /* Only reachable through floating point drift in the accumulation. */
            return candidates[^1];
        }

        private double PheromoneVisibility(Node i, Node j) {
            var edge = EdgeLookup[Edge.KeyOf(i, j)];
            return edge.Pheromone * Math.Pow(1 / edge.Distance, Beta);
        }

        public override void UpdateStateMessages(GraphState state) {
            state.Messages["Iteration"] = state.Iteration.ToString();
            state.Messages["Route"] = string.Join("-", state.Path.Select(n => n.Index));
            state.Messages["Distance"] = Math.Round(state.Distance, 3).ToString();
            state.Messages["Ant Distances"] = string.Join(';', Colony.Select(a => Math.Round(a.Path.Costs, 3)));
        }
    }

    public enum GlobalUpdatingRule {
        ACS = 0,
        AS = 1
    }

    public enum SelectionRule {
        /* Draw the next node with a probability proportional to its weight - the rule that
         * defines the Ant System. */
        Proportional = 0,
        /* Always take the largest weight. Deterministic per starting node. */
        Greedy = 1
    }
}