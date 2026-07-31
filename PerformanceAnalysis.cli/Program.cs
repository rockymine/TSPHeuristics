using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using TravellingSalesmanProblem.Algorithms;
using TravellingSalesmanProblem.Graph;
using Utility.CommandLine;

namespace PerformanceAnalysis.cli {
    internal class Program {
        [Argument('k', "ants")]
        private static int AntCount { get; set; }
        [Argument('b', "beta")]
        private static double Beta { get; set; }
        [Argument('h', "heuristic")]
        private static string Heuristic { get; set; }
        [Argument('i', "phase")]
        private static int PhaseLength { get; set; }
        [Argument('s', "start")]
        private static double StartTemp { get; set; }
        [Argument('m', "min")]
        private static double MinTemp { get; set; }
        [Argument('a', "alpha")]
        private static double Alpha { get; set; }
        [Argument('n', "neighbor")]
        private static NeighbourType NeighbourType { get; set; }
        [Argument('p', "path")]
        private static string Path { get; set; }
        [Argument('g', "graph")]
        private static string Graph { get; set; }
        [Argument('d', "descent")]
        private static DescentType DescentType { get; set; }
        [Argument('z', "number")]
        private static int Number { get; set; }
        [Argument('o', "iterations")]
        private static int Iterations { get; set; }
        /* TSPLIB fixes an edge weight type per instance, and scoring a GEO instance with the
         * euclidean default puts the tour length on a completely different scale. */
        [Argument('t', "metric")]
        private static string Metric { get; set; }
        /* "proportional" is the random proportional rule of the Ant System and the default;
         * "greedy" is the deterministic rule this implementation originally used, which the
         * analysis scripts ask for explicitly to reproduce the numbers published in the thesis. */
        [Argument('r', "selection")]
        private static string Selection { get; set; }

        private static LinkedList<GraphState> History { get; set; }
        private static GraphProblem Instance;

        private static void Main(string[] args) {
            Arguments.Populate();

            if (string.IsNullOrWhiteSpace(Heuristic)) {
                Console.Error.WriteLine("No heuristic given (-h annealing|antsystem|hillclimbing).");
                Environment.Exit(2);
            }

            Edge.Metric = ParseMetric(Metric);
            Instance = GraphProblem.FromText(File.ReadAllText(Graph));

            var instance = System.IO.Path.GetFileNameWithoutExtension(Graph);
            //var graph = GraphProblem.RandomGraphProblem(GraphSizeX, GraphSizeY, GraphNodeCount);
            
            StringBuilder sb = new();
            sb.Append(instance);

            var sw = Stopwatch.StartNew();

            switch (Heuristic.ToLower()) {
                case "sa":
                case "annealing":
                    sb.Append("_SA_" + NeighbourType.ToString())
                        .Append("_PhaseLength_" + PhaseLength + "_");

                    SimulatedAnnealing sa = new();
                    sa.Alpha = Alpha;
                    sa.MinTemp = MinTemp;
                    sa.PhaseLength = PhaseLength;
                    sa.StartTemp = StartTemp;
                    sa.NeighbourEnum = NeighbourType;
                    History = sa.FindPath(Instance);
                    break;
                case "aco":
                case "antsystem":
                    sb.Append("_AS_Ants_" + AntCount)
                        .Append("_Iterations_" + Iterations + "_");

                    AntSystem ant = new();
                    ant.AntCount = AntCount;
                    ant.Iterations = Iterations;
                    ant.Alpha = Alpha;
                    ant.Beta = Beta;
                    ant.SelectionRule = ParseSelection(Selection);
                    History = ant.FindPath(Instance);
                    break;
                case "hc":
                case "hillclimbing":
                    sb.Append("_HC_" + NeighbourType.ToString())
                        .Append("_" + DescentType.ToString() + "_");

                    HillClimbing hc = new();
                    hc.NeighbourEnum = NeighbourType;
                    hc.DescentType = DescentType;
                    History = hc.FindPath(Instance);
                    break;
                default:
                    Console.Error.WriteLine($"Unknown heuristic '{Heuristic}'.");
                    Environment.Exit(2);
                    break;
            }

            var last = History.Last.Value;
            sw.Stop();

            var seconds = Math.Round((double)sw.ElapsedMilliseconds / 1000, 3);

            var result = new SimulationResult {
                Instance = instance,
                NodeCount = last.Path.Count - 1,
                Heuristic = Heuristic,
                RunTime =  seconds,
                Iterations = last.Iteration,
                Tour = string.Join("-", last.Path.Select(n => n.Index)),
                TourLength = last.Distance,
                NeighbourType = NeighbourType.ToString(),
                DescentType = DescentType.ToString(),
                PhaseLength = PhaseLength,
                StartTemp = StartTemp,
                MinTemp = MinTemp,
                Alpha = Alpha,
                Beta = Beta,
                AntCount = AntCount,
                FinalTemp = last.Temperature
            };

            var target = Path + sb.ToString() + Number + ".json";
            var directory = System.IO.Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(target, JsonConvert.SerializeObject(result));
        }

        private static SelectionRule ParseSelection(string selection) {
            return (selection ?? string.Empty).Trim().ToLower() switch {
                "greedy" => SelectionRule.Greedy,
                _ => SelectionRule.Proportional
            };
        }

        private static DistanceMetric ParseMetric(string metric) {
            return (metric ?? string.Empty).Trim().ToLower() switch {
                "geo" => DistanceMetric.Geo,
                "euc_2d" or "euc2d" => DistanceMetric.Euclidean2D,
                _ => DistanceMetric.Euclidean
            };
        }
    }
}