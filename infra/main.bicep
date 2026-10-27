targetScope = 'resourceGroup'

@allowed([
  'prod'
  'stage'
])
param environmentName string

param location string = 'westeurope'
@description('Region for the shared Azure SQL logical server and its databases. Keep consistent across PROD and STAGE.')
param sqlLocation string = 'francecentral'
param sqlAdministratorLogin string = 'dginvestSqlAdmin'
@secure()
param sqlAdministratorPassword string

@description('Current App Service and Functions egress IPs collected by the deployment workflow for Azure SQL firewall rules. Flex addresses can change.')
param sqlFirewallIpAddresses array = []

@description('Set true only after the Cloudflare DNS records are verified and the existing Static Web App domain binding is ready to move.')
param bindCustomDomain bool = false
param frontendCustomDomain string

var appServicePlanName = 'dginvest-apps-weu-f1'
var sqlServerName = 'dginvest-data-frc-01'
var logAnalyticsWorkspaceName = 'dginvest-observe-weu-01'

var apiName = 'dginvest-${environmentName}-api-weu-01'
var functionName = 'dginvest-${environmentName}-jobs-weu-01'
var functionPlanName = 'dginvest-${environmentName}-jobs-flex-weu'
var frontendName = 'dginvest-${environmentName}-web-weu-01'
var keyVaultName = 'dginvest-${environmentName}-sec-weu1'
var storageAccountName = environmentName == 'prod' ? 'dginvestproddataweu1' : 'dginveststagedataweu1'
var databaseName = 'dginvest-${environmentName}'
var appInsightsName = 'dginvest-${environmentName}-observe-weu-01'
var deploymentContainerName = 'function-app-packages'
var keyVaultUri = 'https://${keyVaultName}${environment().suffixes.keyvaultDns}/'
var sqlConnectionStringSecretReference = '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/SqlConnectionString)'
var storageConnectionStringSecretReference = '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/StorageConnectionString)'
var migrationTokenSecretReference = '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/MigrationToken)'
var jwtSecretReference = '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/JwtSigningSecret)'
var coinMarketCapKeyReference = '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/CoinMarketCapApiKey)'
var mailtrapHostReference = '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/MailtrapHost)'
var mailtrapPortReference = '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/MailtrapPort)'
var mailtrapUsernameReference = '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/MailtrapUsername)'
var mailtrapPasswordReference = '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/MailtrapPassword)'
var mailtrapFromReference = '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/MailtrapFrom)'
var databaseHealthFailureMessage = 'Database connection is currently unhealthy or unreachable'
var databaseHealthAlertSubject = 'Database health check failed'
var databaseHealthAlertBodyTemplate = 'The database health check failed. Reason: {Reason}. {ExceptionDetails} Time: {Timestamp}. Environment: {EnvironmentName}'
var corsOrigins = [
  'https://${frontend.properties.defaultHostname}'
  'https://${frontendCustomDomain}'
]
var commonTags = {
  Environment: environmentName
  Purpose: 'test'
  ManagedBy: 'Bicep'
}

resource sharedAppServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: appServicePlanName
  location: location
  kind: 'app'
  sku: {
    name: 'F1'
    tier: 'Free'
    size: 'F1'
    capacity: 1
  }
  properties: {
    reserved: false
    perSiteScaling: false
    zoneRedundant: false
  }
  tags: union(commonTags, { Environment: 'shared', Component: 'apps' })
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01' = {
  name: sqlServerName
  location: sqlLocation
  properties: {
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorPassword
    publicNetworkAccess: 'Enabled'
    minimalTlsVersion: '1.2'
  }
  tags: union(commonTags, { Environment: 'shared', Component: 'data' })
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01' = {
  parent: sqlServer
  name: databaseName
  location: sqlLocation
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    maxSizeBytes: 2147483648
    zoneRedundant: false
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: 'Local'
  }
  tags: commonTags
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
  tags: union(commonTags, { Environment: 'shared', Component: 'observability' })
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
    IngestionMode: 'LogAnalytics'
  }
  tags: union(commonTags, { Component: 'observability' })
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
  }
  tags: union(commonTags, { Component: 'secrets' })
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
  }
  tags: union(commonTags, { Component: 'data' })
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource logsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'logs'
  properties: {
    publicAccess: 'None'
  }
}

