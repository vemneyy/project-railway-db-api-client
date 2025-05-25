// Converters/ObjectPropertyConverter.cs
using System.Globalization;
using System.Windows.Data;
using ApiManagerApp.Classes;

namespace ApiManagerApp.Converters
{
    // Класс ObjectPropertyConverter реализует интерфейс IValueConverter, 
    // предоставляя возможность преобразования значения между моделью и представлением (View)
    public class ObjectPropertyConverter : IValueConverter
    {
        // Метод Convert вызывается при передаче данных из ViewModel в View
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Если значение — объект типа ProcedureInfo, вернуть его имя или пустую строку, если имя null
            if (value is ProcedureInfo proc) return proc.Name ?? string.Empty;

            // Если значение — объект типа FunctionInfo, вернуть его имя или пустую строку
            if (value is FunctionInfo func) return func.Name ?? string.Empty;

            // Если объект не соответствует ожидаемым типам — вернуть пустую строку
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
