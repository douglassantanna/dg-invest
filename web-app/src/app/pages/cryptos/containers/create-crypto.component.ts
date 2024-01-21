import { BehaviorSubject, finalize } from 'rxjs';
import { CommonModule } from '@angular/common';
import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgbDatepickerModule, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { CryptoService } from '../../../core/services/crypto.service';
import { ToastService } from '../../../core/services/toast.service';
import { CreateCryptoAssetCommand } from 'src/app/core/models/create-crypto-asset-command';
import { Crypto } from 'src/app/core/models/crypto';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
  selector: 'app-create-crypto',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    NgbDatepickerModule],
  template: `
    <ng-template #content let-modal>
      <div class="modal-header">
        <h2 class="modal-title" id="modal-basic-title">New crypto asset</h2>
        <button type="button" class="btn-close" aria-label="Close" (click)="closeModal(modal)"></button>
      </div>
      <div class="modal-body">
        <form>
          <div class="mb-3">
          <select
            class="form-select"
            aria-label="Crypto selector"
            [(ngModel)]="selectedCoinMarketCapId"
            name="selectedValue"
            required>
            <option *ngFor="let crypto of cryptoOptions$ | async" [value]="crypto.coinMarketCapId" >
              {{ crypto.symbol }}
            </option>
          </select>
          </div>
        </form>
      </div>
      <div class="modal-footer">
        <button
          type="button"
          class="btn btn-primary"
          (click)="createCryptoAsset(selectedCoinMarketCapId, modal)"
          [disabled]="selectedCoinMarketCapId < 1" >Save
          <span *ngIf="loading" class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
        </button>
      </div>
    </ng-template>

    <button
      type="button"
      class="btn btn-primary"
      (click)="open(content)">
      Add asset
    </button>
  `,
  styles: [`
  `]
})
export class CreateCryptoComponent implements OnInit {
  @Output() cryptoCreated = new EventEmitter();
  selectedCoinMarketCapId = 0;
  loading: boolean = false;

  cryptoOptions$ = new BehaviorSubject<Crypto[]>([]);

  constructor(
    private modalService: NgbModal,
    private cryptoService: CryptoService,
    private toastService: ToastService,
    private authService: AuthService) {
  }
  ngOnInit(): void {
    this.getCryptos();
  }

  createCryptoAsset(selectedCoinMarketCapId: number, modalRef: any): void {
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
          this.cryptoCreated.emit();
          this.cryptoCreated.complete();
          modalRef.close();
        },
        error: (err) => {
          this.toastService.showError(err.error.message);
        }
      });
  }

  closeModal(modal: any) {
    modal.close();
  }

  open(content: any) {
    this.modalService.open(content);
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
