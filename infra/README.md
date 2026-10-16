# Azure infrastructure deployment

Infrastructure is deployed to the existing `TestDouglas` resource group in the MPN subscription. PROD and STAGE are test environments in `westeurope`; application code deployments remain separate.

## Deploy with GitHub Actions

1. Configure the GitHub Environment `azure-infra-sandbox` with the existing OIDC variables `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID`.
2. Add the SQL logical server administrator password as the Environment secret `SQL_ADMIN_PASSWORD`. Do not put it in a Bicep parameter file or repository file.
3. Run **Deploy Azure Infrastructure** with `stage` or `prod` and leave `apply` disabled first. Review validation and `what-if` output.
4. Run it again with `apply` enabled only after reviewing the changes.

The two parameter files intentionally leave `sqlFirewallIpAddresses` empty for the first deployment. That deployment creates the apps and reports their possible outbound IPs. Add the required API and Function App IPs to that environment parameter file, then run validation/what-if/apply again to add the narrow Azure SQL firewall rules. The database remains inaccessible to the apps until those rules are applied.

The workflow serializes PROD and STAGE deployments because both update shared resources: the Windows F1 App Service plan, SQL logical server, and Log Analytics workspace.

## Runtime secrets and permissions

Bicep creates Key Vaults and app settings containing versionless Key Vault references. It never creates or exports secret values. After deployment, add the following secret names to the environment-specific Key Vault. Secret values are supplied by the owner and must not be placed in Git:

- `SqlConnectionString` — connection string for that environment database.
- `StorageConnectionString` — connection string for that environment storage account. The application currently uses the storage SDK with a connection string, so shared-key access remains enabled on these test accounts.
- `JwtSigningSecret`
- `CoinMarketCapApiKey`
- `MailtrapHost`, `MailtrapPort`, `MailtrapUsername`, `MailtrapPassword`, and `MailtrapFrom`

The API reads `ConnectionStrings__DefaultConnection`; the Functions project currently reads `DefaultConnection`. Both read `AzureStorageSettings__ConnectionString`.

Application Insights resources and connection strings are provisioned, but the API project does not currently include the Application Insights SDK and the Functions SDK references are commented out. Confirm telemetry collection during application validation; code instrumentation changes remain separate from infrastructure deployment.

The deployment identity has Contributor, which cannot create Azure RBAC role assignments. After each environment deployment, the subscription Owner must manually assign:

- `Key Vault Secrets User` to the API and Function App system identities, scoped to that environment Key Vault.
- `Key Vault Secrets Officer` to the operator identity that will create/update secrets in that environment vault. Subscription Owner alone does not grant Key Vault data-plane access when the vault uses RBAC authorization.
- `Storage Blob Data Owner`, `Storage Queue Data Contributor`, and `Storage Table Data Contributor` to the Function App system identity, scoped to that environment storage account. These roles are used by the Flex Functions host and its managed-identity deployment storage.

Use the `apiPrincipalId`, `functionPrincipalId`, `keyVaultResourceId`, and `functionStorageRoleScope` deployment outputs as the assignment inputs. The functions will not resolve their Key Vault references or access host storage until the required role assignments exist.

The SQL server administrator credential is only for provisioning and database administration. After importing each BACPAC, create a database-scoped application login/user with only the permissions the application and its EF migrations require. Put that connection string in the environment Key Vault as `SqlConnectionString`; do not use the server administrator login in application settings.

## Databases and domain cutover

Bicep creates the shared SQL server and an empty Basic 5 DTU / 2 GB database for the selected environment. The owner exports/imports the existing data manually and validates it. Bicep does not migrate, replace, or delete database contents.

Custom-domain binding is disabled by default (`bindCustomDomain = false`). Validate each new Static Web App using its Azure hostname first. The current domains `app.sanfi.xyz` and `stage.sanfi.xyz` stay attached to the existing apps until their new counterparts and imported data pass validation and cutover is approved. Cloudflare DNS records are always configured manually by the owner. Only then should the relevant binding be enabled in that environment parameter file and deployed.

The API CORS allowlist includes the environment Static Web App default hostname and matching custom domain. PROD and STAGE do not allow each other's origins. The API stays on its `azurewebsites.net` hostname because the shared F1 plan does not support API custom domains.

## Local Bicep validation

```powershell
az bicep build --file infra/main.bicep
az bicep build-params --file infra/environments/stage.bicepparam
az bicep build-params --file infra/environments/prod.bicepparam
```
