using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WeighbridgeSoftwareYashCotex.Views
{
    public partial class PlaceholderSelectorWindow : Window
    {
        public string? SelectedPlaceholder { get; private set; }
        
        private readonly Dictionary<string, string> _placeholderDescriptions = new()
        {
            { "{COMPANY_NAME}", "The name of your company as configured in settings" },
            { "{COMPANY_ADDRESS}", "The complete address of your company" },
            { "{COMPANY_PHONE}", "Company phone number" },
            { "{COMPANY_EMAIL}", "Company email address" },
            { "{COMPANY_GST}", "Company GST/Tax identification number" },
            { "{RST_NUMBER}", "Unique weighment slip number" },
            { "{VEHICLE_NUMBER}", "Vehicle registration number" },
            { "{CUSTOMER_NAME}", "Name of the customer" },
            { "{CUSTOMER_PHONE}", "Customer's phone number" },
            { "{CUSTOMER_ADDRESS}", "Customer's address" },
            { "{MATERIAL}", "Type of material being weighed" },
            { "{ENTRY_WEIGHT}", "Weight when vehicle enters (gross weight)" },
            { "{EXIT_WEIGHT}", "Weight when vehicle exits (tare weight)" },
            { "{NET_WEIGHT}", "Calculated net weight (entry - exit)" },
            { "{ENTRY_DATE}", "Date when vehicle entered weighbridge" },
            { "{ENTRY_TIME}", "Time when vehicle entered weighbridge" },
            { "{EXIT_DATE}", "Date when vehicle exited weighbridge" },
            { "{EXIT_TIME}", "Time when vehicle exited weighbridge" },
            { "{CURRENT_DATE}", "Current date when printing" },
            { "{CURRENT_TIME}", "Current time when printing" },
            { "{LINE_SEPARATOR}", "Prints a line of dashes for visual separation" }
        };

        public PlaceholderSelectorWindow()
        {
            InitializeComponent();
            CreatePlaceholderButtons();
        }

        private void CreatePlaceholderButtons()
        {
            // Company Information placeholders
            var companyPlaceholders = new[]
            {
                "{COMPANY_NAME}", "{COMPANY_ADDRESS}", "{COMPANY_PHONE}", 
                "{COMPANY_EMAIL}", "{COMPANY_GST}"
            };
            
            foreach (var placeholder in companyPlaceholders)
            {
                CompanyInfoPanel.Children.Add(CreatePlaceholderButton(placeholder));
            }

            // Weighment Data placeholders
            var weighmentPlaceholders = new[]
            {
                "{RST_NUMBER}", "{VEHICLE_NUMBER}", "{CUSTOMER_NAME}", 
                "{CUSTOMER_PHONE}", "{CUSTOMER_ADDRESS}", "{MATERIAL}"
            };
            
            foreach (var placeholder in weighmentPlaceholders)
            {
                WeighmentDataPanel.Children.Add(CreatePlaceholderButton(placeholder));
            }

            // Weight Information placeholders
            var weightPlaceholders = new[]
            {
                "{ENTRY_WEIGHT}", "{EXIT_WEIGHT}", "{NET_WEIGHT}"
            };
            
            foreach (var placeholder in weightPlaceholders)
            {
                WeightInfoPanel.Children.Add(CreatePlaceholderButton(placeholder));
            }

            // Date and Time placeholders
            var dateTimePlaceholders = new[]
            {
                "{ENTRY_DATE}", "{ENTRY_TIME}", "{EXIT_DATE}", 
                "{EXIT_TIME}", "{CURRENT_DATE}", "{CURRENT_TIME}"
            };
            
            foreach (var placeholder in dateTimePlaceholders)
            {
                DateTimePanel.Children.Add(CreatePlaceholderButton(placeholder));
            }

            // Formatting placeholders
            var formattingPlaceholders = new[]
            {
                "{LINE_SEPARATOR}"
            };
            
            foreach (var placeholder in formattingPlaceholders)
            {
                FormattingPanel.Children.Add(CreatePlaceholderButton(placeholder));
            }
        }

        private Button CreatePlaceholderButton(string placeholder)
        {
            var button = new Button
            {
                Content = placeholder,
                Margin = new Thickness(2.0, 2.0, 2.0, 2.0),
                Padding = new Thickness(8.0, 4.0, 8.0, 4.0),
                Background = new SolidColorBrush(Color.FromRgb(236, 240, 241)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(189, 195, 199)),
                BorderThickness = new Thickness(1.0, 1.0, 1.0, 1.0),
                FontFamily = new System.Windows.Media.FontFamily("Consolas, monospace"),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Cursor = Cursors.Hand
            };
            
            button.Click += PlaceholderButton_Click;
            return button;
        }

        private void PlaceholderButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Content is string placeholder)
            {
                SelectedPlaceholder = placeholder;
                SelectedPlaceholderText.Text = placeholder;
                
                if (_placeholderDescriptions.TryGetValue(placeholder, out var description))
                {
                    PlaceholderDescriptionText.Text = description;
                }
                else
                {
                    PlaceholderDescriptionText.Text = "No description available for this placeholder.";
                }
                
                InsertButton.IsEnabled = true;
            }
        }

        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}