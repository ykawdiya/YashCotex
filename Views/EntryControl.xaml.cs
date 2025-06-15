using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WeighbridgeSoftwareYashCotex.Services;
using WeighbridgeSoftwareYashCotex.Models;
using WeighbridgeSoftwareYashCotex.Helpers;

namespace WeighbridgeSoftwareYashCotex.Views
{
    public partial class EntryControl : UserControl
    {
        private readonly DatabaseService _databaseService;
        private readonly WeightService _weightService;
        private double _capturedWeight;
        
        public event EventHandler<string>? FormCompleted;

        public EntryControl()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _weightService = new WeightService();
            
            this.Loaded += EntryControl_Loaded;
            this.KeyDown += EntryControl_KeyDown;
        }
        
        private void EntryControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeForm();
            LoadData();
            CaptureCurrentWeight();
            SetFieldDefaults();
            ValidateForm();
        }
        
        private void EntryControl_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    // Quick capture weight
                    CaptureCurrentWeight();
                    UpdateFormStatus("Weight captured manually", true);
                    e.Handled = true;
                    break;
                case Key.F6:
                    // Quick validation
                    ValidateForm();
                    e.Handled = true;
                    break;
                case Key.F7:
                    // Auto-complete vehicle number if partial
                    AutoCompleteVehicleNumber();
                    e.Handled = true;
                    break;
                case Key.F8:
                    // Auto-fill customer details from last entry
                    AutoFillFromLastEntry();
                    e.Handled = true;
                    break;
                case Key.F9:
                    // Save entry
                    if (SaveButton.IsEnabled)
                        SaveButton_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case Key.F10:
                    // Clear form
                    ClearButton_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case Key.F11:
                    // Print preview
                    PreviewEntry();
                    e.Handled = true;
                    break;
                case Key.Escape:
                    // Cancel/Exit
                    CancelButton_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case Key.Tab:
                    // Allow normal tab navigation
                    break;
                case Key.Enter:
                    // Smart Enter navigation
                    HandleEnterKeyNavigation();
                    e.Handled = true;
                    break;
                case Key.F1:
                    // Show help
                    ShowEntryHelp();
                    e.Handled = true;
                    break;
            }
        }

        private void HandleEnterKeyNavigation()
        {
            var focusedElement = Keyboard.FocusedElement;
            
            // Special handling for specific fields
            if (focusedElement == VehicleNumberTextBox)
            {
                NameTextBox.Focus();
            }
            else if (focusedElement == NameTextBox)
            {
                PhoneNumberTextBox.Focus();
            }
            else if (focusedElement == PhoneNumberTextBox)
            {
                AddressComboBox.Focus();
            }
            else if (focusedElement == AddressComboBox)
            {
                MaterialComboBox.Focus();
            }
            else if (focusedElement == MaterialComboBox)
            {
                // Skip weight field as it's read-only, go to save
                if (SaveButton.IsEnabled)
                {
                    SaveButton.Focus();
                }
                else
                {
                    VehicleNumberTextBox.Focus(); // Cycle back to start
                }
            }
            else
            {
                // Default behavior - move to next focusable element
                if (focusedElement is UIElement element)
                {
                    element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
            }
        }

        private void AutoCompleteVehicleNumber()
        {
            try
            {
                var partialNumber = VehicleNumberTextBox.Text?.Trim().ToUpper();
                if (string.IsNullOrEmpty(partialNumber) || partialNumber.Length < 2)
                    return;

                // Find similar vehicle numbers from database
                var recentEntries = _databaseService.GetRecentVehicleNumbers(partialNumber, 5);
                
                if (recentEntries.Any())
                {
                    var firstMatch = recentEntries.First();
                    VehicleNumberTextBox.Text = firstMatch;
                    VehicleNumberTextBox.SelectionStart = partialNumber.Length;
                    VehicleNumberTextBox.SelectionLength = firstMatch.Length - partialNumber.Length;
                    
                    UpdateFormStatus($"Auto-completed from {recentEntries.Count} matches", true);
                }
                else
                {
                    UpdateFormStatus("No matching vehicle numbers found", false);
                }
            }
            catch (Exception ex)
            {
                UpdateFormStatus($"Auto-complete error: {ex.Message}", false);
            }
        }

        private void AutoFillFromLastEntry()
        {
            try
            {
                var vehicleNumber = VehicleNumberTextBox.Text?.Trim().ToUpper();
                if (string.IsNullOrEmpty(vehicleNumber))
                {
                    UpdateFormStatus("Enter vehicle number first", false);
                    return;
                }

                var lastEntry = _databaseService.GetLastEntryForVehicle(vehicleNumber);
                if (lastEntry != null)
                {
                    NameTextBox.Text = lastEntry.Name;
                    PhoneNumberTextBox.Text = lastEntry.PhoneNumber;
                    AddressComboBox.Text = lastEntry.Address;
                    MaterialComboBox.Text = lastEntry.Material;
                    
                    UpdateFormStatus("Customer details filled from last entry", true);
                }
                else
                {
                    UpdateFormStatus("No previous entry found for this vehicle", false);
                }
            }
            catch (Exception ex)
            {
                UpdateFormStatus($"Auto-fill error: {ex.Message}", false);
            }
        }

        private void PreviewEntry()
        {
            try
            {
                if (!SaveButton.IsEnabled)
                {
                    UpdateFormStatus("Complete all required fields first", false);
                    return;
                }

                var previewText = $"ENTRY PREVIEW\n" +
                                $"=============\n" +
                                $"RST: {RstNumberTextBox.Text}\n" +
                                $"Vehicle: {VehicleNumberTextBox.Text}\n" +
                                $"Customer: {NameTextBox.Text}\n" +
                                $"Phone: {PhoneNumberTextBox.Text}\n" +
                                $"Material: {MaterialComboBox.Text}\n" +
                                $"Weight: {WeightTextBox.Text} kg\n" +
                                $"Date: {DateTimeTextBox.Text}\n" +
                                $"Address: {AddressComboBox.Text}";

                MessageBox.Show(previewText, "Entry Preview (F11)", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                UpdateFormStatus($"Preview error: {ex.Message}", false);
            }
        }

        private void ShowEntryHelp()
        {
            var helpText = "ENTRY FORM KEYBOARD SHORTCUTS\n" +
                          "==============================\n\n" +
                          "F1  - Show this help\n" +
                          "F5  - Capture current weight\n" +
                          "F6  - Validate form\n" +
                          "F7  - Auto-complete vehicle number\n" +
                          "F8  - Auto-fill from last entry\n" +
                          "F9  - Save entry\n" +
                          "F10 - Clear form\n" +
                          "F11 - Preview entry\n" +
                          "ESC - Cancel/Exit\n\n" +
                          "NAVIGATION:\n" +
                          "Enter - Move to next field\n" +
                          "Tab   - Move to next field\n" +
                          "Shift+Tab - Move to previous field\n\n" +
                          "VALIDATION:\n" +
                          "Vehicle: 9-10 characters, auto-uppercase\n" +
                          "Phone: 10 digits only\n" +
                          "Name: First and last name required\n" +
                          "Address: Single word location";

            MessageBox.Show(helpText, "Entry Form Help (F1)", 
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void InitializeForm()
        {
            // Set auto-generated fields
            RstNumberTextBox.Text = _databaseService.GetNextRstNumber().ToString();
            DateTimeTextBox.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            
            // Set focus to first input field
            VehicleNumberTextBox.Focus();
        }

        private void LoadData()
        {
            try
            {
                // Load addresses for dropdown
                var addresses = _databaseService.GetAddresses();
                AddressComboBox.ItemsSource = addresses;
                
                // Load materials for dropdown
                var materials = _databaseService.GetMaterials();
                MaterialComboBox.ItemsSource = materials;
                
                // Set default material to first in list
                if (materials.Any())
                {
                    MaterialComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                UpdateFormStatus($"Error loading data: {ex.Message}", false);
            }
        }

        private void CaptureCurrentWeight()
        {
            try
            {
                // Simulate weight capture from weighbridge
                _capturedWeight = _weightService.GetCurrentWeight();
                WeightTextBox.Text = _capturedWeight.ToString("F2");
                UpdateWeightStatus("Weight Captured Successfully", true);
            }
            catch (Exception ex)
            {
                UpdateWeightStatus($"Weight capture failed: {ex.Message}", false);
                _capturedWeight = 0;
                WeightTextBox.Text = "0.00";
            }
        }

        private void SetFieldDefaults()
        {
            // Clear all input fields
            VehicleNumberTextBox.Text = "";
            PhoneNumberTextBox.Text = "";
            NameTextBox.Text = "";
            AddressComboBox.Text = "";
            CustomerIdTextBox.Text = "";
            
            // Hide all error messages
            HideAllErrors();
        }

        #region Field Validation and Event Handlers

        private void VehicleNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = VehicleNumberTextBox.Text.ToUpper();
            VehicleNumberTextBox.Text = text; // Ensure uppercase
            VehicleNumberTextBox.SelectionStart = text.Length; // Keep cursor at end
            
            if (ValidationHelper.ValidateVehicleNumber(text, out string error))
            {
                VehicleNumberError.Visibility = Visibility.Collapsed;
                VehicleNumberTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));
            }
            else
            {
                VehicleNumberError.Text = error;
                VehicleNumberError.Visibility = Visibility.Visible;
                VehicleNumberTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 53, 69));
            }
            
            ValidateForm();
        }

        private void PhoneNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Only allow numeric input
            var text = new string(PhoneNumberTextBox.Text.Where(char.IsDigit).ToArray());
            if (text != PhoneNumberTextBox.Text)
            {
                PhoneNumberTextBox.Text = text;
                PhoneNumberTextBox.SelectionStart = text.Length;
            }
            
            if (ValidationHelper.ValidatePhoneNumber(text, out string error))
            {
                PhoneNumberError.Visibility = Visibility.Collapsed;
                PhoneNumberTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                
                // Auto-complete customer data if phone exists
                if (text.Length == 10)
                {
                    AutoCompleteCustomerData(text);
                }
            }
            else
            {
                PhoneNumberError.Text = error;
                PhoneNumberError.Visibility = Visibility.Visible;
                PhoneNumberTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 53, 69));
            }
            
            ValidateForm();
        }

        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Auto-format to Title Case
            var text = ValidationHelper.FormatToTitleCase(NameTextBox.Text);
            if (text != NameTextBox.Text)
            {
                var selectionStart = NameTextBox.SelectionStart;
                NameTextBox.Text = text;
                NameTextBox.SelectionStart = Math.Min(selectionStart, text.Length);
            }
            
            if (ValidationHelper.ValidateName(text, out string error))
            {
                NameError.Visibility = Visibility.Collapsed;
                NameTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));
            }
            else
            {
                NameError.Text = error;
                NameError.Visibility = Visibility.Visible;
                NameTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 53, 69));
            }
            
            ValidateForm();
        }


        private void AddressComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Auto-format to Title Case when selection changes
            var text = ValidationHelper.FormatToTitleCase(AddressComboBox.Text);
            if (text != AddressComboBox.Text)
            {
                AddressComboBox.Text = text;
            }
            
            if (ValidationHelper.ValidateAddress(text, out string error))
            {
                AddressError.Visibility = Visibility.Collapsed;
                AddressComboBox.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));
            }
            else
            {
                AddressError.Text = error;
                AddressError.Visibility = Visibility.Visible;
                AddressComboBox.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 53, 69));
            }
            
            ValidateForm();
        }

        private void MaterialComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MaterialComboBox.SelectedItem != null)
            {
                MaterialError.Visibility = Visibility.Collapsed;
                MaterialComboBox.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));
            }
            else
            {
                MaterialError.Text = "Please select a material";
                MaterialError.Visibility = Visibility.Visible;
                MaterialComboBox.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 53, 69));
            }
            
            ValidateForm();
        }

        #endregion

        #region Auto-completion and Data Management

        private void AutoCompleteCustomerData(string phoneNumber)
        {
            try
            {
                var customer = _databaseService.GetCustomerByPhoneNumber(phoneNumber);
                if (customer != null)
                {
                    CustomerIdTextBox.Text = customer.Id.ToString();
                    NameTextBox.Text = customer.Name;
                    AddressComboBox.Text = customer.Address;
                    
                    UpdateFormStatus("Customer data auto-completed", true);
                }
                else
                {
                    // Generate new customer ID
                    var newId = ValidationHelper.GenerateUniqueId(phoneNumber, NameTextBox.Text, AddressComboBox.Text);
                    CustomerIdTextBox.Text = newId;
                    UpdateFormStatus("New customer - ID generated", true);
                }
            }
            catch (Exception ex)
            {
                UpdateFormStatus($"Error loading customer data: {ex.Message}", false);
            }
        }

        #endregion

        #region Form Validation

        private void ValidateForm()
        {
            bool isValid = true;
            var errors = new List<string>();

            // Validate all fields
            if (!ValidationHelper.ValidateVehicleNumber(VehicleNumberTextBox.Text, out string vehicleError))
            {
                isValid = false;
                errors.Add("Vehicle Number");
            }

            if (!ValidationHelper.ValidatePhoneNumber(PhoneNumberTextBox.Text, out string phoneError))
            {
                isValid = false;
                errors.Add("Phone Number");
            }

            if (!ValidationHelper.ValidateName(NameTextBox.Text, out string nameError))
            {
                isValid = false;
                errors.Add("Name");
            }

            if (!ValidationHelper.ValidateAddress(AddressComboBox.Text, out string addressError))
            {
                isValid = false;
                errors.Add("Address");
            }

            if (MaterialComboBox.SelectedItem == null)
            {
                isValid = false;
                errors.Add("Material");
            }

            if (_capturedWeight <= 0)
            {
                isValid = false;
                errors.Add("Weight");
            }

            // Update Save button state
            SaveButton.IsEnabled = isValid;
            
            // Update validation status
            if (isValid)
            {
                UpdateValidationStatus("All fields valid - Ready to save", true);
            }
            else
            {
                UpdateValidationStatus($"Invalid: {string.Join(", ", errors)}", false);
            }
        }

        private void HideAllErrors()
        {
            VehicleNumberError.Visibility = Visibility.Collapsed;
            PhoneNumberError.Visibility = Visibility.Collapsed;
            NameError.Visibility = Visibility.Collapsed;
            AddressError.Visibility = Visibility.Collapsed;
            MaterialError.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Button Event Handlers

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create new weighment entry
                var entry = new WeighmentEntry
                {
                    RstNumber = int.Parse(RstNumberTextBox.Text),
                    Id = int.Parse(CustomerIdTextBox.Text),
                    VehicleNumber = VehicleNumberTextBox.Text.ToUpper(),
                    PhoneNumber = PhoneNumberTextBox.Text,
                    Name = NameTextBox.Text,
                    Address = AddressComboBox.Text,
                    Material = MaterialComboBox.SelectedItem.ToString(),
                    EntryWeight = _capturedWeight,
                    EntryDateTime = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    LastUpdated = DateTime.Now
                };

                // Save to database
                _databaseService.SaveEntry(entry);
                
                // Show success message
                UpdateFormStatus("Entry saved successfully!", true);
                
                // Notify parent and close form
                FormCompleted?.Invoke(this, $"Entry saved: RST {entry.RstNumber} - {entry.VehicleNumber}");
                
                // Auto-close form after successful save
                System.Threading.Tasks.Task.Delay(1500).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => {
                        FormCompleted?.Invoke(this, "Entry form auto-closed after save");
                    });
                });
            }
            catch (Exception ex)
            {
                UpdateFormStatus($"Error saving entry: {ex.Message}", false);
                MessageBox.Show($"Error saving entry: {ex.Message}", "Save Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear all fields?", "Clear Form", 
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                SetFieldDefaults();
                InitializeForm();
                CaptureCurrentWeight(); // Re-capture weight
                UpdateFormStatus("Form cleared - Ready for new entry", true);
                VehicleNumberTextBox.Focus();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            FormCompleted?.Invoke(this, "Entry cancelled by user");
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

        private void UpdateValidationStatus(string message, bool isSuccess)
        {
            ValidationStatusText.Text = message;
            ValidationStatusText.Foreground = new SolidColorBrush(isSuccess ? 
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