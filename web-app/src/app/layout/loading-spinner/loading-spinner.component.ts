import { Component, computed, inject } from '@angular/core';
import { LoadingService } from 'src/app/core/services/loading.service';

@Component({
  selector: 'app-loading-spinner',
  templateUrl: './loading-spinner.component.html',
  standalone: true,
})
export class LoadingSpinnerComponent {
  private loadingService = inject(LoadingService);
  isLoading = computed(() => this.loadingService.isLoading());
}
