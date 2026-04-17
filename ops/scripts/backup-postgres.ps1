param()

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupDir = "ops/backups"
$backupFile = Join-Path $backupDir "tailbook-$timestamp.dump"

New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
cmd /c "docker compose exec -T postgres pg_dump -U $env:POSTGRES_USER -d $env:POSTGRES_DB -Fc > $backupFile"
Write-Host "Backup created: $backupFile"
