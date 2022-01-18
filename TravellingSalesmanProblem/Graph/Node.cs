using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TravellingSalesmanProblem.Graph {
    public class Node {
        public int Index { get; set; }
        public bool Visited { get; set; }
        public Vector2 Position { get; set; }
        public List<Edge> Edges { get; set; } = new();

        public Node Copy() {
            return new Node {
                Index = Index,
                Visited = Visited,
                Position = Position
            };
        }
    }
}