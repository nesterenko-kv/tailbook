param(
    [Parameter(Mandatory = $true)]
    [string]$BackupFile
)

if (-not (Test-Path $BackupFile)) {
    throw "Backup file not found: $BackupFile"
}

cmd /c "docker compose exec -T postgres psql -U $env:POSTGRES_USER -d postgres -c \"DROP DATABASE IF EXISTS \"\"$env:POSTGRES_DB\"\";\""
cmd /c "docker compose exec -T postgres psql -U $env:POSTGRES_USER -d postgres -c \"CREATE DATABASE \"\"$env:POSTGRES_DB\"\";\""
cmd /c "docker compose exec -T postgres pg_restore -U $env:POSTGRES_USER -d $env:POSTGRES_DB --clean --if-exists < $BackupFile"
Write-Host "Restore completed from: $BackupFile"
