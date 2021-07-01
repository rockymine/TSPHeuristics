using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public abstract class Algorithm {
        public abstract IEnumerable<GraphState> FindPath(GraphProblem graph);
        public abstract void UpdateStateMessages(GraphState state);
    }

    public enum AlgorithmEnum {
        NN = 0,
        SA = 1
    }
}