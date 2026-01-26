class ChartManager {
    constructor(options) {
        this.containerId = options.containerId;
        this.symbolSelectId = options.symbolSelectId;
        this.timeframeSelectId = options.timeframeSelectId;
        this.timeRangeSelectId = options.timeRangeSelectId;
        this.autoUpdateCheckId = options.autoUpdateCheckId;
        this.showMacdId = options.showMacdId;
        this.showRsiId = options.showRsiId;
        this.showMAId = options.showMAId;
        this.showVolumeId = options.showVolumeId;
        this.defaultSymbol = options.defaultSymbol || 'SPY';
        this.defaultTimeframe = options.defaultTimeframe || '5m';
        
        this.chart = null;
        this.candlestickSeries = null;
        this.volumeSeries = null;
        this.macdSeries = null;
        this.rsiSeries = null;
        this.maSeries = null;
        this.updateInterval = null;
        this.lastCandleTime = null;
    }

    init() {
        const container = document.getElementById(this.containerId);
        if (!container) {
            console.error('Chart container not found');
            return;
        }

        // Clear loading spinner
        container.innerHTML = '';

        // Check if TradingView library is loaded
        if (typeof LightweightCharts === 'undefined') {
            console.error('TradingView Lightweight Charts library not loaded');
            container.innerHTML = '<div class="alert alert-danger m-3">Chart library failed to load. Please refresh the page.</div>';
            return;
        }

        // Verify createChart function exists
        if (typeof LightweightCharts.createChart !== 'function') {
            console.error('LightweightCharts.createChart is not a function');
            container.innerHTML = '<div class="alert alert-danger m-3">Chart library API error. Please refresh the page.</div>';
            return;
        }

        try {
            // Initialize TradingView chart
            this.chart = LightweightCharts.createChart(container, {
                width: container.clientWidth,
                height: 600,
                layout: {
                    backgroundColor: '#ffffff',
                    textColor: '#333333',
                },
                grid: {
                    vertLines: { color: '#f0f0f0' },
                    horzLines: { color: '#f0f0f0' },
                },
                crosshair: {
                    mode: LightweightCharts.CrosshairMode.Normal,
                },
                rightPriceScale: {
                    borderColor: '#cccccc',
                },
                timeScale: {
                    timeVisible: true,
                    secondsVisible: false,
                    borderColor: '#cccccc',
                },
            });

            // Verify chart was created
            if (!this.chart) {
                throw new Error('Chart object is null');
            }

            // Verify addCandlestickSeries method exists
            if (typeof this.chart.addCandlestickSeries !== 'function') {
                console.error('Chart object:', this.chart);
                console.error('Chart methods:', Object.getOwnPropertyNames(this.chart));
                console.error('Chart prototype:', Object.getPrototypeOf(this.chart));
                throw new Error('addCandlestickSeries is not a function on chart object');
            }

            // Add candlestick series
            this.candlestickSeries = this.chart.addCandlestickSeries({
                upColor: '#26a69a',
                downColor: '#ef5350',
                borderVisible: false,
                wickUpColor: '#26a69a',
                wickDownColor: '#ef5350',
            });

            if (!this.candlestickSeries) {
                throw new Error('Failed to create candlestick series');
            }
        } catch (error) {
            console.error('Error creating chart:', error);
            container.innerHTML = '<div class="alert alert-danger m-3">Error initializing chart: ' + error.message + '</div>';
            return;
        }

        // Add volume series
        if (typeof this.chart.addHistogramSeries === 'function') {
            this.volumeSeries = this.chart.addHistogramSeries({
                color: '#26a69a',
                priceFormat: {
                    type: 'volume',
                },
                priceScaleId: 'volume',
                scaleMargins: {
                    top: 0.8,
                    bottom: 0,
                },
            });
        } else {
            console.warn('addHistogramSeries not available');
        }

        // Add MACD series (below chart)
        if (typeof this.chart.addLineSeries === 'function') {
            this.macdSeries = this.chart.addLineSeries({
                color: '#2196F3',
                lineWidth: 2,
                priceScaleId: 'macd',
                title: 'MACD',
            });

            // Add Moving Average series
            this.maSeries = this.chart.addLineSeries({
                color: '#FF9800',
                lineWidth: 1,
                title: 'MA(20)',
            });
        } else {
            console.warn('addLineSeries not available');
        }

        // Add RSI series (separate pane - would need separate chart for proper display)
        // For now, we'll overlay it on the main chart with a different scale

        // Setup event listeners
        this.setupEventListeners();

        // Load initial data
        this.loadChartData();

        // Setup auto-update
        this.setupAutoUpdate();
    }

    setupEventListeners() {
        const symbolSelect = document.getElementById(this.symbolSelectId);
        const timeframeSelect = document.getElementById(this.timeframeSelectId);
        const timeRangeSelect = document.getElementById(this.timeRangeSelectId);
        const autoUpdateCheck = document.getElementById(this.autoUpdateCheckId);
        const showMacd = document.getElementById(this.showMacdId);
        const showRsi = document.getElementById(this.showRsiId);
        const showMA = document.getElementById(this.showMAId);
        const showVolume = document.getElementById(this.showVolumeId);

        symbolSelect?.addEventListener('change', () => this.loadChartData());
        timeframeSelect?.addEventListener('change', () => this.loadChartData());
        timeRangeSelect?.addEventListener('change', () => this.loadChartData());
        autoUpdateCheck?.addEventListener('change', (e) => {
            if (e.target.checked) {
                this.setupAutoUpdate();
            } else {
                this.stopAutoUpdate();
            }
        });

        showMacd?.addEventListener('change', (e) => this.toggleIndicator('macd', e.target.checked));
        showRsi?.addEventListener('change', (e) => this.toggleIndicator('rsi', e.target.checked));
        showMA?.addEventListener('change', (e) => this.toggleIndicator('ma', e.target.checked));
        showVolume?.addEventListener('change', (e) => this.toggleIndicator('volume', e.target.checked));

        // Handle window resize
        window.addEventListener('resize', () => {
            if (this.chart) {
                const container = document.getElementById(this.containerId);
                this.chart.applyOptions({ width: container.clientWidth });
            }
        });
    }

    async loadChartData() {
        const symbol = document.getElementById(this.symbolSelectId)?.value || this.defaultSymbol;
        const timeframe = document.getElementById(this.timeframeSelectId)?.value || this.defaultTimeframe;
        const timeRange = document.getElementById(this.timeRangeSelectId)?.value || '1D';

        const { from, to } = this.getTimeRange(timeRange);

        try {
            // Load candles
            const url = `/Charts/api/candles?symbol=${symbol}&timeframe=${timeframe}&from=${from.toISOString()}&to=${to.toISOString()}`;
            console.log('Fetching chart data from:', url);
            
            const response = await fetch(url);
            if (!response.ok) {
                const errorText = await response.text();
                console.error('Failed to load chart data:', response.status, errorText);
                throw new Error(`Failed to load chart data: ${response.status} ${errorText}`);
            }

            const data = await response.json();
            console.log('Chart data received:', data.candles?.length || 0, 'candles');

            if (!data.candles || data.candles.length === 0) {
                console.warn('No candle data available');
                const container = document.getElementById(this.containerId);
                if (container) {
                    container.innerHTML = '<div class="alert alert-warning m-3">No candle data available for the selected symbol and timeframe. Please ensure market data is configured and try again.</div>';
                }
                return;
            }

            // Update candlestick series
            const candleData = data.candles.map(c => ({
                time: c.time,
                open: c.open,
                high: c.high,
                low: c.low,
                close: c.close,
            }));

            this.candlestickSeries.setData(candleData);
            this.lastCandleTime = data.candles[data.candles.length - 1].time;

            // Update volume series
            const volumeData = data.candles.map(c => ({
                time: c.time,
                value: c.volume,
                color: c.close >= c.open ? '#26a69a26' : '#ef535026',
            }));

            const showVolume = document.getElementById(this.showVolumeId)?.checked ?? true;
            if (showVolume) {
                this.volumeSeries.setData(volumeData);
            } else {
                this.volumeSeries.setData([]);
            }

            // Load indicators
            await this.loadIndicators(symbol, timeframe, from, to);

            // Fit content
            this.chart.timeScale().fitContent();
        } catch (error) {
            console.error('Error loading chart data:', error);
            const container = document.getElementById(this.containerId);
            if (container && container.children.length === 0) {
                container.innerHTML = `<div class="alert alert-danger m-3">Failed to load chart data: ${error.message}. Please check the browser console for details.</div>`;
            }
        }
    }

    async loadIndicators(symbol, timeframe, from, to) {
        const showMacd = document.getElementById(this.showMacdId)?.checked ?? false;
        const showRsi = document.getElementById(this.showRsiId)?.checked ?? false;
        const showMA = document.getElementById(this.showMAId)?.checked ?? false;

        try {
            // Load MACD
            if (showMacd) {
                const macdResponse = await fetch(`/Charts/api/indicators?symbol=${symbol}&timeframe=${timeframe}&indicator=MACD&from=${from.toISOString()}&to=${to.toISOString()}`);
                if (macdResponse.ok) {
                    const macdData = await macdResponse.json();
                    if (macdData.data && macdData.data.length > 0) {
                        // MACD is typically displayed below the main chart
                        // For simplicity, we'll show MACD line on the main chart with a different scale
                        const macdLineData = macdData.data.map(d => ({
                            time: d.time,
                            value: d.macd,
                        }));
                        if (this.macdSeries) {
                            this.macdSeries.setData(macdLineData);
                        }
                    }
                }
            } else if (this.macdSeries) {
                this.macdSeries.setData([]);
            }

            // Load RSI
            if (showRsi) {
                const rsiResponse = await fetch(`/Charts/api/indicators?symbol=${symbol}&timeframe=${timeframe}&indicator=RSI&from=${from.toISOString()}&to=${to.toISOString()}`);
                if (rsiResponse.ok) {
                    const rsiData = await rsiResponse.json();
                    if (rsiData.data && rsiData.data.length > 0) {
                        // RSI would ideally be in a separate pane, but for simplicity, we'll skip it for now
                        // or overlay it with a different scale
                        console.log('RSI data loaded:', rsiData.data.length, 'points');
                    }
                }
            }

            // Load Moving Average
            if (showMA) {
                const maResponse = await fetch(`/Charts/api/indicators?symbol=${symbol}&timeframe=${timeframe}&indicator=MA&from=${from.toISOString()}&to=${to.toISOString()}`);
                if (maResponse.ok) {
                    const maData = await maResponse.json();
                    if (maData.data && maData.data.length > 0) {
                        const maLineData = maData.data.map(d => ({
                            time: d.time,
                            value: d.value,
                        }));
                        if (this.maSeries) {
                            this.maSeries.setData(maLineData);
                        }
                    }
                }
            } else if (this.maSeries) {
                this.maSeries.setData([]);
            }
        } catch (error) {
            console.error('Error loading indicators:', error);
        }
    }

    getTimeRange(range) {
        const to = new Date();
        let from = new Date();

        switch (range) {
            case '1D':
                from = new Date(to.getTime() - 24 * 60 * 60 * 1000);
                break;
            case '1W':
                from = new Date(to.getTime() - 7 * 24 * 60 * 60 * 1000);
                break;
            case '1M':
                from = new Date(to.getTime() - 30 * 24 * 60 * 60 * 1000);
                break;
            case '3M':
                from = new Date(to.getTime() - 90 * 24 * 60 * 60 * 1000);
                break;
            case '1Y':
                from = new Date(to.getTime() - 365 * 24 * 60 * 60 * 1000);
                break;
            case 'ALL':
                from = new Date(to.getTime() - 365 * 2 * 24 * 60 * 60 * 1000); // 2 years
                break;
            default:
                from = new Date(to.getTime() - 24 * 60 * 60 * 1000);
        }

        return { from, to };
    }

    setupAutoUpdate() {
        this.stopAutoUpdate();

        const autoUpdateCheck = document.getElementById(this.autoUpdateCheckId);
        if (!autoUpdateCheck?.checked) {
            return;
        }

        this.updateInterval = setInterval(async () => {
            await this.updateLatestCandle();
        }, 5000); // Update every 5 seconds
    }

    stopAutoUpdate() {
        if (this.updateInterval) {
            clearInterval(this.updateInterval);
            this.updateInterval = null;
        }
    }

    async updateLatestCandle() {
        const symbol = document.getElementById(this.symbolSelectId)?.value || this.defaultSymbol;
        const timeframe = document.getElementById(this.timeframeSelectId)?.value || this.defaultTimeframe;

        try {
            const response = await fetch(`/Charts/api/latest?symbol=${symbol}&timeframe=${timeframe}`);
            if (!response.ok) {
                return;
            }

            const data = await response.json();
            if (!data.isNew || !data.candle) {
                return;
            }

            // Check if this is a new candle or update to existing
            if (data.candle.time === this.lastCandleTime) {
                // Update existing candle
                this.candlestickSeries.update({
                    time: data.candle.time,
                    open: data.candle.open,
                    high: data.candle.high,
                    low: data.candle.low,
                    close: data.candle.close,
                });
            } else {
                // New candle
                this.candlestickSeries.update({
                    time: data.candle.time,
                    open: data.candle.open,
                    high: data.candle.high,
                    low: data.candle.low,
                    close: data.candle.close,
                });
                this.lastCandleTime = data.candle.time;
            }
        } catch (error) {
            console.error('Error updating latest candle:', error);
        }
    }

    toggleIndicator(indicator, show) {
        // Indicator visibility is handled by data loading
        if (show) {
            const symbol = document.getElementById(this.symbolSelectId)?.value || this.defaultSymbol;
            const timeframe = document.getElementById(this.timeframeSelectId)?.value || this.defaultTimeframe;
            const timeRange = document.getElementById(this.timeRangeSelectId)?.value || '1D';
            const { from, to } = this.getTimeRange(timeRange);
            this.loadIndicators(symbol, timeframe, from, to);
        } else {
            // Clear indicator data
            switch (indicator) {
                case 'macd':
                    if (this.macdSeries) {
                        this.macdSeries.setData([]);
                    }
                    break;
                case 'rsi':
                    // RSI not displayed separately for now
                    break;
                case 'ma':
                    if (this.maSeries) {
                        this.maSeries.setData([]);
                    }
                    break;
                case 'volume':
                    if (this.volumeSeries) {
                        this.volumeSeries.setData([]);
                    }
                    break;
            }
        }
    }
}
