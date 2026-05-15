#!/usr/bin/env bash
set -euo pipefail

if [ $# -lt 1 ]; then
  echo "Usage: bash ops/scripts/restore-postgres.sh ops/backups/<file>.dump"
  exit 1
fi

BACKUP_FILE="$1"
if [ ! -f "$BACKUP_FILE" ]; then
  echo "Backup file not found: $BACKUP_FILE"
  exit 1
fi

docker compose exec -T postgres psql -U "${POSTGRES_USER:-tailbook}" -d postgres -c "DROP DATABASE IF EXISTS \"${POSTGRES_DB:-tailbook}\";"
docker compose exec -T postgres psql -U "${POSTGRES_USER:-tailbook}" -d postgres -c "CREATE DATABASE \"${POSTGRES_DB:-tailbook}\";"
docker compose exec -T postgres pg_restore -U "${POSTGRES_USER:-tailbook}" -d "${POSTGRES_DB:-tailbook}" --clean --if-exists < "$BACKUP_FILE"

echo "Restore completed from: $BACKUP_FILE"
