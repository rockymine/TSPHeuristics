using Excubo.Blazor.Canvas.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Algorithms;
using TravellingSalesmanProblem.Graph;
using WebApplication.Extensions;

namespace WebApplication.Utils {
    public class CanvasRenderer {
        private static readonly Brush EdgeBrush = new() {
            Color = "black", Width = 2,
            TextFont = "15px serif", TextStyle = "black"
        };
        private static readonly Brush NodeBrush = new() {
            Color = "#4e5072", Width = 2, Style = FillStyle.Fill,
            TextFont = "15px serif bold", TextStyle = "white"
        };
        private static readonly Brush GridBrush = new() {
            Color = "#999999", Width = 0.75
        };

        public static async Task DrawGrid(Context2D context, CanvasSettings settings) {
            for (int i = (int)settings.MinPos.Y; i <= settings.MaxPos.Y; i++) {
                await context.DrawLine(GridBrush,
                    Manipulate(new Vector2((int)settings.MinPos.X, i), settings),
                    Manipulate(new Vector2(settings.MaxPos.X, i), settings));
            }
            for (int i = (int)settings.MinPos.X; i <= settings.MaxPos.X; i++) {
                await context.DrawLine(GridBrush,
                    Manipulate(new Vector2(i, (int)settings.MinPos.Y), settings),
                    Manipulate(new Vector2(i, settings.MaxPos.Y), settings));
            }
        }

        public static async Task DrawNodes(Context2D context, GraphState state, LinkedListNode<GraphState> currentStateNode, CanvasSettings settings) {
            var nodes = state.Nodes;
            var brush = NodeBrush.Copy();

            foreach (var node in nodes) {
                UpdateNodeBrushColor(currentStateNode.Value.SwapInfo, node, brush);

                await context.DrawCircle(brush, settings.NodeRadius,
                    Manipulate(node.Position, settings));

                await context.WriteText(brush.TextFont, brush.TextStyle, node.Index.ToString(),
                    Manipulate(node.Position, settings));
            }
        }

        private static void UpdateNodeBrushColor(SwapInfo swapInfo, Node node, Brush brush) {
            if (swapInfo != null) {
                if (node.Index == swapInfo.Nodes[0].Index) {
                    brush.Color = "purple";
                } else if (node.Index == swapInfo.Nodes[1].Index) {
                    brush.Color = "orange";
                } else {
                    brush.Color = NodeBrush.Color;
                }
            }
        }

        public static async Task DrawEdges(Context2D context, GraphState state, LinkedListNode<GraphState> stateNode, CanvasSettings settings) {
            var edges = state.PathEdges;
            var brush = EdgeBrush.Copy();

            foreach (var edge in edges) {
                if (state == stateNode.Value) {
                    if (stateNode.Previous != null)
                        UpdateEdgeBrushColor(stateNode.Previous.Value.PathEdges, edge, brush, "green");
                } else {
                    UpdateEdgeBrushColor(stateNode.Value.PathEdges, edge, brush, "red");
                }

                if (edge.Pheromone != 0) {
                    brush.Width = edge.Pheromone * 500;
                    settings.Annotate = false;
                }

                await context.DrawLine(brush,
                    Manipulate(edge.Node1.Position, settings),
                    Manipulate(edge.Node2.Position, settings));
            }

            if (settings.Annotate)
                await DrawEdgeTextBox(context, edges, brush, settings);
        }

        private static void UpdateEdgeBrushColor(List<Edge> compare, Edge edge, Brush brush, string color) {
            if (compare.Find(e => e.IsEqual(edge)) != null) {
                brush.Color = EdgeBrush.Color;
            } else {
                brush.Color = color;
            }
        }

        public static async Task DrawEdgeTextBox(Context2D context, List<Edge> edges, Brush brush, CanvasSettings settings) {
            var positions = new List<Vector2>();
            foreach (var edge in edges) {
                var center = edge.FindCenter();
                //TODO: include small offsets
                if (positions.Contains(center)) {
                    center = (edge.Node1.Position + edge.FindCenter()) / 2;
                    if (positions.Contains(center)) {
                        center = (edge.Node2.Position + edge.FindCenter()) / 2;
                    }
                }

                await context.DrawTextBox(brush, Manipulate(center, settings), Math.Round(edge.Distance, 1).ToString());
                positions.Add(center);
            }
        }

        private static Vector2 Manipulate(Vector2 vector, CanvasSettings settings) {
            return ((vector - settings.MinPos) * settings.Scale + settings.Offset).InverseY(settings.Height);
        }
    }
}