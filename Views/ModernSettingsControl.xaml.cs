using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WeighbridgeSoftwareYashCotex.ViewModels;

namespace WeighbridgeSoftwareYashCotex.Views
{
    public partial class ModernSettingsControl : UserControl, IDisposable
    {
        public event EventHandler<string>? FormCompleted;

        public ModernSettingsControl()
        {
            InitializeComponent();
            
            // Create ViewModel in code-behind instead of XAML to ensure proper initialization
            try
            {
                var viewModel = new ComprehensiveModernSettingsViewModel();
                this.DataContext = viewModel;
                Console.WriteLine($"✅ ModernSettingsControl: ViewModel created with {GetTotalFieldCount(viewModel)} fields");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ModernSettingsControl: Error creating ViewModel: {ex.Message}");
            }
            
            this.Loaded += ModernSettingsControl_Loaded;
        }

        private void ModernSettingsControl_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("🔄 ModernSettingsControl_Loaded called");
            
            // Initialize any additional setup here
            if (DataContext is ComprehensiveModernSettingsViewModel viewModel)
            {
                Console.WriteLine($"✅ ViewModel found with {GetTotalFieldCount(viewModel)} total fields");
                
                // Subscribe to view model events
                viewModel.SettingsOperationCompleted += OnSettingsOperationCompleted;
                
                // Debug: Print first few fields to verify data
                DebugPrintSampleFields(viewModel);
            }
            else
            {
                Console.WriteLine("❌ No ViewModel found in DataContext!");
            }
        }
        
        private int GetTotalFieldCount(ComprehensiveModernSettingsViewModel viewModel)
        {
            int count = 0;
            var collections = new[] {
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
            
            foreach (var collection in collections)
            {
                foreach (var group in collection)
                {
                    count += group.Fields.Count;
                }
            }
            return count;
        }
        
        private void DebugPrintSampleFields(ComprehensiveModernSettingsViewModel viewModel)
        {
            Console.WriteLine("\n🔍 Sample field values:");
            
            // Check Company Settings
            if (viewModel.CompanySettings.Count > 0)
            {
                var companyGroup = viewModel.CompanySettings[0];
                Console.WriteLine($"   Company Group: '{companyGroup.Title}' with {companyGroup.Fields.Count} fields");
                foreach (var field in companyGroup.Fields.Take(3))
                {
                    Console.WriteLine($"     - {field.Key}: Default='{field.DefaultValue}', Value='{field.Value}', Type={field.FieldType}");
                }
            }
            
            // Check Hardware Settings
            if (viewModel.HardwareSettings.Count > 0)
            {
                var hardwareGroup = viewModel.HardwareSettings[0];
                Console.WriteLine($"   Hardware Group: '{hardwareGroup.Title}' with {hardwareGroup.Fields.Count} fields");
                foreach (var field in hardwareGroup.Fields.Take(2))
                {
                    Console.WriteLine($"     - {field.Key}: Default='{field.DefaultValue}', Value='{field.Value}', Type={field.FieldType}");
                    if (field.Options != null)
                        Console.WriteLine($"       Options: {field.Options.Count} items");
                }
            }
        }

        private void OnSettingsOperationCompleted(object? sender, string message)
        {
            // Forward the event to the parent
            FormCompleted?.Invoke(this, message);
        }

        public void Dispose()
        {
            // Clean up resources
            if (DataContext is ComprehensiveModernSettingsViewModel viewModel)
            {
                viewModel.SettingsOperationCompleted -= OnSettingsOperationCompleted;
            }
            
            if (DataContext is IDisposable disposableViewModel)
            {
                disposableViewModel.Dispose();
            }
        }
    }
}