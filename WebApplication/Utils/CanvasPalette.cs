namespace WebApplication.Utils {
    /* The colours the canvas is drawn with, read from the CSS custom properties on the document.
     *
     * An element inherits a colour from the stylesheet; a canvas does not, because its contents are
     * painted rather than laid out. The values are resolved in JS against the document element, so
     * they already reflect whichever theme is active, and handed over. Each field defaults to a
     * usable colour, so a failed or absent read still yields a legible drawing. */
    public class CanvasPalette {
        public string Background { get; set; } = "#FFFFFF";
        public string Grid { get; set; } = "#999999";
        public string Edge { get; set; } = "black";
        public string Node { get; set; } = "#4e5072";
        public string NodeText { get; set; } = "white";
        public string Added { get; set; } = "green";
        public string Removed { get; set; } = "red";
        public string NodeI { get; set; } = "purple";
        public string NodeJ { get; set; } = "orange";
        public string LabelBackground { get; set; } = "#3f74a8";
        public string LabelText { get; set; } = "white";

        /* Ignores empty values, so a token that is missing falls back to the default. */
        public void Apply(CanvasPalette read) {
            if (read == null)
                return;

            Background = Or(read.Background, Background);
            Grid = Or(read.Grid, Grid);
            Edge = Or(read.Edge, Edge);
            Node = Or(read.Node, Node);
            NodeText = Or(read.NodeText, NodeText);
            Added = Or(read.Added, Added);
            Removed = Or(read.Removed, Removed);
            NodeI = Or(read.NodeI, NodeI);
            NodeJ = Or(read.NodeJ, NodeJ);
            LabelBackground = Or(read.LabelBackground, LabelBackground);
            LabelText = Or(read.LabelText, LabelText);
        }

        private static string Or(string value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
