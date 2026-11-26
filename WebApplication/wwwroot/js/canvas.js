/* Reports the box the canvases have to fit, so the page can size them to it (CanvasSettings.Fit).
 *
 * The canvases used to be a fixed number of pixels per coordinate unit, drawn at one buffer pixel
 * per CSS pixel, and the browser scaled that to the screen - soft on any display with a device
 * pixel ratio above 1, and small in a wide column. The width comes from the frame around the
 * current canvas (its content box), the height limit from the window, and the pixel ratio from the
 * device; a resize, a zoom or moving the window to another screen reports again. */
window.tspCanvas = {
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
