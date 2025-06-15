using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WeighbridgeSoftwareYashCotex.Services;
using WeighbridgeSoftwareYashCotex.Models;
using Microsoft.VisualBasic;
using System.Threading.Tasks;

namespace WeighbridgeSoftwareYashCotex.Views
{
    public partial class WeightManagementWindow : Window
    {
        private readonly WeightManipulationService _weightService;
        private readonly DatabaseService _databaseService;
        private readonly AuthenticationService _authService;
        private readonly PdfGenerationService _pdfService;
        private WeighmentEntry? _currentEntry;
        private readonly string _currentUser;

        public WeightManagementWindow(string currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _authService = new AuthenticationService();
            _weightService = new WeightManipulationService(_authService);
            _databaseService = new DatabaseService();
            _pdfService = new PdfGenerationService();
            
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            CurrentUserText.Text = $"User: {_currentUser}";
            
            // Set default date ranges
            FilterFromDatePicker.SelectedDate = DateTime.Today.AddDays(-30);
            FilterToDatePicker.SelectedDate = DateTime.Today;
            SummaryFromDatePicker.SelectedDate = DateTime.Today.AddDays(-30);
            SummaryToDatePicker.SelectedDate = DateTime.Today;
            
            // Start timer for current time
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => CurrentTimeText.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            timer.Start();
        }

        #region Weight Modification

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(SearchRstTextBox.Text.Trim(), out int rstNumber))
                {
                    UpdateSearchStatus("Please enter a valid RST number", false);
                    return;
                }

                UpdateSearchStatus("Searching...", true);

                var entry = _databaseService.GetWeighmentByRst(rstNumber);
                if (entry == null)
                {
                    UpdateSearchStatus($"No entry found for RST {rstNumber}", false);
                    HideEntryDetails();
                    return;
                }

