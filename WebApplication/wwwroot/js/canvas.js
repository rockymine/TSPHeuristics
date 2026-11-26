/* Reports the box the canvases have to fit, so the page can size them to it (CanvasSettings.Fit).
 *
 * The canvases used to be a fixed number of pixels per coordinate unit, drawn at one buffer pixel
 * per CSS pixel, and the browser scaled that to the screen - soft on any display with a device
 * pixel ratio above 1, and small in a wide column. The width comes from the frame around the
 * current canvas (its content box), the height limit from the window, and the pixel ratio from the
 * device; a resize, a zoom or moving the window to another screen reports again. */
window.tspCanvas = {
    /* Paints one frame (CanvasScene in Utils/CanvasScene.cs): every coordinate is in CSS pixels,
     * the transform maps them onto the buffer, which is ratio times larger. A canvas that is not
     * in the document (the mode has no "Previous" canvas) is skipped. With no ops the canvas is
     * only cleared to the background. */
    draw(id, json) {
        const canvas = document.getElementById(id);
        if (!canvas)
            return;

        const scene = JSON.parse(json);

        const ctx = canvas.getContext("2d");
        ctx.setTransform(scene.ratio, 0, 0, scene.ratio, 0, 0);
        ctx.clearRect(0, 0, scene.width, scene.height);
        ctx.fillStyle = scene.background;
        ctx.fillRect(0, 0, scene.width, scene.height);

        for (const op of scene.ops) {
            const p = op.points;
            switch (op.kind) {
                case "lines":
                    ctx.setLineDash(op.dashed ? [5] : []);
                    ctx.strokeStyle = op.color;
                    ctx.lineWidth = op.width;
                    ctx.beginPath();
                    for (let i = 0; i < p.length; i += 4) {
                        ctx.moveTo(p[i], p[i + 1]);
                        ctx.lineTo(p[i + 2], p[i + 3]);
                    }
                    ctx.stroke();
                    ctx.setLineDash([]);
                    break;

                /* The halo is the outer half of a stroke along each circle; the fill covers the
                 * inner half. Nonzero fill, so a node listed twice (a closed tour's start) is
                 * still filled rather than cancelled out. */
                case "circles":
                    ctx.beginPath();
                    for (let i = 0; i < p.length; i += 2) {
                        ctx.moveTo(p[i] + op.radius, p[i + 1]);
                        ctx.arc(p[i], p[i + 1], op.radius, 0, 2 * Math.PI);
                    }
                    if (op.halo && op.width > 0) {
                        ctx.strokeStyle = op.halo;
                        ctx.lineWidth = op.width * 2;
                        ctx.stroke();
                    }
                    ctx.fillStyle = op.color;
                    ctx.fill("nonzero");
                    break;

                case "texts":
                    ctx.font = op.font;
                    ctx.fillStyle = op.color;
                    ctx.textAlign = "center";
                    ctx.textBaseline = "middle";
                    for (let i = 0; i < op.texts.length; i++)
                        ctx.fillText(op.texts[i], p[i * 2], p[i * 2 + 1]);
                    break;

                /* Pills as tall as the line, padded by the line height rather than by a share
                 * of the text width, so a single digit still has room. */
                case "boxes":
                    ctx.font = op.font;
                    ctx.textAlign = "center";
                    ctx.textBaseline = "middle";
                    for (let i = 0; i < op.texts.length; i++) {
                        const m = ctx.measureText(op.texts[i]);
                        const h = (m.fontBoundingBoxAscent + m.fontBoundingBoxDescent) || parseFloat(op.font) * 1.2;
                        const w = Math.max(m.width + h * 0.7, h * 1.4);
                        const x = p[i * 2], y = p[i * 2 + 1];
                        ctx.fillStyle = op.fill;
                        ctx.beginPath();
                        ctx.roundRect(x - w / 2, y - h / 2, w, h, h / 2);
                        ctx.fill();
                        ctx.fillStyle = op.color;
                        ctx.fillText(op.texts[i], x, y);
                    }
                    break;
            }
        }
    },

    observe(frame, page) {
        let pending = false;
        let last = "";

        const report = () => {
            pending = false;
            const style = getComputedStyle(frame);
            const width = frame.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight);
            const box = [Math.floor(width), window.innerHeight, window.devicePixelRatio || 1];
            const key = box.join(",");
            if (width <= 0 || key === last)
                return;

            last = key;
            page.invokeMethodAsync("OnCanvasBox", box[0], box[1], box[2]);
        };

        /* One report per frame, however many observers fire in it. */
        const schedule = () => {
            if (pending)
                return;
            pending = true;
            requestAnimationFrame(report);
        };

        new ResizeObserver(schedule).observe(frame);
        window.addEventListener("resize", schedule);
        schedule();
    }
};
