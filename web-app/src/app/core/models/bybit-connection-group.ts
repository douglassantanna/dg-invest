export interface BybitConnectionGroupDto {
  id: string;
  name: string;
  subaccountCount: number;
  maxSubaccounts: number;
  subaccounts: BybitSubaccountRowDto[];
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
}
