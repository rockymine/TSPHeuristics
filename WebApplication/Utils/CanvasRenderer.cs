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
         * labels share one font/style setup instead of re-sending it per node. The move's two nodes
         * are coloured from the step being shown - also on the "before" picture, so they mark the
         * same nodes on both - and pulse while the step is played. */
        public static void DrawNodes(CanvasScene scene, GraphState state, MoveInfo move, CanvasSettings settings, bool pulse) {
            var nodes = state.Nodes;
            var theme = settings.Theme;
            var brush = NodeBrush(theme);
            /* "Highlight changes" off: no highlighted nodes either. */
            var moveInfo = settings.Colorize ? move : null;

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

            foreach (var group in byColor)
                scene.Circles(group.Key, radius, group.Value, theme.Background, HaloWidth, pulse && group.Key != theme.Node);

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

        /* One canvas shows the step; it used to take two, the tour before on the left and after on
         * the right, and the change had to be found by comparing them. Now the page plays the change
         * (canvas.js): the edges the step put in are drawn in, green, from their first node; the
         * edges it took out fade to a faint red dashed ghost, which stays so a paused step is still
         * readable. A rejected draw leaves the tour as it was, so its neighbor's edges are drawn in
         * dashed and taken back, and the edges it would have removed flash. Replay plays it again;
         * the tour before is a picture of its own (DrawBefore). */
        public static void DrawStep(CanvasScene scene, GraphState state, GraphState previous, CanvasSettings settings) {
            var theme = settings.Theme;

            if (IsPheromone(state)) {
                DrawPheromone(scene, state, previous, settings);
                return;
            }

            var prevKeys = settings.Colorize && previous != null ? PathKeys(previous.PathEdges) : null;
            var rejected = settings.Colorize && state.Outcome == DrawOutcome.LongerRejected &&
                           state.MoveInfo?.Added is { Count: > 0 };
            var flashKeys = rejected
                ? state.MoveInfo.Removed.Select(e => Key(e.A.Index, e.B.Index)).ToHashSet()
                : null;

            var keep = new List<(Vector2, Vector2)>();
            var added = new List<(Vector2, Vector2)>();
            var flash = new List<(Vector2, Vector2)>();

            foreach (var edge in state.PathEdges) {
                var key = EdgeKey(edge);
                if (flashKeys != null && flashKeys.Contains(key))
                    flash.Add(Segment(edge.Node1, edge.Node2, settings));
                else if (prevKeys != null && !prevKeys.Contains(key))
                    added.Add(Grown(edge, previous, settings));
                else
                    keep.Add(Segment(edge.Node1, edge.Node2, settings));
            }

            scene.Lines(new Brush { Color = theme.Edge, Width = EdgeWidth }, keep);

            if (prevKeys != null) {
                var currentKeys = PathKeys(state.PathEdges);
                var removed = previous.PathEdges.Where(e => !currentKeys.Contains(EdgeKey(e)))
                    .Select(e => Segment(e.Node1, e.Node2, settings)).ToList();
                scene.Lines(new Brush { Color = theme.Removed, Width = 2.2, Dashed = true }, removed, "fade");
                scene.Lines(new Brush { Color = theme.Added, Width = 2.8 }, added, "grow");
            }

            if (rejected) {
                scene.Lines(new Brush { Color = theme.Edge, Width = EdgeWidth }, flash, "flash", theme.Removed);
                scene.Lines(new Brush { Color = theme.Removed, Width = 2.4, Dashed = true },
                    state.MoveInfo.Added.Select(e => Segment(e.A, e.B, settings)).ToList(), "candidate");
            }

            DrawAnnotations(scene, state, settings);
        }

        /* The tour before the step, shown while Replay or B is held: its edges, with those the step
         * removes dashed red. */
        public static void DrawBefore(CanvasScene scene, GraphState previous, GraphState state, CanvasSettings settings) {
            var theme = settings.Theme;

            if (IsPheromone(previous)) {
                DrawPheromone(scene, previous, null, settings);
                return;
            }

            var currentKeys = settings.Colorize ? PathKeys(state.PathEdges) : null;
            var keep = new List<(Vector2, Vector2)>();
            var removed = new List<(Vector2, Vector2)>();

            foreach (var edge in previous.PathEdges) {
                if (currentKeys != null && !currentKeys.Contains(EdgeKey(edge)))
                    removed.Add(Segment(edge.Node1, edge.Node2, settings));
                else
                    keep.Add(Segment(edge.Node1, edge.Node2, settings));
            }

            scene.Lines(new Brush { Color = theme.Edge, Width = EdgeWidth }, keep);
            scene.Lines(new Brush { Color = theme.Removed, Width = 2.4, Dashed = true }, removed);
            DrawAnnotations(scene, previous, settings);
        }

        /* An ant colony state carries a pheromone level on every edge of the complete graph. The
         * levels are drawn as widths, scaled to the highest level of the whole run so they compare
         * across iterations, and grow or shrink from the previous iteration's as the step plays. The
         * best tour of the iteration is drawn on top; it used not to be drawn at all. */
        private static void DrawPheromone(CanvasScene scene, GraphState state, GraphState previous, CanvasSettings settings) {
            var theme = settings.Theme;
            var max = settings.PheromoneMax > 0 ? settings.PheromoneMax : state.PathEdges.Max(e => e.Pheromone);
            double Width(double tau) => 0.4 + 9 * tau / max;

            var before = previous != null && IsPheromone(previous)
                ? previous.PathEdges.GroupBy(EdgeKey).ToDictionary(g => g.Key, g => g.First().Pheromone)
                : null;

            var segments = new List<(Vector2, Vector2)>(state.PathEdges.Count);
            var from = new List<double>(state.PathEdges.Count);
            var to = new List<double>(state.PathEdges.Count);
            foreach (var edge in state.PathEdges) {
                segments.Add(Segment(edge.Node1, edge.Node2, settings));
                to.Add(Width(edge.Pheromone));
                from.Add(before != null && before.TryGetValue(EdgeKey(edge), out var tau) ? Width(tau) : Width(edge.Pheromone));
            }
            scene.Widths(theme.Pheromone, 0.55, segments, from, to);

            var tour = new List<(Vector2, Vector2)>();
            for (var i = 0; i + 1 < state.Path.Count; i++)
                tour.Add(Segment(state.Path[i], state.Path[i + 1], settings));
            scene.Lines(new Brush { Color = theme.Edge, Width = 1.6 }, tour);
        }

        private static bool IsPheromone(GraphState state) => state.PathEdges.Any(e => e.Pheromone != 0);

        /* Edge lengths; not for ant runs, where every edge of the complete graph is drawn. */
        private static void DrawAnnotations(CanvasScene scene, GraphState state, CanvasSettings settings) {
            if (!settings.Annotate || IsPheromone(state))
                return;

            var labels = BuildAnnotations(state).Select(a => (a.Text, Manipulate(a.Position, settings))).ToList();
            scene.Boxes(EdgeTextFont(settings.Theme), settings.Theme.LabelBackground, settings.Theme.LabelText, labels);
        }

        private static (Vector2, Vector2) Segment(Node a, Node b, CanvasSettings settings) =>
            (Manipulate(a.Position, settings), Manipulate(b.Position, settings));

        /* An added edge is drawn in from its first node - for nearest neighbor from the node the
         * tour had reached, so the new edge grows out of the tour's end. */
        private static (Vector2, Vector2) Grown(Edge edge, GraphState previous, CanvasSettings settings) {
            var end = previous?.Path.Count > 0 ? previous.Path[^1] : null;
            return end != null && end.Index == edge.Node2.Index
                ? Segment(edge.Node2, edge.Node1, settings)
                : Segment(edge.Node1, edge.Node2, settings);
        }

        private static (int, int) Key(int a, int b) => a < b ? (a, b) : (b, a);

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
