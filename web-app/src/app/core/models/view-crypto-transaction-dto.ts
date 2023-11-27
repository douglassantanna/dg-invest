import { ETransactionType } from "./etransaction-type";

export interface ViewCryptoTransactionDto {
  amount: number;
  price: number;
  purchaseDate: Date;
  exchangeName: string;
  transactionType: ETransactionType;
}
