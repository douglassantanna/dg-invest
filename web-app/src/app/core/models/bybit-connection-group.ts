export interface BybitConnectionGroupDto {
  id: string;
  name: string;
  subaccountCount: number;
  maxSubaccounts: number;
  subaccounts: BybitSubaccountRowDto[];
  integrationStatus?: string;
  integrationEnabled?: boolean;
  consecutiveTransportFailures?: number;
  lastErrorCode?: string | null;
  lastErrorMessage?: string | null;
  lastErrorEndpoint?: string | null;
  lastErrorAccountId?: number | null;
  autoPausedAt?: string | null;
}

export interface BybitSubaccountRowDto {
  accountId: number;
  name: string;
  externalId: string | null;
  status: string;
  hasApiKey: boolean;
  hasApiSecret: boolean;
  hasWebhookSecret: boolean;
  maskedApiKey: string | null;
  webhookUrl: string;
  lastVerifiedAt: string | null;
  isEnabled: boolean;
  isMaster: boolean;
}
