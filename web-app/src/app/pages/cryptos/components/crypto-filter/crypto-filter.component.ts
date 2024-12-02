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
  templateUrl: './crypto-filter.component.html',
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
