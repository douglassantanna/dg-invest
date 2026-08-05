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
