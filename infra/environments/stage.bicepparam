using '../main.bicep'

param environmentName = 'stage'
param location = 'westeurope'
param sqlLocation = 'francecentral'
param frontendCustomDomain = 'stage.sanfi.xyz'
param bindCustomDomain = false
// Reads the GitHub Environment secret through the workflow process environment; no value is stored here.
param sqlAdministratorPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
// Proportional since 2026-11-10, after reviewing GET /api/Recalculate/preview against STAGE data (README "Cost basis").
param transferCostPolicy = 'Proportional'
// TEMPORARY (2026-11-10): one-off admin recalculation of manual accounts after switching to Proportional.
// Set back to false in a follow-up PR once POST /api/Recalculate/recalculate has run.
param enableRecalculation = true
