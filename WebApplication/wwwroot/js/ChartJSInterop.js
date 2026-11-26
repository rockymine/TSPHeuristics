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
    : { text: "#a1a1aa", grid: "#2e2e35", line: "#f87171", fill: "transparent" };

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
}

window.addEventListener("tsp-themechange", () => {
    for (const chart of charts.values()) {
        applyColors(chart);
        chart.update(0);
    }
});

export function LineChart(canvasid, chartInfo) {
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
        chart.data.datasets[0].pointBackgroundColor = yAxis.colors;
        chart.update(0);
        return;
    }

    const c = colors();
    chart = new Chart(canvas, {
        type: "line",
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

    charts.set(canvasid, chart);
}
