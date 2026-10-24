using '../main.bicep'

param environmentName = 'prod'
param location = 'westeurope'
param sqlLocation = 'francecentral'
param frontendCustomDomain = 'app.sanfi.xyz'
param bindCustomDomain = false
param sqlFirewallIpAddresses = split(readEnvironmentVariable('SQL_FIREWALL_IP_ADDRESSES', ''), ',')
// Reads the GitHub Environment secret through the workflow process environment; no value is stored here.
param sqlAdministratorPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
