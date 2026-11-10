using '../main.bicep'

param environmentName = 'stage'
param location = 'westeurope'
param sqlLocation = 'francecentral'
param frontendCustomDomain = 'stage.sanfi.xyz'
param bindCustomDomain = false
// Reads the GitHub Environment secret through the workflow process environment; no value is stored here.
param sqlAdministratorPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
// Legacy until GET /api/Recalculate/preview has been reviewed; then Proportional (README "Cost basis").
param transferCostPolicy = 'Legacy'
