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
        public double StateTransition { get; set; } //ExploitVsExplore
        public double InitialPheromone { get; set; }
        private static readonly Random Random = new();
        private GraphProblem GlobalBest = new();
        private GraphProblem DistributedMemory = new();
        public override IEnumerable<GraphState> FindPath(GraphProblem graph) {
            var state = new GraphState { Nodes = graph.Nodes };
            var nearestNeighbor = new NearestNeighbour { Start = graph.Nodes[Random.Next(0, graph.Nodes.Count)] };

            Colony = Enumerable.Range(1, AntCount).Select(i => new Ant()).ToList();
            GlobalBest = nearestNeighbor.FindPath(graph).Last().ToGraphProblem();
            InitialPheromone = Math.Pow((GlobalBest.Nodes.Count) * GlobalBest.CalcCosts(), -1);
            DistributedMemory = GraphProblem.ConnectedGraphProblem(graph);

            DistributedMemory.Edges.ForEach(e => e.Pheromone += InitialPheromone);
            yield return UpdateState(state);

            for (int i = 0; i < 100; i++) {
                state.Iteration++;
                if (i > 0)
                    Colony.ForEach(a => a.Reset());

                /* 1) Initialization */
                ScatterAnts(graph);

                /* 2) Ants build their tours */
                AntPathBuilding(graph);

                var best = Colony
                    .OrderBy(x => x.Path.CalcCosts())
                    .FirstOrDefault().Path;

                if (Colony.TrueForAll(a => a.Path.Costs == best.Costs))
                    state.Finished = true;

                GlobalBest = best;

                /* 3) Global Updating */
                GlobalUpdatingRule();
                yield return UpdateGlobalState(state);
            }
        }

        private GraphState UpdateState(GraphState state) {
            state.Path = GlobalBest.Nodes;
            state.PathEdges = GlobalBest.Edges;
            state.Distance = GlobalBest.CalcCosts();

            UpdateStateMessages(state);
            return state;
        }

        private GraphState UpdateGlobalState(GraphState state) {
            state.Path = GlobalBest.Nodes;
            state.PathEdges = DistributedMemory.Edges;
            state.Distance = GlobalBest.CalcCosts();
            state.Equations = Equations;

            UpdateStateMessages(state);
            return state;
        }

        private void AntPathBuilding(GraphProblem graph) {
            for (int j = 0; j < graph.Nodes.Count; j++) {
                foreach (var ant in Colony) {
                    var next = ant.Unvisited.Any() ? StateTransitionRule(ant) : ant.Path.Nodes[0];
                    ant.UpdatePath(ant.Current, next);
                }
                /* Local Updating */
                foreach (var ant in Colony) {
                    LocalUpdatingRule(ant);
                }
            }
        }

        public override IEnumerable<GraphState> MultiStart(GraphProblem graph) {
            throw new NotImplementedException();
        }

        public override void UpdateStateMessages(GraphState state) {
            state.Messages["Iteration"] = state.Iteration.ToString();
            state.Messages["Route"] = string.Join("-", state.Path.Select(n => n.Index));
            state.Messages["Distance"] = Math.Round(state.Distance, 2).ToString();
            state.Messages["Pheromones"] = DistributedMemory.Edges.Sum(e => e.Pheromone).ToString();
            state.Messages["Highest Pheromone"] = DistributedMemory.Edges.Max(e => e.Pheromone).ToString();
            state.Messages["Lowest Pheromone"] = DistributedMemory.Edges.Min(e => e.Pheromone).ToString();
            state.Messages["Average Pheromone"] = DistributedMemory.Edges.Average(e => e.Pheromone).ToString();
        }

        private double RandomProportionalRule(Ant k, Node r, Node s) {
            var sum = k.Unvisited.Sum(n => PheromoneClosenessProduct(r, n));
            var edge = DistributedMemory.Edges.Find(e => e.IsBetween(r, s));
            var pcp = PheromoneClosenessProduct(r, s);

            Equations["Random Proportional Rule"] = MathString.RandomProportionalRule(Colony.IndexOf(k), r, s, edge, Beta, pcp, sum);
            return pcp / sum;
        }

        private double PheromoneClosenessProduct(Node from, Node to) {
            var edge = DistributedMemory.Edges.Find(e => e.IsBetween(from, to));
            var pcp = edge.Pheromone * Math.Pow(edge.Visibility, Beta);

            Equations["Pheromone Closeness Product"] = MathString.PheromoneClosenessProduct(from, to, edge, Beta, pcp);
            
            return pcp;
        }

        private void GlobalUpdatingRule(bool acs = true) {
            foreach (var edge in DistributedMemory.Edges) {
                edge.Pheromone = (1 - Alpha) * edge.Pheromone;

                if (acs) {
                    /* Only Reinforce Best Edges */
                    if (IsEdgeInTour(edge, GlobalBest.Edges)) {
                        var lgbInversed = Math.Pow(GlobalBest.CalcCosts(), -1);
                        Equations["Global Updating Rule"] = MathString.GlobalUpdatingRule(edge, Alpha, lgbInversed);
                        edge.Pheromone += Alpha * lgbInversed;                        
                    }
                } else {
                    edge.Pheromone += Colony
                        .Where(k => IsEdgeInTour(edge, k.Path.Edges))
                        .Sum(k => 1 / k.Path.Nodes.Count);
                }
            }
        }

        //ants visit edges and change their pheromone level
        //local updating will help to avoid ants converging to a common path
        private void LocalUpdatingRule(Ant k) {
            foreach (var edge in DistributedMemory.Edges) {
                if (IsEdgeInTour(edge, k.Path.Edges)) {
                    Equations["Local Updating Rule"] = MathString.LocalUpdatingRule(edge, Rho, InitialPheromone);
                    edge.Pheromone = ((1 - Rho) * edge.Pheromone) + (Rho * InitialPheromone);
                } 
            }
        }

        private Node StateTransitionRule(Ant k) {
            /*every time an ant in node r has to choose a city s to move to,
             it samples a random number 1 <= q <= 1*/
            var q = Random.NextDouble();
            //if q <= q0 then the best edge, according to STATETRANSITIONRULE is chosen (exploitation)
            //otherwise an edge is chosen according to RANDOMPROPORTIONALRULE (biased exploration)
            return q <= StateTransition ? Exploitation(k) : BiasedExploration(k);
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
                var move = new Tuple<Node, Node>(r, node);
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
                .FirstOrDefault(mp => Random.NextDouble() < mp.Value).Key.Item2;
        }

        public void ScatterAnts(GraphProblem graph) {
            var nodes = graph.Nodes;
            nodes.Shuffle();

            for (int i = 0; i < AntCount; i++) {
                Colony[i].Path = new GraphProblem { Nodes = new List<Node> { nodes[i] } };
                Colony[i].Unvisited = nodes.Where(n => n != nodes[i]).ToList();
            }
        }

        private static bool IsEdgeInTour(Edge e, List<Edge> edges) {
            return edges.Any(edge => e.IsEqual(edge));
        }
    }

    public class Ant {
        public GraphProblem Path { get; set; } = new();
        public List<Node> Unvisited { get; set; } = new();
        public Node Current => Path.Nodes.Last();
        public Dictionary<Tuple<Node, Node>, double> MoveProbability { get; set; } = new();

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