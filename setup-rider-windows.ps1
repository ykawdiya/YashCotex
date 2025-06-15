# JetBrains Rider Windows Setup Script
# This script sets up the weighbridge software project in Rider on Windows

param(
    [string]$GitHubRepoUrl = "",
    [string]$ProjectPath = "$env:USERPROFILE\source\repos\weighbridge-software"
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " Weighbridge Software - Rider Setup" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Git is installed
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: Git is not installed" -ForegroundColor Red
    Write-Host "Please install Git from: https://git-scm.com/download/win" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Check if .NET 8.0 SDK is installed
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: .NET SDK is not installed" -ForegroundColor Red
    Write-Host "Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Check .NET version
$dotnetVersion = dotnet --version
Write-Host "Found .NET version: $dotnetVersion" -ForegroundColor Green

# Get GitHub repository URL if not provided
if (-not $GitHubRepoUrl) {
    Write-Host "Enter your GitHub repository URL:" -ForegroundColor Yellow
    Write-Host "Example: https://github.com/yourusername/weighbridge-software.git" -ForegroundColor Gray
    $GitHubRepoUrl = Read-Host "GitHub Repository URL"
}

if (-not $GitHubRepoUrl) {
    Write-Host "ERROR: GitHub repository URL is required" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Create source directory if it doesn't exist
$sourceDir = Split-Path $ProjectPath -Parent
if (-not (Test-Path $sourceDir)) {
    Write-Host "Creating source directory: $sourceDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $sourceDir -Force | Out-Null
}

# Clone the repository
Write-Host "Cloning repository from: $GitHubRepoUrl" -ForegroundColor Yellow
if (Test-Path $ProjectPath) {
    Write-Host "Project directory already exists. Pulling latest changes..." -ForegroundColor Yellow
    Set-Location $ProjectPath
    git pull origin main
} else {
    git clone $GitHubRepoUrl $ProjectPath
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to clone repository" -ForegroundColor Red
        Read-Host "Press Enter to exit"
        exit 1
    }
}

# Navigate to project directory
Set-Location $ProjectPath
Write-Host "Project cloned to: $ProjectPath" -ForegroundColor Green

# Restore NuGet packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to restore packages" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Build the project
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build --configuration Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green

# Check if Rider is installed
$riderPaths = @(
    "${env:ProgramFiles}\JetBrains\JetBrains Rider*\bin\rider64.exe",
    "${env:LOCALAPPDATA}\JetBrains\Toolbox\apps\Rider\ch-0\*\bin\rider64.exe"
)

$riderExe = $null
foreach ($path in $riderPaths) {
    $found = Get-ChildItem -Path $path -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) {
        $riderExe = $found.FullName
        break
    }
}

if ($riderExe) {
    Write-Host "Found JetBrains Rider at: $riderExe" -ForegroundColor Green
    Write-Host "Opening project in Rider..." -ForegroundColor Yellow
    
    # Find the .csproj file
    $csprojFile = Get-ChildItem -Path $ProjectPath -Filter "*.csproj" -Recurse | Select-Object -First 1
    
    if ($csprojFile) {
        Write-Host "Opening project file: $($csprojFile.FullName)" -ForegroundColor Gray
        Start-Process -FilePath $riderExe -ArgumentList "`"$($csprojFile.FullName)`""
    } else {
        Write-Host "Opening project directory in Rider..." -ForegroundColor Gray
        Start-Process -FilePath $riderExe -ArgumentList "`"$ProjectPath`""
    }
} else {
    Write-Host "JetBrains Rider not found. Please install Rider or open the project manually:" -ForegroundColor Yellow
    Write-Host "Project location: $ProjectPath" -ForegroundColor Cyan
    Write-Host "Main project file: WeighbridgeSoftwareYashCotex.csproj" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " Setup Complete!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Project Location: $ProjectPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "To run the application in Rider:" -ForegroundColor Yellow
Write-Host "1. Open WeighbridgeSoftwareYashCotex.csproj in Rider" -ForegroundColor White
Write-Host "2. Set startup project to WeighbridgeSoftwareYashCotex" -ForegroundColor White
Write-Host "3. Click Run (Ctrl+F5) or Debug (F5)" -ForegroundColor White
Write-Host ""
Write-Host "Alternative - Run from command line:" -ForegroundColor Yellow
Write-Host "cd `"$ProjectPath`"" -ForegroundColor Gray
Write-Host "dotnet run" -ForegroundColor Gray
Write-Host ""

Read-Host "Press Enter to exit"