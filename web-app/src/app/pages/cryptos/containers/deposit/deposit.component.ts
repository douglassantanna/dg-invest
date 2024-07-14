import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DepositFundCommand } from 'src/app/core/models/deposit-fund-command';
import { CryptoService } from 'src/app/core/services/crypto.service';
import { AccountTransactionType } from '../account/account.component';
import { ToastService } from 'src/app/core/services/toast.service';
import { JsonPipe, UpperCasePipe } from '@angular/common';
import { Subject, takeUntil, tap } from 'rxjs';
import { Crypto } from '../../../../core/models/crypto';
import { ViewCryptoInformation } from 'src/app/core/models/view-crypto-information';

@Component({
  selector: 'app-deposit',
  standalone: true,
  imports: [ReactiveFormsModule, UpperCasePipe],
  templateUrl: './deposit.component.html',
  styleUrl: './deposit.component.scss'
})
export class DepositComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private cryptoService = inject(CryptoService);
  private toastService = inject(ToastService);
  private subscription: Subject<void> = new Subject();
  cryptoAssets = signal<ViewCryptoInformation[]>([]);
  depositType = AccountTransactionType;
  depositForm!: FormGroup;
  constructor() {
    this.depositForm = this.fb.group({
      accountTransactionType: [this.depositType, Validators.minLength(1)],
      amount: ['', [Validators.min(0)]],
      currentPrice: [],
      cryptoAssetId: [],
      exchangeName: [],
      date: ['', [Validators.required]],
    });
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
    this.subscription.complete();
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
      cryptoAssetId: (this.depositForm.value.cryptoAssetId?.trim() || ''),
      date: this.depositForm.value.date,
    };

    this.cryptoService.depositFund(deposit)
      .pipe(takeUntil(this.subscription))
      .subscribe({
        next: (result) => { console.log(result) },
        error: (err) => { console.log(err) }
      });
  }

  private mapAccountDepositType(depositType: number): AccountTransactionType {
    return depositType == 1 ? AccountTransactionType.DepositFiat : AccountTransactionType.DepositCrypto;
  }

  private addConditionalValidation() {
    this.depositForm.get('accountTransactionType')?.valueChanges.pipe(
      takeUntil(this.subscription),
      tap(value => {
        if (value == this.depositType.DepositCrypto) {
          this.getCryptos();
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

  private getCryptos() {
    this.cryptoService.getCryptoAssets()
      .pipe(takeUntil(this.subscription))
      .subscribe({
        next: (response) => this.cryptoAssets.set(response.items),
        error: (err) => console.log(err)
      });
  }

  get accountTransactionType() { return this.depositForm.get('accountTransactionType') as FormControl }
  get currentPrice() { return this.depositForm.get('currentPrice') as FormControl }
  get exchangeName() { return this.depositForm.get('exchangeName') as FormControl }
  get cryptoAssetId() { return this.depositForm.get('cryptoAssetId') as FormControl }
  get date() { return this.depositForm.get('date') as FormControl }
}
