﻿using System;
using System.IO;
using TravellingSalesmanProblem.Graph;
using Utility.CommandLine;

namespace GraphGenerator.cli {
    internal class Program {
        [Argument('x', "graphSizeX")]
        private static int GraphSizeX { get; set; }
        [Argument('y', "graphSizeY")]
        private static int GraphSizeY { get; set; }
        [Argument('n', "graphNodeCount")]
        private static int GraphNodeCount { get; set; }
        [Argument('p', "path")]
        private static string Path { get; set; }
        static void Main(string[] args) {
            Arguments.Populate();
            var graph = GraphProblem.RandomGraphProblem(GraphSizeX, GraphSizeY, GraphNodeCount);
            File.WriteAllText(Path, GraphProblem.SerializeGraph(graph));
        }
    }
}