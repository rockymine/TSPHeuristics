using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class AntAlgorithm : Algorithm {
        public List<Ant> Colony { get; set; }
        public int AntCount { get; set; }
        public double Alpha { get; set; }
        public double Rho { get; set; }
        public double Beta { get; set; }
        public double StateTransition { get; set; }
        public double InitialPheromone { get; set; }
        /* Was a hard coded 100, with no way for a caller to ask for a shorter or longer run. */
        public int Iterations { get; set; } = 100;
        private static readonly Random Random = new();
        private GraphProblem GlobalBest = new();
        private GraphProblem DistributedMemory = new();
        private Dictionary<(int, int), Edge> EdgeLookup = new();
        public override LinkedList<GraphState> FindPath(GraphProblem graph) {
            var history = new LinkedList<GraphState>();
            InitializeDistributedAntMemory(graph);
            var state = new GraphState { Nodes = graph.Nodes };

            int iteration = 0;
            history.AddLast(state);

            for (int i = 0; i < Iterations; i++) {
                ScatterAnts(graph);
                BuildAntPaths(graph);

                GlobalBest = Colony
                    .OrderBy(x => x.Path.Costs)
                    .FirstOrDefault().Path;

                GlobalUpdatingRule();

                iteration++;
                history.AddLast(AdvanceState(history.Last.Value, iteration));
            }

            return history;
        }

        private void InitializeDistributedAntMemory(GraphProblem graph) {
            var nearestNeighbor = new NearestNeighbour { Start = graph.Nodes[Random.Next(0, graph.Nodes.Count)] };
            GlobalBest = nearestNeighbor.FindPath(graph).Last().ToGraphProblem();

            Colony = Enumerable.Range(1, AntCount).Select(i => new Ant()).ToList();     
            InitialPheromone = Math.Pow((GlobalBest.Nodes.Count) * GlobalBest.Costs, -1);
            DistributedMemory = GraphProblem.ConnectedGraphProblem(graph);

            DistributedMemory.Edges.ForEach(e => e.Pheromone = InitialPheromone);

            EdgeLookup = new Dictionary<(int, int), Edge>(DistributedMemory.Edges.Count);
            foreach (var edge in DistributedMemory.Edges)
                EdgeLookup[edge.Key] = edge;
        }

        private GraphState AdvanceState(GraphState state, int iteration) {
            var newState = state.DeepCopy();

            newState.Distance = GlobalBest.Costs;
            newState.Path = GlobalBest.Nodes;
            newState.PathEdges = DistributedMemory.Edges;
            newState.Equations = Equations?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DeepCopy());
            newState.Iteration = iteration;

            UpdateStateMessages(newState);
            return newState;
        }

        private void BuildAntPaths(GraphProblem graph) {
            for (int j = 0; j < graph.Nodes.Count; j++) {
                foreach (var ant in Colony) {
                    var next = ant.Unvisited.Any() ? StateTransitionRule(ant) : ant.Path.Nodes[0];
                    ant.UpdatePath(ant.Current, next);
                }
                /* Local Updating */
                Colony.ForEach(a => LocalUpdatingRule(a));
            }
        }

        public override void UpdateStateMessages(GraphState state) {
            state.Messages["Iteration"] = state.Iteration.ToString();
            state.Messages["Route"] = string.Join("-", state.Path.Select(n => n.Index));
            state.Messages["Distance"] = Math.Round(state.Distance, 3).ToString();
        }

        /* The normalising sum is over the same candidate set for every call within one step, so
         * calling this per candidate made the selection O(u^2) in pheromone lookups. */
        private double RandomProportionalRule(Ant k, Node r, Node s) {
            var sum = 0d;
            foreach (var node in k.Unvisited)
                sum += PheromoneClosenessProduct(r, node);

            var edge = Lookup(r, s);
            var pcp = PheromoneClosenessProduct(r, s);

            Equations["Random Proportional Rule"] = MathString.RandomProportionalRule(Colony.IndexOf(k), edge, Beta, pcp, sum);
            return sum > 0 ? pcp / sum : 0;
        }

        /* Kept free of side effects: it used to allocate a MathString on every call from the
         * innermost selection loop, which dominated the running time. The displayed equation is
         * recorded once per decision by RecordPheromoneClosenessProduct instead. */
        private double PheromoneClosenessProduct(Node r, Node s) {
            var edge = Lookup(r, s);
            return edge.Pheromone * Math.Pow(1 / edge.Distance, Beta);
        }

        private void RecordPheromoneClosenessProduct(Node r, Node s) {
            var edge = Lookup(r, s);
            Equations["Pheromone Closeness Product"] =
                MathString.PheromoneClosenessProduct(edge, Beta, PheromoneClosenessProduct(r, s));
        }

        private Edge Lookup(Node r, Node s) => EdgeLookup[Edge.KeyOf(r, s)];

        private void GlobalUpdatingRule() {
            var bestKeys = Edge.KeySet(GlobalBest.Edges);
            var lgbInversed = Math.Pow(GlobalBest.Costs, -1);
            Edge lastEdge = null;

            foreach (var edge in DistributedMemory.Edges) {
                edge.Pheromone = (1 - Alpha) * edge.Pheromone;

                if (bestKeys.Contains(edge.Key)) {
                    lastEdge = Snapshot(edge);
                    edge.Pheromone += Alpha * lgbInversed;
                }
            }

            if (lastEdge != null)
                Equations["Global Updating Rule"] = MathString.GlobalUpdatingRule(lastEdge, Alpha, lgbInversed);
        }

        /* Swept every edge of the complete graph, testing membership of the ant's tour with a
         * linear scan, once per ant per step - O(E * n) work to touch n edges. Walk the tour. */
        private void LocalUpdatingRule(Ant k) {
            Edge lastEdge = null;

            foreach (var pathEdge in k.Path.Edges) {
                if (!EdgeLookup.TryGetValue(pathEdge.Key, out var edge))
                    continue;

                lastEdge = Snapshot(edge);
                edge.Pheromone = ((1 - Rho) * edge.Pheromone) + (Rho * InitialPheromone);
            }

            if (lastEdge != null)
                Equations["Local Updating Rule"] = MathString.LocalUpdatingRule(lastEdge, Rho, InitialPheromone);
        }

        private static Edge Snapshot(Edge edge) {
            var copy = edge.Copy();
            copy.Node1 = edge.Node1;
            copy.Node2 = edge.Node2;
            return copy;
        }

        private Node StateTransitionRule(Ant k) {
            return Random.NextDouble() <= StateTransition ? Exploitation(k) : BiasedExploration(k);
        }

        /* Sorted the whole candidate list to take one element off the front. */
        private Node Exploitation(Ant k) {
            Node best = null;
            var bestWeight = double.NegativeInfinity;

            foreach (var node in k.Unvisited) {
                var weight = PheromoneClosenessProduct(k.Current, node);
                if (weight > bestWeight) {
                    bestWeight = weight;
                    best = node;
                }
            }

            if (best != null)
                RecordPheromoneClosenessProduct(k.Current, best);

            return best;
        }

        /* Roulette wheel selection over the unvisited nodes.
         *
         * The previous implementation drew a fresh random number for every candidate and returned
         * the first one that exceeded it. That is not a roulette wheel - it biases heavily towards
         * the head of the collection - and when no candidate matched (which happens whenever the
         * draws all land above the running cumulative, including the very common case of the last
         * candidate) FirstOrDefault returned a default KeyValuePair whose Key is null, so .Key.Node2
         * threw a NullReferenceException. One draw against the cumulative distribution, with the
         * last candidate as the fallback for floating point drift, fixes both. */
        private Node BiasedExploration(Ant k) {
            k.MoveProbability.Clear();
            var r = k.Current;

            //1. weight every unvisited node
            var candidates = new List<(Node Node, double Weight)>(k.Unvisited.Count);
            var sum = 0d;
            foreach (var node in k.Unvisited) {
                var probability = RandomProportionalRule(k, r, node);
                candidates.Add((node, probability));
                sum += probability;
                k.MoveProbability[Edge.Between(r, node, false)] = probability;
            }

            if (candidates.Count == 0)
                return null;
            if (sum <= 0)
                return candidates[Random.Next(candidates.Count)].Node;

            //2. one draw against the cumulative distribution
            var target = Random.NextDouble() * sum;
            var cumulative = 0d;
            foreach (var candidate in candidates) {
                cumulative += candidate.Weight;
                if (target < cumulative)
                    return candidate.Node;
            }

            return candidates[^1].Node;
        }

        public void ScatterAnts(GraphProblem graph) {
            /* Shuffling graph.Nodes in place reordered the caller's instance, which the rest of
             * the application still renders and serializes. Shuffle a copy. */
            var nodes = new List<Node>(graph.Nodes);
            nodes.Shuffle();

            for (int i = 0; i < AntCount; i++) {
                var start = nodes[i % nodes.Count];
                Colony[i].Reset();
                Colony[i].Path = new GraphProblem { Nodes = new List<Node> { start } };
                Colony[i].Unvisited = nodes.Where(n => n != start).ToList();
            }
        }
    }

    public class Ant {
        public Node Start { get; set; }
        public GraphProblem Path { get; set; } = new();
        public List<Node> Unvisited { get; set; } = new();
        public Node Current => Path.Nodes.Last();
        public Dictionary<Edge, double> MoveProbability { get; set; } = new();
        
        public void Initialize(GraphProblem graph) {
            var nodes = graph.Nodes;
            
            Reset();
            Path.Nodes = new List<Node> { Start };
            Unvisited = nodes.Where(n => n != Start).ToList();
        }
        
        public void UpdatePath(Node from, Node to) {
            Path.Nodes.Add(to);
            /* An ant's tour edges live in its own Path; registering each of them in the shared
             * Node.Edges adjacency cost two O(degree) scans per step and changed nothing. */
            Path.Edges.Add(Edge.Between(from, to, false));
            Unvisited.Remove(to);
        }

        public void Reset() {
            Path.Nodes.Clear();
            Path.Edges.Clear();
            Unvisited.Clear();
            MoveProbability.Clear();
        }
    }
}