#!/usr/bin/env bash
set -euo pipefail

mkdir -p ops/backups
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
BACKUP_FILE="ops/backups/tailbook-${TIMESTAMP}.dump"

docker compose exec -T postgres pg_dump \
  -U "${POSTGRES_USER:-tailbook}" \
  -d "${POSTGRES_DB:-tailbook}" \
  -Fc > "$BACKUP_FILE"

echo "Backup created: $BACKUP_FILE"
