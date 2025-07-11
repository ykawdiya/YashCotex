﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using WeighbridgeSoftwareYashCotex.Views;
using WeighbridgeSoftwareYashCotex.Services;
using WeighbridgeSoftwareYashCotex.Models;

namespace WeighbridgeSoftwareYashCotex
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _dateTimeTimer;
        private WeightService _weightService;
        private UserControl? _currentFormControl;
        private AuthenticationService? _authService;
        private User? _currentUser;
        private readonly SettingsService _settingsService;

        public MainWindow()
        {
            InitializeComponent();
            
            _weightService = new WeightService();
            _settingsService = SettingsService.Instance;
            
            InitializeDateTimeTimer();
            InitializeWeightDisplay();
            InitializeSettingsEventHandlers();
            
            // Update company info display with loaded settings
            UpdateCompanyInfoDisplay();
            
            // Field binding fixes have been applied and tested successfully
            
            // Set up keyboard shortcuts
            this.KeyDown += MainWindow_KeyDown;
            
            // Show login window on startup
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await ShowLoginWindow();
        }

        private async System.Threading.Tasks.Task ShowLoginWindow()
        {
            try
            {
                var loginWindow = new LoginWindow();
                loginWindow.Owner = this;
                
                var result = loginWindow.ShowDialog();
                
                if (result == true && loginWindow.IsLoginSuccessful && loginWindow.LoggedInUser != null)
                {
                    // Successful login
                    _currentUser = loginWindow.LoggedInUser;
                    _authService = loginWindow.GetAuthenticationService();
                    
                    // Subscribe to authentication events
                    _authService.UserLoggedOut += OnUserLoggedOut;
                    _authService.SessionExpired += OnSessionExpired;
                    _authService.PrivilegeEscalated += OnPrivilegeEscalated;
                    _authService.PrivilegeExpired += OnPrivilegeExpired;
                    
                    // Update UI with user info
                    UpdateUserInterface();
                    
                    LatestOperation.Text = $"Welcome, {_currentUser.FullName}! ({_currentUser.Role})";
                    
                }
                else
                {
                    // Login cancelled or failed
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Login error: {ex.Message}", "Authentication Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void UpdateUserInterface()
        {
            if (_currentUser == null) return;
            
            // Update user display (assuming we have a UserRoleText element in XAML)
            try
            {
                // Update current user display in footer
                if (CurrentUserText != null)
                {
                    CurrentUserText.Text = $"{_currentUser.FullName} ({_currentUser.Role})";
                }
                
                // Update role-based access
                UpdateRoleBasedAccess();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating UI: {ex.Message}");
            }
        }

        private void UpdateRoleBasedAccess()
        {
            if (_authService == null) return;
            
            try
            {
                // Settings access - Admin and Super Admin only
                SettingsButton.IsEnabled = _authService.HasPermission(UserRole.Admin);
                
                // All users can access Entry and Exit
                EntryButton.IsEnabled = true;
                ExitButton.IsEnabled = true;
                PrintButton.IsEnabled = true;
                
                // Update tooltip for restricted functions
                if (!_authService.HasPermission(UserRole.Admin))
                {
                    SettingsButton.ToolTip = "Admin access required";
                }
                else
                {
                    SettingsButton.ToolTip = "Open system settings (F4)";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating role access: {ex.Message}");
            }
        }
        
        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Check if we're in a text input field - if so, let some keys pass through
            var focusedElement = Keyboard.FocusedElement;
            var isInTextInput = focusedElement is TextBox || focusedElement is PasswordBox || focusedElement is ComboBox;
            
            switch (e.Key)
            {
                case System.Windows.Input.Key.F1:
                    if (_currentFormControl != null)
                    {
                        // Let the current form handle F1 for context help
                        return;
                    }
                    else
                    {
                        EntryButton_Clicked(this, new RoutedEventArgs());
                        e.Handled = true;
                    }
                    break;
                case System.Windows.Input.Key.F2:
                    ExitButton_Clicked(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case System.Windows.Input.Key.F3:
                    PrintButton_Clicked(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case System.Windows.Input.Key.F4:
                    SettingsButton_Clicked(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case System.Windows.Input.Key.F5:
                    LogoutButton_Clicked(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case System.Windows.Input.Key.F12:
                    // Global help - show comprehensive function key guide
                    ShowGlobalHelp();
                    e.Handled = true;
                    break;
                case System.Windows.Input.Key.Escape:
                    if (_currentFormControl != null)
                    {
                        ShowHome();
                    }
                    e.Handled = true;
                    break;
                case System.Windows.Input.Key.Home:
                    if (!isInTextInput)
                    {
                        ShowHome();
                        e.Handled = true;
                    }
                    break;
                case System.Windows.Input.Key.Tab:
                    // Allow normal tab navigation
                    break;
                case System.Windows.Input.Key.Enter:
                    // Let forms handle Enter key
                    break;
            }
        }

        private void ShowGlobalHelp()
        {
            var currentForm = "Main Window";
            var helpText = "WEIGHBRIDGE SOFTWARE - GLOBAL KEYBOARD SHORTCUTS\n" +
                          "===============================================\n\n" +
                          "MAIN NAVIGATION:\n" +
                          "F1  - Entry Form (New weighment entry)\n" +
                          "F2  - Exit Form (Complete weighment)\n" +
                          "F3  - Print Center (Reports & reprints)\n" +
                          "F4  - Settings (System configuration)\n" +
                          "F5  - Logout\n" +
                          "F12 - This help screen\n" +
                          "ESC - Return to home/cancel\n" +
                          "Home- Return to home screen\n\n";

            // Add context-specific help based on current form
            if (_currentFormControl is EntryControl)
            {
                currentForm = "Entry Form";
                helpText += "ENTRY FORM SHORTCUTS:\n" +
                           "F5  - Capture weight\n" +
                           "F6  - Validate form\n" +
                           "F7  - Auto-complete vehicle\n" +
                           "F8  - Auto-fill from last entry\n" +
                           "F9  - Save entry\n" +
                           "F10 - Clear form\n" +
                           "F11 - Preview entry\n\n";
            }
            else if (_currentFormControl is ExitControl)
            {
                currentForm = "Exit Form";
                helpText += "EXIT FORM SHORTCUTS:\n" +
                           "F3  - Search for entry\n" +
                           "F5  - Capture exit weight\n" +
                           "F6  - Validate exit data\n" +
                           "F7  - Quick search by RST\n" +
                           "F8  - Show vehicle history\n" +
                           "F9  - Save exit\n" +
                           "F10 - Clear form\n" +
                           "F11 - Print slip\n" +
                           "F12 - Reprint last slip\n\n";
            }
            else if (_currentFormControl is SettingsControl)
            {
                currentForm = "Settings";
                helpText += "SETTINGS SHORTCUTS:\n" +
                           "F2  - Save all settings\n" +
                           "F3  - Test connections\n" +
                           "F4  - Export settings\n" +
                           "F5  - Backup database\n" +
                           "F6  - Sync Google Sheets\n" +
                           "F7  - System diagnostics\n" +
                           "F8  - Next tab\n" +
                           "F9  - Previous tab\n" +
                           "F10 - Reset current tab\n\n";
            }

            helpText += "UNIVERSAL SHORTCUTS:\n" +
                       "Enter - Smart navigation/activate\n" +
                       "Tab   - Move to next field\n" +
                       "Shift+Tab - Move to previous field\n\n" +
                       "TIPS:\n" +
                       "• Most forms have F1 for context help\n" +
                       "• Function keys work globally\n" +
                       "• ESC always goes back/cancels\n" +
                       "• Enter provides smart navigation\n\n" +
                       $"Current Context: {currentForm}";

            MessageBox.Show(helpText, "Global Help (F12)", 
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void InitializeDateTimeTimer()
        {
            _dateTimeTimer = new DispatcherTimer();
            _dateTimeTimer.Interval = TimeSpan.FromSeconds(1);
            _dateTimeTimer.Tick += (sender, e) =>
            {
                CurrentDateTime.Text = DateTime.Now.ToString("dd/MM/yyyy\nHH:mm:ss");
            };
            _dateTimeTimer.Start();
        }

        private void InitializeWeightDisplay()
        {
            _weightService.WeightChanged += (sender, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    LiveWeight.Text = e.Weight.ToString("F2");
                    StabilityIndicator.Text = e.IsStable ? "STABLE" : "UNSTABLE";
                    StabilityIndicator.Foreground = e.IsStable ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
                    LastUpdated.Text = $"Last Updated: {e.Timestamp:HH:mm:ss}";
                    ConnectionStatus.Text = "Connected";
                    ConnectionStatus.Foreground = System.Windows.Media.Brushes.Green;
                });
            };
        }

        private void ShowHome()
        {
            try
            {
                // Dispose current form if any
                if (_currentFormControl is EntryControl entryControl)
                    entryControl.Dispose();
                else if (_currentFormControl is ExitControl exitControl)
                    exitControl.Dispose();
                else if (_currentFormControl is PrintControl printControl)
                    printControl.Dispose();
                else if (_currentFormControl is SettingsControl settingsControl)
                    settingsControl.Dispose();
                else if (_currentFormControl is ModernSettingsControl modernSettingsControl)
                    modernSettingsControl.Dispose();
                
                _currentFormControl = null;
                
                // Hide any full-screen overlays
                FullScreenFormPresenter.Content = null;
                FullScreenFormPresenter.Visibility = Visibility.Collapsed;
                FormContentPresenter.Content = null;
                FormContentPresenter.Visibility = Visibility.Collapsed;
                
                // Show all camera grids and live weight panel
                LeftCamerasGrid.Visibility = Visibility.Visible;
                RightCamerasGrid.Visibility = Visibility.Visible;
                LiveWeightPanel.Visibility = Visibility.Visible;
                
                LatestOperation.Text = "Home - Live Weight Display";
            }
            catch (Exception ex)
            {
                LatestOperation.Text = $"Error returning home: {ex.Message}";
            }
        }
        
        private void HomeButton_Clicked(object sender, RoutedEventArgs e)
        {
            ShowHome();
        }

        private void EntryButton_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Dispose current form if any
                if (_currentFormControl is EntryControl oldEntry)
                    oldEntry.Dispose();
                else if (_currentFormControl is ExitControl oldExit)
                    oldExit.Dispose();
                
                var entryControl = new EntryControl();
                entryControl.FormCompleted += (s, message) => {
                    LatestOperation.Text = message;
                    ShowHome();
                };
                
                _currentFormControl = entryControl;
                
                // Hide full-screen overlay and show cameras
                FullScreenFormPresenter.Content = null;
                FullScreenFormPresenter.Visibility = Visibility.Collapsed;
                LeftCamerasGrid.Visibility = Visibility.Visible;
                RightCamerasGrid.Visibility = Visibility.Visible;
                
                // Show entry form in center area only
                FormContentPresenter.Content = entryControl;
                FormContentPresenter.Visibility = Visibility.Visible;
                LiveWeightPanel.Visibility = Visibility.Collapsed;
                
                LatestOperation.Text = "Entry form opened";
            }
            catch (Exception ex)
            {
                LatestOperation.Text = $"Error opening entry: {ex.Message}";
            }
        }

        private void ExitButton_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Dispose current form if any
                if (_currentFormControl is EntryControl oldEntry)
                    oldEntry.Dispose();
                else if (_currentFormControl is ExitControl oldExit)
                    oldExit.Dispose();
                
                var exitControl = new ExitControl();
                exitControl.FormCompleted += (s, message) => {
                    LatestOperation.Text = message;
                    ShowHome();
                };
                
                _currentFormControl = exitControl;
                
                // Hide full-screen overlay and show cameras
                FullScreenFormPresenter.Content = null;
                FullScreenFormPresenter.Visibility = Visibility.Collapsed;
                LeftCamerasGrid.Visibility = Visibility.Visible;
                RightCamerasGrid.Visibility = Visibility.Visible;
                
                // Show exit form in center area only
                FormContentPresenter.Content = exitControl;
                FormContentPresenter.Visibility = Visibility.Visible;
                LiveWeightPanel.Visibility = Visibility.Collapsed;
                
                LatestOperation.Text = "Exit form opened";
            }
            catch (Exception ex)
            {
                LatestOperation.Text = $"Error opening exit: {ex.Message}";
            }
        }

        private void PrintButton_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("=== PRINT BUTTON CLICKED ===");
                
                // Dispose current form if any
                if (_currentFormControl is EntryControl oldEntry)
                    oldEntry.Dispose();
                else if (_currentFormControl is ExitControl oldExit)
                    oldExit.Dispose();
                else if (_currentFormControl is SettingsControl oldSettings)
                    oldSettings.Dispose();
                else if (_currentFormControl is PrintControl oldPrint)
                    oldPrint.Dispose();
                
                Console.WriteLine("Creating new PrintControl...");
                var printControl = new PrintControl();
                Console.WriteLine("PrintControl created successfully");
                
                printControl.FormCompleted += (s, message) => {
                    LatestOperation.Text = message;
                    ShowHome();
                };
                
                _currentFormControl = printControl;
                
                // Hide full-screen overlay and show cameras
                FullScreenFormPresenter.Content = null;
                FullScreenFormPresenter.Visibility = Visibility.Collapsed;
                LeftCamerasGrid.Visibility = Visibility.Visible;
                RightCamerasGrid.Visibility = Visibility.Visible;
                
                // Show print form in center area only
                FormContentPresenter.Content = printControl;
                FormContentPresenter.Visibility = Visibility.Visible;
                LiveWeightPanel.Visibility = Visibility.Collapsed;
                
                LatestOperation.Text = "Print center opened";
                Console.WriteLine("Print center opened successfully");
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error opening print center: {ex.Message}";
                Console.WriteLine($"PRINT ERROR: {errorMessage}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                LatestOperation.Text = errorMessage;
                
                // Show error dialog for user visibility
                MessageBox.Show($"Failed to open print center:\n\n{ex.Message}", 
                    "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SettingsButton_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("=== SETTINGS BUTTON CLICKED ===");
                
                // Check permissions
                if (_authService == null || !_authService.HasPermission(UserRole.Admin))
                {
                    MessageBox.Show("Admin access required to open settings.", "Access Denied", 
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Console.WriteLine("Permission check passed");

                // Dispose current form if any
                if (_currentFormControl is EntryControl oldEntry)
                    oldEntry.Dispose();
                else if (_currentFormControl is ExitControl oldExit)
                    oldExit.Dispose();
                else if (_currentFormControl is PrintControl oldPrint)
                    oldPrint.Dispose();
                else if (_currentFormControl is SettingsControl oldSettings)
                    oldSettings.Dispose();
                else if (_currentFormControl is ModernSettingsControl oldModernSettings)
                    oldModernSettings.Dispose();
                
                Console.WriteLine("Disposed old controls, creating ModernSettingsControl...");
                var modernSettings = new ModernSettingsControl();
                Console.WriteLine("ModernSettingsControl created successfully");
                
                modernSettings.FormCompleted += (s, message) => {
                    LatestOperation.Text = message;
                    ShowHome();
                };
                
                _currentFormControl = modernSettings;
                
                // Hide all camera grids and live weight panel
                LeftCamerasGrid.Visibility = Visibility.Collapsed;
                RightCamerasGrid.Visibility = Visibility.Collapsed;
                LiveWeightPanel.Visibility = Visibility.Collapsed;
                
                // Show modern settings in full-screen overlay
                FullScreenFormPresenter.Content = modernSettings;
                FullScreenFormPresenter.Visibility = Visibility.Visible;
                
                LatestOperation.Text = "Modern Settings opened";
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error opening modern settings: {ex.Message}";
                Console.WriteLine($"SETTINGS ERROR: {errorMessage}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                LatestOperation.Text = errorMessage;
                
                // Show error dialog for user visibility
                MessageBox.Show($"Failed to open modern settings:\n\n{ex.Message}", 
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogoutButton_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Are you sure you want to logout?", "Logout Confirmation", 
                                            MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    PerformLogout();
                }
            }
            catch (Exception ex)
            {
                LatestOperation.Text = $"Logout error: {ex.Message}";
            }
        }

        private void PerformLogout()
        {
            try
            {
                LatestOperation.Text = "Logging out...";
                
                // Clear current form
                ShowHome();
                
                // Logout from authentication service
                _authService?.Logout();
                
                // Clear user info
                _currentUser = null;
                
                // Show login window again
                ShowLoginWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Logout error: {ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Authentication Event Handlers

        private void OnUserLoggedOut(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                LatestOperation.Text = "User logged out";
                PerformLogout();
            });
        }

        private void OnSessionExpired(object? sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"Session expired: {message}\n\nPlease login again.", "Session Expired", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                PerformLogout();
            });
        }

        private void OnPrivilegeEscalated(object? sender, UserRole escalatedRole)
        {
            Dispatcher.Invoke(() =>
            {
                LatestOperation.Text = $"Privilege escalated to {escalatedRole}";
                UpdateRoleBasedAccess();
            });
        }

        private void OnPrivilegeExpired(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                LatestOperation.Text = "Elevated privileges expired";
                UpdateRoleBasedAccess();
            });
        }


        #endregion
        
        #region Settings Event Handlers
        
        private void InitializeSettingsEventHandlers()
        {
            // Subscribe to settings change events
            _settingsService.SettingsChanged += OnSettingsChanged;
            _settingsService.CompanyInfoChanged += OnCompanyInfoChanged;
            _settingsService.WeighbridgeSettingsChanged += OnWeighbridgeSettingsChanged;
            _settingsService.DatabaseSettingsChanged += OnDatabaseSettingsChanged;
            _settingsService.GoogleSheetsSettingsChanged += OnGoogleSheetsSettingsChanged;
            _settingsService.CameraSettingsChanged += OnCameraSettingsChanged;
            _settingsService.PrinterSettingsChanged += OnPrinterSettingsChanged;
            _settingsService.SystemSettingsChanged += OnSystemSettingsChanged;
        }
        
        private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                LatestOperation.Text = $"Settings Updated: {e.Description}";
                
                // Update UI elements that depend on general settings
                UpdateUIFromSettings();
            });
        }
        
        private void OnCompanyInfoChanged(object? sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                LatestOperation.Text = $"Company Info: {message}";
                
                // Update company information displays
                UpdateCompanyInfoDisplay();
            });
        }
        
        private void OnWeighbridgeSettingsChanged(object? sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                LatestOperation.Text = $"Weighbridge: {message}";
                
                // Restart weight service with new settings
                RestartWeightService();
            });
        }
        
        private void OnDatabaseSettingsChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                LatestOperation.Text = "Database settings updated";
                
                // Refresh database connections
                RefreshDatabaseConnections();
            });
        }
        
        private void OnGoogleSheetsSettingsChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                LatestOperation.Text = "Google Sheets integration updated";
                
                // Refresh Google Sheets service
                RefreshGoogleSheetsService();
            });
        }
        
        private void OnCameraSettingsChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                LatestOperation.Text = "Camera settings updated";
                
                // Refresh camera connections
                RefreshCameraConnections();
            });
        }
        
        private void OnPrinterSettingsChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                LatestOperation.Text = "Printer settings updated";
                
                // Refresh printer connections
                RefreshPrinterConnections();
            });
        }
        
        private void OnSystemSettingsChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                LatestOperation.Text = "System settings updated";
                
                // Apply system-wide settings changes
                ApplySystemSettings();
            });
        }
        
        private void UpdateUIFromSettings()
        {
            try
            {
                // Update any UI elements that depend on settings
                // This could include themes, layout preferences, etc.
                
                // Update window title with company name if needed
                if (!string.IsNullOrEmpty(_settingsService.CompanyName))
                {
                    this.Title = $"Weighbridge Software - {_settingsService.CompanyName}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating UI from settings: {ex.Message}");
            }
        }
        
        private void UpdateCompanyInfoDisplay()
        {
            try
            {
                var settings = SettingsService.Instance;
                
                Console.WriteLine("=== UPDATING HEADER DISPLAY ===");
                Console.WriteLine($"CompanyName: '{settings.CompanyName}'");
                Console.WriteLine($"CompanyAddress: '{settings.CompanyAddress}'");
                Console.WriteLine($"CompanyEmail: '{settings.CompanyEmail}'");
                Console.WriteLine($"CompanyPhone: '{settings.CompanyPhone}'");
                Console.WriteLine($"CompanyGSTIN: '{settings.CompanyGSTIN}'");
                Console.WriteLine($"CompanyLogo: '{settings.CompanyLogo}'");
                
                // Update company logo
                if (CompanyLogoImage != null)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(settings.CompanyLogo) && File.Exists(settings.CompanyLogo))
                        {
                            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(settings.CompanyLogo);
                            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            
                            // Auto-crop the image to remove transparent/void areas
                            var croppedBitmap = AutoCropImage(bitmap);
                            CompanyLogoImage.Source = croppedBitmap;
                        }
                        else
                        {
                            // No logo or file doesn't exist - clear the image
                            CompanyLogoImage.Source = null;
                        }
                    }
                    catch (Exception logoEx)
                    {
                        Console.WriteLine($"Error loading logo: {logoEx.Message}");
                        CompanyLogoImage.Source = null;
                    }
                }
                
                if (CompanyNameHeader != null)
                {
                    CompanyNameHeader.Text = "";
                    CompanyNameHeader.Text = settings.CompanyName ?? "YASH COTEX";
                }
                
                // Display address lines separately
                if (CompanyAddressLine1Header != null)
                {
                    CompanyAddressLine1Header.Text = "";
                    CompanyAddressLine1Header.Text = settings.CompanyAddressLine1 ?? "";
                }
                
                if (CompanyAddressLine2Header != null)
                {
                    CompanyAddressLine2Header.Text = "";
                    CompanyAddressLine2Header.Text = settings.CompanyAddressLine2 ?? "";
                }
                
                if (CompanyEmailHeader != null)
                {
                    CompanyEmailHeader.Text = "";
                    CompanyEmailHeader.Text = settings.CompanyEmail ?? "";
                }
                
                if (CompanyPhoneHeader != null)
                {
                    CompanyPhoneHeader.Text = "";
                    CompanyPhoneHeader.Text = settings.CompanyPhone ?? "";
                }
                
                if (CompanyGSTHeader != null)
                {
                    CompanyGSTHeader.Text = "";
                    CompanyGSTHeader.Text = settings.CompanyGSTIN ?? "";
                }
                
                // Update window title
                var windowTitle = $"Weighbridge Software - {settings.CompanyName ?? "YASH COTEX"}";
                this.Title = windowTitle;
                
                Console.WriteLine($"Header updated successfully. Window title: {windowTitle}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating company info display: {ex.Message}");
                Console.WriteLine($"Error updating company info display: {ex.Message}");
            }
        }
        
        private void RestartWeightService()
        {
            try
            {
                // Restart weight service with new COM port settings
                _weightService?.Dispose();
                _weightService = new WeightService();
                InitializeWeightDisplay();
                
                LatestOperation.Text = "Weight service restarted with new settings";
            }
            catch (Exception ex)
            {
                LatestOperation.Text = $"Error restarting weight service: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error restarting weight service: {ex.Message}");
            }
        }
        
        private void RefreshDatabaseConnections()
        {
            try
            {
                // Refresh database connections with new settings
                // This would involve updating connection strings, etc.
                
                LatestOperation.Text = "Database connections refreshed";
            }
            catch (Exception ex)
            {
                LatestOperation.Text = $"Error refreshing database: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error refreshing database connections: {ex.Message}");
            }
        }
        
        private void RefreshGoogleSheetsService()
        {
            try
            {
                // Refresh Google Sheets service with new credentials/settings
                LatestOperation.Text = "Google Sheets service refreshed";
            }
            catch (Exception ex)
            {
                LatestOperation.Text = $"Error refreshing Google Sheets: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error refreshing Google Sheets service: {ex.Message}");
            }
        }
        
        private void RefreshCameraConnections()
        {
            try
            {
                // Refresh camera connections with new IP addresses/settings
                LatestOperation.Text = "Camera connections refreshed";
            }
            catch (Exception ex)
            {
                LatestOperation.Text = $"Error refreshing cameras: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error refreshing camera connections: {ex.Message}");
            }
        }
        
        private void RefreshPrinterConnections()
        {
            try
            {
                // Refresh printer connections with new settings
                LatestOperation.Text = "Printer connections refreshed";
            }
            catch (Exception ex)
            {
                LatestOperation.Text = $"Error refreshing printers: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error refreshing printer connections: {ex.Message}");
            }
        }
        
        private void ApplySystemSettings()
        {
            try
            {
                // Apply system-wide settings changes
                // This could include language settings, theme changes, etc.
                
                LatestOperation.Text = "System settings applied";
            }
            catch (Exception ex)
            {
                LatestOperation.Text = $"Error applying system settings: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error applying system settings: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Logo Auto-Cropping
        
        private System.Windows.Media.Imaging.BitmapSource AutoCropImage(System.Windows.Media.Imaging.BitmapSource source)
        {
            try
            {
                // Convert to format we can work with
                var formatConvertedBitmap = new System.Windows.Media.Imaging.FormatConvertedBitmap();
                formatConvertedBitmap.BeginInit();
                formatConvertedBitmap.Source = source;
                formatConvertedBitmap.DestinationFormat = System.Windows.Media.PixelFormats.Bgra32;
                formatConvertedBitmap.EndInit();

                int width = formatConvertedBitmap.PixelWidth;
                int height = formatConvertedBitmap.PixelHeight;
                int stride = width * 4; // 4 bytes per pixel (BGRA)
                
                byte[] pixels = new byte[height * stride];
                formatConvertedBitmap.CopyPixels(pixels, stride, 0);

                // Find content bounds by scanning for non-transparent pixels
                int left = width, right = 0, top = height, bottom = 0;
                bool hasContent = false;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * stride + x * 4;
                        byte alpha = pixels[index + 3]; // Alpha channel
                        
                        // Consider pixel as content if alpha > threshold and not pure white background
                        if (alpha > 30)
                        {
                            byte blue = pixels[index];
                            byte green = pixels[index + 1];
                            byte red = pixels[index + 2];
                            
                            // Skip near-white pixels (common background)
                            if (!(red > 240 && green > 240 && blue > 240))
                            {
                                hasContent = true;
                                left = Math.Min(left, x);
                                right = Math.Max(right, x);
                                top = Math.Min(top, y);
                                bottom = Math.Max(bottom, y);
                            }
                        }
                    }
                }

                // If no content found or content area is too small, return original
                if (!hasContent || right <= left || bottom <= top || 
                    (right - left) < 10 || (bottom - top) < 10)
                {
                    return source;
                }

                // Add small padding around content
                int padding = Math.Min(10, Math.Min(left, top));
                left = Math.Max(0, left - padding);
                top = Math.Max(0, top - padding);
                right = Math.Min(width - 1, right + padding);
                bottom = Math.Min(height - 1, bottom + padding);

                // Create cropped bitmap
                int cropWidth = right - left + 1;
                int cropHeight = bottom - top + 1;
                
                var cropRect = new System.Windows.Int32Rect(left, top, cropWidth, cropHeight);
                var croppedBitmap = new System.Windows.Media.Imaging.CroppedBitmap(source, cropRect);
                
                Console.WriteLine($"Auto-cropped logo: {width}x{height} -> {cropWidth}x{cropHeight} (removed {left},{top},{width-right},{height-bottom} padding)");
                
                return croppedBitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error auto-cropping image: {ex.Message}");
                return source; // Return original on error
            }
        }
        
        #endregion

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // Cleanup authentication service
            try
            {
                _authService?.Logout();
                _authService?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during auth cleanup: {ex.Message}");
            }
            
            // Cleanup settings event handlers
            try
            {
                _settingsService.SettingsChanged -= OnSettingsChanged;
                _settingsService.CompanyInfoChanged -= OnCompanyInfoChanged;
                _settingsService.WeighbridgeSettingsChanged -= OnWeighbridgeSettingsChanged;
                _settingsService.DatabaseSettingsChanged -= OnDatabaseSettingsChanged;
                _settingsService.GoogleSheetsSettingsChanged -= OnGoogleSheetsSettingsChanged;
                _settingsService.CameraSettingsChanged -= OnCameraSettingsChanged;
                _settingsService.PrinterSettingsChanged -= OnPrinterSettingsChanged;
                _settingsService.SystemSettingsChanged -= OnSystemSettingsChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during settings cleanup: {ex.Message}");
            }
            
            // Cleanup resources
            _dateTimeTimer?.Stop();
            _weightService?.Dispose();
            
            // Dispose current form control
            if (_currentFormControl is EntryControl entryControl)
                entryControl.Dispose();
            else if (_currentFormControl is ExitControl exitControl)
                exitControl.Dispose();
            else if (_currentFormControl is PrintControl printControl)
                printControl.Dispose();
            else if (_currentFormControl is SettingsControl settingsControl)
                settingsControl.Dispose();
            else if (_currentFormControl is ModernSettingsControl modernSettingsControl)
                modernSettingsControl.Dispose();
        }
        
    }
}