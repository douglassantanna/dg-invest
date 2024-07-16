import { Component, OnDestroy, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { WithdrawFundCommand } from 'src/app/core/models/deposit-fund-command';
import { CryptoService } from 'src/app/core/services/crypto.service';
import { ToastService } from 'src/app/core/services/toast.service';

@Component({
  selector: 'app-withdraw',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './withdraw.component.html',
  styleUrl: './withdraw.component.scss'
})
export class WithdrawComponent implements OnDestroy {
  private fb = inject(FormBuilder);
  private cryptoService = inject(CryptoService);
  private toastService = inject(ToastService);
  private subscription: Subject<void> = new Subject();
  withdrawForm!: FormGroup;
  constructor() {
    this.withdrawForm = this.fb.group({
      amount: ['', Validators.required],
      date: ['', Validators.required],
      notes: ['', [Validators.max(255)]]
    });
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
    this.subscription.complete();
  }

  onSubmit() {
    if (this.withdrawForm.invalid) {
      this.toastService.showError('Please fill all fields correctly');
      return;
    }

    const withdraw: WithdrawFundCommand = {
      amount: this.withdrawForm.value.amount,
      date: this.withdrawForm.value.date,
      notes: this.withdrawForm.value.notes
    };
    this.cryptoService.withdrawFund(withdraw)
      .pipe(takeUntil(this.subscription))
      .subscribe({
        next: (result) => { this.toastService.showSuccess(result.message) },
        error: (err) => { this.toastService.showError(err), console.log(err) }
      });
  }
}
