export interface SyncStatusDto {
  accountId: number;
  accountTag: string | null;
  exchangeName: string;
  status: string;
  lastSyncAt: string | null;
  lastOrderId: string | null;
  errorCount: number;
  lastErrorMessage: string | null;
}
