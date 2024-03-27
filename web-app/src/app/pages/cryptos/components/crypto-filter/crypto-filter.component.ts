import { Component, EventEmitter, Input, OnDestroy, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { LocalStorageService } from 'src/app/core/services/local-storage.service';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatBottomSheetModule, MatBottomSheetRef } from '@angular/material/bottom-sheet';
@Component({
  selector: 'app-crypto-filter',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatSelectModule,
    MatFormFieldModule,
    MatInputModule,
    MatSlideToggleModule,
    MatIconModule,
    MatButtonModule,
    MatExpansionModule,
    MatBottomSheetModule],
  templateUrl: 'crypto-filter.component.html',
})
export class CryptoFilterComponent implements OnDestroy {
  @Output() searchControlEvent = new EventEmitter<string>();
  @Output() hideZeroBalanceControlEvent = new EventEmitter<boolean>(false);
  @Input() setBalanceStatus = (value: boolean) => this.showZeroBalance.setValue(value);
  showFilter: boolean = false;
  searchControl: FormControl = new FormControl();
  showZeroBalance: FormControl = new FormControl();

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
