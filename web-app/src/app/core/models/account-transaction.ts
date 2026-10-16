import { AccountTransactionType } from './deposit-fund-command';

export interface AccountTransaction {
  imageUrl: string;
  transactionType: AccountTransactionType;
  transactionValue: number;
  date: Date;
  notes: string;
  cryptoAmount?: number;
  cryptoCurrentPrice?: number;
  cryptoSymbol?: string;
  fee?: number;
}
