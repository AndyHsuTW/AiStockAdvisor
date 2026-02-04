# 打包與推送 AiStockAdvisor.Contracts (BaGet)

param(
    [string]$Configuration = "Release",
    [string]$Source = "http://192.168.0.43:5555/v3/index.json",
    [string]$ApiKey = $env:BAGET_API_KEY,
    [switch]$SkipPush
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Join-Path $ScriptDir "AiStockAdvisor.Contracts"
$ProjectFile = Join-Path $ProjectDir "AiStockAdvisor.Contracts.csproj"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " AiStockAdvisor.Contracts Pack & Push" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration"
Write-Host "Source: $Source"
Write-Host ""

Write-Host "Packing..."
dotnet pack $ProjectFile -c $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Host "Pack failed!" -ForegroundColor Red
    exit 1
}

if ($SkipPush) {
    Write-Host "Skip push requested." -ForegroundColor Yellow
    exit 0
}

if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    Write-Host "BAGET_API_KEY not set, skipping push." -ForegroundColor Yellow
    exit 0
}

Write-Host "Pushing to BaGet..."
dotnet nuget push (Join-Path $ProjectDir "bin\$Configuration\AiStockAdvisor.Contracts.*.nupkg") `
    --source $Source `
    --api-key $ApiKey `
    --skip-duplicate

if ($LASTEXITCODE -ne 0) {
    Write-Host "Push failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host " Pack & Push Complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
