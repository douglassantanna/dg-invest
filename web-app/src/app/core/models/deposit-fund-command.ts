export interface DepositFundCommand {
  accountTransactionType: AccountTransactionType;
  amount: number;
  currentPrice: number;
  cryptoAssetId: string;
  exchangeName: string;
  date: string;
  notes: string;
  subAccountTag: string;
}

export interface WithdrawFundCommand {
  amount: number;
  date: string;
  notes: string;
}

export enum AccountTransactionType {
  DepositFiat = 1,
  DepositCrypto = 2,
  WithdrawToBank = 3,
  In = 4,
  Out = 5
}
