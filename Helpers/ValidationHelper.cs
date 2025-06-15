using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace WeighbridgeSoftwareYashCotex.Helpers;

public static class ValidationHelper
{
    // Vehicle number validation patterns - STRICT: 9-10 characters only
    private static readonly Regex VehicleNumberPattern = new(@"^[A-Z0-9]{9,10}$", RegexOptions.Compiled);
    private static readonly Regex PhoneNumberPattern = new(@"^[6-9]\d{9}$", RegexOptions.Compiled);
    private static readonly Regex NamePattern = new(@"^[A-Z][a-z]+ [A-Z][a-z]+$", RegexOptions.Compiled); // Exactly two words
    private static readonly Regex AddressPattern = new(@"^[A-Z][a-z]+$", RegexOptions.Compiled); // Exactly one word

    public static ValidationResult ValidateVehicleNumber(string vehicleNumber)
    {
        if (string.IsNullOrWhiteSpace(vehicleNumber))
            return new ValidationResult(false, "Vehicle number is required");

        var cleaned = vehicleNumber.Trim().ToUpper().Replace(" ", "");
        
        if (cleaned.Length < 9)
            return new ValidationResult(false, "Vehicle number must be exactly 9-10 characters");
        
        if (cleaned.Length > 10)
            return new ValidationResult(false, "Vehicle number must be exactly 9-10 characters");

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
        
        // Check if exactly two words in title case
        var words = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 2)
            return new ValidationResult(false, "Name must be exactly two words");

        if (!NamePattern.IsMatch(trimmed))
            return new ValidationResult(false, "Name must be in title case (e.g., 'John Smith')");

        return new ValidationResult(true, "Valid name");
    }

    public static ValidationResult ValidateAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return new ValidationResult(false, "Address is required");

        var trimmed = address.Trim();
        
        // Check if exactly one word in title case
        var words = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 1)
            return new ValidationResult(false, "Address must be exactly one word");

        if (!AddressPattern.IsMatch(trimmed))
            return new ValidationResult(false, "Address must be in title case (e.g., 'Mumbai')");

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
        try
        {
            if (control == null) return;
            
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error applying validation style: {ex.Message}");
        }
    }

    public static void ShowValidationMessage(TextBlock messageBlock, ValidationResult result)
    {
        try
        {
            if (messageBlock == null || result == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
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
                        try
                        {
                            messageBlock.Visibility = Visibility.Collapsed;
                            timer.Stop();
                        }
                        catch (Exception timerEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Timer error: {timerEx.Message}");
                            timer.Stop();
                        }
                    };
                    timer.Start();
                }
                else
                {
                    messageBlock.Foreground = new SolidColorBrush(Colors.Red);
                    messageBlock.Visibility = Visibility.Visible;
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing validation message: {ex.Message}");
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

    // Format input methods with strict enforcement
    public static string FormatVehicleNumber(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var formatted = input.ToUpper().Replace(" ", "");
        
        // Limit to 10 characters maximum
        if (formatted.Length > 10)
            formatted = formatted.Substring(0, 10);
            
        return formatted;
    }

    public static string FormatPhoneNumber(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        
        // Remove all non-digits
        var digits = Regex.Replace(input, @"\D", "");
        
        // Limit to exactly 10 digits
        if (digits.Length > 10)
            digits = digits.Substring(0, 10);
            
        return digits;
    }

    public static string FormatName(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        
        var words = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Limit to exactly 2 words
        if (words.Length > 2)
        {
            words = words.Take(2).ToArray();
        }
        
        // Title case each word
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + (words[i].Length > 1 ? words[i][1..].ToLower() : "");
            }
        }
        
        return string.Join(" ", words);
    }

    public static string FormatAddress(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        
        var words = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Take only the first word
        if (words.Length > 0)
        {
            var word = words[0];
            return char.ToUpper(word[0]) + (word.Length > 1 ? word[1..].ToLower() : "");
        }
        
        return string.Empty;
    }
    
    // Additional methods needed by EntryControl
    public static bool ValidateVehicleNumber(string vehicleNumber, out string error)
    {
        var result = ValidateVehicleNumber(vehicleNumber);
        error = result.Message;
        return result.IsValid;
    }
    
    public static bool ValidatePhoneNumber(string phoneNumber, out string error)
    {
        var result = ValidatePhoneNumber(phoneNumber);
        error = result.Message;
        return result.IsValid;
    }
    
    public static bool ValidateName(string name, out string error)
    {
        var result = ValidateName(name);
        error = result.Message;
        return result.IsValid;
    }
    
    public static bool ValidateAddress(string address, out string error)
    {
        var result = ValidateAddress(address);
        error = result.Message;
        return result.IsValid;
    }
    
    
    public static string FormatToTitleCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        
        var words = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Title case each word
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + (words[i].Length > 1 ? words[i][1..].ToLower() : "");
            }
        }
        
        return string.Join(" ", words);
    }
    
    public static string GenerateUniqueId(string phoneNumber, string name, string address)
    {
        // Generate unique ID based on phone number + name + address combination
        var combinedString = $"{phoneNumber}{name}{address}".ToLower().Replace(" ", "");
        var hash = combinedString.GetHashCode();
        return Math.Abs(hash).ToString();
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