using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class SwapMove {
        public GraphProblem Graph { get; set; }
        public SwapInfo SwapInfo => GenerateSwapInfo();
        public int I { get; set; }
        public int J { get; set; }
        private Node NodeI => Graph.Nodes[I];
        private Node NodeJ => Graph.Nodes[J];
        private Node IPrev => Graph.Nodes[I - 1];
        private Node JPrev => Graph.Nodes[J - 1];
        private Node INext => Graph.Nodes[I + 1];
        private Node JNext => Graph.Nodes[J + 1];
        private double I_INext => Edge.GetDistance(NodeI, INext);
        private double I_JNext => Edge.GetDistance(NodeI, JNext);
        private double J_INext => Edge.GetDistance(NodeJ, INext);
        private double J_JNext => Edge.GetDistance(NodeJ, JNext);
        private double IPrev_I => Edge.GetDistance(IPrev, NodeI);
        private double IPrev_J => Edge.GetDistance(IPrev, NodeJ);
        private double JPrev_I => Edge.GetDistance(JPrev, NodeI);
        private double JPrev_J => Edge.GetDistance(JPrev, NodeJ);
        public double Costs => CalcCosts();
        public SwapMove(GraphProblem graph, int i, int j) {
            Graph = graph;
            I = i;
            J = j;
        }

        /* Measured exactly so the sign of the delta agrees with the tour costs the search compares
         * it against; only the displayed calculation is rounded. */
        public double CalcCosts() {
            if (NodeI == JPrev || JPrev == INext) {
                return IPrev_J + I_JNext - IPrev_I - J_JNext;
            } else if (NodeI == JNext || JNext == IPrev) {
                return JPrev_I + J_INext - JPrev_J - I_INext;
            } else {
                return IPrev_J + J_INext + JPrev_I + I_JNext - IPrev_I - I_INext - JPrev_J - J_JNext;
            }
        }

        public SwapInfo GenerateSwapInfo() {
            var nodes = new List<Node> { NodeI, NodeJ };
            string calculation;

            if (NodeI == JPrev || JPrev == INext) {
                calculation = $"${Round(IPrev_J)}+{Round(I_JNext)}-{Round(IPrev_I)}-{Round(J_JNext)}={Round(Costs)}$";
            } else if (NodeI == JNext || JNext == IPrev) {
                calculation = $"${Round(JPrev_I)}+{Round(J_INext)}-{Round(JPrev_J)}-{Round(I_INext)}={Round(Costs)}$";
            } else {
                calculation = $"${Round(IPrev_J)}+{Round(J_INext)}+{Round(JPrev_I)}+{Round(I_JNext)}-{Round(IPrev_I)}-{Round(I_INext)}-{Round(JPrev_J)}-{Round(J_JNext)}={Round(Costs)}$";
            }

            return new SwapInfo(nodes, calculation);
        }

        private static double Round(double value) => Math.Round(value, 1);

        public GraphProblem SwapNodes() {
            var nodes = Graph.Nodes.ToArray();
            var node = nodes[I];
            nodes[I] = nodes[J];
            nodes[J] = node;

            var best = new GraphProblem { Nodes = nodes.ToList() };
            best.ConnectPathNodes();
            return best;
        }
    }
}