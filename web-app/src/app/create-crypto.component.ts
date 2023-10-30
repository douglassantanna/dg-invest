import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ModalDismissReasons, NgbDatepickerModule, NgbModal } from '@ng-bootstrap/ng-bootstrap';


interface CryptoPurchase {
  cryptoName: string;
  amount: number;
  currency: string;
  date: Date;
  pricePerUnit: number;
  exchangeName: string;
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
            name="closeResult">
            <option selected *ngFor="let item of cryptoOptions" [value]="item" >
            {{ item }}
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
export class CreateCryptoComponent {
  closeResult = '';

  constructor(private modalService: NgbModal) { }

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
  currenciesOptions: any[] = [
    'BRL',
    'USD',
  ]
  cancel() {
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


  private getDismissReason(reason: any): string {
    if (reason === ModalDismissReasons.ESC) {
      return 'by pressing ESC';
    } else if (reason === ModalDismissReasons.BACKDROP_CLICK) {
      return 'by clicking on a backdrop';
    } else {
      return `with: ${reason}`;
    }
  }

}
