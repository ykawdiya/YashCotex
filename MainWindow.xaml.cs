using System;
using System.Windows;
using System.Windows.Controls;
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

        public MainWindow()
        {
            InitializeComponent();
            
            _weightService = new WeightService();
            
            InitializeDateTimeTimer();
            InitializeWeightDisplay();
            
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
            switch (e.Key)
            {
                case System.Windows.Input.Key.F1:
                    EntryButton_Clicked(this, new RoutedEventArgs());
                    break;
                case System.Windows.Input.Key.F2:
                    ExitButton_Clicked(this, new RoutedEventArgs());
                    break;
                case System.Windows.Input.Key.F3:
                    PrintButton_Clicked(this, new RoutedEventArgs());
                    break;
                case System.Windows.Input.Key.F4:
                    SettingsButton_Clicked(this, new RoutedEventArgs());
                    break;
                case System.Windows.Input.Key.F5:
                    LogoutButton_Clicked(this, new RoutedEventArgs());
                    break;
                case System.Windows.Input.Key.Escape:
                    ShowHome();
                    break;
            }
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
                
                _currentFormControl = null;
                FormContentPresenter.Content = null;
                FormContentPresenter.Visibility = Visibility.Collapsed;
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
                // Dispose current form if any
                if (_currentFormControl is EntryControl oldEntry)
                    oldEntry.Dispose();
                else if (_currentFormControl is ExitControl oldExit)
                    oldExit.Dispose();
                else if (_currentFormControl is SettingsControl oldSettings)
                    oldSettings.Dispose();
                
                var printControl = new PrintControl();
                printControl.FormCompleted += (s, message) => {
                    LatestOperation.Text = message;
                    ShowHome();
                };
                
                _currentFormControl = printControl;
                FormContentPresenter.Content = printControl;
                FormContentPresenter.Visibility = Visibility.Visible;
                LiveWeightPanel.Visibility = Visibility.Collapsed;
                
                LatestOperation.Text = "Print center opened";
            }
            catch (Exception ex)
            {
                LatestOperation.Text = $"Error opening print center: {ex.Message}";
            }
        }

        private void SettingsButton_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check permissions
                if (_authService == null || !_authService.HasPermission(UserRole.Admin))
                {
                    MessageBox.Show("Admin access required to open settings.", "Access Denied", 
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Dispose current form if any
                if (_currentFormControl is EntryControl oldEntry)
                    oldEntry.Dispose();
                else if (_currentFormControl is ExitControl oldExit)
                    oldExit.Dispose();
                else if (_currentFormControl is PrintControl oldPrint)
                    oldPrint.Dispose();
                
                var settingsControl = new SettingsControl();
                
                // Pass authentication service to settings control for role-based access
                if (settingsControl is SettingsControl settings)
                {
                    settings.SetAuthenticationService(_authService);
                }
                
                settingsControl.FormCompleted += (s, message) => {
                    LatestOperation.Text = message;
                    ShowHome();
                };
                
                _currentFormControl = settingsControl;
                FormContentPresenter.Content = settingsControl;
                FormContentPresenter.Visibility = Visibility.Visible;
                LiveWeightPanel.Visibility = Visibility.Collapsed;
                
                LatestOperation.Text = "Settings opened";
            }
            catch (Exception ex)
            {
                LatestOperation.Text = $"Error opening settings: {ex.Message}";
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
        }
    }
}