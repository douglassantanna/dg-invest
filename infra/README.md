# Azure infrastructure deployment

Infrastructure is deployed to the existing `TestDouglas` resource group in the MPN subscription. PROD and STAGE are test environments; application resources are in `westeurope`, while the shared SQL server and databases are in `francecentral`. Application code deployments remain separate.

## Deploy with GitHub Actions

1. Configure the GitHub Environment `azure-infra-sandbox` with the existing OIDC variables `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID`.
2. Add the SQL logical server administrator password as the Environment secret `SQL_ADMIN_PASSWORD`. The `.bicepparam` file reads it from the workflow process environment; never put the password value in a repository file.
3. Before deploying, verify the resource providers required by the template are registered in the subscription identified by `AZURE_SUBSCRIPTION_ID`. This includes `Microsoft.Insights`, `Microsoft.OperationalInsights`, `Microsoft.KeyVault`, `Microsoft.Web`, `Microsoft.Sql`, and `Microsoft.Storage`. `Microsoft.Insights` and `Microsoft.OperationalInsights` are separate providers. Register any missing provider at subscription scope.
4. Run **Deploy Azure Infrastructure** with `stage` or `prod` and leave `apply` disabled first. Review validation and `what-if` output.
5. Run it again with `apply` enabled only after reviewing the changes.

The two parameter files intentionally leave `sqlFirewallIpAddresses` empty for the first deployment. That deployment creates the apps and reports their possible outbound IPs. Add the reported API and Function App IPs to that environment parameter file, then run validation/what-if/apply again to add the SQL firewall rules. The database remains inaccessible to the apps until those rules are applied.

The owner accepts this as a best-effort, test-only allowlist: Flex Consumption egress addresses can change as Azure scales the app, so a Functions-to-SQL connection can stop working until the updated address list is applied. This is not a stable-egress guarantee or a production network boundary. Refresh the list after egress-related connection failures and remove obsolete rules when addresses change.

The workflow serializes PROD and STAGE deployments because both update shared resources: the Windows F1 App Service plan and Log Analytics workspace in `westeurope`, plus the SQL logical server in `francecentral`. Keep `sqlLocation` consistent in both parameter files.

## Runtime secrets and permissions

Bicep creates Key Vaults and app settings containing versionless Key Vault references. It never creates or exports secret values. After deployment, add the following secret names to the environment-specific Key Vault. Secret values are supplied by the owner and must not be placed in Git:

- `SqlConnectionString` — connection string for that environment database.
- `StorageConnectionString` — connection string for that environment storage account. The application currently uses the storage SDK with a connection string, so shared-key access remains enabled on these test accounts.
- `JwtSigningSecret`
- `CoinMarketCapApiKey`
- `MailtrapHost`, `MailtrapPort`, `MailtrapUsername`, `MailtrapPassword`, and `MailtrapFrom`

The API reads `ConnectionStrings__DefaultConnection`; the Functions project currently reads `DefaultConnection`. Both read `AzureStorageSettings__ConnectionString`.

Each Function App is explicitly provisioned with `BybitSync__Enabled=true`; it begins scheduled synchronization after its code, Key Vault permissions/secrets, and database connection are configured. Set the app setting to `false` if an environment must remain paused.

Application Insights resources and connection strings are provisioned, but the API project does not currently include the Application Insights SDK and the Functions SDK references are commented out. Confirm telemetry collection during application validation; code instrumentation changes remain separate from infrastructure deployment.

The deployment identity has Contributor, which cannot create Azure RBAC role assignments. After each environment deployment, the subscription Owner must manually assign:

- `Key Vault Secrets Officer` to the operator identity that will create/update secrets in that environment vault. Subscription Owner alone does not grant Key Vault data-plane access when the vault uses RBAC authorization.
- `Key Vault Secrets Officer` to the API system identity, scoped to that environment Key Vault. The API saves, updates, and clears Bybit credentials there.
- `Key Vault Secrets User` to the Function App system identity, scoped to that environment Key Vault, for read-only secret access.
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
```

The parameter files require `SQL_ADMIN_PASSWORD` in the process environment. The GitHub workflow supplies the Environment secret and invokes deployment through the `.bicepparam` file, which also identifies the Bicep template.
