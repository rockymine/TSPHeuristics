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

        private static LinkedList<GraphState> History { get; set; }
        private static GraphProblem Instance;

        private static void Main(string[] args) {
            Arguments.Populate();
            Instance = GraphProblem.FromText(File.ReadAllText(Graph));

            var instance = System.IO.Path.GetFileNameWithoutExtension(Graph);
            //var graph = GraphProblem.RandomGraphProblem(GraphSizeX, GraphSizeY, GraphNodeCount);
            
            StringBuilder sb = new();
            sb.Append(instance);

            Stopwatch sw = new();
            sw.Restart();

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

            File.WriteAllText(Path + sb.ToString() + Number + ".json", JsonConvert.SerializeObject(result));
        }
    }
}