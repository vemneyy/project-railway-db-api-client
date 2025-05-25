// Converters/BoolToViewTableConverter.cs
using System.Globalization;
using System.Windows.Data;

namespace ApiManagerApp.Converters
{
    // Конвертер, преобразующий логическое значение в строковое представление "View" или "Table"
    public class BoolToViewTableConverter : IValueConverter
    {
        // Метод Convert вызывается при передаче значения от ViewModel к View
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Если значение — булев тип
            if (value is bool isView)
            {
                // true → "View", false → "Table"
                return isView ? "View" : "Table";
            }

            // Если значение не является bool — возвращаем "Unknown"
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}