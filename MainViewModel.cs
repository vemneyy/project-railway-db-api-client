// ViewModels/MainViewModel.cs
using ApiManagerApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions; // Для парсинга сигнатуры
using System.Windows;
using System.Windows.Input;

namespace ApiManagerApp.ViewModels
{

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

        private TableSchemaDetail? _selectedTableSchema;
        public TableSchemaDetail? SelectedTableSchema
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


        private string? _selectedRoutineName;
        public string? SelectedRoutineName
        {
            get => _selectedRoutineName;
            set { _selectedRoutineName = value; OnPropertyChanged(); }
        }

        private string? _routineArgumentsInput;
        public string? RoutineArgumentsInput
        {
            get => _routineArgumentsInput;
            set { _routineArgumentsInput = value; OnPropertyChanged(); }
        }

        private string? _routineCallResult;
        public string? RoutineCallResult
        {
            get => _routineCallResult;
            set { _routineCallResult = value; OnPropertyChanged(); }
        }

        private string? _apiStatusMessage;
        public string? ApiStatusMessage
        {
            get => _apiStatusMessage;
            set { _apiStatusMessage = value; OnPropertyChanged(); }
        }

        private string? _dataQueryTableName;
        public string? DataQueryTableName
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

        private DataTable? _queriedDataTable;
        public DataTable? QueriedDataTable
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
        private DataTable? _queriedByColumnDataTable;
        public DataTable? QueriedByColumnDataTable
        {
            get => _queriedByColumnDataTable;
            set { _queriedByColumnDataTable = value; OnPropertyChanged(); }
        }

        private DataTable? _routineCallDataTableResult; // Новое свойство для табличного результата
        public DataTable? RoutineCallDataTableResult
        {
            get => _routineCallDataTableResult;
            set { SetProperty(ref _routineCallDataTableResult, value); }
        }


        private object _selectedRoutineItem;
        public object SelectedRoutineItem
        {
            get => _selectedRoutineItem;
            set
            {
                if (SetProperty(ref _selectedRoutineItem, value))
                {
                    RoutineCallResult = string.Empty; // Очищаем текстовый результат
                    RoutineCallDataTableResult = null; // Очищаем табличный результат

                    if (value is ProcedureInfo proc)
                    {
                        SelectedRoutineName = proc.Name;
                        GenerateExamplePayloadForProcedure(proc);
                    }
                    else if (value is FunctionInfo func)
                    {
                        SelectedRoutineName = func.Name;
                        GenerateExamplePayloadForFunction(func); // Можно и для функций, если нужно
                    }
                    else
                    {
                        SelectedRoutineName = null;
                        RoutineArgumentsInput = "[]"; // Сброс
                    }
                }
            }
        }

        public ICommand CheckHealthCommand { get; }
        public ICommand LoadTablesCommand { get; }
        public ICommand LoadViewsCommand { get; }
        public ICommand LoadTableSchemaCommand { get; }
        public ICommand LoadProceduresCommand { get; }
        public ICommand LoadFunctionsCommand { get; }
        public ICommand CallProcedureCommand { get; }
        public ICommand CallFunctionCommand { get; }
        public ICommand ReadDataCommand { get; }
        public ICommand ReadDataByColumnCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand SelectRoutineCommand { get; }
        public ICommand CallSelectedRoutineCommand { get; }

        // Fix for CS8618: Initialize non-nullable fields and properties in the constructor.

        public MainViewModel()
        {
            _apiService = new ApiService(); // Uses default URL from ApiService
            _healthStatus = "Нажмите чтобы получить статус API."; // Initialize _healthStatus
            _selectedTableNameForSchema = string.Empty; // Initialize _selectedTableNameForSchema
            _dataQuerySortBy = string.Empty; // Initialize _dataQuerySortBy
            _dataQueryFields = string.Empty; // Initialize _dataQueryFields
            _dataByColumnTableName = string.Empty;
            _dataByColumnName = string.Empty;
            _dataByColumnValue = string.Empty;
            _selectedRoutineItem = new object();
            SelectRoutineCommand = new RelayCommand(param => { /* Command logic */ }, param => true);

            ApiStatusMessage = "Готово";
            RoutineArgumentsInput = "[]";
            DataQueryTableName = "";
            DataByColumnTableName = "";

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
            ReadDataCommand = new RelayCommand(async param => await ExecuteReadDataAsync(),
                                               param => !string.IsNullOrWhiteSpace(DataQueryTableName));
            ReadDataByColumnCommand = new RelayCommand(async param => await ExecuteReadDataByColumnAsync(),
                                                       param => !string.IsNullOrWhiteSpace(DataByColumnTableName) &&
                                                                !string.IsNullOrWhiteSpace(DataByColumnName) &&
                                                                DataByColumnValue != null); // Allow empty string for value
            NextPageCommand = new RelayCommand(async param => { DataQueryOffset += DataQueryLimit; await ExecuteReadDataAsync(); },
                                               param => QueriedDataTable != null && (DataQueryOffset + DataQueryLimit) < DataQueryTotalCount);
            PreviousPageCommand = new RelayCommand(async param => { DataQueryOffset = Math.Max(0, DataQueryOffset - DataQueryLimit); await ExecuteReadDataAsync(); },
                                                   param => QueriedDataTable != null && DataQueryOffset > 0);

            CallSelectedRoutineCommand = new RelayCommand(
               async param => await ExecuteCallSelectedRoutineAsync(),
               param => SelectedRoutineItem != null && !string.IsNullOrWhiteSpace(SelectedRoutineName)
           );
        }

