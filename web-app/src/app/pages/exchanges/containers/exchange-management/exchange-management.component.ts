import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ExchangeService } from 'src/app/core/services/exchange.service';
import { BybitConnectionGroupDto, BybitSubaccountRowDto } from 'src/app/core/models/bybit-connection-group';
import { ModalComponent } from 'src/app/layout/modal/modal.component';

@Component({
  selector: 'app-exchange-management',
  standalone: true,
  imports: [CommonModule, FormsModule, ModalComponent],
  templateUrl: './exchange-management.component.html',
})
export class ExchangeManagementComponent implements OnInit {
  private exchangeService = inject(ExchangeService);

  groups: BybitConnectionGroupDto[] = [];
  loading = false;

  showModal = false;
  editingSubaccount: BybitSubaccountRowDto | null = null;
  selectedGroupId: string | null = null;

  formName = '';
  formUid = '';
  formApiKey = '';
  formApiSecret = '';
  formWebhookSecret = '';
  formSaving = false;

  testingAccountId: number | null = null;
  togglingAccountId: number | null = null;
  toastMessage = '';
  showToast = false;
  private toastTimer: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.loadGroups();
  }

  loadGroups(): void {
    this.loading = true;
    this.exchangeService.getBybitConnectionGroups().subscribe({
      next: (res) => {
        this.groups = res.data ?? [];
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  toggleGroup(groupId: string): void {
    const group = this.groups.find((g) => g.id === groupId);
    if (!group) return;
    (group as any).collapsed = !(group as any).collapsed;
  }

  isCollapsed(groupId: string): boolean {
    const group = this.groups.find((g) => g.id === groupId);
    return !!(group as any)?.collapsed;
  }

  statusMeta(status: string): { label: string; cls: string } {
    switch (status) {
      case 'ok':
        return { label: 'Connected', cls: 'ok' };
      case 'err':
        return { label: 'Error', cls: 'err' };
      case 'paused':
        return { label: 'Paused', cls: 'paused' };
      case 'pending':
        return { label: 'No key', cls: 'pending' };
      default:
        return { label: status, cls: 'paused' };
    }
  }

  okCount(subaccounts: BybitSubaccountRowDto[]): number {
    return subaccounts.filter((s) => s.status === 'ok').length;
  }

  errCount(subaccounts: BybitSubaccountRowDto[]): number {
    return subaccounts.filter((s) => s.status === 'err').length;
  }

  openAddModal(groupId: string): void {
    this.editingSubaccount = null;
    this.selectedGroupId = groupId;
    this.formName = '';
    this.formUid = '';
    this.formApiKey = '';
    this.formApiSecret = '';
    this.formWebhookSecret = '';
    this.showModal = true;
  }

  openEditModal(subaccount: BybitSubaccountRowDto, groupId: string): void {
    this.editingSubaccount = subaccount;
    this.selectedGroupId = groupId;
    this.formName = subaccount.name;
    this.formUid = subaccount.bybitUid ?? '';
    this.formApiKey = '';
    this.formApiSecret = '';
    this.formWebhookSecret = '';
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.editingSubaccount = null;
    this.selectedGroupId = null;
  }

  saveSubaccount(): void {
    const name = this.formName.trim();
    const uid = this.formUid.trim();
    if (!name || !uid) {
      this.flashToast('Please fill in name and UID');
      return;
    }

    const accountId = this.editingSubaccount?.accountId ?? 0;
    if (this.editingSubaccount && accountId === 0) {
      this.flashToast('Cannot determine account ID for edit');
      return;
    }

    this.formSaving = true;
    this.exchangeService
      .saveBybitCredentials(
        this.editingSubaccount ? accountId : 0,
        this.formApiKey,
        this.formApiSecret,
        this.formWebhookSecret,
        this.editingSubaccount ? undefined : name,
        this.editingSubaccount ? undefined : uid
      )
      .subscribe({
        next: (res) => {
          this.formSaving = false;
          if (res.isSuccess) {
            this.flashToast(
              this.editingSubaccount
                ? `${name} updated`
                : `${name} added`
            );
            this.closeModal();
            this.loadGroups();
          } else {
            this.flashToast(res.message);
          }
        },
        error: () => {
          this.formSaving = false;
          this.flashToast('Failed to save credentials');
        },
      });
  }

  testConnection(accountId: number, name: string): void {
    this.testingAccountId = accountId;
    this.flashToast(`Testing connection for "${name}"...`);
    this.exchangeService.testBybitConnection(accountId).subscribe({
      next: (res) => {
        this.testingAccountId = null;
        if (res.isSuccess) {
          this.flashToast(`"${name}" validated successfully`);
          this.loadGroups();
        } else {
          this.flashToast(`Failed to validate "${name}": ${res.message}`);
        }
      },
      error: () => {
        this.testingAccountId = null;
        this.flashToast(`Failed to validate "${name}"`);
      },
    });
  }

  toggleEnabled(accountId: number, name: string, currentEnabled: boolean): void {
    this.togglingAccountId = accountId;
    this.exchangeService.toggleBybitAccount(accountId).subscribe({
      next: () => {
        this.togglingAccountId = null;
        const newState = currentEnabled ? 'paused' : 'reactivated';
        // We could use the response data, but simpler: just show toast and reload
        this.flashToast(
          `${name} ${newState}`
        );
        this.loadGroups();
      },
      error: () => {
        this.togglingAccountId = null;
        this.flashToast(`Failed to toggle "${name}"`);
      },
    });
  }

  deleteSubaccount(accountId: number, name: string): void {
    if (!confirm(`Remove "${name}"? The subaccount and its API credentials will be removed.`)) {
      return;
    }
    this.exchangeService.deleteCredentials(accountId).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          this.flashToast(`${name} removed`);
          this.loadGroups();
        } else {
          this.flashToast(res.message);
        }
      },
      error: () => {
        this.flashToast(`Failed to remove "${name}"`);
      },
    });
  }

  copyWebhookUrl(webhookUrl: string): void {
    const baseUrl = window.location.origin;
    const fullUrl = `${baseUrl}${webhookUrl}`;
    navigator.clipboard.writeText(fullUrl).then(() => {
      this.flashToast('Webhook URL copied');
    }).catch(() => {
      this.flashToast('Failed to copy URL');
    });
  }

  private flashToast(message: string): void {
    this.toastMessage = message;
    this.showToast = true;
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => {
      this.showToast = false;
    }, 2500);
  }
}
