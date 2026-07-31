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
        /* GraphView selects this font on the context before measuring the annotations:
         * MeasureTextAsync reports whatever font the context carries, so measuring in another one
         * sizes the label box wrongly. */
        public const string EdgeTextFont = "15px serif";

        /* The colours the drawing uses, resolved from the stylesheet by GraphView. A canvas is
         * painted rather than laid out, so it inherits nothing and the values have to be handed
         * in. The brushes are methods rather than fields so they pick up the current palette; one
         * is built per draw call, not per node or edge. */
        public static CanvasPalette Palette { get; set; } = new();

        private static Brush EdgeBrush() => new() {
            Color = Palette.Edge, Width = 2,
            TextFont = EdgeTextFont, TextStyle = Palette.Edge
        };
        private static Brush NodeBrush() => new() {
            Color = Palette.Node, Width = 2, Style = FillStyle.Fill,
            TextFont = "15px serif bold", TextStyle = Palette.NodeText
        };
        private static Brush GridBrush() => new() {
            Color = Palette.Grid, Width = 0.75
        };

        /* The whole grid is one path with one stroke: the canvas is cleared and redrawn on every
         * animation frame. */
        public static async Task DrawGrid(IContext2DWithoutGetters context, CanvasSettings settings) {
            var segments = new List<(Vector2, Vector2)>();

            for (int i = (int)settings.MinPos.Y; i <= settings.MaxPos.Y; i++) {
                segments.Add((
                    Manipulate(new Vector2((int)settings.MinPos.X, i), settings),
                    Manipulate(new Vector2(settings.MaxPos.X, i), settings)));
            }
            for (int i = (int)settings.MinPos.X; i <= settings.MaxPos.X; i++) {
                segments.Add((
                    Manipulate(new Vector2(i, (int)settings.MinPos.Y), settings),
                    Manipulate(new Vector2(i, settings.MaxPos.Y), settings)));
            }

            await context.DrawLines(GridBrush(), segments);
        }

        /* Nodes are grouped by fill colour so each colour is a single path, and all the index
         * labels share one font and style setup. */
        public static async Task DrawNodes(IContext2DWithoutGetters context, GraphState state,
            LinkedListNode<GraphState> currentStateNode, CanvasSettings settings) {
            var nodes = state.Nodes;
            var brush = NodeBrush();
            var moveInfo = currentStateNode.Value.MoveInfo;

            var byColor = new Dictionary<string, List<Vector2>>();
            var labels = new List<(string, Vector2)>(nodes.Count);

            foreach (var node in nodes) {
                var position = Manipulate(node.Position, settings);
                var color = NodeColor(moveInfo, node);

                if (!byColor.TryGetValue(color, out var group)) {
                    group = new List<Vector2>();
                    byColor[color] = group;
                }

                group.Add(position);
                labels.Add((node.Index.ToString(), position));
            }

            foreach (var group in byColor) {
                brush.Color = group.Key;
                await context.DrawCircles(brush, settings.NodeRadius, group.Value);
            }

            await context.WriteTexts(brush.TextFont, brush.TextStyle, labels);
        }

        private static string NodeColor(MoveInfo moveInfo, Node node) {
            if (moveInfo == null || moveInfo.Nodes.Count < 2)
                return Palette.Node;
            if (node.Index == moveInfo.Nodes[0].Index)
                return Palette.NodeI;
            if (node.Index == moveInfo.Nodes[1].Index)
                return Palette.NodeJ;

            return Palette.Node;
        }

        public static async Task DrawEdges(IContext2DWithoutGetters context, GraphState state,
            LinkedListNode<GraphState> stateNode, CanvasSettings settings, IReadOnlyList<TextMetric> annotations) {
            var edges = state.PathEdges;

            /* Pheromone weighted edges each need their own width, so they cannot share a path;
             * everything else is grouped by (colour, width). */
            var groups = new Dictionary<(string Color, double Width), List<(Vector2, Vector2)>>();
            var edgeBrush = EdgeBrush();

            var compare = state == stateNode.Value ? stateNode.Previous?.Value.PathEdges : stateNode.Value.PathEdges;
            var changeColor = state == stateNode.Value ? Palette.Added : Palette.Removed;
            var compareKeys = compare == null ? null : PathKeys(compare);

            foreach (var edge in edges) {
                var color = Palette.Edge;
                if (compareKeys != null && !compareKeys.Contains(EdgeKey(edge)))
                    color = changeColor;

                var width = edge.Pheromone != 0 ? edge.Pheromone * 500 : edgeBrush.Width;
                var key = (color, width);

                if (!groups.TryGetValue(key, out var group)) {
                    group = new List<(Vector2, Vector2)>();
                    groups[key] = group;
                }

                group.Add((Manipulate(edge.Node1.Position, settings), Manipulate(edge.Node2.Position, settings)));
            }

            foreach (var group in groups) {
                edgeBrush.Color = group.Key.Color;
                edgeBrush.Width = group.Key.Width;
                /* Lost edges are dashed as well as recoloured. */
                edgeBrush.Dashed = group.Key.Color == Palette.Removed;
                await context.DrawLines(edgeBrush, group.Value);
            }

            if (annotations != null)
                await DrawEdgeTextBoxes(context, annotations, settings);
        }

        /* Hashes the comparison tour once, so membership is a set lookup rather than a scan per
         * edge drawn. */
        private static HashSet<(int, int)> PathKeys(List<Edge> edges) {
            var keys = new HashSet<(int, int)>();
            foreach (var edge in edges)
                keys.Add(EdgeKey(edge));

            return keys;
        }

        private static (int, int) EdgeKey(Edge edge) {
            var a = edge.Node1.Index;
            var b = edge.Node2.Index;
            return a < b ? (a, b) : (b, a);
        }

        /* Label placement and the text measurements are computed by the caller, which still has
         * an unbatched context; only the drawing happens here. */
        public static List<TextMetric> BuildAnnotations(GraphState state) {
            var positions = new HashSet<Vector2>();
            var annotations = new List<TextMetric>(state.PathEdges.Count);

            foreach (var edge in state.PathEdges) {
                var center = edge.FindCenter();

                //TODO: include small offsets
                if (positions.Contains(center)) {
                    center = (edge.Node1.Position + edge.FindCenter()) / 2;
                    if (positions.Contains(center))
                        center = (edge.Node2.Position + edge.FindCenter()) / 2;
                }

                positions.Add(center);
                annotations.Add(new TextMetric {
                    Text = Math.Round(edge.Distance, 1).ToString(),
                    Position = center
                });
            }

            return annotations;
        }

        private static async Task DrawEdgeTextBoxes(IContext2DWithoutGetters context,
            IReadOnlyList<TextMetric> annotations, CanvasSettings settings) {
            /* The chip has its own fill and text colour, taken from the brush so DrawTextBox
             * holds no colours of its own. */
            var brush = EdgeBrush();
            brush.Color = Palette.LabelBackground;
            brush.TextStyle = Palette.LabelText;

            foreach (var annotation in annotations) {
                await context.DrawTextBox(brush, Manipulate(annotation.Position, settings),
                    annotation.Text, annotation.Width, annotation.Height);
            }
        }

        private static Vector2 Manipulate(Vector2 vector, CanvasSettings settings) {
            return ((vector - settings.MinPos) * settings.Scale + settings.Offset).InverseY(settings.Height);
        }
    }

    public class TextMetric {
        public string Text { get; set; }
        public Vector2 Position { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }
}
