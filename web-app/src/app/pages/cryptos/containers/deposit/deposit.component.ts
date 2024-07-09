import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DepositFundCommand } from 'src/app/core/models/deposit-fund-command';
import { CryptoService } from 'src/app/core/services/crypto.service';
import { AccountTransactionType } from '../account/account.component';
import { ToastService } from 'src/app/core/services/toast.service';
import { JsonPipe } from '@angular/common';
import { tap } from 'rxjs';

@Component({
  selector: 'app-deposit',
  standalone: true,
  imports: [ReactiveFormsModule, JsonPipe],
  templateUrl: './deposit.component.html',
  styleUrl: './deposit.component.scss'
})
export class DepositComponent implements OnInit {
  private fb = inject(FormBuilder);
  private cryptoService = inject(CryptoService);
  private toastService = inject(ToastService);

  depositType = AccountTransactionType;
  depositForm!: FormGroup;
  constructor() {
    this.depositForm = this.fb.group({
      accountTransactionType: [this.depositType, Validators.minLength(1)],
      amount: ['', [Validators.min(0)]],
      currentPrice: [],
      exchangeName: [],
      date: ['', [Validators.required]],
    });
  }
  ngOnInit(): void {
    this.addConditionalValidation();
  }

  onSubmit() {

    if (this.depositForm.invalid) {
      this.toastService.showError('Please fill all fields correctly');
      return;
    }
    const deposit: DepositFundCommand = {
      amount: parseFloat(this.depositForm.value.amount),
      accountTransactionType: this.mapAccountDepositType(this.depositForm.value.accountTransactionType),
      currentPrice: this.depositForm.value.currentPrice ? parseFloat(this.depositForm.value.currentPrice) : 0,
      exchangeName: this.depositForm.value.exchangeName?.trim() || '',
      date: this.depositForm.value.date,
    };

    this.cryptoService.depositFund(deposit).subscribe({
      next: (result) => { console.log(result) },
      error: (err) => { console.log(err) }
    });
  }

  private mapAccountDepositType(depositType: number): AccountTransactionType {
    return depositType == 1 ? AccountTransactionType.DepositFiat : AccountTransactionType.DepositCrypto;
  }

  private addConditionalValidation() {
    this.depositForm.get('accountTransactionType')?.valueChanges.pipe(tap(
      value => {
        console.log(value)
        if (value == this.depositType.DepositCrypto) {
          this.currentPrice.setValidators([Validators.required, Validators.min(0)]);
          this.exchangeName.setValidators([Validators.required, Validators.maxLength(255)]);
        } else {
          this.currentPrice.setValue('');
          this.exchangeName.setValue('');
          this.currentPrice.clearValidators();
          this.exchangeName.clearValidators();
        }

        this.currentPrice.updateValueAndValidity();
        this.exchangeName.updateValueAndValidity();
      }
    )).subscribe();
  }

  get accountTransactionType() { return this.depositForm.get('accountTransactionType') as FormControl }
  get currentPrice() { return this.depositForm.get('currentPrice') as FormControl }
  get exchangeName() { return this.depositForm.get('exchangeName') as FormControl }
  get date() { return this.depositForm.get('date') as FormControl }
}
