import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormControl, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { UserService } from 'src/app/core/services/user.service';
import { CreateUserCommand, Role } from 'src/app/core/models/create-user';
import { finalize } from 'rxjs';
import { ToastService } from 'src/app/core/services/toast.service';
import { environment } from 'src/environments/environment.development';

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
  btnColor = environment.btnColor;
  loading = false;
  title = 'New user';
  roleId = 0;
  roles = [{ id: 1, name: 'admin' }, { id: 2, name: 'user' }];
  createUserForm = this.fb.group({
    fullName: ['', [Validators.minLength(2), Validators.maxLength(225)]],
    email: ['', [Validators.email]],
    role: [0],
  });
  @Output() userCreated = new EventEmitter<CreateUserCommand>();


  constructor(
    private modalService: NgbModal,
    private fb: FormBuilder,
    private userService: UserService,
    private toastService: ToastService
  ) { }
  open(content: any) {
    this.modalService.open(content);
  }
  closeModal(modal: any) {
    modal.close();
  }
  createCryptoAsset(modalRef: any) {
    this.loading = true;

    const role = this.getRole();
    if (role === undefined)
      return;

    const command: CreateUserCommand = {
      fullName: this.fullName.value,
      email: this.email.value,
      role: role
    };

    this.userService.createUser(command)
      .pipe(
        finalize(() => {
          this.loading = false;
        })
      )
      .subscribe({
        next: () => {
          modalRef.close();
          this.userCreated.emit(command);
          this.userCreated.complete();
          this.toastService.showSuccess('User added successfully');
        },
        error: (err) => {
          this.toastService.showError(err.error.message);
        }
      });
  }

  get fullName() {
    return this.createUserForm.get('fullName') as FormControl;
  }
  get email() {
    return this.createUserForm.get('email') as FormControl;
  }

  private getRole(): Role {
    return this.createUserForm.value?.role == 1 ? Role.Admin : Role.User;
  }
}
