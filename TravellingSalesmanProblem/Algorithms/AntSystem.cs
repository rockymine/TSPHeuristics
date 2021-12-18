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
        public double InitialPheromone { get; set; }
        private static readonly Random Random = new();
        private GraphProblem XBest = new();
        private GraphProblem X = new();
        public override LinkedList<GraphState> FindPath(GraphProblem graph) {
            var history = new LinkedList<GraphState>();
            var nearestNeighbor = new NearestNeighbour { Start = graph.Nodes[Random.Next(0, graph.Nodes.Count)] };
            var initialSolution = nearestNeighbor.FindPath(graph).Last();
            
            InitialPheromone = Math.Pow((initialSolution.Nodes.Count) * initialSolution.Distance, -1);

            X = GraphProblem.ConnectedGraphProblem(graph);
            X.Edges.ForEach(e => e.Pheromone += InitialPheromone);
            
            /* Give each ant a starting node */
            Colony = Enumerable.Range(1, AntCount)
                .Select(i => new Ant { Start = graph.Nodes[i - 1] }).ToList();

            var state = new GraphState {
                Nodes = initialSolution.Nodes,
                PathEdges = X.Edges
            };

            history.AddLast(state);
            var iteration = 0;

            for (int i = 0; i < Iterations; i++) {
                BuildAntPaths(graph);

                XBest = Colony.OrderBy(a => a.Path.Costs).FirstOrDefault().Path;

                X = GlobalUpdating(X);

                iteration++;
                history.AddLast(AdvanceState(history.Last.Value, iteration));

                if (BreakWhenPathsAreEqual && Colony.TrueForAll(a => a.Path.Costs == XBest.Costs))
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

            UpdateStateMessages(newState);
            return newState;
        }

        private GraphProblem GlobalUpdating(GraphProblem graph) {
            var newGraph = graph.DeepCopy();
            var edges = newGraph.Edges;

            foreach (var edge in edges) {
                /* Nodes are not completely copied. Without this piece of code
                 * a NullReferenceError is thrown inside PheromoneVisibility. When using
                 * the Id instead of the node the pheromone decreases instead of increasing. */
                edge.Node1 = graph.Nodes.Find(n => n.Index == edge.Node1Id);
                edge.Node2 = graph.Nodes.Find(n => n.Index == edge.Node2Id);

                //Ant System Global Updating Rule
                //edge.Pheromone = (1 - Alpha) * edge.Pheromone + Colony.Sum(n => DeltaAntij(n, edge));

                //Ant Colony System Global Updating Rule
                edge.Pheromone = (1 - Alpha) * edge.Pheromone;
                if (edge.IsInside(XBest.Edges)) {
                    var lgbInversed = Math.Pow(XBest.Costs, -1);
                    edge.Pheromone += Alpha * lgbInversed;
                }
            }

            return new GraphProblem {
                Nodes = graph.Nodes,
                Edges = edges
            };
        }

        private static double DeltaAntij(Ant ant, Edge edge) {
            if (edge.IsInside(ant.Path.Edges))
                return 1 / ant.Path.Costs;

            return 0;
        }

        private void BuildAntPaths(GraphProblem graph) {
            Colony.ForEach(a => a.Initialize(graph));
            for (int j = 0; j < graph.Nodes.Count; j++) {
                foreach (var ant in Colony) {
                    var next = ant.Unvisited.Any() ? StateTransitionRule(ant) : ant.Start;
                    ant.UpdatePath(ant.Current, next);
                }
            }
        }

        private Node StateTransitionRule(Ant ant) {
            var orderedList = ant.Unvisited.OrderByDescending(node => MoveProbability(ant, ant.Current, node));
            return orderedList.FirstOrDefault();
        }

        private double MoveProbability(Ant ant, Node i, Node j) {
            var current = PheromoneVisibility(i, j);
            var sum = ant.Unvisited.Sum(n => PheromoneVisibility(i, n));
            return current / sum;
        }

        private double PheromoneVisibility(Node i, Node j) {
            var edge = X.Edges.Find(e => e.IsBetween(i, j));
            return edge.Pheromone * Math.Pow(1 / edge.Distance, Beta);
        }

        public override void UpdateStateMessages(GraphState state) {
            state.Messages["Iteration"] = state.Iteration.ToString();
            state.Messages["Route"] = string.Join("-", state.Path.Select(n => n.Index));
            state.Messages["Distance"] = Math.Round(state.Distance, 2).ToString();
            state.Messages["Ant Distances"] = string.Join(';', Colony.Select(a => Math.Round(a.Path.Costs, 2)));
        }
    }
}