using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WeighbridgeSoftwareYashCotex.Models;
using WeighbridgeSoftwareYashCotex.Services;

namespace WeighbridgeSoftwareYashCotex.Views
{
    public partial class EntryWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly WeightService _weightService;
        private int _currentRstNumber;

        public EntryWindow()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _weightService = new WeightService();
            
            InitializeForm();
            LoadData();
            
            this.KeyDown += EntryWindow_KeyDown;
            this.Closed += EntryWindow_Closed;
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

        private void VehicleNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            var text = textBox?.Text?.ToUpper() ?? "";
            
            if (text.Length >= 4)
            {
                var customer = _databaseService.GetCustomerByVehicleNumber(text);
                if (customer != null)
                {
                    PhoneNumberTextBox.Text = customer.PhoneNumber;
                    NameComboBox.Text = customer.Name;
                    AddressComboBox.Text = customer.Address;
                }
            }
        }

        private void PhoneNumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Regex.IsMatch(e.Text, @"^[0-9]+$"))
            {
                e.Handled = true;
            }
        }

        private void PhoneNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            var text = textBox?.Text ?? "";
            
            if (text.Length == 10)
            {
                var customer = _databaseService.GetCustomerByPhoneNumber(text);
                if (customer != null)
                {
                    VehicleNumberTextBox.Text = customer.VehicleNumber;
                    NameComboBox.Text = customer.Name;
                    AddressComboBox.Text = customer.Address;
                }
            }
        }

        private void CaptureWeight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var weight = _weightService.GetCurrentWeight();
                WeightTextBox.Text = weight.ToString("F2");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing weight: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            VehicleNumberTextBox.Clear();
            PhoneNumberTextBox.Clear();
            NameComboBox.Text = "";
            AddressComboBox.SelectedIndex = -1;
            MaterialComboBox.SelectedIndex = 0;
            WeightTextBox.Text = "0.00";
            
            VehicleNumberTextBox.Focus();
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