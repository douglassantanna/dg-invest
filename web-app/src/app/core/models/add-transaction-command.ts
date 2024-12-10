import { ETransactionType } from "./etransaction-type";

export interface AddTransactionCommand {
  amount: number;
  price: number;
  purchaseDate: Date;
  exchangeName: string;
  transactionType: ETransactionType;
  cryptoAssetId: number;
  fee: number;
}
