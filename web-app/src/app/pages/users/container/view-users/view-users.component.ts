import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { CreateUserComponent } from '../create-user/create-user.component';
import { UserService } from 'src/app/core/services/user.service';
import { BehaviorSubject, Subject, takeUntil } from 'rxjs';
import { ViewUserDto } from 'src/app/core/models/view-user-dto';
import { Pagination } from 'src/app/core/models/pagination';
import { ModalComponent } from 'src/app/layout/modal/modal.component';
import { AsyncPipe, NgClass } from '@angular/common';

@Component({
  selector: 'app-view-users',
  standalone: true,
  imports: [
    NgClass,
    AsyncPipe,
    CreateUserComponent,
    ModalComponent],
  templateUrl: './view-users.component.html',
})
export class ViewUsersComponent implements OnInit, OnDestroy {
  private readonly userService = inject(UserService);
  private unsubscribe$: Subject<void> = new Subject<void>();
  users$: BehaviorSubject<ViewUserDto[]> = new BehaviorSubject<ViewUserDto[]>([]);
  isModalOpen = signal(false);

  ngOnInit(): void {
    this.loadUsers();
  }

  ngOnDestroy() {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }

  loadUsers(
    page: number = 1,
    pageSize: number = 50,
    fullName: string = "",
    sortOrder: string = "ASC",
  ) {
    this.userService.getUsers(page, pageSize, fullName, sortOrder).subscribe(res => {
      this.users$.next(res.items);
    })
  }

  search(input: string) {
    this.userService.getUsers(1, 50, input, "ASC")
      .pipe(
        takeUntil(this.unsubscribe$)
      ).subscribe({
        next: (users: Pagination<ViewUserDto>) => {
          this.users$.next(users.items);
        },
        error: () => {
          console.log("No user found with given name");
        },
      });
  }

  toggleCreateUserModal(user: any = null) {
    if (user) {
      this.addUserToTable(user);
    }
    this.isModalOpen.set(!this.isModalOpen());
  }

  private addUserToTable(user: any) {
    this.users$.next([...this.users$.value, user]);
    console.log("User added successfully");
  }
}
