import { Injectable } from '@angular/core';
import { BehaviorSubject, fromEvent, takeUntil } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ScreenSizeService {
  private screenWidth$ = new BehaviorSubject<number>(window.innerWidth);

  constructor() {
    this.getUserScreenSize();
  }

  get screenSize(): number {
    return this.screenWidth$.value;
  }

  private getUserScreenSize() {
    fromEvent(window, 'resize')
      .subscribe(() => {
        this.screenWidth$.next(window.innerWidth);
      });
  }
}
