using ChartData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Algorithms;

namespace TravellingSalesmanProblem.Graph {
    /* Shorter: taken without a draw. Longer: the Metropolis rule took it (r < P) or rejected it. */
    public enum DrawOutcome { Shorter, LongerAccepted, LongerRejected }

    public class GraphState {
        public List<Node> Nodes { get; set; } = new();
        public List<Node> Path { get; set; } = new();
        public List<Edge> PathEdges { get; set; } = new();
        public MoveInfo MoveInfo { get; set; }
        public double Distance { get; set; }
        public double Temperature { get; set; }
        public int Iteration { get; set; }
        public Dictionary<string, string> Messages { get; set; } = new();
        public Dictionary<string, MathString> Equations { get; set; } = new();
        /* The rules worked through for this state (move, Metropolis, cooling, pheromone update),
         * in display order. Derivations are immutable once built, so copies share them.
         *
         * A heuristic can hand over a recipe instead (DeriveLater): Simulated Annealing records a
         * state per draw, and building every draw's cards up front made a run four times slower in
         * the browser, for cards of which only the ones played back are ever read. */
        private List<Derivation> _derivations = new();
        private Func<List<Derivation>> _deriveLater;

        public List<Derivation> Derivations {
            get {
                if (_deriveLater != null) {
                    _derivations = _deriveLater() ?? new();
                    _deriveLater = null;
                }
                return _derivations;
            }
            set {
                _derivations = value;
                _deriveLater = null;
            }
        }

        public void DeriveLater(Func<List<Derivation>> build) => _deriveLater = build;
        public List<ChartInfo> ChartInfo { get; set; } = new();

        /* Simulated Annealing records every draw: what became of the neighbour drawn in this state.
         * Null for states that are not a draw (the start, the final best tour, other heuristics). */
        public DrawOutcome? Outcome { get; set; }

        /* The point of the charts this state belongs to, highlighted while it is shown, or -1. Lets
         * every state share one chart for the whole run instead of carrying a copy of its own. */
        public int ChartCursor { get; set; } = -1;

        public GraphProblem ToGraphProblem() {
            return new GraphProblem {
                Nodes = Path,
                Edges = PathEdges
            };
        }

        public double CalcCosts() => PathEdges.Sum(e => e.Distance);

        public GraphState DeepCopy() {
            var state = new GraphState();
            state.Nodes.AddRange(Nodes);
            state.Path.AddRange(Path);
            state.PathEdges.AddRange(PathEdges);
            state.MoveInfo = MoveInfo?.DeepCopy();
            state.Derivations = new List<Derivation>(Derivations ?? new());
            state.Distance = Distance;
            state.Temperature = Temperature;
            state.Iteration = Iteration;
            state.Messages = new Dictionary<string, string>(Messages);
            state.Equations = Equations?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DeepCopy());
            state.ChartInfo = ChartInfo?.Select(c => c.DeepCopy()).ToList();
            state.Outcome = Outcome;
            state.ChartCursor = ChartCursor;

            return state;
        }
    }
}