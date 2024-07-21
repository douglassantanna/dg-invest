import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AccountTransactionCardComponent } from './account-transaction-card.component';

describe('AccountTransactionCardComponent', () => {
  let component: AccountTransactionCardComponent;
  let fixture: ComponentFixture<AccountTransactionCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AccountTransactionCardComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(AccountTransactionCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
