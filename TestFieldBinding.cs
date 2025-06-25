using System;
using System.Collections.Generic;
using WeighbridgeSoftwareYashCotex.Models;

namespace WeighbridgeSoftwareYashCotex.Test
{
    /// <summary>
    /// Test program to verify that SettingsField initialization and value binding work correctly
    /// </summary>
    public class TestFieldBinding
    {
        public static void RunTests()
        {
            Console.WriteLine("=== Testing SettingsField Value Initialization ===");
            
            // Test 1: Text Field
            Console.WriteLine("\n1. Testing Text Field:");
            var textField = new SettingsField
            {
                Key = "CompanyName",
                Label = "Company Name",
                FieldType = FieldType.Text,
                DefaultValue = "YASH COTEX PRIVATE LIMITED",
                Value = "YASH COTEX PRIVATE LIMITED"
            };
            
            Console.WriteLine($"   DefaultValue: '{textField.DefaultValue}'");
            Console.WriteLine($"   Value: '{textField.Value}'");
            Console.WriteLine($"   Values match: {textField.DefaultValue?.ToString() == textField.Value?.ToString()}");
            
            // Test 2: Dropdown Field
            Console.WriteLine("\n2. Testing Dropdown Field:");
            var dropdownField = new SettingsField
            {
                Key = "ComPort",
                Label = "COM Port",
                FieldType = FieldType.Dropdown,
                Options = new List<SettingsOption>
                {
                    new() { Text = "COM1", Value = "COM1" },
                    new() { Text = "COM2", Value = "COM2" },
                    new() { Text = "COM3", Value = "COM3" }
                },
                DefaultValue = "COM2",
                Value = "COM2"
            };
            
            Console.WriteLine($"   DefaultValue: '{dropdownField.DefaultValue}'");
            Console.WriteLine($"   Value: '{dropdownField.Value}'");
            Console.WriteLine($"   Options count: {dropdownField.Options?.Count}");
            if (dropdownField.Options != null)
            {
                var matchingOption = dropdownField.Options.Find(o => o.Value?.ToString() == dropdownField.Value?.ToString());
                Console.WriteLine($"   Matching option found: {matchingOption != null}");
                if (matchingOption != null)
                    Console.WriteLine($"   Matching option text: '{matchingOption.Text}'");
            }
            
            // Test 3: Checkbox Field
            Console.WriteLine("\n3. Testing Checkbox Field:");
            var checkboxField = new SettingsField
            {
                Key = "EnableFeature",
                Label = "Enable Feature",
                FieldType = FieldType.Checkbox,
                CheckboxText = "Enable this feature",
                DefaultValue = true,
                Value = true
            };
            
            Console.WriteLine($"   DefaultValue: '{checkboxField.DefaultValue}' (Type: {checkboxField.DefaultValue?.GetType().Name})");
            Console.WriteLine($"   Value: '{checkboxField.Value}' (Type: {checkboxField.Value?.GetType().Name})");
            Console.WriteLine($"   Is boolean: {checkboxField.Value is bool}");
            if (checkboxField.Value is bool boolValue)
                Console.WriteLine($"   Boolean value: {boolValue}");
            
            // Test 4: PropertyChanged Event
            Console.WriteLine("\n4. Testing PropertyChanged Event:");
            bool eventFired = false;
            textField.PropertyChanged += (s, e) =>
            {
                Console.WriteLine($"   PropertyChanged fired for: {e.PropertyName}");
                eventFired = true;
            };
            
            textField.Value = "NEW COMPANY NAME";
            Console.WriteLine($"   New value: '{textField.Value}'");
            Console.WriteLine($"   PropertyChanged event fired: {eventFired}");
            
            // Test 5: Validation
            Console.WriteLine("\n5. Testing Field Validation:");
            var requiredField = new SettingsField
            {
                Key = "RequiredField",
                Label = "Required Field",
                FieldType = FieldType.Text,
                IsRequired = true,
                Value = ""
            };
            
            Console.WriteLine($"   Field is required: {requiredField.IsRequired}");
            Console.WriteLine($"   Current value: '{requiredField.Value}'");
            Console.WriteLine($"   Validation passes: {requiredField.Validate()}");
            
            requiredField.Value = "Some value";
            Console.WriteLine($"   After setting value: '{requiredField.Value}'");
            Console.WriteLine($"   Validation passes: {requiredField.Validate()}");
            
            Console.WriteLine("\n=== All Tests Completed ===");
        }
    }
}