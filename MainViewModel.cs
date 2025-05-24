// ViewModels/MainViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows; // Для MessageBox
using System.Windows.Input;
using ApiManagerApp.Services; // Убедитесь, что этот namespace корректен
using System.Collections.Generic;
using System.Data; // Для DataTable

namespace ApiManagerApp.ViewModels
{
    public class FilterEntry : INotifyPropertyChanged
    {
        private string _column;
        public string Column
        {
            get => _column;
            set { _column = value; OnPropertyChanged(); }
        }

        private string _operator = "eq"; // Значение по умолчанию
        public string Operator
        {
            get => _operator;
            set { _operator = value; OnPropertyChanged(); }
        }
        public List<string> AvailableOperators { get; } = new List<string>
            { "eq", "ne", "gt", "gte", "lt", "lte", "like", "ilike", "startswith", "endswith", "in", "isnull" };


        private string _value;
        public string Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _apiService;

        private string _healthStatus;
        public string HealthStatus
        {
            get => _healthStatus;
            set { _healthStatus = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Tables { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> Views { get; } = new ObservableCollection<string>();
        public ObservableCollection<ProcedureInfo> Procedures { get; } = new ObservableCollection<ProcedureInfo>();
        public ObservableCollection<FunctionInfo> Functions { get; } = new ObservableCollection<FunctionInfo>();

        private TableSchemaDetail _selectedTableSchema;
        public TableSchemaDetail SelectedTableSchema
        {
            get => _selectedTableSchema;
            set { _selectedTableSchema = value; OnPropertyChanged(); }
        }

        private string _selectedTableNameForSchema;
        public string SelectedTableNameForSchema
        {
            get => _selectedTableNameForSchema;
            set
            {
                _selectedTableNameForSchema = value;
                OnPropertyChanged();
                // Автоматически обновляем имена для запросов данных, если это удобно пользователю
                if (!string.IsNullOrWhiteSpace(value))
                {
                    DataQueryTableName = value;
                    DataByColumnTableName = value;
                }
            }
        }

        private string _selectedRoutineName;
        public string SelectedRoutineName
        {
            get => _selectedRoutineName;
            set { _selectedRoutineName = value; OnPropertyChanged(); }
        }

        private string _routineArgumentsInput;
        public string RoutineArgumentsInput
        {
            get => _routineArgumentsInput;
            set { _routineArgumentsInput = value; OnPropertyChanged(); }
        }

        private string _routineCallResult;
        public string RoutineCallResult
        {
            get => _routineCallResult;
            set { _routineCallResult = value; OnPropertyChanged(); }
        }

        private string _apiStatusMessage;
        public string ApiStatusMessage
        {
            get => _apiStatusMessage;
            set { _apiStatusMessage = value; OnPropertyChanged(); }
        }

        // Свойства для чтения данных (общий запрос)
        private string _dataQueryTableName;
        public string DataQueryTableName
        {
            get => _dataQueryTableName;
            set { _dataQueryTableName = value; OnPropertyChanged(); }
        }

        private int _dataQueryLimit = 10;
        public int DataQueryLimit
        {
            get => _dataQueryLimit;
            set { _dataQueryLimit = value > 0 ? value : 1; OnPropertyChanged(); } // Ensure limit is positive
        }

        private int _dataQueryOffset = 0;
        public int DataQueryOffset
        {
            get => _dataQueryOffset;
            set { _dataQueryOffset = value >= 0 ? value : 0; OnPropertyChanged(); } // Ensure offset is non-negative
        }

        private int _dataQueryTotalCount = 0;
        public int DataQueryTotalCount
        {
            get => _dataQueryTotalCount;
            set { _dataQueryTotalCount = value; OnPropertyChanged(); }
        }

        private string _dataQuerySortBy;
        public string DataQuerySortBy
        {
            get => _dataQuerySortBy;
            set { _dataQuerySortBy = value; OnPropertyChanged(); }
        }

        private string _dataQueryFields;
        public string DataQueryFields
        {
            get => _dataQueryFields;
            set { _dataQueryFields = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FilterEntry> DataQueryFilters { get; } = new ObservableCollection<FilterEntry>();

        private DataTable _queriedDataTable;
        public DataTable QueriedDataTable
        {
            get => _queriedDataTable;
            set { _queriedDataTable = value; OnPropertyChanged(); }
        }

        // Свойства для чтения данных по значению колонки
        private string _dataByColumnTableName;
        public string DataByColumnTableName
        {
            get => _dataByColumnTableName;
            set { _dataByColumnTableName = value; OnPropertyChanged(); }
        }
        private string _dataByColumnName;
        public string DataByColumnName
        {
            get => _dataByColumnName;
            set { _dataByColumnName = value; OnPropertyChanged(); }
        }
        private string _dataByColumnValue;
        public string DataByColumnValue
        {
            get => _dataByColumnValue;
            set { _dataByColumnValue = value; OnPropertyChanged(); }
        }
        private DataTable _queriedByColumnDataTable;
        public DataTable QueriedByColumnDataTable
        {
            get => _queriedByColumnDataTable;
            set { _queriedByColumnDataTable = value; OnPropertyChanged(); }
        }

        public ICommand CheckHealthCommand { get; }
        public ICommand LoadTablesCommand { get; }
        public ICommand LoadViewsCommand { get; }
        public ICommand LoadTableSchemaCommand { get; }
        public ICommand LoadProceduresCommand { get; }
        public ICommand LoadFunctionsCommand { get; }
        public ICommand CallProcedureCommand { get; }
        public ICommand CallFunctionCommand { get; }
        public ICommand AddFilterCommand { get; }
        public ICommand RemoveFilterCommand { get; }
        public ICommand ReadDataCommand { get; }
        public ICommand ReadDataByColumnCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand SelectRoutineCommand { get; }

        public MainViewModel()
        {
            _apiService = new ApiService(); // Использует URL по умолчанию из ApiService
            HealthStatus = "Click 'Check Health' to get status.";
            ApiStatusMessage = "Ready";
            RoutineArgumentsInput = "[]";
            DataQueryTableName = "";
            DataByColumnTableName = "";

            SelectRoutineCommand = new RelayCommand(ExecuteSelectRoutine, CanExecuteSelectRoutine);
            CheckHealthCommand = new RelayCommand(async param => await ExecuteCheckHealthAsync(), param => true);
            LoadTablesCommand = new RelayCommand(async param => await ExecuteLoadTablesAsync(), param => true);
            LoadViewsCommand = new RelayCommand(async param => await ExecuteLoadViewsAsync(), param => true);
            LoadTableSchemaCommand = new RelayCommand(async param => await ExecuteLoadTableSchemaAsync(),
                                                      param => !string.IsNullOrWhiteSpace(SelectedTableNameForSchema));
            LoadProceduresCommand = new RelayCommand(async param => await ExecuteLoadProceduresAsync(), param => true);
            LoadFunctionsCommand = new RelayCommand(async param => await ExecuteLoadFunctionsAsync(), param => true);
            CallProcedureCommand = new RelayCommand(async param => await ExecuteCallRoutineAsync("procedure"),
                                                    param => !string.IsNullOrWhiteSpace(SelectedRoutineName));
            CallFunctionCommand = new RelayCommand(async param => await ExecuteCallRoutineAsync("function"),
                                                   param => !string.IsNullOrWhiteSpace(SelectedRoutineName));
            AddFilterCommand = new RelayCommand(param => DataQueryFilters.Add(new FilterEntry()), param => true);
            RemoveFilterCommand = new RelayCommand(param => { if (param is FilterEntry filter) DataQueryFilters.Remove(filter); },
                                                   param => param is FilterEntry);
            ReadDataCommand = new RelayCommand(async param => await ExecuteReadDataAsync(),
                                               param => !string.IsNullOrWhiteSpace(DataQueryTableName));
            ReadDataByColumnCommand = new RelayCommand(async param => await ExecuteReadDataByColumnAsync(),
                                                       param => !string.IsNullOrWhiteSpace(DataByColumnTableName) &&
                                                                !string.IsNullOrWhiteSpace(DataByColumnName) &&
                                                                DataByColumnValue != null); // Разрешаем пустую строку для значения
            NextPageCommand = new RelayCommand(async param => { DataQueryOffset += DataQueryLimit; await ExecuteReadDataAsync(); },
                                               param => QueriedDataTable != null && (DataQueryOffset + DataQueryLimit) < DataQueryTotalCount);
            PreviousPageCommand = new RelayCommand(async param => { DataQueryOffset = Math.Max(0, DataQueryOffset - DataQueryLimit); await ExecuteReadDataAsync(); },
                                                   param => QueriedDataTable != null && DataQueryOffset > 0);
        }

        private async Task ExecuteCheckHealthAsync()
        {
            ApiStatusMessage = "Checking health...";
            var (healthInfo, errorMessage) = await _apiService.CheckHealthAsync();
            if (healthInfo != null)
            {
                HealthStatus = $"API Status: {healthInfo.Status}, DB Connection: {healthInfo.Database_Connection}";
                ApiStatusMessage = "Health check successful.";
            }
            else
            {
                HealthStatus = $"Health Check Failed: {errorMessage}";
                ApiStatusMessage = $"Error: {errorMessage}";
            }
        }

        private bool CanExecuteSelectRoutine(object parameter)
        {
            return parameter is ProcedureInfo || parameter is FunctionInfo;
        }

        private void ExecuteSelectRoutine(object parameter)
        {
            if (parameter is ProcedureInfo proc)
            {
                SelectedRoutineName = proc.Name;
                ApiStatusMessage = $"Procedure '{proc.Name}' selected for execution.";
            }
            else if (parameter is FunctionInfo func)
            {
                SelectedRoutineName = func.Name;
                ApiStatusMessage = $"Function '{func.Name}' selected for execution.";
            }
        }

        private async Task ExecuteLoadTablesAsync()
        {
            ApiStatusMessage = "Loading tables...";
            Tables.Clear();
            var (tables, errorMessage) = await _apiService.GetTablesAsync();
            if (tables != null)
            {
                foreach (var table in tables.OrderBy(t => t)) Tables.Add(table);
                ApiStatusMessage = Tables.Any() ? $"Loaded {Tables.Count} tables." : "No tables found.";
            }
            else
            {
                ApiStatusMessage = $"Error loading tables: {errorMessage}";
            }
        }

        private async Task ExecuteLoadViewsAsync()
        {
            ApiStatusMessage = "Loading views...";
            Views.Clear();
            var (views, errorMessage) = await _apiService.GetViewsAsync();
            if (views != null)
            {
                foreach (var view in views.OrderBy(v => v)) Views.Add(view);
                ApiStatusMessage = Views.Any() ? $"Loaded {Views.Count} views." : "No views found.";
            }
            else
            {
                ApiStatusMessage = $"Error loading views: {errorMessage}";
            }
        }

        private async Task ExecuteLoadTableSchemaAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedTableNameForSchema))
            {
                ApiStatusMessage = "Please select or enter a table/view name.";
                SelectedTableSchema = null;
                return;
            }
            ApiStatusMessage = $"Loading schema for {SelectedTableNameForSchema}...";
            SelectedTableSchema = null;
            var (schema, errorMessage) = await _apiService.GetTableOrViewSchemaAsync(SelectedTableNameForSchema);
            if (schema != null)
            {
                SelectedTableSchema = schema;
                ApiStatusMessage = $"Schema loaded for {SelectedTableNameForSchema}.";
            }
            else
            {
                ApiStatusMessage = $"Error loading schema for {SelectedTableNameForSchema}: {errorMessage}";
                MessageBox.Show($"Could not load schema: {errorMessage}", "Schema Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteLoadProceduresAsync()
        {
            ApiStatusMessage = "Loading procedures...";
            Procedures.Clear();
            var (procedures, errorMessage) = await _apiService.GetProceduresAsync();
            if (procedures != null)
            {
                foreach (var proc in procedures.OrderBy(p => p.Name)) Procedures.Add(proc);
                ApiStatusMessage = Procedures.Any() ? $"Loaded {Procedures.Count} procedures." : "No procedures found.";
            }
            else
            {
                ApiStatusMessage = $"Error loading procedures: {errorMessage}";
            }
        }

        private async Task ExecuteLoadFunctionsAsync()
        {
            ApiStatusMessage = "Loading functions...";
            Functions.Clear();
            var (functions, errorMessage) = await _apiService.GetFunctionsAsync();
            if (functions != null)
            {
                foreach (var func in functions.OrderBy(f => f.Name)) Functions.Add(func);
                ApiStatusMessage = Functions.Any() ? $"Loaded {Functions.Count} functions." : "No functions found.";
            }
            else
            {
                ApiStatusMessage = $"Error loading functions: {errorMessage}";
            }
        }

        private async Task ExecuteCallRoutineAsync(string routineType)
        {
            if (string.IsNullOrWhiteSpace(SelectedRoutineName))
            {
                ApiStatusMessage = "Please select a routine name.";
                RoutineCallResult = "No routine selected.";
                return;
            }

            ApiStatusMessage = $"Calling {routineType} {SelectedRoutineName}...";
            RoutineCallResult = "";

            RoutineCallArgs payload = new RoutineCallArgs();
            try
            {
                if (!string.IsNullOrWhiteSpace(RoutineArgumentsInput))
                {
                    var parsedArgs = JsonSerializer.Deserialize<List<object>>(RoutineArgumentsInput, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsedArgs != null)
                    {
                        // Попытка преобразовать числа из double (стандарт для JsonSerializer) в int/long, если это возможно
                        for (int i = 0; i < parsedArgs.Count; i++)
                        {
                            if (parsedArgs[i] is JsonElement el && el.ValueKind == JsonValueKind.Number)
                            {
                                if (el.TryGetInt64(out long lVal)) parsedArgs[i] = lVal;
                                // else if (el.TryGetDouble(out double dVal)) parsedArgs[i] = dVal; // Уже double
                            }
                        }
                        payload.Args = parsedArgs;
                    }
                }
            }
            catch (JsonException jsonEx)
            {
                ApiStatusMessage = $"Warning: Could not parse arguments as JSON array: {jsonEx.Message}. Sending empty args.";
                RoutineCallResult = $"Argument parsing error: {jsonEx.Message}";
                // Можно не прерывать, а попробовать отправить с пустыми аргументами, если это имеет смысл
                // return; 
            }
            catch (Exception ex)
            {
                ApiStatusMessage = $"Error parsing arguments: {ex.Message}";
                RoutineCallResult = $"Error parsing arguments: {ex.Message}";
                return;
            }

            var (response, errorMessage) = await _apiService.CallRoutineAsync(routineType, SelectedRoutineName, payload);

            if (response != null)
            {
                ApiStatusMessage = $"{routineType} '{SelectedRoutineName}' called successfully.";
                if (response.Result.ValueKind != JsonValueKind.Undefined && response.Result.ValueKind != JsonValueKind.Null)
                {
                    RoutineCallResult = JsonSerializer.Serialize(response.Result, new JsonSerializerOptions { WriteIndented = true });
                }
                else if (!string.IsNullOrEmpty(response.Status))
                {
                    RoutineCallResult = $"Status: {response.Status}";
                    if (response.ArgsUsed != null && response.ArgsUsed.Any())
                    {
                        RoutineCallResult += $"\nArgs Used: {JsonSerializer.Serialize(response.ArgsUsed, new JsonSerializerOptions { WriteIndented = true })}";
                    }
                }
                else
                {
                    RoutineCallResult = "Routine executed. No specific result data or status returned in the expected format.";
                }
            }
            else
            {
                ApiStatusMessage = $"Error calling {routineType} {SelectedRoutineName}: {errorMessage}";
                RoutineCallResult = $"Error: {errorMessage}";
            }
        }

        private async Task ExecuteReadDataAsync()
        {
            if (string.IsNullOrWhiteSpace(DataQueryTableName))
            {
                ApiStatusMessage = "Please enter a table/view name for data query.";
                QueriedDataTable = null;
                DataQueryTotalCount = 0;
                UpdatePaginationCommands();
                return;
            }

            ApiStatusMessage = $"Querying data from {DataQueryTableName}...";
            QueriedDataTable = null;

            var filtersDict = new Dictionary<string, string>();
            foreach (var filter in DataQueryFilters)
            {
                if (!string.IsNullOrWhiteSpace(filter.Column) && filter.Value != null) // Разрешаем пустую строку для Value
                {
                    string filterKey = filter.Operator.ToLower() == "eq" ? filter.Column : $"{filter.Column}__{filter.Operator}";
                    filtersDict[filterKey] = filter.Value;
                }
            }

            var (response, errorMessage) = await _apiService.ReadItemsAsync(
                DataQueryTableName,
                DataQueryLimit,
                DataQueryOffset,
                DataQuerySortBy,
                DataQueryFields,
                filtersDict
            );

            if (response != null && response.Data != null)
            {
                DataQueryTotalCount = response.TotalCount;
                DataQueryOffset = response.Offset; // Обновляем offset из ответа API
                DataQueryLimit = response.Limit;   // Обновляем limit из ответа API
                QueriedDataTable = ConvertJsonElementsToDataTable(response.Data);
                ApiStatusMessage = $"Data loaded from {DataQueryTableName}. Showing {response.Data.Count} of {response.TotalCount} items. (Offset: {response.Offset}, Limit: {response.Limit})";
            }
            else
            {
                DataQueryTotalCount = 0;
                ApiStatusMessage = $"Error querying data from {DataQueryTableName}: {errorMessage}";
                MessageBox.Show($"Could not query data: {errorMessage}", "Data Query Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            UpdatePaginationCommands();
        }

        private async Task ExecuteReadDataByColumnAsync()
        {
            if (string.IsNullOrWhiteSpace(DataByColumnTableName) ||
               string.IsNullOrWhiteSpace(DataByColumnName) ||
               DataByColumnValue == null) // Разрешаем пустую строку
            {
                ApiStatusMessage = "Please fill all required fields for 'Read by Column Value'.";
                QueriedByColumnDataTable = null;
                return;
            }
            ApiStatusMessage = $"Querying data from {DataByColumnTableName} by {DataByColumnName}='{DataByColumnValue}'...";
            QueriedByColumnDataTable = null;

            var (items, errorMessage) = await _apiService.ReadItemsByColumnValueAsync(
                DataByColumnTableName,
                DataByColumnName,
                DataByColumnValue
            );

            if (items != null)
            {
                QueriedByColumnDataTable = ConvertJsonElementsToDataTable(items);
                ApiStatusMessage = $"Data loaded from {DataByColumnTableName} where {DataByColumnName} = '{DataByColumnValue}'. Found {items.Count} items.";
            }
            else
            {
                ApiStatusMessage = $"Error querying data by column: {errorMessage}";
                MessageBox.Show($"Could not query data by column: {errorMessage}", "Data Query Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DataTable ConvertJsonElementsToDataTable(List<JsonElement> elements)
        {
            var dataTable = new DataTable();
            if (elements == null || !elements.Any())
            {
                return dataTable;
            }

            var firstElement = elements.First();
            if (firstElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in firstElement.EnumerateObject())
                {
                    Type columnType = typeof(object); // По умолчанию object, чтобы избежать ошибок преобразования
                    // Можно добавить более точное определение типа, если это критично
                    // if (property.Value.ValueKind == JsonValueKind.Number) columnType = typeof(double);
                    // else if (property.Value.ValueKind == JsonValueKind.True || property.Value.ValueKind == JsonValueKind.False) columnType = typeof(bool);
                    // else columnType = typeof(string); // По умолчанию строка для всего остального
                    dataTable.Columns.Add(property.Name, columnType);
                }
            }
            else
            {
                dataTable.Columns.Add("Value", typeof(string));
            }

            foreach (var element in elements)
            {
                var row = dataTable.NewRow();
                if (element.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in element.EnumerateObject())
                    {
                        if (dataTable.Columns.Contains(property.Name))
                        {
                            if (property.Value.ValueKind == JsonValueKind.Null)
                            {
                                row[property.Name] = DBNull.Value;
                            }
                            else
                            {
                                // Простое присваивание, DataGrid попытается отобразить как строку, если тип object
                                // Для более точного отображения типов (числа, даты) можно оставить логику определения типа колонки выше
                                // или реализовать IValueConverter для DataGrid колонок.
                                row[property.Name] = GetValueFromJsonElement(property.Value);
                            }
                        }
                    }
                }
                else
                {
                    if (dataTable.Columns.Contains("Value"))
                    {
                        row["Value"] = GetValueFromJsonElement(element);
                    }
                }
                dataTable.Rows.Add(row);
            }
            return dataTable;
        }

        private object GetValueFromJsonElement(JsonElement jsonElement)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.String:
                    return jsonElement.GetString();
                case JsonValueKind.Number:
                    if (jsonElement.TryGetInt64(out long l)) return l;
                    if (jsonElement.TryGetDouble(out double d)) return d;
                    return jsonElement.GetRawText(); // fallback
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return DBNull.Value;
                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    return jsonElement.ToString(); // Отображаем как JSON строку
                default:
                    return jsonElement.ToString();
            }
        }

        private void UpdatePaginationCommands()
        {
            (NextPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PreviousPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // Обновление состояния команд при изменении зависимых свойств
            switch (propertyName)
            {
                case nameof(SelectedTableNameForSchema):
                    (LoadTableSchemaCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    break;
                case nameof(SelectedRoutineName):
                    (CallProcedureCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (CallFunctionCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    break;
                case nameof(DataQueryTableName):
                    (ReadDataCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    break;
                case nameof(DataByColumnTableName):
                case nameof(DataByColumnName):
                case nameof(DataByColumnValue):
                    (ReadDataByColumnCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    break;
                case nameof(DataQueryTotalCount):
                case nameof(DataQueryOffset):
                case nameof(DataQueryLimit):
                case nameof(QueriedDataTable): // Также обновляем при появлении данных
                    UpdatePaginationCommands();
                    break;
            }
        }
    }
}