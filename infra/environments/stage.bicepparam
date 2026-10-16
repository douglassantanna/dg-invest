using '../main.bicep'

param environmentName = 'stage'
param location = 'westeurope'
param frontendCustomDomain = 'stage.sanfi.xyz'
param bindCustomDomain = false
param sqlFirewallIpAddresses = []
// The infrastructure workflow overrides this with SQL_ADMIN_PASSWORD from GitHub Environment secrets.
param sqlAdministratorPassword = ''
