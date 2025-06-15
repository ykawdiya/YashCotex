using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WeighbridgeSoftwareYashCotex.Models;
using WeighbridgeSoftwareYashCotex.Services;

namespace WeighbridgeSoftwareYashCotex.Views
{
    public partial class ExitControl : UserControl
    {
        private readonly DatabaseService _databaseService;
        private readonly WeightService _weightService;
        private WeighmentEntry? _currentEntry;

        public event EventHandler<string>? FormCompleted;

        public ExitControl()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _weightService = new WeightService();
            
            InitializeExitWeight();
            SearchVehicleTextBox.Focus();
        }

        private async void InitializeExitWeight()
        {
            try
            {
                // Auto-capture exit weight immediately
                await Task.Delay(500);
                var weight = _weightService.GetCurrentWeight();
                ExitWeightTextBox.Text = weight.ToString("F2");
                
                // Update summary if entry is loaded
                UpdateWeightSummary();
            }
            catch (Exception ex)
            {
                ExitWeightTextBox.Text = "0.00";
                System.Diagnostics.Debug.WriteLine($"Exit weight capture error: {ex.Message}");
            }
        }

        private void SearchVehicleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Auto-format vehicle number
            var textBox = sender as TextBox;
            if (textBox != null && textBox.Text.Length >= 9)
            {
                // Auto-search when vehicle number is complete
                SearchVehicle();
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchVehicle();
        }

        private void SearchVehicle()
        {
            try
            {
                var vehicleNumber = SearchVehicleTextBox.Text.Trim().ToUpper();
                
                if (string.IsNullOrEmpty(vehicleNumber))
                {
                    MessageBox.Show("Please enter a vehicle number to search.", "Search Required", 
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Search for the entry
                _currentEntry = _databaseService.GetEntryByVehicleNumber(vehicleNumber);
                
                if (_currentEntry == null)
                {
                    MessageBox.Show($"No entry found for vehicle number: {vehicleNumber}", "Not Found", 
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearEntryDetails();
                    return;
                }

                // Check if already exited
                if (_currentEntry.ExitDateTime.HasValue)
                {
                    MessageBox.Show($"Vehicle {vehicleNumber} has already completed exit on {_currentEntry.ExitDateTime:dd/MM/yyyy HH:mm}", 
                                    "Already Exited", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearEntryDetails();
                    return;
                }

                // Display entry details
                DisplayEntryDetails(_currentEntry);
                UpdateWeightSummary();
                ExitButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching vehicle: {ex.Message}", "Search Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                ClearEntryDetails();
            }
        }

        private void DisplayEntryDetails(WeighmentEntry entry)
        {
            EntryRstNumber.Text = entry.RstNumber.ToString();
            EntryCustomer.Text = entry.Name;
            EntryPhone.Text = entry.PhoneNumber;
            EntryMaterial.Text = entry.Material;
            EntryWeight.Text = $"{entry.EntryWeight:F2} KG";
            EntryTime.Text = entry.EntryDateTime.ToString("dd/MM/yyyy HH:mm");
            
            EntryDetailsPanel.Visibility = Visibility.Visible;
        }

        private void UpdateWeightSummary()
        {
            if (_currentEntry != null && double.TryParse(ExitWeightTextBox.Text, out var exitWeight))
            {
                SummaryEntryWeight.Text = $"{_currentEntry.EntryWeight:F2} KG";
                SummaryExitWeight.Text = $"{exitWeight:F2} KG";
                
                var netWeight = _currentEntry.EntryWeight - exitWeight;
                SummaryNetWeight.Text = $"{netWeight:F2} KG";
                
                WeightSummaryPanel.Visibility = Visibility.Visible;
            }
            else
            {
                WeightSummaryPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void ClearEntryDetails()
        {
            _currentEntry = null;
            EntryDetailsPanel.Visibility = Visibility.Collapsed;
            WeightSummaryPanel.Visibility = Visibility.Collapsed;
            ExitButton.IsEnabled = false;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentEntry == null)
                {
                    MessageBox.Show("No entry selected for exit.", "Error", 
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!double.TryParse(ExitWeightTextBox.Text, out var exitWeight))
                {
                    MessageBox.Show("Invalid exit weight.", "Error", 
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Update entry with exit details
                _currentEntry.ExitWeight = exitWeight;
                _currentEntry.ExitDateTime = DateTime.Now;

                // Save to database
                _databaseService.UpdateEntryExit(_currentEntry);
                
                MessageBox.Show($"Exit completed successfully!\n\nNet Weight: {_currentEntry.NetWeight:F2} KG", 
                                "Exit Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Notify parent and reset
                FormCompleted?.Invoke(this, $"Exit completed for {_currentEntry.VehicleNumber}");
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error completing exit: {ex.Message}", "Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        private void ResetForm()
        {
            SearchVehicleTextBox.Clear();
            ClearEntryDetails();
            InitializeExitWeight();
            SearchVehicleTextBox.Focus();
        }

        public void Dispose()
        {
            try
            {
                _weightService?.Dispose();
                _databaseService?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }
    }
}