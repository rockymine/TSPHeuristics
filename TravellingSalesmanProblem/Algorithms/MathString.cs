using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class MathString {
        public Dictionary<string, object> Variables { get; set; } = new();
        public string Latex { get; set; }
        public string Description { get; set; }
        public string Dummy { get; set; }
        public string Result { get; set; }
        public MathString(string latex, string dummy, string result) {
            Latex = latex;
            Dummy = dummy;
            Result = result;
        }

        public void SetVar(string variable, object data) {
            Variables[variable] = data;
        }

        public string Generate() {
            var edited = Dummy;
            foreach (var kvp in Variables) {
                edited = edited.Replace("?" + kvp.Key + "?", kvp.Value.ToString());
            }
            return edited;
        }

        public static MathString UpdateTemperature(GraphState state, double alpha) {
            var latex = "$T_{k+1} = f(T(k)) = \\alpha \\cdot T_{k}$";
            var dummy = "$T_{?k+1?} = ?alpha? \\cdot ?temp?$";
            var result = $"$T_{{{state.Iteration + 1}}} = {state.Temperature * alpha}$";

            var mathString = new MathString(latex, dummy, result);
            mathString.SetVar("k+1", state.Iteration + 1);
            mathString.SetVar("temp", state.Temperature);
            mathString.SetVar("alpha", alpha);

            return mathString;
        }

        public static MathString MetropolisRule(GraphProblem graph, GraphState state, double random, bool condition) {
            var latex = "$\\text{exp}(\\frac{f(x) - f(y)}{T_{k}}) > \\text{rand}(0,1)$";
            var dummy = "$\\text{exp}(\\frac{?f(x)? - ?f(y)?}{?temp?}) > ?rand?$";
            var result = $"$\\text{{{condition}}}$";
            
            var mathString = new MathString(latex, dummy, result);
            mathString.SetVar("f(x)", graph.Costs);
            mathString.SetVar("f(y)", state.Distance);
            mathString.SetVar("temp", state.Temperature);
            mathString.SetVar("rand", random);   

            return mathString;
        }

        public static MathString RandomProportionalRule(int antIndex, Edge edge, double beta, double pcp, double sum) {
            var latex = "$p_{k}(r,s) = \\begin{cases}\\frac{[\\tau(r,s)] \\cdot [\\eta(r,s)]^\\beta}{\\sum\\limits_{u \\in J_{k}(r)}[\\tau(r,u)] \\cdot [\\eta(r,u)]^\\beta}, & \\text{if } s \\in J_{k}(r)\\\\0, & \\text{otherwise}\\end{cases}$";
            var dummy = "$p_{?k?}(?r?,?s?) = \\begin{cases}\\frac{[?tau?] \\cdot [?eta?]^?beta?}{\\sum\\limits_{u \\in J_{?k?}(?r?)}[\\tau(?r?,u)] \\cdot [\\eta(?r?,u)]^?beta?}, & \\text{if } s \\in J_{?k?}(?r?)\\\\0, & \\text{otherwise}\\end{cases}$";
            var result = $"$p_{antIndex}({edge.Node1.Index},{edge.Node2.Index}) = \\frac{{{pcp}}}{{{sum}}} = {pcp / sum}$";

            var mathString = new MathString(latex, dummy, result);
            mathString.SetVar("k", antIndex);
            mathString.SetVar("r", edge.Node1.Index);
            mathString.SetVar("s", edge.Node2.Index);
            mathString.SetVar("tau", edge.Pheromone);
            mathString.SetVar("eta", edge.Visibility);
            mathString.SetVar("beta", beta);            

            return mathString;
        }

        public static MathString PheromoneClosenessProduct(Edge edge, double beta, double pcp) {
            var latex = "$\\omega(r,s) = [\\tau(r,s)] \\cdot [\\eta(r,s)]^\\beta$";
            var dummy = "$\\omega(?r?,?s?) = ?tau? \\cdot ?eta?^?beta?$";
            var result = $"$\\omega({edge.Node1.Index},{edge.Node2.Index}) = {pcp}$";

            var mathString = new MathString(latex, dummy, result);
            mathString.SetVar("r", edge.Node1.Index);
            mathString.SetVar("s", edge.Node2.Index);
            mathString.SetVar("tau", edge.Pheromone);
            mathString.SetVar("eta", edge.Visibility);
            mathString.SetVar("beta", beta);

            return mathString;
        }

        public static MathString GlobalUpdatingRule(Edge edge, double alpha, double delta) {
            var latex = "$\\tau(r, s) \\leftarrow(1 -\\alpha) \\cdot \\tau(r, s) + \\alpha \\cdot \\triangle \\tau(r, s)$";
            var dummy = "$\\tau(?r?,?s?) = (1-?alpha?) \\cdot ?tau? + ?alpha? \\cdot ?triangle?$";
            var description = "$\\text{ Once all ants have constructed their tour, the amount of pheromone is modified by applying the global updating rule. Here, only the globally best ant is allowed to deposit pheromone. } \\triangle \\tau(r, s) \\text{ is set to the inversed length of the globally best tour } (L_{ gb })^{ -1 } \\text{ and } 0 < \\alpha < 1 \\text{ is the pheromone decay parameter.}$";
            var result = $"$\\tau({edge.Node1.Index},{edge.Node2.Index}) = {edge.Pheromone + (alpha * delta)}$";

            var mathString = new MathString(latex, dummy, result) {
                Description = description
            };

            mathString.SetVar("r", edge.Node1.Index);
            mathString.SetVar("s", edge.Node2.Index);
            mathString.SetVar("alpha", alpha);
            mathString.SetVar("tau", edge.Pheromone);
            mathString.SetVar("triangle", delta);

            return mathString;
        }

        public static MathString LocalUpdatingRule(Edge edge, double rho, double initialPheromone) {
            var latex = "$\\tau(r,s) \\leftarrow (1-\\rho) \\cdot \\tau(r,s) + \\rho \\cdot \\triangle \\tau(r,s)$";
            var dummy = "$\\tau(?r?,?s?) = (1-?rho?) \\cdot ?tau? + ?rho? \\cdot ?triangle?$";
            var result = $"$\\tau({edge.Node1.Index},{edge.Node2.Index}) = {((1 - rho) * edge.Pheromone) + (rho * initialPheromone)}$";
            var description = "$\\text{While building a solution (i.e., a tour) of the TSP, ants visit edges and change their pheromone level by applying the local updating rule. Ants build their tours by both heuristic information (they prefer to choose short edges) and pheromone information. The term } \\triangle \\tau(r,s) \\text{ is set to the initial pheromone value } \\tau_0.$";

            var mathString = new MathString(latex, dummy, result) {
                Description = description
            };

            mathString.SetVar("r", edge.Node1.Index);
            mathString.SetVar("s", edge.Node2.Index);
            mathString.SetVar("rho", rho);
            mathString.SetVar("tau", edge.Pheromone);
            mathString.SetVar("triangle", initialPheromone);

            return mathString;
        }
    }
}
