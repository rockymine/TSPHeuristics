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
        private const double MaxGridLines = 30;

        private static double GridStep(double span) {
            var magnitude = 1d;
            while (true) {
                foreach (var factor in new[] { 1d, 2d, 5d }) {
                    var step = factor * magnitude;
                    if (span / step <= MaxGridLines)
                        return step;
                }
                magnitude *= 10;
            }
        }

        /* GraphView has to select this font on the context before measuring the annotations, or
         * MeasureTextAsync reports the canvas default (10px sans-serif) and every label box comes
         * out too small for the text it is drawn behind. */
        public const string EdgeTextFont = "15px serif";

        /* Was "15px serif bold", which is not a valid font: the weight has to come first. The
         * context ignores an invalid font, so the node numbers were written in whatever font was
         * set last - the 10px sans-serif default on most frames. Sized to sit inside the default
         * 8px node radius with two digits. */
        private const string NodeTextFont = "600 11px system-ui, sans-serif";
        private const double EdgeWidth = 2;

        /* The colours come from the theme (settings.Theme), so every brush is built per frame. */
        private static Brush GridBrush(CanvasTheme theme) => new() {
            Color = theme.Grid, Width = 0.75, TextFont = "", TextStyle = ""
        };
        private static Brush NodeBrush(CanvasTheme theme) => new() {
            Color = theme.Node, Width = 2, Style = FillStyle.Fill,
            TextFont = NodeTextFont, TextStyle = theme.NodeLabel
        };
        private static Brush LabelBrush(CanvasTheme theme) => new() {
            Color = theme.LabelBackground, Width = 2, Style = FillStyle.Fill,
            TextFont = EdgeTextFont, TextStyle = theme.LabelText
        };

        /* One grid line used to be one DrawLine, i.e. seven interop calls each. A 90x90 instance
         * meant well over a thousand round trips per canvas clear, and the canvas is cleared on
         * every animation frame. The whole grid is one path with one stroke now. */
        public static async Task DrawGrid(IContext2DWithoutGetters context, CanvasSettings settings) {
            var segments = new List<(Vector2, Vector2)>();

            /* One line per whole coordinate meant 5,000 lines for a 0..5000 graph (6.4 s per
             * Execute, 2.2 s per step) and a hung page at 0..100000. The step is the smallest of
             * 1, 2, 5, 10, 20, 50, ... that keeps it to at most MaxGridLines lines per axis. */
            var step = GridStep(Math.Max(settings.MaxPos.X - settings.MinPos.X, settings.MaxPos.Y - settings.MinPos.Y));

            for (var y = Math.Ceiling(settings.MinPos.Y / step) * step; y <= settings.MaxPos.Y; y += step) {
                segments.Add((
                    Manipulate(new Vector2(settings.MinPos.X, (float)y), settings),
                    Manipulate(new Vector2(settings.MaxPos.X, (float)y), settings)));
            }
            for (var x = Math.Ceiling(settings.MinPos.X / step) * step; x <= settings.MaxPos.X; x += step) {
                segments.Add((
                    Manipulate(new Vector2((float)x, settings.MinPos.Y), settings),
                    Manipulate(new Vector2((float)x, settings.MaxPos.Y), settings)));
            }

            await context.DrawLines(GridBrush(settings.Theme), segments);
        }

        /* Nodes are grouped by fill colour so each colour is a single path, and all the index
         * labels share one font/style setup instead of re-sending it per node. */
        public static async Task DrawNodes(IContext2DWithoutGetters context, GraphState state,
            LinkedListNode<GraphState> currentStateNode, CanvasSettings settings) {
            var nodes = state.Nodes;
            var theme = settings.Theme;
            var brush = NodeBrush(theme);
            /* "Changes" off: no highlighted nodes either. Colorize used to be set and never read. */
            var moveInfo = settings.Colorize ? currentStateNode.Value.MoveInfo : null;

            var byColor = new Dictionary<string, List<Vector2>>();
            var labels = new List<(string, Vector2)>(nodes.Count);

            foreach (var node in nodes) {
                var position = Manipulate(node.Position, settings);
                var color = NodeColor(moveInfo, node, theme);

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

        private static string NodeColor(MoveInfo moveInfo, Node node, CanvasTheme theme) {
            if (moveInfo == null || moveInfo.Nodes.Count < 2)
                return theme.Node;
            if (node.Index == moveInfo.Nodes[0].Index)
                return theme.MoveFirst;
            if (node.Index == moveInfo.Nodes[1].Index)
                return theme.MoveSecond;

            return theme.Node;
        }

        public static async Task DrawEdges(IContext2DWithoutGetters context, GraphState state,
            LinkedListNode<GraphState> stateNode, CanvasSettings settings, IReadOnlyList<TextMetric> annotations) {
            var edges = state.PathEdges;

            /* Pheromone weighted edges each need their own width, so they cannot share a path;
             * everything else is grouped by (colour, width). The old loop also never restored
             * brush.Width after a pheromone edge, so once one edge carried pheromone every
             * following edge inherited its width. */
            var groups = new Dictionary<(string Color, double Width, bool Dashed), List<(Vector2, Vector2)>>();
            var theme = settings.Theme;

            /* The current state shows the edges the step put in, the previous one those it took out. */
            var current = state == stateNode.Value;
            var compare = current ? stateNode.Previous?.Value.PathEdges : stateNode.Value.PathEdges;
            var changeColor = current ? theme.Added : theme.Removed;
            var compareKeys = compare == null || !settings.Colorize ? null : PathKeys(compare);

            foreach (var edge in edges) {
                var color = theme.Edge;
                var dashed = false;
                if (compareKeys != null && !compareKeys.Contains(EdgeKey(edge))) {
                    color = changeColor;
                    dashed = !current;
                }

                var width = edge.Pheromone != 0 ? edge.Pheromone * 500 : EdgeWidth;
                var key = (color, width, dashed);

                if (!groups.TryGetValue(key, out var group)) {
                    group = new List<(Vector2, Vector2)>();
                    groups[key] = group;
                }

                group.Add((Manipulate(edge.Node1.Position, settings), Manipulate(edge.Node2.Position, settings)));
            }

            var brush = new Brush { TextFont = EdgeTextFont, TextStyle = theme.Edge };
            foreach (var group in groups) {
                brush.Color = group.Key.Color;
                brush.Width = group.Key.Width;
                brush.Dashed = group.Key.Dashed;
                await context.DrawLines(brush, group.Value);
            }

            if (annotations != null)
                await DrawEdgeTextBoxes(context, annotations, settings);
        }

        /* Membership was tested with compare.Find(e => e.IsEqual(edge)) - a linear scan of the
         * comparison tour for every edge drawn, so O(E^2) per frame on a complete graph. */
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

        /* Label placement and the text measurements are computed by the caller, which still has an
         * unbatched context; only the drawing happens here. */
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
            var brush = LabelBrush(settings.Theme);

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
