// Converters/NullToVisibilityConverter.cs
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ApiManagerApp.Converters
{
    // Класс NullToVisibilityConverter реализует интерфейс IValueConverter,
    // и используется для преобразования значения в видимость элемента управления (Visible/Collapsed)
    public class NullToVisibilityConverter : IValueConverter
    {
        // Метод Convert вызывается при передаче данных от ViewModel к View
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Устанавливаем видимость в зависимости от того, null ли значение
            bool isVisible = value != null;

            // Если передан параметр "inverse", инвертируем логику
            if (parameter is string paramString && paramString.Equals("inverse", StringComparison.OrdinalIgnoreCase))
            {
                isVisible = !isVisible;
            }

            // Возвращаем либо Visible, либо Collapsed
            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}