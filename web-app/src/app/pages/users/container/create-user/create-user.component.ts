import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormControl, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-create-user',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule
  ],
  templateUrl: './create-user.component.html',
  styleUrls: ['./create-user.component.scss']
})
export class CreateUserComponent {
  loading = false;
  title = 'New user';
  roleId = 0;
  roles = [{ id: 1, name: 'admin' }, { id: 2, name: 'user' }];
  createUserForm = this.fb.group({
    fullName: ['', [Validators.minLength(2), Validators.maxLength(225)]],
    email: ['', [Validators.email]],
    role: [''],
  })

  constructor(
    private modalService: NgbModal,
    private fb: FormBuilder
  ) { }
  open(content: any) {
    this.modalService.open(content);
  }
  closeModal(modal: any) {
    modal.close();
  }
  createCryptoAsset(id: number, modal: any) {
    console.log(this.createUserForm.value)
  }

  get fullName() {
    return this.createUserForm.get('fullName') as FormControl;
  }
  get email() {
    return this.createUserForm.get('email') as FormControl;
  }
}
