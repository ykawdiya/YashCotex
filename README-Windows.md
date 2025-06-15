# Weighbridge Software - Windows Setup

This WPF application has been converted from MAUI and is now Windows-only. Follow these instructions to run it on Windows.

## Prerequisites

1. **Windows 10/11** (required for WPF applications)
2. **.NET 8.0 Desktop Runtime** - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Make sure to download the "Desktop Runtime" version, not just the regular runtime

## Running the Application

### Option 1: Using Batch File (Recommended)
1. Copy the entire project folder to your Windows machine
2. Double-click `run-on-windows.bat`
3. The script will automatically:
   - Check for .NET installation
   - Restore NuGet packages
   - Build the application
   - Run the application

### Option 2: Using PowerShell (Advanced)
1. Copy the entire project folder to your Windows machine
2. Right-click on `run-on-windows.ps1` and select "Run with PowerShell"
3. If you get an execution policy error, run PowerShell as Administrator and execute:
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   ```

### Option 3: Manual Command Line
1. Open Command Prompt or PowerShell in the project directory
2. Run the following commands:
   ```cmd
   dotnet restore
   dotnet build --configuration Release
   dotnet run --configuration Release
   ```

## Application Features

- **Vehicle Entry**: Record vehicle details and entry weight
- **Vehicle Exit**: Search for entries and record exit weight
- **Database**: SQLite database for storing weighment records
- **Printing**: Generate receipt files in Documents/WeighbridgePrints
- **Material Management**: Configurable materials list

## Keyboard Shortcuts

- **F6**: Save Entry/Exit
- **F7**: Print Receipt / Clear Form
- **F8**: Clear Search (Exit window)
- **Esc**: Close window

## Troubleshooting

### "Framework not found" error
- Install .NET 8.0 Desktop Runtime from Microsoft's website
- Make sure you download the Desktop Runtime, not just the regular runtime

### "Cannot resolve symbol" errors
- The project has been converted to WPF and should build without errors on Windows
- Make sure all NuGet packages are restored

### Database issues
- The application uses SQLite and will create the database automatically
- Database file is created in the application directory

## Project Structure

- `MainWindow.xaml/.cs`: Main application window with navigation
- `Views/EntryWindow.xaml/.cs`: Vehicle entry form
- `Views/ExitWindow.xaml/.cs`: Vehicle exit processing
- `Services/DatabaseService.cs`: Database operations
- `Services/WeightService.cs`: Weight capture simulation
- `Services/PrintService.cs`: Receipt generation
- `Models/`: Data models for weighment entries and customers

## Notes

- This application was converted from .NET MAUI to WPF for compatibility
- It requires Windows to run due to WPF framework limitations
- All build errors have been resolved and the application should run smoothly on Windows