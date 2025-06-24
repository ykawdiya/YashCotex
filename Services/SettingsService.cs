using System.IO;
using System.Text.Json;
using System;
using WeighbridgeSoftwareYashCotex.Models;
using Newtonsoft.Json;

namespace WeighbridgeSoftwareYashCotex.Services;

public class SettingsService
{
    private static SettingsService? _instance;
    private static bool _isInitializing = false;
    public static SettingsService Instance 
    {
        get
        {
            if (_instance == null && !_isInitializing)
            {
                _isInitializing = true;
                _instance = new SettingsService();
                _isInitializing = false;
            }
            return _instance ?? new SettingsService();
        }
    }
    
    // Events for real-time settings updates
    [field: NonSerialized] public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
    [field: NonSerialized] public event EventHandler<string>? CompanyInfoChanged;
    [field: NonSerialized] public event EventHandler<string>? WeighbridgeSettingsChanged;
    [field: NonSerialized] public event EventHandler? DatabaseSettingsChanged;
    [field: NonSerialized] public event EventHandler? GoogleSheetsSettingsChanged;
    [field: NonSerialized] public event EventHandler? CameraSettingsChanged;
    [field: NonSerialized] public event EventHandler? PrinterSettingsChanged;
    [field: NonSerialized] public event EventHandler? SystemSettingsChanged;
    
    public string? WeighbridgeComPort { get; set; } = "COM1";
    public string CompanyName { get; set; } = "YASH COTEX";
    public string CompanyAddress { get; set; } = "Company Address Here";
    public string CompanyAddressLine1 { get; set; } = "";
    public string CompanyAddressLine2 { get; set; } = "";
    public string CompanyEmail { get; set; } = "email@company.com";
    public string CompanyPhone { get; set; } = "Phone: +91-9876543210";
    public string CompanyGSTIN { get; set; } = "GSTIN: 22AAAAA0000A1Z5";
    public string CompanyLogo { get; set; } = "Assets/logo.png";
    
    public List<WeightRule>? WeightRules { get; set; }
    public List<string> Materials { get; set; } = new();

    public int MaxWeightCapacity { get; set; } = 100000;
    
    // Dot Matrix Printer Settings
    public string DefaultPrinter { get; set; } = string.Empty;
    public string PrinterPaperSize { get; set; } = "Continuous Form (9.5\" x 11\")";
    public string CharactersPerLine { get; set; } = "80 characters";
    public string PrintSpeed { get; set; } = "Draft (Fast)";
    public string FontType { get; set; } = "Draft (9-pin)";
    public string LineSpacing { get; set; } = "6 LPI (Lines Per Inch)";
    public string PaperFeed { get; set; } = "Tractor Feed (Continuous)";
    public bool AutoPrintAfterWeighment { get; set; } = true;
    public bool FormFeedAfterPrint { get; set; } = true;
    
    public string BackupPath { get; set; } = "Backups";
    public bool GoogleSheetsEnabled { get; set; } = false;
    public string ServiceAccountKeyPath { get; set; } = string.Empty;
    public string SpreadsheetId { get; set; } = string.Empty;
    public List<string> Addresses { get; set; } = new();
    public List<WeighbridgeSoftwareYashCotex.Services.CameraConfiguration> Cameras { get; set; } = new();
    public List<WeighbridgeSoftwareYashCotex.Models.LedDisplayConfiguration> LedDisplays { get; set; } = new();
    public WeighbridgeSoftwareYashCotex.Models.RstTemplate RstTemplate { get; set; } = new();
    
    private SettingsService()
    {
        LoadSettings();
    }
    
    private static readonly string SettingsFilePath = GetSettingsFilePath();
    
