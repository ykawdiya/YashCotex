using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using WeighbridgeSoftwareYashCotex.Commands;
using WeighbridgeSoftwareYashCotex.Models;
using WeighbridgeSoftwareYashCotex.Services;

namespace WeighbridgeSoftwareYashCotex.ViewModels
{
    public class ComprehensiveModernSettingsViewModel : INotifyPropertyChanged
    {
        private readonly SettingsService _settingsService;
        private readonly DatabaseService _databaseService;
        private bool _isLoading;

        public event EventHandler<string>? SettingsOperationCompleted;

        // All Settings Collections
        public ObservableCollection<SettingsGroup> CompanySettings { get; }
        public ObservableCollection<SettingsGroup> HardwareSettings { get; }
        public ObservableCollection<SettingsGroup> CameraSettings { get; }
        public ObservableCollection<SettingsGroup> IntegrationSettings { get; }
        public ObservableCollection<SettingsGroup> DataManagementSettings { get; }
        public ObservableCollection<SettingsGroup> SecuritySettings { get; }
        public ObservableCollection<SettingsGroup> WeightRulesSettings { get; }
        public ObservableCollection<SettingsGroup> UserSettings { get; }
        public ObservableCollection<SettingsGroup> SystemSettings { get; }
        public ObservableCollection<SettingsGroup> AdminToolsSettings { get; }

        // Data Management Collections
        public ObservableCollection<Material> CurrentMaterials { get; }
        public ObservableCollection<Address> CurrentAddresses { get; }

        // Selected Items for Data Management
        private Material? _selectedMaterial;
        public Material? SelectedMaterial
        {
            get => _selectedMaterial;
            set
            {
                _selectedMaterial = value;
                OnPropertyChanged();
            }
        }

