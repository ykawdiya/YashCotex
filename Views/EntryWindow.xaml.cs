using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using WeighbridgeSoftwareYashCotex.Models;
using WeighbridgeSoftwareYashCotex.Services;
using WeighbridgeSoftwareYashCotex.Helpers;

namespace WeighbridgeSoftwareYashCotex.Views
{
    public partial class EntryWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly WeightService _weightService;
        private int _currentRstNumber;
        private int _validFieldsCount = 0;
        private readonly DispatcherTimer _autoSaveTimer;
        private bool _isFormValid = false;

        public EntryWindow()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _weightService = new WeightService();
            
            InitializeForm();
            LoadData();
            
            this.KeyDown += EntryWindow_KeyDown;
            this.Closed += EntryWindow_Closed;
            
            // Initialize auto-save timer
            _autoSaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _autoSaveTimer.Tick += AutoSaveTimer_Tick;
            
            VehicleNumberTextBox.Focus();
        }

        private void InitializeForm()
        {
            _currentRstNumber = _databaseService.GetNextRstNumber();
            RstNumberTextBox.Text = _currentRstNumber.ToString();
            
            var nextId = _databaseService.GetNextId();
            IdTextBox.Text = nextId.ToString();
            
            DateTimeTextBox.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            
            WeightTextBox.Text = "0.00";
        }

        private void LoadData()
        {
            var addresses = _databaseService.GetAddresses();
            AddressComboBox.ItemsSource = addresses;
            
            var materials = _databaseService.GetMaterials();
            MaterialComboBox.ItemsSource = materials;
            if (materials.Any())
                MaterialComboBox.SelectedIndex = 0;
        }

        private void EntryWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F6:
                    SaveButton_Click(this, new RoutedEventArgs());
                    break;
                case Key.F7:
                    ClearButton_Click(this, new RoutedEventArgs());
                    break;
                case Key.Escape:
                    CloseButton_Click(this, new RoutedEventArgs());
                    break;
            }
        }

        private void VehicleNumberTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ValidateVehicleNumber();
        }

        private void PhoneNumberTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ValidatePhoneNumber();
        }

        private void NameComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateName();
        }

        private void NameComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ValidateName();
        }

        private void AddressComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateAddress();
        }

        private void AddressComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ValidateAddress();
        }

        private void MaterialComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateMaterial();
        }

        private void ValidateVehicleNumber()
        {
            var result = ValidationHelper.ValidateVehicleNumber(VehicleNumberTextBox.Text);
            ValidationHelper.ApplyValidationStyle(VehicleNumberTextBox, result);
            ValidationHelper.ShowValidationMessage(VehicleNumberValidation, result);
            UpdateFormProgress();
        }

        private void ValidatePhoneNumber()
        {
            var result = ValidationHelper.ValidatePhoneNumber(PhoneNumberTextBox.Text);
            ValidationHelper.ApplyValidationStyle(PhoneNumberTextBox, result);
            ValidationHelper.ShowValidationMessage(PhoneNumberValidation, result);
            UpdateFormProgress();
        }

        private void ValidateName()
        {
            var result = ValidationHelper.ValidateName(NameComboBox.Text);
            ValidationHelper.ApplyValidationStyle(NameComboBox, result);
            ValidationHelper.ShowValidationMessage(NameValidation, result);
            UpdateFormProgress();
        }

        private void ValidateAddress()
        {
            var result = ValidationHelper.ValidateAddress(AddressComboBox.Text);
            ValidationHelper.ApplyValidationStyle(AddressComboBox, result);
            ValidationHelper.ShowValidationMessage(AddressValidation, result);
            UpdateFormProgress();
        }

        private void ValidateMaterial()
        {
            var result = ValidationHelper.ValidateMaterial(MaterialComboBox.Text);
            ValidationHelper.ApplyValidationStyle(MaterialComboBox, result);
            ValidationHelper.ShowValidationMessage(MaterialValidation, result);
            UpdateFormProgress();
        }

        private void ValidateWeight()
        {
            var result = ValidationHelper.ValidateWeight(WeightTextBox.Text);
            ValidationHelper.ApplyValidationStyle(WeightTextBox, result);
            ValidationHelper.ShowValidationMessage(WeightValidation, result);
            UpdateFormProgress();
        }

        private void UpdateFormProgress()
        {
            _validFieldsCount = 0;
            
            // Check each required field
            if (ValidationHelper.ValidateVehicleNumber(VehicleNumberTextBox.Text).IsValid)
                _validFieldsCount++;
            if (ValidationHelper.ValidatePhoneNumber(PhoneNumberTextBox.Text).IsValid)
                _validFieldsCount++;
            if (ValidationHelper.ValidateName(NameComboBox.Text).IsValid)
                _validFieldsCount++;
            if (ValidationHelper.ValidateAddress(AddressComboBox.Text).IsValid)
                _validFieldsCount++;
            if (ValidationHelper.ValidateWeight(WeightTextBox.Text).IsValid)
                _validFieldsCount++;

            FormProgressBar.Value = _validFieldsCount;
            _isFormValid = _validFieldsCount == 5;
            SaveButton.IsEnabled = _isFormValid;

            // Update status text
            if (_isFormValid)
            {
                FormStatusText.Text = "✅ All fields are valid. Ready to save!";
                FormStatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                FormStatusText.Text = $"Please complete {5 - _validFieldsCount} more required field(s)";
                FormStatusText.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private async void ShowLoadingIndicator(ProgressBar progressBar, int durationMs = 1000)
        {
            progressBar.Visibility = Visibility.Visible;
            await Task.Delay(durationMs);
            progressBar.Visibility = Visibility.Collapsed;
        }

        private void AutoSaveTimer_Tick(object? sender, EventArgs e)
        {
            // Auto-save draft functionality could be implemented here
            _autoSaveTimer.Stop();
        }

        private async void VehicleNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            var originalText = textBox?.Text ?? "";
            
            // Format the input
            var formattedText = ValidationHelper.FormatVehicleNumber(originalText);
            
            if (formattedText != originalText)
            {
                var cursorPosition = textBox.CaretIndex;
                textBox.Text = formattedText;
                textBox.CaretIndex = Math.Min(cursorPosition, formattedText.Length);
            }
            
            // Auto-complete from database
            if (formattedText.Length >= 4)
            {
                VehicleNumberLoading.Visibility = Visibility.Visible;
                
                // Simulate async database lookup
                await Task.Delay(300);
                
                var customer = _databaseService.GetCustomerByVehicleNumber(formattedText);
                if (customer != null)
                {
                    PhoneNumberTextBox.Text = customer.PhoneNumber;
                    NameComboBox.Text = customer.Name;
                    AddressComboBox.Text = customer.Address;
                    
                    // Show success message
                    VehicleNumberValidation.Text = "✅ Customer found and auto-filled";
                    VehicleNumberValidation.Foreground = System.Windows.Media.Brushes.Green;
                    VehicleNumberValidation.Visibility = Visibility.Visible;
                }
                
                VehicleNumberLoading.Visibility = Visibility.Collapsed;
            }
            
            // Real-time validation
            if (formattedText.Length > 0)
            {
                ValidateVehicleNumber();
            }
        }

        private void PhoneNumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Regex.IsMatch(e.Text, @"^[0-9]+$"))
            {
                e.Handled = true;
            }
        }

        private async void PhoneNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            var originalText = textBox?.Text ?? "";
            
            // Format the input
            var formattedText = ValidationHelper.FormatPhoneNumber(originalText);
            
            if (formattedText != originalText)
            {
                var cursorPosition = textBox.CaretIndex;
                textBox.Text = formattedText;
                textBox.CaretIndex = Math.Min(cursorPosition, formattedText.Length);
            }
            
            // Auto-complete from database
            if (formattedText.Length == 10)
            {
                PhoneNumberLoading.Visibility = Visibility.Visible;
                
                // Simulate async database lookup
                await Task.Delay(300);
                
                var customer = _databaseService.GetCustomerByPhoneNumber(formattedText);
                if (customer != null)
                {
                    VehicleNumberTextBox.Text = customer.VehicleNumber;
                    NameComboBox.Text = customer.Name;
                    AddressComboBox.Text = customer.Address;
                    
                    // Show success message
                    PhoneNumberValidation.Text = "✅ Customer found and auto-filled";
                    PhoneNumberValidation.Foreground = System.Windows.Media.Brushes.Green;
                    PhoneNumberValidation.Visibility = Visibility.Visible;
                }
                
                PhoneNumberLoading.Visibility = Visibility.Collapsed;
            }
            
            // Real-time validation
            if (formattedText.Length > 0)
            {
                ValidatePhoneNumber();
            }
        }

        private async void CaptureWeight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                button.IsEnabled = false;
                button.Content = "⏳ Capturing...";
                
                WeightLoading.Visibility = Visibility.Visible;
                
                // Simulate weight capture delay
                await Task.Delay(1500);
                
                var weight = _weightService.GetCurrentWeight();
                WeightTextBox.Text = weight.ToString("F2");
                
                // Show success animation
                WeightValidation.Text = "✅ Weight captured successfully!";
                WeightValidation.Foreground = System.Windows.Media.Brushes.Green;
                WeightValidation.Visibility = Visibility.Visible;
                
                ValidateWeight();
                
                // Auto-hide success message
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                timer.Tick += (s, args) =>
                {
                    WeightValidation.Visibility = Visibility.Collapsed;
                    timer.Stop();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                WeightValidation.Text = $"❌ Error: {ex.Message}";
                WeightValidation.Foreground = System.Windows.Media.Brushes.Red;
                WeightValidation.Visibility = Visibility.Visible;
            }
            finally
            {
                WeightLoading.Visibility = Visibility.Collapsed;
                var button = sender as Button;
                button.IsEnabled = true;
                button.Content = "📏 Capture Weight";
            }
        }

        private void NameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem is string selectedName)
            {
                var customer = _databaseService.GetCustomerByPhoneNumber(""); // Need to implement search by name
                if (customer != null)
                {
                    PhoneNumberTextBox.Text = customer.PhoneNumber;
                    VehicleNumberTextBox.Text = customer.VehicleNumber;
                    AddressComboBox.Text = customer.Address;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var entry = new WeighmentEntry
                {
                    RstNumber = _currentRstNumber,
                    VehicleNumber = VehicleNumberTextBox.Text,
                    PhoneNumber = PhoneNumberTextBox.Text,
                    Name = NameComboBox.Text,
                    Address = AddressComboBox.Text,
                    Material = MaterialComboBox.Text,
                    EntryWeight = double.Parse(WeightTextBox.Text),
                    EntryDateTime = DateTime.Now
                };

                _databaseService.SaveEntry(entry);
                MessageBox.Show("Entry saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving entry: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear all form data?",
                "Clear Form",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Clear all form fields
                VehicleNumberTextBox.Clear();
                PhoneNumberTextBox.Clear();
                NameComboBox.Text = "";
                AddressComboBox.SelectedIndex = -1;
                MaterialComboBox.SelectedIndex = 0;
                WeightTextBox.Text = "0.00";
                
                // Clear all validation messages and styles
                ValidationHelper.ClearValidation(VehicleNumberTextBox, VehicleNumberValidation);
                ValidationHelper.ClearValidation(PhoneNumberTextBox, PhoneNumberValidation);
                ValidationHelper.ClearValidation(NameComboBox, NameValidation);
                ValidationHelper.ClearValidation(AddressComboBox, AddressValidation);
                ValidationHelper.ClearValidation(MaterialComboBox, MaterialValidation);
                ValidationHelper.ClearValidation(WeightTextBox, WeightValidation);
                
                // Reset form progress
                UpdateFormProgress();
                
                // Reset RST and ID numbers
                InitializeForm();
                
                VehicleNumberTextBox.Focus();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void EntryWindow_Closed(object? sender, EventArgs e)
        {
            _weightService?.Dispose();
        }
    }
}