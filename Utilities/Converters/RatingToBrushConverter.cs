using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CarRentalManagment.Utilities.Converters
{
    public class RatingToBrushConverter : IMultiValueConverter
    {
        public Brush SelectedBrush { get; set; } = new SolidColorBrush(Color.FromRgb(245, 179, 1));

        public Brush UnselectedBrush { get; set; } = new SolidColorBrush(Color.FromRgb(204, 204, 221));

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
            {
                return UnselectedBrush;
            }

            var starValue = GetRating(values[0]);
            var currentRating = GetRating(values[1]);

            return currentRating >= starValue ? SelectedBrush : UnselectedBrush;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static int GetRating(object value)
        {
            return value switch
            {
                byte byteValue => byteValue,
                int intValue => intValue,
                double doubleValue => (int)Math.Round(doubleValue),
                string text when int.TryParse(text, out var parsed) => parsed,
                _ => 0
            };
        }
    }
}