        private Address? _selectedAddress;
        public Address? SelectedAddress
        {
            get => _selectedAddress;
            set
            {
                _selectedAddress = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand SaveAllCommand { get; set; }
        public ICommand ResetCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        
        // Company Logo Commands
        public ICommand BrowseLogoCommand { get; set; }
        public ICommand RemoveLogoCommand { get; set; }
        
        // Hardware Commands
        public ICommand AddRowCommand { get; set; }
        public ICommand AddPlaceholderCommand { get; set; }
        public ICommand ClearTemplateCommand { get; set; }
        public ICommand PreviewTemplateCommand { get; set; }
        public ICommand AddLedDisplayCommand { get; set; }
        public ICommand TestWeightDisplayCommand { get; set; }
        public ICommand CalibrateWeightCommand { get; set; }
        
        // Camera Commands
        public ICommand TestAllCamerasCommand { get; set; }
        public ICommand StartMonitoringCommand { get; set; }
        public ICommand StopMonitoringCommand { get; set; }
        public ICommand CaptureAllCommand { get; set; }
        public ICommand TestCameraCommand { get; set; }
        public ICommand CaptureCameraCommand { get; set; }
        public ICommand PreviewCameraCommand { get; set; }
        
        // Integration Commands
        public ICommand BrowseKeyFileCommand { get; set; }
        public ICommand TestGoogleSheetsCommand { get; set; }
        public ICommand SyncNowCommand { get; set; }
        public ICommand BrowseBackupLocationCommand { get; set; }
        public ICommand BackupNowCommand { get; set; }
        public ICommand RestoreBackupCommand { get; set; }
        
        // Data Management Commands
        public ICommand AddMaterialCommand { get; set; }
        public ICommand EditMaterialCommand { get; set; }
        public ICommand DeleteMaterialCommand { get; set; }
        public ICommand AddAddressCommand { get; set; }
        public ICommand EditAddressCommand { get; set; }
        public ICommand DeleteAddressCommand { get; set; }
        
        // User Management Commands
        public ICommand AddUserCommand { get; set; }
        public ICommand EditUserCommand { get; set; }
        public ICommand DeleteUserCommand { get; set; }
        public ICommand CreateUserCommand { get; set; }
        public ICommand ClearUserFormCommand { get; set; }
        
        // System Commands
        public ICommand RefreshSystemInfoCommand { get; set; }
        public ICommand RestartAppCommand { get; set; }
        public ICommand SystemDiagnosticsCommand { get; set; }
        public ICommand ClearCacheCommand { get; set; }
        public ICommand ExportLogsCommand { get; set; }
        public ICommand ForceBackupCommand { get; set; }
        public ICommand DetailedHealthCheckCommand { get; set; }
        
        // Admin Tools Commands
        public ICommand OpenWeightManagementCommand { get; set; }
        public ICommand ViewAuditHistoryCommand { get; set; }
        public ICommand ReverseOperationsCommand { get; set; }
        public ICommand IntegrityCheckCommand { get; set; }
        public ICommand CleanupRecordsCommand { get; set; }
        public ICommand ManageUsersCommand { get; set; }
        public ICommand ResetPasswordsCommand { get; set; }
        public ICommand ViewActivityCommand { get; set; }
        public ICommand EmergencyResetCommand { get; set; }
        public ICommand MaintenanceModeCommand { get; set; }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public ComprehensiveModernSettingsViewModel()
        {
            _settingsService = SettingsService.Instance;
            _databaseService = new DatabaseService();
            
            // Initialize Collections
            CompanySettings = new ObservableCollection<SettingsGroup>();
            HardwareSettings = new ObservableCollection<SettingsGroup>();
            CameraSettings = new ObservableCollection<SettingsGroup>();
            IntegrationSettings = new ObservableCollection<SettingsGroup>();
            DataManagementSettings = new ObservableCollection<SettingsGroup>();
            SecuritySettings = new ObservableCollection<SettingsGroup>();
            WeightRulesSettings = new ObservableCollection<SettingsGroup>();
            UserSettings = new ObservableCollection<SettingsGroup>();
            SystemSettings = new ObservableCollection<SettingsGroup>();
            AdminToolsSettings = new ObservableCollection<SettingsGroup>();

            // Initialize Data Management Collections
            CurrentMaterials = new ObservableCollection<Material>();
            CurrentAddresses = new ObservableCollection<Address>();

            // Initialize Commands
            InitializeCommands();
            
            // Initialize Settings
            InitializeAllSettings();
            LoadCurrentValues();
            
            // Load Data Management Data
            LoadDataManagementData();
        }

        private void InitializeCommands()
        {
            // Basic Commands
            SaveAllCommand = new RelayCommand(SaveAll, CanSaveAll);
            ResetCommand = new RelayCommand(ResetAll);
            CancelCommand = new RelayCommand(Cancel);
            
            // Company Commands
            BrowseLogoCommand = new RelayCommand(BrowseLogo);
            RemoveLogoCommand = new RelayCommand(RemoveLogo);
            
            // Hardware Commands
            AddRowCommand = new RelayCommand(AddRow);
            AddPlaceholderCommand = new RelayCommand(AddPlaceholder);
            ClearTemplateCommand = new RelayCommand(ClearTemplate);
            PreviewTemplateCommand = new RelayCommand(PreviewTemplate);
            AddLedDisplayCommand = new RelayCommand(AddLedDisplay);
            TestWeightDisplayCommand = new RelayCommand(TestWeightDisplay);
            CalibrateWeightCommand = new RelayCommand(CalibrateWeight);
            
            // Camera Commands
            TestAllCamerasCommand = new RelayCommand(TestAllCameras);
            StartMonitoringCommand = new RelayCommand(StartMonitoring);
            StopMonitoringCommand = new RelayCommand(StopMonitoring);
            CaptureAllCommand = new RelayCommand(CaptureAll);
            TestCameraCommand = new RelayCommand<string>(TestCamera);
            CaptureCameraCommand = new RelayCommand<string>(CaptureCamera);
            PreviewCameraCommand = new RelayCommand<string>(PreviewCamera);
            
            // Integration Commands
            BrowseKeyFileCommand = new RelayCommand(BrowseKeyFile);
            TestGoogleSheetsCommand = new RelayCommand(TestGoogleSheets);
            SyncNowCommand = new RelayCommand(SyncNow);
            BrowseBackupLocationCommand = new RelayCommand(BrowseBackupLocation);
            BackupNowCommand = new RelayCommand(BackupNow);
            RestoreBackupCommand = new RelayCommand(RestoreBackup);
            
            // Data Management Commands
            AddMaterialCommand = new RelayCommand(AddMaterial);
            EditMaterialCommand = new RelayCommand(EditMaterial);
            DeleteMaterialCommand = new RelayCommand(DeleteMaterial);
            AddAddressCommand = new RelayCommand(AddAddress);
            EditAddressCommand = new RelayCommand(EditAddress);
            DeleteAddressCommand = new RelayCommand(DeleteAddress);
            
            // User Management Commands
            AddUserCommand = new RelayCommand(AddUser);
            EditUserCommand = new RelayCommand(EditUser);
            DeleteUserCommand = new RelayCommand(DeleteUser);
            CreateUserCommand = new RelayCommand(CreateUser);
            ClearUserFormCommand = new RelayCommand(ClearUserForm);
            
            // System Commands
            RefreshSystemInfoCommand = new RelayCommand(RefreshSystemInfo);
            RestartAppCommand = new RelayCommand(RestartApp);
            SystemDiagnosticsCommand = new RelayCommand(SystemDiagnostics);
            ClearCacheCommand = new RelayCommand(ClearCache);
            ExportLogsCommand = new RelayCommand(ExportLogs);
            ForceBackupCommand = new RelayCommand(ForceBackup);
            DetailedHealthCheckCommand = new RelayCommand(DetailedHealthCheck);
            
            // Admin Tools Commands
            OpenWeightManagementCommand = new RelayCommand(OpenWeightManagement);
            ViewAuditHistoryCommand = new RelayCommand(ViewAuditHistory);
            ReverseOperationsCommand = new RelayCommand(ReverseOperations);
            IntegrityCheckCommand = new RelayCommand(IntegrityCheck);
            CleanupRecordsCommand = new RelayCommand(CleanupRecords);
            ManageUsersCommand = new RelayCommand(ManageUsers);
            ResetPasswordsCommand = new RelayCommand(ResetPasswords);
            ViewActivityCommand = new RelayCommand(ViewActivity);
            EmergencyResetCommand = new RelayCommand(EmergencyReset);
            MaintenanceModeCommand = new RelayCommand(MaintenanceMode);
        }

        private void InitializeAllSettings()
        {
            InitializeCompanySettings();
            InitializeHardwareSettings();
            InitializeCameraSettings();
            InitializeIntegrationSettings();
            InitializeDataManagementSettings();
            InitializeSecuritySettings();
            InitializeWeightRulesSettings();
            InitializeUserSettings();
            InitializeSystemSettings();
            InitializeAdminToolsSettings();
        }

        private void InitializeCompanySettings()
        {
            var companyInfo = new SettingsGroup
            {
                Title = "Company Information",
                Description = "Complete company details and branding configuration",
                ColumnCount = 3,
                Fields = new List<SettingsField>
                {
                    new() { Key = "CompanyName", Label = "Company Name", FieldType = FieldType.Text, IsRequired = true, 
                           Placeholder = "YASH COTEX PRIVATE LIMITED", Tooltip = "Official registered company name",
                           DefaultValue = "YASH COTEX PRIVATE LIMITED", Value = "YASH COTEX PRIVATE LIMITED" },
                    new() { Key = "CompanyAddressLine1", Label = "Address Line 1", FieldType = FieldType.Text, IsRequired = true,
                           Placeholder = "Enter primary address", DefaultValue = "", Value = "" },
                    new() { Key = "CompanyAddressLine2", Label = "Address Line 2", FieldType = FieldType.Text,
                           Placeholder = "Enter secondary address", DefaultValue = "", Value = "" },
                    new() { Key = "CompanyPhone", Label = "Phone Number", FieldType = FieldType.Text, IsRequired = true,
                           Placeholder = "Enter contact number", ValidationPattern = @"^[\+]?[0-9\-\(\)\s]+$",
                           DefaultValue = "", Value = "" },
                    new() { Key = "CompanyEmail", Label = "Email Address", FieldType = FieldType.Text,
                           ValidationPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$", ValidationMessage = "Invalid email format",
                           DefaultValue = "", Value = "" },
                    new() { Key = "CompanyGSTIN", Label = "GST Number", FieldType = FieldType.Text,
                           Placeholder = "Enter GST number", Tooltip = "Goods and Services Tax Identification Number",
                           DefaultValue = "", Value = "" },
                    new() { Key = "CompanyLogo", Label = "Company Logo", FieldType = FieldType.File,
                           FileFilter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                           Tooltip = "Supported formats: PNG, JPG, JPEG", DefaultValue = "", Value = "" }
                }
            };

            CompanySettings.Add(companyInfo);
        }

        private void InitializeHardwareSettings()
        {
            // Weighbridge Settings
            var weighbridgeGroup = new SettingsGroup
            {
                Title = "Weighbridge Configuration",
                Description = "Serial communication and weighbridge hardware settings",
                ColumnCount = 4,
                Fields = new List<SettingsField>
                {
                    new() { Key = "WeighbridgeComPort", Label = "Scale COM Port", FieldType = FieldType.Dropdown, IsRequired = true,
                           Options = GetComPortOptions(), Tooltip = "Serial port for weighbridge communication",
                           DefaultValue = "COM1", Value = "COM1" },
                    new() { Key = "WeighbridgeBaudRate", Label = "Baud Rate", FieldType = FieldType.Dropdown, IsRequired = true,
                           Options = GetBaudRateOptions(), DefaultValue = "9600", Value = "9600" },
                    new() { Key = "WeighbridgeDataBits", Label = "Data Bits", FieldType = FieldType.Dropdown, IsRequired = true,
                           Options = GetDataBitsOptions(), DefaultValue = "8", Value = "8" },
                    new() { Key = "WeighbridgeParity", Label = "Parity", FieldType = FieldType.Dropdown, IsRequired = true,
                           Options = GetParityOptions(), DefaultValue = "None", Value = "None" },
                    new() { Key = "WeighbridgeStopBits", Label = "Stop Bits", FieldType = FieldType.Dropdown, IsRequired = true,
                           Options = GetStopBitsOptions(), DefaultValue = "1", Value = "1" },
                    new() { Key = "WeighbridgeTimeout", Label = "Timeout (ms)", FieldType = FieldType.Number, IsRequired = true,
                           DefaultValue = "5000", Value = "5000", Tooltip = "Communication timeout in milliseconds" },
                    new() { Key = "WeighbridgeCapacity", Label = "Maximum Capacity (KG)", FieldType = FieldType.Number, IsRequired = true,
                           DefaultValue = "100000", Value = "100000", Tooltip = "Maximum weighing capacity" }
                }
            };

            // Dot Matrix Printer Settings
            var printerGroup = new SettingsGroup
            {
                Title = "Dot Matrix Printer Configuration",
                Description = "Configure dot matrix printer for RST printing",
                ColumnCount = 3,
                Fields = new List<SettingsField>
                {
                    new() { Key = "PrinterName", Label = "Printer Name/Model", FieldType = FieldType.Dropdown,
                           Options = GetPrinterOptions(), Tooltip = "Select installed dot matrix printer",
                           DefaultValue = "Epson LQ-310", Value = "Epson LQ-310" },
                    new() { Key = "PaperSize", Label = "Paper Size", FieldType = FieldType.Dropdown,
                           Options = GetPaperSizeOptions(), DefaultValue = "Continuous Form (9.5\" x 11\")", Value = "Continuous Form (9.5\" x 11\")" },
                    new() { Key = "CharactersPerLine", Label = "Characters Per Line", FieldType = FieldType.Dropdown,
                           Options = GetCharactersPerLineOptions(), DefaultValue = "80", Value = "80" },
                    new() { Key = "PrintSpeed", Label = "Print Speed", FieldType = FieldType.Dropdown,
                           Options = GetPrintSpeedOptions(), DefaultValue = "Draft", Value = "Draft" },
                    new() { Key = "FontType", Label = "Font Type", FieldType = FieldType.Dropdown,
                           Options = GetFontTypeOptions(), DefaultValue = "Draft", Value = "Draft" },
                    new() { Key = "LineSpacing", Label = "Line Spacing", FieldType = FieldType.Dropdown,
                           Options = GetLineSpacingOptions(), DefaultValue = "6 LPI", Value = "6 LPI" },
                    new() { Key = "PaperFeed", Label = "Paper Feed", FieldType = FieldType.Dropdown,
                           Options = GetPaperFeedOptions(), DefaultValue = "Tractor Feed", Value = "Tractor Feed" },
                    new() { Key = "AutoPrintAfterWeighment", Label = "Auto-print after weighment", FieldType = FieldType.Checkbox,
                           CheckboxText = "Enable automatic printing", DefaultValue = true, Value = true },
                    new() { Key = "FormFeedAfterPrint", Label = "Form feed after print", FieldType = FieldType.Checkbox,
                           CheckboxText = "Enable form feed", DefaultValue = true, Value = true }
                }
            };

            // Live Weight Display Settings
            var liveWeightGroup = new SettingsGroup
            {
                Title = "Live Weight Display",
                Description = "Real-time weight display configuration and calibration",
                ColumnCount = 2,
                Fields = new List<SettingsField>
                {
                    new() { Key = "EnableLiveWeightDisplay", Label = "Enable Live Weight Display", FieldType = FieldType.Checkbox,
                           CheckboxText = "Show real-time weight", DefaultValue = true, Value = true },
                    new() { Key = "WeightRefreshRate", Label = "Refresh Rate", FieldType = FieldType.Dropdown,
                           Options = GetRefreshRateOptions(), DefaultValue = "500ms", Value = "500ms" },
                    new() { Key = "WeightFormat", Label = "Weight Format", FieldType = FieldType.Dropdown,
                           Options = GetWeightFormatOptions(), DefaultValue = "0.00 kg", Value = "0.00 kg" }
                }
            };

            HardwareSettings.Add(weighbridgeGroup);
            HardwareSettings.Add(printerGroup);
            HardwareSettings.Add(liveWeightGroup);
        }

        private void InitializeCameraSettings()
        {
            // Camera Controls
            var cameraControlsGroup = new SettingsGroup
            {
                Title = "Camera Controls",
                Description = "Global camera control and monitoring settings",
                ColumnCount = 1,
                Fields = new List<SettingsField>
                {
                    new() { Key = "EnableCameraPreview", Label = "Enable Live Preview", FieldType = FieldType.Checkbox,
                           CheckboxText = "Show live camera feeds in interface", DefaultValue = true, Value = true }
                }
            };

            // Individual Camera Settings (4 cameras)
            for (int i = 1; i <= 4; i++)
            {
                var cameraGroup = new SettingsGroup
                {
                    Title = $"Camera {i} Configuration",
                    Description = $"Settings for camera {i} - {GetCameraLocation(i)}",
                    ColumnCount = 3,
                    Fields = new List<SettingsField>
                    {
                        new() { Key = $"Camera{i}Enable", Label = "Enable Camera", FieldType = FieldType.Checkbox,
                               CheckboxText = $"Enable Camera {i}", DefaultValue = false, Value = false },
                        new() { Key = $"Camera{i}Name", Label = "Camera Name", FieldType = FieldType.Text,
                               Placeholder = GetCameraLocation(i), DefaultValue = GetCameraLocation(i), Value = GetCameraLocation(i) },
                        new() { Key = $"Camera{i}Protocol", Label = "Protocol", FieldType = FieldType.Dropdown, IsRequired = true,
                               Options = GetProtocolOptions(), DefaultValue = "HTTP", Value = "HTTP" },
                        new() { Key = $"Camera{i}IpAddress", Label = "IP Address", FieldType = FieldType.Text, IsRequired = true,
                               Placeholder = "192.168.1.100", ValidationPattern = @"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$",
                               DefaultValue = $"192.168.1.{100 + i}", Value = $"192.168.1.{100 + i}" },
                        new() { Key = $"Camera{i}Port", Label = "Port", FieldType = FieldType.Number, IsRequired = true,
                               DefaultValue = "80", Value = "80" },
                        new() { Key = $"Camera{i}StreamPath", Label = "Stream Path", FieldType = FieldType.Text,
                               Placeholder = "/video.mjpg", DefaultValue = "/video.mjpg", Value = "/video.mjpg" },
                        new() { Key = $"Camera{i}Username", Label = "Username", FieldType = FieldType.Text,
                               Placeholder = "admin", DefaultValue = "admin", Value = "admin" },
                        new() { Key = $"Camera{i}Password", Label = "Password", FieldType = FieldType.Password,
                               Placeholder = "Enter camera password", DefaultValue = "", Value = "" }
                    }
                };
                CameraSettings.Add(cameraGroup);
            }

            CameraSettings.Insert(0, cameraControlsGroup);
        }

        #region Command Implementations

        private void SaveAll()
        {
            try
            {
                IsLoading = true;

                // Validate all fields first
                var allGroups = GetAllSettingsGroups();

                foreach (var group in allGroups)
                {
                    if (!group.ValidateAll())
                    {
                        MessageBox.Show($"Validation failed in {group.Title}. Please check all required fields.", 
                                      "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Save all settings
                SaveAllSettings();
                
                MessageBox.Show("All settings saved successfully!", "Settings Saved", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Notify that settings were saved successfully
                SettingsOperationCompleted?.Invoke(this, "Settings saved successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Save Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SaveAllSettings()
        {
            // Save Company Settings
            SaveCompanySettings();
            
            // Save Hardware Settings
            SaveHardwareSettings();
            
            // Save Camera Settings
            SaveCameraSettings();
            
            // Save Integration Settings
            SaveIntegrationSettings();
            
            // Save other settings...
            _settingsService.SaveSettings();
        }

        private void SaveCompanySettings()
        {
            var companyGroup = CompanySettings.FirstOrDefault();
            if (companyGroup != null)
            {
                var values = companyGroup.GetValues();
                _settingsService.CompanyName = values.GetValueOrDefault("CompanyName")?.ToString() ?? "";
                _settingsService.CompanyAddressLine1 = values.GetValueOrDefault("CompanyAddressLine1")?.ToString() ?? "";
                _settingsService.CompanyAddressLine2 = values.GetValueOrDefault("CompanyAddressLine2")?.ToString() ?? "";
                _settingsService.CompanyPhone = values.GetValueOrDefault("CompanyPhone")?.ToString() ?? "";
                _settingsService.CompanyEmail = values.GetValueOrDefault("CompanyEmail")?.ToString() ?? "";
                _settingsService.CompanyGSTIN = values.GetValueOrDefault("CompanyGSTIN")?.ToString() ?? "";
                _settingsService.CompanyLogo = values.GetValueOrDefault("CompanyLogo")?.ToString() ?? "";
            }
        }

        private void SaveHardwareSettings()
        {
            foreach (var group in HardwareSettings)
            {
                var values = group.GetValues();
                
                if (group.Title.Contains("Weighbridge"))
                {
                    _settingsService.WeighbridgeComPort = values.GetValueOrDefault("WeighbridgeComPort")?.ToString();
                    
                    if (int.TryParse(values.GetValueOrDefault("WeighbridgeBaudRate")?.ToString(), out var baudRate))
                        _settingsService.WeighbridgeBaudRate = baudRate;
                    
                    if (int.TryParse(values.GetValueOrDefault("WeighbridgeDataBits")?.ToString(), out var dataBits))
                        _settingsService.WeighbridgeDataBits = dataBits;
                    
                    if (double.TryParse(values.GetValueOrDefault("WeighbridgeCapacity")?.ToString(), out var capacity))
                        _settingsService.WeighbridgeCapacity = capacity;
                    
                    if (int.TryParse(values.GetValueOrDefault("WeighbridgeTimeout")?.ToString(), out var timeout))
                        _settingsService.WeighbridgeTimeout = timeout;
                }
            }
        }

        private void SaveCameraSettings()
        {
            // Implementation for saving camera settings
            // This would involve updating the camera configurations in the settings service
        }

        private void SaveIntegrationSettings()
        {
            // Implementation for saving integration settings
        }

        private bool CanSaveAll()
        {
            return !IsLoading;
        }

        private void ResetAll()
        {
            var result = MessageBox.Show("Are you sure you want to reset all settings to default values?", 
                                       "Reset Settings", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                var allGroups = GetAllSettingsGroups();

                foreach (var group in allGroups)
                {
                    group.ResetAll();
                }
            }
        }

        private void Cancel()
        {
            LoadCurrentValues(); // Reload original values
            SettingsOperationCompleted?.Invoke(this, "Settings cancelled");
        }

        // Company Commands
        private void BrowseLogo()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                Title = "Select Company Logo"
            };

            if (dialog.ShowDialog() == true)
            {
                var logoField = CompanySettings.FirstOrDefault()?.Fields.FirstOrDefault(f => f.Key == "CompanyLogo");
                if (logoField != null)
                {
                    logoField.Value = dialog.FileName;
                }
            }
        }

        private void RemoveLogo()
        {
            var logoField = CompanySettings.FirstOrDefault()?.Fields.FirstOrDefault(f => f.Key == "CompanyLogo");
            if (logoField != null)
            {
                logoField.Value = "";
            }
        }

        // Hardware Commands (placeholder implementations)
        private void AddRow() { /* RST Template row addition */ }
        private void AddPlaceholder() { /* RST Template placeholder addition */ }
        private void ClearTemplate() { /* Clear RST template */ }
        private void PreviewTemplate() { /* Preview RST template */ }
        private void AddLedDisplay() { /* Add LED display configuration */ }
        private void TestWeightDisplay() { /* Test weight display */ }
        private void CalibrateWeight() { /* Calibrate weighbridge */ }

        // Camera Commands (placeholder implementations)
        private void TestAllCameras() { /* Test all cameras */ }
        private void StartMonitoring() { /* Start camera monitoring */ }
        private void StopMonitoring() { /* Stop camera monitoring */ }
        private void CaptureAll() { /* Capture from all cameras */ }
        private void TestCamera(string cameraId) { /* Test specific camera */ }
        private void CaptureCamera(string cameraId) { /* Capture from specific camera */ }
        private void PreviewCamera(string cameraId) { /* Preview specific camera */ }

        // Integration Commands (placeholder implementations)
        private void BrowseKeyFile() { /* Browse for Google Sheets key file */ }
        private void TestGoogleSheets() { /* Test Google Sheets connection */ }
        private void SyncNow() { /* Sync with Google Sheets */ }
        private void BrowseBackupLocation() { /* Browse backup location */ }
        private void BackupNow() { /* Perform backup */ }
        private void RestoreBackup() { /* Restore from backup */ }

        // Data Management Commands
        private void AddMaterial()
        {
            try
            {
                var materialName = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter material name:", 
                    "Add New Material", 
                    "");

                if (string.IsNullOrWhiteSpace(materialName))
                    return;

                if (_databaseService.CreateMaterial(materialName))
                {
                    LoadDataManagementData(); // Refresh the collections
                    _settingsService.OnMaterialsChanged(); // Trigger refresh event for Entry/Exit forms
                    MessageBox.Show($"Material '{materialName}' added successfully!", 
                                  "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to add material '{materialName}'. It may already exist.", 
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding material: {ex.Message}", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditMaterial()
        {
            try
            {
                if (SelectedMaterial != null)
                {
                    var selectedMaterial = SelectedMaterial;
                    var newName = Microsoft.VisualBasic.Interaction.InputBox(
                        "Edit material name:", 
                        "Edit Material", 
                        selectedMaterial.Name);

                    if (string.IsNullOrWhiteSpace(newName) || newName == selectedMaterial.Name)
                        return;

                    if (_databaseService.UpdateMaterial(selectedMaterial.Id, newName))
                    {
                        LoadDataManagementData(); // Refresh the collections
                        _settingsService.OnMaterialsChanged(); // Trigger refresh event for Entry/Exit forms
                        MessageBox.Show($"Material updated successfully!", 
                                      "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to update material. Name may already exist.", 
                                      "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a material to edit.", 
                                  "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error editing material: {ex.Message}", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteMaterial()
        {
            try
            {
                if (SelectedMaterial != null)
                {
                    var selectedMaterial = SelectedMaterial;
                    var result = MessageBox.Show(
                        $"Are you sure you want to delete the material '{selectedMaterial.Name}'?\n\nThis will mark it as inactive and it won't appear in entry forms.", 
                        "Confirm Delete", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (_databaseService.DeleteMaterial(selectedMaterial.Id))
                        {
                            LoadDataManagementData(); // Refresh the collections
                            _settingsService.OnMaterialsChanged(); // Trigger refresh event for Entry/Exit forms
                            MessageBox.Show($"Material '{selectedMaterial.Name}' deleted successfully!", 
                                          "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Failed to delete material.", 
                                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Please select a material to delete.", 
                                  "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting material: {ex.Message}", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddAddress()
        {
            try
            {
                var addressName = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter address name:", 
                    "Add New Address", 
                    "");

                if (string.IsNullOrWhiteSpace(addressName))
                    return;

                if (_databaseService.CreateAddress(addressName))
                {
                    LoadDataManagementData(); // Refresh the collections
                    _settingsService.OnAddressesChanged(); // Trigger refresh event for Entry/Exit forms
                    MessageBox.Show($"Address '{addressName}' added successfully!", 
                                  "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to add address '{addressName}'. It may already exist.", 
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding address: {ex.Message}", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditAddress()
        {
            try
            {
                if (SelectedAddress != null)
                {
                    var selectedAddress = SelectedAddress;
                    var newName = Microsoft.VisualBasic.Interaction.InputBox(
                        "Edit address name:", 
                        "Edit Address", 
                        selectedAddress.Name);

                    if (string.IsNullOrWhiteSpace(newName) || newName == selectedAddress.Name)
                        return;

                    if (_databaseService.UpdateAddress(selectedAddress.Id, newName))
                    {
                        LoadDataManagementData(); // Refresh the collections
                        _settingsService.OnAddressesChanged(); // Trigger refresh event for Entry/Exit forms
                        MessageBox.Show($"Address updated successfully!", 
                                      "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to update address. Name may already exist.", 
                                      "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Please select an address to edit.", 
                                  "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error editing address: {ex.Message}", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteAddress()
        {
            try
            {
                if (SelectedAddress != null)
                {
                    var selectedAddress = SelectedAddress;
                    var result = MessageBox.Show(
                        $"Are you sure you want to delete the address '{selectedAddress.Name}'?\n\nThis will mark it as inactive and it won't appear in entry forms.", 
                        "Confirm Delete", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (_databaseService.DeleteAddress(selectedAddress.Id))
                        {
                            LoadDataManagementData(); // Refresh the collections
                            _settingsService.OnAddressesChanged(); // Trigger refresh event for Entry/Exit forms
                            MessageBox.Show($"Address '{selectedAddress.Name}' deleted successfully!", 
                                          "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Failed to delete address.", 
                                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Please select an address to delete.", 
                                  "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting address: {ex.Message}", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // User Management Commands (placeholder implementations)
        private void AddUser() { /* Add user */ }
        private void EditUser() { /* Edit user */ }
        private void DeleteUser() { /* Delete user */ }
        private void CreateUser() { /* Create new user */ }
        private void ClearUserForm() { /* Clear user form */ }

        // System Commands (placeholder implementations)
        private void RefreshSystemInfo() { /* Refresh system information */ }
        private void RestartApp() { /* Restart application */ }
        private void SystemDiagnostics() { /* Run system diagnostics */ }
        private void ClearCache() { /* Clear cache */ }
        private void ExportLogs() { /* Export system logs */ }
        private void ForceBackup() { /* Force database backup */ }
        private void DetailedHealthCheck() { /* Detailed health check */ }

        // Admin Tools Commands (placeholder implementations)
        private void OpenWeightManagement() { /* Open weight management */ }
        private void ViewAuditHistory() { /* View audit history */ }
        private void ReverseOperations() { /* Reverse operations */ }
        private void IntegrityCheck() { /* Database integrity check */ }
        private void CleanupRecords() { /* Cleanup old records */ }
        private void ManageUsers() { /* Manage user accounts */ }
        private void ResetPasswords() { /* Reset user passwords */ }
        private void ViewActivity() { /* View user activity */ }
        private void EmergencyReset() { /* Emergency system reset */ }
        private void MaintenanceMode() { /* Enable maintenance mode */ }

        #endregion

        #region Option Providers

        private List<SettingsOption> GetComPortOptions()
        {
            var ports = new[] { "COM1", "COM2", "COM3", "COM4" };
            return ports.Select(p => new SettingsOption { Text = p, Value = p }).ToList();
        }

        private List<SettingsOption> GetBaudRateOptions()
        {
            var rates = new[] { "9600", "19200", "38400", "57600" };
            return rates.Select(r => new SettingsOption { Text = r, Value = r }).ToList();
        }

        private List<SettingsOption> GetDataBitsOptions()
        {
            var bits = new[] { "7", "8" };
            return bits.Select(b => new SettingsOption { Text = b, Value = b }).ToList();
        }

        private List<SettingsOption> GetParityOptions()
        {
            return new List<SettingsOption>
            {
                new() { Text = "None", Value = "None" },
                new() { Text = "Even", Value = "Even" },
                new() { Text = "Odd", Value = "Odd" }
            };
        }

        private List<SettingsOption> GetStopBitsOptions()
        {
            return new List<SettingsOption>
            {
                new() { Text = "1", Value = "1" },
                new() { Text = "2", Value = "2" }
            };
        }

        private List<SettingsOption> GetPrinterOptions()
        {
            // In real implementation, this would query installed printers
            return new List<SettingsOption>
            {
                new() { Text = "Epson LQ-310", Value = "Epson LQ-310" },
                new() { Text = "TVS MSP 240 Star", Value = "TVS MSP 240 Star" },
                new() { Text = "Generic Dot Matrix", Value = "Generic" }
            };
        }

        private List<SettingsOption> GetPaperSizeOptions()
        {
            return new List<SettingsOption>
            {
                new() { Text = "Continuous Form (9.5\" x 11\")", Value = "9.5x11" },
                new() { Text = "Continuous Form (8.5\" x 11\")", Value = "8.5x11" },
                new() { Text = "Continuous Form (8.5\" x 14\")", Value = "8.5x14" }
            };
        }

        private List<SettingsOption> GetCharactersPerLineOptions()
        {
            return new List<SettingsOption>
            {
                new() { Text = "80 characters", Value = "80" },
                new() { Text = "96 characters", Value = "96" },
                new() { Text = "132 characters", Value = "132" }
            };
        }

        private List<SettingsOption> GetPrintSpeedOptions()
        {
            return new List<SettingsOption>
            {
                new() { Text = "Draft (Fast)", Value = "Draft" },
                new() { Text = "Near Letter Quality", Value = "NLQ" },
                new() { Text = "Letter Quality", Value = "LQ" }
            };
        }

        private List<SettingsOption> GetFontTypeOptions()
        {
            return new List<SettingsOption>
            {
                new() { Text = "Draft (9-pin)", Value = "Draft" },
                new() { Text = "Near Letter Quality (24-pin)", Value = "NLQ" },
                new() { Text = "Condensed", Value = "Condensed" },
                new() { Text = "Elite", Value = "Elite" },
                new() { Text = "Pica", Value = "Pica" }
            };
        }

        private List<SettingsOption> GetLineSpacingOptions()
        {
            return new List<SettingsOption>
            {
                new() { Text = "6 LPI (Lines Per Inch)", Value = "6" },
                new() { Text = "8 LPI (Lines Per Inch)", Value = "8" },
                new() { Text = "Single Spacing", Value = "Single" },
                new() { Text = "Double Spacing", Value = "Double" }
            };
        }

        private List<SettingsOption> GetPaperFeedOptions()
        {
            return new List<SettingsOption>
            {
                new() { Text = "Tractor Feed (Continuous)", Value = "Tractor" },
                new() { Text = "Friction Feed (Single Sheet)", Value = "Friction" },
                new() { Text = "Auto Feed", Value = "Auto" }
            };
        }

        private List<SettingsOption> GetRefreshRateOptions()
        {
            return new List<SettingsOption>
            {
                new() { Text = "100ms (Very Fast)", Value = "100" },
                new() { Text = "250ms (Fast)", Value = "250" },
                new() { Text = "500ms (Normal)", Value = "500" },
                new() { Text = "1000ms (Slow)", Value = "1000" }
            };
        }

        private List<SettingsOption> GetWeightFormatOptions()
        {
            return new List<SettingsOption>
            {
                new() { Text = "0.00 kg", Value = "0.00 kg" },
                new() { Text = "0.000 kg", Value = "0.000 kg" },
                new() { Text = "0 kg", Value = "0 kg" }
            };
        }

        private List<SettingsOption> GetProtocolOptions()
        {
            return new List<SettingsOption>
            {
                new() { Text = "HTTP", Value = "HTTP" },
                new() { Text = "HTTPS", Value = "HTTPS" },
                new() { Text = "RTSP", Value = "RTSP" },
                new() { Text = "TCP", Value = "TCP" }
            };
        }

        private string GetCameraLocation(int cameraNumber)
        {
            return cameraNumber switch
            {
                1 => "Entry Camera",
                2 => "Exit Camera", 
                3 => "Left Side Camera",
                4 => "Right Side Camera",
                _ => $"Camera {cameraNumber}"
            };
        }

        #endregion

        #region Helper Methods

        private SettingsField CreateField(string key, string label, FieldType fieldType, object? defaultValue = null, 
            bool isRequired = false, string? placeholder = null, string? tooltip = null, string? checkboxText = null,
            List<SettingsOption>? options = null, string? validationPattern = null, string? validationMessage = null,
            string? fileFilter = null, bool isEnabled = true)
        {
            return new SettingsField
            {
                Key = key,
                Label = label,
                FieldType = fieldType,
                DefaultValue = defaultValue,
                Value = defaultValue, // Set both DefaultValue and Value
                IsRequired = isRequired,
                Placeholder = placeholder,
                Tooltip = tooltip,
                CheckboxText = checkboxText,
                Options = options,
                ValidationPattern = validationPattern,
                ValidationMessage = validationMessage,
                FileFilter = fileFilter,
                IsEnabled = isEnabled
            };
        }

        private IEnumerable<SettingsGroup> GetAllSettingsGroups()
        {
            return CompanySettings
                .Concat(HardwareSettings)
                .Concat(CameraSettings)
                .Concat(IntegrationSettings)
                .Concat(DataManagementSettings)
                .Concat(SecuritySettings)
                .Concat(WeightRulesSettings)
                .Concat(UserSettings)
                .Concat(SystemSettings)
                .Concat(AdminToolsSettings);
        }

        private void LoadCurrentValues()
        {
            // Load current values from settings service
            LoadCompanyValues();
            LoadHardwareValues();
            LoadCameraValues();
            // Load other values...
        }

        private void LoadDataManagementData()
        {
            try
            {
                // Load current materials
                var materials = _databaseService.GetActiveMaterials();
                CurrentMaterials.Clear();
                foreach (var material in materials)
                {
                    CurrentMaterials.Add(material);
                }

                // Load current addresses
                var addresses = _databaseService.GetActiveAddresses();
                CurrentAddresses.Clear();
                foreach (var address in addresses)
                {
                    CurrentAddresses.Add(address);
                }

                // Update UI fields with current data
                UpdateDataManagementUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data management data: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateDataManagementUI()
        {
            try
            {
                // Update materials list display
                var materialsGroup = DataManagementSettings.FirstOrDefault(g => g.Title.Contains("Materials"));
                if (materialsGroup != null)
                {
                    var materialsListField = materialsGroup.Fields.FirstOrDefault(f => f.Key == "CurrentMaterialsList");
                    if (materialsListField != null)
                    {
                        var materialNames = CurrentMaterials.Select(m => m.Name).ToList();
                        materialsListField.Value = materialNames.Count > 0 
                            ? string.Join(", ", materialNames)
                            : "No materials found";
                    }
                }

                // Update addresses list display
                var addressesGroup = DataManagementSettings.FirstOrDefault(g => g.Title.Contains("Addresses"));
                if (addressesGroup != null)
                {
                    var addressesListField = addressesGroup.Fields.FirstOrDefault(f => f.Key == "CurrentAddressesList");
                    if (addressesListField != null)
                    {
                        var addressNames = CurrentAddresses.Select(a => a.Name).ToList();
                        addressesListField.Value = addressNames.Count > 0 
                            ? string.Join(", ", addressNames)
                            : "No addresses found";
                    }
                }

                // Update summary counts
                var summaryGroup = DataManagementSettings.FirstOrDefault(g => g.Title.Contains("Summary"));
                if (summaryGroup != null)
                {
                    var materialCountField = summaryGroup.Fields.FirstOrDefault(f => f.Key == "MaterialCount");
                    var addressCountField = summaryGroup.Fields.FirstOrDefault(f => f.Key == "AddressCount");
                    var statusField = summaryGroup.Fields.FirstOrDefault(f => f.Key == "DataStatus");

                    if (materialCountField != null)
                        materialCountField.Value = CurrentMaterials.Count.ToString();
                    
                    if (addressCountField != null)
                        addressCountField.Value = CurrentAddresses.Count.ToString();
                    
                    if (statusField != null)
                        statusField.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating data management UI: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCompanyValues()
        {
            var companyGroup = CompanySettings.FirstOrDefault();
            if (companyGroup != null)
            {
                // Only set values that are not null/empty from settings service
                // This preserves our default values
                var values = new Dictionary<string, object?>();
                
                if (!string.IsNullOrEmpty(_settingsService.CompanyName))
                    values["CompanyName"] = _settingsService.CompanyName;
                if (!string.IsNullOrEmpty(_settingsService.CompanyAddressLine1))
                    values["CompanyAddressLine1"] = _settingsService.CompanyAddressLine1;
                if (!string.IsNullOrEmpty(_settingsService.CompanyAddressLine2))
                    values["CompanyAddressLine2"] = _settingsService.CompanyAddressLine2;
                if (!string.IsNullOrEmpty(_settingsService.CompanyPhone))
                    values["CompanyPhone"] = _settingsService.CompanyPhone;
                if (!string.IsNullOrEmpty(_settingsService.CompanyEmail))
                    values["CompanyEmail"] = _settingsService.CompanyEmail;
                if (!string.IsNullOrEmpty(_settingsService.CompanyGSTIN))
                    values["CompanyGSTIN"] = _settingsService.CompanyGSTIN;
                if (!string.IsNullOrEmpty(_settingsService.CompanyLogo))
                    values["CompanyLogo"] = _settingsService.CompanyLogo;
                
                // Only call SetValues if we have values to set
                if (values.Any())
                    companyGroup.SetValues(values);
            }
        }

        private void LoadHardwareValues()
        {
            // Load hardware values from settings service - only if they exist
            // This preserves our default values for demo purposes
            foreach (var group in HardwareSettings)
            {
                if (group.Title.Contains("Weighbridge") && !string.IsNullOrEmpty(_settingsService.WeighbridgeComPort))
                {
                    var values = new Dictionary<string, object?>();
                    if (!string.IsNullOrEmpty(_settingsService.WeighbridgeComPort))
                        values["WeighbridgeComPort"] = _settingsService.WeighbridgeComPort;
                    if (_settingsService.WeighbridgeBaudRate.HasValue)
                        values["WeighbridgeBaudRate"] = _settingsService.WeighbridgeBaudRate.ToString();
                    
                    if (values.Any())
                        group.SetValues(values);
                }
            }
        }

        private void LoadCameraValues()
        {
            // Load camera values from settings service - preserve defaults for demo
            // In a real application, this would load actual saved camera settings
        }

        private void InitializeIntegrationSettings()
        {
            // Google Sheets Integration
            var googleSheetsGroup = new SettingsGroup
            {
                Title = "Google Sheets Integration",
                Description = "Configure real-time synchronization with Google Sheets",
                ColumnCount = 2,
                Fields = new List<SettingsField>
                {
                    new() { Key = "EnableGoogleSheets", Label = "Enable Google Sheets Sync", FieldType = FieldType.Checkbox,
                           CheckboxText = "Enable real-time sync", DefaultValue = false, Value = false },
                    new() { Key = "ServiceAccountKeyFile", Label = "Service Account Key File", FieldType = FieldType.File,
                           FileFilter = "JSON Files (*.json)|*.json", Tooltip = "Google Cloud service account key file",
                           DefaultValue = "", Value = "" },
                    new() { Key = "SpreadsheetId", Label = "Spreadsheet ID", FieldType = FieldType.Text,
                           Placeholder = "Google Sheets document ID", DefaultValue = "", Value = "" },
                    new() { Key = "WorksheetName", Label = "Worksheet Name", FieldType = FieldType.Text,
                           DefaultValue = "WeighmentData", Value = "WeighmentData", Placeholder = "Sheet name for data" }
                }
            };

            // Database Backup Settings
            var backupGroup = new SettingsGroup
            {
                Title = "Database Backup Configuration",
                Description = "Automated backup settings and restore options",
                ColumnCount = 2,
                Fields = new List<SettingsField>
                {
                    new() { Key = "EnableAutomaticBackup", Label = "Enable Automatic Backup", FieldType = FieldType.Checkbox,
                           CheckboxText = "Schedule automatic backups", DefaultValue = true, Value = true },
                    new() { Key = "BackupLocation", Label = "Backup Location", FieldType = FieldType.File,
                           Tooltip = "Directory to store backup files", DefaultValue = "", Value = "" },
                    new() { Key = "BackupFrequency", Label = "Backup Frequency", FieldType = FieldType.Dropdown,
                           Options = GetBackupFrequencyOptions(), DefaultValue = "Daily", Value = "Daily" }
                }
            };

            IntegrationSettings.Add(googleSheetsGroup);
            IntegrationSettings.Add(backupGroup);
        }

        private void InitializeDataManagementSettings()
        {
            // Materials Management
            var materialsGroup = new SettingsGroup
            {
                Title = "🏗️ Materials Management",
                Description = "Manage material types used in weighment records. Add, edit, or remove materials from the system.",
                ColumnCount = 2,
                Fields = new List<SettingsField>
                {
                    new() { 
                        Key = "CurrentMaterialsList", 
                        Label = "Current Materials", 
                        FieldType = FieldType.Text,
                        DefaultValue = "Loading materials...", 
                        Value = "Loading materials...",
                        IsEnabled = false,
                        Description = "Select a material from the list below to edit or delete"
                    },
                    new() { 
                        Key = "MaterialActions", 
                        Label = "Material Actions", 
                        FieldType = FieldType.Text,
                        DefaultValue = "Use buttons below to manage materials", 
                        Value = "Use buttons below to manage materials",
                        IsEnabled = false,
                        Description = "Add new materials or manage existing ones"
                    },
                    new() { 
                        Key = "AddMaterialBtn", 
                        Label = "➕ Add Material", 
                        FieldType = FieldType.Text,
                        DefaultValue = "Click to add new material", 
                        Value = "Click to add new material",
                        IsEnabled = true,
                        Description = "Add a new material type to the system"
                    },
                    new() { 
                        Key = "EditMaterialBtn", 
                        Label = "✏️ Edit Material", 
                        FieldType = FieldType.Text,
                        DefaultValue = "Select a material first", 
                        Value = "Select a material first",
                        IsEnabled = false,
                        Description = "Edit the selected material name"
                    },
                    new() { 
                        Key = "DeleteMaterialBtn", 
                        Label = "🗑️ Delete Material", 
                        FieldType = FieldType.Text,
                        DefaultValue = "Select a material first", 
                        Value = "Select a material first",
                        IsEnabled = false,
                        Description = "Remove the selected material (will be marked as inactive)"
                    }
                }
            };

            // Addresses Management
            var addressesGroup = new SettingsGroup
            {
                Title = "📍 Addresses Management",
                Description = "Manage customer and supplier addresses. Add, edit, or remove addresses from the system.",
                ColumnCount = 2,
                Fields = new List<SettingsField>
                {
                    new() { 
                        Key = "CurrentAddressesList", 
                        Label = "Current Addresses", 
                        FieldType = FieldType.Text,
                        DefaultValue = "Loading addresses...", 
                        Value = "Loading addresses...",
                        IsEnabled = false,
                        Description = "Select an address from the list below to edit or delete"
                    },
                    new() { 
                        Key = "AddressActions", 
                        Label = "Address Actions", 
                        FieldType = FieldType.Text,
                        DefaultValue = "Use buttons below to manage addresses", 
                        Value = "Use buttons below to manage addresses",
                        IsEnabled = false,
                        Description = "Add new addresses or manage existing ones"
                    },
                    new() { 
                        Key = "AddAddressBtn", 
                        Label = "➕ Add Address", 
                        FieldType = FieldType.Text,
                        DefaultValue = "Click to add new address", 
                        Value = "Click to add new address",
                        IsEnabled = true,
                        Description = "Add a new address to the system"
                    },
                    new() { 
                        Key = "EditAddressBtn", 
                        Label = "✏️ Edit Address", 
                        FieldType = FieldType.Text,
                        DefaultValue = "Select an address first", 
                        Value = "Select an address first",
                        IsEnabled = false,
                        Description = "Edit the selected address"
                    },
                    new() { 
                        Key = "DeleteAddressBtn", 
                        Label = "🗑️ Delete Address", 
                        FieldType = FieldType.Text,
                        DefaultValue = "Select an address first", 
                        Value = "Select an address first",
                        IsEnabled = false,
                        Description = "Remove the selected address (will be marked as inactive)"
                    }
                }
            };

            // Data Summary
            var summaryGroup = new SettingsGroup
            {
                Title = "📊 Data Summary",
                Description = "Overview of current data in the system",
                ColumnCount = 2,
                Fields = new List<SettingsField>
                {
                    new() { 
                        Key = "MaterialCount", 
                        Label = "Total Active Materials", 
                        FieldType = FieldType.Text,
                        DefaultValue = "0", 
                        Value = "0",
                        IsEnabled = false,
                        Description = "Number of active materials in the system"
                    },
                    new() { 
                        Key = "AddressCount", 
                        Label = "Total Active Addresses", 
                        FieldType = FieldType.Text,
                        DefaultValue = "0", 
                        Value = "0",
                        IsEnabled = false,
                        Description = "Number of active addresses in the system"
                    },
                    new() { 
                        Key = "RefreshData", 
                        Label = "🔄 Refresh Data", 
                        FieldType = FieldType.Text,
                        DefaultValue = "Click to refresh", 
                        Value = "Click to refresh",
                        IsEnabled = true,
                        Description = "Reload materials and addresses from database"
                    },
                    new() { 
                        Key = "DataStatus", 
                        Label = "Last Updated", 
                        FieldType = FieldType.Text,
                        DefaultValue = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), 
                        Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                        IsEnabled = false,
                        Description = "When data was last loaded from the database"
                    }
                }
            };

            DataManagementSettings.Add(materialsGroup);
            DataManagementSettings.Add(addressesGroup);
            DataManagementSettings.Add(summaryGroup);
        }

        private void InitializeSecuritySettings()
        {
            var sessionGroup = new SettingsGroup
            {
                Title = "Session Management",
                Description = "Configure user session timeouts and security settings",
                ColumnCount = 2,
                Fields = new List<SettingsField>
                {
                    new() { Key = "AdminSessionTimeout", Label = "Admin Session Timeout (minutes)", FieldType = FieldType.Number,
                           DefaultValue = "5", Value = "5", Tooltip = "Auto-logout time for admin users" },
                    new() { Key = "SuperAdminSessionTimeout", Label = "Super Admin Session Timeout (minutes)", FieldType = FieldType.Number,
                           DefaultValue = "1", Value = "1", Tooltip = "Auto-logout time for super admin" },
                    new() { Key = "EnableAutoLogout", Label = "Enable auto-logout on inactivity", FieldType = FieldType.Checkbox,
                           CheckboxText = "Auto-logout inactive users", DefaultValue = true, Value = true }
                }
            };

            SecuritySettings.Add(sessionGroup);
        }

        private void InitializeWeightRulesSettings()
        {
            // Weight Tolerance Settings
            var toleranceGroup = new SettingsGroup
            {
                Title = "Weight Tolerance Settings",
                Description = "Configure weight measurement tolerances and validation rules",
                ColumnCount = 2,
                Fields = new List<SettingsField>
                {
                    new() { Key = "MinWeightDifference", Label = "Minimum Weight Difference (KG)", FieldType = FieldType.Number,
                           DefaultValue = "5.0", Value = "5.0", Tooltip = "Minimum difference between entry and exit weights" },
                    new() { Key = "MaxWeightVariance", Label = "Maximum Weight Variance (%)", FieldType = FieldType.Number,
                           DefaultValue = "10.0", Value = "10.0", Tooltip = "Maximum allowed weight variance percentage" },
                    new() { Key = "StabilityTimeout", Label = "Stability Timeout (seconds)", FieldType = FieldType.Number,
                           DefaultValue = "30", Value = "30", Tooltip = "Time to wait for weight stabilization" },
                    new() { Key = "AllowNegativeWeights", Label = "Allow negative weight calculations", FieldType = FieldType.Checkbox,
                           CheckboxText = "Enable negative weight calculations", DefaultValue = false, Value = false },
                    new() { Key = "EnableWeightValidation", Label = "Enable weight validation rules", FieldType = FieldType.Checkbox,
                           CheckboxText = "Apply weight validation", DefaultValue = true, Value = true }
                }
            };

            // Weight Adjustment Rules
            var adjustmentGroup = new SettingsGroup
            {
                Title = "Weight Adjustment Rules",
                Description = "Configure manual weight adjustment permissions and limits",
                ColumnCount = 2,
                Fields = new List<SettingsField>
                {
                    new() { Key = "MaxAdjustmentLimit", Label = "Maximum Adjustment Limit (KG)", FieldType = FieldType.Number,
                           DefaultValue = "50.0", Value = "50.0", Tooltip = "Maximum allowed manual weight adjustment" },
                    new() { Key = "AdjustmentReasonCategories", Label = "Adjustment Reason Categories", FieldType = FieldType.Dropdown,
                           Options = GetAdjustmentReasonOptions(), DefaultValue = "Calibration", Value = "Calibration" },
                    new() { Key = "AllowManualAdjustments", Label = "Allow manual weight adjustments", FieldType = FieldType.Checkbox,
                           CheckboxText = "Enable manual adjustments", DefaultValue = false, Value = false },
                    new() { Key = "RequireJustification", Label = "Require justification for adjustments", FieldType = FieldType.Checkbox,
                           CheckboxText = "Require adjustment reason", DefaultValue = true, Value = true },
                    new() { Key = "LogAllAdjustments", Label = "Log all weight adjustments", FieldType = FieldType.Checkbox,
                           CheckboxText = "Enable adjustment logging", DefaultValue = true, Value = true },
                    new() { Key = "RequireSecondApproval", Label = "Require second approval for large adjustments", FieldType = FieldType.Checkbox,
                           CheckboxText = "Dual approval for large adjustments", DefaultValue = true, Value = true }
                }
            };

            WeightRulesSettings.Add(toleranceGroup);
            WeightRulesSettings.Add(adjustmentGroup);
        }

        private void InitializeUserSettings()
        {
            var newUserGroup = new SettingsGroup
            {
                Title = "Add New User",
                Description = "Create new user account with role-based permissions",
                ColumnCount = 3,
                Fields = new List<SettingsField>
                {
                    new() { Key = "NewUsername", Label = "Username", FieldType = FieldType.Text, IsRequired = true,
                           Placeholder = "Enter username", DefaultValue = "", Value = "" },
                    new() { Key = "NewPassword", Label = "Password", FieldType = FieldType.Password, IsRequired = true,
                           Placeholder = "Enter password", DefaultValue = "", Value = "" },
                    new() { Key = "ConfirmPassword", Label = "Confirm Password", FieldType = FieldType.Password, IsRequired = true,
                           Placeholder = "Confirm password", DefaultValue = "", Value = "" },
                    new() { Key = "FullName", Label = "Full Name", FieldType = FieldType.Text, IsRequired = true,
                           Placeholder = "Enter full name", DefaultValue = "", Value = "" },
                    new() { Key = "EmailAddress", Label = "Email Address", FieldType = FieldType.Text,
                           ValidationPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$", DefaultValue = "", Value = "" },
                    new() { Key = "PhoneNumber", Label = "Phone Number", FieldType = FieldType.Text,
                           Placeholder = "Enter phone number", DefaultValue = "", Value = "" },
                    new() { Key = "UserRole", Label = "User Role", FieldType = FieldType.Dropdown, IsRequired = true,
                           Options = GetUserRoleOptions(), DefaultValue = "User", Value = "User" },
                    new() { Key = "Department", Label = "Department", FieldType = FieldType.Dropdown,
                           Options = GetDepartmentOptions(), DefaultValue = "Operations", Value = "Operations" },
                    new() { Key = "AccountActive", Label = "Account Active", FieldType = FieldType.Checkbox,
                           CheckboxText = "Enable account", DefaultValue = true, Value = true }
                }
            };

            UserSettings.Add(newUserGroup);
        }

        private void InitializeSystemSettings()
        {
            // System Information (Read-only display)
            var systemInfoGroup = new SettingsGroup
            {
                Title = "System Information",
                Description = "Current system status and version information",
                ColumnCount = 2,
                Fields = new List<SettingsField>
                {
                    new() { Key = "SoftwareVersion", Label = "Software Version", FieldType = FieldType.Text,
                           DefaultValue = "v2.1.0", Value = "v2.1.0", IsEnabled = false },
                    new() { Key = "DatabaseVersion", Label = "Database Version", FieldType = FieldType.Text,
                           DefaultValue = "v1.5.2", Value = "v1.5.2", IsEnabled = false },
                    new() { Key = "LastBackup", Label = "Last Backup", FieldType = FieldType.Text,
                           DefaultValue = "2024-12-25 10:30:00", Value = "2024-12-25 10:30:00", IsEnabled = false },
                    new() { Key = "TotalRecords", Label = "Total Records", FieldType = FieldType.Text,
                           DefaultValue = "1,247 records", Value = "1,247 records", IsEnabled = false },
                    new() { Key = "DiskUsage", Label = "Disk Usage", FieldType = FieldType.Text,
                           DefaultValue = "2.4 GB / 50 GB (4.8%)", Value = "2.4 GB / 50 GB (4.8%)", IsEnabled = false },
                    new() { Key = "SystemUptime", Label = "System Uptime", FieldType = FieldType.Text,
                           DefaultValue = "3 days, 14 hours", Value = "3 days, 14 hours", IsEnabled = false },
                    new() { Key = "ActiveUsers", Label = "Active Users", FieldType = FieldType.Text,
                           DefaultValue = "2 users online", Value = "2 users online", IsEnabled = false }
                }
            };

            SystemSettings.Add(systemInfoGroup);
        }

        private void InitializeAdminToolsSettings()
        {
            var emergencyGroup = new SettingsGroup
            {
                Title = "Emergency Functions",
                Description = "Critical system administration and emergency controls",
                ColumnCount = 1,
                Fields = new List<SettingsField>
                {
                    new() { Key = "EmergencyMessage", Label = "Emergency system controls require Super Admin privileges", 
                           FieldType = FieldType.Text, DefaultValue = "Contact system administrator for access", 
                           Value = "Contact system administrator for access", IsEnabled = false }
                }
            };

            AdminToolsSettings.Add(emergencyGroup);
        }

        #region Additional Option Providers

        private List<SettingsOption> GetBackupFrequencyOptions()
        {
            return new List<SettingsOption>
            {
                new() { Text = "Daily", Value = "Daily" },
                new() { Text = "Weekly", Value = "Weekly" },
                new() { Text = "Monthly", Value = "Monthly" }
            };
        }

        private List<SettingsOption> GetAdjustmentReasonOptions()
        {
            return new List<SettingsOption>
            {
                new() { Text = "Scale Calibration", Value = "Calibration" },
                new() { Text = "Manual Override", Value = "Override" },
                new() { Text = "System Error", Value = "Error" },
                new() { Text = "Other", Value = "Other" }
            };
        }

        private List<SettingsOption> GetUserRoleOptions()
        {
            return new List<SettingsOption>
            {
                new() { Text = "User", Value = "User" },
                new() { Text = "Admin", Value = "Admin" },
                new() { Text = "Super Admin", Value = "Super Admin" }
            };
        }

        private List<SettingsOption> GetDepartmentOptions()
        {
            return new List<SettingsOption>
            {
                new() { Text = "Operations", Value = "Operations" },
                new() { Text = "Administration", Value = "Administration" },
                new() { Text = "Quality Control", Value = "Quality Control" },
                new() { Text = "Management", Value = "Management" }
            };
        }

        #endregion

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}