using System;
using System.Windows;
using System.Linq;
using WeighbridgeSoftwareYashCotex.Models;
using WeighbridgeSoftwareYashCotex.Services;

namespace WeighbridgeSoftwareYashCotex.Views
{
    public partial class RstTemplatePreviewWindow : Window
    {
        private RstTemplate _template;
        private readonly SettingsService _settingsService;

        public RstTemplatePreviewWindow(RstTemplate template)
        {
            InitializeComponent();
            _template = template ?? new RstTemplate();
            _settingsService = SettingsService.Instance;
            
            LoadTemplateInfo();
            GeneratePreview();
        }

        private void LoadTemplateInfo()
        {
            TemplateNameTextBox.Text = _template.Name;
            
            // Set paper width
            foreach (System.Windows.Controls.ComboBoxItem item in PaperWidthComboBox.Items)
            {
                if (int.TryParse(item.Tag?.ToString(), out var width) && width == _template.TotalWidth)
                {
                    PaperWidthComboBox.SelectedItem = item;
                    break;
                }
            }
            
            UpdateStatistics();
            UpdateRuler();
        }

        private void UpdateStatistics()
        {
            RowCountText.Text = $"Rows: {_template.Rows.Count}";
            
            var placeholderCount = _template.Rows.Sum(row => 
            {
                if (row.IsMultiColumn)
                    return row.Columns.Sum(col => CountPlaceholders(col.Content));
                else
                    return CountPlaceholders(row.Content);
            });
            
            PlaceholderCountText.Text = $"Placeholders: {placeholderCount}";
            EstimatedHeightText.Text = $"Est. Height: {_template.Rows.Count} lines";
        }

        private int CountPlaceholders(string content)
        {
            if (string.IsNullOrEmpty(content)) return 0;
            
            var placeholders = new[]
            {
                "{COMPANY_NAME}", "{COMPANY_ADDRESS}", "{COMPANY_PHONE}", "{COMPANY_EMAIL}", "{COMPANY_GST}",
                "{RST_NUMBER}", "{VEHICLE_NUMBER}", "{CUSTOMER_NAME}", "{CUSTOMER_PHONE}", "{CUSTOMER_ADDRESS}",
                "{MATERIAL}", "{ENTRY_WEIGHT}", "{EXIT_WEIGHT}", "{NET_WEIGHT}", "{ENTRY_DATE}", "{ENTRY_TIME}",
                "{EXIT_DATE}", "{EXIT_TIME}", "{LINE_SEPARATOR}", "{CURRENT_DATE}", "{CURRENT_TIME}"
            };
            
            return placeholders.Count(placeholder => content.Contains(placeholder));
        }

        private void UpdateRuler()
        {
            var width = _template.TotalWidth;
            var ruler = "";
            
            for (int i = 1; i <= width; i++)
            {
                if (i % 10 == 0)
                    ruler += (i / 10).ToString();
                else
                    ruler += (i % 10).ToString();
            }
            
            RulerText.Text = ruler;
        }

        private void GeneratePreview()
        {
            try
            {
                var preview = _template.GeneratePreview();
                PreviewTextBlock.Text = preview;
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                PreviewTextBlock.Text = $"Error generating preview: {ex.Message}";
            }
        }

        private void RefreshPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            GeneratePreview();
        }

        private void TestPrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var preview = _template.GeneratePreview();
                
                // Create a simple test print dialog
                var result = MessageBox.Show(
                    $"Send the following content to the default printer?\n\n" +
                    $"Lines: {_template.Rows.Count}\n" +
                    $"Characters per line: {_template.TotalWidth}\n\n" +
                    $"This will print the template with sample data.",
                    "Test Print Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Create a simple print service for testing
                    var printService = new DotMatrixPrintService();
                    var success = printService.PrintText(preview, _template.TotalWidth);
                    
                    if (success)
                    {
                        MessageBox.Show("Test print sent successfully!", "Print Complete", 
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Test print failed. Please check printer connection.", "Print Error", 
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during test print: {ex.Message}", "Print Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PaperWidthComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (PaperWidthComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item)
            {
                if (int.TryParse(item.Tag?.ToString(), out var width))
                {
                    _template.TotalWidth = width;
                    UpdateRuler();
                    GeneratePreview();
                }
            }
        }

        private void SaveTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _template.Name = TemplateNameTextBox.Text;
                _settingsService.RstTemplate = _template;
                _settingsService.SaveSettings();
                
                MessageBox.Show("Template saved successfully!", "Save Complete", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving template: {ex.Message}", "Save Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}