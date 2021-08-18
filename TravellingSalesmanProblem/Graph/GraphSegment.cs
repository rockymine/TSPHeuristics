using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravellingSalesmanProblem.Graph {
    public class GraphSegment {
        public string Identifier { get; set; }
        public SegmentType Type { get; set; }
        public List<Edge> Edges { get; set; } = new();
        public string Info { get; set; }
    }

    public enum SegmentType {
        Normal = 0,
        Reversed = 1
    }
}