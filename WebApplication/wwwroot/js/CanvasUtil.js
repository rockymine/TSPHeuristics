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