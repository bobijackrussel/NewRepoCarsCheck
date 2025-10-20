using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CarRentalManagment.Utilities.Converters
{
    public class ZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var invert = parameter is string text && text.Equals("invert", StringComparison.OrdinalIgnoreCase);

            if (value is int intValue)
            {
                var isZero = intValue == 0;
                return invert ? (isZero ? Visibility.Collapsed : Visibility.Visible) : (isZero ? Visibility.Visible : Visibility.Collapsed);
            }

            if (value is long longValue)
            {
                var isZero = longValue == 0;
                return invert ? (isZero ? Visibility.Collapsed : Visibility.Visible) : (isZero ? Visibility.Visible : Visibility.Collapsed);
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
