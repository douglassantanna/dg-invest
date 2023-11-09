import { CommonModule } from '@angular/common';
import { Component, Input, inject } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { AddTransactionCommand, CryptoService, ETransactionType } from '../services/crypto.service';
import { ToastService } from '../services/toast.service';
export interface MyDate {
  year: number;
  month: number;
}
@Component({
  selector: 'app-add-transaction',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule],
  template: `
    <form class="row g-3 border border-1 rounded" [formGroup]="transactionForm" (ngSubmit)="save()">
      <div class="col-md-6">
        <label for="transactionType" class="form-label">Transaction Type</label>
        <select class="form-select" id="transactionType" name="transactionType" formControlName="transactionType">
          <option [value]="1">Buy</option>
          <option [value]="2">Sell</option>
        </select>
      </div>
      <div class="col-md-6">
          <label for="amount" class="form-label">Amount</label>
          <input type="number" class="form-control" id="amount" name="amount" formControlName="amount">
      </div>
      <div class="col-md-6">
          <label for="pricePerUnit" class="form-label">Price per Unit</label>
          <input type="number" class="form-control" id="pricePerUnit" name="pricePerUnit" formControlName="price">
      </div>
      <div class="col-md-6">
        <label for="exchangeName" class="form-label">Exchange Name</label>
        <input type="text" class="form-control" id="exchangeName" name="exchangeName" formControlName="exchangeName">
      </div>
      <div class="col-md-6">
          <label for="purchaseDate" class="form-label">Date of Purchase</label>
          <input type="date" class="form-control" id="purchaseDate" name="purchaseDate" formControlName="purchaseDate">
      </div>
      <div class="col-12">
        <button type="submit" class="btn btn-primary">Save</button>
      </div>
    </form>
  `,
  styles: [`

  `]
})
export class AddTransactionComponent {
  @Input() cryptoAssetId!: number;
  cryptoService = inject(CryptoService);
  toastService = inject(ToastService);
  fb = inject(FormBuilder);
  transactionForm!: FormGroup;

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

    console.log("command", command);
    console.log("transactionForm", this.transactionForm.value);


    this.cryptoService.addTransaction(command).subscribe({
      next: (response) => {
        console.log(response);
        this.toastService.showSuccess("Transaction added successfully");
        this.transactionForm.reset();

      },
      error: (err) => {
        console.log(err.error.data);

        this.toastService.showError(err.error.data);
      }
    });
  }

  mapTransactionType(value: number): ETransactionType {
    switch (value) {
      case 1:
        return ETransactionType.Buy;
      case 2:
        return ETransactionType.Sell;
      default:
        return ETransactionType.Buy;
    }
  }
}
