using System;
using System.Windows;
using System.Windows.Threading;
using WeighbridgeSoftwareYashCotex.Views;
using WeighbridgeSoftwareYashCotex.Services;

namespace WeighbridgeSoftwareYashCotex
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _dateTimeTimer;
        private WeightService _weightService;

        public MainWindow()
        {
            InitializeComponent();
            
            _weightService = new WeightService();
            
            InitializeDateTimeTimer();
            InitializeWeightDisplay();
        }

        private void InitializeDateTimeTimer()
        {
            _dateTimeTimer = new DispatcherTimer();
            _dateTimeTimer.Interval = TimeSpan.FromSeconds(1);
            _dateTimeTimer.Tick += (sender, e) =>
            {
                CurrentDateTime.Content = DateTime.Now.ToString("dd/MM/yyyy\nHH:mm:ss");
            };
            _dateTimeTimer.Start();
        }

        private void InitializeWeightDisplay()
        {
            _weightService.WeightChanged += (sender, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    LiveWeight.Content = e.Weight.ToString("F2");
                    StabilityIndicator.Content = e.IsStable ? "STABLE" : "UNSTABLE";
                    StabilityIndicator.Foreground = e.IsStable ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
                    LastUpdated.Content = $"Last Updated: {e.Timestamp:HH:mm:ss}";
                    ConnectionStatus.Content = "Connected";
                    ConnectionStatus.Foreground = System.Windows.Media.Brushes.Green;
                });
            };
        }

        private void EntryButton_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var entryWindow = new EntryWindow();
                entryWindow.Show();
                LatestOperation.Content = "Entry window opened";
            }
            catch (Exception ex)
            {
                LatestOperation.Content = $"Error opening entry: {ex.Message}";
            }
        }

        private void ExitButton_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var exitWindow = new ExitWindow();
                exitWindow.Show();
                LatestOperation.Content = "Exit window opened";
            }
            catch (Exception ex)
            {
                LatestOperation.Content = $"Error opening exit: {ex.Message}";
            }
        }

        private void PrintButton_Clicked(object sender, RoutedEventArgs e)
        {
            LatestOperation.Content = "Print window opened";
        }

        private void SettingsButton_Clicked(object sender, RoutedEventArgs e)
        {
            LatestOperation.Content = "Settings window opened";
        }

        private void LogoutButton_Clicked(object sender, RoutedEventArgs e)
        {
            LatestOperation.Content = "Logout requested";
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _dateTimeTimer?.Stop();
            _weightService?.Dispose();
        }
    }
}