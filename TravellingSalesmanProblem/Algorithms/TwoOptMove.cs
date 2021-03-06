using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class TwoOptMove {
        public GraphProblem Graph { get; set; }
        public SwapInfo SwapInfo => GenerateSwapInfo();
        public int I { get; set; }
        public int J { get; set; }
        private Node NodeI => Graph.Nodes[I];
        private Node NodeI1 => Graph.Nodes[I + 1];
        private Node NodeJ => Graph.Nodes[J];
        private Node NodeJ1 => Graph.Nodes[J + 1];
        private double Di_j => Edge.GetDistanceRounded(NodeI, NodeJ);
        private double Di1_j1 => Edge.GetDistanceRounded(NodeI1, NodeJ1);
        private double Di_i1 => Edge.GetDistanceRounded(NodeI, NodeI1);
        private double Dj_j1 => Edge.GetDistanceRounded(NodeJ, NodeJ1);
        public double Costs => Math.Round(Di_j + Di1_j1 - Di_i1 - Dj_j1, 1);

        public TwoOptMove(GraphProblem graph, int i, int j) {
            Graph = graph;
            I = i;
            J = j;
        }

        public SwapInfo GenerateSwapInfo() {
            var nodes = new List<Node> { NodeI, NodeJ };
            string calculation = $"${Di_j}+{Di1_j1}-{Di_i1}-{Dj_j1}={Costs}$";
            return new(nodes, calculation);
        }

        public GraphProblem SwapEdges() {
            var tour = Graph.Nodes.ToArray();
            var i = I + 1;
            var j = J + 1;

            var a = tour[0..i];
            var b = tour[i..j];
            var c = tour[j..^0];
            var nodes = a.Concat(b.Reverse()).Concat(c).ToList();

            var best = new GraphProblem { Nodes = nodes };
            best.ConnectPathNodes();
            return best;
        }
    }
}