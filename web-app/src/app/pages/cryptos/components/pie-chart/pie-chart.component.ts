import { AfterViewInit, Component, ElementRef, HostListener, ViewChild, computed, input, signal } from '@angular/core';
import { ViewCryptoInformation } from 'src/app/core/models/view-crypto-information';
import { NgStyle } from '@angular/common';
import * as echarts from 'echarts';

@Component({
  selector: 'app-pie-chart',
  standalone: true,
  imports: [NgStyle],
  templateUrl: './pie-chart.component.html',
})
export class PieChartComponent implements AfterViewInit {
  cryptos = input<ViewCryptoInformation[]>([]);
  @ViewChild('pieChartContainer', { static: false }) pieChartContainer!: ElementRef;
  pieChartInstance: any = null;
  pieChartTitle = signal('Portfolio Holdings');
  isMobileMode = computed(() => window.innerWidth < 640);
  @HostListener('window:resize', ['$event'])
  onResize() {
    if (this.pieChartInstance) {
      this.pieChartInstance.resize();
    }
  }

  ngAfterViewInit(): void {
    this.initPieChart();
  }

  private initPieChart() {
    const chartElement = this.pieChartContainer.nativeElement;
    if (this.pieChartInstance) {
      this.pieChartInstance.dispose();
    }
    this.pieChartInstance = echarts.init(chartElement);
    const totalSum = this.cryptos().reduce((sum, item) => sum + item.investedAmount, 0);
    const cryptoHoldingData = this.cryptos().map((item) => {
      return {
        value: item.investedAmount,
        name: item.symbol
      }
    });

    const option = {
      tooltip: {
        trigger: 'item',
        formatter: (params: any) => `${params.name}: $${params.value.toFixed(2)} (${params.percent}%)`,
        showContent: true
      },
      legend: {
        show: false
      },
      avoidLabelOverlap: false,
      graphic: {
        type: 'text',
        left: 'center',
        top: 'center',
        style: {
          text: `Total: ${totalSum.toFixed(2)}`,
          fontSize: 14,
          fontWeight: 'bold',
          fill: '#333'
        }
      },
      series: [
        {
          name: '',
          type: 'pie',
          radius: ['30%', '50%'],
          itemStyle: {
            borderRadius: 5,
            borderColor: '#fff',
            borderWidth: 0.5
          },
          labelLine: {
            length: this.isMobileMode() ? 15 : 15,
          },
          label: {
            formatter: (params: any) => `{b|${params.name}}: $${params.value.toFixed(2)} (${params.percent}%)`,
            backgroundColor: '#F6F8FC',
            borderColor: '#8C8D8E',
            rich: {
              b: {
                color: '#4C5058',
                fontSize: this.isMobileMode() ? 12 : 14,
                fontWeight: 'bold',
                lineHeight: 33
              },
              per: {
                color: '#fff',
                backgroundColor: '#4C5058',
                padding: [3, 4],
                borderRadius: 4
              }
            },
            show: this.isMobileMode() ? true : true
          },
          data: cryptoHoldingData.filter((item) => item.value > 0)
        }
      ]
    };
    this.pieChartInstance.setOption(option);
  }
}
export interface DataValue {
  value: number;
  name: string;
}
