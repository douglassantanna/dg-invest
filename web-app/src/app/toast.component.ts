import { Component, TemplateRef } from '@angular/core';
import { CommonModule, NgTemplateOutlet } from '@angular/common';
import { ToastService } from './core/services/toast.service';
import { NgbToastModule } from '@ng-bootstrap/ng-bootstrap';

export interface ToastInfo {
  header: string;
  body: string;
  delay?: number;
  classname: string;
  textOrTpl: TemplateRef<any> | null;
}

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule, NgbToastModule, NgTemplateOutlet],
  template: `
    <ngb-toast
			*ngFor="let toast of toastService.toasts"
			[class]="toast.classname"
			[autohide]="true"
			[delay]="toast.delay || 5000"
			(hidden)="toastService.remove(toast)"
		>
			<ng-template [ngIf]="isTemplate(toast)" [ngIfElse]="text">
				<ng-template [ngTemplateOutlet]="toast.textOrTpl"></ng-template>
			</ng-template>

			<ng-template #text>{{ toast.textOrTpl }}</ng-template>
		</ngb-toast>
    `,
  host: { class: 'toast-container position-fixed top-0 end-0 p-3', style: 'z-index: 1200' },
  styles: [`
  `]
})
export class ToastComponent {
  constructor(public toastService: ToastService) { }
  toasts: ToastInfo[] = [];

  isTemplate(toast: ToastInfo) {
    return toast.textOrTpl instanceof TemplateRef;
  }
}
