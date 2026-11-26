using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public enum StepKind { Rule, Values, Result }

    public enum VerdictKind { Neutral, Good, Warn, Bad }

    /* One line of a derivation, aligned on its relation: "Δ | = | d(3,6) + …". Lhs, Rel and Rhs
     * are TeX; the note is plain text shown under the line. */
    public record DerivationStep(string Lhs, string Rel, string Rhs, StepKind Kind, string Note = null);

    /* A rule worked through for one state: the symbolic line, the same line with the numbers, and
     * the result, with a one-line caption saying what the rule does and a verdict saying what came
     * out. It replaces the latex/dummy/result triplets of MathString, which the page rendered as
     * three loose paragraphs whose numbers did not always follow from each other.
     *
     * Every result is computed from the operands as they are shown, so each line is true as
     * printed - a student checking a hand calculation can follow it digit by digit. */
    public class Derivation {
        public string Title { get; init; }
        public string Context { get; init; }
        public string Verdict { get; init; }
        public VerdictKind VerdictKind { get; init; }
        public string Caption { get; init; }
        public List<DerivationStep> Steps { get; init; } = new();
        /* Optional TeX line under the steps, e.g. the schedule's parameters. */
        public string Footer { get; init; }
    }

    /* Numbers as they appear in formulas: culture invariant, lengths to two decimals, everything
     * else (temperatures, pheromone, probabilities) to three significant figures, and values below
     * 0.001 as m · 10^e so a cold temperature does not round to 0. */
    public static class Tex {
        public static double Len(double value) => Math.Round(value, 2) + 0d;

        public static string LenText(double value) => Len(value).ToString("0.00", CultureInfo.InvariantCulture);

        public static string Signed(double value) {
            var v = Len(value);
            return (v > 0 ? "+" : v < 0 ? "-" : "") + Math.Abs(v).ToString("0.00", CultureInfo.InvariantCulture);
        }

        public static double Sig(double value) {
            if (value == 0 || double.IsNaN(value) || double.IsInfinity(value))
                return value;

            var digits = 2 - (int)Math.Floor(Math.Log10(Math.Abs(value)));
            return digits > 15 ? value : Math.Round(value, Math.Max(0, digits)) + 0d;
        }

        public static string SigText(double value) {
            if (value == 0)
                return "0";

            var abs = Math.Abs(value);
            if (abs < 0.001 || abs >= 1e6) {
                var exponent = (int)Math.Floor(Math.Log10(abs));
                var mantissa = Math.Round(value / Math.Pow(10, exponent), 2);
                return $"{mantissa.ToString("0.##", CultureInfo.InvariantCulture)} \\cdot 10^{{{exponent}}}";
            }

            return Sig(value).ToString("0.###############", CultureInfo.InvariantCulture);
        }
    }

    public static class Derivations {
        private static string D(MoveEdge e) => $"d({e.A.Index},{e.B.Index})";

        /* Δ as the move card shows it: the sum of the rounded edge lengths, which is not always the
         * difference of the rounded tour lengths. A card shown next to it uses this one. */
        public static double MoveDelta(MoveInfo move) =>
            Tex.Len(move.Added.Sum(e => Tex.Len(e.Length)) - move.Removed.Sum(e => Tex.Len(e.Length)));

        /* f(y) as the move card arrives at it: f(x) + Δ, both as shown. */
        public static double MoveResult(MoveInfo move, double fx) => Tex.Len(Tex.Len(fx) + MoveDelta(move));

        /* Δ for a move: the edges put in minus the edges taken out, then f(y) = f(x) + Δ. */
        public static Derivation Move(MoveInfo move, double fx, string context, string caption) {
            if (move?.Added == null || move.Removed == null || move.Added.Count == 0)
                return null;

            var symbolic = string.Join(" + ", move.Added.Select(D)) + string.Concat(move.Removed.Select(e => " - " + D(e)));
            var values = string.Join(" + ", move.Added.Select(e => Tex.LenText(e.Length))) +
                         string.Concat(move.Removed.Select(e => " - " + Tex.LenText(e.Length)));
            var delta = MoveDelta(move);
            var fy = MoveResult(move, fx);

            var verdict = delta < 0 ? $"Shorter by {Tex.LenText(-delta)}"
                        : delta > 0 ? $"Longer by {Tex.LenText(delta)}" : "No change";

            return new Derivation {
                Title = "Change in length",
                Context = context,
                Verdict = verdict,
                VerdictKind = delta < 0 ? VerdictKind.Good : delta > 0 ? VerdictKind.Bad : VerdictKind.Neutral,
                Caption = caption,
                Steps = new() {
                    new("\\Delta", "=", symbolic, StepKind.Rule, "edges put in minus edges taken out"),
                    new("", "=", values, StepKind.Values),
                    new("", "=", Tex.Signed(delta), StepKind.Result,
                        delta < 0 ? "negative: the tour gets shorter" : delta > 0 ? "positive: the tour gets longer" : null),
                    new("f(y)", "=", "f(x) + \\Delta", StepKind.Rule),
                    new("", "=", $"{Tex.LenText(fx)} {(delta < 0 ? "-" : "+")} {Tex.LenText(Math.Abs(delta))}", StepKind.Values),
                    new("", "=", Tex.LenText(fy), StepKind.Result)
                }
            };
        }

        /* The Metropolis test for a longer neighbour: P = e^(-Δ/T), accept if r < P. */
        public static Derivation Metropolis(int phase, double fx, double fy, double temperature, double r, string context) {
            var delta = Tex.Len(Tex.Len(fy) - Tex.Len(fx));
            var t = Tex.Sig(temperature);
            var exponent = Tex.Sig(-delta / t);
            var p = Math.Exp(exponent);
            var rs = Math.Round(r, 3);
            var accepted = rs < p;

            var pRel = p == 0 || p != Tex.Sig(p) ? "\\approx" : "=";

            return new Derivation {
                Title = "Metropolis rule",
                Context = context,
                Verdict = accepted ? "Accepted: r < P" : "Rejected: r ≥ P",
                VerdictKind = accepted ? VerdictKind.Warn : VerdictKind.Neutral,
                Caption = "A longer neighbour is taken only if a random number r falls below the probability P. The colder it gets, the smaller P becomes.",
                Steps = new() {
                    new("\\Delta", "=", "f(y) - f(x)", StepKind.Rule),
                    new("", "=", $"{Tex.LenText(fy)} - {Tex.LenText(fx)}", StepKind.Values),
                    new("", "=", Tex.Signed(delta), StepKind.Result, "positive: y is longer, so the rule has to draw"),
                    new("P", "=", $"e^{{-\\Delta / T_{{{phase}}}}}", StepKind.Rule),
                    new("", "=", $"e^{{-{Tex.LenText(delta)} / {Tex.SigText(t)}}} = e^{{{Tex.SigText(exponent)}}}", StepKind.Values),
                    new("", pRel, Tex.SigText(p), StepKind.Result,
                        p < 1e-6 ? "practically 0: at this temperature longer tours are no longer accepted" : null),
                    new("r", "=", rs.ToString("0.000", CultureInfo.InvariantCulture), StepKind.Values, "uniform random number in [0, 1)"),
                    new("r", accepted ? "<" : "\\geq", accepted ? "P \\Rightarrow \\text{accept } y" : "P \\Rightarrow \\text{reject } y \\text{, keep } x", StepKind.Result)
                }
            };
        }

        /* The cooling step at the end of a phase: T_(k+1) = α · T_k. */
        public static Derivation Cooling(int phase, double temperature, double alpha, double t0, double tMin, int phaseLength) {
            var t = Tex.Sig(temperature);
            var next = Tex.Sig(alpha * t);

            return new Derivation {
                Title = "Cooling",
                Context = $"end of phase {phase}",
                Verdict = $"T: {Tex.SigText(t).Replace("\\cdot ", "·")} → {Tex.SigText(next).Replace("\\cdot ", "·")}",
                VerdictKind = VerdictKind.Neutral,
                Caption = $"After each phase of {phaseLength} {(phaseLength == 1 ? "draw" : "draws")} the temperature is multiplied by the cooling factor \u03b1. The run stops once it falls below the minimum temperature.",
                Steps = new() {
                    new($"T_{{{phase + 1}}}", "=", $"\\alpha \\cdot T_{{{phase}}}", StepKind.Rule),
                    new("", "=", $"{alpha.ToString("0.###", CultureInfo.InvariantCulture)} \\cdot {Tex.SigText(t)}", StepKind.Values),
                    new("", "=", Tex.SigText(next), StepKind.Result)
                },
                Footer = $"T_0 = {Tex.SigText(t0)} \\quad T_{{min}} = {Tex.SigText(tMin)} \\quad \\alpha = {alpha.ToString("0.###", CultureInfo.InvariantCulture)} \\quad it = {phaseLength}"
            };
        }

        /* The global pheromone update after an iteration. ACS (Dorigo & Gambardella 1997, the rule
         * the thesis uses): only the best tour's edges receive α · 1/L_gb. With "every ant", each
         * ant's tour deposits 1/L_k. In both, α is the global pheromone decay. */
        public static Derivation GlobalUpdate(Edge edge, double tauBefore, double alpha, double deposit, double bestLength,
                                              bool acs, int iteration) {
            var r = edge.Node1.Index;
            var s = edge.Node2.Index;
            var tau = Tex.Sig(tauBefore);
            var a = Tex.Sig((1 - alpha) * tau);
            var steps = new List<DerivationStep>();
            double after;

            if (acs) {
                var dTau = Tex.Sig(deposit);
                var b = Tex.Sig(alpha * dTau);
                after = Tex.Sig(a + b);
                steps.Add(new($"\\tau({r},{s})", "\\leftarrow", "(1 - \\alpha) \\cdot \\tau + \\alpha \\cdot \\Delta\\tau", StepKind.Rule));
                steps.Add(new("\\Delta\\tau", "=", $"1 / L_{{gb}} = 1 / {Tex.LenText(bestLength)} = {Tex.SigText(dTau)}", StepKind.Values, "the best tour so far deposits"));
                steps.Add(new($"\\tau({r},{s})", "=", $"(1 - {F(alpha)}) \\cdot {Tex.SigText(tau)} + {F(alpha)} \\cdot {Tex.SigText(dTau)}", StepKind.Values));
                steps.Add(new("", "=", $"{Tex.SigText(a)} + {Tex.SigText(b)}", StepKind.Values));
            } else {
                var sum = Tex.Sig(deposit);
                after = Tex.Sig(a + sum);
                steps.Add(new($"\\tau({r},{s})", "\\leftarrow", "(1 - \\alpha) \\cdot \\tau + \\sum_k \\Delta\\tau_k", StepKind.Rule));
                steps.Add(new($"\\tau({r},{s})", "=", $"(1 - {F(alpha)}) \\cdot {Tex.SigText(tau)} + {Tex.SigText(sum)}", StepKind.Values, "every ant whose tour uses the edge deposits 1/L_k"));
                steps.Add(new("", "=", $"{Tex.SigText(a)} + {Tex.SigText(sum)}", StepKind.Values));
            }

            steps.Add(new("", "=", Tex.SigText(after), StepKind.Result));

            return new Derivation {
                Title = acs ? "Global pheromone update (ACS)" : "Global pheromone update (Ant System)",
                Context = $"edge ({r}, {s}), iteration {iteration}",
                Verdict = $"τ: {Tex.SigText(tau).Replace("\\cdot ", "·")} → {Tex.SigText(after).Replace("\\cdot ", "·")}",
                VerdictKind = VerdictKind.Neutral,
                Caption = acs
                    ? "After every iteration all pheromone evaporates by \u03b1, and the edges of the best tour so far receive \u03b1 times one over its length."
                    : "After every iteration all pheromone evaporates by \u03b1, and every ant deposits one over its tour length on the edges it used.",
                Steps = steps
            };
        }

        /* "swap 3 ⇄ 9", "2-opt on 3, 7", … - the move as a short context line. */
        public static string MoveContext(MoveInfo move) {
            if (move == null)
                return null;

            var nodes = move.Nodes.Select(n => n.Index.ToString()).ToList();
            return move.Type == NeighbourType.Swap && nodes.Count == 2
                ? $"swap {nodes[0]} \u21c4 {nodes[1]}"
                : $"{ManualSearch.Label(move.Type)} on {string.Join(", ", nodes)}";
        }

        private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
