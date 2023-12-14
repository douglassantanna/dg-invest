import { Component, EventEmitter, OnDestroy, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';

@Component({
  selector: 'app-crypto-filter',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule],
  template: `
  <div class="col">
    <input class="form-control" placeholder="Search by name.." aria-label="Search" type="text" [formControl]="searchControl">
  </div>
  `,
})
export class CryptoFilterComponent implements OnDestroy {
  @Output() searchControlEvent = new EventEmitter<string>();
  searchControl: FormControl = new FormControl();
  private unsubscribe$: Subject<void> = new Subject<void>();

  constructor() {
    this.searchControl.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
      )
      .subscribe(value => {
        this.searchControlEvent.emit(value);
      });
  }

  ngOnDestroy() {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }
}
