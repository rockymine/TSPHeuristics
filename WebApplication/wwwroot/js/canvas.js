/* Reports the box the canvases have to fit, so the page can size them to it (CanvasSettings.Fit).
 *
 * The canvases used to be a fixed number of pixels per coordinate unit, drawn at one buffer pixel
 * per CSS pixel, and the browser scaled that to the screen - soft on any display with a device
 * pixel ratio above 1, and small in a wide column. The width comes from the frame around the
 * current canvas (its content box), the height limit from the window, and the pixel ratio from the
 * device; a resize, a zoom or moving the window to another screen reports again. */
window.tspCanvas = {
    /* Paints one frame (CanvasScene in Utils/CanvasScene.cs) at a point t of its step animation,
     * 0 to 1, or at rest when t is undefined. Every coordinate is in CSS pixels; the transform maps
     * them onto the buffer, which is ratio times larger. */
    paint(canvas, scene, t) {
        const ctx = canvas.getContext("2d");
        const anim = typeof t === "number";
        const clamp = v => Math.max(0, Math.min(1, v));
        const ease = v => v < .5 ? 4 * v * v * v : 1 - Math.pow(-2 * v + 2, 3) / 2;

        ctx.setTransform(scene.ratio, 0, 0, scene.ratio, 0, 0);
        ctx.globalAlpha = 1;
        ctx.clearRect(0, 0, scene.width, scene.height);
        ctx.fillStyle = scene.background;
        ctx.fillRect(0, 0, scene.width, scene.height);

        for (const op of scene.ops) {
            const p = op.points;
            ctx.globalAlpha = 1;
            switch (op.kind) {
                case "lines": {
                    /* How far each segment is drawn (f), and how strongly. */
                    let f = 1, width = op.width, color = op.color;
                    if (op.anim === "grow") {
                        f = anim ? ease(clamp((t - .12) / .6)) : 1;
                    } else if (op.anim === "fade") {
                        ctx.globalAlpha = anim ? (t < .35 ? 1 : 1 - .65 * clamp((t - .35) / .65)) : .35;
                    } else if (op.anim === "candidate") {
                        if (anim) {
                            f = t < .45 ? ease(t / .45) : t < .6 ? 1 : 1 - ease((t - .6) / .4);
                            ctx.globalAlpha = .95;
                        } else {
                            ctx.globalAlpha = .5;
                            width = Math.min(width, 1.6);
                        }
                    } else if (op.anim === "flash") {
                        if (anim && t > .15 && t < .75)
                            color = op.fill;
                    }
                    if (f <= 0.01)
                        break;

                    ctx.setLineDash(op.dashed ? [7, 5] : []);
                    ctx.strokeStyle = color;
                    ctx.lineWidth = width;
                    ctx.lineCap = op.anim === "grow" ? "round" : "butt";
                    ctx.beginPath();
                    for (let i = 0; i < p.length; i += 4) {
                        ctx.moveTo(p[i], p[i + 1]);
                        ctx.lineTo(p[i] + (p[i + 2] - p[i]) * f, p[i + 1] + (p[i + 3] - p[i + 1]) * f);
                    }
                    ctx.stroke();
                    ctx.setLineDash([]);
                    break;
                }

                /* Widths moving from w0 to w1: each segment is its own stroke. */
                case "widths": {
                    const e = anim ? ease(t) : 1;
                    ctx.globalAlpha = op.alpha;
                    ctx.strokeStyle = op.color;
                    ctx.lineCap = "round";
                    for (let i = 0, s = 0; i < p.length; i += 4, s++) {
                        ctx.lineWidth = op.w0[s] + (op.w1[s] - op.w0[s]) * e;
                        ctx.beginPath();
                        ctx.moveTo(p[i], p[i + 1]);
                        ctx.lineTo(p[i + 2], p[i + 3]);
                        ctx.stroke();
                    }
                    break;
                }

                /* The halo is the outer half of a stroke along each circle; the fill covers the
                 * inner half. Nonzero fill, so a node listed twice (a closed tour's start) is
                 * still filled rather than cancelled out. */
                case "circles": {
                    const r = op.anim === "pulse" && anim ? op.radius * (1 + .4 * Math.sin(Math.PI * clamp(t * 1.2))) : op.radius;
                    ctx.beginPath();
                    for (let i = 0; i < p.length; i += 2) {
                        ctx.moveTo(p[i] + r, p[i + 1]);
                        ctx.arc(p[i], p[i + 1], r, 0, 2 * Math.PI);
                    }
                    if (op.halo && op.width > 0) {
                        ctx.strokeStyle = op.halo;
                        ctx.lineWidth = op.width * 2;
                        ctx.stroke();
                    }
                    ctx.fillStyle = op.color;
                    ctx.fill("nonzero");
                    break;
                }

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
        ctx.globalAlpha = 1;
    },

    /* A bare frame at rest: the empty canvas before a run. */
    draw(id, json) {
        const canvas = document.getElementById(id);
        if (!canvas)
            return;
        this.steps[id] = null;
        this.paint(canvas, JSON.parse(json));
    },

    /* The step on show per canvas: { scene, before, duration }. Kept here so Replay and holding B
     * work without a round trip to .NET. */
    steps: {},
    timers: {},

    /* Shows a step, playing its change for duration ms (0, or reduced motion: no animation). */
    show(id, json) {
        const step = JSON.parse(json);
        this.steps[id] = step;
        this.animate(id, step.duration);
    },

    animate(id, duration) {
        const canvas = document.getElementById(id), step = this.steps[id];
        cancelAnimationFrame(this.timers[id]);
        if (!canvas || !step)
            return;

        const reduce = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        if (!duration || reduce) {
            this.paint(canvas, step.scene);
            return;
        }

        const start = performance.now();
        const frame = now => {
            const t = Math.min(1, (now - start) / duration);
            if (t < 1) {
                this.paint(canvas, step.scene, t);
                this.timers[id] = requestAnimationFrame(frame);
            } else {
                this.paint(canvas, step.scene);
            }
        };
        this.timers[id] = requestAnimationFrame(frame);
    },

    /* Replay step: the change again, a little slower than playback, as often as wanted. */
    replay(id) {
        if (this.steps[id]?.before)
            this.animate(id, 650);
    },

    /* While held (the Replay button or B), the tour before the step. */
    hold(id, on) {
        const canvas = document.getElementById(id), step = this.steps[id];
        if (!canvas || !step?.before)
            return;
        cancelAnimationFrame(this.timers[id]);
        this.paint(canvas, on ? step.before : step.scene);
    },

    /* Replay and hold for the button #replayStep and the keys R and B, for the canvas the page
     * shows. A click replays; pressing longer than 250 ms holds the tour before until release. */
    bindReplay(id) {
        if (this.replayBound)
            return;
        this.replayBound = true;

        let timer = null, held = false;
        const typing = e => e.target.closest && e.target.closest("input, textarea, select, [contenteditable]");
        document.addEventListener("pointerdown", e => {
            if (!e.target.closest || !e.target.closest("#replayStep") || e.target.closest("#replayStep").disabled)
                return;
            held = false;
            timer = setTimeout(() => { held = true; this.hold(id, true); }, 250);
        });
        const release = e => {
            if (timer === null)
                return;
            clearTimeout(timer);
            timer = null;
            if (held) {
                this.hold(id, false);
            } else if (e.type === "pointerup" && e.target.closest && e.target.closest("#replayStep")) {
                this.replay(id);
            }
            held = false;
        };
        document.addEventListener("pointerup", release);
        document.addEventListener("pointercancel", release);
        /* Keyboard activation of the button (Enter, Space) arrives as a click without a pointer. */
        document.addEventListener("click", e => {
            if (e.detail === 0 && e.target.closest && e.target.closest("#replayStep"))
                this.replay(id);
        });
        document.addEventListener("keydown", e => {
            if (typing(e) || e.ctrlKey || e.metaKey || e.altKey)
                return;
            if ((e.key === "b" || e.key === "B") && !e.repeat)
                this.hold(id, true);
            else if (e.key === "r" || e.key === "R")
                this.replay(id);
        });
        document.addEventListener("keyup", e => {
            if (e.key === "b" || e.key === "B")
                this.hold(id, false);
        });
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
