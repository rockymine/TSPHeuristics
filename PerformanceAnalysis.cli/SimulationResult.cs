using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Algorithms;

namespace PerformanceAnalysis.cli {
    public class SimulationResult {
        public int GraphSizeX { get; set; }
        public int GraphSizeY { get; set; }
        public int GraphNodeCount { get; set; }
        public string Heuristic { get; set; }
        public int PhaseLength { get; set; }
        public double StartTemp { get; set; }
        public double MinTemp { get; set; }
        public double Alpha { get; set; }
        public NeighbourType NeighbourEnum { get; set; }
        public string RunTime { get; set; }
        public double TourLength { get; set; }
    }
}