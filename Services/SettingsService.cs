using System.IO;
using System.Text.Json;
using System;
using WeighbridgeSoftwareYashCotex.Models;

namespace WeighbridgeSoftwareYashCotex.Services;

public class SettingsService
{
    private static SettingsService? _instance;
    public static SettingsService Instance => _instance ??= new SettingsService();
    
    // Events for real-time settings updates
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
    public event EventHandler<string>? CompanyInfoChanged;
    public event EventHandler<string>? WeighbridgeSettingsChanged;
    public event EventHandler? DatabaseSettingsChanged;
    public event EventHandler? GoogleSheetsSettingsChanged;
    public event EventHandler? CameraSettingsChanged;
    public event EventHandler? PrinterSettingsChanged;
    public event EventHandler? SystemSettingsChanged;
    
    public string? WeighbridgeComPort { get; set; } = "COM1";
    public string CompanyName { get; set; } = "YASH COTEX";
    public string CompanyAddress { get; set; } = "Company Address Here";
    public string CompanyEmail { get; set; } = "email@company.com";
    public string CompanyPhone { get; set; } = "Phone: +91-9876543210";
    public string CompanyGSTIN { get; set; } = "GSTIN: 22AAAAA0000A1Z5";
    public string CompanyLogo { get; set; } = "Assets/logo.png";
    
    public List<WeightRule>? WeightRules { get; set; }
    public List<string> Materials { get; set; } = new();
    
    private SettingsService()
    {
        LoadSettings();
    }
    
    private const string SettingsFilePath = "settings.json";

    private void LoadSettings()
    {
        if (File.Exists(SettingsFilePath))
        {
            try
            {
                var json = File.ReadAllText(SettingsFilePath);
                var loaded = JsonSerializer.Deserialize<SettingsService>(json);
                if (loaded != null)
                {
                    CompanyName = loaded.CompanyName;
                    CompanyAddress = loaded.CompanyAddress;
                    CompanyEmail = loaded.CompanyEmail;
                    CompanyPhone = loaded.CompanyPhone;
                    CompanyGSTIN = loaded.CompanyGSTIN;
                    CompanyLogo = loaded.CompanyLogo;
                    WeighbridgeComPort = loaded.WeighbridgeComPort;
                    WeightRules = loaded.WeightRules;
                    Materials = loaded.Materials ?? new List<string>();
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
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to save settings: " + ex.Message);
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