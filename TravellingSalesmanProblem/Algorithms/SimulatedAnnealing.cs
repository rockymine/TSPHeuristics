using ChartData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class SimulatedAnnealing : Algorithm {
        public int PhaseLength { get; set; }
        public int MaxPhases { get; set; }
        public double StartTemp { get; set; }
        public double MinTemp { get; set; }
        public double Alpha { get; set; }
        public NeighbourType NeighbourEnum { get; set; }
        public bool CalculateTemperature { get; set; }

        /* Every draw is a state of its own up to this many draws. The schedule's settings allow
         * runs of millions of draws (a cooling factor of 0.9999 with phases of 80), which no
         * browser holds as states; such a run is recorded once per phase, or every n-th phase. */
        public const int MaxRecordedDraws = 20_000;

        private GraphProblem X = new();
        private GraphProblem XBest = new();

        private static readonly Random Random = new();

        /* The history used to hold only the start and each new best tour, so playback jumped from
         * improvement to improvement and the moves that make annealing what it is - the longer
         * tours taken while it is hot, the rejections as it cools - never appeared. Now every draw
         * is a state: the current tour x after the decision, the neighbour that was drawn
         * (MoveInfo, highlighted on the canvas), and the rules that decided it. The run ends on
         * the best tour found, which is not necessarily the tour the search ended on. */
        public override LinkedList<GraphState> FindPath(GraphProblem graph) {
            graph.Reset();
            var history = new LinkedList<GraphState>();
            X = GraphProblem.OrderedGraphProblem(graph);
            XBest = X;

            var t0 = CalculateTemperature ? CalculateInitialTemperature(X) : StartTemp;
            var phases = PhaseCount(t0);
            var everyDraw = (long)phases * PhaseLength <= MaxRecordedDraws;
            var phaseStride = everyDraw ? 1 : (int)Math.Ceiling(phases / (double)MaxRecordedDraws);

            /* One pair of charts for the whole run, shared by every state; each state only says
             * which phase it is in (ChartCursor). Point k is the temperature of phase k and the
             * length of x when that phase starts. */
            var temperatureChart = new ChartInfo {
                Title = "Temperature Progress",
                XAxis = new ChartSet { Title = "Phase" },
                YAxis = new List<ChartSet> { new ChartSet { Title = "Temperature" } }
            };
            var distanceChart = new ChartInfo {
                Title = "Distance Progress",
                XAxis = new ChartSet { Title = "Phase" },
                YAxis = new List<ChartSet> { new ChartSet { Title = "Distance of x" } }
            };
            var charts = new List<ChartInfo> { temperatureChart, distanceChart };

            void Plot(int phase, double temperature, double distance) {
                temperatureChart.XAxis.Add(phase, null);
                temperatureChart.YAxis[0].Add(temperature, null);
                distanceChart.XAxis.Add(phase, null);
                distanceChart.YAxis[0].Add(distance, null);
            }

            var nodes = X.Nodes.ToList();
            var temperature = t0;
            var phase = 0;
            var draw = 0;
            Plot(0, t0, X.Costs);

            var start = NewState(nodes, charts, phase, draw, temperature);
            start.Messages["Decision"] = "start: the tour as listed";
            history.AddLast(start);

            while (temperature >= MinTemp) {
                var record = phase % phaseStride == 0;
                var counts = new int[3];
                List<Func<Derivation>> lastExtras = null;

                for (var i = 0; i < PhaseLength; i++) {
                    draw++;
                    var y = NeighbourState.Create(X, NeighbourEnum, DescentType.Random);
                    var fx = X.Costs;
                    var fy = y.Costs;
                    double? r = null;
                    DrawOutcome outcome;

                    if (fy <= fx) {
                        outcome = DrawOutcome.Shorter;
                    } else {
                        r = Random.NextDouble();
                        outcome = r < Math.Exp(-(fy - fx) / temperature) ? DrawOutcome.LongerAccepted : DrawOutcome.LongerRejected;
                    }

                    if (outcome != DrawOutcome.LongerRejected)
                        X = y;

                    var newBest = X.Costs < XBest.Costs;
                    if (newBest)
                        XBest = X;

                    counts[(int)outcome]++;

                    if (everyDraw) {
                        lastExtras = new List<Func<Derivation>>();
                        history.AddLast(DrawState(nodes, charts, phase, draw, i, temperature, y.MoveInfo, fx, fy, r,
                            outcome, newBest, lastExtras));
                    }
                }

                var cooled = temperature * Alpha;
                var phaseNow = phase;
                var temperatureNow = temperature;
                Derivation Cooling() => Derivations.Cooling(phaseNow, temperatureNow, Alpha, t0, MinTemp, PhaseLength);

                if (everyDraw) {
                    /* The cooling happens after the phase's last draw, so it is explained there. */
                    lastExtras.Add(Cooling);
                } else if (record) {
                    var summary = NewState(nodes, charts, phase, draw, temperature);
                    summary.MoveInfo = null;
                    summary.Messages["Decision"] =
                        $"{counts[0]} shorter, {counts[1]} longer taken, {counts[2]} rejected in this phase";
                    summary.Messages["Recorded"] = phaseStride == 1 ? "every phase" : $"every {phaseStride}th phase";
                    summary.Derivations.Add(Cooling());
                    history.AddLast(summary);
                }

                phase++;
                temperature = cooled;
                Plot(phase, temperature, X.Costs);
            }

            /* The answer of the run is the best tour seen, not where the walk stopped. */
            var final = NewState(nodes, charts, phase, draw, temperature, XBest);
            final.ChartCursor = phase;
            final.Messages["Decision"] = $"finished: T < T_min after {draw} draws; shown is the best tour found";
            /* The cooling that ended the run: the last phase's temperature times α fell below T_min. */
            if (phase > 0)
                final.Derivations.Add(Derivations.Cooling(phase - 1, temperature / Alpha, Alpha, t0, MinTemp, PhaseLength));
            history.AddLast(final);

            return history;
        }

        /* Phases until the temperature falls below MinTemp, the loop's own condition. */
        private int PhaseCount(double t0) {
            var phases = 0;
            for (var t = t0; t >= MinTemp && phases < int.MaxValue; t *= Alpha)
                phases++;
            return phases;
        }

        private static double CalculateInitialTemperature(GraphProblem graph) {
            var nearestNeighbor = new NearestNeighbour { Start = graph.Nodes[Random.Next(0, graph.Nodes.Count)] };
            var best = nearestNeighbor.MultiStart(graph).Last().Distance;
            var worst = nearestNeighbor.MultiStart(graph, false).Last().Distance;

            return worst - best;
        }

        /* States share the node list and the tour's lists with the search instead of copying them:
         * a rejected draw leaves x as it was, so a long cold stretch of the run costs no tour copies
         * at all, and an accepted one already has the fresh lists NeighbourState built for it. */
        private GraphState NewState(List<Node> nodes, List<ChartInfo> charts, int phase, int draw, double temperature,
            GraphProblem tour = null) {
            tour ??= X;
            var state = new GraphState {
                Nodes = nodes,
                Path = tour.Nodes,
                PathEdges = tour.Edges,
                Distance = tour.Costs,
                Temperature = temperature,
                Iteration = phase,
                ChartInfo = charts,
                ChartCursor = phase
            };

            /* Decision is set by the caller; it is created here so it reads right after the draw. */
            state.Messages["Phase"] = phase.ToString();
            state.Messages["Draw"] = draw.ToString();
            state.Messages["Decision"] = "";
            UpdateStateMessages(state);
            return state;
        }

        private GraphState DrawState(List<Node> nodes, List<ChartInfo> charts, int phase, int draw, int inPhase,
            double temperature, MoveInfo move, double fx, double fy, double? r, DrawOutcome outcome, bool newBest,
            List<Func<Derivation>> extras) {
            var state = NewState(nodes, charts, phase, draw, temperature);
            /* The neighbour's own move: y is not kept, so nothing else refers to it. */
            state.MoveInfo = move;
            state.Outcome = outcome;
            state.Messages["Draw"] = $"{draw} ({inPhase + 1} of {PhaseLength} in the phase)";
            state.Messages["Decision"] = outcome switch {
                DrawOutcome.Shorter => newBest ? "shorter: taken, new best tour" : "shorter: taken",
                DrawOutcome.LongerAccepted => "longer: taken by the Metropolis rule",
                _ => "longer: rejected, x stays"
            };

            state.DeriveLater(() => {
                var cards = new List<Derivation>();
                var context = $"draw {draw}, {Derivations.MoveContext(move)}";
                var caption = outcome == DrawOutcome.Shorter
                    ? "A shorter neighbour is always taken, without a random draw." + (newBest ? " This one is also the best tour so far." : "")
                    : "A longer neighbour is not taken outright: the Metropolis rule below decides.";

                var moveCard = Derivations.Move(move, fx, context, caption);
                if (moveCard != null)
                    cards.Add(moveCard);
                /* f(y) as the move card arrived at it, so both cards show the same Δ: rounding the
                 * edges and rounding the tours can differ in the last digit. */
                var fyShown = moveCard != null ? Derivations.MoveResult(move, fx) : fy;
                if (r is { } random)
                    cards.Add(Derivations.Metropolis(phase, fx, fyShown, temperature, random, $"draw {draw}, phase {phase}"));
                foreach (var extra in extras)
                    cards.Add(extra());

                return cards;
            });

            return state;
        }

        public override void UpdateStateMessages(GraphState state) {
            state.Messages["Route"] = string.Join("-", state.Path.Select(n => n.Index));
            state.Messages["Distance"] = Math.Round(state.Distance, 3).ToString(CultureInfo.InvariantCulture);
            state.Messages["Best"] = Math.Round(XBest.Costs, 3).ToString(CultureInfo.InvariantCulture);
            /* Four significant digits: the schedule spans ten orders of magnitude, so a fixed number of
             * decimals either prints sixteen digits early on or rounds to 0 late in the run. */
            state.Messages["Temperature"] = state.Temperature.ToString("G4", CultureInfo.InvariantCulture);
        }
    }
}
