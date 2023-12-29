import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-view-users',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './view-users.component.html',
  styleUrls: ['./view-users.component.scss']
})
export class ViewUsersComponent implements OnInit {
  constructor() { }

  users: any = [
    {
      name: "John Doe",
      email: "john@example.com",
      role: 1,
      emailConfirmed: true,
    },
    {
      name: "Jane Smith",
      email: "jane@example.com",
      role: 2,
      emailConfirmed: false,
    }
  ];

  ngOnInit(): void {
  }

}
