# Weighbridge Software - Windows PowerShell Runner
# This script builds and runs the WPF application on Windows

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Weighbridge Software - Windows Runner" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Function to check if command exists
function Test-Command($command) {
    try {
        if (Get-Command $command -ErrorAction Stop) {
            return $true
        }
    }
    catch {
        return $false
    }
}

# Check if .NET 8.0 is installed
Write-Host "Checking .NET 8.0 installation..." -ForegroundColor Yellow
if (-not (Test-Command "dotnet")) {
    Write-Host "ERROR: .NET is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install .NET 8.0 from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Check .NET version
$dotnetVersion = dotnet --version
Write-Host "Found .NET version: $dotnetVersion" -ForegroundColor Green

# Check if Windows Desktop Runtime is available
Write-Host "Checking Windows Desktop Runtime..." -ForegroundColor Yellow
$runtimes = dotnet --list-runtimes
$hasDesktopRuntime = $runtimes | Where-Object { $_ -match "Microsoft.WindowsDesktop.App" }

if (-not $hasDesktopRuntime) {
    Write-Host "ERROR: Windows Desktop Runtime is not installed" -ForegroundColor Red
    Write-Host "Please install .NET 8.0 Desktop Runtime from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "Windows Desktop Runtime found!" -ForegroundColor Green
Write-Host ""

# Navigate to script directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# Restore packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
$restoreResult = dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to restore packages" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "Packages restored successfully!" -ForegroundColor Green

# Build the application
Write-Host "Building application..." -ForegroundColor Yellow
$buildResult = dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green
Write-Host ""

# Run the application
Write-Host "Starting Weighbridge Software..." -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop the application" -ForegroundColor Gray
Write-Host ""

try {
    dotnet run --configuration Release
}
catch {
    Write-Host "Application encountered an error: $_" -ForegroundColor Red
}

# Keep window open if there's an error
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Application exited with error code: $LASTEXITCODE" -ForegroundColor Red
    Read-Host "Press Enter to exit"
}