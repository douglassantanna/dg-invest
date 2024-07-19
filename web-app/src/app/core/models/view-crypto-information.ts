export interface ViewCryptoInformation {
  symbol: string;
  pricePerUnit: number;
  balance: number;
  investedAmount: number;
  currentWorth: number;
  investmentGainLossValue: number;
  investmentGainLossPercentage: number;
  coinMarketCapId: number;
  id: number;
}
export interface UserCryptoAssetDto {
  accountBalance: number;
  cryptoAssetDto: ViewCryptoInformation;
}
