// RelayCommand.cs
using System.Windows.Input;

namespace ApiManagerApp.ViewModels
{
    // Класс RelayCommand реализует интерфейс ICommand, позволяя создавать команды в MVVM-паттерне
    public class RelayCommand : ICommand
    {
        // Делегат, содержащий метод, который будет выполняться при вызове команды
        private readonly Action<object?> _execute;

        // Делегат, определяющий, можно ли выполнить команду (опционально)
        private readonly Predicate<object?>? _canExecute;

        // Конструктор принимает делегат выполнения и (необязательно) делегат проверки возможности выполнения
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute)); // Проверка на null
            _canExecute = canExecute;
        }

        // Метод определяет, можно ли выполнить команду с переданным параметром
        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter); // Если _canExecute не задан, считаем, что команду всегда можно выполнить
        }

        // Метод выполняет команду, вызывая переданный делегат _execute
        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        // Событие используется системой WPF для автоматического обновления доступности команд
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; } // Подписка на системное событие
            remove { CommandManager.RequerySuggested -= value; } // Отписка от события
        }

        // Метод вручную инициирует обновление доступности команды
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested(); // Просим систему WPF пересчитать, можно ли выполнить команду
        }
    }
}
