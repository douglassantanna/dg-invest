import { Component, ElementRef, inject, output, ViewChild, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgbDatepickerModule } from '@ng-bootstrap/ng-bootstrap';
import { ViewCryptoDto } from 'src/app/core/models/crypto';
import { AuthService } from 'src/app/core/services/auth.service';
import { CryptoService } from 'src/app/core/services/crypto.service';
import { AccountService, AddCryptoAssetRequest } from 'src/app/core/services/account.service';
import { UpperCasePipe } from '@angular/common';

@Component({
  selector: 'app-add-crypto',
  imports: [
    FormsModule,
    NgbDatepickerModule,
    UpperCasePipe],
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
  cryptoOptions = signal<ViewCryptoDto[]>([]);
  selectedCrypto: ViewCryptoDto | null = null;
  isDropdownVisible = false;

  ngOnInit(): void {
    this.getCryptos();
  }

  toggleDropdown() {
    this.isDropdownVisible = !this.isDropdownVisible;
  }

  selectCrypto(crypto: ViewCryptoDto) {
    this.isDropdownVisible = false;
    this.selectedCrypto = crypto;
  }

  createCryptoAsset(): void {
    this.loading.set(true);
    if (!this.selectedCrypto) {
      return;
    }

    let userId = this.authService.userId;
    if (!userId)
      return;

    const command: AddCryptoAssetRequest = {
      symbol: this.selectedCrypto.symbol,
      coinMarketCapId: this.selectedCrypto.coinMarketCapId,
    };

    this.accountService.addCryptoAsset(command)
      .subscribe({
        next: () => {
          this.cryptoCreated.emit(command);
          this.loading.set(false);
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

}
