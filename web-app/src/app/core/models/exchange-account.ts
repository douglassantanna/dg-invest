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
  accountName: string;
  connections: ExchangeConnectionDto[];
}

export interface ExchangeTransactionDto {
  id: number;
  date: string;
  type: string;
  asset: string;
  amount: number;
  price: number;
  fee: number;
  exchangeName: string | null;
  exchangeStatus: string | null;
  notes: string;
}
