import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-add-transaction',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule],
  template: `
    <form class="row g-3 border border-1 rounded">
      <div class="col-md-6">
        <label for="transactionType" class="form-label">Transaction Type</label>
        <select class="form-select" id="transactionType" name="transactionType">
          <option value="1">Buy</option>
          <option value="2">Sell</option>
        </select>
      </div>
      <div class="col-md-6">
          <label for="amount" class="form-label">Amount</label>
          <input type="number" class="form-control" id="amount" name="amount">
      </div>
      <div class="col-md-6">
          <label for="pricePerUnit" class="form-label">Price per Unit</label>
          <input type="number" class="form-control" id="pricePerUnit" name="pricePerUnit">
      </div>
      <div class="col-md-6">
        <label for="exchangeName" class="form-label">Exchange Name</label>
        <input type="text" class="form-control" id="exchangeName" name="exchangeName">
      </div>
      <div class="col-md-6">
          <label for="purchaseDate" class="form-label">Date of Purchase</label>
          <input type="date" class="form-control" id="purchaseDate" name="purchaseDate">
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
  title = 'Add transaction';
  cryptoOptions: any[] = [
    'Bitcoin',
    'Ethereum',
    'Tether',
    'Litecoin',
    'Cardano',
    'Binance Coin',
    'Polkadot',
    'Solana',
    'Avalanche',
  ];
  save() { }
}
