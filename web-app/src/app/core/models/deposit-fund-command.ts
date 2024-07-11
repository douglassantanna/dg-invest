export interface DepositFundCommand {
  accountTransactionType: AccountTransactionType;
  amount: number;
  currentPrice: number;
  cryptoAsset: string;
  exchangeName: string;
  date: string;
}

export enum AccountTransactionType {
  DepositFiat = 1,
  DepositCrypto = 2,
  WithdrawToBank = 3,
  In = 4,
  Out = 5
}
