using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using WeighbridgeSoftwareYashCotex.Models;
using WeighbridgeSoftwareYashCotex.Services;

namespace WeighbridgeSoftwareYashCotex.ViewModels
{
    public class ModernSettingsViewModel : INotifyPropertyChanged
    {
        private readonly SettingsService _settingsService;
        private bool _isLoading;

        public event EventHandler<string>? SettingsOperationCompleted;

        public ObservableCollection<SettingsGroup> CompanySettings { get; }
        public ObservableCollection<SettingsGroup> HardwareSettings { get; }
        public ObservableCollection<SettingsGroup> CameraSettings { get; }
        public ObservableCollection<SettingsGroup> IntegrationSettings { get; }
        public ObservableCollection<SettingsGroup> UserSettings { get; }
        public ObservableCollection<SettingsGroup> SystemSettings { get; }

        public ICommand SaveAllCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand CancelCommand { get; }

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

        public ModernSettingsViewModel()
        {
            _settingsService = SettingsService.Instance;
            
            CompanySettings = new ObservableCollection<SettingsGroup>();
            HardwareSettings = new ObservableCollection<SettingsGroup>();
            CameraSettings = new ObservableCollection<SettingsGroup>();
            IntegrationSettings = new ObservableCollection<SettingsGroup>();
            UserSettings = new ObservableCollection<SettingsGroup>();
            SystemSettings = new ObservableCollection<SettingsGroup>();

            SaveAllCommand = new RelayCommand(SaveAll, CanSaveAll);
            ResetCommand = new RelayCommand(ResetAll);
            CancelCommand = new RelayCommand(Cancel);

            InitializeSettings();
            LoadCurrentValues();
        }

        private void InitializeSettings()
        {
            InitializeCompanySettings();
            InitializeHardwareSettings();
            InitializeCameraSettings();
            InitializeIntegrationSettings();
            InitializeUserSettings();
            InitializeSystemSettings();
        }

