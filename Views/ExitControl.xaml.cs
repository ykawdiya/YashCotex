using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WeighbridgeSoftwareYashCotex.Services;
using WeighbridgeSoftwareYashCotex.Models;
using System.Windows.Documents;

namespace WeighbridgeSoftwareYashCotex.Views
{
    public partial class ExitControl : UserControl
    {
        private readonly DatabaseService _databaseService;
        private readonly WeightService _weightService;
        private WeighmentEntry? _currentEntry;
        private double _capturedExitWeight;
        
        public event EventHandler<string>? FormCompleted;

        public ExitControl()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _weightService = new WeightService();
            
            this.Loaded += ExitControl_Loaded;
            this.KeyDown += ExitControl_KeyDown;
        }
        
        private void ExitControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeForm();
            SearchTextBox.Focus();
        }
        
        private void ExitControl_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F9:
                    if (SaveExitButton.IsEnabled)
                        SaveExitButton_Click(this, new RoutedEventArgs());
                    break;
                case Key.F11:
                    if (PrintSlipButton.IsEnabled)
                        PrintSlipButton_Click(this, new RoutedEventArgs());
                    break;
                case Key.Escape:
                    CancelButton_Click(this, new RoutedEventArgs());
                    break;
                case Key.Enter:
                    if (SearchTextBox.IsFocused && !string.IsNullOrWhiteSpace(SearchTextBox.Text))
                        SearchButton_Click(this, new RoutedEventArgs());
                    break;
            }
        }

        private void InitializeForm()
        {
            HideEntryDetails();
            UpdateFormStatus("Ready to search for pending entries", true);
            UpdateWeightStatus("Waiting for entry selection", true);
            UpdateExitStatus("Ready for exit processing", true);
        }

        #region Search Functionality

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(searchText))
            {
                SearchStatusText.Text = "Enter search criteria to find pending entries";
                SearchStatusText.Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125));
                HideEntryDetails();
                return;
            }
            
            // Auto-search if criteria is specific enough
            if (IsValidSearchCriteria(searchText))
            {
                SearchStatusText.Text = "Press Enter or click Search to find entry";
                SearchStatusText.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69));
            }
            else
            {
                SearchStatusText.Text = "Enter at least 3 characters to search";
                SearchStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7));
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var searchText = SearchTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(searchText))
            {
                UpdateFormStatus("Please enter search criteria", false);
                return;
            }

            try
            {
                UpdateFormStatus("Searching...", true);
                var entry = FindPendingEntry(searchText);
                
                if (entry != null)
                {
                    LoadEntryDetails(entry);
                    UpdateFormStatus($"Entry found: RST {entry.RstNumber}", true);
                }
                else
                {
                    UpdateFormStatus("No pending entry found for the given criteria", false);
                    HideEntryDetails();
                }
            }
            catch (Exception ex)
            {
                UpdateFormStatus($"Search error: {ex.Message}", false);
                HideEntryDetails();
            }
        }

        private bool IsValidSearchCriteria(string searchText)
        {
            // RST number (numeric)
            if (int.TryParse(searchText, out _))
                return true;
            
            // Vehicle number (9-10 characters)
            if (searchText.Length >= 9 && searchText.Length <= 10)
                return true;
            
            // Phone number (10 digits)
            if (searchText.Length == 10 && searchText.All(char.IsDigit))
                return true;
            
            return false;
        }

        private WeighmentEntry? FindPendingEntry(string searchText)
        {
            // Auto-detect search type and find latest pending entry
            
            // Try RST number first
            if (int.TryParse(searchText, out int rstNumber))
            {
                var rstEntry = _databaseService.GetWeighmentByRst(rstNumber);
                if (rstEntry != null && !rstEntry.ExitDateTime.HasValue)
                    return rstEntry;
            }
            
            // Try Vehicle Number
            var vehicleEntry = _databaseService.GetEntryByVehicleNumber(searchText.ToUpper());
            if (vehicleEntry != null)
                return vehicleEntry;
            
            // Try Phone Number
            if (searchText.Length == 10 && searchText.All(char.IsDigit))
            {
                var phoneEntry = _databaseService.GetLatestIncompleteWeighmentByPhone(searchText);
                if (phoneEntry != null)
                    return phoneEntry;
            }
            
            return null;
        }

        #endregion

        #region Entry Details Management

        private void LoadEntryDetails(WeighmentEntry entry)
        {
            _currentEntry = entry;
            
            // Populate entry fields
            RstNumberTextBox.Text = entry.RstNumber.ToString();
            VehicleNumberTextBox.Text = entry.VehicleNumber;
            CustomerNameTextBox.Text = entry.Name;
            PhoneNumberTextBox.Text = entry.PhoneNumber;
            AddressTextBox.Text = entry.Address;
            MaterialTextBox.Text = entry.Material;
            EntryWeightTextBox.Text = $"{entry.EntryWeight:F2} KG";
            EntryDateTimeTextBox.Text = entry.EntryDateTime.ToString("dd/MM/yyyy HH:mm:ss");
            
            // Capture current exit weight
            CaptureExitWeight();
            
            // Set exit date/time
            ExitDateTimeTextBox.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            
            // Calculate and display weight comparison
            CalculateWeightComparison();
            
            // Show entry details
            EntryDetailsPanel.Visibility = Visibility.Visible;
            WeightComparisonPanel.Visibility = Visibility.Visible;
            
            // Enable action buttons
            SaveExitButton.IsEnabled = true;
            PrintSlipButton.IsEnabled = false; // Enable after save
            
            UpdateWeightStatus("Exit weight captured successfully", true);
            UpdateExitStatus("Ready to save exit", true);
        }

        private void HideEntryDetails()
        {
            EntryDetailsPanel.Visibility = Visibility.Collapsed;
            WeightComparisonPanel.Visibility = Visibility.Collapsed;
            
            SaveExitButton.IsEnabled = false;
            PrintSlipButton.IsEnabled = false;
            
            _currentEntry = null;
            _capturedExitWeight = 0;
        }

        private void CaptureExitWeight()
        {
            try
            {
                _capturedExitWeight = _weightService.GetCurrentWeight();
                ExitWeightTextBox.Text = _capturedExitWeight.ToString("F2");
                UpdateWeightStatus("Exit weight captured successfully", true);
            }
            catch (Exception ex)
            {
                UpdateWeightStatus($"Weight capture failed: {ex.Message}", false);
                _capturedExitWeight = 0;
                ExitWeightTextBox.Text = "0.00";
            }
        }

        private void CalculateWeightComparison()
        {
            if (_currentEntry == null) return;
            
            var entryWeight = _currentEntry.EntryWeight;
            var exitWeight = _capturedExitWeight;
            
            // Determine gross and tare weights
            var grossWeight = Math.Max(entryWeight, exitWeight);
            var tareWeight = Math.Min(entryWeight, exitWeight);
            var netWeight = grossWeight - tareWeight;
            
            // Update displays
            GrossWeightText.Text = $"{grossWeight:F2} KG";
            TareWeightText.Text = $"{tareWeight:F2} KG";
            NetWeightText.Text = $"{netWeight:F2} KG";
            
            // Update current entry with exit weight (gross and tare are calculated automatically)
            _currentEntry.ExitWeight = exitWeight;
        }

        #endregion

        #region Button Event Handlers

        private void SaveExitButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEntry == null)
            {
                UpdateExitStatus("No entry selected", false);
                return;
            }

            try
            {
                // Update entry with exit information
                _currentEntry.ExitWeight = _capturedExitWeight;
                _currentEntry.ExitDateTime = DateTime.Now;
                _currentEntry.LastUpdated = DateTime.Now;
                
                // Save to database
                _databaseService.UpdateEntryExit(_currentEntry);
                
                UpdateExitStatus("Exit saved successfully!", true);
                
                // Enable print button
                PrintSlipButton.IsEnabled = true;
                SaveExitButton.IsEnabled = false;
                
                // Auto-print by default
                var result = MessageBox.Show(
                    $"Exit saved successfully!\n\nRST: {_currentEntry.RstNumber}\nNet Weight: {_currentEntry.NetWeight:F2} KG\n\nDo you want to print the weighment slip now?", 
                    "Exit Completed", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    PrintWeighmentSlip();
                }
                
                // Ask for reprint option
                var reprintResult = MessageBox.Show("Do you want to print another copy?", "Reprint", 
                                                   MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (reprintResult == MessageBoxResult.Yes)
                {
                    PrintWeighmentSlip();
                }
                
                // Notify completion and auto-close
                FormCompleted?.Invoke(this, $"Exit completed: RST {_currentEntry.RstNumber} - Net: {_currentEntry.NetWeight:F2} KG");
                
                // Auto-close after 3 seconds
                System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => {
                        FormCompleted?.Invoke(this, "Exit form auto-closed after completion");
                    });
                });
            }
            catch (Exception ex)
            {
                UpdateExitStatus($"Error saving exit: {ex.Message}", false);
                MessageBox.Show($"Error saving exit: {ex.Message}", "Save Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintSlipButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEntry == null)
            {
                UpdateExitStatus("No entry to print", false);
                return;
            }

            PrintWeighmentSlip();
        }

        private void PrintWeighmentSlip()
        {
            try
            {
                if (_currentEntry == null) return;
                
                // Create print document
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var document = CreateWeighmentSlipDocument();
                    var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
                    printDialog.PrintDocument(paginator, $"Weighment Slip - RST {_currentEntry.RstNumber}");
                    
                    UpdateExitStatus("Weighment slip printed successfully", true);
                }
            }
            catch (Exception ex)
            {
                UpdateExitStatus($"Print error: {ex.Message}", false);
                MessageBox.Show($"Error printing slip: {ex.Message}", "Print Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument CreateWeighmentSlipDocument()
        {
            var document = new FlowDocument();
            document.PageWidth = 300; // Dot matrix printer width
            document.PagePadding = new Thickness(10);
            
            // Header
            var header = new Paragraph(new Run("YASH COTEX PRIVATE LIMITED"))
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center
            };
            document.Blocks.Add(header);
            
            var subHeader = new Paragraph(new Run("WEIGHMENT SLIP"))
            {
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 10)
            };
            document.Blocks.Add(subHeader);
            
            // Entry details
            if (_currentEntry != null)
            {
                var details = new Paragraph();
                details.Inlines.Add(new Run($"RST Number: {_currentEntry.RstNumber}\n"));
                details.Inlines.Add(new Run($"Vehicle: {_currentEntry.VehicleNumber}\n"));
                details.Inlines.Add(new Run($"Customer: {_currentEntry.Name}\n"));
                details.Inlines.Add(new Run($"Phone: {_currentEntry.PhoneNumber}\n"));
                details.Inlines.Add(new Run($"Material: {_currentEntry.Material}\n"));
                details.Inlines.Add(new Run($"Entry Weight: {_currentEntry.EntryWeight:F2} KG\n"));
                details.Inlines.Add(new Run($"Exit Weight: {_currentEntry.ExitWeight:F2} KG\n"));
                details.Inlines.Add(new Run($"Net Weight: {_currentEntry.NetWeight:F2} KG\n"));
                details.Inlines.Add(new Run($"Date: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n"));
                details.FontSize = 9;
                document.Blocks.Add(details);
            }
            
            // Footer
            var footer = new Paragraph(new Run($"Printed: {DateTime.Now:dd/MM/yyyy HH:mm:ss}"))
            {
                FontSize = 8,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            document.Blocks.Add(footer);
            
            return document;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            FormCompleted?.Invoke(this, "Exit cancelled by user");
        }

        #endregion

        #region Status Updates

        private void UpdateFormStatus(string message, bool isSuccess)
        {
            FormStatusText.Text = message;
            FormStatusText.Foreground = new SolidColorBrush(isSuccess ? 
                Color.FromRgb(40, 167, 69) : Color.FromRgb(220, 53, 69));
        }

        private void UpdateWeightStatus(string message, bool isSuccess)
        {
            WeightStatusText.Text = message;
            WeightStatusText.Foreground = new SolidColorBrush(isSuccess ? 
                Color.FromRgb(40, 167, 69) : Color.FromRgb(220, 53, 69));
        }

        private void UpdateExitStatus(string message, bool isSuccess)
        {
            ExitStatusText.Text = message;
            ExitStatusText.Foreground = new SolidColorBrush(isSuccess ? 
                Color.FromRgb(40, 167, 69) : Color.FromRgb(220, 53, 69));
        }

        #endregion

        public void Dispose()
        {
            try
            {
                _databaseService?.Dispose();
                _weightService?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }
    }
}