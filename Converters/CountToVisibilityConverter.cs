// Converters/CountToVisibilityConverter.cs
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ApiManagerApp.Converters
{
    // Конвертер, определяющий видимость элемента в зависимости от количества элементов в коллекции или целого числа
    public class CountToVisibilityConverter : IValueConverter
    {
        // Метод Convert вызывается при передаче данных из ViewModel в View
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Если значение — целое число, проверяем, больше ли оно нуля
            if (value is int count)
            {
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            // Если значение — коллекция, проверяем количество элементов в ней
            if (value is ICollection collection)
            {
                return collection.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            // Если тип значения не поддерживается — возвращаем Collapsed
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}