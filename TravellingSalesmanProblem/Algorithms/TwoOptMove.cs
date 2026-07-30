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
        private double Di_j => Edge.GetDistance(NodeI, NodeJ);
        private double Di1_j1 => Edge.GetDistance(NodeI1, NodeJ1);
        private double Di_i1 => Edge.GetDistance(NodeI, NodeI1);
        private double Dj_j1 => Edge.GetDistance(NodeJ, NodeJ1);

        /* Measured exactly so the sign of the delta agrees with the tour costs the search compares
         * it against; only the displayed calculation is rounded. */
        public double Costs => Di_j + Di1_j1 - Di_i1 - Dj_j1;

        public TwoOptMove(GraphProblem graph, int i, int j) {
            Graph = graph;
            I = i;
            J = j;
        }

        public SwapInfo GenerateSwapInfo() {
            var nodes = new List<Node> { NodeI, NodeJ };
            string calculation = $"${Round(Di_j)}+{Round(Di1_j1)}-{Round(Di_i1)}-{Round(Dj_j1)}={Round(Costs)}$";
            return new(nodes, calculation);
        }

        private static double Round(double value) => Math.Round(value, 1);

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