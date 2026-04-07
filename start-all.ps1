# CapFinLoan — Start All Services
# Double-click or run: powershell -ExecutionPolicy Bypass -File start-all.ps1

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$backend = "$root\Backend\Services"
$gateway = "$root\Backend\ApiGateway\CapFinLoan.Gateway"
$frontend = "$root\Frontend\capfinloan-frontend"

# ── Kill any already-running services on these ports ─────────────────────────
Write-Host "Stopping any existing services..." -ForegroundColor Yellow
$ports = @(5000, 5001, 5002, 5003, 5004, 5005, 5006)
foreach ($port in $ports) {
    $conn = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    if ($conn) {
        $pid = $conn.OwningProcess
        Write-Host "  Killing process $pid on port $port" -ForegroundColor Yellow
        Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
    }
}
# Also kill any dotnet processes that may have locked build output
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
    $_.MainWindowTitle -like "*CapFinLoan*"
} | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
Write-Host "  Done. All previous instances stopped." -ForegroundColor Yellow
Write-Host ""

$services = @(
    @{ Name = "Gateway        :5000"; Path = $gateway;                                           Proj = "CapFinLoan.Gateway.csproj" },
    @{ Name = "AuthService    :5001"; Path = "$backend\CapFinLoan.AuthService";                  Proj = "CapFinLoan.AuthService.csproj" },
    @{ Name = "AppService     :5002"; Path = "$backend\CapFinLoan.ApplicationService";           Proj = "CapFinLoan.ApplicationService.csproj" },
    @{ Name = "DocumentService:5003"; Path = "$backend\CapFinLoan.DocumentService";              Proj = "CapFinLoan.DocumentService.csproj" },
    @{ Name = "AdminService   :5004"; Path = "$backend\CapFinLoan.AdminService";                 Proj = "CapFinLoan.AdminService.csproj" },
    @{ Name = "Notification   :5005"; Path = "$backend\CapFinLoan.NotificationService";          Proj = "CapFinLoan.NotificationService.csproj" },
    @{ Name = "PaymentService :5006"; Path = "$backend\CapFinLoan.PaymentService";               Proj = "CapFinLoan.PaymentService.csproj" }
)

Write-Host "Starting CapFinLoan services..." -ForegroundColor Cyan

foreach ($svc in $services) {
    $title = $svc.Name
    $path  = $svc.Path
    $proj  = $svc.Proj
    Start-Process powershell -ArgumentList "-NoExit", "-Command", `
        "Set-Location '$path'; `$host.UI.RawUI.WindowTitle = '$title'; dotnet run --project $proj"
    Write-Host "  Started $title" -ForegroundColor Green
    Start-Sleep -Milliseconds 300
}

# Start React frontend
Write-Host "  Starting Frontend    :5173" -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", `
    "Set-Location '$frontend'; `$host.UI.RawUI.WindowTitle = 'Frontend :5173'; npm run dev"

Write-Host ""
Write-Host "All services started!" -ForegroundColor Cyan
Write-Host "  Gateway:    http://localhost:5000" -ForegroundColor Yellow
Write-Host "  Frontend:   http://localhost:5173" -ForegroundColor Yellow
Write-Host "  RabbitMQ:   http://localhost:15672  (guest/guest)" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press any key to exit this window..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
