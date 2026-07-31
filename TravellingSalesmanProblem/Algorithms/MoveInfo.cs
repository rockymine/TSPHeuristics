using System.Collections.Generic;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    /* What a single move did, in the form the tables and the info panel need.
     *
     * This replaces SwapInfo, which held exactly two nodes and a pre-rendered string. Two nodes
     * describe a swap and a 2-opt move, but a 3-opt move has three cut points and one of seven
     * reconnections, and a double bridge has four - so the type and the tour positions belong here
     * as well. A move history that mixes types also has to be able to say, per row, which rule
     * produced it; the run level NeighbourType cannot answer that. */
    public class MoveInfo {
        public NeighbourType Type { get; set; }
        /* The nodes the move names: i and j for a swap or a 2-opt move, the cut points otherwise. */
        public List<Node> Nodes { get; set; }
        /* Their positions in the tour the move was built on. */
        public List<int> Positions { get; set; }
        /* Exact change in tour length. Negative means shorter. */
        public double Delta { get; set; }
        /* The same change written out with the numbers substituted, as LaTeX. */
        public string Calculation { get; set; }

        public MoveInfo(NeighbourType type, List<Node> nodes, List<int> positions,
                        double delta, string calculation) {
            Type = type;
            Nodes = nodes;
            Positions = positions;
            Delta = delta;
            Calculation = calculation;
        }

        public MoveInfo DeepCopy() =>
            new(Type, new List<Node>(Nodes), new List<int>(Positions), Delta, Calculation);
    }
}
