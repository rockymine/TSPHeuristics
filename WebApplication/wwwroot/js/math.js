/* Typesets every [data-tex] element inside a container with KaTeX. One interop call per card
 * instead of one per formula; an element is only re-typeset when its TeX changed. */
window.tspMath = {
    render(container) {
        if (!container || !window.katex)
            return;

        for (const el of container.querySelectorAll("[data-tex]")) {
            const tex = el.getAttribute("data-tex") || "";
            if (el.dataset.rendered === tex)
                continue;

            katex.render(tex, el, { throwOnError: false, displayMode: false });
            el.dataset.rendered = tex;
        }
    }
};
