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

/* The page uses the dark Bootstrap theme; Chart.js 2 defaults to #666 text and near-black grid
 * lines, which left the axes all but invisible on the #333 panels. */
const textColor = "#c8c8c8";
const gridColor = "rgba(255, 255, 255, 0.12)";

function axis(title) {
    return {
        scaleLabel: { display: !!title, labelString: title, fontColor: textColor },
        ticks: { fontColor: textColor },
        gridLines: { color: gridColor, zeroLineColor: gridColor }
    };
}

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

    chart = new Chart(canvas, {
        type: "line",
        data: {
            labels: chartInfo.xAxis.values,
            datasets: [{
                lineTension: 0,
                label: yAxis.title,
                data: yAxis.values,
                pointBackgroundColor: yAxis.colors,
                borderColor: "rgba(220, 53, 69, 0.8)",
                backgroundColor: "rgba(220, 53, 69, 0.08)"
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
            legend: { labels: { fontColor: textColor } },
            scales: {
                xAxes: [axis(chartInfo.xAxis.title)],
                yAxes: [axis(null)]
            }
        }
    });

    charts.set(canvasid, chart);
}
