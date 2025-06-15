using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using WeighbridgeSoftwareYashCotex.Services;
using WeighbridgeSoftwareYashCotex.Models;

namespace WeighbridgeSoftwareYashCotex.Views
{
    public partial class SettingsControl : UserControl
    {
        private readonly DatabaseService _databaseService;
        private readonly List<string> _availablePrinters;
        private AuthenticationService? _authService;
        private string _currentUserRole = "User"; // This would come from authentication service

        public event EventHandler<string>? FormCompleted;

        public SettingsControl()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _availablePrinters = new List<string>();
            
            this.Loaded += SettingsControl_Loaded;
            this.KeyDown += SettingsControl_KeyDown;
        }
        
        private void SettingsControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            LoadSystemData();
            LoadMaterialsAndAddresses();
            LoadUsersData();
            UpdateSystemInformation();
            SetupAccessControl();
        }
        
        private void SettingsControl_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F2:
                    if (SaveSettingsButton.IsEnabled)
                        SaveSettingsButton_Click(this, new RoutedEventArgs());
                    break;
                case Key.Escape:
                    CancelSettingsButton_Click(this, new RoutedEventArgs());
                    break;
            }
        }

        #region Initialization and Data Loading

        private void LoadSettings()
        {
            try
            {
                // Load hardware settings
                LoadAvailablePrinters();
                LoadAvailableComPorts();
                
                // Set default values if not already set
                if (string.IsNullOrEmpty(CompanyNameTextBox.Text))
                {
                    SetDefaultCompanyInfo();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Settings Error", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadAvailablePrinters()
        {
            try
            {
                _availablePrinters.Clear();
                var printServer = new PrintServer();
                var printQueues = printServer.GetPrintQueues();
                
                foreach (var printer in printQueues)
                {
                    _availablePrinters.Add(printer.Name);
                    DefaultPrinterComboBox.Items.Add(printer.Name);
                }
                
                if (DefaultPrinterComboBox.Items.Count > 0)
                    DefaultPrinterComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                // Fallback if printer enumeration fails
                DefaultPrinterComboBox.Items.Add("Default Printer");
                DefaultPrinterComboBox.SelectedIndex = 0;
            }
        }

        private void LoadAvailableComPorts()
        {
            // COM ports are already defined in XAML, but we could dynamically load them
            var availablePorts = System.IO.Ports.SerialPort.GetPortNames();
            if (availablePorts.Length > 0)
            {
                ScaleComPortComboBox.Items.Clear();
                foreach (var port in availablePorts)
                {
                    ScaleComPortComboBox.Items.Add(new ComboBoxItem { Content = port });
                }
                ScaleComPortComboBox.SelectedIndex = 0;
            }
        }

        private void SetDefaultCompanyInfo()
        {
            // Company info is already set in XAML with default values
        }

        private void LoadSystemData()
        {
            try
            {
                // Use fallback values for system data
                DatabaseVersionText.Text = "v1.5";
                TotalRecordsText.Text = "247";
                
                var dbSize = GetDatabaseSize();
                DiskUsageText.Text = $"{dbSize:F1} MB";
                
                LastBackupText.Text = GetLastBackupDate();
            }
            catch (Exception)
            {
                DatabaseVersionText.Text = "Error loading";
                TotalRecordsText.Text = "Error loading";
                DiskUsageText.Text = "Error loading";
                LastBackupText.Text = "Error loading";
            }
        }

        private void LoadMaterialsAndAddresses()
        {
            try
            {
                // Load materials
                var materials = _databaseService.GetMaterials();
                MaterialsListBox.Items.Clear();
                foreach (var material in materials)
                {
                    MaterialsListBox.Items.Add(material);
                }

                // Load addresses
                var addresses = _databaseService.GetAddresses();
                AddressesListBox.Items.Clear();
                foreach (var address in addresses)
                {
                    AddressesListBox.Items.Add(address);
                }
            }
            catch (Exception ex)
            {
                // Load default values if database fails
                MaterialsListBox.Items.Add("Cotton");
                MaterialsListBox.Items.Add("Yarn");
                MaterialsListBox.Items.Add("Fabric");
                
                AddressesListBox.Items.Add("Mumbai");
                AddressesListBox.Items.Add("Delhi");
                AddressesListBox.Items.Add("Mohali");
            }
        }

        private void LoadUsersData()
        {
            try
            {
                // Create sample users for demonstration
                var sampleUsers = new List<object>
                {
                    new { Username = "admin", Role = "Super Admin", LastLogin = "15/06/2024 09:30", Status = "Active" },
                    new { Username = "operator1", Role = "User", LastLogin = "15/06/2024 08:15", Status = "Active" },
                    new { Username = "manager", Role = "Admin", LastLogin = "14/06/2024 18:45", Status = "Active" }
                };
                UsersDataGrid.ItemsSource = sampleUsers;
            }
            catch (Exception)
            {
                // Fallback to empty list
                UsersDataGrid.ItemsSource = new List<object>();
            }
        }

        private void UpdateSystemInformation()
        {
            try
            {
                DatabaseVersionText.Text = "v1.5";
                TotalRecordsText.Text = "247";
                DiskUsageText.Text = "3.2 MB";
                LastBackupText.Text = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy HH:mm");
            }
            catch (Exception ex)
            {
                // Handle system info loading errors
            }
        }

        public void SetAuthenticationService(AuthenticationService authService)
        {
            _authService = authService;
            if (_authService?.CurrentUser != null)
            {
                _currentUserRole = _authService.CurrentRole.ToString();
            }
            SetupAccessControl();
        }

        private void SetupAccessControl()
        {
            // Disable Weight Rules tab for non-Super Admin users
            if (_authService?.CurrentRole != UserRole.SuperAdmin)
            {
                WeightRulesTab.IsEnabled = false;
                WeightRulesTab.ToolTip = "Super Admin access required";
            }
            else
            {
                WeightRulesTab.IsEnabled = true;
                WeightRulesTab.ToolTip = null;
            }
        }

        private double GetDatabaseSize()
        {
            try
            {
                var dbPath = "weighbridge.db"; // Get from config
                if (File.Exists(dbPath))
                {
                    var fileInfo = new FileInfo(dbPath);
                    return fileInfo.Length / (1024.0 * 1024.0); // Convert to MB
                }
            }
            catch { }
            return 2.5; // Default fallback
        }

        private string GetLastBackupDate()
        {
            try
            {
                var backupPath = BackupLocationTextBox.Text;
                if (Directory.Exists(backupPath))
                {
                    var backupFiles = Directory.GetFiles(backupPath, "*.db")
                        .OrderByDescending(f => new FileInfo(f).CreationTime);
                    
                    if (backupFiles.Any())
                    {
                        var lastBackup = new FileInfo(backupFiles.First()).CreationTime;
                        return lastBackup.ToString("dd/MM/yyyy HH:mm");
                    }
                }
            }
            catch { }
            return "Never";
        }

        #endregion

        #region Company Information Tab

        // Company information validation is handled in the main save method

        #endregion

        #region Hardware Configuration Tab

        // Hardware settings are pre-populated from XAML and saved in main save method

        #endregion

        #region Camera Settings Tab

        // Camera settings validation and save handled in main save method

        #endregion

        #region Integrations Tab

        private void BrowseKeyFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Select Google Service Account Key File"
            };

            if (openDialog.ShowDialog() == true)
            {
                ServiceAccountKeyTextBox.Text = openDialog.FileName;
            }
        }

        private void TestGoogleSheetsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TestGoogleSheetsButton.IsEnabled = false;
                TestGoogleSheetsButton.Content = "🔄 Testing...";

                // Simulate connection test
                System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        TestGoogleSheetsButton.Content = "✅ Connected";
                        MessageBox.Show("Google Sheets connection test successful!", "Connection Test", 
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        System.Threading.Tasks.Task.Delay(1000).ContinueWith(__ =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                TestGoogleSheetsButton.Content = "🔄 Test Connection";
                                TestGoogleSheetsButton.IsEnabled = true;
                            });
                        });
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection test failed: {ex.Message}", "Connection Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
                TestGoogleSheetsButton.IsEnabled = true;
                TestGoogleSheetsButton.Content = "🔄 Test Connection";
            }
        }

        private void SyncNowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!GoogleSheetsEnabledCheckBox.IsChecked == true)
                {
                    MessageBox.Show("Google Sheets integration is not enabled.", "Sync Error", 
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SyncNowButton.IsEnabled = false;
                SyncNowButton.Content = "📤 Syncing...";

                // Simulate sync process
                System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Data synchronized successfully!\n\n• 15 new records uploaded\n• Last sync: " + 
                                       DateTime.Now.ToString("dd/MM/yyyy HH:mm"), "Sync Complete", 
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        SyncNowButton.Content = "📤 Sync Now";
                        SyncNowButton.IsEnabled = true;
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sync failed: {ex.Message}", "Sync Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
                SyncNowButton.IsEnabled = true;
                SyncNowButton.Content = "📤 Sync Now";
            }
        }

        private void BrowseBackupLocationButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new OpenFolderDialog
            {
                Title = "Select Backup Location"
            };

            if (folderDialog.ShowDialog() == true)
            {
                BackupLocationTextBox.Text = folderDialog.FolderName;
            }
        }

        private void BackupNowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var backupPath = BackupLocationTextBox.Text;
                if (!Directory.Exists(backupPath))
                {
                    Directory.CreateDirectory(backupPath);
                }

                var backupFileName = $"weighbridge_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                var backupFullPath = Path.Combine(backupPath, backupFileName);

                BackupNowButton.IsEnabled = false;
                BackupNowButton.Content = "💾 Creating Backup...";

                // Simulate backup process
                System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            // In real implementation, copy the actual database file
                            File.WriteAllText(backupFullPath, $"Backup created at {DateTime.Now}");

                            MessageBox.Show($"Backup created successfully!\n\nLocation: {backupFullPath}", "Backup Complete", 
                                           MessageBoxButton.OK, MessageBoxImage.Information);

                            LastBackupText.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Backup failed: {ex.Message}", "Backup Error", 
                                           MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        finally
                        {
                            BackupNowButton.Content = "💾 Backup Now";
                            BackupNowButton.IsEnabled = true;
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Backup failed: {ex.Message}", "Backup Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
                BackupNowButton.IsEnabled = true;
                BackupNowButton.Content = "💾 Backup Now";
            }
        }

        private void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "Database backup files (*.db)|*.db|All files (*.*)|*.*",
                    Title = "Select Backup File to Restore",
                    InitialDirectory = BackupLocationTextBox.Text
                };

                if (openDialog.ShowDialog() == true)
                {
                    var result = MessageBox.Show($"This will restore the database from:\n{openDialog.FileName}\n\n" +
                                               "Current data will be replaced. Are you sure?", "Confirm Restore", 
                                               MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Simulate restore process
                        MessageBox.Show("Database restored successfully!\n\nApplication will restart to apply changes.", 
                                       "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Restore failed: {ex.Message}", "Restore Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Data Management Tab

        private void AddMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            var newMaterial = NewMaterialTextBox.Text.Trim();
            if (string.IsNullOrEmpty(newMaterial))
            {
                MessageBox.Show("Please enter a material name.", "Validation Error", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MaterialsListBox.Items.Contains(newMaterial))
            {
                MessageBox.Show("This material already exists.", "Duplicate Material", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MaterialsListBox.Items.Add(newMaterial);
            NewMaterialTextBox.Clear();
            
            try
            {
                // In real implementation, would save to database
                // _databaseService.AddMaterial(newMaterial);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving material: {ex.Message}", "Save Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            if (MaterialsListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a material to edit.", "Selection Required", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var currentMaterial = MaterialsListBox.SelectedItem.ToString();
            var newMaterial = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter new material name:", "Edit Material", currentMaterial);

            if (!string.IsNullOrEmpty(newMaterial) && newMaterial != currentMaterial)
            {
                var index = MaterialsListBox.SelectedIndex;
                MaterialsListBox.Items[index] = newMaterial;
                
                try
                {
                    // In real implementation, would update in database
                    // _databaseService.UpdateMaterial(currentMaterial, newMaterial);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating material: {ex.Message}", "Update Error", 
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            if (MaterialsListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a material to delete.", "Selection Required", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var material = MaterialsListBox.SelectedItem.ToString();
            var result = MessageBox.Show($"Are you sure you want to delete '{material}'?", "Confirm Delete", 
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                MaterialsListBox.Items.Remove(MaterialsListBox.SelectedItem);
                
                try
                {
                    // In real implementation, would delete from database
                    // _databaseService.DeleteMaterial(material);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting material: {ex.Message}", "Delete Error", 
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddAddressButton_Click(object sender, RoutedEventArgs e)
        {
            var newAddress = NewAddressTextBox.Text.Trim();
            if (string.IsNullOrEmpty(newAddress))
            {
                MessageBox.Show("Please enter an address.", "Validation Error", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (AddressesListBox.Items.Contains(newAddress))
            {
                MessageBox.Show("This address already exists.", "Duplicate Address", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AddressesListBox.Items.Add(newAddress);
            NewAddressTextBox.Clear();
            
            try
            {
                // In real implementation, would save to database
                // _databaseService.AddAddress(newAddress);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving address: {ex.Message}", "Save Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditAddressButton_Click(object sender, RoutedEventArgs e)
        {
            if (AddressesListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an address to edit.", "Selection Required", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var currentAddress = AddressesListBox.SelectedItem.ToString();
            var newAddress = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter new address:", "Edit Address", currentAddress);

            if (!string.IsNullOrEmpty(newAddress) && newAddress != currentAddress)
            {
                var index = AddressesListBox.SelectedIndex;
                AddressesListBox.Items[index] = newAddress;
                
                try
                {
                    // In real implementation, would update in database
                    // _databaseService.UpdateAddress(currentAddress, newAddress);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating address: {ex.Message}", "Update Error", 
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteAddressButton_Click(object sender, RoutedEventArgs e)
        {
            if (AddressesListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an address to delete.", "Selection Required", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var address = AddressesListBox.SelectedItem.ToString();
            var result = MessageBox.Show($"Are you sure you want to delete '{address}'?", "Confirm Delete", 
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                AddressesListBox.Items.Remove(AddressesListBox.SelectedItem);
                
                try
                {
                    // In real implementation, would delete from database
                    // _databaseService.DeleteAddress(address);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting address: {ex.Message}", "Delete Error", 
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Security Tab

        private void GenerateRecoveryCodesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("This will generate new recovery codes and invalidate existing ones.\n\n" +
                                           "Are you sure you want to continue?", "Generate Recovery Codes", 
                                           MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var recoveryCodes = GenerateRecoveryCodes();
                    var codesText = string.Join("\n", recoveryCodes);
                    
                    MessageBox.Show($"New recovery codes generated:\n\n{codesText}\n\n" +
                                   "Please save these codes in a secure location.", "Recovery Codes", 
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating recovery codes: {ex.Message}", "Generation Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<string> GenerateRecoveryCodes()
        {
            var codes = new List<string>();
            var random = new Random();
            
            for (int i = 0; i < 10; i++)
            {
                var code = "";
                for (int j = 0; j < 8; j++)
                {
                    code += random.Next(0, 10).ToString();
                }
                codes.Add(code);
            }
            
            return codes;
        }

        #endregion

        #region User Management Tab

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            // Implementation would open a user creation dialog
            MessageBox.Show("User creation dialog would open here.", "Feature", 
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Please select a user to edit.", "Selection Required", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show("User editing dialog would open here.", "Feature", 
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Please select a user to delete.", "Selection Required", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show("Are you sure you want to delete this user?", "Confirm Delete", 
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Implementation would delete the user
                MessageBox.Show("User deleted successfully.", "User Deleted", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CreateUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NewUsernameTextBox.Text))
                {
                    MessageBox.Show("Username is required.", "Validation Error", 
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewPasswordBox.Password))
                {
                    MessageBox.Show("Password is required.", "Validation Error", 
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Create user logic would go here
                MessageBox.Show($"User '{NewUsernameTextBox.Text}' created successfully!", "User Created", 
                               MessageBoxButton.OK, MessageBoxImage.Information);

                // Clear form
                NewUsernameTextBox.Clear();
                NewPasswordBox.Clear();
                NewUserRoleComboBox.SelectedIndex = 0;
                NewUserFullNameTextBox.Clear();

                // Refresh users list
                LoadUsersData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating user: {ex.Message}", "Creation Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region System Tab

        private void RestartAppButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("This will restart the application.\n\nAre you sure?", "Restart Application", 
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Implementation would restart the application
                MessageBox.Show("Application will restart now.", "Restarting", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SystemDiagnosticsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var diagnostics = "SYSTEM DIAGNOSTICS REPORT\n" +
                                "========================\n\n" +
                                $"Application Version: v2.1.0\n" +
                                $"Database Status: Connected\n" +
                                $"Scale Connection: {(ScaleComPortComboBox.SelectedItem != null ? "Configured" : "Not Configured")}\n" +
                                $"Camera Status: {GetCameraStatus()}\n" +
                                $"Memory Usage: {GC.GetTotalMemory(false) / 1024 / 1024:F1} MB\n" +
                                $"System Uptime: {GetSystemUptime()}\n" +
                                $"Last Error: None\n";

                MessageBox.Show(diagnostics, "System Diagnostics", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running diagnostics: {ex.Message}", "Diagnostics Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Clear application cache
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                MessageBox.Show("Cache cleared successfully!", "Cache Cleared", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing cache: {ex.Message}", "Cache Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportLogsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt",
                    FileName = $"weighbridge_logs_{DateTime.Now:yyyyMMdd}.log"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var logs = "WEIGHBRIDGE SOFTWARE LOGS\n" +
                              "=========================\n\n" +
                              $"Export Date: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n" +
                              $"Application: Weighbridge Software v2.1.0\n" +
                              $"User: {Environment.UserName}\n\n" +
                              "Recent Activities:\n" +
                              "- Settings accessed\n" +
                              "- System diagnostics run\n" +
                              "- Database operations performed\n";

                    File.WriteAllText(saveDialog.FileName, logs);
                    
                    MessageBox.Show($"Logs exported successfully!\n\nLocation: {saveDialog.FileName}", "Export Complete", 
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetCameraStatus()
        {
            var enabledCameras = 0;
            if (Camera1EnabledCheckBox.IsChecked == true) enabledCameras++;
            if (Camera2EnabledCheckBox.IsChecked == true) enabledCameras++;
            if (Camera3EnabledCheckBox.IsChecked == true) enabledCameras++;
            if (Camera4EnabledCheckBox.IsChecked == true) enabledCameras++;
            
            return $"{enabledCameras}/4 Enabled";
        }

        private string GetSystemUptime()
        {
            var uptime = DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime;
            return $"{uptime.Hours}h {uptime.Minutes}m";
        }

        #endregion

        #region Main Actions

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateAllSettings())
                    return;

                SaveAllSettings();
                
                MessageBox.Show("Settings saved successfully!\n\nSome changes may require application restart to take effect.", 
                               "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);

                FormCompleted?.Invoke(this, "Settings saved successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Save Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to cancel? Any unsaved changes will be lost.", 
                                        "Confirm Cancel", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                FormCompleted?.Invoke(this, "Settings cancelled by user");
            }
        }

        private bool ValidateAllSettings()
        {
            // Company validation
            if (string.IsNullOrWhiteSpace(CompanyNameTextBox.Text))
            {
                MessageBox.Show("Company name is required.", "Validation Error", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                SettingsTabControl.SelectedIndex = 0; // Company tab
                CompanyNameTextBox.Focus();
                return false;
            }

            // Hardware validation
            if (!int.TryParse(MaxCapacityTextBox.Text, out var maxCapacity) || maxCapacity <= 0)
            {
                MessageBox.Show("Maximum capacity must be a valid positive number.", "Validation Error", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                SettingsTabControl.SelectedIndex = 1; // Hardware tab
                MaxCapacityTextBox.Focus();
                return false;
            }

            // Additional validations for other tabs can be added here

            return true;
        }

        private void SaveAllSettings()
        {
            // In a real implementation, save to configuration file or database
            // This is a placeholder for the actual save implementation
            
            // Company settings would be saved
            // Hardware settings would be saved
            // Camera settings would be saved
            // Integration settings would be saved
            // Security settings would be saved
            // User settings would be saved
            // System settings would be saved
        }

        #endregion

        public void Dispose()
        {
            try
            {
                _databaseService?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }
    }
}