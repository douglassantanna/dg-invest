import { Component, Input, OnInit, WritableSignal, signal } from '@angular/core';
import { Chart, registerables } from 'chart.js';
Chart.register(...registerables);
import { ViewCryptoInformation } from 'src/app/core/models/view-crypto-information';

@Component({
  selector: 'app-pie-chart',
  standalone: true,
  imports: [],
  templateUrl: './pie-chart.component.html',
})
export class PieChartComponent implements OnInit {
  @Input() cryptos: WritableSignal<any[]> = signal<any[]>([]);
  labels: string[] = [];
  positionValues: number[] = [];
  labelsSignal = signal<string[]>([]);

  ngOnInit(): void {

    // const labels = this.cryptos.map((x) => x.symbol);
    // this.labels = labels;
    // this.labelsSignal.set(labels)
    console.log('signal', this.labelsSignal())

    // const amounts = this.cryptos.map((x => x.investedAmount));
    // this.positionValues = amounts;
    console.log('position values', this.positionValues);
    console.log('labels', this.labels);

    const ctx = document.getElementById('chart') as HTMLCanvasElement;
    new Chart(ctx, {
      type: 'pie',
      data: {
        labels: this.labels,
        datasets: [{
          label: 'Position Value',
          data: this.positionValues,
          backgroundColor: ['#ff6384', '#36a2eb', '#cc65fe', '#ffce56'],
          hoverOffset: 4
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false
      }
    });
  }

  // []
  // {}
}
