@echo off
REM JetBrains Rider Windows Setup Script (Batch Version)
REM This script sets up the weighbridge software project in Rider on Windows

echo =========================================
echo  Weighbridge Software - Rider Setup
echo =========================================
echo.

REM Check if Git is installed
git --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Git is not installed
    echo Please install Git from: https://git-scm.com/download/win
    pause
    exit /b 1
)

REM Check if .NET is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK is not installed
    echo Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo Found .NET version:
dotnet --version

REM Get GitHub repository URL
set /p REPO_URL="Enter your GitHub repository URL: "
if "%REPO_URL%"=="" (
    echo ERROR: GitHub repository URL is required
    pause
    exit /b 1
)

REM Set project path
set PROJECT_PATH=%USERPROFILE%\source\repos\weighbridge-software

REM Create source directory
if not exist "%USERPROFILE%\source\repos" (
    echo Creating source directory...
    mkdir "%USERPROFILE%\source\repos"
)

REM Clone or update repository
if exist "%PROJECT_PATH%" (
    echo Project directory exists. Pulling latest changes...
    cd /d "%PROJECT_PATH%"
    git pull origin main
) else (
    echo Cloning repository...
    git clone "%REPO_URL%" "%PROJECT_PATH%"
    if %errorlevel% neq 0 (
        echo ERROR: Failed to clone repository
        pause
        exit /b 1
    )
)

REM Navigate to project
cd /d "%PROJECT_PATH%"
echo Project location: %PROJECT_PATH%

REM Restore packages
echo Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore packages
    pause
    exit /b 1
)

REM Build project
echo Building project...
dotnet build --configuration Debug
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo Build successful!

REM Try to find and open Rider
set RIDER_EXE=
for /d %%i in ("%ProgramFiles%\JetBrains\JetBrains Rider*") do (
    if exist "%%i\bin\rider64.exe" (
        set RIDER_EXE=%%i\bin\rider64.exe
        goto :found_rider
    )
)

for /d %%i in ("%LOCALAPPDATA%\JetBrains\Toolbox\apps\Rider\ch-0\*") do (
    if exist "%%i\bin\rider64.exe" (
        set RIDER_EXE=%%i\bin\rider64.exe
        goto :found_rider
    )
)

:found_rider
if defined RIDER_EXE (
    echo Found JetBrains Rider: %RIDER_EXE%
    echo Opening project in Rider...
    
    REM Find the .csproj file
    for /r . %%f in (*.csproj) do (
        echo Opening: %%f
        start "" "%RIDER_EXE%" "%%f"
        goto :opened
    )
    
    REM If no .csproj found, open directory
    start "" "%RIDER_EXE%" "%PROJECT_PATH%"
    :opened
) else (
    echo JetBrains Rider not found.
    echo Please install Rider or open the project manually:
    echo Project location: %PROJECT_PATH%
    echo Main project file: WeighbridgeSoftwareYashCotex.csproj
)

echo.
echo =========================================
echo  Setup Complete!
echo =========================================
echo Project Location: %PROJECT_PATH%
echo.
echo To run the application in Rider:
echo 1. Open WeighbridgeSoftwareYashCotex.csproj in Rider
echo 2. Set startup project to WeighbridgeSoftwareYashCotex
echo 3. Click Run (Ctrl+F5) or Debug (F5)
echo.
echo Alternative - Run from command line:
echo cd "%PROJECT_PATH%"
echo dotnet run
echo.

pause