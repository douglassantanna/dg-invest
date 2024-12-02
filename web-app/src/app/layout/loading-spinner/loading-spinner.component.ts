import { Component, input } from '@angular/core';

@Component({
  selector: 'app-loading-spinner',
  templateUrl: './loading-spinner.component.html',
  standalone: true
})
export class LoadingSpinnerComponent {
  loading = input.required<boolean>();
}
