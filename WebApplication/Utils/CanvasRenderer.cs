using Excubo.Blazor.Canvas.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;
using WebApplication.Extensions;

namespace WebApplication.Utils {
    public class CanvasRenderer {
        //private const float NodeSize = 8;
        //private const float Scale = 20;

        private static readonly Brush EdgeBrush = new() {
            Color = "black",
            Width = 3,
            TextFont = "15px serif",
            TextStyle = "black"
        };
        private static readonly Brush NodeBrush = new() {
            Color = "#4e5072",
            Width = 2,
            Style = FillStyle.Fill,
            TextFont = "15px serif bold",
            TextStyle = "white"
        };
        private static readonly Brush GridBrush = new() { Color = "#999999", Width = 0.75 };
        //private static readonly Vector2 Offset = new(Scale / 2, Scale / 2);

        public static async Task DrawGrid(Context2D context, CanvasSettings settings) {
            for (int i = 0; i <= settings.Max.Y; i++) {
                await context.DrawLine(GridBrush,
                    Manipulate(new Vector2(0, i), settings),
                    Manipulate(new Vector2(settings.Max.X, i), settings));
            }
            for (int i = 0; i <= settings.Max.X; i++) {
                await context.DrawLine(GridBrush,
                    Manipulate(new Vector2(i, 0), settings),
                    Manipulate(new Vector2(i, settings.Max.Y), settings));
            }
        }

        public static async Task DrawPath(Context2D context, List<Node> nodes, List<Edge> edges, CanvasSettings settings, bool annotate, bool colorize) {
            settings.Colorize = colorize;
            settings.Annotate = annotate;
            await DrawEdges(context, edges, settings);
            await DrawNodes(context, nodes, settings);
        }

        public static async Task DrawPathCompared(Context2D context, GraphState state, GraphState compare, CanvasSettings settings) {

        }

        private static async Task DrawNodes(Context2D context, List<Node> nodes, CanvasSettings settings) {
            var brush = NodeBrush.Copy();

            foreach (var node in nodes) {
                brush.Color = node.Color != null ? node.Color : NodeBrush.Color;

                await context.DrawCircle(brush, settings.NodeRadius,
                    Manipulate(node.Position, settings));
            }

            foreach (var node in nodes) {
                await context.WriteText(brush.TextFont, brush.TextStyle, node.Index.ToString(),
                    Manipulate(node.Position, settings));
            }
        }

        private static async Task DrawEdges(Context2D context, List<Edge> edges, CanvasSettings settings) {
            var brush = EdgeBrush.Copy();

            foreach (var edge in edges) {
                if (edge.Color != null && settings.Colorize)
                    brush.Color = edge.Color;

                if (edge.Pheromone != 0) {
                    brush.Width = edge.Pheromone * 1000;
                    settings.Annotate = false;
                }

                await context.DrawLine(brush,
                    Manipulate(edge.Node1.Position, settings),
                    Manipulate(edge.Node2.Position, settings));
            }

            if (settings.Annotate)
                await DrawEdgeTextBox(context, edges, brush, settings);
        }

        private static async Task DrawEdgesCompared(Context2D context, List<Edge> edges, List<Edge> compare, CanvasSettings settings) {

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
            return (vector * settings.Scale + settings.Offset).InverseY(settings.Height);
        }
    }
}