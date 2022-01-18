using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Algorithms;

namespace PerformanceAnalysis.cli {
    public class SimulationResult {
        public string Instance { get; set; }
        public int NodeCount { get; set; }
        public string Heuristic { get; set; }
        public int PhaseLength { get; set; }
        public double StartTemp { get; set; }
        public double MinTemp { get; set; }
        public double Alpha { get; set; }
        public double Beta { get; set; }
        public int AntCount { get; set; }
        public string NeighbourType { get; set; }
        public string DescentType { get; set; }
        public double RunTime { get; set; }
        public double TourLength { get; set; }
        public int Iterations { get; set; }
        public double FinalTemp { get; set; }
        public string Tour { get; set; }
    }
}