import { Component } from '@angular/core';
import { CryptoFilterComponent } from '../../components/crypto-filter.component';

@Component({
  selector: 'app-account',
  standalone: true,
  imports: [
    CryptoFilterComponent
  ],
  templateUrl: './account.component.html',
})
export class AccountComponent {

  searchTransactions(input: any) {
    console.log(input);
  }
}
