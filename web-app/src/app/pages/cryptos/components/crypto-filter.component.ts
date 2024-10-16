import { Component, EventEmitter, Input, OnDestroy, Output, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { LocalStorageService } from 'src/app/core/services/local-storage.service';
import { environment } from 'src/environments/environment.development';

@Component({
  selector: 'app-crypto-filter',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule],
  template: `
  <div class="row d-flex align-items-center">
    @if (displayHideCryptoCheck()) {
    <div class="col">
        <div class="form-check form-switch">
          <input class="form-check-input" [ngClass]="navbarColor" type="checkbox" id="gridCheck" [formControl]="showZeroBalance">
          <label class="form-check-label" for="gridCheck">
            Hide zero balance cryptos
          </label>
        </div>
      </div>
      <div class="col d-flex flex-grow-1">
        <input class="form-control" placeholder="Search by name.." aria-label="Search" type="text" [formControl]="searchControl">
      </div>
    } @else {
      <div class="col-12">
        <input class="form-control" placeholder="Search by name.." aria-label="Search" type="text" [formControl]="searchControl">
      </div>
    }
  </div>
  `,
})
export class CryptoFilterComponent implements OnDestroy {
  displayHideCryptoCheck = input<boolean>(true);
  @Output() searchControlEvent = new EventEmitter<string>();
  @Output() hideZeroBalanceControlEvent = new EventEmitter<boolean>(false);
  searchControl: FormControl = new FormControl();
  showZeroBalance: FormControl = new FormControl();
  @Input() setBalanceStatus = (value: boolean) => this.showZeroBalance.setValue(value);
  navbarColor = environment.navbarColor;
  private unsubscribe$: Subject<void> = new Subject<void>();

  constructor(localStorageService: LocalStorageService) {
    this.showZeroBalance.setValue(localStorageService.getHideZeroBalance());

    this.searchControl.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
      )
      .subscribe(value => {
        this.searchControlEvent.emit(value);
      });

    this.showZeroBalance.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
      ).subscribe(value => {
        this.hideZeroBalanceControlEvent.next(value);
      });
  }

  ngOnDestroy() {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }
}
