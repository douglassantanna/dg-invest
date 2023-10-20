import { Injectable, TemplateRef } from '@angular/core';
import { ToastInfo } from '../toast.component';


@Injectable({
  providedIn: 'root'
})
export class ToastService {
  toasts: ToastInfo[] = [];

  private show(textOrTpl: string | TemplateRef<any>, options: any = {}) {
    this.toasts.push({ textOrTpl, ...options });
  }

  showSuccess(successMessage: string | TemplateRef<any>) {
    this.show(successMessage, { classname: 'bg-success text-light' })
  }

  showError(errorMessage: string | TemplateRef<any>) {
    this.show(errorMessage, { classname: 'bg-danger text-light' })
  }

  remove(toast: any) {
    this.toasts = this.toasts.filter((t) => t !== toast);
  }

  private clear() {
    this.toasts.splice(0, this.toasts.length);
  }
}