        private void GenerateExamplePayloadForProcedure(ProcedureInfo proc)
        {
            if (proc == null || string.IsNullOrWhiteSpace(proc.ArgumentsSignature))
            {
                RoutineArgumentsInput = "[]";
                return;
            }

            GeneratePayload(proc.ArgumentsSignature);
        }
        private void GenerateExamplePayloadForFunction(FunctionInfo func)
        {
            if (func == null || string.IsNullOrWhiteSpace(func.ArgumentsSignature))
            {
                RoutineArgumentsInput = "[]";
                return;
            }

            GeneratePayload(func.ArgumentsSignature);
        }

        private void GeneratePayload(string argumentsSignature)
        {
            if (string.IsNullOrWhiteSpace(argumentsSignature))
            {
                RoutineArgumentsInput = "[]";
                return;
            }

            // Более надежный парсинг аргументов с учетом возможных пробелов и ключевого слова IN/OUT/INOUT
            // Пример: "IN p_last_name character varying, p_first_name character varying"
            // или "p_id integer, p_name text"
            // или "OUT result_code integer, IN p_input text"
            var argDefinitions = new List<Tuple<string, string>>(); // Имя параметра, Тип параметра
            var regex = new Regex(@"(?:(IN|OUT|INOUT)\s+)?([a-zA-Z0-9_]+)\s+([^,]+(?:\[\])?)(?:,|$)", RegexOptions.IgnoreCase);
            MatchCollection matches = regex.Matches(argumentsSignature);

            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 4)
                {
                    string direction = match.Groups[1].Value.ToUpperInvariant();
                    string name = match.Groups[2].Value;
                    string type = match.Groups[3].Value.Trim();

                    if (direction != "OUT") // Включаем IN и INOUT (или если направление не указано)
                    {
                        argDefinitions.Add(Tuple.Create(name, type));
                    }
                }
            }

            if (!argDefinitions.Any())
            {
                RoutineArgumentsInput = "[]";
                return;
            }

            var exampleValues = new List<object>();
            foreach (var argDef in argDefinitions)
            {
                string paramName = argDef.Item1.ToLowerInvariant();
                string paramType = argDef.Item2.ToLowerInvariant();
                object exampleValue = "string_value"; // Значение по умолчанию

                // Определение значения по типу
                if (paramType.Contains("int") || paramType.Contains("serial") || paramType.Contains("smallint") || paramType.Contains("bigint"))
                    exampleValue = 0;
                else if (paramType.Contains("numeric") || paramType.Contains("decimal") || paramType.Contains("real") || paramType.Contains("double"))
                    exampleValue = 0.0;
                else if (paramType.Contains("bool"))
                    exampleValue = false;
                else if (paramType.Contains("date")) // Для типа DATE
                    exampleValue = DateTime.Now.ToString("yyyy-MM-dd");
                else if (paramType.Contains("timestamp") || paramType.Contains("time")) // Для TIMESTAMP или TIME
                    exampleValue = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                else if (paramType.Contains("uuid"))
                    exampleValue = Guid.NewGuid().ToString();
                else if (paramType.Contains("json"))
                    exampleValue = new JsonObject(); // Пустой JSON объект как пример
                else if (paramType.Contains("bytea"))
                    exampleValue = "base64_encoded_bytes";
                else if (paramType.EndsWith("[]")) // Массив
                    exampleValue = new List<object>(); // Пустой массив как пример


                // Определение значения по имени параметра (переопределяет значение по типу, если найдено совпадение)
                if (paramName.Contains("name"))
                {
                    if (paramName.Contains("first")) exampleValue = "John";
                    else if (paramName.Contains("last")) exampleValue = "Doe";
                    else if (paramName.Contains("middle")) exampleValue = "M";
                    else if (paramName.Contains("user")) exampleValue = "username";
                    else exampleValue = "Default Name";
                }
                else if (paramName.Contains("email"))
                    exampleValue = "email@example.com";
                else if (paramName.Contains("phone"))
                    exampleValue = "+1234567890";
                else if (paramName.Contains("password"))
                    exampleValue = "P@$$wOrd";
                else if (paramName.Contains("date")) // Более точное для имен, содержащих "date"
                {
                    if (paramName.Contains("birth")) exampleValue = new DateTime(1990, 1, 1).ToString("yyyy-MM-dd");
                    else if (paramName.Contains("start")) exampleValue = DateTime.Now.ToString("yyyy-MM-dd");
                    else if (paramName.Contains("end")) exampleValue = DateTime.Now.AddDays(7).ToString("yyyy-MM-dd");
                    else if (paramName.Contains("hire")) exampleValue = DateTime.Now.ToString("yyyy-MM-dd");
                    else if (paramType == "date") exampleValue = DateTime.Now.ToString("yyyy-MM-dd"); // если тип date, но имя не уточнено
                    else exampleValue = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"); // если тип timestamp, но имя не уточнено
                }
                else if (paramName.Contains("id"))
                {
                    if (paramType.Contains("int") || paramType.Contains("serial")) exampleValue = 1; // Для ID обычно целое число
                    else if (paramType.Contains("uuid")) exampleValue = Guid.NewGuid().ToString();
                    else exampleValue = "id_value";
                }
                else if (paramName.Contains("status"))
                    exampleValue = "active";
                else if (paramName.Contains("passport"))
                    exampleValue = "AB123456";
                else if (paramName.Contains("p_plain_password")) // Конкретный случай из вашего примера
                    exampleValue = "SecurePassword123!";
                else if (paramName.EndsWith("_str") && paramType.Contains("char")) // Если параметр оканчивается на _str и тип character varying
                {
                    if (paramName.Contains("date")) exampleValue = DateTime.Now.ToString("yyyy-MM-dd"); // Если имя содержит date, но тип varchar
                    // Можно добавить другие эвристики для _str параметров
                }


                exampleValues.Add(exampleValue);
            }

            try
            {
                // ИЗМЕНЕНИЕ ЗДЕСЬ: Добавляем Encoder для корректного отображения кириллицы
                RoutineArgumentsInput = JsonSerializer.Serialize(exampleValues,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // <--- Вот это важно!
                    });
            }
            catch
            {
                RoutineArgumentsInput = "[]"; // В случае ошибки сериализации
            }
        }

        private async Task ExecuteCallSelectedRoutineAsync()
        {
            if (SelectedRoutineItem == null || string.IsNullOrWhiteSpace(SelectedRoutineName))
            {
                ApiStatusMessage = "Please select a routine.";
                RoutineCallResult = "No routine selected.";
                RoutineCallDataTableResult = null;
                return;
            }

            string routineType = SelectedRoutineItem is ProcedureInfo ? "процедура" : "функция";
            ApiStatusMessage = $"Calling {routineType} {SelectedRoutineName}...";
            RoutineCallResult = "";
            RoutineCallDataTableResult = null;

            RoutineCallArgs payload = new RoutineCallArgs();
            try
            {
                if (!string.IsNullOrWhiteSpace(RoutineArgumentsInput))
                {
                    var parsedArgs = JsonSerializer.Deserialize<List<object>>(RoutineArgumentsInput, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsedArgs != null)
                    {
                        for (int i = 0; i < parsedArgs.Count; i++)
                        {
                            if (parsedArgs[i] is JsonElement el && el.ValueKind == JsonValueKind.Number)
                            {
                                if (el.TryGetInt64(out long lVal)) parsedArgs[i] = lVal;
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
                    // Проверяем, является ли результат табличным для функций
                    if (SelectedRoutineItem is FunctionInfo funcInfo &&
                        (funcInfo.PreciseReturnType?.ToUpperInvariant().StartsWith("TABLE") == true ||
                         funcInfo.PreciseReturnType?.ToUpperInvariant().Contains("SETOF RECORD") == true ||
                         funcInfo.PreciseReturnType?.ToUpperInvariant().Contains("RECORD[]") == true)) // Добавим RECORD[]
                    {
                        if (response.Result.ValueKind == JsonValueKind.Array)
                        {
                            try
                            {
                                var elements = JsonSerializer.Deserialize<List<JsonElement>>(response.Result.GetRawText());
                                RoutineCallDataTableResult = ConvertJsonElementsToDataTable(elements); // Используем существующий метод
                                RoutineCallResult = $"Table result displayed below. ({elements?.Count ?? 0} rows)";
                            }
                            catch (Exception ex)
                            {
                                RoutineCallResult = $"Error converting table result: {ex.Message}\nRaw JSON:\n{JsonSerializer.Serialize(response.Result, new JsonSerializerOptions { WriteIndented = true })}";
                                RoutineCallDataTableResult = null;
                            }
                        }
                        else
                        {
                            RoutineCallResult = $"Expected array for table result, but got {response.Result.ValueKind}.\nRaw JSON:\n{JsonSerializer.Serialize(response.Result, new JsonSerializerOptions { WriteIndented = true })}";
                            RoutineCallDataTableResult = null;
                        }
                    }
                    else // Обычный (не табличный) результат или процедура
                    {
                        RoutineCallResult = JsonSerializer.Serialize(response.Result, new JsonSerializerOptions { WriteIndented = true });
                        RoutineCallDataTableResult = null;
                    }
                }
                else if (!string.IsNullOrEmpty(response.Status)) // Для процедур
                {
                    RoutineCallResult = $"Status: {response.Status}";
                    if (response.ArgsUsed != null && response.ArgsUsed.Any())
                    {
                        RoutineCallResult += $"\nArgs Used: {JsonSerializer.Serialize(response.ArgsUsed, new JsonSerializerOptions { WriteIndented = true })}";
                    }
                    RoutineCallDataTableResult = null;
                }
                else
                {
                    RoutineCallResult = "Routine executed. No specific result data or status returned.";
                    RoutineCallDataTableResult = null;
                }
            }
            else
            {
                ApiStatusMessage = $"Error calling {routineType} {SelectedRoutineName}: {errorMessage}";
                RoutineCallResult = $"Error: {errorMessage}";
                RoutineCallDataTableResult = null;
            }
        }

        private async Task ExecuteCheckHealthAsync()
        {
            ApiStatusMessage = "Проверка соединения...";
            // Вызываем новый метод с измерением задержки
            var (healthInfo, latency, errorMessage) = await _apiService.CheckHealthAsyncWithLatency();

            string latencyString = $" (Задержка: {latency.TotalMilliseconds:F0} ms)"; // Форматируем миллисекунды

            if (healthInfo != null)
            {
                HealthStatus = $"API статус: {healthInfo.Status}, DB соединение: {healthInfo.DatabaseConnection}{latencyString}";
                ApiStatusMessage = "Проверка соединения успешна.";
            }
            else
            {
                // Если ошибка, но задержка была измерена (например, сервер ответил ошибкой)
                if (latency > TimeSpan.Zero || (errorMessage?.Contains("timed out") ?? false))
                {
                    HealthStatus = $"Проверка соединения завершилась ошибкой: {errorMessage}{latencyString}";
                }
                else
                {
                    HealthStatus = $"Проверка соединения завершилась ошибкой: {errorMessage}";
                }
                ApiStatusMessage = $"Ошибка: {errorMessage}";
            }
        }


        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
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

        private DataTable ConvertJsonElementsToDataTable(List<JsonElement>? elements)
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
                    Type columnType = typeof(object); // Default to object to avoid conversion errors
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
                    return jsonElement.GetString() ?? string.Empty;
                case JsonValueKind.Number:
                    if (jsonElement.TryGetInt64(out long l)) return l;
                    if (jsonElement.TryGetDouble(out double d)) return d;
                    return jsonElement.GetRawText();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return DBNull.Value;
                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    return jsonElement.ToString();
                default:
                    return jsonElement.ToString();
            }
        }

        private void UpdatePaginationCommands()
        {
            (NextPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PreviousPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            switch (propertyName)
            {
                case nameof(SelectedTableNameForSchema):
                    (LoadTableSchemaCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    // Если DataQueryTableName и DataByColumnTableName привязаны к SelectedTableNameForSchema,
                    // то их OnPropertyChanged вызовет обновление соответствующих команд.
                    break;
                case nameof(SelectedRoutineItem): // Обновлено
                case nameof(SelectedRoutineName): // SelectedRoutineName обновляется из SelectedRoutineItem
                    (CallSelectedRoutineCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    // ApiStatusMessage обновляется в сеттере SelectedRoutineItem
                    Debug.WriteLine($"SelectedRoutineName changed to: {SelectedRoutineName}");
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
                case nameof(QueriedDataTable):
                    UpdatePaginationCommands();
                    break;
            }
        }
    }
}