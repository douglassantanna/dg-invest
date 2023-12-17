import { Injectable } from '@angular/core';
import { BehaviorSubject, fromEvent } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ScreenSizeService {
  private screenWidth$ = new BehaviorSubject<number>(window.innerWidth);

  constructor() {
    this.getUserScreenSize();
  }

  get getActualScreenSize(): number {
    return this.screenWidth$.value;
  }

  get screenSize(): number {
    return 768;
  }

  private getUserScreenSize() {
    fromEvent(window, 'resize')
      .subscribe(() => {
        this.screenWidth$.next(window.innerWidth);
      });
  }
}
