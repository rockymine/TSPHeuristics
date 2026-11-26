/* The theme switch, and the bridge that lets the canvas and the charts follow it.
 *
 * CSS styles the page, but the graph is drawn on a <canvas> and the charts by Chart.js, and neither
 * reads CSS. Both take their colours from the --canvas-* and --chart-* tokens through this file
 * instead, so tokens.css stays the one place a colour is defined. */
window.tspTheme = (function () {
    const root = document.documentElement;
    const css = name => getComputedStyle(root).getPropertyValue(name).trim();

    return {
        get() {
            return root.getAttribute("data-theme") === "light" ? "light" : "dark";
        },

        /* Stores the choice and tells the charts. The canvas is redrawn by the page, which asks for
         * canvasColors() again afterwards. */
        set(theme) {
            root.setAttribute("data-theme", theme === "light" ? "light" : "dark");
            try { localStorage.setItem("tsp-theme", this.get()); } catch (e) { }
            window.dispatchEvent(new CustomEvent("tsp-themechange"));
            return this.get();
        },

        /* "small", "medium" or "large", on <html> as data-text-size; tokens.css scales the type. */
        getTextSize() {
            const s = root.getAttribute("data-text-size");
            return s === "small" || s === "large" ? s : "medium";
        },

        setTextSize(size) {
            if (size === "small" || size === "large")
                root.setAttribute("data-text-size", size);
            else
                root.removeAttribute("data-text-size");
            try { localStorage.setItem("tsp-text-size", this.getTextSize()); } catch (e) { }
            window.dispatchEvent(new CustomEvent("tsp-themechange"));
            return this.getTextSize();
        },

        canvasColors() {
            return {
                background: css("--canvas-bg"),
                grid: css("--canvas-grid"),
                edge: css("--canvas-edge"),
                node: css("--canvas-node"),
                nodeLabel: css("--canvas-node-label"),
                added: css("--canvas-added"),
                removed: css("--canvas-removed"),
                moveFirst: css("--canvas-move-first"),
                moveSecond: css("--canvas-move-second"),
                labelBackground: css("--canvas-label-bg"),
                labelText: css("--canvas-label-text"),
                pheromone: css("--canvas-pheromone"),
                labelSize: parseFloat(css("--canvas-label-size")) || 11
            };
        },

        chartColors() {
            return {
                text: css("--text-secondary"),
                grid: css("--border"),
                line: css("--chart-line"),
                fill: css("--chart-fill"),
                cursor: css("--accent-text")
            };
        }
    };
})();
