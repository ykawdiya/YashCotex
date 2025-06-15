using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WeighbridgeSoftwareYashCotex.Services;

namespace WeighbridgeSoftwareYashCotex.Views
{
    public partial class TwoFactorSetupWindow : Window
    {
        private readonly TwoFactorAuthService _twoFactorService;
        private string _currentSecretKey = string.Empty;
        private TwoFactorMethod _selectedMethod;
        private List<string> _backupCodes = new();
        private string _username;

        public bool SetupCompleted { get; private set; }
        public TwoFactorMethod EnabledMethod { get; private set; }

        public TwoFactorSetupWindow(string username)
        {
            InitializeComponent();
            _twoFactorService = new TwoFactorAuthService();
            _username = username;
            
            this.KeyDown += TwoFactorSetupWindow_KeyDown;
        }

        private void TwoFactorSetupWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    CancelButton_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case Key.Enter:
                    if (VerifyTOTPButton.IsEnabled && VerificationCodeTextBox.IsFocused)
                        VerifyTOTPButton_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;
            }
        }

        #region Method Selection

        private void SelectTOTPButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedMethod = TwoFactorMethod.TOTP;
            SetupTOTP();
        }

        private void SelectEmailButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EmailAddressTextBox.Text))
            {
                MessageBox.Show("Please enter a valid email address.", "Email Required", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailAddressTextBox.Focus();
                return;
            }

            if (!IsValidEmail(EmailAddressTextBox.Text))
            {
                MessageBox.Show("Please enter a valid email address format.", "Invalid Email", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailAddressTextBox.Focus();
                return;
            }

            _selectedMethod = TwoFactorMethod.Email;
            SetupEmail();
        }

        private void SelectSMSButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PhoneNumberTextBox.Text))
            {
                MessageBox.Show("Please enter a valid phone number.", "Phone Number Required", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PhoneNumberTextBox.Focus();
                return;
            }

            _selectedMethod = TwoFactorMethod.SMS;
            SetupSMS();
        }

        #endregion

        #region TOTP Setup

        private void SetupTOTP()
        {
            try
            {
                _currentSecretKey = _twoFactorService.GenerateSecretKey();
                SecretKeyTextBox.Text = FormatSecretKey(_currentSecretKey);
                
                // Generate QR code URL for display
                var qrUrl = _twoFactorService.GenerateQrCodeUrl(_username, _currentSecretKey);
                QRCodePlaceholder.Text = "📱 QR Code Generated\n\nScan with your\nauthenticator app\n\n(or use secret key below)";
                
                TOTPSetupTab.Visibility = Visibility.Visible;
                var tabControl = (TabControl)TOTPSetupTab.Parent;
                tabControl.SelectedItem = TOTPSetupTab;
                
                StatusText.Text = "Scan QR code or enter secret key in your authenticator app";
                VerificationCodeTextBox.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting up TOTP: {ex.Message}", "Setup Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopySecretButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(_currentSecretKey);
                StatusText.Text = "Secret key copied to clipboard";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying to clipboard: {ex.Message}", "Copy Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void VerifyTOTPButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var code = VerificationCodeTextBox.Text.Trim();
                
                if (string.IsNullOrEmpty(code) || code.Length != 6)
                {
                    MessageBox.Show("Please enter a 6-digit verification code.", "Invalid Code", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    VerificationCodeTextBox.Focus();
                    return;
                }

                if (!code.All(char.IsDigit))
                {
                    MessageBox.Show("Verification code must contain only numbers.", "Invalid Code", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    VerificationCodeTextBox.Focus();
                    return;
                }

                bool isValid = _twoFactorService.ValidateTOTPCode(_currentSecretKey, code);
                
                if (isValid)
                {
                    EnabledMethod = TwoFactorMethod.TOTP;
                    GenerateBackupCodes();
                    StatusText.Text = "TOTP verification successful! Generating backup codes...";
                }
                else
                {
                    MessageBox.Show("Invalid verification code. Please check your authenticator app and try again.", 
                        "Verification Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    VerificationCodeTextBox.Clear();
                    VerificationCodeTextBox.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error verifying TOTP code: {ex.Message}", "Verification Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Email/SMS Setup

        private void SetupEmail()
        {
            try
            {
                // In a real implementation, you would send a test email here
                var result = MessageBox.Show(
                    $"Email 2FA will be configured for: {EmailAddressTextBox.Text}\n\n" +
                    "Verification codes will be sent to this email address when logging in.\n\n" +
                    "Confirm setup?",
                    "Confirm Email 2FA Setup",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    EnabledMethod = TwoFactorMethod.Email;
                    GenerateBackupCodes();
                    StatusText.Text = "Email 2FA configured successfully! Generating backup codes...";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting up email 2FA: {ex.Message}", "Setup Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetupSMS()
        {
            try
            {
                // In a real implementation, you would send a test SMS here
                var result = MessageBox.Show(
                    $"SMS 2FA will be configured for: {PhoneNumberTextBox.Text}\n\n" +
                    "Verification codes will be sent to this phone number when logging in.\n\n" +
                    "Confirm setup?",
                    "Confirm SMS 2FA Setup",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    EnabledMethod = TwoFactorMethod.SMS;
                    GenerateBackupCodes();
                    StatusText.Text = "SMS 2FA configured successfully! Generating backup codes...";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting up SMS 2FA: {ex.Message}", "Setup Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Backup Codes

        private void GenerateBackupCodes()
        {
            try
            {
                _backupCodes = _twoFactorService.GenerateBackupCodes(10);
                DisplayBackupCodes();
                
                BackupCodesTab.Visibility = Visibility.Visible;
                var tabControl = (TabControl)BackupCodesTab.Parent;
                tabControl.SelectedItem = BackupCodesTab;
                
                FinishButton.IsEnabled = true;
                StatusText.Text = "2FA setup complete! Save your backup codes securely.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating backup codes: {ex.Message}", "Backup Codes Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayBackupCodes()
        {
            BackupCodesPanel.Children.Clear();
            
            for (int i = 0; i < _backupCodes.Count; i++)
            {
                var border = new Border
                {
                    Background = System.Windows.Media.Brushes.White,
                    BorderBrush = System.Windows.Media.Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new System.Windows.CornerRadius(3),
                    Padding = new Thickness(10, 5, 10, 5),
                    Margin = new Thickness(0, 2, 0, 2)
                };

                var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
                
                var numberBlock = new TextBlock
                {
                    Text = $"{i + 1:D2}:",
                    FontWeight = FontWeights.Bold,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    Width = 30,
                    Foreground = System.Windows.Media.Brushes.Gray
                };

                var codeBlock = new TextBlock
                {
                    Text = _backupCodes[i],
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(10, 0, 0, 0)
                };

                stackPanel.Children.Add(numberBlock);
                stackPanel.Children.Add(codeBlock);
                border.Child = stackPanel;
                
                BackupCodesPanel.Children.Add(border);
            }
        }

        private void CopyBackupCodesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var codesText = string.Join("\n", _backupCodes.Select((code, index) => $"{index + 1:D2}: {code}"));
                var fullText = $"Two-Factor Authentication Backup Codes\nUser: {_username}\nGenerated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n{codesText}\n\nIMPORTANT: Each code can only be used once. Store securely.";
                
                Clipboard.SetText(fullText);
                StatusText.Text = "All backup codes copied to clipboard";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying backup codes: {ex.Message}", "Copy Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void GenerateNewCodesButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will generate new backup codes and invalidate the current ones.\n\n" +
                "Are you sure you want to continue?",
                "Generate New Backup Codes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                GenerateBackupCodes();
                StatusText.Text = "New backup codes generated successfully";
            }
        }

        private void PrintCodesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("Print functionality would be implemented here.\n\n" +
                    "For now, please copy the codes and print them manually.",
                    "Print Codes", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing codes: {ex.Message}", "Print Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Window Controls

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to cancel 2FA setup?\n\n" +
                "Any progress will be lost.",
                "Cancel Setup",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SetupCompleted = false;
                this.Close();
            }
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Save 2FA configuration
                var success = _twoFactorService.EnableTwoFactorAsync(_username, EnabledMethod, _currentSecretKey).Result;
                
                if (success)
                {
                    SetupCompleted = true;
                    MessageBox.Show(
                        "Two-factor authentication has been successfully enabled!\n\n" +
                        "You will need to provide a verification code on your next login.",
                        "2FA Setup Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    MessageBox.Show(
                        "Failed to save 2FA configuration. Please try again.",
                        "Setup Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error completing 2FA setup: {ex.Message}", "Setup Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helper Methods

        private string FormatSecretKey(string secretKey)
        {
            // Format secret key in groups of 4 for better readability
            var formatted = "";
            for (int i = 0; i < secretKey.Length; i += 4)
            {
                if (i > 0) formatted += " ";
                formatted += secretKey.Substring(i, Math.Min(4, secretKey.Length - i));
            }
            return formatted;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void ShowHelp()
        {
            MessageBox.Show(
                "Two-Factor Authentication Setup Help:\n\n" +
                "• Choose an authentication method that works best for you\n" +
                "• TOTP (Authenticator App) is the most secure option\n" +
                "• Save backup codes in a secure location\n" +
                "• Each backup code can only be used once\n\n" +
                "Keyboard Shortcuts:\n" +
                "• F1: Show this help\n" +
                "• Enter: Verify TOTP code (when focused)\n" +
                "• Escape: Cancel setup",
                "2FA Setup Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        protected override void OnClosed(EventArgs e)
        {
            _twoFactorService?.Dispose();
            base.OnClosed(e);
        }

        #endregion
    }
}