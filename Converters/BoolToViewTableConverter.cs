// Converters/BoolToViewTableConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace ApiManagerApp.Converters
{
    public class BoolToViewTableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isView)
            {
                return isView ? "View" : "Table";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}