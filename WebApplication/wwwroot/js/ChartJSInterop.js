export function LineChart(canvasid, chartInfo) {
    console.log(chartInfo);
    var lineChart = new Chart(canvasid, {
        type: "line",
        data: {
            labels: chartInfo.xAxis.values,
            datasets: [{
                lineTension: 0,
                label: chartInfo.yAxis[0].title,
                data: chartInfo.yAxis[0].values,
                pointBackgroundColor: chartInfo.yAxis[0].colors
            }]
        },
        options: {
            responsive: false,
            animation: {
                duration: 0
            },
            pan: {
                enabled: false
            },
            zoom: {
                enabled: false
            }
        }
    });
}