        private void InitializeCompanySettings()
        {
            var companyInfo = new SettingsGroup
            {
                Title = "Company Information",
                Description = "Basic company details and branding",
                ColumnCount = 3,
                Fields = new List<SettingsField>
                {
                    new() { Key = "CompanyName", Label = "Company Name", FieldType = FieldType.Text, IsRequired = true, Placeholder = "Enter company name" },
                    new() { Key = "CompanyEmail", Label = "Email Address", FieldType = FieldType.Text, ValidationPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$", ValidationMessage = "Invalid email format" },
                    new() { Key = "CompanyPhone", Label = "Phone Number", FieldType = FieldType.Text },
                    new() { Key = "CompanyAddress", Label = "Address Line 1", FieldType = FieldType.Text },
                    new() { Key = "CompanyAddressLine2", Label = "Address Line 2", FieldType = FieldType.Text },
                    new() { Key = "CompanyCity", Label = "City", FieldType = FieldType.Text },
                    new() { Key = "CompanyGSTIN", Label = "GSTIN", FieldType = FieldType.Text, Tooltip = "Goods and Services Tax Identification Number" },
                    new() { Key = "CompanyLicense", Label = "License Number", FieldType = FieldType.Text },
                    new() { Key = "CompanyLogo", Label = "Company Logo", FieldType = FieldType.File, FileFilter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg" }
                }
            };

            CompanySettings.Add(companyInfo);
        }

        private void InitializeHardwareSettings()
        {
            var weighbridgeConfig = new SettingsGroup
            {
                Title = "Weighbridge Configuration",
                Description = "Configure weighbridge hardware connection and parameters",
                ColumnCount = 2,
                Fields = new List<SettingsField>
                {
                    new() { Key = "WeighbridgeComPort", Label = "COM Port", FieldType = FieldType.Dropdown, IsRequired = true,
                           Options = GetComPortOptions() },
                    new() { Key = "BaudRate", Label = "Baud Rate", FieldType = FieldType.Dropdown, IsRequired = true,
                           Options = GetBaudRateOptions(), DefaultValue = "9600" },
                    new() { Key = "DataBits", Label = "Data Bits", FieldType = FieldType.Dropdown,
                           Options = GetDataBitsOptions(), DefaultValue = "8" },
                    new() { Key = "StopBits", Label = "Stop Bits", FieldType = FieldType.Dropdown,
                           Options = GetStopBitsOptions(), DefaultValue = "One" },
                    new() { Key = "WeighbridgeCapacity", Label = "Maximum Capacity (KG)", FieldType = FieldType.Number, IsRequired = true, DefaultValue = "50000" },
                    new() { Key = "WeighbridgeTimeout", Label = "Timeout (seconds)", FieldType = FieldType.Number, DefaultValue = "30" }
                }
            };

            HardwareSettings.Add(weighbridgeConfig);
        }

        private void InitializeCameraSettings()
        {
            for (int i = 1; i <= 4; i++)
            {
                var cameraConfig = new SettingsGroup
                {
                    Title = $"Camera {i} Configuration",
                    Description = $"Configure camera {i} connection settings",
                    ColumnCount = 3,
                    Fields = new List<SettingsField>
                    {
                        new() { Key = $"Camera{i}Name", Label = "Camera Name", FieldType = FieldType.Text, DefaultValue = $"Camera {i}" },
                        new() { Key = $"Camera{i}IpAddress", Label = "IP Address", FieldType = FieldType.Text, IsRequired = true, 
                               ValidationPattern = @"^(\d{1,3}\.){3}\d{1,3}$", ValidationMessage = "Invalid IP address format" },
                        new() { Key = $"Camera{i}Port", Label = "Port", FieldType = FieldType.Number, DefaultValue = "80" },
                        new() { Key = $"Camera{i}Username", Label = "Username", FieldType = FieldType.Text, DefaultValue = "admin" },
                        new() { Key = $"Camera{i}Password", Label = "Password", FieldType = FieldType.Password },
                        new() { Key = $"Camera{i}StreamPath", Label = "Stream Path", FieldType = FieldType.Text, DefaultValue = "/mjpeg/1" },
                        new() { Key = $"Camera{i}Protocol", Label = "Protocol", FieldType = FieldType.Dropdown,
                               Options = GetProtocolOptions(), DefaultValue = "HTTP" },
                        new() { Key = $"Camera{i}Enabled", Label = "Enable Camera", FieldType = FieldType.Checkbox, DefaultValue = true },
                        new() { Key = $"Camera{i}RefreshRate", Label = "Refresh Rate (ms)", FieldType = FieldType.Number, DefaultValue = "1000" }
                    }
                };

                CameraSettings.Add(cameraConfig);
            }
        }

        private void InitializeIntegrationSettings()
        {
            var googleSheets = new SettingsGroup
            {
                Title = "Google Sheets Integration",
                Description = "Configure Google Sheets synchronization settings",
                ColumnCount = 2,
                Fields = new List<SettingsField>
                {
                    new() { Key = "GoogleServiceAccountFile", Label = "Service Account File", FieldType = FieldType.File, IsRequired = true,
                           FileFilter = "JSON Files (*.json)|*.json" },
                    new() { Key = "GoogleSpreadsheetId", Label = "Spreadsheet ID", FieldType = FieldType.Text, IsRequired = true },
                    new() { Key = "GoogleWorksheetName", Label = "Worksheet Name", FieldType = FieldType.Text, DefaultValue = "Sheet1" },
                    new() { Key = "GoogleAutoSync", Label = "Enable Auto Sync", FieldType = FieldType.Checkbox, DefaultValue = true },
                    new() { Key = "GoogleSyncInterval", Label = "Sync Interval (minutes)", FieldType = FieldType.Number, DefaultValue = "5" },
                    new() { Key = "GoogleSyncOnEntry", Label = "Sync on Entry", FieldType = FieldType.Checkbox, DefaultValue = true }
                }
            };

            IntegrationSettings.Add(googleSheets);
        }

        private void InitializeUserSettings()
        {
            var userManagement = new SettingsGroup
            {
                Title = "User Management",
                Description = "Configure user accounts and permissions",
                ColumnCount = 3,
                Fields = new List<SettingsField>
                {
                    new() { Key = "NewUsername", Label = "Username", FieldType = FieldType.Text, IsRequired = true },
                    new() { Key = "NewPassword", Label = "Password", FieldType = FieldType.Password, IsRequired = true },
                    new() { Key = "NewPasswordConfirm", Label = "Confirm Password", FieldType = FieldType.Password, IsRequired = true },
                    new() { Key = "NewUserFullName", Label = "Full Name", FieldType = FieldType.Text, IsRequired = true },
                    new() { Key = "NewUserEmail", Label = "Email Address", FieldType = FieldType.Text },
                    new() { Key = "NewUserPhone", Label = "Phone Number", FieldType = FieldType.Text },
                    new() { Key = "NewUserRole", Label = "User Role", FieldType = FieldType.Dropdown, IsRequired = true,
                           Options = GetUserRoleOptions(), DefaultValue = "User" },
                    new() { Key = "NewUserDepartment", Label = "Department", FieldType = FieldType.Dropdown,
                           Options = GetDepartmentOptions(), DefaultValue = "Operations" },
                    new() { Key = "NewUserActive", Label = "Account Active", FieldType = FieldType.Checkbox, DefaultValue = true }
                }
            };

            UserSettings.Add(userManagement);
        }

        private void InitializeSystemSettings()
        {
            var systemInfo = new SettingsGroup
            {
                Title = "System Information",
                Description = "View and manage system status and performance",
                ColumnCount = 3,
                Fields = new List<SettingsField>
                {
                    new() { Key = "SystemVersion", Label = "Software Version", FieldType = FieldType.Text, IsEnabled = false, DefaultValue = "v2.1.0" },
                    new() { Key = "DatabaseVersion", Label = "Database Version", FieldType = FieldType.Text, IsEnabled = false },
                    new() { Key = "LastBackup", Label = "Last Backup", FieldType = FieldType.Text, IsEnabled = false },
                    new() { Key = "TotalRecords", Label = "Total Records", FieldType = FieldType.Text, IsEnabled = false },
                    new() { Key = "DiskUsage", Label = "Disk Usage", FieldType = FieldType.Text, IsEnabled = false },
                    new() { Key = "SystemUptime", Label = "System Uptime", FieldType = FieldType.Text, IsEnabled = false },
                    new() { Key = "ActiveUsers", Label = "Active Users", FieldType = FieldType.Text, IsEnabled = false },
                    new() { Key = "CpuUsage", Label = "CPU Usage", FieldType = FieldType.Text, IsEnabled = false },
                    new() { Key = "MemoryUsage", Label = "Memory Usage", FieldType = FieldType.Text, IsEnabled = false }
                }
            };

            SystemSettings.Add(systemInfo);
        }

        private void LoadCurrentValues()
        {
            IsLoading = true;
            
            try
            {
                // Load company settings
                var companyGroup = CompanySettings.FirstOrDefault();
                if (companyGroup != null)
                {
                    companyGroup.SetValues(new Dictionary<string, object?>
                    {
                        ["CompanyName"] = _settingsService.CompanyName,
                        ["CompanyEmail"] = _settingsService.CompanyEmail,
                        ["CompanyPhone"] = _settingsService.CompanyPhone,
                        ["CompanyAddress"] = _settingsService.CompanyAddressLine1,
                        ["CompanyAddressLine2"] = _settingsService.CompanyAddressLine2,
                        ["CompanyCity"] = _settingsService.CompanyCity,
                        ["CompanyGSTIN"] = _settingsService.CompanyGSTIN,
                        ["CompanyLicense"] = _settingsService.CompanyLicense,
                        ["CompanyLogo"] = _settingsService.CompanyLogo
                    });
                }

                // Load hardware settings
                var hardwareGroup = HardwareSettings.FirstOrDefault();
                if (hardwareGroup != null)
                {
                    hardwareGroup.SetValues(new Dictionary<string, object?>
                    {
                        ["WeighbridgeComPort"] = _settingsService.WeighbridgeComPort,
                        ["BaudRate"] = _settingsService.WeighbridgeBaudRate?.ToString(),
                        ["DataBits"] = _settingsService.WeighbridgeDataBits?.ToString(),
                        ["StopBits"] = _settingsService.WeighbridgeStopBits?.ToString(),
                        ["WeighbridgeCapacity"] = _settingsService.WeighbridgeCapacity?.ToString(),
                        ["WeighbridgeTimeout"] = _settingsService.WeighbridgeTimeout?.ToString()
                    });
                }

                // Load other settings groups...
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SaveAll()
        {
            try
            {
                IsLoading = true;

                // Validate all fields first
                var allGroups = new[]
                {
                    CompanySettings, HardwareSettings, CameraSettings, 
                    IntegrationSettings, UserSettings, SystemSettings
                }.SelectMany(collection => collection);

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
                SaveSettings();
                
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

        private void SaveSettings()
        {
            // Save company settings
            var companyGroup = CompanySettings.FirstOrDefault();
            if (companyGroup != null)
            {
                var values = companyGroup.GetValues();
                _settingsService.CompanyName = values.GetValueOrDefault("CompanyName")?.ToString();
                _settingsService.CompanyEmail = values.GetValueOrDefault("CompanyEmail")?.ToString();
                _settingsService.CompanyPhone = values.GetValueOrDefault("CompanyPhone")?.ToString();
                _settingsService.CompanyAddressLine1 = values.GetValueOrDefault("CompanyAddress")?.ToString();
                _settingsService.CompanyAddressLine2 = values.GetValueOrDefault("CompanyAddressLine2")?.ToString();
                _settingsService.CompanyCity = values.GetValueOrDefault("CompanyCity")?.ToString();
                _settingsService.CompanyGSTIN = values.GetValueOrDefault("CompanyGSTIN")?.ToString();
                _settingsService.CompanyLicense = values.GetValueOrDefault("CompanyLicense")?.ToString();
                _settingsService.CompanyLogo = values.GetValueOrDefault("CompanyLogo")?.ToString();
            }

            // Save hardware settings
            var hardwareGroup = HardwareSettings.FirstOrDefault();
            if (hardwareGroup != null)
            {
                var values = hardwareGroup.GetValues();
                _settingsService.WeighbridgeComPort = values.GetValueOrDefault("WeighbridgeComPort")?.ToString();
                
                if (int.TryParse(values.GetValueOrDefault("BaudRate")?.ToString(), out var baudRate))
                    _settingsService.WeighbridgeBaudRate = baudRate;
                
                if (int.TryParse(values.GetValueOrDefault("DataBits")?.ToString(), out var dataBits))
                    _settingsService.WeighbridgeDataBits = dataBits;
                
                if (double.TryParse(values.GetValueOrDefault("WeighbridgeCapacity")?.ToString(), out var capacity))
                    _settingsService.WeighbridgeCapacity = capacity;
                
                if (int.TryParse(values.GetValueOrDefault("WeighbridgeTimeout")?.ToString(), out var timeout))
                    _settingsService.WeighbridgeTimeout = timeout;
            }

            // Save other settings...
            _settingsService.SaveSettings();
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
                var allGroups = new[]
                {
                    CompanySettings, HardwareSettings, CameraSettings, 
                    IntegrationSettings, UserSettings, SystemSettings
                }.SelectMany(collection => collection);

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

        // Helper methods to get options for dropdowns
        private List<SettingsOption> GetComPortOptions()
        {
            var ports = System.IO.Ports.SerialPort.GetPortNames();
            return ports.Select(p => new SettingsOption { Text = p, Value = p }).ToList();
        }

        private List<SettingsOption> GetBaudRateOptions()
        {
            var rates = new[] { "9600", "19200", "38400", "57600", "115200" };
            return rates.Select(r => new SettingsOption { Text = r, Value = r }).ToList();
        }

        private List<SettingsOption> GetDataBitsOptions()
        {
            var bits = new[] { "7", "8" };
            return bits.Select(b => new SettingsOption { Text = b, Value = b }).ToList();
        }

        private List<SettingsOption> GetStopBitsOptions()
        {
            return new List<SettingsOption>
            {
                new() { Text = "None", Value = "None" },
                new() { Text = "One", Value = "One" },
                new() { Text = "Two", Value = "Two" },
                new() { Text = "OnePointFive", Value = "OnePointFive" }
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute();
        }
    }
}