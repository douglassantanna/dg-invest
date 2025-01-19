import { DatePipe } from '@angular/common';
import { Component, inject, input, signal } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { AddTransactionCommand } from 'src/app/core/models/add-transaction-command';
import { ETransactionType } from 'src/app/core/models/etransaction-type';
import { FormatCurrencyPipe } from 'src/app/core/pipes/format-currency.pipe';
import { CryptoService } from 'src/app/core/services/crypto.service';
import { ModalComponent } from 'src/app/layout/modal/modal.component';

@Component({
  selector: 'app-add-transaction',
  templateUrl: './add-transaction.component.html',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    ModalComponent,
    FormatCurrencyPipe,
    DatePipe]
})
export class AddTransactionComponent {
  cryptoAssetId = input(0);
  private cryptoService = inject(CryptoService);
  fb = inject(FormBuilder);
  transactionForm!: FormGroup;
  loading = signal(false);
  isConfirmationModalOpen = signal(false);

  toggleConfirmTransaction() {
    this.isConfirmationModalOpen.set(!this.isConfirmationModalOpen());
  }
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
    } as AddTransactionCommand;

    this.cryptoService.addTransaction(command)
      .subscribe({
        next: (response) => {
          this.transactionForm.reset();
          this.loading.set(false);
          this.toggleConfirmTransaction();
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
