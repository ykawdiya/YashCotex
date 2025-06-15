using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using WeighbridgeSoftwareYashCotex.Services;

namespace WeighbridgeSoftwareYashCotex.Views
{
    public partial class TwoFactorVerificationWindow : Window
    {
        private readonly TwoFactorAuthService _twoFactorService;
        private readonly string _username;
        private readonly string _challengeId;
        private readonly TwoFactorMethod _primaryMethod;
        private string _secretKey = string.Empty;
        private DispatcherTimer? _resendTimer;
        private int _resendCountdown = 0;

        public bool VerificationSuccessful { get; private set; }
        public string VerifiedUsername { get; private set; } = string.Empty;

        public TwoFactorVerificationWindow(string username, string challengeId, TwoFactorMethod method, string secretKey = "")
        {
            InitializeComponent();
            
            _twoFactorService = new TwoFactorAuthService();
            _username = username;
            _challengeId = challengeId;
            _primaryMethod = method;
            _secretKey = secretKey;

            SetupUI();
            this.KeyDown += TwoFactorVerificationWindow_KeyDown;
            this.Loaded += TwoFactorVerificationWindow_Loaded;
        }

        private void TwoFactorVerificationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Focus the appropriate input field
            switch (_primaryMethod)
            {
                case TwoFactorMethod.TOTP:
                    TOTPCodeTextBox.Focus();
                    break;
                case TwoFactorMethod.Email:
                case TwoFactorMethod.SMS:
                    EmailSMSCodeTextBox.Focus();
                    break;
            }
        }

