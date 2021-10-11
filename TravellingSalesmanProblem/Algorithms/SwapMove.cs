using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class SwapMove {
        public GraphProblem Graph { get; set; }
        public SwapInfo SwapInfo { get; set; }
        public int I { get; set; }
        public int J { get; set; }
        private Node NodeI => Graph.Nodes[I];
        private Node NodeJ => Graph.Nodes[J];
        private Node IPrev => Graph.Nodes[I - 1];
        private Node JPrev => Graph.Nodes[J - 1];
        private Node INext => Graph.Nodes[I + 1];
        private Node JNext => Graph.Nodes[J + 1];
        private double I_INext => Edge.GetDistanceRounded(NodeI, INext);
        private double I_JNext => Edge.GetDistanceRounded(NodeI, JNext);
        private double J_INext => Edge.GetDistanceRounded(NodeJ, INext);
        private double J_JNext => Edge.GetDistanceRounded(NodeJ, JNext);
        private double IPrev_I => Edge.GetDistanceRounded(IPrev, NodeI);
        private double IPrev_J => Edge.GetDistanceRounded(IPrev, NodeJ);
        private double JPrev_I => Edge.GetDistanceRounded(JPrev, NodeI);
        private double JPrev_J => Edge.GetDistanceRounded(JPrev, NodeJ);

        public SwapMove(GraphProblem graph, int i, int j) {
            Graph = graph;
            I = i;
            J = j;
        }

        public double CalcCosts() {
            double added;
            double deleted;

            if (NodeJ == IPrev || INext == JPrev) {
                added = IPrev_J + I_JNext;
                deleted = IPrev_I + J_JNext;
                SwapInfo = new SwapInfo(new List<Node> { NodeI, NodeJ },
                    $"${IPrev_J}+{I_JNext}-{IPrev_I}-{J_JNext}$");
            } else if (NodeI == JNext || JNext == IPrev) {
                added = JPrev_I + J_INext;
                deleted = JPrev_J + I_INext;
                SwapInfo = new SwapInfo(new List<Node> { NodeI, NodeJ },
                    $"${JPrev_I}+{J_INext}-{JPrev_J}-{I_INext}$");
            } else {
                added = IPrev_J + J_INext + JPrev_I + I_JNext;
                deleted = IPrev_I + I_INext + JPrev_J + J_JNext;
                SwapInfo = new SwapInfo(new List<Node> { NodeI, NodeJ },
                    $"${IPrev_J}+{J_INext}+{JPrev_I}+{I_JNext}-{IPrev_I}-{I_INext}-{JPrev_J}-{J_JNext}$");
            }

            return Math.Round(added - deleted, 1);
        }

        public GraphProblem SwapNodes() {
            //Graph = Graph.DeepCopy();

            var nodes = Graph.DeepCopy().Nodes;
            var node = nodes[I];
            nodes[I] = nodes[J];
            nodes[J] = node;

            Graph.Nodes = nodes;
            Graph.ConnectPathNodes();
            return Graph;

            //var best = new GraphProblem { Nodes = nodes };
            //best.ConnectPathNodes();
            //return best;
        }
    }
}