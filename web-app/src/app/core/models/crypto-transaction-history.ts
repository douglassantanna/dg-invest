import { ETransactionType } from "./etransaction-type";

export interface CryptoTransactionHistory {
  id?: number;
  amount: number;
  price: number;
  purchaseDate: Date;
  exchangeName: string;
  transactionType: ETransactionType;
}
