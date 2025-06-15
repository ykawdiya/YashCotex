using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WeighbridgeSoftwareYashCotex.Models;
using WeighbridgeSoftwareYashCotex.Services;
using WeighbridgeSoftwareYashCotex.Helpers;

namespace WeighbridgeSoftwareYashCotex.Views
{
    public partial class EntryControl : UserControl
    {
        private readonly DatabaseService _databaseService;
        private readonly WeightService _weightService;
        private int _currentRstNumber;
        private bool _isFormValid = false;

        public event EventHandler<string>? FormCompleted;

        public EntryControl()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _weightService = new WeightService();
            
            InitializeForm();
            LoadData();
            
            VehicleNumberTextBox.Focus();
        }

        private void InitializeForm()
        {
            try
            {
                _currentRstNumber = _databaseService.GetNextRstNumber();
                RstNumberTextBox.Text = _currentRstNumber.ToString();
                
                // Initialize with placeholder ID - will be calculated later
                IdTextBox.Text = "Auto-Generated";
                
                // Auto-capture weight immediately
                CaptureWeightAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing form: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CaptureWeightAsync()
        {
            try
            {
                // Simulate weight capture
                await Task.Delay(500);
                var weight = _weightService.GetCurrentWeight();
                WeightTextBox.Text = weight.ToString("F2");
            }
            catch (Exception ex)
            {
                WeightTextBox.Text = "0.00";
                System.Diagnostics.Debug.WriteLine($"Weight capture error: {ex.Message}");
            }
        }

        private void LoadData()
        {
            try
            {
                var materials = _databaseService.GetMaterials();
                MaterialComboBox.ItemsSource = materials;
                if (materials.Any())
                    MaterialComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
            }
        }

        private void VehicleNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var textBox = sender as TextBox;
                var originalText = textBox?.Text ?? "";
                
                // Format the input (auto caps and limit length)
                var formattedText = ValidationHelper.FormatVehicleNumber(originalText);
                
                if (formattedText != originalText)
                {
                    var cursorPosition = textBox.CaretIndex;
                    textBox.Text = formattedText;
                    textBox.CaretIndex = Math.Min(cursorPosition, formattedText.Length);
                }
                
                ValidateField(formattedText, ValidationHelper.ValidateVehicleNumber, VehicleNumberError);
                UpdateFormValidation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in vehicle number change: {ex.Message}");
            }
        }

