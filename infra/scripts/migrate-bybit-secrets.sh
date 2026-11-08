#!/usr/bin/env bash
# Copies user Bybit credentials (bybit-* secrets) from an environment's app vault to its
# credential vault. One-time migration for environments created before the credential vault.
# Environments deployed after it never write bybit-* secrets to the app vault.
#
# Usage: infra/scripts/migrate-bybit-secrets.sh <stage|prod>
#
# - Idempotent: re-running overwrites with the same values. Run it again after restarting the
#   API and Function App, to catch credentials saved to the old vault mid-migration.
# - Secret values never reach stdout, logs or process arguments.
# - Copies only. Deleting bybit-* secrets from the app vault is a separate, manual step after
#   validation (see infra/README.md).
# - Requires Key Vault Secrets User on the app vault and Key Vault Secrets Officer on the
#   credential vault for the signed-in identity.
set -euo pipefail

environment_name="${1:-}"
case "$environment_name" in
  stage | prod) ;;
  *)
    echo "Usage: $0 <stage|prod>" >&2
    exit 1
    ;;
esac

source_vault="dginvest-${environment_name}-sec-weu1"
target_vault="dginvest-${environment_name}-cred-weu1"

names=$(az keyvault secret list \
  --vault-name "$source_vault" \
  --query "[?starts_with(name, 'bybit-')].name" \
  --output tsv)

if [ -z "$names" ]; then
  echo "No bybit-* secrets found in $source_vault; nothing to copy."
  exit 0
fi

count=0
while IFS= read -r name; do
  [ -n "$name" ] || continue
  value=$(az keyvault secret show --vault-name "$source_vault" --name "$name" --query value --output tsv)
  # printf without a trailing newline; piping tsv output straight in would append "\n" to the secret.
  printf '%s' "$value" | az keyvault secret set \
    --vault-name "$target_vault" \
    --name "$name" \
    --file /dev/stdin \
    --output none
  unset value
  count=$((count + 1))
done <<< "$names"

echo "Copied $count bybit-* secret(s) from $source_vault to $target_vault."
