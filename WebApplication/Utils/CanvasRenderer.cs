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

        private const string FontFamily = "system-ui, -apple-system, sans-serif";

        /* The edge length labels' font; it follows the page's text size. */
        public static string EdgeTextFont(CanvasTheme theme) =>
            $"500 {Math.Round(theme.LabelSize * 1.15, 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}px {FontFamily}";

        /* The weight has to come first: "15px serif bold" was invalid, and the context ignores an
         * invalid font, so the node numbers were once written in the 10px default. */
        private static string NodeTextFont(double size) =>
            $"600 {Math.Round(size, 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}px {FontFamily}";

        private const double EdgeWidth = 2;

        /* Below this the numbers are no longer readable, and the nodes are drawn as plain dots. */
        private const double MinNodeFont = 8;
        /* A node may take this share of the typical distance to its nearest neighbour on screen,
         * so neighbours keep a gap between them. */
        private const double SpacingShare = 0.42;
        /* A ring in the canvas colour around each node, so edges stop short of it instead of
         * running into the number. */
        private const double HaloWidth = 1.5;

        /* The node size used to be a setting (8 px by default, 1-10). Now it follows from what a
         * node has to hold: its number, at the page's label size, so the nodes grow and shrink with
         * the text size. Only where the graph is too crowded on screen for that - many nodes, a small
         * canvas, a phone - are the nodes made smaller, numbers and all, and below a readable size
         * the numbers are left out. */
        public static (double Radius, double FontSize) NodeLayout(CanvasSettings settings, int maxIndex) {
            var font = settings.Theme.LabelSize;
            var digits = Math.Max(1, maxIndex.ToString().Length);

            /* A digit of a semibold system font is about 0.62 em wide; the circle leaves 2 px
             * around the number, and at least 0.75 em of radius so single digits are not tiny. */
            double RadiusFor(double size) => Math.Max(0.75 * size, 0.31 * size * digits + 2);

            var radius = RadiusFor(font);
            var cap = settings.NodeSpacing * settings.Scale * SpacingShare;
            if (cap <= 0 || cap >= radius)
                return (radius, font);

            var shrunk = font * cap / radius;
            return shrunk >= MinNodeFont ? (cap, shrunk) : (Math.Max(2.5, cap), 0);
        }

        /* The colours come from the theme (settings.Theme), so every brush is built per frame. */
        private static Brush GridBrush(CanvasTheme theme) => new() {
            Color = theme.Grid, Width = 0.75, TextFont = "", TextStyle = ""
        };
        private static Brush NodeBrush(CanvasTheme theme) => new() {
            Color = theme.Node, Width = 2, Style = FillStyle.Fill, TextStyle = theme.NodeLabel
        };

        /* One grid line used to be one DrawLine, i.e. seven interop calls each. A 90x90 instance
         * meant well over a thousand round trips per canvas clear, and the canvas is cleared on
         * every animation frame. The whole grid is one path with one stroke now. */
        public static void DrawGrid(CanvasScene scene, CanvasSettings settings) {
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

            scene.Lines(GridBrush(settings.Theme), segments);
        }

        /* Nodes are grouped by fill colour so each colour is a single path, and all the index
         * labels share one font/style setup instead of re-sending it per node. */
        public static void DrawNodes(CanvasScene scene, GraphState state,
            LinkedListNode<GraphState> currentStateNode, CanvasSettings settings) {
            var nodes = state.Nodes;
            var theme = settings.Theme;
            var brush = NodeBrush(theme);
            /* "Changes" off: no highlighted nodes either. Colorize used to be set and never read. */
            var moveInfo = settings.Colorize ? currentStateNode.Value.MoveInfo : null;

            var (radius, fontSize) = NodeLayout(settings, nodes.Count == 0 ? 0 : nodes.Max(n => n.Index));

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
                scene.Circles(group.Key, radius, group.Value, theme.Background, HaloWidth);
            }

            if (fontSize > 0)
                scene.Texts(NodeTextFont(fontSize), brush.TextStyle, labels);
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

        public static void DrawEdges(CanvasScene scene, GraphState state,
            LinkedListNode<GraphState> stateNode, CanvasSettings settings) {
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

            var brush = new Brush { TextFont = EdgeTextFont(theme), TextStyle = theme.Edge };
            foreach (var group in groups) {
                brush.Color = group.Key.Color;
                brush.Width = group.Key.Width;
                brush.Dashed = group.Key.Dashed;
                scene.Lines(brush, group.Value);
            }

            /* A rejected draw leaves the tour as it was, so there is nothing to colour - which would
             * make it look as if nothing had happened. The edges the neighbour would have put in
             * are drawn thin and dashed instead: the move that was turned down. */
            if (current && settings.Colorize && state.Outcome == DrawOutcome.LongerRejected &&
                state.MoveInfo?.Added is { Count: > 0 } added) {
                brush.Color = theme.Removed;
                brush.Width = 1.5;
                brush.Dashed = true;
                scene.Lines(brush, added.Select(e =>
                    (Manipulate(e.A.Position, settings), Manipulate(e.B.Position, settings))).ToList());
            }

            /* Ant runs put a pheromone level on every edge of the complete graph; annotating all of
             * them is unreadable, which is why this is suppressed. */
            if (settings.Annotate && !state.PathEdges.Any(e => e.Pheromone != 0)) {
                var labels = BuildAnnotations(state).Select(a => (a.Text, Manipulate(a.Position, settings))).ToList();
                scene.Boxes(EdgeTextFont(theme), theme.LabelBackground, theme.LabelText, labels);
            }
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

        /* Where each edge's length goes; the page measures and draws the labels (CanvasScene.Boxes). */
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


        private static Vector2 Manipulate(Vector2 vector, CanvasSettings settings) {
            return ((vector - settings.MinPos) * settings.Scale + settings.Offset).InverseY(settings.Height);
        }
    }

    public class TextMetric {
        public string Text { get; set; }
        public Vector2 Position { get; set; }
    }
}
