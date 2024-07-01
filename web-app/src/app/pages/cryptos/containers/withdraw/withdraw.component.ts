import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

@Component({
  selector: 'app-withdraw',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './withdraw.component.html',
  styleUrl: './withdraw.component.scss'
})
export class WithdrawComponent {
  private fb = inject(FormBuilder);
  withdrawForm = this.fb.group({
    amount: ['', Validators.required],
    currency: ['', Validators.required],
    destination: ['', Validators.required],
    date: ['', Validators.required],
    notes: ['']
  });
  onSubmit() {
    console.log(this.withdrawForm.value)
  }
}
