using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace WebApplication.Utils {
    public class CanvasSettings {
        /* The canvas used to be sized by a Scale setting - pixels per coordinate unit - and drawn
         * at one buffer pixel per CSS pixel. The default graph came out as a 360 px square that sat
         * small inside its column, and on any display with a device pixel ratio above 1 (HiDPI
         * laptops, phones, a zoomed browser) the browser stretched that buffer, which is why the
         * graph always looked soft.
         *
         * Now the canvas is fitted to its frame: Fit takes the frame's width, a height limit and the
         * device pixel ratio. Width and Height are the CSS size, and everything is drawn in those
         * units; the buffer is that size times the pixel ratio, and each frame starts with the
         * matching transform (GraphView.ResetCanvas). */

        /* A buffer is quadratic in its edge length, so the ratio is capped against the buffer's
         * longest edge as well: a 3000 px edge is about 36 MB of backing store per canvas. */
        private const float MaxBufferEdge = 3000;
        private const float MaxPixelRatio = 3;

        public Vector2 MinPos { get; set; }
        public Vector2 MaxPos { get; set; }

        /* CSS pixels per coordinate unit, as fitted. */
        public float Scale { get; private set; } = 30;
        public float PixelRatio { get; private set; } = 1;

        private float SpanX => Math.Max(1, MaxPos.X - MinPos.X + 1);
        private float SpanY => Math.Max(1, MaxPos.Y - MinPos.Y + 1);

        public Vector2 Offset => new(Scale / 2, Scale / 2);
        public int Height => (int)Math.Round(Scale * SpanY);
        public int Width => (int)Math.Round(Scale * SpanX);
        public int BufferWidth => (int)Math.Round(Width * PixelRatio);
        public int BufferHeight => (int)Math.Round(Height * PixelRatio);

        /* The element's size: the width, with the height following from the aspect ratio, so a
         * column narrower than the fitted width (mid-resize) shrinks it without distorting it. */
        public string CssSize => $"width: {Width}px; aspect-ratio: {Width} / {Height};";

        /* Fits the graph's extent into availableWidth x maxHeight CSS pixels at the given device
         * pixel ratio. Returns whether the canvas changed size - a resized canvas is cleared by the
         * browser and has to be drawn again. */
        public bool Fit(double availableWidth, double maxHeight, double devicePixelRatio) {
            var before = (BufferWidth, BufferHeight, Width, Height);

            var scale = (float)Math.Min(availableWidth / SpanX, maxHeight / SpanY);
            Scale = Math.Max(1, scale);

            var ratio = (float)Math.Clamp(devicePixelRatio, 1, MaxPixelRatio);
            PixelRatio = Math.Min(ratio, MaxBufferEdge / Math.Max(1, Math.Max(Width, Height)));

            return before != (BufferWidth, BufferHeight, Width, Height);
        }

        public bool Colorize { get; set; }
        public bool Annotate { get; set; }
        public bool ShowGrid { get; set; }
        public CanvasTheme Theme { get; set; } = new();

        /* How far apart the nodes typically sit, in coordinate units: the distance to the nearest
         * neighbour that 90 % of the nodes have at least. A low percentile rather than the minimum,
         * so one close pair does not shrink every node. CanvasRenderer.NodeLayout turns it into a
         * cap on the node size once the canvas has a scale. */
        public double NodeSpacing { get; private set; }

        /* The highest pheromone level of the whole ant colony run, so line widths compare across
         * iterations instead of being scaled anew in each one. */
        public double PheromoneMax { get; set; }

        public void SetNodes(IReadOnlyList<Vector2> positions) {
            var nearest = new List<double>(positions.Count);

            for (var i = 0; i < positions.Count; i++) {
                var best = double.MaxValue;
                for (var j = 0; j < positions.Count; j++) {
                    if (i == j)
                        continue;
                    var d = Vector2.Distance(positions[i], positions[j]);
                    if (d > 0 && d < best)
                        best = d;
                }
                if (best < double.MaxValue)
                    nearest.Add(best);
            }

            nearest.Sort();
            NodeSpacing = nearest.Count == 0 ? 0 : nearest[nearest.Count / 10];
        }
    }
}