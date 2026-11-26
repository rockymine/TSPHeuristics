using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace WebApplication.Utils {
    /* One frame of the graph, described rather than drawn: CanvasRenderer fills it, and
     * tspCanvas.draw (canvas.js) paints it in a single interop call.
     *
     * The frame used to be drawn through Excubo's Batch2D, which serialises every canvas call -
     * each MoveTo, LineTo and Ellipse - in .NET and replays them in the page. With the interpreter
     * that runs .NET in the browser, that was about 25 ms per canvas for a twelve node graph, half
     * of every playback step. A scene is a handful of operations, each with a flat array of
     * coordinates, and the drawing loops run in JavaScript. */
    public class CanvasScene {
        public double Width { get; set; }
        public double Height { get; set; }
        public double Ratio { get; set; }
        public string Background { get; set; }
        public List<SceneOp> Ops { get; } = new();

        /* Segments sharing one style, as x1, y1, x2, y2, ... An animation role tells the page how the
         * segments enter when the step is played (canvas.js): "grow" draws them in from their first
         * point, "fade" fades them to a faint ghost, "candidate" draws them in and takes them back
         * (a rejected neighbor), "flash" turns them to the alt colour for a moment. Without one
         * they are simply there. */
        public void Lines(Brush brush, IReadOnlyList<(Vector2 From, Vector2 To)> segments, string anim = null, string alt = null) {
            if (segments.Count == 0)
                return;

            var points = new float[segments.Count * 4];
            for (var i = 0; i < segments.Count; i++) {
                var (from, to) = segments[i];
                points[i * 4] = Round(from.X);
                points[i * 4 + 1] = Round(from.Y);
                points[i * 4 + 2] = Round(to.X);
                points[i * 4 + 3] = Round(to.Y);
            }

            Ops.Add(new SceneOp {
                Kind = "lines", Color = brush.Color, Width = brush.Width, Dashed = brush.Dashed, Points = points,
                Anim = anim, Fill = alt
            });
        }

        /* Segments of one colour whose widths change from w0 to w1 as the step is played: the
         * pheromone on every edge of an ant colony run. */
        public void Widths(string color, double alpha, IReadOnlyList<(Vector2 From, Vector2 To)> segments,
            IReadOnlyList<double> from, IReadOnlyList<double> to) {
            if (segments.Count == 0)
                return;

            Lines(new Brush { Color = color, Width = 1 }, segments);
            var op = Ops[^1];
            op.Kind = "widths";
            op.Alpha = alpha;
            op.W0 = Round(from);
            op.W1 = Round(to);
        }

        /* Filled circles of one colour and radius, with an optional ring (halo) around each. */
        /* Pulse: the circles swell once while the step is played (the move's two nodes). */
        public void Circles(string color, double radius, IReadOnlyList<Vector2> centres, string halo = null, double haloWidth = 0,
            bool pulse = false) {
            if (centres.Count == 0)
                return;

            Ops.Add(new SceneOp {
                Kind = "circles", Color = color, Radius = radius, Halo = halo, Width = haloWidth, Points = Flatten(centres),
                Anim = pulse ? "pulse" : null
            });
        }

        /* Texts centred on their points. */
        public void Texts(string font, string color, IReadOnlyList<(string Text, Vector2 At)> labels) {
            if (labels.Count == 0)
                return;

            var texts = new string[labels.Count];
            var centres = new Vector2[labels.Count];
            for (var i = 0; i < labels.Count; i++)
                (texts[i], centres[i]) = labels[i];

            Ops.Add(new SceneOp { Kind = "texts", Font = font, Color = color, Texts = texts, Points = Flatten(centres) });
        }

        /* Texts on pill-shaped boxes; the page measures them, so no round trip is needed for that. */
        public void Boxes(string font, string boxColor, string textColor, IReadOnlyList<(string Text, Vector2 At)> labels) {
            if (labels.Count == 0)
                return;

            Texts(font, textColor, labels);
            var op = Ops[^1];
            op.Kind = "boxes";
            op.Fill = boxColor;
        }

        /* The scene as JSON, written by hand. Handing the object to the interop serialised it by
         * reflection, which the browser's .NET interpreter makes the slowest part of drawing a
         * frame; the scene is a few flat arrays and needs none of it. */
        public string ToJson() {
            var sb = new StringBuilder(256 + Ops.Count * 128);
            sb.Append("{\"width\":").Append(Num(Width))
              .Append(",\"height\":").Append(Num(Height))
              .Append(",\"ratio\":").Append(Num(Ratio))
              .Append(",\"background\":").Append(Str(Background))
              .Append(",\"ops\":[");

            for (var i = 0; i < Ops.Count; i++) {
                var op = Ops[i];
                if (i > 0)
                    sb.Append(',');
                sb.Append("{\"kind\":").Append(Str(op.Kind))
                  .Append(",\"color\":").Append(Str(op.Color))
                  .Append(",\"fill\":").Append(Str(op.Fill))
                  .Append(",\"halo\":").Append(Str(op.Halo))
                  .Append(",\"font\":").Append(Str(op.Font))
                  .Append(",\"width\":").Append(Num(op.Width))
                  .Append(",\"radius\":").Append(Num(op.Radius))
                  .Append(",\"dashed\":").Append(op.Dashed ? "true" : "false")
                  .Append(",\"anim\":").Append(Str(op.Anim))
                  .Append(",\"alpha\":").Append(Num(op.Alpha))
                  .Append(",\"points\":[");
                for (var j = 0; j < op.Points.Length; j++) {
                    if (j > 0)
                        sb.Append(',');
                    sb.Append(op.Points[j].ToString(CultureInfo.InvariantCulture));
                }
                sb.Append(']');
                if (op.W0 != null) {
                    sb.Append(",\"w0\":[").Append(string.Join(",", op.W0.Select(w => w.ToString(CultureInfo.InvariantCulture))))
                      .Append("],\"w1\":[").Append(string.Join(",", op.W1.Select(w => w.ToString(CultureInfo.InvariantCulture))))
                      .Append(']');
                }
                if (op.Texts != null) {
                    sb.Append(",\"texts\":[");
                    for (var j = 0; j < op.Texts.Length; j++) {
                        if (j > 0)
                            sb.Append(',');
                        sb.Append(Str(op.Texts[j]));
                    }
                    sb.Append(']');
                }
                sb.Append('}');
            }

            return sb.Append("]}").ToString();
        }

        private static string Num(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string Str(string value) => value == null ? "null" : JsonSerializer.Serialize(value);

        private static float[] Round(IReadOnlyList<double> values) {
            var rounded = new float[values.Count];
            for (var i = 0; i < values.Count; i++)
                rounded[i] = Round((float)values[i]);
            return rounded;
        }

        private static float[] Flatten(IReadOnlyList<Vector2> points) {
            var flat = new float[points.Count * 2];
            for (var i = 0; i < points.Count; i++) {
                flat[i * 2] = Round(points[i].X);
                flat[i * 2 + 1] = Round(points[i].Y);
            }
            return flat;
        }

        /* A tenth of a CSS pixel is below what the buffer resolves even at a pixel ratio of 3, and
         * keeps the payload short. */
        private static float Round(float value) => MathF.Round(value * 10) / 10;
    }

    public class SceneOp {
        public string Kind { get; set; }
        public string Color { get; set; }
        public string Fill { get; set; }
        public string Halo { get; set; }
        public string Font { get; set; }
        public double Width { get; set; }
        public double Radius { get; set; }
        public bool Dashed { get; set; }
        public string Anim { get; set; }
        public double Alpha { get; set; } = 1;
        public float[] W0 { get; set; }
        public float[] W1 { get; set; }
        public float[] Points { get; set; }
        public string[] Texts { get; set; }
    }
}
