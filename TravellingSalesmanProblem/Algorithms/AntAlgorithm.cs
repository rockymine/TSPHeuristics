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
            var rpr = "Random Proportional Rule";

            Equations[rpr] = new("$p_{k}(r,s) = \\begin{cases}\\frac{[\\tau(r,s)] \\cdot [\\eta(r,s)]^\\beta}{\\sum\\limits_{u \\in J_{k}(r)}[\\tau(r,u)] \\cdot [\\eta(r,u)]^\\beta}, & \\text{if } s \\in J_{k}(r)\\\\0, & \\text{otherwise}\\end{cases}$") {
                Dummy = "$p_{?k?}(?r?,?s?) = \\begin{cases}\\frac{[?tau?] \\cdot [?eta?]^?beta?}{\\sum\\limits_{u \\in J_{?k?}(?r?)}[\\tau(?r?,u)] \\cdot [\\eta(?r?,u)]^?beta?}, & \\text{if } s \\in J_{?k?}(?r?)\\\\0, & \\text{otherwise}\\end{cases}$",
                Variables = new Dictionary<string, object> {
                    { "k", Colony.IndexOf(k) },
                    { "r", r.Index },
                    { "s", s.Index },
                    { "tau", edge.Pheromone },
                    { "eta" , edge.Visibility },
                    { "beta", Beta }
                }
            };

            var pcp = PheromoneClosenessProduct(r, s);
            Equations[rpr].Result = $"$p_{Colony.IndexOf(k)}({r.Index},{s.Index}) = \\frac{{{pcp}}}{{{sum}}} = {pcp / sum}$";

            return pcp / sum;
        }

        private double PheromoneClosenessProduct(Node from, Node to) {
            var edge = DistributedMemory.Edges.Find(e => e.IsBetween(from, to));
            var result = edge.Pheromone * Math.Pow(edge.Visibility, Beta);
            var pcp = "Pheromone Cloneness Product";

            Equations[pcp] = new("$\\omega(r,s) = [\\tau(r,s)] \\cdot [\\eta(r,s)]^\\beta$") {
                Dummy = "$\\omega(?r?,?s?) = ?tau? \\cdot ?eta?^?beta?$",
                Variables = new Dictionary<string, object> {
                    { "r", from.Index },
                    { "s", to.Index },
                    { "tau", edge.Pheromone },
                    { "eta", edge.Visibility },
                    { "beta", Beta }
                },
                Result = $"$\\omega({from.Index},{to.Index}) = {result}$"
            };
            
            return result;
        }

        private void GlobalUpdatingRule(bool acs = true) {
            var gur = "Global Updating Rule";
            Equations[gur] = new("$\\tau(r, s) \\leftarrow(1 -\\alpha) \\cdot \\tau(r, s) + \\alpha \\cdot \\triangle \\tau(r, s)$") {
                Dummy = "$\\tau(?r?,?s?) = (1-?alpha?) \\cdot ?tau? + ?alpha? \\cdot ?triangle?$",
                Description = "$\\text{ Once all ants have constructed their tour, the amount of pheromone is modified by applying the global updating rule. Here, only the globally best ant is allowed to deposit pheromone. } \\triangle \\tau(r, s) \\text{ is set to the inversed length of the globally best tour } (L_{ gb })^{ -1 } \\text{ and } 0 < \\alpha < 1 \\text{ is the pheromone decay parameter.}$"
            };

            foreach (var edge in DistributedMemory.Edges) {
                edge.Pheromone = (1 - Alpha) * edge.Pheromone;

                if (acs) {
                    /* Only Reinforce Best Edges */
                    if (IsEdgeInTour(edge, GlobalBest.Edges)) {
                        var lgbInversed = Math.Pow(GlobalBest.CalcCosts(), -1);
                        Equations[gur].Variables = new Dictionary<string, object> {
                            { "r", edge.Node1.Index },
                            { "s", edge.Node2.Index },
                            { "alpha", Alpha },
                            { "tau", edge.Pheromone },
                            { "triangle", lgbInversed }
                        };

                        edge.Pheromone += Alpha * lgbInversed;
                        Equations[gur].Result = $"$\\tau({edge.Node1.Index},{edge.Node2.Index}) = {edge.Pheromone}$";
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
            var lur = "Local Updating Rule";
            Equations[lur] = new("$\\tau(r,s) \\leftarrow (1-\\rho) \\cdot \\tau(r,s) + \\rho \\cdot \\triangle \\tau(r,s)$") {
                Dummy = "$\\tau(?r?,?s?) = (1-?rho?) \\cdot ?tau? + ?rho? \\cdot ?triangle?$",
                Description = "$\\text{While building a solution (i.e., a tour) of the TSP, ants visit edges and change their pheromone level by applying the local updating rule. Ants build their tours by both heuristic information (they prefer to choose short edges) and pheromone information. The term } \\triangle \\tau(r,s) \\text{ is set to the initial pheromone value } \\tau_0.$"
            };

            foreach (var edge in DistributedMemory.Edges) {
                if (IsEdgeInTour(edge, k.Path.Edges)) {
                    Equations[lur].Variables = new Dictionary<string, object> {
                        { "r", edge.Node1.Index },
                        { "s", edge.Node2.Index },
                        { "rho", Rho },
                        { "tau", edge.Pheromone },
                        { "triangle", InitialPheromone }
                    };

                    edge.Pheromone = ((1 - Rho) * edge.Pheromone) + (Rho * InitialPheromone);
                    Equations[lur].Result = $"$\\tau({edge.Node1.Index},{edge.Node2.Index}) = {edge.Pheromone}$";
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