                _currentEntry = entry;
                ShowEntryDetails(entry);
                UpdateSearchStatus($"Entry found: RST {rstNumber}", true);
            }
            catch (Exception ex)
            {
                UpdateSearchStatus($"Search error: {ex.Message}", false);
                HideEntryDetails();
            }
        }

        private void ShowEntryDetails(WeighmentEntry entry)
        {
            RstNumberText.Text = entry.RstNumber.ToString();
            VehicleNumberText.Text = entry.VehicleNumber;
            CustomerNameText.Text = entry.Name;
            MaterialText.Text = entry.Material;
            
            CurrentEntryWeightText.Text = $"{entry.EntryWeight:F2} KG";
            CurrentExitWeightText.Text = entry.ExitWeight?.ToString("F2") + " KG" ?? "Not recorded";
            
            // Enable/disable exit weight modification based on whether exit weight exists
            ModifyExitWeightButton.IsEnabled = entry.ExitWeight.HasValue;
            NewExitWeightTextBox.IsEnabled = entry.ExitWeight.HasValue;
            ExitReasonTextBox.IsEnabled = entry.ExitWeight.HasValue;
            
            if (!entry.ExitWeight.HasValue)
            {
                CurrentExitWeightText.Text = "Not recorded yet";
                CurrentExitWeightText.Foreground = new SolidColorBrush(Colors.Gray);
            }
            else
            {
                CurrentExitWeightText.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69));
            }
            
            EntryDetailsGroup.Visibility = Visibility.Visible;
        }

        private void HideEntryDetails()
        {
            EntryDetailsGroup.Visibility = Visibility.Collapsed;
            _currentEntry = null;
        }

        private async void ModifyEntryWeightButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEntry == null) return;

            try
            {
                if (!double.TryParse(NewEntryWeightTextBox.Text.Trim(), out double newWeight))
                {
                    UpdateModificationStatus("Please enter a valid weight value", false);
                    return;
                }

                var reason = EntryReasonTextBox.Text.Trim();
                if (string.IsNullOrEmpty(reason))
                {
                    UpdateModificationStatus("Please provide a reason for modification", false);
                    return;
                }

                var confirmResult = MessageBox.Show(
                    $"Confirm Entry Weight Modification\\n\\n" +
                    $"RST: {_currentEntry.RstNumber}\\n" +
                    $"Vehicle: {_currentEntry.VehicleNumber}\\n" +
                    $"Current Weight: {_currentEntry.EntryWeight:F2} KG\\n" +
                    $"New Weight: {newWeight:F2} KG\\n" +
                    $"Difference: {(newWeight - _currentEntry.EntryWeight):+F2;-F2} KG\\n\\n" +
                    $"Reason: {reason}\\n\\n" +
                    $"This action will be permanently logged. Continue?",
                    "Confirm Weight Modification",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmResult != MessageBoxResult.Yes) return;

                UpdateModificationStatus("Modifying entry weight...", true);

                var result = await _weightService.ModifyEntryWeightAsync(_currentEntry.RstNumber, newWeight, reason, _currentUser);
                
                if (result.Success)
                {
                    UpdateModificationStatus(result.Message, true);
                    
                    // Refresh entry details
                    _currentEntry.EntryWeight = newWeight;
                    CurrentEntryWeightText.Text = $"{newWeight:F2} KG";
                    
                    // Clear input fields
                    NewEntryWeightTextBox.Clear();
                    EntryReasonTextBox.Clear();
                    
                    MessageBox.Show($"Entry weight modified successfully!\\n\\n{result.Message}",
                                   "Modification Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    UpdateModificationStatus(result.Message, false);
                    MessageBox.Show($"Failed to modify entry weight:\\n\\n{result.Message}",
                                   "Modification Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                UpdateModificationStatus($"Error: {ex.Message}", false);
                MessageBox.Show($"An error occurred:\\n\\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ModifyExitWeightButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEntry == null) return;

            try
            {
                if (!double.TryParse(NewExitWeightTextBox.Text.Trim(), out double newWeight))
                {
                    UpdateModificationStatus("Please enter a valid weight value", false);
                    return;
                }

                var reason = ExitReasonTextBox.Text.Trim();
                if (string.IsNullOrEmpty(reason))
                {
                    UpdateModificationStatus("Please provide a reason for modification", false);
                    return;
                }

                var confirmResult = MessageBox.Show(
                    $"Confirm Exit Weight Modification\\n\\n" +
                    $"RST: {_currentEntry.RstNumber}\\n" +
                    $"Vehicle: {_currentEntry.VehicleNumber}\\n" +
                    $"Current Weight: {_currentEntry.ExitWeight:F2} KG\\n" +
                    $"New Weight: {newWeight:F2} KG\\n" +
                    $"Difference: {(newWeight - _currentEntry.ExitWeight.Value):+F2;-F2} KG\\n\\n" +
                    $"Reason: {reason}\\n\\n" +
                    $"This action will be permanently logged. Continue?",
                    "Confirm Weight Modification",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmResult != MessageBoxResult.Yes) return;

                UpdateModificationStatus("Modifying exit weight...", true);

                var result = await _weightService.ModifyExitWeightAsync(_currentEntry.RstNumber, newWeight, reason, _currentUser);
                
                if (result.Success)
                {
                    UpdateModificationStatus(result.Message, true);
                    
                    // Refresh entry details
                    _currentEntry.ExitWeight = newWeight;
                    CurrentExitWeightText.Text = $"{newWeight:F2} KG";
                    
                    // Clear input fields
                    NewExitWeightTextBox.Clear();
                    ExitReasonTextBox.Clear();
                    
                    MessageBox.Show($"Exit weight modified successfully!\\n\\n{result.Message}",
                                   "Modification Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    UpdateModificationStatus(result.Message, false);
                    MessageBox.Show($"Failed to modify exit weight:\\n\\n{result.Message}",
                                   "Modification Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                UpdateModificationStatus($"Error: {ex.Message}", false);
                MessageBox.Show($"An error occurred:\\n\\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Audit History

        private async void LoadAuditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateAuditStatus("Loading audit records...", true);

                int? rstFilter = null;
                if (int.TryParse(FilterRstTextBox.Text.Trim(), out int rst))
                {
                    rstFilter = rst;
                }

                var fromDate = FilterFromDatePicker.SelectedDate;
                var toDate = FilterToDatePicker.SelectedDate?.AddDays(1).AddTicks(-1);

                var auditRecords = await _weightService.GetWeightAuditHistoryAsync(rstFilter, fromDate, toDate);
                
                AuditDataGrid.ItemsSource = auditRecords;
                
                UpdateAuditStatus($"Loaded {auditRecords.Count} audit records", true);
            }
            catch (Exception ex)
            {
                UpdateAuditStatus($"Error loading audit: {ex.Message}", false);
                MessageBox.Show($"Failed to load audit records:\\n\\n{ex.Message}",
                               "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ReverseModificationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AuditDataGrid.SelectedItem is not WeightAudit selectedAudit)
                {
                    MessageBox.Show("Please select an audit record to reverse.",
                                   "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (selectedAudit.IsReversed)
                {
                    MessageBox.Show("This modification has already been reversed.",
                                   "Already Reversed", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var reason = Interaction.InputBox(
                    "Enter reason for reversing this weight modification:",
                    "Reverse Weight Modification",
                    "");

                if (string.IsNullOrEmpty(reason)) return;

                var confirmResult = MessageBox.Show(
                    $"Confirm Weight Modification Reversal\\n\\n" +
                    $"Audit ID: {selectedAudit.Id}\\n" +
                    $"RST: {selectedAudit.RstNumber}\\n" +
                    $"Weight Type: {selectedAudit.WeightType}\\n" +
                    $"Will restore weight from {selectedAudit.NewWeight:F2} KG back to {selectedAudit.OriginalWeight:F2} KG\\n\\n" +
                    $"Reversal Reason: {reason}\\n\\n" +
                    $"This action cannot be undone. Continue?",
                    "Confirm Reversal",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmResult != MessageBoxResult.Yes) return;

                UpdateAuditStatus("Reversing modification...", true);

                var result = await _weightService.ReverseWeightModificationAsync(selectedAudit.Id, reason, _currentUser);

                if (result.Success)
                {
                    UpdateAuditStatus(result.Message, true);
                    
                    // Refresh audit grid
                    LoadAuditButton_Click(sender, e);
                    
                    MessageBox.Show($"Weight modification reversed successfully!\\n\\n{result.Message}",
                                   "Reversal Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    UpdateAuditStatus(result.Message, false);
                    MessageBox.Show($"Failed to reverse modification:\\n\\n{result.Message}",
                                   "Reversal Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                UpdateAuditStatus($"Error: {ex.Message}", false);
                MessageBox.Show($"An error occurred:\\n\\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportAuditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AuditDataGrid.ItemsSource is not IEnumerable<WeightAudit> auditRecords || !auditRecords.Any())
                {
                    MessageBox.Show("No audit records to export. Please load audit data first.",
                                   "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                UpdateAuditStatus("Generating PDF report...", true);

                // Create PDF report using a simple method for now
                var reportDate = DateTime.Today;
                var filename = $"WeightAuditReport_{reportDate:yyyyMMdd}_{DateTime.Now:HHmmss}.pdf";
                
                // For now, we'll use the daily report format
                // In a real implementation, you'd create a specific audit report format
                var pdfPath = await _pdfService.GenerateDailyReportAsync(reportDate);
                
                UpdateAuditStatus("PDF report generated successfully", true);

                var openResult = MessageBox.Show(
                    $"Audit report generated successfully!\\n\\nFile: {System.IO.Path.GetFileName(pdfPath)}\\n\\nOpen the PDF now?",
                    "Export Complete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (openResult == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = pdfPath,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                UpdateAuditStatus($"Export error: {ex.Message}", false);
                MessageBox.Show($"Failed to export audit report:\\n\\n{ex.Message}",
                               "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Summary Report

        private async void GenerateSummaryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fromDate = SummaryFromDatePicker.SelectedDate ?? DateTime.Today.AddDays(-30);
                var toDate = SummaryToDatePicker.SelectedDate ?? DateTime.Today;

                if (fromDate > toDate)
                {
                    MessageBox.Show("From date cannot be later than To date.",
                                   "Invalid Date Range", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var summary = await _weightService.GetAuditSummaryAsync(fromDate, toDate);
                
                DisplaySummary(summary);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate summary:\\n\\n{ex.Message}",
                               "Summary Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplaySummary(WeightAuditSummary summary)
        {
            SummaryPanel.Children.Clear();

            // Title
            var title = new TextBlock
            {
                Text = $"📊 WEIGHT MODIFICATION SUMMARY REPORT",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            SummaryPanel.Children.Add(title);

            // Date Range
            var dateRange = new TextBlock
            {
                Text = $"Period: {summary.DateRange}",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 30)
            };
            SummaryPanel.Children.Add(dateRange);

            // Statistics Grid
            var statsGrid = new Grid();
            for (int i = 0; i < 3; i++)
            {
                statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            for (int i = 0; i < 2; i++)
            {
                statsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            // Create stat cards
            AddStatCard(statsGrid, 0, 0, "📊 Total Modifications", summary.TotalModifications.ToString());
            AddStatCard(statsGrid, 0, 1, "📥 Entry Weight Changes", summary.EntryWeightModifications.ToString());
            AddStatCard(statsGrid, 0, 2, "📤 Exit Weight Changes", summary.ExitWeightModifications.ToString());
            AddStatCard(statsGrid, 1, 0, "📈 Weight Increased", $"{summary.TotalWeightIncreased:F2} KG");
            AddStatCard(statsGrid, 1, 1, "📉 Weight Decreased", $"{summary.TotalWeightDecreased:F2} KG");
            AddStatCard(statsGrid, 1, 2, "🎯 Unique RST Modified", summary.UniqueRstModified.ToString());

            SummaryPanel.Children.Add(statsGrid);

            // User breakdown
            if (summary.ModificationsByUser.Any())
            {
                var userTitle = new TextBlock
                {
                    Text = "👥 Modifications by User",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 30, 0, 15)
                };
                SummaryPanel.Children.Add(userTitle);

                foreach (var userStat in summary.ModificationsByUser)
                {
                    var userSummary = new TextBlock
                    {
                        Text = $"• {userStat.Key}: {userStat.Value} modifications",
                        FontSize = 12,
                        Margin = new Thickness(20, 5, 0, 0)
                    };
                    SummaryPanel.Children.Add(userSummary);
                }
            }
        }

        private void AddStatCard(Grid parent, int row, int col, string label, string value)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(222, 226, 230)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(5)
            };

            var panel = new StackPanel();
            
            var labelText = new TextBlock
            {
                Text = label,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            var valueText = new TextBlock
            {
                Text = value,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 123, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0)
            };

            panel.Children.Add(labelText);
            panel.Children.Add(valueText);
            border.Child = panel;

            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            parent.Children.Add(border);
        }

        #endregion

        #region Status Updates

        private void UpdateSearchStatus(string message, bool isSuccess)
        {
            SearchStatusText.Text = message;
            SearchStatusText.Foreground = new SolidColorBrush(isSuccess ? 
                Color.FromRgb(40, 167, 69) : Color.FromRgb(220, 53, 69));
        }

        private void UpdateModificationStatus(string message, bool isSuccess)
        {
            ModificationStatusText.Text = message;
            ModificationStatusText.Foreground = new SolidColorBrush(isSuccess ? 
                Color.FromRgb(40, 167, 69) : Color.FromRgb(220, 53, 69));
        }

        private void UpdateAuditStatus(string message, bool isSuccess)
        {
            AuditStatusText.Text = message;
            AuditStatusText.Foreground = new SolidColorBrush(isSuccess ? 
                Color.FromRgb(40, 167, 69) : Color.FromRgb(220, 53, 69));
        }

        #endregion

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _weightService?.Dispose();
            _databaseService?.Dispose();
            base.OnClosed(e);
        }
    }
}