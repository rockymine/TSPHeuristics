using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace WebApplication.Extensions {
    public static class Vector2Extensions {
        public static Vector2 InverseY(this Vector2 v2, float yHeight) => new(v2.X, yHeight - v2.Y);
        public static Vector2 ScaleOffsetInverse(this Vector2 v2, float scale, Vector2 offset, float yHeight) {
            var vector = v2 * scale + offset;
            vector.InverseY(yHeight);
            return vector;
        }
    }
}