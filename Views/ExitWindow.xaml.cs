using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WeighbridgeSoftwareYashCotex.Models;
using WeighbridgeSoftwareYashCotex.Services;

namespace WeighbridgeSoftwareYashCotex.Views
{
    public partial class ExitWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly WeightService _weightService;
        private WeighmentEntry? _currentEntry;

        public ExitWindow()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _weightService = new WeightService();
            
            this.KeyDown += ExitWindow_KeyDown;
            this.Closed += ExitWindow_Closed;
            
            SearchTextBox.Focus();
        }

        private void ExitWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F6:
                    SaveExitButton_Click(this, new RoutedEventArgs());
                    break;
                case Key.F7:
                    PrintButton_Click(this, new RoutedEventArgs());
                    break;
                case Key.Escape:
                    CloseButton_Click(this, new RoutedEventArgs());
                    break;
                case Key.Enter:
                    SearchButton_Click(this, new RoutedEventArgs());
                    break;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(searchText) || searchText == "RST Number, Vehicle Number, or Phone Number")
                return;

            if (searchText.Length >= 3)
            {
                SearchForEntry(searchText);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var searchText = SearchTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(searchText) || searchText == "RST Number, Vehicle Number, or Phone Number")
            {
                MessageBox.Show("Please enter search criteria.", "Search", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SearchForEntry(searchText);
        }

        private void SearchForEntry(string searchText)
        {
            try
            {
                WeighmentEntry? entry = null;

                if (int.TryParse(searchText, out int rstNumber))
                {
                    entry = _databaseService.GetWeighmentByRst(rstNumber);
                }
                
                if (entry == null && searchText.Length == 10 && Regex.IsMatch(searchText, @"^\d{10}$"))
                {
                    entry = _databaseService.GetLatestIncompleteWeighmentByPhone(searchText);
                }
                
                if (entry == null)
                {
                    entry = _databaseService.GetLatestIncompleteWeighmentByVehicle(searchText);
                }

                if (entry != null)
                {
                    DisplayEntry(entry);
                    SearchStatusTextBlock.Text = "Entry found and loaded.";
                    SearchStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                    
                    EntryDetailsPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    ClearForm();
                    SearchStatusTextBlock.Text = "No matching entry found.";
                    SearchStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    
                    EntryDetailsPanel.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching for entry: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayEntry(WeighmentEntry entry)
        {
            _currentEntry = entry;
            
            DisplayRstNumber.Text = entry.RstNumber.ToString();
            DisplayVehicleNumber.Text = entry.VehicleNumber;
            DisplayPhoneNumber.Text = entry.PhoneNumber;
            DisplayName.Text = entry.Name;
            DisplayAddress.Text = entry.Address;
            DisplayMaterial.Text = entry.Material;
            DisplayEntryWeight.Text = $"{entry.EntryWeight:F2}";
            DisplayEntryDateTime.Text = entry.EntryDateTime.ToString("dd/MM/yyyy HH:mm:ss");
            
            ExitWeightTextBox.Text = "0.00";
            // Exit date/time will be set when saving
            
            CalculateNetWeight();
        }

        private void ExitWeightTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateNetWeight();
        }

        private void CaptureExitWeight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var weight = _weightService.GetCurrentWeight();
                ExitWeightTextBox.Text = weight.ToString("F2");
                
                SaveExitButton.IsEnabled = true;
                PrintButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing weight: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateNetWeight()
        {
            if (_currentEntry != null && decimal.TryParse(ExitWeightTextBox.Text, out decimal exitWeight))
            {
                var grossWeight = _currentEntry.EntryWeight;
                var tareWeight = (double)exitWeight;
                var netWeight = grossWeight - tareWeight;
                
                GrossWeightDisplay.Text = $"{grossWeight:F2} KG";
                TareWeightDisplay.Text = $"{tareWeight:F2} KG";
                NetWeightDisplay.Text = $"{netWeight:F2} KG";
                
                NetWeightDisplay.Foreground = netWeight >= 0 
                    ? System.Windows.Media.Brushes.Green 
                    : System.Windows.Media.Brushes.Red;
            }
            else
            {
                GrossWeightDisplay.Text = "0.00 KG";
                TareWeightDisplay.Text = "0.00 KG";
                NetWeightDisplay.Text = "0.00 KG";
                NetWeightDisplay.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void SaveExitButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEntry == null)
            {
                MessageBox.Show("No entry selected for exit.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(ExitWeightTextBox.Text, out decimal exitWeight))
            {
                MessageBox.Show("Please enter a valid exit weight.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var result = MessageBox.Show(
                    "Are you sure you want to save this exit entry?",
                    "Confirm Exit",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _currentEntry.ExitWeight = (double)exitWeight;
                    _currentEntry.ExitDateTime = DateTime.Now;
                    
                    _databaseService.UpdateExitWeight(_currentEntry.RstNumber, (double)exitWeight);
                    
                    MessageBox.Show("Exit entry saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving exit entry: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEntry == null)
            {
                MessageBox.Show("No entry selected for printing.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var printService = new PrintService();
                printService.PrintReceipt(_currentEntry);
                MessageBox.Show("Receipt printed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing receipt: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "RST Number, Vehicle Number, or Phone Number";
            SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
            SearchStatusTextBlock.Visibility = Visibility.Collapsed;
            ClearForm();
            EntryDetailsPanel.Visibility = Visibility.Collapsed;
            SearchTextBox.Focus();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "RST Number, Vehicle Number, or Phone Number")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = "RST Number, Vehicle Number, or Phone Number";
                SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void ClearForm()
        {
            _currentEntry = null;
            
            DisplayRstNumber.Text = "";
            DisplayVehicleNumber.Text = "";
            DisplayPhoneNumber.Text = "";
            DisplayName.Text = "";
            DisplayAddress.Text = "";
            DisplayMaterial.Text = "";
            DisplayEntryWeight.Text = "";
            DisplayEntryDateTime.Text = "";
            ExitWeightTextBox.Text = "0.00";
            GrossWeightDisplay.Text = "0.00 KG";
            TareWeightDisplay.Text = "0.00 KG";
            NetWeightDisplay.Text = "0.00 KG";
        }

        private void ExitWindow_Closed(object? sender, EventArgs e)
        {
            _weightService?.Dispose();
        }
    }
}