import { CommonModule } from '@angular/common';
import { Component, inject, Input } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { AddTransactionCommand } from 'src/app/core/models/add-transaction-command';
import { ETransactionType } from 'src/app/core/models/etransaction-type';
import { CryptoService } from 'src/app/core/services/crypto.service';
import { ToastService } from 'src/app/core/services/toast.service';
import { environment } from 'src/environments/environment.development';

@Component({
  selector: 'app-add-transaction',
  templateUrl: './add-transaction.component.html',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule]
})
export class AddTransactionComponent {
  @Input() cryptoAssetId!: number;
  cryptoService = inject(CryptoService);
  toastService = inject(ToastService);
  fb = inject(FormBuilder);
  transactionForm!: FormGroup;
  btnColor = environment.btnColor;

  constructor() {
    this.transactionForm = this.fb.group({
      amount: ['', [Validators.min(0)]],
      price: ['',],
      purchaseDate: [''],
      exchangeName: [''],
      transactionType: [0]
    });
  }

  save() {
    const command = {
      amount: this.transactionForm.value.amount,
      price: this.transactionForm.value.price,
      purchaseDate: this.transactionForm.value.purchaseDate,
      exchangeName: this.transactionForm.value.exchangeName,
      transactionType: this.mapTransactionType(this.transactionForm.value.transactionType),
      cryptoAssetId: this.cryptoAssetId
    } as AddTransactionCommand;

    this.cryptoService.addTransaction(command)
      .subscribe({
        next: (response) => {
          this.toastService.showSuccess("Transaction added successfully");
          this.transactionForm.reset();
        },
        error: (err) => {
          this.toastService.showError(err.error.data);
        }
      });
  }

  private mapTransactionType(value: number): ETransactionType {
    return value == 1 ? ETransactionType.Buy : ETransactionType.Sell;
  }
}
