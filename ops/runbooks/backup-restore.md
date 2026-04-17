# Backup and restore runbook

## Backup (Linux/macOS)
```bash
bash ops/scripts/backup-postgres.sh
```

## Restore (Linux/macOS)
```bash
bash ops/scripts/restore-postgres.sh ops/backups/<file>.dump
```

## Backup (PowerShell)
```powershell
pwsh -File ops/scripts/backup-postgres.ps1
```

## Restore (PowerShell)
```powershell
pwsh -File ops/scripts/restore-postgres.ps1 -BackupFile ops/backups/<file>.dump
```

## Notes
- Scripts assume the local Docker Compose PostgreSQL service name is `postgres`.
- Backups are stored in `ops/backups` by default.
- Restore drops and recreates the target database before loading the dump.
