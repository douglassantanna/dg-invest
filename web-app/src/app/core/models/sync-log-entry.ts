export interface SyncLogEntry {
  id: string;
  userId: number;
  accountId: number;
  exchangeName: string;
  orderId: string;
  symbol: string;
  side: string;
  qty: number;
  price: number;
  status: string;
  errorMessage: string | null;
  timestamp: string;
  importSource: string;
}
