import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

import { AddTransactionComponent } from './add-transaction.component';
import { MyCryptoComponent } from './my-crypto.component';
import { PurchaseHistoryComponent } from './purchase-history.component';
import { ActivatedRoute, Router } from '@angular/router';
import { CryptoDto, CryptoService } from './services/crypto.service';

@Component({
  selector: 'app-crypto-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    AddTransactionComponent,
    MyCryptoComponent,
    PurchaseHistoryComponent,
    MatCardModule],
  template: `
  <div class="grid-container">
    <div class="div1">
      <app-my-crypto [crypto]="crypto"/>
    </div>
    <div class="div2">
      <app-add-transaction [cryptoAssetId]="crypto.id" />
    </div>
    <div class="div3">
      <mat-card>
        <mat-card-content>
          <app-purchase-history [crypto]="crypto"/>
        </mat-card-content>
      </mat-card>
    </div>
  </div>
  `,
  styles: [`
    .grid-container {
      display: grid;
      grid-template-columns: 1fr 1fr;
      grid-template-rows: 0.4fr 1.6fr;
      gap: 10px 10px;
      grid-template-areas:
        "div1 div3"
        "div2 div3";
      padding:10px;
    }
    .div1 {
      grid-area:div1;
    }
    .div2 {
      grid-area:div2;
    }
    .div3 {
      grid-area:div3;
      max-height:600px;
      overflow:auto;
    }
    @media (max-width: 640px) {
        .grid-container {
          grid-template-columns: 1fr;
          grid-template-rows: 0.4fr;
          grid-template-areas:
          "div1"
          "div2"
          "div3";
          justify-content: center;
          align-items: center;
          gap: 5px;
        }
      }
  `]
})
export class CryptoDashboardComponent {
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private cryptoService = inject(CryptoService);
  cryptoId = 0;
  crypto: CryptoDto = {} as CryptoDto;
  constructor() { }

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.cryptoId = params['cryptoId'];
      if (this.cryptoId) {
        this.getCryptoById(this.cryptoId);
      }
      else {
        this.router.navigate(['/cryptos']);
      }
    })
  }

  private getCryptoById(cryptoId: number) {
    this.cryptoService.getById(cryptoId).subscribe(
      {
        next: (crypto) => {
          this.crypto = crypto;
          console.log(this.crypto);
        }
        ,
        error: (error) => {
          console.log(error);
        },
      }
    )
  }
}
