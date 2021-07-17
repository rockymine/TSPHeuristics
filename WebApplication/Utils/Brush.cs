using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.Utils {
    public class Brush {
        public string Color { get; set; }
        public double Width { get; set; }
        public string TextFont { get; set; }
        public string TextStyle { get; set; }
        public FillStyle Style { get; set; }

        public Brush Copy() {
            return new Brush {
                Color = new string(Color),
                Width = Width,
                TextFont = new string(TextFont),
                TextStyle = new string(TextStyle),
                Style = Style
            };
        }
    }

    public enum FillStyle {
        Stroke,
        Fill
    }    
}