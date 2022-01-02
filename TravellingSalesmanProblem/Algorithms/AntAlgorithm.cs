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
        private static readonly Random Random = new();
        private GraphProblem GlobalBest = new();
        private GraphProblem DistributedMemory = new();
        public override LinkedList<GraphState> FindPath(GraphProblem graph) {
            var history = new LinkedList<GraphState>();
            InitializeDistributedAntMemory(graph);
            var state = new GraphState { Nodes = graph.Nodes };

            int iteration = 0;
            history.AddLast(state);

            for (int i = 0; i < 100; i++) {
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

        private double RandomProportionalRule(Ant k, Node r, Node s) {
            var sum = k.Unvisited.Sum(n => PheromoneClosenessProduct(r, n));
            var edge = DistributedMemory.Edges.Find(e => e.IsBetween(r, s));
            var pcp = PheromoneClosenessProduct(r, s);

            Equations["Random Proportional Rule"] = MathString.RandomProportionalRule(Colony.IndexOf(k), edge, Beta, pcp, sum);
            return pcp / sum;
        }

        private double PheromoneClosenessProduct(Node r, Node s) {
            var edge = DistributedMemory.Edges.Find(e => e.IsBetween(r, s));
            var pcp = edge.Pheromone * Math.Pow(1 / edge.Distance, Beta);

            Equations["Pheromone Closeness Product"] = MathString.PheromoneClosenessProduct(edge, Beta, pcp);
            return pcp;
        }

        private void GlobalUpdatingRule() {
            foreach (var edge in DistributedMemory.Edges) {
                edge.Pheromone = (1 - Alpha) * edge.Pheromone;

                if (edge.IsInside(GlobalBest.Edges)) {
                    var lgbInversed = Math.Pow(GlobalBest.Costs, -1);
                    Equations["Global Updating Rule"] = MathString.GlobalUpdatingRule(edge, Alpha, lgbInversed);
                    edge.Pheromone += Alpha * lgbInversed;
                }
            }
        }

        private void LocalUpdatingRule(Ant k) {
            foreach (var edge in DistributedMemory.Edges) {
                if (edge.IsInside(k.Path.Edges)) {
                    Equations["Local Updating Rule"] = MathString.LocalUpdatingRule(edge, Rho, InitialPheromone);
                    edge.Pheromone = ((1 - Rho) * edge.Pheromone) + (Rho * InitialPheromone);
                } 
            }
        }

        private Node StateTransitionRule(Ant k) {
            return Random.NextDouble() <= StateTransition ? Exploitation(k) : BiasedExploration(k);
        }

        private Node Exploitation(Ant k) {
            return k.Unvisited
                .OrderByDescending(node => PheromoneClosenessProduct(k.Current, node))
                .FirstOrDefault();
        }

        private Node BiasedExploration(Ant k) {
            k.MoveProbability.Clear();
            var r = k.Current;

            //1. store RANDOMPROPORTIONALRULE for all unvisited nodes
            foreach (var node in k.Unvisited) {
                var move = new Edge { Node1 = r, Node2 = node};
                var probability = RandomProportionalRule(k, r, node);
                if (!k.MoveProbability.ContainsKey(move))
                    k.MoveProbability.Add(move, probability);
            }

            //2. divide each element of the list by the sum of the list
            var sum = k.MoveProbability.Values.Sum();
            foreach (var mp in k.MoveProbability) {
                k.MoveProbability[mp.Key] = mp.Value / sum;
            }

            //3. create a new list that contains the cumulative of those values
            var cumulative = 0d;
            foreach (var mp in k.MoveProbability) {
                cumulative += mp.Value;
                k.MoveProbability[mp.Key] = cumulative;
            }

            //4. find the first node where a random number is greater than the element
            return k.MoveProbability
                .FirstOrDefault(mp => Random.NextDouble() < mp.Value).Key.Node2;
        }

        public void ScatterAnts(GraphProblem graph) {
            var nodes = graph.Nodes;
            nodes.Shuffle();

            for (int i = 0; i < AntCount; i++) {
                Colony[i].Reset();
                Colony[i].Path = new GraphProblem { Nodes = new List<Node> { nodes[i] } };
                Colony[i].Unvisited = nodes.Where(n => n != nodes[i]).ToList();
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
            Path.Edges.Add(Edge.Between(from, to));
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