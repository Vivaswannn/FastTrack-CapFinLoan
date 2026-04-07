# CapFinLoan — Start All Services (Docker mode)
# Double-click or run: powershell -ExecutionPolicy Bypass -File start-all.ps1

$root     = Split-Path -Parent $MyInvocation.MyCommand.Path
$frontend = "$root\Frontend\capfinloan-frontend"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CapFinLoan — Starting All Services"    -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ── Start Docker services ─────────────────────────────────────────────────────
Write-Host "Starting Docker containers..." -ForegroundColor Yellow
Set-Location $root
docker-compose up -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: docker-compose failed. Is Docker Desktop running?" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "Waiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# ── Start React Frontend ──────────────────────────────────────────────────────
Write-Host "Starting React frontend on port 5173..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", `
    "Set-Location '$frontend'; `$host.UI.RawUI.WindowTitle = 'Frontend :5173'; npm run dev"

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  All services started!"                 -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Frontend:   http://localhost:5173"     -ForegroundColor Yellow
Write-Host "  Gateway:    http://localhost:5000"     -ForegroundColor Yellow
Write-Host "  RabbitMQ:   http://localhost:15672  (guest/guest)" -ForegroundColor Yellow
Write-Host "  Ollama:     http://localhost:11434"    -ForegroundColor Yellow
Write-Host ""
Write-Host "  NOTE: First startup takes ~60s for SQL Server to be ready." -ForegroundColor Gray
Write-Host ""
Write-Host "Press any key to exit this window..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
