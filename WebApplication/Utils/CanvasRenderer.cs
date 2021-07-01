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

        public static async Task DrawNodes(Context2D context, IEnumerator<GraphState> enumerator, int cHeight) {
            foreach (var node in enumerator.Current.Nodes) {
                var position = node.Position;

                await context.DrawCircle(NodeBrush, NodeSize,
                    Manipulate(position, cHeight));
                await context.WriteText(NodeBrush.TextFont, NodeBrush.TextStyle, node.Index.ToString(),
                    Manipulate(position, cHeight));
            }
        }

        public static async Task DrawEdges(Context2D context, IEnumerator<GraphState> enumerator, int cHeight) {
            foreach (var edge in enumerator.Current.PathEdges) {
                var center = edge.FindCenter();
                var pos1 = edge.Node1.Position;
                var pos2 = edge.Node2.Position;

                await context.DrawLine(EdgeBrush,
                    Manipulate(pos1, cHeight),
                    Manipulate(pos2, cHeight));
                await context.WriteText(EdgeBrush.TextFont, EdgeBrush.TextStyle, Math.Round(edge.Distance, 1).ToString(),
                    Manipulate(center, cHeight));
            }
        }

        private static Vector2 Manipulate(Vector2 v, int CanvasHeight) => (v * Scale + Offset).InverseY(CanvasHeight);
    }
}