export type AccountHealthStatus = 'Connected' | 'NeedsAttention' | 'Paused' | 'NotConfigured';

export interface AccountSelectorOption {
  accountId: number;
  accountName: string;
  balance: number;
  isSelected: boolean;
  accountType: 'Manual' | 'Exchange';
  exchangeName: string | null;
  isBybitCombinedWallet: boolean;
  healthStatus: AccountHealthStatus | null;
  lastSyncAt: string | null;
  errorCount: number;
  lastErrorMessage: string | null;
}
