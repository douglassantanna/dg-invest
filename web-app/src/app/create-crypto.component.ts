import { BehaviorSubject } from 'rxjs';
import { CommonModule } from '@angular/common';
import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgbDatepickerModule, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { Crypto, CryptoService } from './services/crypto.service';


export interface CreateCryptoAssetCommand {
  crypto: string
  currency: string;
  coinMarketCapId: number;
}

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
        <button type="button" class="btn-close" aria-label="Close" (click)="modal.close()"></button>
      </div>
      <div class="modal-body">
        <form>
          <div class="mb-3">
          <select
            class="form-select"
            aria-label="Default select example"
            [(ngModel)]="selectedCoinMarketCapId"
            #selectedValue
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
          (click)="modal.close(createCryptoAsset(selectedCoinMarketCapId))"
          [disabled]="selectedCoinMarketCapId < 1" >Save</button>
      </div>
    </ng-template>

    <button
      type="button"
      class="btn btn-primary"
      (click)="open(content)">
      Add Crypto
    </button>
  `,
  styles: [`
  `]
})
export class CreateCryptoComponent implements OnInit {
  @Output() cryptoCreated = new EventEmitter();
  selectedCoinMarketCapId = 0;

  cryptoOptions$ = new BehaviorSubject<Crypto[]>([]);

  constructor(
    private modalService: NgbModal,
    private cryptoService: CryptoService) {
  }
  ngOnInit(): void {
    this.getCryptos();
  }

  createCryptoAsset(selectedCoinMarketCapId: number): void {
    const selectedCrypto = this.getCryptoById(selectedCoinMarketCapId);

    if (!selectedCrypto) {
      return;
    }

    const command: CreateCryptoAssetCommand = {
      crypto: selectedCrypto.symbol,
      currency: 'USD',
      coinMarketCapId: selectedCrypto.coinMarketCapId,
    };

    this.cryptoService.createCryptoAsset(command).subscribe((res) => {
      this.cryptoCreated.emit(res.isSuccess);
      this.cryptoCreated.complete();
    });
  }

  open(content: any) {
    const modalRef = this.modalService.open(content);
    modalRef.result.then(result => {
      if (result) {
      }
    })
  }

  getCryptos() {
    this.cryptoService.getCryptos().subscribe(response => {
      this.cryptoOptions$.next(response.data);
    });
  }

  private getCryptoById(coinMarketCapId: number): Crypto | undefined {
    return this.cryptoOptions$.value.find((x) => x.coinMarketCapId == coinMarketCapId);
  }
}
