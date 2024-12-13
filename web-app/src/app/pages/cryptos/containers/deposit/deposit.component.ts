import { Component, OnInit, inject, output, signal } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DepositFundCommand } from 'src/app/core/models/deposit-fund-command';
import { AccountTransactionType } from '../account/account.component';
import { ToastService } from 'src/app/core/services/toast.service';
import { UpperCasePipe } from '@angular/common';
import { tap } from 'rxjs';
import { ViewCryptoInformation } from 'src/app/core/models/view-crypto-information';
import { AccountService } from 'src/app/core/services/account.service';

@Component({
  selector: 'app-deposit',
  standalone: true,
  imports: [ReactiveFormsModule, UpperCasePipe],
  templateUrl: './deposit.component.html',
  styleUrl: './deposit.component.scss'
})
export class DepositComponent implements OnInit {
  private fb = inject(FormBuilder);
  private toastService = inject(ToastService);
  private accountService = inject(AccountService);

  depositEvent = output<DepositFundCommand | null>();
  cryptoAssets = signal<ViewCryptoInformation[]>([]);
  depositType = AccountTransactionType;
  depositForm!: FormGroup;
  loading = signal(false);
  constructor() {
    this.depositForm = this.fb.group({
      accountTransactionType: [this.depositType, Validators.minLength(1)],
      amount: ['', [Validators.min(0)]],
      currentPrice: [],
      cryptoAssetId: [],
      exchangeName: [],
      notes: ['', [Validators.max(255)]],
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
    const command: DepositFundCommand = {
      amount: parseFloat(this.depositForm.value.amount),
      accountTransactionType: this.mapAccountDepositType(this.depositForm.value.accountTransactionType),
      currentPrice: this.depositForm.value.currentPrice ? parseFloat(this.depositForm.value.currentPrice) : 0,
      exchangeName: this.depositForm.value.exchangeName?.trim() || '',
      cryptoAssetId: (this.depositForm.value.cryptoAssetId?.trim() || ''),
      date: this.depositForm.value.date,
      notes: this.depositForm.value.notes,
    };
    this.loading.set(true);

    this.accountService.depositFund("main", command)
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          this.depositEvent.emit(command);
        },
        error: (err) => { this.loading.set(false); console.log(err) }
      });
  }

  private mapAccountDepositType(depositType: number): AccountTransactionType {
    return depositType == 1 ? AccountTransactionType.DepositFiat : AccountTransactionType.DepositCrypto;
  }

  private addConditionalValidation() {
    this.depositForm.get('accountTransactionType')?.valueChanges.pipe(
      tap(value => {
        if (value == this.depositType.DepositCrypto) {
          this.currentPrice.setValidators([Validators.required, Validators.min(0)]);
          this.exchangeName.setValidators([Validators.required, Validators.maxLength(255)]);
          this.cryptoAssetId.setValidators([Validators.required, Validators.maxLength(255)]);
        } else {
          this.currentPrice.setValue('');
          this.exchangeName.setValue('');
          this.cryptoAssetId.setValue('');
          this.currentPrice.clearValidators();
          this.exchangeName.clearValidators();
          this.cryptoAssetId.clearValidators();
        }

        this.currentPrice.updateValueAndValidity();
        this.exchangeName.updateValueAndValidity();
        this.cryptoAssetId.updateValueAndValidity();
      })).subscribe();
  }

  get accountTransactionType() { return this.depositForm.get('accountTransactionType') as FormControl }
  get currentPrice() { return this.depositForm.get('currentPrice') as FormControl }
  get exchangeName() { return this.depositForm.get('exchangeName') as FormControl }
  get cryptoAssetId() { return this.depositForm.get('cryptoAssetId') as FormControl }
  get date() { return this.depositForm.get('date') as FormControl }
}
