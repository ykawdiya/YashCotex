using System.Collections.Generic;
using System.Windows;
using WeighbridgeSoftwareYashCotex.Models;

namespace WeighbridgeSoftwareYashCotex
{
    public partial class TestSettingsWindow : Window
    {
        public TestSettingsWindow()
        {
            InitializeComponent();
            SetupTestFields();
        }

        private void SetupTestFields()
        {
            // Test Field 1: Simple Text Field
            var textField = new SettingsField
            {
                Key = "TestCompanyName",
                Label = "Company Name",
                FieldType = FieldType.Text,
                IsRequired = true,
                Placeholder = "Enter company name",
                DefaultValue = "YASH COTEX PRIVATE LIMITED",
                Value = "YASH COTEX PRIVATE LIMITED",
                Tooltip = "Test company name field"
            };
            TestField1.Field = textField;

            // Test Field 2: Dropdown Field
            var dropdownField = new SettingsField
            {
                Key = "TestComPort",
                Label = "COM Port",
                FieldType = FieldType.Dropdown,
                IsRequired = true,
                Options = new List<SettingsOption>
                {
                    new() { Text = "COM1", Value = "COM1" },
                    new() { Text = "COM2", Value = "COM2" },
                    new() { Text = "COM3", Value = "COM3" },
                    new() { Text = "COM4", Value = "COM4" }
                },
                DefaultValue = "COM2",
                Value = "COM2",
                Tooltip = "Test dropdown field"
            };
            TestField2.Field = dropdownField;

            // Test Field 3: Checkbox Field
            var checkboxField = new SettingsField
            {
                Key = "TestEnable",
                Label = "Enable Feature",
                FieldType = FieldType.Checkbox,
                CheckboxText = "Enable this test feature",
                DefaultValue = true,
                Value = true,
                Tooltip = "Test checkbox field"
            };
            TestField3.Field = checkboxField;

            // Setup debug info
            UpdateDebugInfo();
            
            // Monitor value changes
            textField.ValueChanged += (s, e) => UpdateDebugInfo();
            dropdownField.ValueChanged += (s, e) => UpdateDebugInfo();
            checkboxField.ValueChanged += (s, e) => UpdateDebugInfo();
        }

        private void UpdateDebugInfo()
        {
            var text = $"Field 1 Value: '{TestField1.Field?.Value}' (Type: {TestField1.Field?.Value?.GetType().Name})\n";
            text += $"Field 2 Value: '{TestField2.Field?.Value}' (Type: {TestField2.Field?.Value?.GetType().Name})\n";
            text += $"Field 3 Value: '{TestField3.Field?.Value}' (Type: {TestField3.Field?.Value?.GetType().Name})\n";
            text += $"Field 1 DataContext: {TestField1.DataContext}\n";
            text += $"Field 2 DataContext: {TestField2.DataContext}\n";
            text += $"Field 3 DataContext: {TestField3.DataContext}";
            
            DebugText.Text = text;
        }
    }
}