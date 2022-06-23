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