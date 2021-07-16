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
        public double ExploitVsExplore { get; set; } = 0.9; //Relative importance of exploitation versus exploration
        public double InitialPheromone { get; set; }
        private static readonly Random Random = new();
        private GraphProblem GlobalBest = new();
        private GraphProblem DistributedMemory = new();
        public override IEnumerable<GraphState> FindPath(GraphProblem graph) {
            /* Set up ant colony. */
            AntCount = graph.Nodes.Count;
            Colony = new List<Ant>(new Ant[AntCount]);
            DistributedMemory.Nodes = graph.Nodes;
            DistributedMemory.ConnectAllNodes();

            /* Create tour using tour constructive heuristic.
             This tour will be improved by the ACS heuristic. */
            NearestNeighbour nn = new();
            nn.Start = graph.Nodes[Random.Next(0, graph.Nodes.Count)];
            GlobalBest = nn.FindPath(graph).First().ToGraphProblem();            

            /* 1) Initialization phase */
            ScatterAnts();
            InitialPheromone = Math.Pow(GlobalBest.Nodes.Count - 1 * GlobalBest.CalcCosts(), -1);

            //TODO: actually put initial pheromone on all edges?

            /* 2) This is the phase in which ants build their tours.
             The tour of an ant k is stored in k.Path.Nodes */

            //TODO: add loop.
            //TODO: reset ants.
            //TODO: add graphstate logic.

            for (int i = 0; i < GlobalBest.Nodes.Count - 1; i++) {
                foreach (var ant in Colony) {
                    var next = new Node();
                    if (!ant.Unvisited.Any()) {
                        next = StateTransitionRule(ant);
                    } else {
                        next = ant.Path.Nodes[0];
                    }                    
                    ant.UpdatePath(ant.Current, next);
                }

                /* In this phase local updating occurs and pheromone is updated. */
                foreach (var ant in Colony) {
                    LocalUpdatingRule(ant);
                }                
            }

            /* 3) In this phase global updating occurs and pheromone is updated. */
            GlobalUpdatingRule();

            //TODO: print best solution

            return null;
        }

        public override IEnumerable<GraphState> MultiStart(GraphProblem graph) {
            throw new NotImplementedException();
        }

        public override void UpdateStateMessages(GraphState state) {
            throw new NotImplementedException();
        }

        private double RandomProportionalRule(Ant k, Node r, Node s) {
            var sum = k.Unvisited.Sum(n => PheromoneClosenessProduct(r, n));
            return PheromoneClosenessProduct(r, s) / sum;
        }

        private double PheromoneClosenessProduct(Node from, Node to) {
            var edge = DistributedMemory.Edges.Find(e => e == Edge.Between(from, to));
            return edge.Pheromone * Math.Pow(edge.Visibility, Beta);
        }

        private void GlobalUpdatingRule(bool acs = true) {
            foreach (var edge in DistributedMemory.Edges) {
                edge.Pheromone = (1 - Alpha) * edge.Pheromone;

                if (acs) {
                    //only those edges belonging to the globally best tour are reinforced.
                    if (IsEdgeInTour(edge, GlobalBest))
                        edge.Pheromone += Alpha * Math.Pow(GlobalBest.CalcCosts(), -1);
                } else {
                    edge.Pheromone += Colony.Where(k => IsEdgeUsedByAntK(edge, k)).Sum(k => 1 / k.Path.Nodes.Count);
                }                
            }
        }

        //ants visit edges and change their pheromone level
        //local updating will help to avoid ants converging to a common path
        private void LocalUpdatingRule(Ant k) {
            //TODO: Q-Learning, Disable Local Updating.            
            foreach (var edge in DistributedMemory.Edges) {
                if (IsEdgeUsedByAntK(edge, k))
                    edge.Pheromone = (1 - Rho) * edge.Pheromone + Rho * InitialPheromone;
            }

            //foreach (var edge in k.AntSolution.Edges) {
            //    edge.Pheromone = (1 - Rho) * edge.Pheromone + Rho * InitialPheromone;
            //}
        }

        //TODO: make sure uses the pheromone values of the shared graph
        private Node StateTransitionRule(Ant k) {
            /*every time an ant in node r has to choose a city s to move to,
             it samples a random number 1 <= q <= 1*/
            var q = k.Random.NextDouble();

            //if q <= q0 then the best edge, according to STATETRANSITIONRULE is chosen (exploitation)
            if (q <= ExploitVsExplore) {
                return Exploitation(k, k.Current);
            //otherwise an edge is chosen according to RANDOMPROPORTIONALRULE (biased exploration)
            } else {
                return BiasedExploration(k, k.Current);
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
                Colony[i].Path.Nodes.Add(node);
                Colony[i].Unvisited = unvisited.GetRange(1, unvisited.Count - 1);
            }
        }

        private static bool IsEdgeUsedByAntK(Edge e, Ant k) {
            return k.Path.Nodes.Any(node => node.Edges.Any(edge => edge == e));
        }

        private static bool IsEdgeInTour(Edge e, GraphProblem best) {
            return best.Nodes.Any(node => node.Edges.Any(edge => edge == e));
        }
    }

    public class Ant {
        public GraphProblem Path { get; set; }
        public List<Node> Unvisited { get; set; }
        public Dictionary<Tuple<Node, Node>, double> MoveProbability = new();

        private static readonly Random random = new();
        public Random Random = random;

        public Node Current => Path.Nodes.Last();
        public double Length => Path.CalcCosts();

        public void UpdatePath(Node from, Node to) {
            Path.Nodes.Add(to);
            Path.Edges.Add(Edge.Between(from, to));
            Unvisited.Remove(to);
        }

        public void Reset() {
            Path.Reset();
            Path.Nodes.Clear();
            Path.Edges.Clear();
            Unvisited.Clear();
            MoveProbability.Clear();
        }
    }
}