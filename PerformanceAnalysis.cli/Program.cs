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
        [Argument('x', "graphSizeX")]
        private static int GraphSizeX { get; set; }
        [Argument('y', "graphSizeY")]
        private static int GraphSizeY { get; set; }
        [Argument('c', "graphNodeCount")]
        private static int GraphNodeCount { get; set; }
        [Argument('k', "antCount")]
        private static int AntCount { get; set; }
        [Argument('b', "beta")]
        private static double Beta { get; set; }
        [Argument('h', "heuristic")]
        private static string Heuristic { get; set; }
        [Argument('g', "graph")]
        private static string Graph { get; set; }
        [Argument('i', "phase")]
        private static int PhaseLength { get; set; }
        [Argument('s', "start")]
        private static double StartTemp { get; set; }
        [Argument('m', "min")]
        private static double MinTemp { get; set; }
        [Argument('a', "alpha")]
        private static double Alpha { get; set; }
        [Argument('n', "neighbor")]
        private static NeighbourType NeighbourEnum { get; set; }
        [Argument('p', "path")]
        private static string Path { get; set; }
        private static LinkedList<GraphState> History { get; set; }
        private static void Main(string[] args) {
            Arguments.Populate();
            Graph = File.ReadAllText("C:/Users/Michael/source/repos/TSPHeuristics/GraphGenerator.cli/bin/Debug/net5.0/40Nodes");
            var graph = GraphProblem.FromText(Graph);
            
            //var graph = GraphProblem.RandomGraphProblem(GraphSizeX, GraphSizeY, GraphNodeCount);
            Stopwatch sw = new();

            for (int i = 0; i < 10; i++) {
                sw.Restart();
                switch (Heuristic.ToLower()) {
                    case "sa":
                    case "annealing":
                        SimulatedAnnealing sa = new();
                        sa.Alpha = Alpha;
                        sa.MinTemp = MinTemp;
                        sa.PhaseLength = PhaseLength;
                        sa.StartTemp = StartTemp;
                        sa.NeighbourEnum = NeighbourEnum;
                        History = sa.FindPath(graph);
                        break;
                    case "aco":
                        AntSystem ant = new();
                        ant.AntCount = AntCount;
                        ant.Alpha = Alpha;
                        ant.Beta = Beta;
                        History = ant.FindPath(graph);
                        break;
                }

                sw.Stop();
                var last = History.Last.Value;

                var result = new SimulationResult {
                    GraphSizeX = GraphSizeX,
                    GraphSizeY = GraphSizeY,
                    GraphNodeCount = GraphNodeCount,
                    Heuristic = Heuristic,
                    PhaseLength = PhaseLength,
                    StartTemp = StartTemp,
                    MinTemp = MinTemp,
                    Alpha = Alpha,
                    Beta = Beta,
                    AntCount = AntCount,
                    NeighbourEnum = NeighbourEnum,
                    RunTime = sw.Elapsed.ToString(),
                    TourLength = last.Distance,
                    Iterations = last.Iteration,
                    FinalTemp = last.Temperature
                };

                File.WriteAllText(Path + i + ".json", JsonConvert.SerializeObject(result));
            }
        }
    }
}