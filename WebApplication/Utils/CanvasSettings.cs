using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace WebApplication.Utils {
    public class CanvasSettings {
        /* A canvas is a pixel buffer, so its cost is quadratic in the edge length: at scale 30 a
         * 200x200 coordinate range asks the browser for two 6000x6000 canvases, roughly 300 MB of
         * backing store. The requested scale is capped against the graph's extent instead, which
         * shrinks the drawing rather than clipping it - everything that reads Scale, including the
         * grid and the node positions, sees the same value. */
        private const float MaxEdgePixels = 3000;

        private float _scale;

        public Vector2 MinPos { get; set; }
        public Vector2 MaxPos { get; set; }
        public float Scale {
            get => Math.Min(_scale, MaxEdgePixels / Math.Max(SpanX, SpanY));
            set => _scale = value;
        }

        private float SpanX => Math.Max(1, MaxPos.X - MinPos.X + 1);
        private float SpanY => Math.Max(1, MaxPos.Y - MinPos.Y + 1);

        public Vector2 Offset => new(Scale / 2, Scale / 2);
        public int Height => (int)(Scale * SpanY);
        public int Width => (int)(Scale * SpanX);
        public bool Colorize { get; set; }
        public bool Annotate { get; set; }
        public bool ShowGrid { get; set; }
        public string BackgroundColor { get; set; }
        public float NodeRadius { get; set; }
    }
}