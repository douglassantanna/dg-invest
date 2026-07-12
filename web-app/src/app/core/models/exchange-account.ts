export interface ExchangeAccountDto {
  accountId: number;
  accountTag: string;
  exchangeName: string;
  status: string;
  lastSyncAt: string | null;
  errorCount: number;
  lastErrorMessage: string | null;
}