    private static string GetSettingsFilePath()
    {
        // Use a platform-independent approach
        string settingsDir;
        
        if (OperatingSystem.IsWindows())
        {
            settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YashCotex");
        }
        else if (OperatingSystem.IsMacOS())
        {
            settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "YashCotex");
        }
        else // Linux and other Unix-like systems
        {
            settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "YashCotex");
        }
        
        var settingsPath = Path.Combine(settingsDir, "settings.json");
        Console.WriteLine($"Platform: {Environment.OSVersion.Platform}");
        Console.WriteLine($"Settings directory: {settingsDir}");
        Console.WriteLine($"Settings file path: {settingsPath}");
        return settingsPath;
    }

    private void LoadSettings()
    {
        Console.WriteLine($"=== LOADING SETTINGS ===");
        Console.WriteLine($"Settings file path: {SettingsFilePath}");
        Console.WriteLine($"File exists: {File.Exists(SettingsFilePath)}");
        
        if (File.Exists(SettingsFilePath))
        {
            try
            {
                Console.WriteLine("Reading settings file...");
                var json = File.ReadAllText(SettingsFilePath);
                Console.WriteLine($"File content length: {json.Length} characters");
                // Use JsonDocument instead of deserializing to SettingsService to avoid recursion
                using var document = System.Text.Json.JsonDocument.Parse(json);
                var root = document.RootElement;
                
                if (root.TryGetProperty("CompanyName", out var companyName))
                {
                    CompanyName = companyName.GetString() ?? CompanyName;
                    Console.WriteLine($"Loaded CompanyName: '{CompanyName}'");
                }
                if (root.TryGetProperty("CompanyAddress", out var companyAddress))
                {
                    var loadedAddress = companyAddress.GetString() ?? CompanyAddress;
                    // Clean up corrupted address data - if it contains too many repetitions, reset it
                    if (loadedAddress.Length > 200 || loadedAddress.Split(' ').Length > 30)
                    {
                        Console.WriteLine("Detected corrupted address data, resetting to default");
                        CompanyAddress = "Company Address Here";
                    }
                    else
                    {
                        CompanyAddress = loadedAddress;
                    }
                    Console.WriteLine($"Loaded CompanyAddress: '{CompanyAddress}'");
                }
                if (root.TryGetProperty("CompanyAddressLine1", out var companyAddressLine1))
                {
                    CompanyAddressLine1 = companyAddressLine1.GetString() ?? CompanyAddressLine1;
                    Console.WriteLine($"Loaded CompanyAddressLine1: '{CompanyAddressLine1}'");
                }
                if (root.TryGetProperty("CompanyAddressLine2", out var companyAddressLine2))
                {
                    CompanyAddressLine2 = companyAddressLine2.GetString() ?? CompanyAddressLine2;
                    Console.WriteLine($"Loaded CompanyAddressLine2: '{CompanyAddressLine2}'");
                }
                if (root.TryGetProperty("CompanyEmail", out var companyEmail))
                {
                    CompanyEmail = companyEmail.GetString() ?? CompanyEmail;
                    Console.WriteLine($"Loaded CompanyEmail: '{CompanyEmail}'");
                }
                if (root.TryGetProperty("CompanyPhone", out var companyPhone))
                {
                    CompanyPhone = companyPhone.GetString() ?? CompanyPhone;
                    Console.WriteLine($"Loaded CompanyPhone: '{CompanyPhone}'");
                }
                if (root.TryGetProperty("CompanyGSTIN", out var companyGSTIN))
                {
                    CompanyGSTIN = companyGSTIN.GetString() ?? CompanyGSTIN;
                    Console.WriteLine($"Loaded CompanyGSTIN: '{CompanyGSTIN}'");
                }
                if (root.TryGetProperty("CompanyLogo", out var companyLogo))
                {
                    CompanyLogo = companyLogo.GetString() ?? CompanyLogo;
                    Console.WriteLine($"Loaded CompanyLogo: '{CompanyLogo}'");
                }
                if (root.TryGetProperty("WeighbridgeComPort", out var weighbridgeComPort))
                    WeighbridgeComPort = weighbridgeComPort.GetString() ?? WeighbridgeComPort;
                if (root.TryGetProperty("MaxWeightCapacity", out var maxWeightCapacity))
                    MaxWeightCapacity = maxWeightCapacity.GetInt32();
                if (root.TryGetProperty("DefaultPrinter", out var defaultPrinter))
                    DefaultPrinter = defaultPrinter.GetString() ?? DefaultPrinter;
                if (root.TryGetProperty("BackupPath", out var backupPath))
                    BackupPath = backupPath.GetString() ?? BackupPath;
                if (root.TryGetProperty("GoogleSheetsEnabled", out var googleSheetsEnabled))
                    GoogleSheetsEnabled = googleSheetsEnabled.GetBoolean();
                if (root.TryGetProperty("ServiceAccountKeyPath", out var serviceAccountKeyPath))
                    ServiceAccountKeyPath = serviceAccountKeyPath.GetString() ?? ServiceAccountKeyPath;
                if (root.TryGetProperty("SpreadsheetId", out var spreadsheetId))
                    SpreadsheetId = spreadsheetId.GetString() ?? SpreadsheetId;
                    
                // Handle arrays
                if (root.TryGetProperty("Materials", out var materialsJson))
                {
                    var materials = new List<string>();
                    foreach (var item in materialsJson.EnumerateArray())
                    {
                        var value = item.GetString();
                        if (value != null) materials.Add(value);
                    }
                    Materials = materials;
                }
                
                if (root.TryGetProperty("Addresses", out var addressesJson))
                {
                    var addresses = new List<string>();
                    foreach (var item in addressesJson.EnumerateArray())
                    {
                        var value = item.GetString();
                        if (value != null) addresses.Add(value);
                    }
                    Addresses = addresses;
                }
                
                // Handle WeightRules and Cameras using Newtonsoft.Json for complex objects
                if (root.TryGetProperty("WeightRules", out var weightRulesJson))
                {
                    try
                    {
                        WeightRules = Newtonsoft.Json.JsonConvert.DeserializeObject<List<WeightRule>>(weightRulesJson.GetRawText());
                    }
                    catch { WeightRules = new List<WeightRule>(); }
                }
                
                if (root.TryGetProperty("Cameras", out var camerasJson))
                {
                    try
                    {
                        Cameras = Newtonsoft.Json.JsonConvert.DeserializeObject<List<WeighbridgeSoftwareYashCotex.Services.CameraConfiguration>>(camerasJson.GetRawText()) ?? new List<WeighbridgeSoftwareYashCotex.Services.CameraConfiguration>();
                    }
                    catch { Cameras = new List<WeighbridgeSoftwareYashCotex.Services.CameraConfiguration>(); }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load settings: " + ex.Message);
            }
        }
    }
    
    public void SaveSettings()
    {
        try
        {
            Console.WriteLine($"Attempting to save settings to: {SettingsFilePath}");
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath)!);
            Console.WriteLine($"Directory created/verified: {Path.GetDirectoryName(SettingsFilePath)}");
            
            // Create a settings object to serialize (avoiding circular references)
            var settingsData = new
            {
                CompanyName,
                CompanyAddress,
                CompanyAddressLine1,
                CompanyAddressLine2,
                CompanyEmail,
                CompanyPhone,
                CompanyGSTIN,
                CompanyLogo,
                WeighbridgeComPort,
                MaxWeightCapacity,
                DefaultPrinter,
                BackupPath,
                GoogleSheetsEnabled,
                ServiceAccountKeyPath,
                SpreadsheetId,
                Materials,
                Addresses,
                WeightRules,
                Cameras
            };
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            var json = System.Text.Json.JsonSerializer.Serialize(settingsData, options);
            File.WriteAllText(SettingsFilePath, json);
            Console.WriteLine($"Settings saved successfully to: {SettingsFilePath}");
            Console.WriteLine($"File size: {new FileInfo(SettingsFilePath).Length} bytes");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to save settings: " + ex.Message);
            Console.WriteLine("Stack trace: " + ex.StackTrace);
        }

        RefreshAll();
    }
    
    public void SaveCompanyInfo()
    {
        SaveSettings();
        OnCompanyInfoChanged("Company information updated");
        OnSettingsChanged(new SettingsChangedEventArgs
        {
            ChangeType = "CompanyInfo",
            Description = "Company information saved"
        });
    }
    
    public void SaveWeighbridgeSettings()
    {
        SaveSettings();
        OnWeighbridgeSettingsChanged("Weighbridge settings updated");
        OnSettingsChanged(new SettingsChangedEventArgs
        {
            ChangeType = "Weighbridge",
            Description = "Weighbridge settings saved"
        });
    }
    
    public void SaveDatabaseSettings()
    {
        SaveSettings();
        OnDatabaseSettingsChanged();
        OnSettingsChanged(new SettingsChangedEventArgs
        {
            ChangeType = "Database",
            Description = "Database settings saved"
        });
    }
    
    public void SaveGoogleSheetsSettings()
    {
        SaveSettings();
        OnGoogleSheetsSettingsChanged();
        OnSettingsChanged(new SettingsChangedEventArgs
        {
            ChangeType = "GoogleSheets",
            Description = "Google Sheets settings saved"
        });
    }
    
    public void SaveCameraSettings()
    {
        SaveSettings();
        OnCameraSettingsChanged();
        OnSettingsChanged(new SettingsChangedEventArgs
        {
            ChangeType = "Camera",
            Description = "Camera settings saved"
        });
    }
    
    public void SavePrinterSettings()
    {
        SaveSettings();
        OnPrinterSettingsChanged();
        OnSettingsChanged(new SettingsChangedEventArgs
        {
            ChangeType = "Printer",
            Description = "Printer settings saved"
        });
    }
    
    public void SaveSystemSettings()
    {
        SaveSettings();
        OnSystemSettingsChanged();
        OnSettingsChanged(new SettingsChangedEventArgs
        {
            ChangeType = "System",
            Description = "System settings saved"
        });
    }
    
    protected virtual void OnSettingsChanged(SettingsChangedEventArgs e)
    {
        SettingsChanged?.Invoke(this, e);
    }
    
    protected virtual void OnCompanyInfoChanged(string message)
    {
        CompanyInfoChanged?.Invoke(this, message);
    }
    
    protected virtual void OnWeighbridgeSettingsChanged(string message)
    {
        WeighbridgeSettingsChanged?.Invoke(this, message);
    }
    
    protected virtual void OnDatabaseSettingsChanged()
    {
        DatabaseSettingsChanged?.Invoke(this, EventArgs.Empty);
    }
    
    protected virtual void OnGoogleSheetsSettingsChanged()
    {
        GoogleSheetsSettingsChanged?.Invoke(this, EventArgs.Empty);
    }
    
    protected virtual void OnCameraSettingsChanged()
    {
        CameraSettingsChanged?.Invoke(this, EventArgs.Empty);
    }
    
    protected virtual void OnPrinterSettingsChanged()
    {
        PrinterSettingsChanged?.Invoke(this, EventArgs.Empty);
    }
    
    protected virtual void OnSystemSettingsChanged()
    {
        SystemSettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RefreshAll()
    {
        OnCompanyInfoChanged("Auto-refreshed");
        OnWeighbridgeSettingsChanged("Auto-refreshed");
        OnDatabaseSettingsChanged();
        OnGoogleSheetsSettingsChanged();
        OnCameraSettingsChanged();
        OnPrinterSettingsChanged();
        OnSystemSettingsChanged();
        OnSettingsChanged(new SettingsChangedEventArgs
        {
            ChangeType = "All",
            Description = "All settings refreshed"
        });
    }
}

public class SettingsChangedEventArgs : EventArgs
{
    public string ChangeType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class WeightRule
{
    public string Name { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public double AdjustmentValue { get; set; }
    public bool IsPercentage { get; set; }
    
    public double ApplyRule(double weight)
    {
        return weight;
    }
}