window.addEventListener("resize", SetCanvases);

function SetCanvases(e) {
    var canvases = document.getElementsByClassName("CanvasFullSize");
    for (var i = 0; i < canvases.length; i++) {
        var canvas = canvases[i];
        canvas.width = canvas.parentElement.offsetWidth;
        canvas.height = canvas.parentElement.offsetHeight;
    }
}

function UpdateCanvases() {
    SetCanvases(null);
}

/* How many device pixels the display puts behind one CSS pixel. The canvas buffer has to be sized
   in device pixels or the browser enlarges a smaller bitmap to fit, which is what made the drawing
   soft on HiDPI screens. Clamping is left to the caller, which weighs it against its memory cap. */
function GetDevicePixelRatio() {
    return window.devicePixelRatio || 1;
}

/* The canvas fits itself to the stage, so the stage has to be measured - a Blazor component cannot
   read a layout box on its own. A ResizeObserver covers everything that changes it: the window,
   the dock being collapsed or resized, the sidebar's scrollbar appearing.

   Observing .canvas-area rather than the canvas itself is deliberate. The area is flex: 1 inside
   the workspace, so its size comes from the topbar, run bar, toolbar and dock - never from the
   canvas it contains. Observing the canvas would be a feedback loop. */
let canvasStageObserver = null;

function MeasureCanvasStage(el) {
    const cs = getComputedStyle(el);
    return {
        width: el.clientWidth - parseFloat(cs.paddingLeft) - parseFloat(cs.paddingRight),
        height: el.clientHeight - parseFloat(cs.paddingTop) - parseFloat(cs.paddingBottom)
    };
}

function ObserveCanvasStage(dotNetRef) {
    const el = document.querySelector(".canvas-area");
    if (!el)
        return null;

    if (canvasStageObserver)
        canvasStageObserver.disconnect();

    canvasStageObserver = new ResizeObserver(function () {
        const size = MeasureCanvasStage(el);
        dotNetRef.invokeMethodAsync("OnCanvasStageResized", size.width, size.height);
    });
    canvasStageObserver.observe(el);

    return MeasureCanvasStage(el);
}
/* Theme selection. The value is read back by the toggle so the button can show the right icon,
   and applied to <html> where the token overrides in tokens.css hang off it. */
function GetTheme() {
    return document.documentElement.getAttribute("data-theme") || "dark";
}

function SetTheme(theme) {
    document.documentElement.setAttribute("data-theme", theme);
    try { localStorage.setItem("tsp-theme", theme); } catch (e) { /* private mode: not fatal */ }
    return theme;
}

/* The canvas is painted from C#, so it cannot inherit a colour the way an element does - the
   values have to be read out of the stylesheet and handed over. Resolved against <html>, so they
   already reflect whichever data-theme is active. */
function GetCanvasColors() {
    const s = getComputedStyle(document.documentElement);
    const read = name => s.getPropertyValue(name).trim();

    return {
        background: read("--canvas-bg"),
        grid: read("--canvas-grid"),
        edge: read("--canvas-edge"),
        node: read("--canvas-node"),
        nodeText: read("--canvas-node-text"),
        added: read("--canvas-added"),
        removed: read("--canvas-removed"),
        nodeI: read("--canvas-node-i"),
        nodeJ: read("--canvas-node-j"),
        labelBackground: read("--canvas-label-bg"),
        labelText: read("--canvas-label-text")
    };
}
