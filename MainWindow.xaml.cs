using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WeighbridgeSoftwareYashCotex.Views;
using WeighbridgeSoftwareYashCotex.Services;

namespace WeighbridgeSoftwareYashCotex
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _dateTimeTimer;
        private WeightService _weightService;
        private UserControl? _currentFormControl;

        public MainWindow()
        {
            InitializeComponent();
            
            _weightService = new WeightService();
            
            InitializeDateTimeTimer();
            InitializeWeightDisplay();
            
            // Set up keyboard shortcuts
            this.KeyDown += MainWindow_KeyDown;
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
                // TODO: Implement print functionality in center panel
                LatestOperation.Text = "Print function - Coming Soon";
                MessageBox.Show("Print functionality will be implemented in future update.", "Print", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LatestOperation.Text = $"Print error: {ex.Message}";
            }
        }

        private void SettingsButton_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Implement settings functionality in center panel
                LatestOperation.Text = "Settings function - Coming Soon";
                MessageBox.Show("Settings functionality will be implemented in future update.", "Settings", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LatestOperation.Text = $"Settings error: {ex.Message}";
            }
        }

        private void LogoutButton_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Are you sure you want to logout and close the application?", "Logout Confirmation", 
                                            MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    LatestOperation.Text = "Logging out...";
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                LatestOperation.Text = $"Logout error: {ex.Message}";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // Cleanup resources
            _dateTimeTimer?.Stop();
            _weightService?.Dispose();
            
            // Dispose current form control
            if (_currentFormControl is EntryControl entryControl)
                entryControl.Dispose();
            else if (_currentFormControl is ExitControl exitControl)
                exitControl.Dispose();
        }
    }
}