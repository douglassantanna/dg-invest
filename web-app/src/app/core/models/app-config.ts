export interface AppConfig {
  jwtToken: string | null,
  hideZeroBalance: boolean,
  viewType: DataView
}
export enum DataView {
  Table = 1,
  Card = 2
}
