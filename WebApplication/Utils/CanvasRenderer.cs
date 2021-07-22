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
        private const float NodeSize = 4;
        private const float Scale = 20;

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
            TextFont = "25px serif",
            TextStyle = "blue"
        };
        private static readonly Brush GridBrush = new() { Color = "#999999", Width = 1 };
        private static readonly Vector2 Offset = new(NodeSize + 5, NodeSize + 5);

        public static async Task DrawGrid(Context2D context, int maxX, int maxY, int cHeight) {
            for (int i = 0; i <= maxY; i++) {
                await context.DrawLine(GridBrush,
                    Manipulate(new Vector2(0, i), cHeight),
                    Manipulate(new Vector2(maxX, i), cHeight));
            }
            for (int i = 0; i <= maxX; i++) {
                await context.DrawLine(GridBrush,
                    Manipulate(new Vector2(i, 0), cHeight),
                    Manipulate(new Vector2(i, maxY), cHeight));
            }
        }

        public static async Task DrawNodes(Context2D context, GraphState state, int cHeight) {
            foreach (var node in state.Nodes) {
                await context.DrawCircle(NodeBrush, NodeSize,
                    Manipulate(node.Position, cHeight));
                await context.WriteText(NodeBrush.TextFont, NodeBrush.TextStyle, node.Index.ToString(),
                    Manipulate(node.Position, cHeight));
            }
        }

        public static async Task DrawEdges(Context2D context, GraphState state, int height) {
            var brush = EdgeBrush.Copy();
            foreach (var edge in state.PathEdges) {                
                if (edge.Color != null)
                    brush.Color = edge.Color;

                await context.DrawLine(brush,
                    Manipulate(edge.Node1.Position, height),
                    Manipulate(edge.Node2.Position, height));
                await context.WriteText(brush.TextFont, brush.TextStyle, Math.Round(edge.Distance, 1).ToString(),
                    Manipulate(edge.FindCenter(), height));
            }
        }

        private static Vector2 Manipulate(Vector2 v, int CanvasHeight) => (v * Scale + Offset).InverseY(CanvasHeight);
    }
}