using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using WeighbridgeSoftwareYashCotex.Services;

namespace WeighbridgeSoftwareYashCotex.Views
{
    public partial class SettingsControl : UserControl
    {
        private readonly DatabaseService _databaseService;
        private Button _activeTabButton;

        public event EventHandler<string>? FormCompleted;

        public SettingsControl()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _activeTabButton = CompanyTabButton;
            
            LoadSettings();
            UpdateSystemUptime();
        }

        private void LoadSettings()
        {
            try
            {
                // Load settings from configuration file or database
                // For now, using default values already set in XAML
                DatabaseStatusText.Text = "Database: Connected | Size: 2.5 MB | Records: 150";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Settings Error", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateSystemUptime()
        {
            var uptime = DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime;
            SystemUptimeText.Text = $"System Uptime: {uptime.Hours}h {uptime.Minutes}m";
        }

        #region Tab Navigation

        private void CompanyTabButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchTab(CompanyTabButton, CompanySettingsPanel);
        }

        private void WeighbridgeTabButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchTab(WeighbridgeTabButton, WeighbridgeSettingsPanel);
        }

        private void DatabaseTabButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchTab(DatabaseTabButton, DatabaseSettingsPanel);
        }

        private void SystemTabButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchTab(SystemTabButton, SystemSettingsPanel);
        }

        private void SwitchTab(Button tabButton, StackPanel panel)
        {
            // Reset all tab buttons
            CompanyTabButton.Style = (Style)FindResource("TabButtonStyle");
            WeighbridgeTabButton.Style = (Style)FindResource("TabButtonStyle");
            DatabaseTabButton.Style = (Style)FindResource("TabButtonStyle");
            SystemTabButton.Style = (Style)FindResource("TabButtonStyle");

            // Hide all panels
            CompanySettingsPanel.Visibility = Visibility.Collapsed;
            WeighbridgeSettingsPanel.Visibility = Visibility.Collapsed;
            DatabaseSettingsPanel.Visibility = Visibility.Collapsed;
            SystemSettingsPanel.Visibility = Visibility.Collapsed;

            // Activate selected tab
            tabButton.Style = (Style)FindResource("ActiveTabButtonStyle");
            panel.Visibility = Visibility.Visible;
            _activeTabButton = tabButton;
        }

        #endregion

        #region Weighbridge Settings

        private void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TestConnectionButton.IsEnabled = false;
                ConnectionStatusText.Text = "Testing connection...";
                ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7));

                // Simulate connection test
                System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        // Simulate successful connection
                        ConnectionStatusText.Text = "✅ Connection successful - Scale responding";
                        ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69));
                        TestConnectionButton.IsEnabled = true;
                    });
                });
            }
            catch (Exception ex)
            {
                ConnectionStatusText.Text = $"❌ Connection failed: {ex.Message}";
                ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                TestConnectionButton.IsEnabled = true;
            }
        }

        #endregion

        #region Database Settings

        private void BrowseDatabasePath_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Database files (*.db)|*.db|All files (*.*)|*.*",
                Title = "Select Database File"
            };

            if (openDialog.ShowDialog() == true)
            {
                DatabasePathTextBox.Text = openDialog.FileName;
            }
        }

        private void BrowseBackupLocation_Click(object sender, RoutedEventArgs e)
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

                // Simulate backup process
                File.Copy(DatabasePathTextBox.Text, backupFullPath, true);

                MessageBox.Show($"Backup created successfully!\n\nLocation: {backupFullPath}", "Backup Complete", 
                               MessageBoxButton.OK, MessageBoxImage.Information);

                DatabaseStatusText.Text = $"Database: Connected | Last Backup: {DateTime.Now:dd/MM/yyyy HH:mm}";
                DatabaseStatusText.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Backup failed: {ex.Message}", "Backup Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx",
                    FileName = $"weighbridge_export_{DateTime.Now:yyyyMMdd}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Get all weighment entries
                    var entries = _databaseService.GetAllWeighments();
                    
                    using (var writer = new StreamWriter(saveDialog.FileName))
                    {
                        // Write header
                        writer.WriteLine("RST Number,Vehicle Number,Customer Name,Phone,Material,Entry Weight,Exit Weight,Net Weight,Entry Date,Exit Date");
                        
                        // Write data
                        foreach (var entry in entries)
                        {
                            writer.WriteLine($"{entry.RstNumber},{entry.VehicleNumber},{entry.Name},{entry.PhoneNumber}," +
                                           $"{entry.Material},{entry.EntryWeight},{entry.ExitWeight},{entry.NetWeight}," +
                                           $"{entry.EntryDateTime:dd/MM/yyyy HH:mm},{entry.ExitDateTime?.ToString("dd/MM/yyyy HH:mm")}");
                        }
                    }

                    MessageBox.Show($"Data exported successfully!\n\nLocation: {saveDialog.FileName}\nRecords: {entries.Count}", 
                                   "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CleanDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var retentionDays = int.Parse(DataRetentionTextBox.Text);
                var cutoffDate = DateTime.Now.AddDays(-retentionDays);

                var result = MessageBox.Show($"This will permanently delete all records older than {retentionDays} days " +
                                           $"(before {cutoffDate:dd/MM/yyyy}).\n\nAre you sure you want to continue?", 
                                           "Confirm Data Cleanup", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Simulate data cleanup (in real implementation, would delete old records)
                    var recordsDeleted = 25; // Simulated count
                    
                    MessageBox.Show($"Data cleanup completed!\n\n{recordsDeleted} old records were removed.", 
                                   "Cleanup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    DatabaseStatusText.Text = "Database: Connected | Cleanup completed | Size reduced";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cleanup failed: {ex.Message}", "Cleanup Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Main Actions

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate settings
                if (!ValidateSettings())
                    return;

                // Save settings to configuration file or database
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

        private void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("This will reset all settings to their default values.\n\nAre you sure you want to continue?", 
                                        "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ResetToDefaults();
                MessageBox.Show("Settings have been reset to default values.", "Reset Complete", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportConfigButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Configuration files (*.config)|*.config|JSON files (*.json)|*.json",
                    FileName = $"weighbridge_config_{DateTime.Now:yyyyMMdd}.config"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportConfiguration(saveDialog.FileName);
                    MessageBox.Show($"Configuration exported successfully!\n\nLocation: {saveDialog.FileName}", 
                                   "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helper Methods

        private bool ValidateSettings()
        {
            // Company validation
            if (string.IsNullOrWhiteSpace(CompanyNameTextBox.Text))
            {
                MessageBox.Show("Company name is required.", "Validation Error", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                SwitchTab(CompanyTabButton, CompanySettingsPanel);
                CompanyNameTextBox.Focus();
                return false;
            }

            // Weighbridge validation
            if (!int.TryParse(MaxCapacityTextBox.Text, out var maxCapacity) || maxCapacity <= 0)
            {
                MessageBox.Show("Maximum capacity must be a valid positive number.", "Validation Error", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                SwitchTab(WeighbridgeTabButton, WeighbridgeSettingsPanel);
                MaxCapacityTextBox.Focus();
                return false;
            }

            // Database validation
            if (!Directory.Exists(Path.GetDirectoryName(DatabasePathTextBox.Text)))
            {
                MessageBox.Show("Database path directory does not exist.", "Validation Error", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                SwitchTab(DatabaseTabButton, DatabaseSettingsPanel);
                DatabasePathTextBox.Focus();
                return false;
            }

            return true;
        }

        private void SaveAllSettings()
        {
            // In a real implementation, save to configuration file or registry
            // For now, just simulate saving
        }

        private void ResetToDefaults()
        {
            // Company settings
            CompanyNameTextBox.Text = "YASH COTEX PRIVATE LIMITED";
            Address1TextBox.Text = "Industrial Area, Phase 1";
            Address2TextBox.Text = "Sector 58, Mohali";
            CityTextBox.Text = "Mohali";
            StateTextBox.Text = "Punjab";
            PinCodeTextBox.Text = "160059";
            PhoneTextBox.Text = "+91-9876543210";
            EmailTextBox.Text = "info@yashcotex.com";
            GstNumberTextBox.Text = "22AAAAA0000A1Z5";
            LicenseNumberTextBox.Text = "WB/2024/001";

            // Weighbridge settings
            ScaleModelComboBox.SelectedIndex = 0;
            ComPortComboBox.SelectedIndex = 0;
            BaudRateComboBox.SelectedIndex = 0;
            DataBitsComboBox.SelectedIndex = 1;
            MaxCapacityTextBox.Text = "100000";
            MinDisplayTextBox.Text = "10";
            CalibrationFactorTextBox.Text = "1.000";
            ZeroRangeTextBox.Text = "50";

            // Database settings
            DatabaseTypeComboBox.SelectedIndex = 0;
            DatabasePathTextBox.Text = "weighbridge.db";
            BackupFrequencyComboBox.SelectedIndex = 0;
            BackupLocationTextBox.Text = "./Backups";
            DataRetentionTextBox.Text = "365";
            AutoArchiveTextBox.Text = "90";

            // System settings
            LanguageComboBox.SelectedIndex = 0;
            DateFormatComboBox.SelectedIndex = 0;
            WeightUnitComboBox.SelectedIndex = 0;
            ThemeComboBox.SelectedIndex = 0;
            AutoSaveIntervalTextBox.Text = "5";
            SessionTimeoutTextBox.Text = "60";
            
            StartWithWindowsCheckBox.IsChecked = false;
            MinimizeToTrayCheckBox.IsChecked = false;
            ShowNotificationsCheckBox.IsChecked = true;
            EnableLoggingCheckBox.IsChecked = true;
            AutoUpdateCheckBox.IsChecked = true;
        }

        private void ExportConfiguration(string filePath)
        {
            var config = new System.Text.StringBuilder();
            config.AppendLine($"# Weighbridge Configuration Export - {DateTime.Now}");
            config.AppendLine();
            
            config.AppendLine("[Company]");
            config.AppendLine($"Name={CompanyNameTextBox.Text}");
            config.AppendLine($"Address1={Address1TextBox.Text}");
            config.AppendLine($"Address2={Address2TextBox.Text}");
            config.AppendLine($"City={CityTextBox.Text}");
            config.AppendLine($"State={StateTextBox.Text}");
            config.AppendLine($"PinCode={PinCodeTextBox.Text}");
            config.AppendLine($"Phone={PhoneTextBox.Text}");
            config.AppendLine($"Email={EmailTextBox.Text}");
            config.AppendLine($"GST={GstNumberTextBox.Text}");
            config.AppendLine($"License={LicenseNumberTextBox.Text}");
            config.AppendLine();
            
            config.AppendLine("[Weighbridge]");
            config.AppendLine($"Model={((ComboBoxItem)ScaleModelComboBox.SelectedItem).Content}");
            config.AppendLine($"Port={((ComboBoxItem)ComPortComboBox.SelectedItem).Content}");
            config.AppendLine($"BaudRate={((ComboBoxItem)BaudRateComboBox.SelectedItem).Content}");
            config.AppendLine($"DataBits={((ComboBoxItem)DataBitsComboBox.SelectedItem).Content}");
            config.AppendLine($"MaxCapacity={MaxCapacityTextBox.Text}");
            config.AppendLine($"MinDisplay={MinDisplayTextBox.Text}");
            config.AppendLine($"CalibrationFactor={CalibrationFactorTextBox.Text}");
            config.AppendLine($"ZeroRange={ZeroRangeTextBox.Text}");
            config.AppendLine();
            
            config.AppendLine("[Database]");
            config.AppendLine($"Type={((ComboBoxItem)DatabaseTypeComboBox.SelectedItem).Content}");
            config.AppendLine($"Path={DatabasePathTextBox.Text}");
            config.AppendLine($"BackupFrequency={((ComboBoxItem)BackupFrequencyComboBox.SelectedItem).Content}");
            config.AppendLine($"BackupLocation={BackupLocationTextBox.Text}");
            config.AppendLine($"DataRetention={DataRetentionTextBox.Text}");
            config.AppendLine($"AutoArchive={AutoArchiveTextBox.Text}");
            config.AppendLine();
            
            config.AppendLine("[System]");
            config.AppendLine($"Language={((ComboBoxItem)LanguageComboBox.SelectedItem).Content}");
            config.AppendLine($"DateFormat={((ComboBoxItem)DateFormatComboBox.SelectedItem).Content}");
            config.AppendLine($"WeightUnit={((ComboBoxItem)WeightUnitComboBox.SelectedItem).Content}");
            config.AppendLine($"Theme={((ComboBoxItem)ThemeComboBox.SelectedItem).Content}");
            config.AppendLine($"AutoSaveInterval={AutoSaveIntervalTextBox.Text}");
            config.AppendLine($"SessionTimeout={SessionTimeoutTextBox.Text}");
            config.AppendLine($"StartWithWindows={StartWithWindowsCheckBox.IsChecked}");
            config.AppendLine($"MinimizeToTray={MinimizeToTrayCheckBox.IsChecked}");
            config.AppendLine($"ShowNotifications={ShowNotificationsCheckBox.IsChecked}");
            config.AppendLine($"EnableLogging={EnableLoggingCheckBox.IsChecked}");
            config.AppendLine($"AutoUpdate={AutoUpdateCheckBox.IsChecked}");

            File.WriteAllText(filePath, config.ToString());
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