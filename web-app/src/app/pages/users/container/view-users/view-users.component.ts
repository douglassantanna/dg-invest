import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CreateUserComponent } from '../create-user/create-user.component';
import { UserService } from 'src/app/core/services/user.service';
import { BehaviorSubject, Subject, takeUntil } from 'rxjs';
import { ViewUserDto } from 'src/app/core/models/view-user-dto';
import { ToastService } from 'src/app/core/services/toast.service';
import { Pagination } from 'src/app/core/models/pagination';

@Component({
  selector: 'app-view-users',
  standalone: true,
  imports: [
    CommonModule,
    CreateUserComponent],
  templateUrl: './view-users.component.html',
})
export class ViewUsersComponent implements OnInit, OnDestroy {
  users$: BehaviorSubject<ViewUserDto[]> = new BehaviorSubject<ViewUserDto[]>([]);
  private unsubscribe$: Subject<void> = new Subject<void>();

  constructor(
    private userService: UserService,
    private toastService: ToastService) { }

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

  addUserToTable(user: any) {
    this.users$.next([...this.users$.value, user]);
    this.toastService.showSuccess("User added successfully");
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
          this.toastService.showError("No user found with given name");
        },
      });
  }
}
