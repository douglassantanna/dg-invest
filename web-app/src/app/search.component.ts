import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, filter } from 'rxjs';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
  <div class="row g-3">
    <div class="col-sm">
      <input (keyup)="apply($event)" id="search" name="search" type="text" class="form-control" placeholder="Search by name.." aria-label="Search">
    </div>
  </div>
  `
})
export class SearchComponent {
  protected readonly search = new FormControl('');
  //@Output() searched = this.search.valueChanges.pipe(
  //debounceTime(500),
  //distinctUntilChanged(),
  //filter((value: any) => value),
  //)
  @Output() searched = new EventEmitter();

  apply(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    //console.log(filterValue);

  }
}
