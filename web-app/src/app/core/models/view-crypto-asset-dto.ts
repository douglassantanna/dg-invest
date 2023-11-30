import { CryptoAssetData } from "./crypto-asset-data";
import { CryptoInformation } from "./crypto-information";
import { ViewAddressDto } from "./view-address-dto";
import { ViewCryptoTransactionDto } from "./view-crypto-transaction-dto";

export interface ViewCryptoAssetDto {
  id: number;
  cryptoAssetData: CryptoAssetData[];
  cryptoInformation: CryptoInformation;
  transactions: ViewCryptoTransactionDto[];
  addresses: ViewAddressDto[];
}
