import { NgClass } from '@angular/common';
import { AfterViewInit, ChangeDetectionStrategy, Component, computed, ElementRef, inject, model, signal, ViewChild } from '@angular/core';
import * as echarts from 'echarts';
import { CryptoService, ETimeframe } from 'src/app/core/services/crypto.service';
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
  timeArray = signal<TimeArray[]>([
    {
      description: '24H',
      time: ETimeframe._24h
    },
    {
      description: '7D',
      time: ETimeframe._7d
    },
    {
      description: '1M',
      time: ETimeframe._1m
    },
  ]);
  selectedTimeFilter = model<ETimeframe>(ETimeframe._24h);
  marketDataNew: MarketData = {
    [ETimeframe._24h]: [],
    [ETimeframe._7d]: [],
    [ETimeframe._1m]: [],
  };
  lineChartInstance: any = null;
  lineChartTitle = signal('');
  selectedTimeFilterSignal = computed(() => this.selectedTimeFilter());
  isMobileMode = computed(() => this.layoutService.isMobile());

  ngAfterViewInit(): void {
    if (this.marketDataNew) {
      this.initLineChart(this.selectedTimeFilter());
    }
    this.fetchMarketData();
  }

  private fetchMarketData() {
    this.cryptoService.getMarketDataByTimeframe(this.selectedTimeFilter())
      .subscribe({
        next: (result) => {
          this.marketDataNew[this.selectedTimeFilter()] = result.value;
          this.initLineChart(this.selectedTimeFilter());
        },
        error: (err) => { console.log(err); }
      });
  }

  setTimeFilter(time: ETimeframe) {
    this.selectedTimeFilter.set(time);
    this.fetchMarketData();
    this.initLineChart(this.selectedTimeFilter());
  }

  initLineChart(selectedTime: ETimeframe) {
    const chartElement = this.chartContainer.nativeElement;

    if (!this.lineChartInstance) {
      this.lineChartInstance = echarts.init(chartElement);
    }
    const selectedData = this.marketDataNew[selectedTime];
    const axisLabelFormatter = (value: string) => this.axisLabelFormatter(value, selectedTime);
    this.lineChartInstance.clear();
    const selectedTimeTitle = this.timeArray().find(t => t.time === this.selectedTimeFilter());
    this.lineChartTitle.set(`${selectedTimeTitle?.description} Market Value`);
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
        data: selectedData.map(d => [d.time * 1000, d.value.toFixed(2)]),
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
    this.lineChartInstance.resize();
  }

  private axisLabelFormatter(value: string, selectedTime: ETimeframe): string {
    const date = new Date(value);
    if (selectedTime === ETimeframe._24h) return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    if (selectedTime === ETimeframe._7d) return date.toLocaleDateString([], { month: 'short', day: 'numeric' });
    return date.toLocaleDateString([], { month: 'short', day: 'numeric' });
  }
}

export interface MarketData {
  [key: number]: TimeValue[];
}

export interface TimeArray {
  time: ETimeframe;
  description: string;
}

export interface TimeValue {
  time: number;
  value: number;
}
