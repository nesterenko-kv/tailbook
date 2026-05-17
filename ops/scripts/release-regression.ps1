#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Tailbook release regression gate — automated pre-release validation.
.DESCRIPTION
    Runs all build, test, lint, typecheck, config, and security checks needed before a
    production-pilot release. Fails the first non-zero exit and reports summary.
.EXAMPLE
    ./ops/scripts/release-regression.ps1
#>

$ErrorActionPreference = "Stop"
$exitCode = 0
$results = @()

function Step($name, $scriptBlock) {
    Write-Host "`n### $name" -ForegroundColor Cyan
    try {
        & $scriptBlock
        Write-Host "  PASS: $name" -ForegroundColor Green
        $script:results += @{ Name = $name; Status = "PASS" }
    } catch {
        Write-Host "  FAIL: $name`n  $_" -ForegroundColor Red
        $script:results += @{ Name = $name; Status = "FAIL"; Error = $_ }
        $script:exitCode = 1
    }
}

# ── Prerequisites ──────────────────────────────────────────────
Step "Prerequisites: dotnet SDK" {
    $v = dotnet --version
    if (-not ($v -match '^\d+\.\d+')) { throw "dotnet not found: $v" }
    Write-Host "  dotnet $v"
}

Step "Prerequisites: Node.js" {
    $v = node --version
    if (-not ($v -match '^v?\d+\.')) { throw "Node.js not found: $v" }
    Write-Host "  node $v"
}

Step "Prerequisites: pnpm" {
    $v = pnpm --version
    if (-not ($v -match '^\d+\.')) { throw "pnpm not found: $v" }
    Write-Host "  pnpm $v"
}

# ── Backend ────────────────────────────────────────────────────
Step "Backend: restore" {
    dotnet restore backend/Tailbook.slnx
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed" }
}

Step "Backend: build" {
    $output = dotnet build backend/Tailbook.slnx --no-restore 2>&1
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed`n$output" }
    if ($output -match 'warning\(s\)') {
        $warnCount = [regex]::Match($output, '(\d+)\s+warning\(s\)').Groups[1].Value
        if ($warnCount -gt 0) { Write-Host "  Warnings: $warnCount" -ForegroundColor Yellow }
    }
}

Step "Backend: test" {
    $output = dotnet test backend/Tailbook.slnx --no-build 2>&1
    if ($LASTEXITCODE -ne 0) { throw "dotnet test failed`n$output" }
    $passMatch = [regex]::Match($output, 'passed:\s+(\d+)')
    $failMatch = [regex]::Match($output, 'failed:\s+(\d+)')
    if ($passMatch.Success) { Write-Host "  Passed: $($passMatch.Groups[1].Value)" -ForegroundColor Green }
    if ($failMatch.Success -and $failMatch.Groups[1].Value -ne '0') { throw "Test failures: $($failMatch.Groups[1].Value)" }
}

# ── Frontend ───────────────────────────────────────────────────
Step "Frontend: lint" {
    pnpm lint
    if ($LASTEXITCODE -ne 0) { throw "pnpm lint failed" }
}

Step "Frontend: typecheck" {
    pnpm typecheck
    if ($LASTEXITCODE -ne 0) { throw "pnpm typecheck failed" }
}

Step "Frontend: build" {
    pnpm build
    if ($LASTEXITCODE -ne 0) { throw "pnpm build failed" }
}

# ── Docker / Config ────────────────────────────────────────────
Step "Docker: compose config" {
    docker compose config
    if ($LASTEXITCODE -ne 0) { throw "docker compose config failed" }
}

Step "Docker: production compose config" {
    docker compose -f docker-compose.production.yml config
    if ($LASTEXITCODE -ne 0) { throw "docker compose production config failed" }
}

Step "Docker: validate Caddyfile" {
    $result = docker run --rm -v "${PWD}/ops/caddy/Caddyfile:/Caddyfile:ro" caddy:2-alpine caddy validate --config /Caddyfile --adapter caddyfile 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Caddyfile validation failed`n$result" }
}

# ── Security checks ────────────────────────────────────────────
Step "Security: no staged .env files" {
    $staged = git diff --cached --name-only --diff-filter=A
    if ($staged -match '\.env$') { throw "Staged .env file detected: $staged" }
}

Step "Security: git diff check (whitespace)" {
    $output = git diff --check 2>&1
    if ($LASTEXITCODE -ne 0) { throw "git diff --check failed`n$output" }
}

Step "Security: no uncommitted secrets pattern" {
    $secrets = git diff --name-only
    if ($secrets -match '\.env$') { throw "Uncommitted .env changes: $secrets" }
}

# ── Summary ────────────────────────────────────────────────────
Write-Host "`n═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "RELEASE REGRESSION SUMMARY" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
foreach ($r in $results) {
    $color = if ($r.Status -eq "PASS") { "Green" } else { "Red" }
    Write-Host "  [$($r.Status)] $($r.Name)" -ForegroundColor $color
}
Write-Host ""

if ($exitCode -eq 0) {
    Write-Host "All checks passed — ready for release." -ForegroundColor Green
} else {
    Write-Host "Some checks failed. Review errors above before releasing." -ForegroundColor Red
}

exit $exitCode
