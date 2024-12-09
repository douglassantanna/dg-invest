import { Component, SimpleChanges, input } from '@angular/core';
import { Chart, registerables } from 'chart.js';
Chart.register(...registerables);
import { ViewCryptoInformation } from 'src/app/core/models/view-crypto-information';
import ChartDataLabels from 'chartjs-plugin-datalabels';
@Component({
  selector: 'app-pie-chart',
  standalone: true,
  imports: [],
  templateUrl: './pie-chart.component.html',
})
export class PieChartComponent {
  cryptos = input<ViewCryptoInformation[]>([]);
  labels: string[] = [];
  positionValues: number[] = [];

  ngOnChanges(changes: SimpleChanges) {
    if (changes['cryptos']) {
      const cryptos = changes['cryptos'].currentValue;
      this.labels = cryptos.map((x: any) => x.symbol.replace(/USD$/, ''));
      this.positionValues = cryptos.map((x: any) => x.currentWorth);
      const ctx = document.getElementById('chart') as HTMLCanvasElement;

      if (Chart.getChart(ctx)) {
        Chart.getChart(ctx)?.destroy();
      }

      const numColors = this.labels.length;
      const colors = this.generateColors(numColors);

      new Chart(ctx, {
        type: 'doughnut',
        data: {
          labels: this.labels,
          datasets: [{
            label: 'Position Value',
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

  private generateColors(numColors: number) {
    const colors = [];
    for (let i = 0; i < numColors; i++) {
      const r = Math.floor(Math.random() * 256);
      const g = Math.floor(Math.random() * 256);
      const b = Math.floor(Math.random() * 256);

      const color = `#${r.toString(16).padStart(2, '0')}${g.toString(16).padStart(2, '0')}${b.toString(16).padStart(2, '0')}`;
      colors.push(color);
    }
    return colors;
  }
}
