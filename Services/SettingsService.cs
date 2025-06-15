using WeighbridgeSoftwareYashCotex.Models;

namespace WeighbridgeSoftwareYashCotex.Services;

public class SettingsService
{
    private static SettingsService? _instance;
    public static SettingsService Instance => _instance ??= new SettingsService();
    
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
    }
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