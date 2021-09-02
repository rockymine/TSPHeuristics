using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public abstract class Algorithm {
        public Dictionary<string, MathString> Equations { get; set; } = new();
        public abstract IEnumerable<GraphState> FindPath(GraphProblem graph);
        public abstract IEnumerable<GraphState> MultiStart(GraphProblem graph);
        public abstract void UpdateStateMessages(GraphState state);
    }

    public static class Utils {
        private static readonly Random Random = new();
        public static void Shuffle<T>(this IList<T> list) {
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = Random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    public enum AlgorithmEnum {
        NN = 0,
        SA = 1,
        ACS = 3,
        HC = 4
    }
}