param(
    [switch]$SkipInstall,
    [switch]$SkipRestore,
    [switch]$SkipMigrations,
    [int]$TimeoutSeconds = 240
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$ApiProject = Join-Path $Root "backend/src/Tailbook.Api.Host/Tailbook.Api.Host.csproj"

$DockerServices = @(
    @{ Name = "PostgreSQL"; Container = "tailbook-postgres"; Url = "localhost:5432" },
    @{ Name = "Redis"; Container = "tailbook-redis"; Url = "localhost:6379" },
    @{ Name = "pgAdmin"; Container = "tailbook-pgadmin"; Url = "http://localhost:5050" },
    @{ Name = "OpenTelemetry Collector"; Container = "tailbook-otel-collector"; Url = "localhost:4317" },
    @{ Name = "Prometheus"; Container = "tailbook-prometheus"; Url = "http://localhost:9090" },
    @{ Name = "Grafana"; Container = "tailbook-grafana"; Url = "http://localhost:3000" }
)

$HttpServices = @(
    @{ Name = "pgAdmin web"; Url = "http://localhost:5050" },
    @{ Name = "Prometheus web"; Url = "http://localhost:9090" },
    @{ Name = "Grafana web"; Url = "http://localhost:3000" },
    @{ Name = "API ready"; Url = "https://localhost:5001/health/ready" },
    @{ Name = "Admin web"; Url = "http://localhost:3001" },
    @{ Name = "Client web"; Url = "http://localhost:3002" },
    @{ Name = "Groomer web"; Url = "http://localhost:3003" }
)

function Write-Banner {
    Write-Host ""
    Write-Host "  _______        _ _ _                 _" -ForegroundColor Cyan
    Write-Host " |__   __|      (_) | |               | |" -ForegroundColor Cyan
    Write-Host "    | | __ _ ___ _| | |__   ___   ___ | | __" -ForegroundColor Cyan
    Write-Host "    | |/ _`` / __| | | '_ \ / _ \ / _ \| |/ /" -ForegroundColor Cyan
    Write-Host "    | | (_| \__ \ | | |_) | (_) | (_) |   <" -ForegroundColor Cyan
    Write-Host "    |_|\__,_|___/_|_|_.__/ \___/ \___/|_|\_\" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Local stack launcher" -ForegroundColor White
    Write-Host ""
}

function Require-Command($Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found on PATH."
    }
}

function Ensure-Pnpm {
    if (Get-Command "pnpm" -ErrorAction SilentlyContinue) {
        return
    }

    Require-Command "corepack"
    Invoke-Step "Preparing pnpm 10.33.0 with Corepack" { corepack enable; corepack prepare pnpm@10.33.0 --activate }

    if (-not (Get-Command "pnpm" -ErrorAction SilentlyContinue)) {
        throw "pnpm was not found after Corepack activation. Restart this shell or install pnpm manually."
    }
}

function Invoke-Step($Title, $ScriptBlock) {
    Write-Host "> $Title" -ForegroundColor Yellow
    & $ScriptBlock
}

function Start-DevWindow($Title, $Command) {
    $escapedTitle = $Title.Replace("'", "''")
    $escapedRoot = $Root.Replace("'", "''")
    $windowCommand = "`$host.UI.RawUI.WindowTitle = '$escapedTitle'; Set-Location -LiteralPath '$escapedRoot'; $Command"

    Start-Process -FilePath "powershell" -ArgumentList @(
        "-NoExit",
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-Command", $windowCommand
    ) | Out-Null
}

function Get-ContainerHealth($ContainerName) {
    $status = docker inspect --format "{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}" $ContainerName 2>$null
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($status)) {
        return "missing"
    }

    return $status.Trim()
}

function Test-HttpService($Url) {
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
        return ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500)
    }
    catch {
        return $false
    }
}

function Wait-ForStack {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

    while ((Get-Date) -lt $deadline) {
        $pending = New-Object System.Collections.Generic.List[string]

        foreach ($service in $DockerServices) {
            $health = Get-ContainerHealth $service.Container
            if ($health -ne "healthy" -and $health -ne "running") {
                $pending.Add("$($service.Name): $health")
            }
        }

        foreach ($service in $HttpServices) {
            if (-not (Test-HttpService $service.Url)) {
                $pending.Add("$($service.Name): waiting")
            }
        }

        if ($pending.Count -eq 0) {
            return
        }

        Write-Host ("Waiting for services: " + ($pending -join ", ")) -ForegroundColor DarkGray
        Start-Sleep -Seconds 5
    }

    throw "Timed out after $TimeoutSeconds seconds waiting for the local stack."
}

function Write-ServiceTable {
    Write-Host ""
    Write-Host "Started services" -ForegroundColor Green
    Write-Host "----------------" -ForegroundColor Green

    foreach ($service in $DockerServices) {
        $health = Get-ContainerHealth $service.Container
        Write-Host (("{0,-24} {1,-10} {2}" -f $service.Name, $health, $service.Url))
    }

    foreach ($service in $HttpServices) {
        $status = if (Test-HttpService $service.Url) { "healthy" } else { "waiting" }
        Write-Host (("{0,-24} {1,-10} {2}" -f $service.Name, $status, $service.Url))
    }
}

Set-Location -LiteralPath $Root
Write-Banner

Require-Command "docker"
Require-Command "dotnet"
Ensure-Pnpm

if (-not (Test-Path -LiteralPath (Join-Path $Root ".env")) -and (Test-Path -LiteralPath (Join-Path $Root ".env.example"))) {
    Invoke-Step "Creating .env from .env.example" { Copy-Item -LiteralPath (Join-Path $Root ".env.example") -Destination (Join-Path $Root ".env") }
}

if (-not $SkipInstall) {
    Invoke-Step "Installing frontend dependencies" { pnpm install --frozen-lockfile }
}

if (-not $SkipRestore) {
    Invoke-Step "Restoring backend dependencies" { dotnet restore "backend/Tailbook.slnx" }
}

Invoke-Step "Starting Docker services" { docker compose up -d }

if (-not $SkipMigrations) {
    Invoke-Step "Applying database migrations" { dotnet ef database update --project $ApiProject --startup-project $ApiProject }
}

Write-Host "> Starting dev servers in separate PowerShell windows" -ForegroundColor Yellow
Start-DevWindow "Tailbook API" "dotnet watch --project '$ApiProject' run"
Start-DevWindow "Tailbook Admin Web" "pnpm dev:admin"
Start-DevWindow "Tailbook Client Web" "pnpm dev:client"
Start-DevWindow "Tailbook Groomer Web" "pnpm dev:groomer"

Wait-ForStack
Write-ServiceTable

Write-Host ""
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host " Tailbook is groomed, fluffed, and ready." -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""
