export interface ViewCryptoInformation {
  symbol: string;
  pricePerUnit: number;
  balance: number;
  investedAmount: number;
  currentWorth: number;
  investmentGainLossValue: number;
  investmentGainLossPercentage: number;
  coinMarketCapId: number;
  image: string;
  id: number;
}
export interface UserCryptoAssetDto {
  accountBalance: number;
  accountTag: string;
  cryptoAssetDto: ViewCryptoInformation[];
  totalDeposited: number;
}