resource functionDeploymentContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: deploymentContainerName
  properties: {
    publicAccess: 'None'
  }
}

resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource welcomeEmailQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: queueService
  name: 'welcomeemail'
}

resource frontend 'Microsoft.Web/staticSites@2025-03-01' = {
  name: frontendName
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    stagingEnvironmentPolicy: 'Enabled'
    publicNetworkAccess: 'Enabled'
  }
  tags: union(commonTags, { Component: 'web' })
}

resource frontendCustomDomainBinding 'Microsoft.Web/staticSites/customDomains@2025-03-01' = if (bindCustomDomain) {
  parent: frontend
  name: frontendCustomDomain
  properties: {
    validationMethod: 'dns-txt-token'
  }
}

resource apiApp 'Microsoft.Web/sites@2024-04-01' = {
  name: apiName
  location: location
  kind: 'app'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    httpsOnly: true
    serverFarmId: sharedAppServicePlan.id
    publicNetworkAccess: 'Enabled'
    clientAffinityEnabled: false
    siteConfig: {
      alwaysOn: false
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      netFrameworkVersion: 'v10.0'
      use32BitWorkerProcess: true
      appSettings: [
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: applicationInsights.properties.ConnectionString }
        { name: 'AllowedOrigins__0', value: corsOrigins[0] }
        { name: 'AllowedOrigins__1', value: corsOrigins[1] }
        { name: 'RateLimiterSettings__RequestsPermitLimit', value: '320' }
        { name: 'RateLimiterSettings__WindowLimitInMinutes', value: '10' }
        { name: 'DatabaseHealthCheckOptions__FailureMessage', value: databaseHealthFailureMessage }
        { name: 'DatabaseHealthCheckOptions__AlertSubject', value: databaseHealthAlertSubject }
        { name: 'DatabaseHealthCheckOptions__AlertBodyTemplate', value: databaseHealthAlertBodyTemplate }
        { name: 'DatabaseHealthCheckOptions__SendAlertAlways', value: 'true' }
        { name: 'Migrations__RemoteTriggerEnabled', value: 'true' }
        { name: 'Migrations__RemoteTriggerToken', value: migrationTokenSecretReference }
        { name: 'ConnectionStrings__DefaultConnection', value: sqlConnectionStringSecretReference }
        { name: 'DefaultConnection', value: sqlConnectionStringSecretReference }
        { name: 'AzureStorageSettings__ConnectionString', value: storageConnectionStringSecretReference }
        { name: 'AzureStorageSettings__ContainerName', value: 'assets' }
        { name: 'AzureStorageSettings__LogContainer', value: 'logs' }
        { name: 'AzureStorageSettings__WelcomeEmailQueue', value: 'welcomeemail' }
        { name: 'KeyVaultSettings__VaultUri', value: keyVaultUri }
        { name: 'BybitKeyVaultSettings__VaultUri', value: keyVaultUri }
        { name: 'JWTSettings__Secret', value: jwtSecretReference }
        { name: 'JWTSettings__Issuer', value: 'dginvest-${environmentName}' }
        { name: 'JWTSettings__ExpiryMinutes', value: '120' }
        { name: 'CoinMarketCapSettings__ApiKey', value: coinMarketCapKeyReference }
        { name: 'CoinMarketCapSettings__BaseUrl', value: 'https://pro-api.coinmarketcap.com' }
        { name: 'CoinMarketCapSettings__Header', value: 'X-CMC_PRO_API_KEY' }
        { name: 'MailtrapSettings__Host', value: mailtrapHostReference }
        { name: 'MailtrapSettings__Port', value: mailtrapPortReference }
        { name: 'MailtrapSettings__Username', value: mailtrapUsernameReference }
        { name: 'MailtrapSettings__Password', value: mailtrapPasswordReference }
        { name: 'MailtrapSettings__From', value: mailtrapFromReference }
      ]
    }
  }
  tags: union(commonTags, { Component: 'api' })
}

resource functionPlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: functionPlanName
  location: location
  kind: 'functionapp'
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
    family: 'FC'
  }
  properties: {
    reserved: true
    zoneRedundant: false
  }
  tags: union(commonTags, { Component: 'jobs' })
}

resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: functionName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    httpsOnly: true
    serverFarmId: functionPlan.id
    reserved: true
    publicNetworkAccess: 'Enabled'
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storageAccount.properties.primaryEndpoints.blob}${deploymentContainerName}'
          authentication: {
            type: 'SystemAssignedIdentity'
          }
        }
      }
      scaleAndConcurrency: {
        maximumInstanceCount: 3
        instanceMemoryMB: 512
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '10.0'
      }
    }
    siteConfig: {
      alwaysOn: false
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
    }
  }
  tags: union(commonTags, { Component: 'jobs' })
}

resource functionAppSettings 'Microsoft.Web/sites/config@2024-04-01' = {
  parent: functionApp
  name: 'appsettings'
  properties: {
    AzureWebJobsStorage__accountName: storageAccount.name
    AzureWebJobsStorage__credential: 'managedidentity'
    AzureWebJobsStorage__blobServiceUri: storageAccount.properties.primaryEndpoints.blob
    AzureWebJobsStorage__queueServiceUri: storageAccount.properties.primaryEndpoints.queue
    AzureWebJobsStorage__tableServiceUri: storageAccount.properties.primaryEndpoints.table
    BybitSync__Enabled: 'true'
    APPLICATIONINSIGHTS_CONNECTION_STRING: applicationInsights.properties.ConnectionString
    AzureStorageSettings__ConnectionString: storageConnectionStringSecretReference
    AzureStorageSettings__ContainerName: 'assets'
    AzureStorageSettings__LogContainer: 'logs'
    AzureStorageSettings__WelcomeEmailQueue: 'welcomeemail'
    DatabaseHealthCheckOptions__FailureMessage: databaseHealthFailureMessage
    DatabaseHealthCheckOptions__AlertSubject: databaseHealthAlertSubject
    DatabaseHealthCheckOptions__AlertBodyTemplate: databaseHealthAlertBodyTemplate
    DatabaseHealthCheckOptions__SendAlertAlways: 'true'
    DefaultConnection: sqlConnectionStringSecretReference
    KeyVaultSettings__VaultUri: keyVaultUri
    BybitKeyVaultSettings__VaultUri: keyVaultUri
    JWTSettings__Secret: jwtSecretReference
    JWTSettings__Issuer: 'dginvest-${environmentName}'
    JWTSettings__ExpiryMinutes: '120'
    CoinMarketCapSettings__ApiKey: coinMarketCapKeyReference
    CoinMarketCapSettings__BaseUrl: 'https://pro-api.coinmarketcap.com'
    CoinMarketCapSettings__Header: 'X-CMC_PRO_API_KEY'
    MailtrapSettings__Host: mailtrapHostReference
    MailtrapSettings__Port: mailtrapPortReference
    MailtrapSettings__Username: mailtrapUsernameReference
    MailtrapSettings__Password: mailtrapPasswordReference
    MailtrapSettings__From: mailtrapFromReference
    HealthPingOptions__RunFunction: 'false'
  }
}

var sqlFirewallIpAddressesFiltered = filter(sqlFirewallIpAddresses, ipAddress => !empty(trim(ipAddress)))

resource appOutboundSqlFirewallRules 'Microsoft.Sql/servers/firewallRules@2023-08-01' = [for (ipAddress, index) in sqlFirewallIpAddressesFiltered: {
  parent: sqlServer
  name: '${environmentName}-app-${index}'
  properties: {
    startIpAddress: trim(ipAddress)
    endIpAddress: trim(ipAddress)
  }
}]

output environment string = environmentName
output apiUrl string = 'https://${apiName}.azurewebsites.net'
output apiAppName string = apiApp.name
output functionAppName string = functionApp.name
output functionPrincipalId string = functionApp.identity.principalId
output apiPrincipalId string = apiApp.identity.principalId
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVaultUri
output keyVaultResourceId string = keyVault.id
output frontendDefaultHostname string = frontend.properties.defaultHostname
output frontendCustomDomain string = frontendCustomDomain
output storageAccountName string = storageAccount.name
output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabase.name
output functionStorageRoleScope string = storageAccount.id
output apiPossibleOutboundIpAddresses string = apiApp.properties.possibleOutboundIpAddresses
output functionPossibleOutboundIpAddresses string = functionApp.properties.possibleOutboundIpAddresses
output sqlFirewallIpAddresses array = sqlFirewallIpAddressesFiltered
