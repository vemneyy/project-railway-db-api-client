// Converters/ListToStringConverter.cs
using System.Globalization;
using System.Windows.Data;

namespace ApiManagerApp.Converters
{
    // Конвертер для преобразования списка строк в одну строку, разделённую запятыми
    public class ListToStringConverter : IValueConverter
    {
        // Метод Convert выполняет преобразование от ViewModel к View
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Если значение является списком строк, объединяем его элементы через запятую
            if (value is IEnumerable<string> list)
            {
                return string.Join(", ", list); // Пример: ["a", "b", "c"] → "a, b, c"
            }

            // Если значение не является списком строк, возвращаем пустую строку
            return string.Empty;
        }

        // Обратное преобразование от строки к списку строк не реализовано
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // Явно указываем, что операция не поддерживается
        }
    }
}