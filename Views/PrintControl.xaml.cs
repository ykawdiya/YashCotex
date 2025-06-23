using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Input;
using WeighbridgeSoftwareYashCotex.Services;
using WeighbridgeSoftwareYashCotex.Models;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WeighbridgeSoftwareYashCotex.Views
{
    public partial class PrintControl : UserControl
    {
        private readonly DatabaseService _databaseService;
        private readonly PdfGenerationService _pdfService;
        private readonly CameraService? _cameraService;
        private List<WeighmentEntry> _currentData = new();

        public event EventHandler<string>? FormCompleted;

        public PrintControl()
        {
            try
            {
                Console.WriteLine("Starting PrintControl initialization...");
                InitializeComponent();
                Console.WriteLine("InitializeComponent completed");
                
                Console.WriteLine("Initializing PrintControl...");
                
                // Initialize database service
                Console.WriteLine("Creating DatabaseService...");
                _databaseService = new DatabaseService();
                Console.WriteLine("DatabaseService initialized successfully");
                
                // Try to get camera service from the application
                try
                {
                    // Check if MainWindow has camera service available
                    if (Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        var cameraServiceField = mainWindow.GetType().GetField("_cameraService", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        _cameraService = cameraServiceField?.GetValue(mainWindow) as CameraService;
                        Console.WriteLine($"CameraService: {(_cameraService != null ? "Found" : "Not found")}");
                    }
                }
                catch (Exception cameraEx)
                {
                    // Camera service not available, continue without it
                    _cameraService = null;
                    Console.WriteLine($"Camera service initialization failed: {cameraEx.Message}");
                }
                
                // Initialize PDF service
                Console.WriteLine("Creating PdfGenerationService...");
                _pdfService = new PdfGenerationService(_cameraService, _databaseService);
                Console.WriteLine("PdfGenerationService initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PrintControl initialization failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw to let the caller handle it
            }
            
            // Load data after control is fully loaded
            this.Loaded += PrintControl_Loaded;
        }
        
        private void PrintControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadInitialData();
        }

        private void LoadInitialData()
        {
            try
            {
                Console.WriteLine("Loading initial data...");
                
                if (_databaseService == null)
                {
                    Console.WriteLine("ERROR: DatabaseService is null in LoadInitialData");
                    return;
                }
                
                // Load materials for filter
                Console.WriteLine("Getting materials from database...");
                var materials = _databaseService.GetMaterials();
                Console.WriteLine($"Retrieved {materials?.Count ?? 0} materials");
                
                if (MaterialFilterComboBox != null)
                {
                    MaterialFilterComboBox.ItemsSource = materials;
                    Console.WriteLine("Materials loaded into ComboBox");
                }
                else
                {
                    Console.WriteLine("WARNING: MaterialFilterComboBox is null");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading initial data: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"Error loading initial data: {ex.Message}");
            }
        }

        private void ReportTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReportTypeComboBox.SelectedItem == null) return;

            var selectedType = ((ComboBoxItem)ReportTypeComboBox.SelectedItem).Content.ToString();
            
            // Show/hide filter panels based on report type
            CustomerFilterPanel.Visibility = selectedType.Contains("Customer") ? Visibility.Visible : Visibility.Collapsed;
            MaterialFilterPanel.Visibility = selectedType.Contains("Material") ? Visibility.Visible : Visibility.Collapsed;
            RstNumberPanel.Visibility = selectedType.Contains("Individual") ? Visibility.Visible : Visibility.Collapsed;
            
            // Clear preview when report type changes
            ClearPreview();
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GeneratePreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating preview: {ex.Message}", "Preview Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GeneratePreview()
        {
            var selectedType = ((ComboBoxItem)ReportTypeComboBox.SelectedItem).Content.ToString();
            var fromDate = FromDatePicker.SelectedDate ?? DateTime.Today;
            var toDate = ToDatePicker.SelectedDate ?? DateTime.Today;

            // Fetch data based on report type
            _currentData = FetchReportData(selectedType, fromDate, toDate);

            // Generate preview content
            PreviewPanel.Children.Clear();
            
            if (_currentData.Any())
            {
                GenerateReportContent(selectedType);
                UpdateSummary(selectedType, fromDate, toDate);
                
                PrintButton.IsEnabled = true;
                SavePdfButton.IsEnabled = true;
            }
            else
            {
                ShowNoDataMessage();
                PrintButton.IsEnabled = false;
                SavePdfButton.IsEnabled = false;
            }
        }

        private List<WeighmentEntry> FetchReportData(string reportType, DateTime fromDate, DateTime toDate)
        {
            var allData = _databaseService.GetAllWeighments()
                .Where(w => w.EntryDateTime.Date >= fromDate.Date && w.EntryDateTime.Date <= toDate.Date)
                .ToList();

            return reportType switch
            {
                "Individual Weighment Slip" => FetchIndividualWeighment(),
                "Customer Report" => FetchCustomerReport(allData),
                "Material Wise Report" => FetchMaterialReport(allData),
                _ => allData // Daily Summary and Date Range reports
            };
        }

        private List<WeighmentEntry> FetchIndividualWeighment()
        {
            if (int.TryParse(RstNumberTextBox.Text, out var rstNumber))
            {
                var entry = _databaseService.GetWeighmentByRst(rstNumber);
                return entry != null ? new List<WeighmentEntry> { entry } : new List<WeighmentEntry>();
            }
            return new List<WeighmentEntry>();
        }

        private List<WeighmentEntry> FetchCustomerReport(List<WeighmentEntry> allData)
        {
            var phoneFilter = CustomerPhoneTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(phoneFilter))
            {
                return allData.Where(w => w.PhoneNumber.Contains(phoneFilter)).ToList();
            }
            return allData;
        }

        private List<WeighmentEntry> FetchMaterialReport(List<WeighmentEntry> allData)
        {
            var materialFilter = MaterialFilterComboBox.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(materialFilter))
            {
                return allData.Where(w => w.Material == materialFilter).ToList();
            }
            return allData;
        }

        private void GenerateReportContent(string reportType)
        {
            // Add header if enabled
            if (IncludeHeaderCheckBox.IsChecked == true)
            {
                AddReportHeader();
            }

            // Add report title
            var titleBlock = new TextBlock
            {
                Text = reportType.ToUpper(),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            };
            PreviewPanel.Children.Add(titleBlock);

            if (reportType == "Individual Weighment Slip")
            {
                GenerateWeighmentSlip();
            }
            else
            {
                GenerateDataTable();
            }

            // Add footer if enabled
            if (IncludeFooterCheckBox.IsChecked == true)
            {
                AddReportFooter();
            }

            // Add PDF generation button if we have data
            if (_currentData.Any())
            {
                AddPdfGenerationButton();
            }
        }

        private void AddPdfGenerationButton()
        {
            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var generatePdfButton = new Button
            {
                Content = "📄 Generate PDF with Camera Captures",
                Padding = new Thickness(15, 8, 15, 8),
                Margin = new Thickness(10, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(0, 123, 255)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };

            generatePdfButton.Click += async (s, e) =>
            {
                await GeneratePreviewPdfAsync(true);
            };

            var generateBasicPdfButton = new Button
            {
                Content = "📄 Generate Basic PDF",
                Padding = new Thickness(15, 8, 15, 8),
                Margin = new Thickness(10, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };

            generateBasicPdfButton.Click += async (s, e) =>
            {
                await GeneratePreviewPdfAsync(false);
            };

            buttonPanel.Children.Add(generatePdfButton);
            buttonPanel.Children.Add(generateBasicPdfButton);
            PreviewPanel.Children.Add(buttonPanel);
        }

        private async Task GeneratePreviewPdfAsync(bool includeImages)
        {
            try
            {
                if (!_currentData.Any()) return;

                var reportType = ((ComboBoxItem)ReportTypeComboBox.SelectedItem).Content.ToString();
                
                string pdfPath;
                
                if (reportType == "Individual Weighment Slip" && _currentData.Count == 1)
                {
                    pdfPath = await _pdfService.GenerateWeighmentSlipAsync(_currentData.First(), includeImages);
                }
                else
                {
                    var reportDate = FromDatePicker.SelectedDate ?? DateTime.Today;
                    pdfPath = await _pdfService.GenerateDailyReportAsync(reportDate);
                }

                var openResult = MessageBox.Show(
                    $"PDF generated successfully!\n\nFile: {Path.GetFileName(pdfPath)}\n\nOpen the PDF now?", 
                    "PDF Generated", 
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
                MessageBox.Show($"Error generating PDF: {ex.Message}", "PDF Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddReportHeader()
        {
            var headerPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
            
            var companyName = new TextBlock
            {
                Text = "YASH COTEX PRIVATE LIMITED",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            };
            
            var address = new TextBlock
            {
                Text = "Weighbridge Management System",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125))
            };
            
            var contact = new TextBlock
            {
                Text = "Phone: +91-9876543210 | Email: info@yashcotex.com",
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125))
            };

            headerPanel.Children.Add(companyName);
            headerPanel.Children.Add(address);
            headerPanel.Children.Add(contact);
            
            var separator = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(222, 226, 230)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Margin = new Thickness(0, 10, 0, 0)
            };
            headerPanel.Children.Add(separator);
            
            PreviewPanel.Children.Add(headerPanel);
        }

        private void GenerateWeighmentSlip()
        {
            if (!_currentData.Any()) return;

            var entry = _currentData.First();
            var slipPanel = new StackPanel { Margin = new Thickness(20) };

            // Create weighment slip layout
            var detailsGrid = new Grid();
            detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var details = new[]
            {
                ("RST Number:", entry.RstNumber.ToString()),
                ("Vehicle Number:", entry.VehicleNumber),
                ("Customer Name:", entry.Name),
                ("Phone Number:", entry.PhoneNumber),
                ("Address:", entry.Address),
                ("Material:", entry.Material),
                ("Entry Date:", entry.EntryDateTime.ToString("dd/MM/yyyy HH:mm")),
                ("Entry Weight:", $"{entry.EntryWeight:F2} KG"),
                ("Exit Weight:", entry.ExitWeight?.ToString("F2") + " KG" ?? "Pending"),
                ("Net Weight:", entry.IsCompleted ? $"{entry.NetWeight:F2} KG" : "Pending")
            };

            for (int i = 0; i < details.Length; i++)
            {
                detailsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var label = new TextBlock
                {
                    Text = details[i].Item1,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 5, 10, 5)
                };
                Grid.SetRow(label, i);
                Grid.SetColumn(label, 0);

                var value = new TextBlock
                {
                    Text = details[i].Item2,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                Grid.SetRow(value, i);
                Grid.SetColumn(value, 1);

                detailsGrid.Children.Add(label);
                detailsGrid.Children.Add(value);
            }

            slipPanel.Children.Add(detailsGrid);
            PreviewPanel.Children.Add(slipPanel);
        }

        private void GenerateDataTable()
        {
            var table = new Grid();
            
            // Define columns
            var columns = new[] { "RST", "Vehicle", "Customer", "Material", "Entry Wt", "Exit Wt", "Net Wt", "Date" };
            foreach (var col in columns)
            {
                table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            // Add header row
            table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int i = 0; i < columns.Length; i++)
            {
                var header = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(52, 58, 64)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(222, 226, 230)),
                    BorderThickness = new Thickness(0.5),
                    Padding = new Thickness(8.0, 10.0, 8.0, 10.0)
                };

                var headerText = new TextBlock
                {
                    Text = columns[i],
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                header.Child = headerText;
                Grid.SetRow(header, 0);
                Grid.SetColumn(header, i);
                table.Children.Add(header);
            }

            // Add data rows
            for (int rowIndex = 0; rowIndex < _currentData.Count; rowIndex++)
            {
                table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var entry = _currentData[rowIndex];
                
                var rowData = new[]
                {
                    entry.RstNumber.ToString(),
                    entry.VehicleNumber,
                    entry.Name,
                    entry.Material,
                    $"{entry.EntryWeight:F2}",
                    entry.ExitWeight?.ToString("F2") ?? "-",
                    entry.IsCompleted ? $"{entry.NetWeight:F2}" : "-",
                    entry.EntryDateTime.ToString("dd/MM/yy")
                };

                for (int colIndex = 0; colIndex < rowData.Length; colIndex++)
                {
                    var cell = new Border
                    {
                        Background = rowIndex % 2 == 0 ? Brushes.White : new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(222, 226, 230)),
                        BorderThickness = new Thickness(0.5),
                        Padding = new Thickness(8.0, 6.0, 8.0, 6.0)
                    };

                    var cellText = new TextBlock
                    {
                        Text = rowData[colIndex],
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 11
                    };

                    cell.Child = cellText;
                    Grid.SetRow(cell, rowIndex + 1);
                    Grid.SetColumn(cell, colIndex);
                    table.Children.Add(cell);
                }
            }

            PreviewPanel.Children.Add(table);
        }

        private void AddReportFooter()
        {
            var footerPanel = new StackPanel { Margin = new Thickness(0, 30, 0, 0) };
            
            var separator = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(222, 226, 230)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            var printInfo = new TextBlock
            {
                Text = $"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | User: Admin",
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125))
            };

            footerPanel.Children.Add(separator);
            footerPanel.Children.Add(printInfo);
            PreviewPanel.Children.Add(footerPanel);
        }

        private void UpdateSummary(string reportType, DateTime fromDate, DateTime toDate)
        {
            TotalRecordsText.Text = _currentData.Count.ToString();
            DateRangeText.Text = $"{fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}";
            ReportTypeText.Text = reportType;
            SummaryPanel.Visibility = Visibility.Visible;
        }

        private void ShowNoDataMessage()
        {
            PreviewPanel.Children.Clear();
            var noDataText = new TextBlock
            {
                Text = "📭 No data found for the selected criteria",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                Margin = new Thickness(0, 50, 0, 0)
            };
            PreviewPanel.Children.Add(noDataText);
            SummaryPanel.Visibility = Visibility.Collapsed;
        }

        private void ClearPreview()
        {
            PreviewPanel.Children.Clear();
            var instructionText = new TextBlock
            {
                Text = "📄 Select report options and click 'Generate Preview'",
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                Margin = new Thickness(0, 50, 0, 0)
            };
            PreviewPanel.Children.Add(instructionText);
            SummaryPanel.Visibility = Visibility.Collapsed;
            PrintButton.IsEnabled = false;
            SavePdfButton.IsEnabled = false;
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Create a document for printing
                    var document = new FlowDocument();
                    document.Blocks.Add(new BlockUIContainer(PreviewPanel));
                    
                    var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
                    printDialog.PrintDocument(paginator, "Weighbridge Report");
                    
                    MessageBox.Show("Report sent to printer successfully!", "Print Complete", 
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    FormCompleted?.Invoke(this, "Report printed successfully");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing report: {ex.Message}", "Print Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SavePdfButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_currentData.Any())
                {
                    MessageBox.Show("No data available to generate PDF.", "No Data", 
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var reportType = ((ComboBoxItem)ReportTypeComboBox.SelectedItem).Content.ToString();
                
                // Ask user if they want to include camera captures
                var includeCamerasResult = MessageBox.Show(
                    "Include camera captures in the PDF report?\n\nYes - Include camera images (larger file)\nNo - PDF without images",
                    "Camera Captures", 
                    MessageBoxButton.YesNoCancel, 
                    MessageBoxImage.Question);

                if (includeCamerasResult == MessageBoxResult.Cancel)
                    return;

                var includeImages = includeCamerasResult == MessageBoxResult.Yes;

                // Generate PDF based on report type
                SavePdfButton.IsEnabled = false;
                SavePdfButton.Content = "📄 Generating PDF...";

                string pdfPath;
                
                if (reportType == "Individual Weighment Slip" && _currentData.Count == 1)
                {
                    // Generate individual weighment slip
                    pdfPath = await _pdfService.GenerateWeighmentSlipAsync(_currentData.First(), includeImages);
                }
                else
                {
                    // For daily/date range reports, generate daily report format
                    var reportDate = FromDatePicker.SelectedDate ?? DateTime.Today;
                    pdfPath = await _pdfService.GenerateDailyReportAsync(reportDate);
                }

                SavePdfButton.Content = "💾 Save PDF";
                SavePdfButton.IsEnabled = true;

                var openResult = MessageBox.Show(
                    $"PDF report generated successfully!\n\nLocation: {Path.GetFileName(pdfPath)}\n\nDo you want to open the PDF now?", 
                    "PDF Generated", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Information);

                if (openResult == MessageBoxResult.Yes)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = pdfPath,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Could not open PDF: {ex.Message}\n\nFile saved to: {pdfPath}", 
                                       "Open PDF Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                
                FormCompleted?.Invoke(this, $"PDF report generated: {Path.GetFileName(pdfPath)}");
            }
            catch (Exception ex)
            {
                SavePdfButton.Content = "💾 Save PDF";
                SavePdfButton.IsEnabled = true;
                
                MessageBox.Show($"Error generating PDF: {ex.Message}", "PDF Generation Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateHtmlReport()
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head><title>Weighbridge Report</title>");
            html.AppendLine("<style>body{font-family:Arial,sans-serif;margin:20px;}table{border-collapse:collapse;width:100%;}th,td{border:1px solid #ddd;padding:8px;text-align:center;}th{background-color:#343a40;color:white;}</style>");
            html.AppendLine("</head><body>");
            
            if (IncludeHeaderCheckBox.IsChecked == true)
            {
                html.AppendLine("<h1 style='text-align:center;'>YASH COTEX PRIVATE LIMITED</h1>");
                html.AppendLine("<p style='text-align:center;'>Weighbridge Management System</p>");
                html.AppendLine("<hr>");
            }
            
            var reportType = ((ComboBoxItem)ReportTypeComboBox.SelectedItem).Content.ToString();
            html.AppendLine($"<h2 style='text-align:center;'>{reportType}</h2>");
            
            if (reportType == "Individual Weighment Slip" && _currentData.Any())
            {
                var entry = _currentData.First();
                html.AppendLine("<table style='width:50%;margin:auto;'>");
                html.AppendLine($"<tr><td><strong>RST Number</strong></td><td>{entry.RstNumber}</td></tr>");
                html.AppendLine($"<tr><td><strong>Vehicle Number</strong></td><td>{entry.VehicleNumber}</td></tr>");
                html.AppendLine($"<tr><td><strong>Customer</strong></td><td>{entry.Name}</td></tr>");
                html.AppendLine($"<tr><td><strong>Material</strong></td><td>{entry.Material}</td></tr>");
                html.AppendLine($"<tr><td><strong>Entry Weight</strong></td><td>{entry.EntryWeight:F2} KG</td></tr>");
                html.AppendLine($"<tr><td><strong>Exit Weight</strong></td><td>{(entry.ExitWeight?.ToString("F2") + " KG" ?? "Pending")}</td></tr>");
                html.AppendLine($"<tr><td><strong>Net Weight</strong></td><td>{(entry.IsCompleted ? entry.NetWeight.ToString("F2") + " KG" : "Pending")}</td></tr>");
                html.AppendLine("</table>");
            }
            else
            {
                html.AppendLine("<table>");
                html.AppendLine("<tr><th>RST</th><th>Vehicle</th><th>Customer</th><th>Material</th><th>Entry Wt</th><th>Exit Wt</th><th>Net Wt</th><th>Date</th></tr>");
                
                foreach (var entry in _currentData)
                {
                    html.AppendLine($"<tr><td>{entry.RstNumber}</td><td>{entry.VehicleNumber}</td><td>{entry.Name}</td><td>{entry.Material}</td>");
                    html.AppendLine($"<td>{entry.EntryWeight:F2}</td><td>{(entry.ExitWeight?.ToString("F2") ?? "-")}</td>");
                    html.AppendLine($"<td>{(entry.IsCompleted ? entry.NetWeight.ToString("F2") : "-")}</td><td>{entry.EntryDateTime:dd/MM/yyyy}</td></tr>");
                }
                html.AppendLine("</table>");
            }
            
            if (IncludeFooterCheckBox.IsChecked == true)
            {
                html.AppendLine($"<hr><p style='text-align:center;font-size:12px;'>Generated on: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | User: Admin</p>");
            }
            
            html.AppendLine("</body></html>");
            return html.ToString();
        }

        public void Dispose()
        {
            try
            {
                _databaseService?.Dispose();
                _cameraService?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }
    }
}