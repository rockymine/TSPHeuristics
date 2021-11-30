using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TravellingSalesmanProblem.Algorithms;
using TravellingSalesmanProblem.Graph;
using Utility.CommandLine;

namespace PerformanceAnalysis.cli {
    internal class Program {
        [Argument('k', "antCount")]
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

        private static LinkedList<GraphState> History { get; set; }

        private static void Main(string[] args) {
            Arguments.Populate();
            var graph = GraphProblem.FromText(File.ReadAllText(Graph));
            
            //var graph = GraphProblem.RandomGraphProblem(GraphSizeX, GraphSizeY, GraphNodeCount);
            Stopwatch sw = new();

            sw.Restart();
            switch (Heuristic.ToLower()) {
                case "sa":
                case "annealing":
                    SimulatedAnnealing sa = new();
                    sa.Alpha = Alpha;
                    sa.MinTemp = MinTemp;
                    sa.PhaseLength = PhaseLength;
                    sa.StartTemp = StartTemp;
                    sa.NeighbourEnum = NeighbourType;
                    History = sa.FindPath(graph);
                    break;
                case "aco":
                case "antsystem":
                    AntSystem ant = new();
                    ant.AntCount = AntCount;
                    ant.Alpha = Alpha;
                    ant.Beta = Beta;
                    History = ant.FindPath(graph);
                    break;
                case "hc":
                case "hillclimbing":
                    HillClimbing hill = new();
                    hill.NeighbourEnum = NeighbourType;
                    hill.DescentType = DescentType;
                    History = hill.FindPath(graph);
                    break;
            }

            sw.Stop();
            var last = History.Last.Value;

            var result = new SimulationResult {
                GraphSizeX = 0,
                GraphSizeY = 0,
                GraphNodeCount = 0,
                Heuristic = Heuristic,
                PhaseLength = PhaseLength,
                StartTemp = StartTemp,
                MinTemp = MinTemp,
                Alpha = Alpha,
                Beta = Beta,
                AntCount = AntCount,
                NeighbourType = NeighbourType,
                DescentType = DescentType,
                RunTime = sw.Elapsed.ToString(),
                TourLength = last.Distance,
                Iterations = last.Iteration,
                FinalTemp = last.Temperature
            };

            File.WriteAllText(Path + ".json", JsonConvert.SerializeObject(result));
        }
    }
}