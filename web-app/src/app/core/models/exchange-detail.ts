export interface ExchangeConnectionDto {
  exchangeName: string;
  status: string;
  lastSyncAt: string | null;
  errorCount: number;
  lastErrorMessage: string | null;
  hasApiKey: boolean;
  hasApiSecret: boolean;
  hasWebhookSecret: boolean;
}

export interface ExchangeAccountDetailDto {
  accountId: number;
  accountTag: string;
  connections: ExchangeConnectionDto[];
}
