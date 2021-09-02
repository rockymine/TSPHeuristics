using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TravellingSalesmanProblem.Algorithms {
    public class MathString {
        public Dictionary<string, object> Variables { get; set; } = new();
        public string Latex { get; set; }
        public string Dummy { get; set; }
        public string Result { get; set; }
        public MathString(string latex) {
            Latex = latex;
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
    }
}
