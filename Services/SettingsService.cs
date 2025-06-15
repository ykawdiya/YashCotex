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
    
    private SettingsService()
    {
        LoadSettings();
    }
    
    private void LoadSettings()
    {
    }
    
    public void SaveSettings()
    {
        // Trigger settings changed event
        OnSettingsChanged(new SettingsChangedEventArgs
        {
            ChangeType = "General",
            Description = "Settings saved successfully"
        });
    }
    
    public void SaveCompanyInfo()
    {
        SaveSettings();
        OnCompanyInfoChanged("Company information updated");
    }
    
    public void SaveWeighbridgeSettings()
    {
        SaveSettings();
        OnWeighbridgeSettingsChanged("Weighbridge settings updated");
    }
    
    public void SaveDatabaseSettings()
    {
        SaveSettings();
        OnDatabaseSettingsChanged();
    }
    
    public void SaveGoogleSheetsSettings()
    {
        SaveSettings();
        OnGoogleSheetsSettingsChanged();
    }
    
    public void SaveCameraSettings()
    {
        SaveSettings();
        OnCameraSettingsChanged();
    }
    
    public void SavePrinterSettings()
    {
        SaveSettings();
        OnPrinterSettingsChanged();
    }
    
    public void SaveSystemSettings()
    {
        SaveSettings();
        OnSystemSettingsChanged();
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