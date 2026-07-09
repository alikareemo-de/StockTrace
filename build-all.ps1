$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $root "StockTrace.sln"
$frontendPath = Join-Path $root "frontend\testing-ui"

Write-Host "StockTrace full build" -ForegroundColor Cyan
Write-Host "Root: $root"

if (-not (Test-Path -LiteralPath $solution)) {
    throw "Solution file was not found: $solution"
}

if (-not (Test-Path -LiteralPath (Join-Path $frontendPath "package.json"))) {
    throw "Frontend package.json was not found: $frontendPath"
}

Write-Host ""
Write-Host "1/4 Restoring .NET solution..." -ForegroundColor Yellow
dotnet restore $solution

Write-Host ""
Write-Host "2/4 Building .NET solution..." -ForegroundColor Yellow
dotnet build $solution --configuration Release --no-restore

Write-Host ""
Write-Host "3/4 Running .NET tests..." -ForegroundColor Yellow
dotnet test $solution --configuration Release --no-build

Write-Host ""
Write-Host "4/4 Building frontend..." -ForegroundColor Yellow
Push-Location $frontendPath
try {
    if (-not (Test-Path -LiteralPath "node_modules")) {
        npm install
    }

    npm run build
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "Full build completed successfully." -ForegroundColor Green
