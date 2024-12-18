import { Component, inject, input, signal } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { AddTransactionCommand } from 'src/app/core/models/add-transaction-command';
import { ETransactionType } from 'src/app/core/models/etransaction-type';
import { AccountService } from 'src/app/core/services/account.service';
import { environment } from 'src/environments/environment.development';

@Component({
  selector: 'app-add-transaction',
  templateUrl: './add-transaction.component.html',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule]
})
export class AddTransactionComponent {
  cryptoAssetId = input(0);
  private accountService = inject(AccountService);
  fb = inject(FormBuilder);
  transactionForm!: FormGroup;
  btnColor = environment.btnColor;
  loading = signal(false);

  constructor() {
    this.transactionForm = this.fb.group({
      amount: ['', [Validators.min(0)]],
      price: ['',],
      purchaseDate: [''],
      exchangeName: [''],
      fee: [],
      transactionType: [0]
    });
  }

  save() {
    this.loading.set(true);
    const command = {
      amount: this.transactionForm.value.amount,
      price: this.transactionForm.value.price,
      purchaseDate: this.transactionForm.value.purchaseDate,
      exchangeName: this.transactionForm.value.exchangeName,
      transactionType: this.mapTransactionType(this.transactionForm.value.transactionType),
      cryptoAssetId: this.cryptoAssetId(),
      fee: this.transactionForm.value.fee ?? 0,
      subAccountTag: "main"
    } as AddTransactionCommand;

    this.accountService.addTransaction(command)
      .subscribe({
        next: (response) => {
          this.transactionForm.reset();
          this.loading.set(false);
        },
        error: (err) => {
          this.loading.set(false);
        }
      });
  }

  private mapTransactionType(value: number): ETransactionType {
    return value == 1 ? ETransactionType.Buy : ETransactionType.Sell;
  }
}
