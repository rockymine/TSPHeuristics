using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace WebApplication.Utils {
    public class CanvasSettings {
        public Vector2 Max { get; set; }
        public float Scale { get; set; }
        public Vector2 Offset => new(Scale / 2, Scale / 2);
        public int Height => (int)(Scale * (Max.Y + 1));
        public int Width => (int)(Scale * (Max.X + 1));
        public bool Colorize { get; set; }
        public bool Annotate { get; set; }
        public float NodeRadius { get; set; }
    }
}