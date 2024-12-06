import { CommonModule } from '@angular/common';
import { Component, ElementRef, EventEmitter, Output, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgbDatepickerModule } from '@ng-bootstrap/ng-bootstrap';
import { BehaviorSubject, finalize } from 'rxjs';
import { environment } from 'src/environments/environment.development';
import { Crypto } from 'src/app/core/models/crypto';
import { CreateCryptoAssetCommand } from 'src/app/core/models/create-crypto-asset-command';
import { AuthService } from 'src/app/core/services/auth.service';
import { CryptoService } from 'src/app/core/services/crypto.service';
import { ToastService } from 'src/app/core/services/toast.service';

@Component({
  selector: 'app-add-crypto',
  imports: [
    CommonModule,
    FormsModule,
    NgbDatepickerModule],
  standalone: true,
  templateUrl: './add-crypto.component.html',
})
export class AddCryptoComponent {
  @ViewChild('modal') modal!: ElementRef;
  @Output() cryptoCreated = new EventEmitter();
  loading: boolean = false;
  selectedCoinMarketCapId = 0;
  btnColor = environment.btnColor;
  cryptoOptions$ = new BehaviorSubject<Crypto[]>([]);

  constructor(
    private cryptoService: CryptoService,
    private toastService: ToastService,
    private authService: AuthService) {
  }

  createCryptoAsset(): void {
    this.loading = true;
    const selectedCrypto = this.getCryptoById(this.selectedCoinMarketCapId);

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
      .subscribe({
        next: () => {
          this.toastService.showSuccess('Crypto asset created successfully');
          this.cryptoCreated.emit();
          this.cryptoCreated.complete();
          this.closeModal();
          this.loading = false;
          this.selectedCoinMarketCapId = 0;
        },
        error: (err) => {
          this.loading = false;
          this.toastService.showError(err.error.message);
        }
      });
  }

  open() {
    this.getCryptos();
    this.modal.nativeElement.classList.remove('hidden');
    document.body.classList.add('overflow-hidden');
  }

  closeModal() {
    this.modal.nativeElement.classList.add('hidden');
    document.body.classList.remove('overflow-hidden');
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
