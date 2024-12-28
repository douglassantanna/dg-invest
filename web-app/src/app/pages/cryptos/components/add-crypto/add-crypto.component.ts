import { Component, ElementRef, inject, output, ViewChild, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgbDatepickerModule } from '@ng-bootstrap/ng-bootstrap';
import { environment } from 'src/environments/environment.development';
import { Crypto } from 'src/app/core/models/crypto';
import { CreateCryptoAssetCommand } from 'src/app/core/models/create-crypto-asset-command';
import { AuthService } from 'src/app/core/services/auth.service';
import { CryptoService } from 'src/app/core/services/crypto.service';
import { AccountService, AddCryptoAssetRequest } from 'src/app/core/services/account.service';

@Component({
  selector: 'app-add-crypto',
  imports: [
    FormsModule,
    NgbDatepickerModule],
  standalone: true,
  templateUrl: './add-crypto.component.html',
})
export class AddCryptoComponent implements OnInit {
  @ViewChild('modal') modal!: ElementRef;
  private accountService = inject(AccountService);
  private cryptoService = inject(CryptoService)
  private authService = inject(AuthService);
  cryptoCreated = output<AddCryptoAssetRequest>();
  loading = signal(false);
  selectedCoinMarketCapId = 0;
  btnColor = environment.btnColor;
  cryptoOptions = signal<Crypto[]>([]);

  ngOnInit(): void {
    this.selectedCoinMarketCapId = 0;
    this.getCryptos();
  }

  createCryptoAsset(): void {
    this.loading.set(true);
    const selectedCrypto = this.getCryptoById(this.selectedCoinMarketCapId);

    if (!selectedCrypto) {
      return;
    }

    let userId = this.authService.userId;
    if (!userId)
      return;

    const command: AddCryptoAssetRequest = {
      symbol: selectedCrypto.symbol,
      coinMarketCapId: selectedCrypto.coinMarketCapId,
    };

    this.accountService.addCryptoAsset(command)
      .subscribe({
        next: () => {
          this.cryptoCreated.emit(command);
          this.loading.set(false);
          this.selectedCoinMarketCapId = 0;
        },
        error: (err) => {
          this.loading.set(false);
        }
      });
  }

  private getCryptos() {
    this.cryptoService.getCryptos().subscribe({
      next: (response) => this.cryptoOptions.set(response.data)
    });
  }

  private getCryptoById(coinMarketCapId: number): Crypto | undefined {
    return this.cryptoOptions().find((x) => x.coinMarketCapId == coinMarketCapId);
  }
}
