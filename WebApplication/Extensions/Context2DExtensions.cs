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
    public static class Context2DExtensions {
        public static async Task DrawLine(this Context2D context, Brush brush, Vector2 pos1, Vector2 pos2) {
            if (brush.Color == "red") {
                await context.SetLineDashAsync(new double[] { 5 });
            } else {
                await context.SetLineDashAsync(new double[] { });
            }                

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
            await context.BeginPathAsync();
            await context.MoveToAsync(pos.X, pos.Y);
            await context.EllipseAsync(pos.X, pos.Y, radius, radius, 0, 0, 360);
            await context.FillAsync(FillRule.EvenOdd);
            //if (brush.Style == FillStyle.Fill)
            //    await context.FillAsync(FillRule.EvenOdd);
            //else if (brush.Style == FillStyle.Stroke)
            //    await context.StrokeAsync();
        }

        public static async Task DrawTextBox(this Context2D context, Brush brush, Vector2 pos, string text) {
            var metrics = await context.MeasureTextAsync(text);
            var width = metrics.Width;
            var height = metrics.FontBoundingBoxAscent + metrics.FontBoundingBoxDescent;
            var x = pos.X;
            var y = pos.Y;
            var startX = x - (width / 2);
            var startY = y - (height / 2);
            
            await context.FillStyleAsync("#3f74a8");
            await context.LineWidthAsync(2);
            await context.BeginPathAsync();
            await context.MoveToAsync(x, y);
            await context.FillRectAsync(startX, startY, width, height);
            await WriteText(context, brush.TextFont, "white", text, pos);
        }

        public static async Task FillTextAsyncVector(this Context2D context, string text, Vector2 pos) {
            await context.FillTextAsync(text, pos.X, pos.Y);
        }

        public static async Task WriteText(this Context2D context, string font, string style, string text, Vector2 v2) {
            await context.FontAsync(font);
            await context.FillStyleAsync(style);
            await context.TextBaseLineAsync(TextBaseLine.Middle);
            await context.TextAlignAsync(TextAlign.Center);
            await context.FillTextAsyncVector(text, v2);
        }
    }
}