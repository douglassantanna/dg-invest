using '../main.bicep'

param environmentName = 'prod'
param location = 'westeurope'
param frontendCustomDomain = 'app.sanfi.xyz'
param bindCustomDomain = false
param sqlFirewallIpAddresses = []
// The infrastructure workflow overrides this with SQL_ADMIN_PASSWORD from GitHub Environment secrets.
param sqlAdministratorPassword = ''
