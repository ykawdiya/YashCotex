using System;
using System.Globalization;
using System.Windows.Data;

namespace WeighbridgeSoftwareYashCotex.Converters
{
    public class BooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            
            if (value is bool boolValue)
                return boolValue;
            
            if (bool.TryParse(value.ToString(), out bool result))
                return result;
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue;
            
            return false;
        }
    }
}