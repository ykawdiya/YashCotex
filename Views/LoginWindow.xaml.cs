using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WeighbridgeSoftwareYashCotex.Models;
using WeighbridgeSoftwareYashCotex.Services;

namespace WeighbridgeSoftwareYashCotex.Views
{
    public partial class LoginWindow : Window
    {
        private readonly AuthenticationService _authService;

        public User? LoggedInUser { get; private set; }
        public bool IsLoginSuccessful { get; private set; } = false;

        public LoginWindow()
        {
            InitializeComponent();
            _authService = new AuthenticationService();
            
            this.KeyDown += LoginWindow_KeyDown;
            UsernameTextBox.KeyDown += Field_KeyDown;
            PasswordBox.KeyDown += Field_KeyDown;
            
            // Focus username field on load
            this.Loaded += (s, e) => UsernameTextBox.Focus();
        }

        private void LoginWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    if (LoginButton.IsEnabled)
                        LoginButton_Click(this, new RoutedEventArgs());
                    break;
                case Key.Escape:
                    CloseButton_Click(this, new RoutedEventArgs());
                    break;
                case Key.F1:
                    SetAdminCredentials(this, new RoutedEventArgs());
                    break;
                case Key.F2:
                    SetManagerCredentials(this, new RoutedEventArgs());
                    break;
                case Key.F3:
                    SetUserCredentials(this, new RoutedEventArgs());
                    break;
            }
        }

        private void Field_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender == UsernameTextBox)
                {
                    PasswordBox.Focus();
                }
                else if (sender == PasswordBox)
                {
                    LoginButton_Click(this, new RoutedEventArgs());
                }
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Disable login button and show loading state
                LoginButton.IsEnabled = false;
                LoginButton.Content = "🔄 SIGNING IN...";
                StatusTextBlock.Visibility = Visibility.Collapsed;

                // Validate input
                if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
                {
                    ShowStatus("Please enter username", true);
                    return;
                }

                if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    ShowStatus("Please enter password", true);
                    return;
                }


                // Create login request
                var loginRequest = new LoginRequest
                {
                    Username = UsernameTextBox.Text.Trim(),
                    Password = PasswordBox.Password,
                    RememberMe = RememberMeCheckBox.IsChecked == true
                };

                // Attempt login
                var result = await _authService.LoginAsync(loginRequest);

                if (result.IsSuccess)
                {
                    // Successful login
                    LoggedInUser = result.User;
                    IsLoginSuccessful = true;
                    
                    ShowStatus($"Welcome, {result.User?.FullName}!", false);
                    
                    // Brief delay to show success message
                    await Task.Delay(1000);
                    
                    DialogResult = true;
                    Close();
                }
                else if (result.IsLockedOut)
                {
                    // Account locked
                    ShowStatus(result.Message, true);
                    
                    if (result.LockoutDuration.HasValue)
                    {
                        // Start countdown timer for lockout
                        StartLockoutTimer(result.LockoutDuration.Value);
                    }
                }
                else
                {
                    // Login failed
                    ShowStatus(result.Message, true);
                    
                    // Clear password field on failed login
                    PasswordBox.Clear();

                    // Focus back to password field for regular login failures
                    PasswordBox.Focus();
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Login error: {ex.Message}", true);
            }
            finally
            {
                // Re-enable login button
                LoginButton.IsEnabled = true;
                if (LoginButton.Content.ToString()?.Contains("SIGNING IN") == true)
                {
                    LoginButton.Content = "🔐 SIGN IN";
                }
            }
        }

        private void StartLockoutTimer(TimeSpan lockoutDuration)
        {
            var timer = new System.Windows.Threading.DispatcherTimer();
            var endTime = DateTime.Now.Add(lockoutDuration);
            
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) =>
            {
                var remaining = endTime - DateTime.Now;
                if (remaining <= TimeSpan.Zero)
                {
                    timer.Stop();
                    LoginButton.IsEnabled = true;
                    StatusTextBlock.Visibility = Visibility.Collapsed;
                }
                else
                {
                    LoginButton.IsEnabled = false;
                    ShowStatus($"Account locked. Try again in {remaining.Minutes:D2}:{remaining.Seconds:D2}", true);
                }
            };
            
            timer.Start();
        }

        private void ShowStatus(string message, bool isError)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = isError ? 
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60)) : 
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 174, 96));
            StatusTextBlock.Visibility = Visibility.Visible;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            IsLoginSuccessful = false;
            DialogResult = false;
            Close();
        }


        #region Quick Login Buttons

        private void SetAdminCredentials(object sender, RoutedEventArgs e)
        {
            UsernameTextBox.Text = "admin";
            PasswordBox.Password = "password123";
            PasswordBox.Focus();
        }

        private void SetManagerCredentials(object sender, RoutedEventArgs e)
        {
            UsernameTextBox.Text = "manager";
            PasswordBox.Password = "password123";
            
            PasswordBox.Focus();
        }

        private void SetUserCredentials(object sender, RoutedEventArgs e)
        {
            UsernameTextBox.Text = "operator1";
            PasswordBox.Password = "password123";
            
            PasswordBox.Focus();
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Clean up authentication service if login was not successful
                if (!IsLoginSuccessful)
                {
                    _authService?.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during login window cleanup: {ex.Message}");
            }
            
            base.OnClosed(e);
        }

        public AuthenticationService GetAuthenticationService()
        {
            return _authService;
        }
    }
}