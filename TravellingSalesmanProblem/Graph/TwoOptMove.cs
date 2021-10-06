﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravellingSalesmanProblem.Graph {
    public class TwoOptMove {
        public GraphProblem Graph { get; set; }
        public int I { get; set; }
        public int J { get; set; }

        public TwoOptMove(GraphProblem graph, int i, int j) {
            Graph = graph;
            I = i;
            J = j;
        }

        public double EdgeSwapCost() {
            var tour = Graph.Nodes;
            var ij = Edge.GetDistanceRounded(tour[I], tour[J]);
            var i1j1 = Edge.GetDistanceRounded(tour[I + 1], tour[J + 1]);
            var ii1 = Edge.GetDistanceRounded(tour[I], tour[I + 1]);
            var jj1 = Edge.GetDistanceRounded(tour[J], tour[J + 1]);
            return Math.Round(ij + i1j1 - ii1 - jj1, 0);
        }

        public GraphProblem SwapEdges() {
            //Console.WriteLine(string.Join("-", Graph.Nodes.Select(n => n.Index)));
            //Console.WriteLine($"Node i: {Graph.Nodes[I].Index}, Node j: {Graph.Nodes[J].Index}");

            Graph.Nodes[I].Color = "orange";
            Graph.Nodes[J].Color = "pink";

            I++;
            J++;
            var tour = Graph.Nodes.ToArray();
            var n = tour.Length;

            var a = tour[0..I];
            var b = tour[I..J];
            var c = tour[J..n];

            var best = new GraphProblem { Nodes = a.Concat(b.Reverse()).Concat(c).ToList() };
            //Console.WriteLine(string.Join("-", best.Nodes.Select(n => n.Index)));
            best.ConnectPathNodes();
            return best;
        }
    }
}