        private void TwoFactorVerificationWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    CancelButton_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case Key.Enter:
                    HandleEnterKey();
                    e.Handled = true;
                    break;
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;
            }
        }

        private void HandleEnterKey()
        {
            if (TOTPPanel.Visibility == Visibility.Visible && VerifyTOTPButton.IsEnabled)
            {
                VerifyTOTPButton_Click(this, new RoutedEventArgs());
            }
            else if (EmailSMSPanel.Visibility == Visibility.Visible && VerifyEmailSMSButton.IsEnabled)
            {
                VerifyEmailSMSButton_Click(this, new RoutedEventArgs());
            }
            else if (BackupCodePanel.Visibility == Visibility.Visible && VerifyBackupButton.IsEnabled)
            {
                VerifyBackupButton_Click(this, new RoutedEventArgs());
            }
        }

        private void SetupUI()
        {
            UserNameText.Text = $"Verification required for: {_username}";

            switch (_primaryMethod)
            {
                case TwoFactorMethod.TOTP:
                    ShowTOTPPanel();
                    break;
                case TwoFactorMethod.Email:
                    ShowEmailSMSPanel("📧 Enter the verification code sent to your email", 
                        "Check your email inbox for the 6-digit verification code");
                    StartResendTimer();
                    break;
                case TwoFactorMethod.SMS:
                    ShowEmailSMSPanel("📱 Enter the verification code sent to your phone", 
                        "Check your text messages for the 6-digit verification code");
                    StartResendTimer();
                    break;
            }
        }

        #region UI Panel Management

        private void ShowTOTPPanel()
        {
            TOTPPanel.Visibility = Visibility.Visible;
            EmailSMSPanel.Visibility = Visibility.Collapsed;
            BackupCodePanel.Visibility = Visibility.Collapsed;
            UsePrimaryMethodButton.Visibility = Visibility.Collapsed;
        }

        private void ShowEmailSMSPanel(string instructionText, string targetText)
        {
            EmailSMSInstructionText.Text = instructionText;
            EmailSMSTargetText.Text = targetText;
            
            TOTPPanel.Visibility = Visibility.Collapsed;
            EmailSMSPanel.Visibility = Visibility.Visible;
            BackupCodePanel.Visibility = Visibility.Collapsed;
            UsePrimaryMethodButton.Visibility = Visibility.Collapsed;
        }

        private void ShowBackupCodePanel()
        {
            TOTPPanel.Visibility = Visibility.Collapsed;
            EmailSMSPanel.Visibility = Visibility.Collapsed;
            BackupCodePanel.Visibility = Visibility.Visible;
            UseBackupCodeButton.Visibility = Visibility.Collapsed;
            UsePrimaryMethodButton.Visibility = Visibility.Visible;
        }

        #endregion

        #region Verification Methods

        private async void VerifyTOTPButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var code = TOTPCodeTextBox.Text.Trim();

                if (!ValidateCodeInput(code, "TOTP"))
                    return;

                VerifyTOTPButton.IsEnabled = false;
                StatusText.Text = "Verifying code...";

                var result = await _twoFactorService.VerifyTwoFactorChallengeAsync(_challengeId, code, _secretKey);

                if (result.Success)
                {
                    VerificationSuccessful = true;
                    VerifiedUsername = result.Username ?? _username;
                    StatusText.Text = "Verification successful!";
                    
                    await Task.Delay(1000); // Brief pause to show success
                    this.Close();
                }
                else
                {
                    StatusText.Text = "Invalid code. Please try again.";
                    TOTPCodeTextBox.Clear();
                    TOTPCodeTextBox.Focus();
                    ShowErrorMessage(result.Message);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Verification error: {ex.Message}");
            }
            finally
            {
                VerifyTOTPButton.IsEnabled = true;
            }
        }

        private async void VerifyEmailSMSButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var code = EmailSMSCodeTextBox.Text.Trim();

                if (!ValidateCodeInput(code, "Email/SMS"))
                    return;

                VerifyEmailSMSButton.IsEnabled = false;
                StatusText.Text = "Verifying code...";

                var result = await _twoFactorService.VerifyTwoFactorChallengeAsync(_challengeId, code);

                if (result.Success)
                {
                    VerificationSuccessful = true;
                    VerifiedUsername = result.Username ?? _username;
                    StatusText.Text = "Verification successful!";
                    
                    await Task.Delay(1000); // Brief pause to show success
                    this.Close();
                }
                else
                {
                    StatusText.Text = "Invalid code. Please try again.";
                    EmailSMSCodeTextBox.Clear();
                    EmailSMSCodeTextBox.Focus();
                    ShowErrorMessage(result.Message);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Verification error: {ex.Message}");
            }
            finally
            {
                VerifyEmailSMSButton.IsEnabled = true;
            }
        }

        private async void VerifyBackupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var code = BackupCodeTextBox.Text.Trim().ToUpper();

                if (!ValidateBackupCode(code))
                    return;

                VerifyBackupButton.IsEnabled = false;
                StatusText.Text = "Verifying backup code...";

                // For backup codes, we use a different verification method
                bool isValid = _twoFactorService.ValidateBackupCode(_username, code);

                if (isValid)
                {
                    VerificationSuccessful = true;
                    VerifiedUsername = _username;
                    StatusText.Text = "Backup code verified successfully!";
                    
                    // Show warning about backup code usage
                    MessageBox.Show(
                        "Backup code used successfully!\n\n" +
                        "⚠️ This backup code has been consumed and cannot be used again.\n" +
                        "Consider generating new backup codes if you're running low.",
                        "Backup Code Used",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    
                    this.Close();
                }
                else
                {
                    StatusText.Text = "Invalid backup code. Please check and try again.";
                    BackupCodeTextBox.Clear();
                    BackupCodeTextBox.Focus();
                    ShowErrorMessage("Invalid backup code format or code has already been used.");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Backup code verification error: {ex.Message}");
            }
            finally
            {
                VerifyBackupButton.IsEnabled = true;
            }
        }

        #endregion

        #region Alternative Methods

        private void UseBackupCodeButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Switch to backup code verification?\n\n" +
                "Backup codes are one-time use recovery codes that you saved during 2FA setup.",
                "Use Backup Code",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ShowBackupCodePanel();
                StatusText.Text = "Enter your backup recovery code";
                BackupCodeTextBox.Focus();
            }
        }

        private void UsePrimaryMethodButton_Click(object sender, RoutedEventArgs e)
        {
            SetupUI(); // Return to primary method
            StatusText.Text = "Enter your verification code to continue";
        }

        private async void ResendCodeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ResendCodeButton.IsEnabled = false;
                StatusText.Text = "Sending new verification code...";

                // Generate and send new code
                await _twoFactorService.GenerateVerificationCodeAsync(_username, _primaryMethod);
                
                StatusText.Text = "New verification code sent";
                StartResendTimer();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error resending code: {ex.Message}");
                ResendCodeButton.IsEnabled = true;
            }
        }

        #endregion

        #region Validation

        private bool ValidateCodeInput(string code, string type)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                ShowErrorMessage($"Please enter your {type} verification code.");
                return false;
            }

            if (code.Length != 6)
            {
                ShowErrorMessage($"{type} verification code must be 6 digits long.");
                return false;
            }

            if (!code.All(char.IsDigit))
            {
                ShowErrorMessage($"{type} verification code must contain only numbers.");
                return false;
            }

            return true;
        }

        private bool ValidateBackupCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                ShowErrorMessage("Please enter your backup recovery code.");
                return false;
            }

            if (code.Length != 9 || !code.Contains("-"))
            {
                ShowErrorMessage("Backup code must be in format: XXXX-XXXX");
                return false;
            }

            var parts = code.Split('-');
            if (parts.Length != 2 || parts[0].Length != 4 || parts[1].Length != 4)
            {
                ShowErrorMessage("Invalid backup code format. Use: XXXX-XXXX");
                return false;
            }

            return true;
        }

        #endregion

        #region Timer Management

        private void StartResendTimer()
        {
            _resendCountdown = 60; // 60 seconds before allowing resend
            ResendCodeButton.IsEnabled = false;
            
            _resendTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _resendTimer.Tick += ResendTimer_Tick;
            _resendTimer.Start();
        }

        private void ResendTimer_Tick(object? sender, EventArgs e)
        {
            _resendCountdown--;
            ResendCountdownText.Text = $"(Resend available in {_resendCountdown}s)";

            if (_resendCountdown <= 0)
            {
                _resendTimer?.Stop();
                ResendCodeButton.IsEnabled = true;
                ResendCountdownText.Text = "";
            }
        }

        #endregion

        #region Helper Methods

        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "Verification Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ShowHelp()
        {
            var helpText = _primaryMethod switch
            {
                TwoFactorMethod.TOTP => 
                    "TOTP Verification Help:\n\n" +
                    "• Open your authenticator app (Google Authenticator, Microsoft Authenticator, etc.)\n" +
                    "• Find the entry for 'Weighbridge System'\n" +
                    "• Enter the 6-digit code shown in the app\n" +
                    "• Codes refresh every 30 seconds\n\n" +
                    "Troubleshooting:\n" +
                    "• Make sure your device time is correct\n" +
                    "• Use backup codes if you don't have access to your phone",

                TwoFactorMethod.Email => 
                    "Email Verification Help:\n\n" +
                    "• Check your email inbox for a verification code\n" +
                    "• The code is valid for 5 minutes\n" +
                    "• Check spam/junk folders if you don't see the email\n" +
                    "• Click 'Resend Code' if needed (after 60 seconds)",

                TwoFactorMethod.SMS => 
                    "SMS Verification Help:\n\n" +
                    "• Check your text messages for a verification code\n" +
                    "• The code is valid for 5 minutes\n" +
                    "• Make sure your phone has network coverage\n" +
                    "• Click 'Resend Code' if needed (after 60 seconds)",

                _ => "Two-Factor Authentication Help:\n\nEnter your verification code to continue."
            };

            helpText += "\n\nKeyboard Shortcuts:\n" +
                       "• F1: Show this help\n" +
                       "• Enter: Verify code\n" +
                       "• Escape: Cancel verification";

            MessageBox.Show(helpText, "2FA Verification Help", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Cancel two-factor authentication?\n\n" +
                "You will not be able to access Super Admin functions without completing verification.",
                "Cancel Verification",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                VerificationSuccessful = false;
                this.Close();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _resendTimer?.Stop();
            _twoFactorService?.Dispose();
            base.OnClosed(e);
        }

        #endregion
    }
}