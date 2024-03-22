import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BehaviorSubject, finalize } from 'rxjs';
import { CreateCryptoAssetCommand } from 'src/app/core/models/create-crypto-asset-command';
import { AuthService } from 'src/app/core/services/auth.service';
import { CryptoService } from 'src/app/core/services/crypto.service';
import { ToastService } from 'src/app/core/services/toast.service';
import { Crypto } from 'src/app/core/models/crypto';
import { FormsModule } from '@angular/forms';
import { ButtonComponent } from 'src/app/layout/button/button.component';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-create-asset',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonComponent,
    MatFormFieldModule,
    MatSelectModule],
  templateUrl: './create-asset.component.html'
})
export class CreateAssetComponent implements OnInit {
  selectedCoinMarketCapId = 0;
  loading: boolean = false;
  cryptoOptions$ = new BehaviorSubject<Crypto[]>([]);

  constructor(
    private cryptoService: CryptoService,
    private toastService: ToastService,
    private authService: AuthService,
    private dialogRef: MatDialogRef<CreateAssetComponent>) {
  }
  ngOnInit(): void {
    this.getCryptos();
  }

  createCryptoAsset(selectedCoinMarketCapId: number): void {
    this.loading = true;
    const selectedCrypto = this.getCryptoById(selectedCoinMarketCapId);

    if (!selectedCrypto) {
      return;
    }

    let userId = this.authService.userId;
    if (!userId)
      return;

    const command: CreateCryptoAssetCommand = {
      crypto: selectedCrypto.symbol,
      currency: 'USD',
      coinMarketCapId: selectedCrypto.coinMarketCapId,
      userId: parseInt(userId)
    };

    this.cryptoService.createCryptoAsset(command)
      .pipe(
        finalize(() => {
          this.loading = false;
        })
      ).subscribe({
        next: () => {
          this.closeModal();
        },
        error: (err) => {
          this.toastService.showError(err.error.message);
        }
      });
  }

  closeModal() {
    this.selectedCoinMarketCapId = 0;
    this.dialogRef.close();
  }

  private getCryptos() {
    this.cryptoService.getCryptos().subscribe({
      next: (response) => this.cryptoOptions$.next(response.data)
    });
  }

  private getCryptoById(coinMarketCapId: number): Crypto | undefined {
    return this.cryptoOptions$.value.find((x) => x.coinMarketCapId == coinMarketCapId);
  }
}
