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
         * grid and the node positions, sees the same value.
         *
         * The cap is on the BUFFER edge, which is what actually costs memory, not on the drawing:
         * a ratio of 2 doubles both edges and quadruples the allocation, so the two have to be
         * accounted for together. */
        private const float MaxEdgePixels = 3000;

        /* Beyond 2 the extra samples are not visible at these line widths and only cost memory. */
        private const float MaxDevicePixelRatio = 2;

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

        /* Width and Height are the drawing's own coordinate space, in CSS pixels. Every draw call
         * works in these, which is why adding the pixel ratio below did not touch the renderer. */
        public int Height => (int)(Scale * SpanY);
        public int Width => (int)(Scale * SpanX);

        /* What the browser reported, before clamping. GraphView reads it once from JS. */
        public float RawDevicePixelRatio { get; set; } = 1;

        /* A CSS pixel is not a device pixel. The buffer was sized in CSS pixels and then scaled up
         * by the display, so on any HiDPI screen every line and label was drawn at half resolution
         * and enlarged - the blur was in the bitmap, not in the drawing. The buffer is the CSS box
         * times this ratio, and the transform is pre-multiplied by it so the drawing code is
         * unaffected. Clamped so the buffer edge still respects MaxEdgePixels: on an instance
         * already large enough to hit that ceiling the ratio drops back towards 1 rather than
         * quadrupling an allocation that was deliberately bounded. */
        public float DevicePixelRatio {
            get {
                var ratio = Math.Clamp(RawDevicePixelRatio, 1, MaxDevicePixelRatio);
                var longestEdge = Math.Max(Width, Height);

                if (longestEdge > 0 && longestEdge * ratio > MaxEdgePixels)
                    ratio = Math.Max(1, MaxEdgePixels / longestEdge);

                return ratio;
            }
        }

        public int BufferWidth => Math.Max(1, (int)MathF.Round(Width * DevicePixelRatio));
        public int BufferHeight => Math.Max(1, (int)MathF.Round(Height * DevicePixelRatio));
        public bool Colorize { get; set; }
        public bool Annotate { get; set; }
        public bool ShowGrid { get; set; }
        public string BackgroundColor { get; set; }
        public float NodeRadius { get; set; }
    }
}