        private void PhoneNumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Only allow digits
            if (!Regex.IsMatch(e.Text, @"^[0-9]+$"))
            {
                e.Handled = true;
            }
        }

        private void PhoneNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var textBox = sender as TextBox;
                var originalText = textBox?.Text ?? "";
                
                // Format the input (digits only, limit to 10)
                var formattedText = ValidationHelper.FormatPhoneNumber(originalText);
                
                if (formattedText != originalText)
                {
                    var cursorPosition = textBox.CaretIndex;
                    textBox.Text = formattedText;
                    textBox.CaretIndex = Math.Min(cursorPosition, formattedText.Length);
                }
                
                ValidateField(formattedText, ValidationHelper.ValidatePhoneNumber, PhoneNumberError);
                UpdateFormValidation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in phone number change: {ex.Message}");
            }
        }

        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var textBox = sender as TextBox;
                var originalText = textBox?.Text ?? "";
                
                // Format the input (title case, limit to 2 words)
                var formattedText = ValidationHelper.FormatName(originalText);
                
                if (formattedText != originalText && !string.IsNullOrEmpty(formattedText))
                {
                    var cursorPosition = textBox.CaretIndex;
                    textBox.Text = formattedText;
                    textBox.CaretIndex = Math.Min(cursorPosition, formattedText.Length);
                }
                
                ValidateField(textBox.Text, ValidationHelper.ValidateName, NameError);
                UpdateFormValidation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in name change: {ex.Message}");
            }
        }

        private void AddressTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var textBox = sender as TextBox;
                var originalText = textBox?.Text ?? "";
                
                // Format the input (title case, one word only)
                var formattedText = ValidationHelper.FormatAddress(originalText);
                
                if (formattedText != originalText && !string.IsNullOrEmpty(formattedText))
                {
                    var cursorPosition = textBox.CaretIndex;
                    textBox.Text = formattedText;
                    textBox.CaretIndex = Math.Min(cursorPosition, formattedText.Length);
                }
                
                ValidateField(textBox.Text, ValidationHelper.ValidateAddress, AddressError);
                UpdateFormValidation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in address change: {ex.Message}");
            }
        }

        private void MaterialComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var comboBox = sender as ComboBox;
                var selectedMaterial = comboBox?.SelectedItem?.ToString() ?? "";
                
                ValidateField(selectedMaterial, ValidationHelper.ValidateMaterial, MaterialError);
                UpdateFormValidation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in material selection: {ex.Message}");
            }
        }

        private void ValidateField(string value, Func<string, Helpers.ValidationResult> validator, TextBlock errorBlock)
        {
            try
            {
                var result = validator(value);
                
                if (result.IsValid)
                {
                    errorBlock.Visibility = Visibility.Collapsed;
                }
                else
                {
                    errorBlock.Text = result.Message;
                    errorBlock.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating field: {ex.Message}");
            }
        }

        private void UpdateFormValidation()
        {
            try
            {
                // Check all required fields
                var vehicleValid = ValidationHelper.ValidateVehicleNumber(VehicleNumberTextBox?.Text ?? "").IsValid;
                var phoneValid = ValidationHelper.ValidatePhoneNumber(PhoneNumberTextBox?.Text ?? "").IsValid;
                var nameValid = ValidationHelper.ValidateName(NameTextBox?.Text ?? "").IsValid;
                var addressValid = ValidationHelper.ValidateAddress(AddressTextBox?.Text ?? "").IsValid;
                var materialValid = ValidationHelper.ValidateMaterial(MaterialComboBox?.SelectedItem?.ToString() ?? "").IsValid;
                
                _isFormValid = vehicleValid && phoneValid && nameValid && addressValid && materialValid;
                
                if (SaveButton != null)
                    SaveButton.IsEnabled = _isFormValid;
                
                // Update ID when all customer fields are valid
                if (phoneValid && nameValid && addressValid)
                {
                    UpdateCustomerId();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating form validation: {ex.Message}");
            }
        }

        private void UpdateCustomerId()
        {
            try
            {
                var phone = PhoneNumberTextBox?.Text ?? "";
                var name = NameTextBox?.Text ?? "";
                var address = AddressTextBox?.Text ?? "";
                
                if (!string.IsNullOrEmpty(phone) && !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(address))
                {
                    // Generate unique ID based on phone + name + address
                    var combinedString = phone + name + address;
                    var hash = combinedString.GetHashCode();
                    var uniqueId = Math.Abs(hash);
                    
                    IdTextBox.Text = uniqueId.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating customer ID: {ex.Message}");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isFormValid)
                {
                    MessageBox.Show("Please fill all required fields correctly.", "Validation Error", 
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var entry = new WeighmentEntry
                {
                    RstNumber = _currentRstNumber,
                    Id = int.Parse(IdTextBox.Text),
                    VehicleNumber = VehicleNumberTextBox.Text,
                    PhoneNumber = PhoneNumberTextBox.Text,
                    Name = NameTextBox.Text,
                    Address = AddressTextBox.Text,
                    Material = MaterialComboBox.SelectedItem?.ToString() ?? "",
                    EntryWeight = double.Parse(WeightTextBox.Text),
                    EntryDateTime = DateTime.Now
                };

                _databaseService.SaveEntry(entry);
                
                MessageBox.Show("Entry saved successfully!", "Success", 
                                MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Notify parent that form is completed and close
                FormCompleted?.Invoke(this, "Entry saved successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving entry: {ex.Message}", "Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Clear all fields?", "Confirm", 
                                             MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    VehicleNumberTextBox.Clear();
                    PhoneNumberTextBox.Clear();
                    NameTextBox.Clear();
                    AddressTextBox.Clear();
                    MaterialComboBox.SelectedIndex = 0;
                    
                    // Hide all error messages
                    VehicleNumberError.Visibility = Visibility.Collapsed;
                    PhoneNumberError.Visibility = Visibility.Collapsed;
                    NameError.Visibility = Visibility.Collapsed;
                    AddressError.Visibility = Visibility.Collapsed;
                    MaterialError.Visibility = Visibility.Collapsed;
                    
                    // Reset form
                    InitializeForm();
                    VehicleNumberTextBox.Focus();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing form: {ex.Message}");
            }
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