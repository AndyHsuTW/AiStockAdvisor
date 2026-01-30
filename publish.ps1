# 發布 AiStockAdvisor 專案 (PowerShell)

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Join-Path $ScriptDir "AiStockAdvisor.ConsoleUI"
$OutputDir = Join-Path $ScriptDir "publish"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " AiStockAdvisor Release Build" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration"
Write-Host "Output: $OutputDir"
Write-Host ""

# 清理舊的發布目錄
if (Test-Path $OutputDir) {
    Write-Host "Cleaning previous publish..."
    Remove-Item -Recurse -Force $OutputDir
}

# 還原套件
Write-Host "Restoring packages..."
dotnet restore (Join-Path $ScriptDir "AiStockAdvisor.sln")

# 發布專案
Write-Host "Publishing..."
dotnet publish (Join-Path $ProjectDir "AiStockAdvisor.ConsoleUI.csproj") `
    --configuration $Configuration `
    --output $OutputDir `
    --self-contained false `
    --framework net48

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# 複製 .env.example
$EnvExample = Join-Path $ScriptDir ".env.example"
if (Test-Path $EnvExample) {
    Copy-Item $EnvExample (Join-Path $OutputDir ".env.example")
    Write-Host "Copied .env.example"
}

# 複製 YuantaOneAPI.dll
$YuantaDll = Join-Path $ScriptDir "..\YuantaOneAPI.dll"
if (Test-Path $YuantaDll) {
    Copy-Item $YuantaDll $OutputDir
    Write-Host "Copied YuantaOneAPI.dll"
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host " Build Complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host "Output directory: $OutputDir"
Write-Host ""
Write-Host "To run:" -ForegroundColor Yellow
Write-Host "  cd $OutputDir"
Write-Host "  copy .env.example .env"
Write-Host "  # Edit .env with your settings"
Write-Host "  .\AiStockAdvisor.ConsoleUI.exe"
