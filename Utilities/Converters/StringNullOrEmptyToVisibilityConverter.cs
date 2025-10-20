using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CarRentalManagment.Utilities.Converters
{
    public class StringNullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var isNullOrEmpty = value switch
            {
                null => true,
                string text => string.IsNullOrWhiteSpace(text),
                _ => false
            };

            return isNullOrEmpty ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
