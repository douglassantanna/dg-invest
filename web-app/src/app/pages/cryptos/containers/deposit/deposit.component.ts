import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
export type Deposity = {
  type: number;
  amount: number;
  currentPrice: number;
  exchangeName: string;
  date: Date;
}
@Component({
  selector: 'app-deposit',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './deposit.component.html',
  styleUrl: './deposit.component.scss'
})
export class DepositComponent implements OnInit {
  private fb = inject(FormBuilder);
  deposityForm = this.fb.group({
    type: [0],
    amount: ['', [Validators.min(0)]],
    currentPrice: ['',],
    exchangeName: [''],
    date: [''],
  })

  ngOnInit(): void {
  }

  onSubmit() {
    console.log(this.deposityForm.value)
  }
}
