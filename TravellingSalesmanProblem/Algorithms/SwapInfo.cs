using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class SwapInfo {
        public List<Node> Nodes { get; set; }
        public string Calculation { get; set; }
        public SwapInfo(List<Node> nodes, string calculation) {
            Nodes = nodes;
            Calculation = calculation;
        }

        public SwapInfo DeepCopy() => new(new List<Node>(Nodes), Calculation);

        public override string ToString() {
            var nodes = string.Join(',', Nodes.Select(n => n.Index));
            return nodes + "; " + Calculation;
        }
    }
}