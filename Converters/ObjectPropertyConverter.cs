// Converters/ObjectPropertyConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;
using ApiManagerApp.Services; // Для ProcedureInfo, FunctionInfo

namespace ApiManagerApp.Converters
{
    public class ObjectPropertyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Этот конвертер используется для привязки SelectedItem ListView (который является объектом)
            // к свойству SelectedRoutineName (которое является строкой).
            // При выборе элемента в ListView, value будет объектом (ProcedureInfo или FunctionInfo).
            // Мы извлекаем свойство Name.
            if (value is ProcedureInfo proc) return proc.Name;
            if (value is FunctionInfo func) return func.Name;
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack нужен, если бы мы хотели, чтобы изменение SelectedRoutineName (строки)
            // выбирало соответствующий объект в ListView. Это сложнее и пока не реализовано.
            // Для текущего сценария (только чтение из ListView в ViewModel) это не критично.
            // Если SelectedRoutineName устанавливается программно, ListView не обновится.
            // Чтобы это работало, нужно было бы найти объект в коллекции Procedures/Functions по имени.
            return Binding.DoNothing; // Или throw new NotSupportedException();
        }
    }
}