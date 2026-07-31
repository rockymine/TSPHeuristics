using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace WebApplication.Utils {
    public class CanvasSettings {
        /* A canvas is a pixel buffer, so its cost is quadratic in the edge length. The cap is on
         * the buffer edge, which is what costs memory: a pixel ratio of 2 doubles both edges and
         * quadruples the allocation, so the two are accounted for together. */
        private const float MaxEdgePixels = 3000;

        /* Beyond 2 the extra samples are not visible at these line widths and only cost memory. */
        private const float MaxDevicePixelRatio = 2;

        /* Pixels per coordinate unit used before the stage has been measured. */
        private const float FallbackScale = 30;

        public Vector2 MinPos { get; set; }
        public Vector2 MaxPos { get; set; }

        /* The box one canvas may occupy, in CSS pixels, set from the measured stage. GraphView
         * subtracts the stage padding, the gap between two panes and the pane label first, so
         * this is the budget for the drawing itself. */
        public float AvailableWidth { get; set; }
        public float AvailableHeight { get; set; }

        /* 1 fits the graph to that box. Above 1 the drawing is larger than the stage and the
         * stage scrolls; below it the drawing stays centred. */
        public float Zoom { get; set; } = 1;

        /* Fit the graph to the available box, then apply the zoom. Deriving the scale rather than
         * taking it as an input keeps the drawing sized to its container and separates how large
         * it is from how much memory the buffer costs. */
        private float FitScale =>
            AvailableWidth > 0 && AvailableHeight > 0
                ? Math.Min(AvailableWidth / SpanX, AvailableHeight / SpanY)
                : FallbackScale;

        public float Scale => Math.Min(FitScale * Zoom, MaxEdgePixels / Math.Max(SpanX, SpanY));

        private float SpanX => Math.Max(1, MaxPos.X - MinPos.X + 1);
        private float SpanY => Math.Max(1, MaxPos.Y - MinPos.Y + 1);

        public Vector2 Offset => new(Scale / 2, Scale / 2);

        /* Width and Height are the drawing's own coordinate space, in CSS pixels. Every draw call
         * works in these; the pixel ratio below is applied as a transform rather than by the
         * renderer. */
        public int Height => (int)(Scale * SpanY);
        public int Width => (int)(Scale * SpanX);

        /* What the browser reported, before clamping. GraphView reads it once from JS. */
        public float RawDevicePixelRatio { get; set; } = 1;

        /* A CSS pixel is not a device pixel. The buffer is the CSS box times this ratio, and the
         * transform is pre-multiplied by it so the drawing code works in CSS pixels throughout.
         * Clamped so the buffer edge still respects MaxEdgePixels. */
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