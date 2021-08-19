﻿using Excubo.Blazor.Canvas.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using TravellingSalesmanProblem.Graph;
using WebApplication.Extensions;

namespace WebApplication.Utils {
    public class CanvasRenderer {
        private const float NodeSize = 8;
        private const float Scale = 20;

        private static readonly Brush EdgeBrush = new() {
            Color = "black",
            Width = 3,
            TextFont = "15px serif",
            TextStyle = "black"
        };
        private static readonly Brush NodeBrush = new() {
            Color = "#4e5072",
            Width = 2,
            Style = FillStyle.Fill,
            TextFont = "15px serif bold",
            TextStyle = "white"
        };
        private static readonly Brush GridBrush = new() { Color = "#999999", Width = 0.75 };
        private static readonly Vector2 Offset = new(Scale / 2, Scale / 2);

        public static async Task DrawGrid(Context2D context, Vector2 max, int cHeight) {
            for (int i = 0; i <= max.Y; i++) {
                await context.DrawLine(GridBrush,
                    Manipulate(new Vector2(0, i), cHeight),
                    Manipulate(new Vector2(max.X, i), cHeight));
            }
            for (int i = 0; i <= max.X; i++) {
                await context.DrawLine(GridBrush,
                    Manipulate(new Vector2(i, 0), cHeight),
                    Manipulate(new Vector2(i, max.Y), cHeight));
            }
        }

        public static async Task DrawNodes(Context2D context, List<Node> nodes, int cHeight) {
            foreach (var node in nodes) {
                await context.DrawCircle(NodeBrush, NodeSize,
                    Manipulate(node.Position, cHeight));
            }

            foreach (var node in nodes) {
                await context.WriteText(NodeBrush.TextFont, NodeBrush.TextStyle, node.Index.ToString(),
                    Manipulate(node.Position, cHeight));
            }
        }

        public static async Task DrawEdges(Context2D context, List<Edge> edges, int height) {
            var brush = EdgeBrush.Copy();
            var ants = false;

            foreach (var edge in edges) {
                if (edge.Color != null)
                    brush.Color = edge.Color;

                if (edge.Pheromone != 0) {
                    brush.Width = edge.Pheromone * 1000;
                    if (!ants)
                        ants = true;
                }
                    

                await context.DrawLine(brush,
                    Manipulate(edge.Node1.Position, height),
                    Manipulate(edge.Node2.Position, height));
                
                //await context.WriteText(brush.TextFont, brush.TextStyle, Math.Round(edge.Distance, 1).ToString(),
                //    Manipulate(edge.FindCenter(), height));
            }

            if (!ants)
                await DrawEdgeTextBox(context, edges, brush, height);
        }

        public static async Task DrawEdgeTextBox(Context2D context, List<Edge> edges, Brush brush, int height) {
            var positions = new List<Vector2>();
            foreach (var edge in edges) {
                var center = edge.FindCenter();
                //TODO: include small offsets
                if (positions.Contains(center)) {
                    center = (edge.Node1.Position + edge.FindCenter()) / 2;
                    if (positions.Contains(center)) {
                        center = (edge.Node2.Position + edge.FindCenter()) / 2;
                    }
                }

                await context.DrawTextBox(brush, Manipulate(center, height), Math.Round(edge.Distance, 1).ToString());
                positions.Add(center);
            }
        }

        private static Vector2 Manipulate(Vector2 v, int CanvasHeight) => (v * Scale + Offset).InverseY(CanvasHeight);
    }
}