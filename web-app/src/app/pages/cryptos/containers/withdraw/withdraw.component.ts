import { Component, inject, output, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { WithdrawFundCommand } from 'src/app/core/models/deposit-fund-command';
import { AccountService } from 'src/app/core/services/account.service';

@Component({
  selector: 'app-withdraw',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './withdraw.component.html',
  styleUrl: './withdraw.component.scss'
})
export class WithdrawComponent {
  private fb = inject(FormBuilder);
  private accountService = inject(AccountService);
  withdrawForm!: FormGroup;
  loading = signal(false);
  withdrawEvent = output<WithdrawFundCommand | null>();
  constructor() {
    this.withdrawForm = this.fb.group({
      amount: ['', Validators.required],
      date: ['', Validators.required],
      notes: ['', [Validators.max(255)]]
    });
  }

  onSubmit() {
    if (this.withdrawForm.invalid) {
      console.log('Please fill all fields correctly');
      return;
    }

    const withdraw: WithdrawFundCommand = {
      amount: this.withdrawForm.value.amount,
      date: this.withdrawForm.value.date,
      notes: this.withdrawForm.value.notes
    };
    this.loading.set(true);
    this.accountService.withdrawFund(withdraw)
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          this.withdrawEvent.emit(withdraw);
          console.log('Withdrawal successful');
        },
        error: (err) => {
          this.loading.set(false);
          console.log(err)
        }
      });
  }
}
