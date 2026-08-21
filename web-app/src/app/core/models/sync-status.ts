export interface SyncStatusDto {
  accountId: number;
  accountName: string | null;
  exchangeName: string;
  status: string;
  lastSyncAt: string | null;
  lastOrderId: string | null;
  errorCount: number;
  lastErrorMessage: string | null;
}
