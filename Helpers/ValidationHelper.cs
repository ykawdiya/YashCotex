using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace WeighbridgeSoftwareYashCotex.Helpers;

public static class ValidationHelper
{
    // Vehicle number validation patterns
    private static readonly Regex VehicleNumberPattern = new(@"^[A-Z0-9]{4,10}$", RegexOptions.Compiled);
    private static readonly Regex PhoneNumberPattern = new(@"^[6-9]\d{9}$", RegexOptions.Compiled);
    private static readonly Regex NamePattern = new(@"^[A-Za-z\s\.]{2,50}$", RegexOptions.Compiled);

    public static ValidationResult ValidateVehicleNumber(string vehicleNumber)
    {
        if (string.IsNullOrWhiteSpace(vehicleNumber))
            return new ValidationResult(false, "Vehicle number is required");

        var cleaned = vehicleNumber.Trim().ToUpper().Replace(" ", "");
        
        if (cleaned.Length < 4)
            return new ValidationResult(false, "Vehicle number must be at least 4 characters");
        
        if (cleaned.Length > 10)
            return new ValidationResult(false, "Vehicle number cannot exceed 10 characters");

        if (!VehicleNumberPattern.IsMatch(cleaned))
            return new ValidationResult(false, "Vehicle number can only contain letters and numbers");

        return new ValidationResult(true, "Valid vehicle number");
    }

    public static ValidationResult ValidatePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return new ValidationResult(false, "Phone number is required");

        var cleaned = phoneNumber.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        
        if (cleaned.Length != 10)
            return new ValidationResult(false, "Phone number must be exactly 10 digits");

        if (!PhoneNumberPattern.IsMatch(cleaned))
            return new ValidationResult(false, "Please enter a valid Indian mobile number");

        return new ValidationResult(true, "Valid phone number");
    }

    public static ValidationResult ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new ValidationResult(false, "Name is required");

        var trimmed = name.Trim();
        
        if (trimmed.Length < 2)
            return new ValidationResult(false, "Name must be at least 2 characters");
        
        if (trimmed.Length > 50)
            return new ValidationResult(false, "Name cannot exceed 50 characters");

        if (!NamePattern.IsMatch(trimmed))
            return new ValidationResult(false, "Name can only contain letters, spaces, and dots");

        return new ValidationResult(true, "Valid name");
    }

    public static ValidationResult ValidateAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return new ValidationResult(false, "Address is required");

        var trimmed = address.Trim();
        
        if (trimmed.Length < 5)
            return new ValidationResult(false, "Address must be at least 5 characters");
        
        if (trimmed.Length > 200)
            return new ValidationResult(false, "Address cannot exceed 200 characters");

        return new ValidationResult(true, "Valid address");
    }

    public static ValidationResult ValidateMaterial(string material)
    {
        if (string.IsNullOrWhiteSpace(material))
            return new ValidationResult(false, "Material selection is required");

        return new ValidationResult(true, "Material selected");
    }

    public static ValidationResult ValidateWeight(string weight)
    {
        if (string.IsNullOrWhiteSpace(weight))
            return new ValidationResult(false, "Weight is required");

        if (!decimal.TryParse(weight, out var weightValue))
            return new ValidationResult(false, "Please enter a valid weight");

        if (weightValue <= 0)
            return new ValidationResult(false, "Weight must be greater than 0");

        if (weightValue > 100000) // 100 tons max
            return new ValidationResult(false, "Weight cannot exceed 100,000 KG");

        return new ValidationResult(true, "Valid weight");
    }

    // Apply validation styling to controls
    public static void ApplyValidationStyle(Control control, ValidationResult result)
    {
        if (result.IsValid)
        {
            control.Tag = "Valid";
            control.ToolTip = result.Message;
        }
        else
        {
            control.Tag = "Invalid";
            control.ToolTip = result.Message;
        }
    }

    public static void ShowValidationMessage(TextBlock messageBlock, ValidationResult result)
    {
        if (messageBlock == null) return;

        messageBlock.Text = result.Message;
        
        if (result.IsValid)
        {
            messageBlock.Foreground = new SolidColorBrush(Colors.Green);
            messageBlock.Visibility = Visibility.Visible;
            
            // Auto-hide success messages after 3 seconds
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            timer.Tick += (s, e) =>
            {
                messageBlock.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }
        else
        {
            messageBlock.Foreground = new SolidColorBrush(Colors.Red);
            messageBlock.Visibility = Visibility.Visible;
        }
    }

    public static void ClearValidation(Control control, TextBlock messageBlock = null)
    {
        control.Tag = null;
        control.ToolTip = null;
        
        if (messageBlock != null)
        {
            messageBlock.Visibility = Visibility.Collapsed;
        }
    }

    // Format input methods
    public static string FormatVehicleNumber(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input.ToUpper().Replace(" ", "");
    }

    public static string FormatPhoneNumber(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        
        // Remove all non-digits
        var digits = Regex.Replace(input, @"\D", "");
        
        // Limit to 10 digits
        if (digits.Length > 10)
            digits = digits.Substring(0, 10);
            
        return digits;
    }

    public static string FormatName(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        
        // Capitalize first letter of each word
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
    }
}

public class ValidationResult
{
    public bool IsValid { get; }
    public string Message { get; }

    public ValidationResult(bool isValid, string message)
    {
        IsValid = isValid;
        Message = message;
    }
}