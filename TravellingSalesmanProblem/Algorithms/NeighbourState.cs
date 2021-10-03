using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;

namespace TravellingSalesmanProblem.Algorithms {
    public class NeighbourState {
        private static readonly Random Random = new();
        public static GraphProblem TwoOpt(GraphProblem graph) {
            var tour = graph.Nodes.ToArray();
            var n = tour.Length;
            var i = Random.Next(1, n - 2);
            var j = Random.Next(i, n - 1);
            i++;
            j++;

            var a = tour[0..i];
            var b = tour[i..j];//i = i + 1
            var c = tour[j..n];//j = j + 1
            var bDash = b.Reverse();

            var final = a.Concat(bDash).Concat(c).ToList();
            var swapped = new GraphProblem {
                Nodes = final
            };

            swapped.ConnectPathNodes();
            var edges = swapped.Edges.ToArray();

            i--;
            n--;

            var first = new GraphSegment {
                Identifier = "Seg. A: ",
                Type = SegmentType.Normal,
                Edges = edges[0..i].ToList()
            };

            var second = new GraphSegment {
                Identifier = "Seg. B': ",
                Type = SegmentType.Reversed,
                Edges = edges[i..j].ToList()
            };

            var third = new GraphSegment {
                Identifier = "Seg. C: ",
                Type = SegmentType.Normal,
                Edges = edges[j..n].ToList()
            };

            var segments = new List<GraphSegment> {
                first, second, third
            };

            swapped.Segments = segments;
            return swapped;
        }

        public static GraphProblem TwoOptFull(GraphProblem graph) {
            var best = graph.DeepCopy();
            var n = best.Nodes.Count;
            var improvement = true;

            while (improvement) {
                StartAgain:
                improvement = false;
                for (int i = 1; i <= n - 3; i++) {
                    for (int j = i + 1; j <= n - 2; j++) {
                        var tour = best.Nodes.ToArray();
                        var ij = Edge.GetDistanceRounded(tour[i], tour[j]);
                        var i1j1 = Edge.GetDistanceRounded(tour[i + 1], tour[j + 1]);
                        var ii1 = Edge.GetDistanceRounded(tour[i], tour[i + 1]);
                        var jj1 = Edge.GetDistanceRounded(tour[j], tour[j + 1]);
                        var delta = Math.Round(ij + i1j1 - ii1 - jj1, 0);
                        if (delta < 0) {
                            i++;
                            j++;

                            var a = tour[0..i];
                            var b = tour[i..j];
                            var c = tour[j..n];
                            var bRev = b.Reverse();

                            best = new GraphProblem { Nodes = a.Concat(bRev).Concat(c).ToList() };
                            best.ConnectPathNodes();
                            
                            goto StartAgain;
                        }
                    }
                }
            }

            return best;
        }

        public static GraphProblem ThreeOpt(GraphProblem graph) {
            var tour = graph.Nodes.ToArray();
            var n = tour.Length;
            
            //random indexes for split
            var i = Random.Next(1, n - 3);
            var j = Random.Next(i + 1, n - 2);
            var k = Random.Next(j + 1, n - 1);

            //tour segments
            var start = tour[0..i];
            var BC = tour[i..j];
            var CB = BC.Reverse();
            var DE = tour[j..k];
            var ED = DE.Reverse();
            var end = tour[k..n];

            //relevant nodes
            var A = tour[i];
            var B = tour[i + 1];
            var C = tour[j];
            var D = tour[j + 1];
            var E = tour[k];
            var F = tour[k + 1];

            //edge distances
            var dAB = Edge.GetDistanceRounded(A, B);
            var dAC = Edge.GetDistanceRounded(A, C);
            var dAD = Edge.GetDistanceRounded(A, D);
            var dCD = Edge.GetDistanceRounded(C, D);
            var dEF = Edge.GetDistanceRounded(E, F);           
            var dBD = Edge.GetDistanceRounded(B, D);
            var dCE = Edge.GetDistanceRounded(C, E);
            var dDF = Edge.GetDistanceRounded(D, F);            
            var dEB = Edge.GetDistanceRounded(E, B);
            var dCF = Edge.GetDistanceRounded(C, F);
            var dFB = Edge.GetDistanceRounded(F, B);
            var dAE = Edge.GetDistanceRounded(A, E);
            
            IEnumerable<Node> final = null;
            var distances = new List<double>();

            //var d0 = dAB + dCD + dEF; //A BC DE F
            //pertubations
            var d1 = dAC + dBD + dEF; //A CB DE F
            distances.Add(d1);
            var d2 = dAB + dCE + dDF; //A BC ED F
            distances.Add(d2);
            var d3 = dAD + dCE + dFB; //A DE CB F
            distances.Add(d3);
            var d4 = dAD + dEB + dCF; //A DE BC F
            distances.Add(d4);
            var d5 = dAE + dCD + dFB; //A ED CB F
            distances.Add(d5);
            var d6 = dAE + dBD + dCF; //A ED BC F
            distances.Add(d6);
            var d7 = dAC + dEB + dDF; //A CB ED F
            distances.Add(d7);

            var min = distances.Min();

            if (min == d1)
                final = start.Concat(CB).Concat(DE);
            
            if (min == d2)
                final = start.Concat(BC).Concat(ED);        
            
            if (min == d3)
                final = start.Concat(DE).Concat(CB);
            
            if (min == d4)
                final = start.Concat(DE).Concat(BC);
            
            if (min == d5)
                final = start.Concat(ED).Concat(CB);
            
            if (min == d6)
                final = start.Concat(ED).Concat(BC);
            
            if (min == d7)
                final = start.Concat(CB).Concat(ED);

            final = final.Concat(end);

            var swapped = new GraphProblem {
                Nodes = final.ToList()
            };
            swapped.ConnectPathNodes();
            return swapped;
        }

        public static GraphProblem Reversed(GraphProblem tour, int i, int j) {
            return null;
        }

        public static GraphProblem DoubleBridgeFourOpt(GraphProblem graph) {
            var tour = graph.Nodes.ToArray();
            var n = tour.Length;

            var i = Random.Next(1, n - 4);
            var j = Random.Next(i + 1, n - 3);
            var k = Random.Next(j + 1, n - 2);
            var l = Random.Next(k + 1, n - 1);

            //tour is split into 5 segments: a-b-c-d-a
            var a1 = tour[0..i];
            var b = tour[i..j];
            var c = tour[j..k];
            var d = tour[k..l];
            var a2 = tour[l..n];

            //tour is recombined as: a-d-c-b-a
            var final = a1.Concat(d).Concat(c).Concat(b).Concat(a2);
            var swapped = new GraphProblem {
                Nodes = final.ToList()
            };
            swapped.ConnectPathNodes();
            return swapped;
        }

        public static GraphProblem Swap(GraphProblem graph) {
            var i = Random.Next(1, graph.Nodes.Count - 2);
            var j = Random.Next(i, graph.Nodes.Count - 1);
            
            //if (i == j)
            //    Swap(graph);

            var temp = graph.Nodes;
            var node = temp[i];
            temp[i] = temp[j];
            temp[j] = node;

            var final = new GraphProblem {
                Nodes = temp
            };

            final.ConnectPathNodes();
            return final;
        }
    }

    public enum NeighbourType {
        Swap = 0,
        TwoOpt = 1,
        ThreeOpt = 2,
        FourOpt = 3
    }
}