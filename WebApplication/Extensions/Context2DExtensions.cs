using Excubo.Blazor.Canvas;
using Excubo.Blazor.Canvas.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using WebApplication.Utils;

namespace WebApplication.Extensions {
    public static class Context2DExtensions {
        public static async Task DrawLine(this Context2D context, Brush brush, Vector2 pos1, Vector2 pos2) {
            await context.StrokeStyleAsync(brush.Color);
            await context.LineWidthAsync(brush.Width);
            await context.BeginPathAsync();
            await context.MoveToAsync(pos1.X, pos1.Y);
            await context.LineToAsync(pos2.X, pos2.Y);
            await context.StrokeAsync();
        }

        public static async Task DrawCircle(this Context2D context, Brush brush, double radius, Vector2 pos) {
            await context.FillStyleAsync(brush.Color);
            await context.LineWidthAsync(brush.Width);
            await context.MoveToAsync(pos.X, pos.Y);
            await context.EllipseAsync(pos.X, pos.Y, radius, radius, 0, 0, 360);

            if (brush.Style == FillStyle.Fill)
                await context.FillAsync(FillRule.EvenOdd);
            else if (brush.Style == FillStyle.Stroke)
                await context.StrokeAsync();
        }

        public static async Task DrawTextBox(this Context2D context, Brush brush, Vector2 pos) {
            await context.FillStyleAsync(brush.Color);
            await context.LineWidthAsync(brush.Width);
            await context.MoveToAsync(pos.X - 0.2, pos.X - 0.2);
            await context.RectAsync(pos.X - 0.2, pos.Y - 0.2, 1, 1);
            //await context.FillRectAsync(min1, min2, 0.4, 0.4);

            if (brush.Style == FillStyle.Fill)
                await context.FillAsync(FillRule.EvenOdd);
            else if (brush.Style == FillStyle.Stroke)
                await context.StrokeAsync();
        }

        public static async Task FillTextAsyncVector(this Context2D context, string text, Vector2 pos) {
            await context.FillTextAsync(text, pos.X, pos.Y);
        }

        public static async Task WriteText(this Context2D context, string font, string style, string text, Vector2 v2) {
            await context.FontAsync(font);
            await context.FillStyleAsync(style);
            await context.TextBaseLineAsync(TextBaseLine.Middle);
            await context.TextAlignAsync(TextAlign.Left);
            await context.FillTextAsyncVector(text, v2);
        }
    }
}