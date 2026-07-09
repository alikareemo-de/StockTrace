param(
    [switch]$NoBrowser
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiProject = Join-Path $root "src\StockTrace.Api\StockTrace.Api.csproj"
$frontendPath = Join-Path $root "frontend\testing-ui"

function Get-ListeningProcess {
    param([int]$Port)
    $connection = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue |
        Where-Object { $_.State -eq "Listen" } |
        Select-Object -First 1
    if (-not $connection) { return $null }

    return Get-CimInstance Win32_Process -Filter "ProcessId = $($connection.OwningProcess)" -ErrorAction SilentlyContinue
}

function Start-DevWindow {
    param(
        [string]$Title,
        [string]$WorkingDirectory,
        [string]$Command
    )

    $escapedTitle = $Title.Replace("'", "''")
    $escapedPath = $WorkingDirectory.Replace("'", "''")
    $fullCommand = "`$Host.UI.RawUI.WindowTitle = '$escapedTitle'; Set-Location -LiteralPath '$escapedPath'; $Command"

    Start-Process powershell.exe -ArgumentList @(
        "-NoExit",
        "-ExecutionPolicy",
        "Bypass",
        "-Command",
        $fullCommand
    )
}

Write-Host "StockTrace development startup" -ForegroundColor Cyan
Write-Host "Root: $root"

if (-not (Test-Path $apiProject)) {
    throw "API project was not found at: $apiProject"
}

if (-not (Test-Path (Join-Path $frontendPath "package.json"))) {
    throw "Frontend package.json was not found at: $frontendPath"
}

$backendProcess = Get-ListeningProcess 5133
if ($backendProcess) {
    if ($backendProcess.CommandLine -notlike "*StockTrace.Api*" -and $backendProcess.Name -notlike "StockTrace.Api*") {
        Write-Host "Backend port 5133 is already used by another process:" -ForegroundColor Red
        Write-Host "PID: $($backendProcess.ProcessId)"
        Write-Host "Name: $($backendProcess.Name)"
        Write-Host "Close that process or run .\stop-dev.ps1, then start again." -ForegroundColor Red
    } else {
        Write-Host "Backend port 5133 is already used by StockTrace. The existing backend will be reused." -ForegroundColor Yellow
    }
} else {
    Start-DevWindow `
        -Title "StockTrace Backend - http://localhost:5133" `
        -WorkingDirectory $root `
        -Command "dotnet run --project '$apiProject' --urls http://localhost:5133"
}

if (-not (Test-Path (Join-Path $frontendPath "node_modules"))) {
    Write-Host "Frontend node_modules not found. Running npm install first..." -ForegroundColor Yellow
    Push-Location $frontendPath
    try {
        npm install
    } finally {
        Pop-Location
    }
}

$frontendProcess = Get-ListeningProcess 5173
if ($frontendProcess) {
    Write-Host "Frontend port 5173 is already in use. The existing frontend will be reused." -ForegroundColor Yellow
} else {
    Start-DevWindow `
        -Title "StockTrace Frontend - http://127.0.0.1:5173" `
        -WorkingDirectory $frontendPath `
        -Command "npm run dev -- --host 127.0.0.1 --port 5173"
}

Write-Host ""
Write-Host "Backend:  http://localhost:5133/swagger" -ForegroundColor Green
Write-Host "Frontend: http://127.0.0.1:5173" -ForegroundColor Green
Write-Host "Login:    admin / Admin@12345" -ForegroundColor Green

if (-not $NoBrowser) {
    Start-Process "http://localhost:5133/swagger"
    Start-Process "http://127.0.0.1:5173"
}
