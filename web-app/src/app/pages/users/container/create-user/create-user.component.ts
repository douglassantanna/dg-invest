import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-create-user',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './create-user.component.html',
  styleUrls: ['./create-user.component.scss']
})
export class CreateUserComponent {
  loading = false;
  title = 'New user';
  roleId = 0;
  roles = [{ id: 1, name: 'admin' }, { id: 2, name: 'user' }];
  constructor(
    private modalService: NgbModal,

  ) { }
  open(content: any) {
    this.modalService.open(content);
  }
  closeModal(modal: any) {
    modal.close();
  }
  createCryptoAsset(id: number, modal: any) {

  }
}
