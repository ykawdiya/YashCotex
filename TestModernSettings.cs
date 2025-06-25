using System;
using System.Linq;
using WeighbridgeSoftwareYashCotex.ViewModels;

namespace WeighbridgeSoftwareYashCotex.Test
{
    /// <summary>
    /// Test program to verify that the ComprehensiveModernSettingsViewModel initializes all fields correctly
    /// </summary>
    public class TestModernSettings
    {
        public static void RunTests()
        {
            Console.WriteLine("\n=== Testing ComprehensiveModernSettingsViewModel ===");
            
            try
            {
                var viewModel = new ComprehensiveModernSettingsViewModel();
                
                // Test Company Settings
                Console.WriteLine("\n1. Testing Company Settings:");
                var companyGroup = viewModel.CompanySettings.FirstOrDefault();
                if (companyGroup != null)
                {
                    Console.WriteLine($"   Group Title: '{companyGroup.Title}'");
                    Console.WriteLine($"   Field Count: {companyGroup.Fields.Count}");
                    Console.WriteLine($"   Column Count: {companyGroup.ColumnCount}");
                    
                    var companyNameField = companyGroup.Fields.FirstOrDefault(f => f.Key == "CompanyName");
                    if (companyNameField != null)
                    {
                        Console.WriteLine($"   Company Name DefaultValue: '{companyNameField.DefaultValue}'");
                        Console.WriteLine($"   Company Name Value: '{companyNameField.Value}'");
                        Console.WriteLine($"   Company Name IsRequired: {companyNameField.IsRequired}");
                        Console.WriteLine($"   Values initialized correctly: {companyNameField.Value != null && !string.IsNullOrEmpty(companyNameField.Value.ToString())}");
                    }
                }
                
                // Test Hardware Settings
                Console.WriteLine("\n2. Testing Hardware Settings:");
                if (viewModel.HardwareSettings.Any())
                {
                    var weighbridgeGroup = viewModel.HardwareSettings.FirstOrDefault(g => g.Title.Contains("Weighbridge"));
                    if (weighbridgeGroup != null)
                    {
                        Console.WriteLine($"   Group Title: '{weighbridgeGroup.Title}'");
                        
                        var comPortField = weighbridgeGroup.Fields.FirstOrDefault(f => f.Key == "WeighbridgeComPort");
                        if (comPortField != null)
                        {
                            Console.WriteLine($"   COM Port DefaultValue: '{comPortField.DefaultValue}'");
                            Console.WriteLine($"   COM Port Value: '{comPortField.Value}'");
                            Console.WriteLine($"   COM Port Options: {comPortField.Options?.Count ?? 0}");
                            Console.WriteLine($"   Values match: {comPortField.DefaultValue?.ToString() == comPortField.Value?.ToString()}");
                        }
                    }
                }
                
                // Test Camera Settings
                Console.WriteLine("\n3. Testing Camera Settings:");
                if (viewModel.CameraSettings.Any())
                {
                    var camera1Group = viewModel.CameraSettings.FirstOrDefault(g => g.Title.Contains("Camera 1"));
                    if (camera1Group != null)
                    {
                        Console.WriteLine($"   Group Title: '{camera1Group.Title}'");
                        
                        var enableField = camera1Group.Fields.FirstOrDefault(f => f.Key == "Camera1Enable");
                        if (enableField != null)
                        {
                            Console.WriteLine($"   Enable DefaultValue: '{enableField.DefaultValue}' (Type: {enableField.DefaultValue?.GetType().Name})");
                            Console.WriteLine($"   Enable Value: '{enableField.Value}' (Type: {enableField.Value?.GetType().Name})");
                            Console.WriteLine($"   Is boolean field: {enableField.FieldType == Models.FieldType.Checkbox}");
                        }
                        
                        var nameField = camera1Group.Fields.FirstOrDefault(f => f.Key == "Camera1Name");
                        if (nameField != null)
                        {
                            Console.WriteLine($"   Name DefaultValue: '{nameField.DefaultValue}'");
                            Console.WriteLine($"   Name Value: '{nameField.Value}'");
                        }
                    }
                }
                
                // Test System Settings
                Console.WriteLine("\n4. Testing System Settings:");
                if (viewModel.SystemSettings.Any())
                {
                    var systemGroup = viewModel.SystemSettings.FirstOrDefault();
                    if (systemGroup != null)
                    {
                        Console.WriteLine($"   Group Title: '{systemGroup.Title}'");
                        
                        var versionField = systemGroup.Fields.FirstOrDefault(f => f.Key == "SoftwareVersion");
                        if (versionField != null)
                        {
                            Console.WriteLine($"   Software Version DefaultValue: '{versionField.DefaultValue}'");
                            Console.WriteLine($"   Software Version Value: '{versionField.Value}'");
                            Console.WriteLine($"   Is Enabled: {versionField.IsEnabled}");
                            Console.WriteLine($"   Values properly initialized: {versionField.Value != null}");
                        }
                    }
                }
                
                // Test Weight Rules Settings
                Console.WriteLine("\n5. Testing Weight Rules Settings:");
                if (viewModel.WeightRulesSettings.Any())
                {
                    var toleranceGroup = viewModel.WeightRulesSettings.FirstOrDefault();
                    if (toleranceGroup != null)
                    {
                        Console.WriteLine($"   Group Title: '{toleranceGroup.Title}'");
                        
                        var minWeightField = toleranceGroup.Fields.FirstOrDefault(f => f.Key == "MinWeightDifference");
                        if (minWeightField != null)
                        {
                            Console.WriteLine($"   Min Weight DefaultValue: '{minWeightField.DefaultValue}'");
                            Console.WriteLine($"   Min Weight Value: '{minWeightField.Value}'");
                            Console.WriteLine($"   Field Type: {minWeightField.FieldType}");
                        }
                    }
                }
                
                // Count total fields and check initialization
                int totalFields = 0;
                int initializedFields = 0;
                
                var allGroups = new[]
                {
                    viewModel.CompanySettings,
                    viewModel.HardwareSettings,
                    viewModel.CameraSettings,
                    viewModel.IntegrationSettings,
                    viewModel.DataManagementSettings,
                    viewModel.SecuritySettings,
                    viewModel.WeightRulesSettings,
                    viewModel.UserSettings,
                    viewModel.SystemSettings,
                    viewModel.AdminToolsSettings
                };
                
                foreach (var collection in allGroups)
                {
                    foreach (var group in collection)
                    {
                        foreach (var field in group.Fields)
                        {
                            totalFields++;
                            if (field.Value != null)
                            {
                                initializedFields++;
                            }
                        }
                    }
                }
                
                Console.WriteLine($"\n6. Overall Statistics:");
                Console.WriteLine($"   Total Fields: {totalFields}");
                Console.WriteLine($"   Initialized Fields: {initializedFields}");
                Console.WriteLine($"   Initialization Rate: {(double)initializedFields / totalFields * 100:F1}%");
                Console.WriteLine($"   All fields initialized: {initializedFields == totalFields}");
                
                Console.WriteLine("\n=== Modern Settings Tests Completed Successfully ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Test failed with error: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
        }
    }
}