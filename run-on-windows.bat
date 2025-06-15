@echo off
echo ========================================
echo  Weighbridge Software - Windows Runner
echo ========================================
echo.

REM Check if .NET 8.0 is installed
echo Checking .NET 8.0 installation...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET is not installed or not in PATH
    echo Please install .NET 8.0 from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

REM Check if Windows Desktop Runtime is available
echo Checking Windows Desktop Runtime...
dotnet --list-runtimes | findstr "Microsoft.WindowsDesktop.App" >nul
if %errorlevel% neq 0 (
    echo ERROR: Windows Desktop Runtime is not installed
    echo Please install .NET 8.0 Desktop Runtime from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo .NET 8.0 and Windows Desktop Runtime found!
echo.

REM Navigate to project directory
cd /d "%~dp0"

REM Restore packages
echo Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore packages
    pause
    exit /b 1
)

REM Build the application
echo Building application...
dotnet build --configuration Release
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo Build successful!
echo.

REM Run the application
echo Starting Weighbridge Software...
echo.
dotnet run --configuration Release

REM Keep window open if there's an error
if %errorlevel% neq 0 (
    echo.
    echo Application exited with error code: %errorlevel%
    pause
)