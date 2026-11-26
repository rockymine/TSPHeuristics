namespace WebApplication.Utils {
    /* The colours the graph is drawn in. They are read from the --canvas-* tokens in tokens.css
     * (theme.js, canvasColors) whenever the theme is set, so the drawing follows the page's theme
     * and a colour is defined in one place. The defaults are the light theme's, for the frames drawn
     * before the first read comes back. */
    public class CanvasTheme {
        public string Background { get; set; } = "#ffffff";
        public string Grid { get; set; } = "#dcdce1";
        public string Edge { get; set; } = "#18181b";
        public string Node { get; set; } = "#4e5072";
        public string NodeLabel { get; set; } = "#ffffff";
        public string Added { get; set; } = "#16a34a";
        public string Removed { get; set; } = "#dc2626";
        public string MoveFirst { get; set; } = "#9333ea";
        public string MoveSecond { get; set; } = "#ea8a00";
        public string LabelBackground { get; set; } = "#3f74a8";
        public string LabelText { get; set; } = "#ffffff";
    }
}
