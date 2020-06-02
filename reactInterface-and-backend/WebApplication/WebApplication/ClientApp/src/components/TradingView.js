import React, { useState, useEffect } from 'react';
import { createChart } from 'lightweight-charts';


const colorsMap = {
    up: 'rgba(0, 150, 136, 0.8)',
    down: 'rgba(255,82,82, 0.8)'
};


export function TradingViewChartWrapper({ data, size, ...props }) {

    let chartData = {};
    if (data) {
        chartData = { Portfolio: data };
    }

    return (
        <div>
            <TradingViewChart seriesData={chartData} size={size} UseBar={true} /> 
        </div>
    );

}


function TradingViewChart({ seriesData, size, UseBar, ...props }) {

    const [data, setData] = useState({});
    const useVolume = false;

    //Create two effect, one that does all the data stuff and then sets a data state
    //Another that takes the data state and creates the chart
    //Hopefully this would allow me to cache the data
    //ALWAYS include volume
    useEffect(() => {
        if (!Boolean(seriesData))
            return;
        let tempData = {};
        let keys = Object.keys(seriesData);
        keys.forEach(key => {
            let currData = seriesData[key];
            let sData = [];
            let vData = [];
            let previous = 0;
            currData.forEach(ele => {
                const { time, open, high, low, close, adjClose } = ele;
                sData.push({ time: time, value: adjClose, open: open, high: high, low: low, close: close });
                //let color = (adjClose >= previous) ? colorsMap.up : colorsMap.down;
                previous = adjClose;
                //vData.push({ time: time, value: volume, color: color });
            });
            tempData[key] = { series: sData, volume: vData };
        });
        setData(tempData);

    }, [seriesData]);

    useEffect(() => {
        let chart;
        if (data) {
            let chartDiv = document.getElementById("chart-div");
            if (chartDiv === null)
                return;
            chart = createChart(chartDiv, {
                width: size.w,
                height: size.h
            });
            if (chart === null) {
                return;
            }
            let keys = Object.keys(data);
            keys.forEach(key => {
                let { series, volume } = data[key];
                let newSeries = UseBar ? chart.addBarSeries({ title: key, thinBars: false }) : chart.addLineSeries({ title: key });
                if (useVolume) {
                    let newVolume = chart.addHistogramSeries({
                        lineWidth: 2,
                        priceFormat: { type: "volume" },
                        overlay: true,
                        scaleMargins: {
                            top: 0.8,
                            bottom: 0,
                        }
                    });
                    newVolume.setData(volume);
                }

                newSeries.setData(series);
            });
        }
        return () => {
            if (chart !== null) {
                chart.remove();
                chart = null;
            }
        }
    }, [data, UseBar, size, useVolume]);


    return (
        <div>
            <div id="chart-div"/>
        </div>
    );
}