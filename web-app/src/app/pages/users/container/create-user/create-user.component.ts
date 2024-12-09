import { Component, output, signal, inject } from '@angular/core';
import { FormBuilder, FormControl, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { UserService } from 'src/app/core/services/user.service';
import { CreateUserCommand, Role } from 'src/app/core/models/create-user';
import { environment } from 'src/environments/environment.development';

@Component({
  selector: 'app-create-user',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule
  ],
  templateUrl: './create-user.component.html',
  styleUrls: ['./create-user.component.scss']
})
export class CreateUserComponent {
  private fb = inject(FormBuilder);
  private userService = inject(UserService);
  userCreated = output<CreateUserCommand>();
  btnColor = environment.btnColor;
  loading = signal(false);
  title = 'New user';
  roleId = 0;
  roles = [{ id: 1, name: 'admin' }, { id: 2, name: 'user' }];
  createUserForm = this.fb.group({
    fullName: ['', [Validators.minLength(2), Validators.maxLength(225)]],
    email: ['', [Validators.email]],
    role: [0],
  });

  submit() {
    this.loading.set(true);

    const role = this.getRole();
    if (role === undefined) {
      return;
    }

    const command: CreateUserCommand = {
      fullName: this.fullName.value,
      email: this.email.value,
      role: role
    };

    this.userService.createUser(command)
      .subscribe({
        next: () => {
          this.loading.set(false);
          this.userCreated.emit(command);
        },
        error: (err) => {
          this.loading.set(false);
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
