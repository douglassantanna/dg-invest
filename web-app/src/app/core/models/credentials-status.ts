export interface CredentialsStatusDto {
  accountId: number;
  accountName: string;
  hasApiKey: boolean;
  hasApiSecret: boolean;
  hasWebhookSecret: boolean;
}
