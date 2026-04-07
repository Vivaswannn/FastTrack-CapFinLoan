# CapFinLoan — Stop All Services
# Double-click or run: powershell -ExecutionPolicy Bypass -File stop-all.ps1

Write-Host "Stopping CapFinLoan services..." -ForegroundColor Cyan

# Kill all dotnet run processes for CapFinLoan services
$dotnetProcs = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
$killed = 0

foreach ($proc in $dotnetProcs) {
    try {
        $cmdline = (Get-WmiObject Win32_Process -Filter "ProcessId = $($proc.Id)").CommandLine
        if ($cmdline -match "CapFinLoan") {
            Stop-Process -Id $proc.Id -Force
            $killed++
        }
    } catch {}
}

Write-Host "  Stopped $killed dotnet service(s)" -ForegroundColor Green

# Kill npm / node (Vite frontend)
$nodeProcs = Get-Process -Name "node" -ErrorAction SilentlyContinue
if ($nodeProcs) {
    $nodeProcs | Stop-Process -Force
    Write-Host "  Stopped $($nodeProcs.Count) node/npm process(es)" -ForegroundColor Green
}

Write-Host ""
Write-Host "All CapFinLoan services stopped." -ForegroundColor Cyan
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
