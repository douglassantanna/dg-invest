import { ETransactionType } from "./etransaction-type";

export interface CryptoTransactionHistory {
  id: number;
  amount: number;
  price: number;
  purchaseDate: number;
  exchangeName: string;
  transactionType: ETransactionType;
}
