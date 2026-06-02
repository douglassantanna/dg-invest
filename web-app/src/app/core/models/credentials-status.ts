export interface CredentialsStatusDto {
  accountId: number;
  accountTag: string;
  hasApiKey: boolean;
  hasApiSecret: boolean;
  hasWebhookSecret: boolean;
}
