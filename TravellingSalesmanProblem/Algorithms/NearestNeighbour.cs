﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class NearestNeighbour : Algorithm {
        public Node Start { get; set; }
        public bool Closed { get; set; } = true;
        public override IEnumerable<GraphState> FindPath(GraphProblem graph) {
            var state = new GraphState {
                Nodes = graph.Nodes
            };
            graph.Reset();

            var current = Start;
            Start.Visited = true;
            state.Path.Add(current);

            UpdateStateMessages(state);
            yield return state;

            while (true) {
                var edge = Edge.FindShortest(current);

                if (edge == null) {
                    var isClosed = true;

                    if (Closed) {
                        edge = current.Edges.Find(e => e.IsBetween(current, Start));
                        isClosed = edge != null;

                        if (edge != null) {
                            state.Path.Add(edge.Opposite(current));
                            state.PathEdges.Add(edge);
                            state.Distance += edge.Distance;
                        }
                    }

                    state.Finished = true;
                    state.Success = graph.Nodes.All(n => n.Visited) && isClosed;
                    UpdateStateMessages(state);
                    yield break;
                }

                state.Distance += edge.Distance;
                current = edge.Opposite(current);
                current.Visited = true;

                state.Path.Add(current);
                state.PathEdges.Add(edge);
                UpdateStateMessages(state);
                yield return state;
            }
        }

        public override IEnumerable<GraphState> MultiStart(GraphProblem graph) {
            //x* unspezifiziert
            //f(x*) := unendlich
            //while Stoppkriterium nicht erfüllt do
            //  Konstruiere eine Lösung x;
            //  if f(x) < f(x*) then
            //      Setze x* := x und f(x*) := f(x);
            throw new NotImplementedException();
        }

        public override void UpdateStateMessages(GraphState state) {
            state.Messages["Route"] = string.Join('-', state.Path.Select(n => n.Index));
            state.Messages["Path Edges"] = string.Join(',', state.PathEdges.Select(n => n.Node1.Index + " <-> " + n.Node2.Index));
            state.Messages["Distance"] = Math.Round(state.Distance, 1).ToString();
        }
    }
}