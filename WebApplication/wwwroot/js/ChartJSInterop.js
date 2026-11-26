/* One Chart per canvas, kept across calls.
 *
 * This used to construct a new Chart on every call, and it is called on every history step - so a
 * thousand-iteration annealing run played back left a thousand charts stacked on the same canvas,
 * each holding its own copy of the data and its own resize and hover listeners. Hovering made the
 * stale ones repaint over the current one. It also logged the whole ChartInfo to the console every
 * frame. The chart is now created once and its data replaced in place.
 *
 * A canvas that has been removed from the document (the algorithm was switched, and the chart's
 * markup with it) is forgotten instead of being handed to Chart.js, which cannot acquire a context
 * from a detached or missing element. */
const charts = new Map();

/* Colours come from the theme tokens (theme.js); Chart.js 2 defaults to #666 text and near-black
 * grid lines, which disappear on a dark panel. A theme switch re-colours every live chart. */
const colors = () => window.tspTheme ? window.tspTheme.chartColors()
    : { text: "#a1a1aa", grid: "#2e2e35", line: "#f87171", fill: "transparent", cursor: "#60a5fa" };

/* A chart shared by every state of a run (Simulated Annealing) marks the state's point instead of
 * carrying per-state colours: a vertical line and a dot at the cursor, painted after the data by
 * the plugin below. Moving the cursor is then a redraw, not an update of every point - which for a
 * thousand-point schedule was most of a playback step. Without a cursor the colours the data
 * brings are used, as before. */
function hasCursor(chart) {
    return typeof chart.$cursor === "number" && chart.$cursor >= 0;
}

function stylePoints(chart, values, colorsFromData) {
    const dataset = chart.data.datasets[0];
    if (!hasCursor(chart)) {
        dataset.pointBackgroundColor = colorsFromData;
        dataset.pointRadius = 3;
        return;
    }

    const c = colors();
    dataset.pointBackgroundColor = c.line;
    dataset.pointBorderColor = c.line;
    dataset.pointRadius = values.length > 60 ? 0 : 2;
}

const cursorPlugin = {
    afterDatasetsDraw(chart) {
        if (!hasCursor(chart))
            return;

        const point = chart.getDatasetMeta(0).data[chart.$cursor];
        if (!point)
            return;

        const { x, y } = point._model;
        const area = chart.chartArea;
        const ctx = chart.ctx;
        const color = colors().cursor;

        ctx.save();
        ctx.strokeStyle = color;
        ctx.globalAlpha = 0.5;
        ctx.lineWidth = 1;
        ctx.setLineDash([4, 3]);
        ctx.beginPath();
        ctx.moveTo(x, area.top);
        ctx.lineTo(x, area.bottom);
        ctx.stroke();
        ctx.globalAlpha = 1;
        ctx.setLineDash([]);
        ctx.fillStyle = color;
        ctx.beginPath();
        ctx.arc(x, y, 4.5, 0, 2 * Math.PI);
        ctx.fill();
        ctx.restore();
    }
};

function axis(title, c) {
    return {
        scaleLabel: { display: !!title, labelString: title, fontColor: c.text },
        ticks: { fontColor: c.text },
        gridLines: { color: c.grid, zeroLineColor: c.grid }
    };
}

function applyColors(chart) {
    const c = colors();
    const options = chart.options;
    options.legend.labels.fontColor = c.text;
    for (const scale of [...options.scales.xAxes, ...options.scales.yAxes]) {
        scale.scaleLabel.fontColor = c.text;
        scale.ticks.fontColor = c.text;
        scale.gridLines.color = c.grid;
        scale.gridLines.zeroLineColor = c.grid;
    }
    chart.data.datasets[0].borderColor = c.line;
    chart.data.datasets[0].backgroundColor = c.fill;
    if (chart.$cursor >= 0)
        stylePoints(chart, chart.data.datasets[0].data, null);
}

window.addEventListener("tsp-themechange", () => {
    for (const chart of charts.values()) {
        applyColors(chart);
        chart.update(0);
    }
});

/* Moves the cursor of a chart whose data has not changed - one call per playback step, without
 * sending the run's data again. */
export function SetCursor(canvasid, cursor) {
    const chart = charts.get(canvasid);
    if (!chart || chart.canvas !== document.getElementById(canvasid))
        return false;

    chart.$cursor = cursor;
    chart.draw();
    return true;
}

export function LineChart(canvasid, chartInfo, cursor) {
    const canvas = document.getElementById(canvasid);
    let chart = charts.get(canvasid);

    if (chart && chart.canvas !== canvas) {
        chart.destroy();
        charts.delete(canvasid);
        chart = undefined;
    }

    if (!canvas)
        return;

    const yAxis = chartInfo.yAxis[0];

    if (chart) {
        chart.data.labels = chartInfo.xAxis.values;
        chart.data.datasets[0].label = yAxis.title;
        chart.data.datasets[0].data = yAxis.values;
        chart.$cursor = cursor;
        stylePoints(chart, yAxis.values, yAxis.colors);
        chart.update(0);
        return;
    }

    const c = colors();
    chart = new Chart(canvas, {
        type: "line",
        plugins: [cursorPlugin],
        data: {
            labels: chartInfo.xAxis.values,
            datasets: [{
                lineTension: 0,
                label: yAxis.title,
                data: yAxis.values,
                pointBackgroundColor: yAxis.colors,
                borderColor: c.line,
                backgroundColor: c.fill
            }]
        },
        /* Responsive, because the charts live in a dock tab that may be hidden when they are
         * created: a fixed-size chart measured inside a hidden pane comes out 300 x 150. Chart.js
         * resizes a responsive one to its box as soon as the pane is shown. */
        options: {
            responsive: true,
            maintainAspectRatio: true,
            aspectRatio: 2.4,
            animation: { duration: 0 },
            legend: { labels: { fontColor: c.text } },
            scales: {
                xAxes: [axis(chartInfo.xAxis.title, c)],
                yAxes: [axis(null, c)]
            }
        }
    });

    chart.$cursor = cursor;
    stylePoints(chart, yAxis.values, yAxis.colors);
    chart.update(0);
    charts.set(canvasid, chart);
}
