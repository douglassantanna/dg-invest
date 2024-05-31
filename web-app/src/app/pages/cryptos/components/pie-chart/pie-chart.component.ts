import { Component, SimpleChanges, input } from '@angular/core';
import { Chart, registerables } from 'chart.js';
Chart.register(...registerables);
import { ViewCryptoInformation } from 'src/app/core/models/view-crypto-information';

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
      this.labels = cryptos.map((x: any) => x.symbol);
      this.positionValues = cryptos.map((x: any) => x.currentWorth);

      const ctx = document.getElementById('chart') as HTMLCanvasElement;

      if (Chart.getChart(ctx)) {
        Chart.getChart(ctx)?.destroy();
      }

      const numColors = this.labels.length;
      const colors = this.generateColors(numColors);

      new Chart(ctx, {
        type: 'pie',
        data: {
          labels: this.labels,
          datasets: [{
            label: 'Position Value',
            data: this.positionValues,
            backgroundColor: colors,
            hoverOffset: 4
          }]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false
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
