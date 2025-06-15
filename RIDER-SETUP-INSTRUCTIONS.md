# JetBrains Rider Setup for Weighbridge Software

This guide helps you quickly set up and run the Weighbridge Software project in JetBrains Rider on Windows.

## Quick Setup (Automated)

### Option 1: PowerShell Script (Recommended)
1. **Download** the `setup-rider-windows.ps1` file to your Windows machine
2. **Right-click** and select "Run with PowerShell"
3. **Enter your GitHub repository URL** when prompted
4. The script will automatically:
   - Clone the repository
   - Restore NuGet packages
   - Build the project
   - Open it in Rider (if installed)

### Option 2: Batch File
1. **Download** the `setup-rider-windows.bat` file to your Windows machine
2. **Double-click** to run
3. **Enter your GitHub repository URL** when prompted
4. Follow the same automated process

## Manual Setup

### Prerequisites
1. **JetBrains Rider** - Download from: https://www.jetbrains.com/rider/
2. **.NET 8.0 SDK** - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
3. **Git** - Download from: https://git-scm.com/download/win

### Manual Steps
1. **Clone Repository**:
   ```bash
   git clone <your-github-repo-url> C:\source\repos\weighbridge-software
   cd C:\source\repos\weighbridge-software
   ```

2. **Restore and Build**:
   ```bash
   dotnet restore
   dotnet build
   ```

3. **Open in Rider**:
   - Launch JetBrains Rider
   - Click "Open" or "Open Project"
   - Navigate to `C:\source\repos\weighbridge-software`
   - Select `WeighbridgeSoftwareYashCotex.csproj`

## Running the Application in Rider

### Method 1: Using Run Configuration
1. **Open the project** in Rider
2. **Look for the run configuration** dropdown (top toolbar)
3. **Select** "WeighbridgeSoftwareYashCotex"
4. **Click the green play button** or press `Ctrl+F5`

### Method 2: Using Debug Configuration
1. **Set breakpoints** if needed
2. **Click the debug button** or press `F5`
3. **The WPF application will launch** with debugging enabled

### Method 3: Command Line in Rider Terminal
1. **Open terminal** in Rider (`Alt+F12`)
2. **Run**: `dotnet run`

## Project Structure in Rider

```
WeighbridgeSoftwareYashCotex/
├── App.xaml & App.xaml.cs          # Application entry point
├── MainWindow.xaml & .xaml.cs      # Main application window
├── Views/
│   ├── EntryWindow.xaml & .cs      # Vehicle entry form
│   └── ExitWindow.xaml & .cs       # Vehicle exit form
├── Services/
│   ├── DatabaseService.cs          # Database operations
│   ├── WeightService.cs            # Weight capture
│   └── PrintService.cs             # Receipt printing
├── Models/
│   ├── WeighmentEntry.cs           # Main data model
│   ├── Customer.cs                 # Customer model
│   └── Material.cs                 # Material model
└── Data/
    └── WeighbridgeDbContext.cs     # Entity Framework context
```

## Rider-Specific Features

### 1. Debugging
- **Set breakpoints** by clicking in the gutter
- **Step through code** with F10 (step over) and F11 (step into)
- **Inspect variables** in the debugger window

### 2. XAML Designer
- **Visual designer** for WPF windows
- **Live preview** of UI changes
- **Property inspector** for controls

### 3. Database Tools
- **Connect to SQLite database** created by the app
- **View data** in the database explorer
- **Run SQL queries** directly in Rider

### 4. NuGet Package Management
- **Manage packages** via Rider's NuGet window
- **Update dependencies** easily
- **View package references**

## Troubleshooting

### "Project doesn't load"
- Make sure .NET 8.0 SDK is installed
- Check that all NuGet packages are restored
- Try "File" → "Reload Project"

### "WPF controls not recognized"
- Ensure Windows Desktop Runtime is installed
- Check project file targets `net8.0-windows`
- Verify `<UseWPF>true</UseWPF>` in project file

### "Database errors"
- The app creates SQLite database automatically
- Check write permissions in project directory
- Database file: `weighbridge.db` (created on first run)

### "Build errors"
- Run `dotnet restore` in terminal
- Check all using statements are correct
- Verify all NuGet packages are compatible

## Keyboard Shortcuts in Rider

- **F5**: Start Debugging
- **Ctrl+F5**: Start Without Debugging
- **Shift+F5**: Stop Debugging
- **F9**: Toggle Breakpoint
- **F10**: Step Over
- **F11**: Step Into
- **Ctrl+Shift+F10**: Run Current File
- **Alt+F12**: Terminal

## Application Shortcuts

Once running, the application supports:
- **F6**: Save Entry/Exit
- **F7**: Print Receipt / Clear Form
- **F8**: Clear Search (Exit window)
- **Esc**: Close window

## Next Steps

1. **Run the automated setup script**
2. **Open project in Rider**
3. **Press F5 to run with debugging**
4. **Test the application features**
5. **Set breakpoints to understand the code flow**

The application should run perfectly in Rider on Windows with full debugging and design-time support!