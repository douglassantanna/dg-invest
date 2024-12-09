import { Component, ElementRef, inject, OnDestroy, OnInit, output } from '@angular/core';

@Component({
  selector: 'app-modal',
  templateUrl: './modal.component.html',
  standalone: true,
  imports: []
})
export class ModalComponent implements OnInit, OnDestroy {
  private el = inject(ElementRef);
  close = output();

  ngOnInit(): void {
    document.body.appendChild(this.el.nativeElement);
  }

  ngOnDestroy(): void {
    this.el.nativeElement.remove();
  }

  onCloseClick() {
    this.close.emit();
  }
}
