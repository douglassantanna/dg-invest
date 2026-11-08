# Azure infrastructure deployment

Infrastructure is deployed to the existing `TestDouglas` resource group in the MPN subscription. PROD and STAGE are test environments; application resources are in `westeurope`, while the shared SQL server and databases are in `francecentral`. Application code deployments remain separate.

## Deploy with GitHub Actions

1. Configure the GitHub Environment `azure-infra-sandbox` with the existing OIDC variables `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID`.
2. Add the SQL logical server administrator password as the Environment secret `SQL_ADMIN_PASSWORD`. The `.bicepparam` file reads it from the workflow process environment; never put the password value in a repository file.
3. Before deploying, verify the resource providers required by the template are registered in the subscription identified by `AZURE_SUBSCRIPTION_ID`. This includes `Microsoft.Insights`, `Microsoft.OperationalInsights`, `Microsoft.KeyVault`, `Microsoft.Web`, `Microsoft.Sql`, and `Microsoft.Storage`. `Microsoft.Insights` and `Microsoft.OperationalInsights` are separate providers. Register any missing provider at subscription scope.
4. Run **Deploy Azure Infrastructure** with `stage` or `prod` and leave `apply` disabled first. Review validation and `what-if` output.
5. Run it again with `apply` enabled only after reviewing the changes.

### SQL network access

The Function App runs on Flex Consumption, which has no stable outbound IP addresses, so per-IP SQL firewall rules go stale as Azure scales the app. Bicep instead declares the special `AllowAllWindowsAzureIps` rule (`0.0.0.0`, shown in the portal as "Allow Azure services and resources to access this server"). It costs nothing and needs no maintenance.

Trade-off: any Azure-hosted client, including other tenants' resources, can reach the SQL endpoint; it still has to authenticate. Keep the application credentials least-privilege and in Key Vault. A NAT Gateway static IP (about $35+/month) or a Private Endpoint with VNet integration would give a tighter boundary, but neither fits this app's cost target.

When `apply` is enabled, the workflow also deletes the obsolete `<env>-app-<n>` rules that earlier deployments created from collected outbound IPs. Other rules, such as developer IPs, are left alone.

### Log Analytics daily cap

The shared workspace has a daily ingestion cap (`logAnalyticsDailyQuotaGb`, default `0.15` GB). When the cap is reached, ingestion stops until the next UTC day, so telemetry for the rest of that day is lost. Raise it, or set `-1`, while investigating an incident.

The workflow serializes PROD and STAGE deployments because both update shared resources: the Windows F1 App Service plan and Log Analytics workspace in `westeurope`, plus the SQL logical server in `francecentral`. Keep `sqlLocation` consistent in both parameter files.

## Runtime secrets and permissions

Bicep creates Key Vaults and app settings containing versionless Key Vault references. It never creates or exports secret values. After deployment, add the following secret names to the environment-specific Key Vault. Secret values are supplied by the owner and must not be placed in Git:

- `SqlConnectionString` — connection string for that environment database.
- `StorageConnectionString` — connection string for that environment storage account. The application currently uses the storage SDK with a connection string, so shared-key access remains enabled on these test accounts.
- `MigrationToken` — same random token as the GitHub Environment secret `MIGRATION_TOKEN`; Bicep exposes it to the API as a versionless Key Vault reference.
- `JwtSigningSecret`
- `CoinMarketCapApiKey`
- `MailtrapHost`, `MailtrapPort`, `MailtrapUsername`, `MailtrapPassword`, and `MailtrapFrom`

The API reads `ConnectionStrings__DefaultConnection`; the Functions project currently reads `DefaultConnection`. Both read `AzureStorageSettings__ConnectionString`.

Each Function App is explicitly provisioned with `BybitSync__Enabled=true`; it begins scheduled synchronization after its code, Key Vault permissions/secrets, and database connection are configured. Set the app setting to `false` if an environment must remain paused.

Database health-check message settings are provisioned by Bicep. The timer-based `HealthPing` Function remains disabled by default (`HealthPingOptions__RunFunction=false`); do not copy the local placeholder endpoint or local Function key into Azure. Enable it only after setting the correct API endpoint and a dedicated shared Key Vault secret for its function key.

Application Insights resources and connection strings are provisioned, but the API project does not currently include the Application Insights SDK and the Functions SDK references are commented out. Confirm telemetry collection during application validation; code instrumentation changes remain separate from infrastructure deployment.

### Key Vaults

Each environment has two vaults, both created by Bicep:

