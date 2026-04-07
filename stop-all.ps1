# CapFinLoan — Stop All Services (Docker mode)
# Double-click or run: powershell -ExecutionPolicy Bypass -File stop-all.ps1

$root = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CapFinLoan — Stopping All Services"    -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ── Stop Docker containers ────────────────────────────────────────────────────
Write-Host "Stopping Docker containers..." -ForegroundColor Yellow
Set-Location $root
docker-compose down

Write-Host ""

# ── Stop React frontend (node/npm) ────────────────────────────────────────────
$nodeProcs = Get-Process -Name "node" -ErrorAction SilentlyContinue
if ($nodeProcs) {
    $nodeProcs | Stop-Process -Force
    Write-Host "Stopped $($nodeProcs.Count) frontend process(es)" -ForegroundColor Green
}

Write-Host ""
Write-Host "All services stopped." -ForegroundColor Cyan
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
