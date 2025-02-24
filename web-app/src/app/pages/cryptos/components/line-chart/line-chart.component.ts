import { NgClass } from '@angular/common';
import { AfterViewInit, ChangeDetectionStrategy, Component, computed, ElementRef, inject, model, signal, ViewChild } from '@angular/core';
import * as echarts from 'echarts';
import { CryptoService } from 'src/app/core/services/crypto.service';
import { LayoutService } from 'src/app/core/services/layout.service';

@Component({
  selector: 'app-line-chart',
  templateUrl: './line-chart.component.html',
  standalone: true,
  imports: [NgClass],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LineChartComponent implements AfterViewInit {
  private cryptoService = inject(CryptoService);
  layoutService = inject(LayoutService);
  @ViewChild('chartContainer', { static: false }) chartContainer!: ElementRef;
  timeArray = signal<TimeFilter[]>(['_24h', '_7d', '_1m']);
  selectedTimeFilter = model<TimeFilter>('_24h');
  marketData: MarketData = {
    '_24h': [
      { time: 1640995200, value: 47000 },
      { time: 1640998800, value: 46000 },
      { time: 1641002400, value: 45000 },
      { time: 1641006000, value: 48000 },
      { time: 1641009600, value: 49000 },
      { time: 1641013200, value: 42000 },
      { time: 1641016800, value: 41000 },
      { time: 1641020400, value: 30000 },
      { time: 1641024000, value: 39000 },
      { time: 1641027600, value: 88000 },
      { time: 1641031200, value: 37000 },
      { time: 1641034800, value: 36000 },
      { time: 1641038400, value: 55000 },
      { time: 1641042000, value: 64000 },
      { time: 1641045600, value: 33000 },
      { time: 1641049200, value: 42000 },
      { time: 1641052800, value: 21000 },
      { time: 1641056400, value: 30000 },
      { time: 1641060000, value: 29000 },
      { time: 1641063600, value: 88000 },
      { time: 1641067200, value: 17000 },
      { time: 1641070800, value: 96000 },
      { time: 1641074400, value: 125000 },
      { time: 1641078000, value: 129000 }
    ]
    ,
    '_7d': [
      { time: 1640995200, value: 47000 },
      { time: 1641081600, value: 40000 },
      { time: 1641168000, value: 45500 },
      { time: 1641254400, value: 39000 },
      { time: 1641340800, value: 4500 },
      { time: 1641427200, value: 44000 },
      { time: 1641513600, value: 41000 },
    ],
    '_1m': [
      { time: 1640995200, value: 47000 },
      { time: 1641081600, value: 47500 },
      { time: 1641168000, value: 38000 },
      { time: 1641254400, value: 51500 },
      { time: 1641340800, value: 49000 },
      { time: 1641427200, value: 59500 },
      { time: 1641513600, value: 50000 },
      { time: 1641600000, value: 50500 },
      { time: 1641686400, value: 51000 },
      { time: 1641772800, value: 61500 },
      { time: 1641859200, value: 52000 },
      { time: 1641945600, value: 52500 },
      { time: 1642032000, value: 43000 },
      { time: 1642118400, value: 53500 },
      { time: 1642204800, value: 54000 },
      { time: 1642291200, value: 54500 },
      { time: 1642377600, value: 59000 },
      { time: 1642464000, value: 55500 },
      { time: 1642550400, value: 51000 },
      { time: 1642636800, value: 56500 },
      { time: 1642723200, value: 7000 },
      { time: 1642809600, value: 57500 },
      { time: 1642896000, value: 42800 },
      { time: 1642982400, value: 48500 },
      { time: 1643068800, value: 59000 },
      { time: 1643155200, value: 61500 },
      { time: 1643241600, value: 58000 },
      { time: 1643328000, value: 60500 },
      { time: 1643414400, value: 41000 },
      { time: 1643500800, value: 51500 }
    ]
  };
  marketDataNew: MarketData = { '_24h': [], '_7d': [], '_1m': [] };
  lineChartInstance: any = null;
  lineChartTitle = signal('');
  selectedTimeFilterSignal = computed(() => this.selectedTimeFilter());
  isMobileMode = computed(() => this.layoutService.isMobile());

  ngAfterViewInit(): void {
    if (this.marketData) {
      this.initLineChart(this.selectedTimeFilter());
    }

    this.fetchMarketData();
  }

  private fetchMarketData() {
    this.cryptoService.getMarketDataByTimeframe(this.selectedTimeFilter())
      .subscribe({
        next: (result) => {
          console.log('line chart result', result);
          this.marketDataNew[this.selectedTimeFilter()] = result;
          this.initLineChart(this.selectedTimeFilter());
        },
        error: (err) => { console.log(err); }
      });
  }

  setTimeFilter(time: TimeFilter) {
    this.selectedTimeFilter.set(time);
    this.fetchMarketData();
    this.initLineChart(this.selectedTimeFilter());
  }

  initLineChart(selectedTime: TimeFilter) {
    const chartElement = this.chartContainer.nativeElement;

    if (this.lineChartInstance) {
      this.lineChartInstance.dispose();
    }

    this.lineChartInstance = echarts.init(chartElement);
    const selectedData = this.marketData[selectedTime];
    const selectedDataNew = this.marketDataNew[selectedTime];
    const axisLabelFormatter = (value: string) => {
      const date = new Date(value);
      if (selectedTime === '_24h') return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
      if (selectedTime === '_7d') return date.toLocaleDateString([], { month: 'short', day: 'numeric' });
      return date.toLocaleDateString([], { month: 'short', day: 'numeric' });
    };

    this.lineChartInstance.clear();

    this.lineChartTitle.set(`${selectedTime.toUpperCase()} Market Data`);
    const options = {
      title: { text: '', left: 'left' },
      tooltip: { trigger: 'axis' },
      xAxis: {
        type: 'time',
        axisLabel: { formatter: axisLabelFormatter }
      },
      yAxis: { type: 'value' },
      series: [{
        type: 'line',
        smooth: true,
        data: selectedDataNew.map(d => [d.time * 1000, d.value]),
        itemStyle: { color: '#4F46E5' },
        areaStyle: { opacity: 0.2 }
      }],
      grid: {
        left: '0%',
        right: '0%',
        top: '5%',
        bottom: '0%',
        containLabel: true
      },
      media: [
        {
          option: {
            xAxis: { axisLabel: { fontSize: 10 } }
          }
        }
      ]
    };
    this.lineChartInstance.setOption(options);
  }
}

export type TimeFilter = '_24h' | '_7d' | '_1m';

export interface MarketData {
  '_24h': TimeValue[],
  '_7d': TimeValue[],
  '_1m': TimeValue[],
}

export interface TimeValue {
  time: number;
  value: number;
}
