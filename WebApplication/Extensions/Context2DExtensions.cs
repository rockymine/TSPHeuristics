using Excubo.Blazor.Canvas;
using Excubo.Blazor.Canvas.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using WebApplication.Utils;
using System.Drawing;

namespace WebApplication.Extensions {
    /* Every method here takes IContext2DWithoutGetters, which both Context2D and Batch2D
     * implement. Context2D issues one JS interop round trip per call, so a single redraw of a
     * 30 node instance cost thousands of them; drawing through a Batch2D buffers the whole frame
     * and flushes it in one call instead. The callers decide which to hand in. */
    public static class Context2DExtensions {
        private static readonly double[] Dashed = { 5 };
        private static readonly double[] Solid = { };

        public static async Task DrawLine(this IContext2DWithoutGetters context, Brush brush, Vector2 pos1, Vector2 pos2) {
            await context.SetLineDashAsync(brush.Color == "red" ? Dashed : Solid);
            await context.StrokeStyleAsync(brush.Color);
            await context.LineWidthAsync(brush.Width);
            await context.BeginPathAsync();
            await context.MoveToAsync(pos1.X, pos1.Y);
            await context.LineToAsync(pos2.X, pos2.Y);
            await context.StrokeAsync();
        }

        /* Draws many segments that share one style as a single path. Stroke state is set once and
         * one Stroke call covers the whole batch, instead of seven calls per individual segment. */
        public static async Task DrawLines(this IContext2DWithoutGetters context, Brush brush,
            IReadOnlyList<(Vector2 From, Vector2 To)> segments) {
            if (segments.Count == 0)
                return;

            await context.SetLineDashAsync(brush.Color == "red" ? Dashed : Solid);
            await context.StrokeStyleAsync(brush.Color);
            await context.LineWidthAsync(brush.Width);
            await context.BeginPathAsync();

            foreach (var (from, to) in segments) {
                await context.MoveToAsync(from.X, from.Y);
                await context.LineToAsync(to.X, to.Y);
            }

            await context.StrokeAsync();
        }

        public static async Task DrawCircle(this IContext2DWithoutGetters context, Brush brush, double radius, Vector2 pos) {
            await context.FillStyleAsync(brush.Color);
            await context.LineWidthAsync(brush.Width);
            await context.BeginPathAsync();
            await context.MoveToAsync(pos.X, pos.Y);
            await context.EllipseAsync(pos.X, pos.Y, radius, radius, 0, 0, 360);
            await context.FillAsync(FillRule.EvenOdd);
        }

        /* Circles that share a fill colour go into one path and one Fill call. */
        public static async Task DrawCircles(this IContext2DWithoutGetters context, Brush brush,
            double radius, IReadOnlyList<Vector2> positions) {
            if (positions.Count == 0)
                return;

            await context.FillStyleAsync(brush.Color);
            await context.LineWidthAsync(brush.Width);
            await context.BeginPathAsync();

            foreach (var pos in positions) {
                await context.MoveToAsync(pos.X + radius, pos.Y);
                await context.EllipseAsync(pos.X, pos.Y, radius, radius, 0, 0, 360);
            }

            await context.FillAsync(FillRule.EvenOdd);
        }

        /* Width comes from the caller because MeasureTextAsync needs a round trip and so is not
         * available on a batch; GraphView measures the labels before opening one. */
        public static async Task DrawTextBox(this IContext2DWithoutGetters context, Brush brush,
            Vector2 pos, string text, double width, double height) {
            /* The caller measures the glyphs exactly, so pad before drawing: a box tight against
             * the text reads as a slab. Rounded corners keep the annotation legible where several
             * labels crowd together. */
            var boxWidth = width * 1.2;
            var boxHeight = height * 1.2;

            var left = pos.X - (boxWidth / 2);
            var bottom = pos.Y - (boxHeight / 2);
            var right = left + boxWidth;
            var top = bottom + boxHeight;

            /* A radius wider than half the shorter side would make the arcs overlap. */
            var radius = Math.Min(10, Math.Min(boxWidth, boxHeight) / 2);

            await context.FillStyleAsync("#3f74a8");
            await context.LineWidthAsync(2);
            await context.BeginPathAsync();
            await context.MoveToAsync(left + radius, bottom);
            await context.ArcToAsync(right, bottom, right, top, radius);
            await context.ArcToAsync(right, top, left, top, radius);
            await context.ArcToAsync(left, top, left, bottom, radius);
            await context.ArcToAsync(left, bottom, right, bottom, radius);
            await context.ClosePathAsync();
            await context.FillAsync(FillRule.EvenOdd);
            await WriteText(context, brush.TextFont, "white", text, pos);
        }

        public static async Task FillTextAsyncVector(this IContext2DWithoutGetters context, string text, Vector2 pos) {
            await context.FillTextAsync(text, pos.X, pos.Y);
        }

        public static async Task WriteText(this IContext2DWithoutGetters context, string font, string style, string text, Vector2 v2) {
            await context.FontAsync(font);
            await context.FillStyleAsync(style);
            await context.TextBaseLineAsync(TextBaseLine.Middle);
            await context.TextAlignAsync(TextAlign.Center);
            await context.FillTextAsyncVector(text, v2);
        }

        /* Sets the shared text state once, then emits only the per label draw calls. */
        public static async Task WriteTexts(this IContext2DWithoutGetters context, string font, string style,
            IReadOnlyList<(string Text, Vector2 Position)> labels) {
            if (labels.Count == 0)
                return;

            await context.FontAsync(font);
            await context.FillStyleAsync(style);
            await context.TextBaseLineAsync(TextBaseLine.Middle);
            await context.TextAlignAsync(TextAlign.Center);

            foreach (var (text, position) in labels)
                await context.FillTextAsync(text, position.X, position.Y);
        }
    }
}
