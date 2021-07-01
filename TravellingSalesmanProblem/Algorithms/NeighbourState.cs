using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class NeighbourState {
        private static readonly Random Random = new();
        public static GraphProblem TwoOptSwap(GraphProblem graph) {
            Node[] graphnodes = graph.Nodes.ToArray();
            var size = graphnodes.Length - 1;
            var pos1 = Random.Next(1, size - 2);
            var pos2 = Random.Next(pos1, size - 1);

            var first = new List<Node>();
            var second = new List<Node>();
            var third = new List<Node>();            

            //1. take route [0] to route [i - 1] and add them in oder to newroute
            for (int i = 0; i < pos1; i++) {
                first.Add(graphnodes[i]);
            }
            //2. take route [i] to route [k] and add them in reverse order to newroute
            for (int i = pos1; i <= pos2; i++) {
                second.Add(graphnodes[i]);
            }
            //3. take route [k + 1] to end and add them in order to newroute
            for (int i = pos2 + 1; i <= size; i++) {
                third.Add(graphnodes[i]);
            }

            second.Reverse();
            var newroute = new List<Node>();
            newroute.AddRange(first);
            newroute.AddRange(second);
            newroute.AddRange(third);

            var swapped = new GraphProblem {
                Nodes = newroute
            };
            swapped.ConnectPathNodes();
            return swapped;
        }

        public static GraphProblem Interchange(GraphProblem graph) {
            var pos1 = Random.Next(1, graph.Nodes.Count - 2);
            var pos2 = Random.Next(1, graph.Nodes.Count - 2);
            var temp = graph.Nodes;

            if (pos1 == pos2)
                return graph;            

            Node node = temp[pos1];
            temp[pos1] = temp[pos2];
            temp[pos2] = node;

            var interchanged = new GraphProblem {
                Nodes = temp
            };

            interchanged.ConnectPathNodes();
            return interchanged;
        }
    }
}