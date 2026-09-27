#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Roda os cenários de demonstração da API BookStore via Newman.

.DESCRIPTION
    Pré-requisitos (uma vez):
        npm install -g newman newman-reporter-htmlextra

    Sobe a stack antes de rodar:
        aspire run --project apps/Aspire

.PARAMETER Environment
    Ambiente a usar: 'aspire' (padrão, porta 5085), 'docker' (porta 8080) ou 'https' (porta 7270).

.PARAMETER Folder
    Prefixo do cenário a executar isoladamente, ex: 'S1', 'S3', 'S4'.
    Se omitido, executa todos os cenários em sequência.

.PARAMETER Delay
    Delay em ms entre requests consecutivos (padrão: 2000).
    2000ms cobre o backlog do Worker no Docker Compose (S1+S2+S3 = ~15 s com DEMO_FRAUD_DELAY_SECONDS=5).
    Reduza para 500 se estiver rodando via Aspire sem delay de demo.

.PARAMETER NoOpen
    Não abre o relatório HTML no browser ao final.

.EXAMPLE
    # Roda tudo via Aspire (padrão, HTTP :5085)
    .\run-demo.ps1

    # Roda via Docker Compose (:8080)
    .\run-demo.ps1 -Environment docker

    # Roda com HTTPS (desabilite SSL verification no Postman antes)
    .\run-demo.ps1 -Environment https

    # Roda só o cenário S1
    .\run-demo.ps1 -Folder 'S1'

    # Roda S3 e S4 com delay para o Worker (Docker Compose)
    .\run-demo.ps1 -Folder 'S3' -Delay 2000
    .\run-demo.ps1 -Folder 'S4' -Delay 2000
#>
param(
    [ValidateSet('aspire', 'docker', 'https')]
    [string]$Environment = 'aspire',
    [string]$Folder = '',
    [int]$Delay = 2000,
    [switch]$NoOpen
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$scriptDir  = $PSScriptRoot
$collection = Join-Path $scriptDir 'BookStore.postman_collection.json'
$envFile    = switch ($Environment) {
    'https'  { Join-Path $scriptDir 'local-https.postman_environment.json' }
    'docker' { Join-Path $scriptDir 'local.postman_environment.json' }
    default  { Join-Path $scriptDir 'local-aspire.postman_environment.json' }
}
$reportDir  = Join-Path $scriptDir 'newman'
$timestamp  = Get-Date -Format 'yyyyMMdd-HHmmss'
$reportFile = Join-Path $reportDir "report-$timestamp.html"
$baseUrl    = switch ($Environment) {
    'https'  { 'https://localhost:7270' }
    'docker' { 'http://localhost:8080' }
    default  { 'http://localhost:5085' }
}

# ── Banner ────────────────────────────────────────────────────────────────────
Write-Host ''
Write-Host '  ╔══════════════════════════════════════╗' -ForegroundColor Magenta
Write-Host '  ║   BookStore — Demo Runner (Newman)   ║' -ForegroundColor Magenta
Write-Host '  ╚══════════════════════════════════════╝' -ForegroundColor Magenta
Write-Host ''
Write-Host "  Ambiente : $Environment ($baseUrl)" -ForegroundColor DarkGray
if ($Folder) {
    Write-Host "  Cenário  : $Folder" -ForegroundColor DarkGray
} else {
    Write-Host '  Cenário  : todos (S1 → S11)' -ForegroundColor DarkGray
}
Write-Host "  Delay    : ${Delay}ms entre requests" -ForegroundColor DarkGray
Write-Host "  Relatório: $reportFile" -ForegroundColor DarkGray
Write-Host ''

# ── Verificar Newman ──────────────────────────────────────────────────────────
Write-Host '  [1/3] Verificando Newman...' -ForegroundColor Cyan
if (-not (Get-Command newman -ErrorAction SilentlyContinue)) {
    Write-Host ''
    Write-Host '  [ERRO] Newman não encontrado.' -ForegroundColor Red
    Write-Host '  Instale com:  npm install -g newman newman-reporter-htmlextra' -ForegroundColor Yellow
    exit 1
}
$newmanVersion = (newman --version 2>&1)
Write-Host "         newman $newmanVersion" -ForegroundColor DarkGreen

# ── Health check da API ───────────────────────────────────────────────────────
Write-Host ''
Write-Host "  [2/3] Verificando API em $baseUrl/health ..." -ForegroundColor Cyan
try {
    $resp = Invoke-WebRequest -Uri "$baseUrl/health" `
        -SkipCertificateCheck `
        -TimeoutSec 8 `
        -ErrorAction Stop
    if ($resp.StatusCode -eq 200) {
        Write-Host '         API saudável ✓' -ForegroundColor DarkGreen
    } else {
        Write-Host "  [AVISO] /health retornou $($resp.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host ''
    Write-Host "  [ERRO] API não respondeu em $baseUrl/health" -ForegroundColor Red
    Write-Host '  Certifique-se de que o Aspire está rodando:' -ForegroundColor Yellow
    Write-Host '    aspire run --project apps/Aspire' -ForegroundColor Yellow
    exit 1
}

# ── Executar Newman ───────────────────────────────────────────────────────────
Write-Host ''
Write-Host '  [3/3] Executando cenários...' -ForegroundColor Cyan
Write-Host ''

New-Item -ItemType Directory -Force -Path $reportDir | Out-Null

$newmanArgs = @(
    'run', $collection,
    '--environment', $envFile,
    '--delay-request', $Delay,
    '--reporters', 'cli,htmlextra',
    '--reporter-htmlextra-export', $reportFile,
    '--reporter-htmlextra-title', 'BookStore — Cenários de Fraude',
    '--reporter-htmlextra-logs',
    '--reporter-htmlextra-showOnlyFails', 'false',
    '--reporter-htmlextra-browserTitle', 'BookStore Demo'
)

if ($Folder) {
    $newmanArgs += '--folder'
    $newmanArgs += $Folder
}

newman @newmanArgs
$exitCode = $LASTEXITCODE

# ── Resultado ─────────────────────────────────────────────────────────────────
Write-Host ''
if ($exitCode -eq 0) {
    Write-Host '  ✓ Todos os cenários passaram.' -ForegroundColor Green
} else {
    Write-Host "  ✗ Alguns cenários falharam (exit code $exitCode)." -ForegroundColor Red
}

if (Test-Path $reportFile) {
    Write-Host ''
    Write-Host "  Relatório HTML: $reportFile" -ForegroundColor Cyan
    if (-not $NoOpen) {
        Write-Host '  Abrindo no browser...' -ForegroundColor DarkGray
        Start-Process $reportFile
    }
}

Write-Host ''
exit $exitCode
