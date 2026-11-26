using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class MathString {
        public Dictionary<string, double> Variables { get; set; } = new();
        public string Latex { get; set; }
        public string Description { get; set; }
        public string Dummy { get; set; }
        public string Result { get; set; }
        public MathString(string latex, string dummy, string result) {
            Latex = latex;
            Dummy = dummy;
            Result = result;
        }

        public MathString DeepCopy() {
            return new MathString(Latex, Dummy, Result) {
                Variables = new Dictionary<string, double>(Variables),
                Description = Description
            };
        }

        public void SetVar(string variable, double data) => Variables[variable] = data;

        public string Generate() {
            var edited = Dummy;
            foreach (var kvp in Variables) {
                edited = edited.Replace("?" + kvp.Key + "?", Number(kvp.Value));
            }
            return edited;
        }

        /* A number as it goes into a formula. Values below 0.001 are written as m * 10^e: rounding
         * them to three places, as every caller used to, turned a late annealing temperature into a
         * literal 0, so the Metropolis rule was rendered as a division by zero and the temperature
         * update as T = 0.98 * 0. Zero is written without a sign - a delta that cancels exactly
         * comes out of the arithmetic as -0, and .NET prints that as "-0". */
        public static string Number(double value) {
            if (value == 0)
                return "0";

            if (Math.Abs(value) >= 0.001)
                return value.ToString();

            var exponent = (int)Math.Floor(Math.Log10(Math.Abs(value)));
            var mantissa = Math.Round(value / Math.Pow(10, exponent), 2);
            return $"{mantissa} \\cdot 10^{{{exponent}}}";
        }

        /* Three decimals for ordinary values; tiny ones are kept whole so Number can show them. */
        private static double Rounded(double value) =>
            Math.Abs(value) >= 0.001 ? Math.Round(value, 3) : value;

        public static MathString UpdateTemperature(GraphState state, double alpha) {
            var latex = "$T_{k+1} = f(T(k)) = \\alpha \\cdot T_{k}$";
            var dummy = "$T_{?k+1?} = ?alpha? \\cdot ?temp?$";
            var result = $"$T_{{{state.Iteration + 1}}} = {Number(Rounded(state.Temperature * alpha))}$";

            var mathString = new MathString(latex, dummy, result);
            mathString.SetVar("k+1", state.Iteration + 1);
            mathString.SetVar("temp", Rounded(state.Temperature));
            mathString.SetVar("alpha", alpha);

            return mathString;
        }

        public static MathString MetropolisRule(GraphProblem x, GraphProblem y, double temperature, double random, bool condition) {
            var latex = "$\\text{exp}(\\frac{f(x) - f(y)}{T_{k}}) > \\text{rand}(0,1)$";
            var dummy = "$\\text{exp}(\\frac{?f(x)? - ?f(y)?}{?temp?}) > ?rand?$";
            var result = $"$\\text{{{condition}}}$";

            var mathString = new MathString(latex, dummy, result);
            mathString.SetVar("f(x)", Math.Round(x.Costs, 3));
            mathString.SetVar("f(y)", Math.Round(y.Costs, 3));
            mathString.SetVar("temp", Rounded(temperature));
            mathString.SetVar("rand", Math.Round(random, 3));

            return mathString;
        }

        public static MathString RandomProportionalRule(int antIndex, Edge edge, double beta, double pcp, double sum) {
            var latex = "$p_{k}(r,s) = \\begin{cases}\\frac{[\\tau(r,s)] \\cdot [\\eta(r,s)]^\\beta}{\\sum\\limits_{u \\in J_{k}(r)}[\\tau(r,u)] \\cdot [\\eta(r,u)]^\\beta}, & \\text{if } s \\in J_{k}(r)\\\\0, & \\text{otherwise}\\end{cases}$";
            var dummy = "$p_{?k?}(?r?,?s?) = \\begin{cases}\\frac{[?tau?] \\cdot [?eta?]^?beta?}{\\sum\\limits_{u \\in J_{?k?}(?r?)}[\\tau(?r?,u)] \\cdot [\\eta(?r?,u)]^?beta?}, & \\text{if } s \\in J_{?k?}(?r?)\\\\0, & \\text{otherwise}\\end{cases}$";
            var result = $"$p_{antIndex}({edge.Node1.Index},{edge.Node2.Index}) = \\frac{{{Math.Round(pcp, 3)}}}{{{Math.Round(sum , 3)}}} = {Math.Round(pcp / sum, 3)}$";

            var mathString = new MathString(latex, dummy, result);
            mathString.SetVar("k", antIndex);
            mathString.SetVar("r", edge.Node1.Index);
            mathString.SetVar("s", edge.Node2.Index);
            mathString.SetVar("tau", Math.Round(edge.Pheromone, 3));
            mathString.SetVar("eta", Math.Round(1 / edge.Distance, 3));
            mathString.SetVar("beta", beta);            

            return mathString;
        }

        public static MathString PheromoneClosenessProduct(Edge edge, double beta, double pcp) {
            var latex = "$\\omega(r,s) = [\\tau(r,s)] \\cdot [\\eta(r,s)]^\\beta$";
            var dummy = "$\\omega(?r?,?s?) = ?tau? \\cdot ?eta?^?beta?$";
            var result = $"$\\omega({edge.Node1.Index},{edge.Node2.Index}) = {Math.Round(pcp, 3)}$";

            var mathString = new MathString(latex, dummy, result);
            mathString.SetVar("r", edge.Node1.Index);
            mathString.SetVar("s", edge.Node2.Index);
            mathString.SetVar("tau", Math.Round(edge.Pheromone, 3));
            mathString.SetVar("eta", Math.Round(1 / edge.Distance, 3));
            mathString.SetVar("beta", beta);

            return mathString;
        }

        public static MathString GlobalUpdatingRule(Edge edge, double alpha, double delta) {
            var latex = "$\\tau(r, s) \\leftarrow(1 -\\alpha) \\cdot \\tau(r, s) + \\alpha \\cdot \\triangle \\tau(r, s)$";
            var dummy = "$\\tau(?r?,?s?) = (1-?alpha?) \\cdot ?tau? + ?alpha? \\cdot ?triangle?$";
            var description = "$\\text{ Once all ants have constructed their tour, the amount of pheromone is modified by applying the global updating rule. Here, only the globally best ant is allowed to deposit pheromone. } \\triangle \\tau(r, s) \\text{ is set to the inversed length of the globally best tour } (L_{ gb })^{ -1 } \\text{ and } 0 < \\alpha < 1 \\text{ is the pheromone decay parameter.}$";
            var result = $"$\\tau({edge.Node1.Index},{edge.Node2.Index}) = {Math.Round(((1 - alpha) * edge.Pheromone) + (alpha * delta), 3)}$";

            var mathString = new MathString(latex, dummy, result) {
                Description = description
            };

            mathString.SetVar("r", edge.Node1.Index);
            mathString.SetVar("s", edge.Node2.Index);
            mathString.SetVar("alpha", alpha);
            mathString.SetVar("tau", Math.Round(edge.Pheromone, 3));
            mathString.SetVar("triangle", Math.Round(delta, 5));

            return mathString;
        }

        public static MathString GlobalUpdatingRuleAS(Edge edge, double alpha, double delta) {
            /* "\tau" in a non verbatim string is a TAB followed by "au", so the rendered formula
             * lost its leading symbol. The backslash has to be escaped like every other one here. */
            var latex = "$\\tau(r,s) \\leftarrow (1 - \\alpha) \\cdot \\tau(r,s) + \\sum_{k=1}^{m}\\triangle\\tau_{k}(r,s)$";
            var dummy = "$\\tau(?r?,?s?) = (1 - ?alpha?) \\cdot ?tau? + ?triangle?$";
            var result = $"$\\tau({edge.Node1.Index},{edge.Node2.Index}) = {Math.Round(((1 - alpha) * edge.Pheromone) + delta, 3)}$";

            var mathString = new MathString(latex, dummy, result);
            mathString.SetVar("r", edge.Node1.Index);
            mathString.SetVar("s", edge.Node2.Index);
            mathString.SetVar("alpha", alpha);
            mathString.SetVar("tau", Math.Round(edge.Pheromone, 3));
            mathString.SetVar("triangle", delta);

            return mathString;
        }

        public static MathString LocalUpdatingRule(Edge edge, double rho, double initialPheromone) {
            var latex = "$\\tau(r,s) \\leftarrow (1-\\rho) \\cdot \\tau(r,s) + \\rho \\cdot \\triangle \\tau(r,s)$";
            var dummy = "$\\tau(?r?,?s?) = (1-?rho?) \\cdot ?tau? + ?rho? \\cdot ?triangle?$";
            var result = $"$\\tau({edge.Node1.Index},{edge.Node2.Index}) = {Math.Round(((1 - rho) * edge.Pheromone) + (rho * initialPheromone), 3)}$";
            var description = "$\\text{While building a solution (i.e., a tour) of the TSP, ants visit edges and change their pheromone level by applying the local updating rule. Ants build their tours by both heuristic information (they prefer to choose short edges) and pheromone information. The term } \\triangle \\tau(r,s) \\text{ is set to the initial pheromone value } \\tau_0.$";

            var mathString = new MathString(latex, dummy, result) {
                Description = description
            };

            mathString.SetVar("r", edge.Node1.Index);
            mathString.SetVar("s", edge.Node2.Index);
            mathString.SetVar("rho", rho);
            mathString.SetVar("tau", Math.Round(edge.Pheromone, 3));
            mathString.SetVar("triangle", Math.Round(initialPheromone, 3));

            return mathString;
        }
    }
}