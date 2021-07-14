using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class AntAlgorithm : Algorithm {
        public List<Ant> Colony { get; set; }
        public int AntCount { get; set; } = 10;
        public double Alpha { get; set; } = 0.1;
        public double Rho { get; set; } = 0.1; //Alpha
        public double Beta { get; set; } = 2;
        public double InitialPheromone { get; set; }
        private static readonly Random Random = new();
        private GraphProblem GlobalBest = new();
        public override IEnumerable<GraphState> FindPath(GraphProblem graph) {
            AntCount = graph.Nodes.Count;
            Colony = new List<Ant>(new Ant[AntCount]);

            NearestNeighbour nn = new();
            nn.Start = graph.Nodes[Random.Next(0, graph.Nodes.Count - 1)];
            GlobalBest = nn.FindPath(graph).First().ToGraphProblem();

            InitialPheromone = Math.Pow(GlobalBest.Nodes.Count - 1 * GlobalBest.CalcCosts(), -1);

            //1) Initialization phase
            ScatterAnts();
            //2) Construction phase
            for (int i = 0; i < graph.Nodes.Count; i++) {
                
            }

            return null;
        }

        public override void UpdateStateMessages(GraphState state) {
            throw new NotImplementedException();
        }

        //random-proportional rule
        private double RandomProportionalRule(Ant k, Node r, Node s) {
            var edge = Edge.Between(r, s);
            var sum = k.Unvisited.Sum(n => PheromoneClosenessProduct(r, n));
            return PheromoneClosenessProduct(r, s) / sum;
        }

        private double PheromoneClosenessProduct(Node from, Node to) {
            var edge = Edge.Between(from, to);
            return edge.Pheromone * Math.Pow(edge.Visibility, Beta);
        }

        private void GlobalUpdatingRule(GraphProblem graph, bool acs = true) {
            foreach (var edge in graph.Edges) {
                edge.Pheromone = (1 - Alpha) * edge.Pheromone;

                if (acs) {
                    //only those edges belonging to the globally best tour are reinforced.
                    var Lgb = Math.Pow(GlobalBest.CalcCosts(), -1);
                    if (IsEdgeInGlobalBestTour(edge, GlobalBest))
                        edge.Pheromone += Alpha * Lgb;
                } else {
                    edge.Pheromone += Colony.Where(k => IsEdgeUsedByAntK(edge, k)).Sum(k => 1 / k.AntSolution.Nodes.Count);
                }                
            }
        }

        //ants visit edges and change their pheromone level
        private void LocalUpdatingRule(Ant k) {
            foreach (var edge in k.AntSolution.Edges) {
                edge.Pheromone = (1 - Rho) * edge.Pheromone + Rho * InitialPheromone;
            }
        }

        private Node StateTransitionRule(GraphProblem graph, Ant k, Node r) {
            /*TODO: every time an ant in node r has to choose a city s to move to,
             it samples a random number 1 <= q <= 1*/
            var q = k.Random.NextDouble(); 
            var q0 = 0d; //TODO: Parameter

            //if q <= q0 then the best edge, according to STATETRANSITIONRULE is chosen (exploitation)
            if (q <= q0) {
                return Exploitation(k, r);
            //otherwise an edge is chosen according to RANDOMPROPORTIONALRULE (biased exploration)
            } else {
                return BiasedExploration(k, r);
            }
        }

        private Node Exploitation(Ant k, Node r) {
            var list = k.Unvisited;
            return list.OrderBy(node => PheromoneClosenessProduct(r, node)).FirstOrDefault();
        }

        private Node BiasedExploration(Ant k, Node r) {
            //1. store RANDOMPROPORTIONALRULE for all unvisited nodes
            foreach (var node in k.Unvisited) {
                var move = new Tuple<Node, Node>(r, node);
                var probability = RandomProportionalRule(k, r, node);
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
            return k.MoveProbability.FirstOrDefault(mp => Random.NextDouble() > mp.Value).Key.Item2;
        }

        public void ScatterAnts() {
            var nodes = GlobalBest.Nodes;
            var unvisited = nodes.GetRange(0, nodes.Count - 2);
            unvisited.Shuffle();
            var range = unvisited.GetRange(0, AntCount - 1);

            for (int i = 0; i < AntCount; i++) {
                var node = range[i];
                Colony[i].Start = node;
                Colony[i].AntSolution.Nodes.Add(node);
                Colony[i].Unvisited = unvisited.GetRange(1, unvisited.Count - 1);
            }
        }

        private static bool IsEdgeUsedByAntK(Edge e, Ant k) {
            //TODO: check if the ants tour contains the given edge.
            return k.AntSolution.Nodes.Any(node => node.Edges.Any(edge => edge == e));
        }

        private static bool IsEdgeInGlobalBestTour(Edge e, GraphProblem best) {
            return best.Nodes.Any(node => node.Edges.Any(edge => edge == e));
        }
    }

    //ant
    public class Ant {
        public Node Start { get; set; }
        public GraphProblem AntSolution { get; set; }
        public List<Node> Unvisited { get; set; }
        public Dictionary<Tuple<Node, Node>, double> MoveProbability = new();

        private static readonly Random random = new();
        public Random Random = random;
    }
}