| Vault | Contents |
|---|---|
| `dginvest-<env>-sec-weu1` (app vault) | App secrets: `SqlConnectionString`, `StorageConnectionString`, `JwtSigningSecret`, `MigrationToken`, CoinMarketCap and Mailtrap settings. |
| `dginvest-<env>-cred-weu1` (credential vault) | User-owned Bybit credentials (`bybit-*` secrets). The API and Function App reach it through `BybitKeyVaultSettings__VaultUri`. |

The credential vault has **purge protection** enabled. It cannot be turned off, deleted secrets stay recoverable for the 7-day retention period, and a deleted vault keeps its name reserved until that period ends.

Locally, leave `BybitKeyVaultSettings:VaultUri` empty to use the single `KeyVaultSettings:VaultUri` vault for everything.

### Role assignments

The deployment identity has Contributor, which cannot create Azure RBAC role assignments. After each environment deployment, the subscription Owner must manually assign:

- `Key Vault Secrets Officer` to the operator identity that creates and updates secrets, scoped to the app vault (and to the credential vault only if manual credential support is needed). Subscription Owner alone does not grant Key Vault data-plane access when a vault uses RBAC authorization.
- To the API system identity:
  - `Key Vault Secrets Officer` scoped to the **credential vault**, because the API saves, updates and clears Bybit credentials there.
  - `Key Vault Secrets User` scoped to the **app vault**, which is read-only and enough to resolve Key Vault references. Do not grant the API write access to the app vault.
- `Key Vault Secrets User` to the Function App system identity, scoped to **both** vaults, for read-only secret access.
- `Storage Blob Data Owner`, `Storage Queue Data Contributor`, and `Storage Table Data Contributor` to the Function App system identity, scoped to that environment storage account. The Flex Functions host and its managed-identity deployment storage use these roles.

Use the `apiPrincipalId`, `functionPrincipalId`, `keyVaultResourceId`, `credentialVaultResourceId`, and `functionStorageRoleScope` deployment outputs as the assignment inputs. The Functions won't resolve their Key Vault references or access host storage until the required role assignments exist.

### Migrating an existing environment to the credential vault

This applies only to environments that stored `bybit-*` secrets in the app vault before the credential vault existed (STAGE). A new environment (PROD) writes credentials to the credential vault from the start.

1. Deploy the infrastructure. The credential vault is created, and both apps point `BybitKeyVaultSettings__VaultUri` at it.
2. Assign the credential vault roles above (API: Secrets Officer; Function App: Secrets User). Also assign the API Secrets User on the app vault, but keep its existing Secrets Officer there for now.
3. Run `infra/scripts/migrate-bybit-secrets.sh <env>` as an operator with Secrets User on the app vault and Secrets Officer on the credential vault. It copies the secrets without printing their values.
4. Restart the API and Function App. This clears the 24-hour in-memory Bybit secret cache in `KeyVaultService`.
5. Run the script again. This catches credentials saved to the old vault between steps 1 and 3.
6. Validate: manual sync (`POST /api/Exchange/bybit/sync/{id}`), the timer sync, and saving, updating and disconnecting Bybit credentials.
7. Only then delete the `bybit-*` secrets from the app vault, and remove the API's Secrets Officer role on the app vault.

Rollback before step 7: point `BybitKeyVaultSettings__VaultUri` back at the app vault and restart. The original secrets are still there.

The SQL server administrator credential is only for provisioning and database administration. After importing each BACPAC, create a database-scoped application login/user with only the permissions the application and its EF migrations require. Put that connection string in the environment Key Vault as `SqlConnectionString`; do not use the server administrator login in application settings.

## Databases and domain cutover

Bicep creates the shared SQL server and an empty Basic 5 DTU / 2 GB database for the selected environment. The owner exports/imports the existing data manually and validates it. Bicep does not migrate, replace, or delete database contents.

Custom-domain binding is disabled by default (`bindCustomDomain = false`). Validate each new Static Web App using its Azure hostname first. The current domains `app.sanfi.xyz` and `stage.sanfi.xyz` stay attached to the existing apps until their new counterparts and imported data pass validation and cutover is approved. Cloudflare DNS records are always configured manually by the owner. Only then should the relevant binding be enabled in that environment parameter file and deployed.

The API CORS allowlist includes the environment Static Web App default hostname and matching custom domain. PROD and STAGE do not allow each other's origins. The API stays on its `azurewebsites.net` hostname because the shared F1 plan does not support API custom domains.

## Local Bicep validation

```powershell
az bicep build --file infra/main.bicep
```

The parameter files require `SQL_ADMIN_PASSWORD` in the process environment. The GitHub workflow supplies the Environment secret and invokes deployment through the `.bicepparam` file, which also identifies the Bicep template.
