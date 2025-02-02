import { Component, SimpleChanges, input, signal } from '@angular/core';
import { Chart, registerables } from 'chart.js';
Chart.register(...registerables);
import { ViewCryptoInformation } from 'src/app/core/models/view-crypto-information';
import ChartDataLabels from 'chartjs-plugin-datalabels';
import { forEach } from 'cypress/types/lodash';
import { NgStyle } from '@angular/common';
@Component({
  selector: 'app-pie-chart',
  standalone: true,
  imports: [NgStyle],
  templateUrl: './pie-chart.component.html',
})
export class PieChartComponent {
  cryptos = input<ViewCryptoInformation[]>([]);
  labels: string[] = [];
  imagesAndColors: string[] = [];
  positionValues: number[] = [];
  cryptoImages = signal<CryptoImageAux[]>([]);

  ngOnChanges(changes: SimpleChanges) {
    if (changes['cryptos']) {
      const cryptos = changes['cryptos'].currentValue;
      this.imagesAndColors = cryptos.map((x: any) => {
        let cryptoSymbol = x.symbol.replace(/USD$/, '');
        return `https://dgistage.blob.core.windows.net/crypto-logos/${cryptoSymbol}-logo.png`;
      });
      this.labels = cryptos.map((x: any) => x.symbol);
      this.positionValues = cryptos.map((x: any) => x.currentWorth);
      const ctx = document.getElementById('chart') as HTMLCanvasElement;

      if (Chart.getChart(ctx)) {
        Chart.getChart(ctx)?.destroy();
      }

      const colors = this.generateColors(this.imagesAndColors);

      new Chart(ctx, {
        type: 'doughnut',
        data: {
          labels: this.labels,
          datasets: [{
            data: this.positionValues,
            backgroundColor: colors,
            hoverOffset: 4
          }]
        },
        plugins: [ChartDataLabels],
        options: {
          locale: "en-US",
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: { display: false },
            tooltip: {
              callbacks: {
                label: function (tooltipItem: any) {
                  const rawValue = tooltipItem.raw;
                  const value = parseFloat(rawValue);

                  if (isNaN(value)) {
                    return rawValue;
                  }

                  return `${value.toLocaleString("en-US", {
                    style: "currency",
                    currency: "USD",
                    minimumFractionDigits: 2
                  })}`;
                }
              }
            },
            datalabels: {
              align: 'center',
              color: 'white',
              formatter: (value, context) => {
                const dataPoints = context.chart.config.data.datasets[0].data;
                function totalSum(total: any, datapoint: any) {
                  return total + datapoint;
                }
                const totalValue = dataPoints.reduce(totalSum, 0);
                const percentageValue = (value / totalValue * 100).toFixed(1);
                return `${percentageValue}%`
              }
            }
          }
        }
      });
    }
  }

  private generateColors(imageUrls: string[]): string[] {
    const colors = [];
    for (let i = 0; i < imageUrls.length; i++) {
      const r = Math.floor(Math.random() * 256);
      const g = Math.floor(Math.random() * 256);
      const b = Math.floor(Math.random() * 256);

      const color = `#${r.toString(16).padStart(2, '0')}${g.toString(16).padStart(2, '0')}${b.toString(16).padStart(2, '0')}`;
      colors.push(color);
      this.cryptoImages.set([...this.cryptoImages(), { imageUrl: imageUrls[i], color: color }]);
    }
    return colors;
  }
}
export interface CryptoImageAux {
  imageUrl: string;
  color: string;
}
