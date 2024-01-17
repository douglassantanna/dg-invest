export interface AppConfig {
  jwtToken: string | null,
  hideZeroBalance: boolean,
  viewType: DataViewEnum
}
export enum DataViewEnum {
  Table = 1,
  Card = 2
}
