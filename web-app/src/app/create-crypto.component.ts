import { BehaviorSubject } from 'rxjs';
import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
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
        <h4 class="modal-title" id="modal-basic-title">New crypto asset</h4>
        <button type="button" class="btn-close" aria-label="Close" (click)="modal.dismiss('Cross click')"></button>
      </div>
      <div class="modal-body">
        <form>
          <div class="mb-3">
          <select
            class="form-select"
            aria-label="Default select example"
            [(ngModel)]="closeResult"
            name="closeResult"
            required>
            <option *ngFor="let crypto of cryptoOptions$ | async" [value]="crypto.coinMarketCapId" >
              {{ crypto.symbol }}
            </option>
          </select>
          </div>
        </form>
      </div>
      <div class="modal-footer">
        <button type="button" class="btn btn-outline-dark" (click)="modal.close('Save click')">Save</button>
      </div>
    </ng-template>

    <button
      type="button"
      class="btn btn-lg btn-primary"
      (click)="open(content)">
      Add Crypto
    </button>
  `,
  styles: [`
  `]
})
export class CreateCryptoComponent implements OnInit {
  closeResult = '';
  cryptoOptions$ = new BehaviorSubject<Crypto[]>([]);

  constructor(
    private modalService: NgbModal,
    private cryptoService: CryptoService) {
  }
  ngOnInit(): void {
    this.getCryptos();
  }

  save() { }

  open(content: any) {
    this.modalService.open(content, { ariaLabelledBy: 'modal-basic-title' }).result.then(
      (result) => {
        console.log('closeResult', this.closeResult);
        console.log('result', result);
      },
      (reason) => {
        console.log('closeResult', this.closeResult);
        console.log('reason', reason);
      },
    );
  }

  getCryptos() {
    this.cryptoService.getCryptos().subscribe(response => {
      this.cryptoOptions$.next(response.data);
    });
  }
}
