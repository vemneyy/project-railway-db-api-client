using ApiManagerApp.Classes;
using ApiManagerApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
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
            set { _dataQueryLimit = value > 0 ? value : 1; OnPropertyChanged(); }
        }

        private int _dataQueryOffset = 0;
        public int DataQueryOffset
        {
            get => _dataQueryOffset;
            set { _dataQueryOffset = value >= 0 ? value : 0; OnPropertyChanged(); }
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

        private DataTable? _routineCallDataTableResult;
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
                    RoutineCallResult = string.Empty;
                    RoutineCallDataTableResult = null;

                    if (value is ProcedureInfo proc)
                    {
                        SelectedRoutineName = proc.Name;
                        GenerateExamplePayloadForProcedure(proc);
                    }
                    else if (value is FunctionInfo func)
                    {
                        SelectedRoutineName = func.Name;
                        GenerateExamplePayloadForFunction(func);
                    }
                    else
                    {
                        SelectedRoutineName = null;
                        RoutineArgumentsInput = "[]";
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
        public MainViewModel()
        {
            _apiService = new ApiService();
            _healthStatus = "Нажмите, чтобы получить статус API.";
            _selectedTableNameForSchema = string.Empty;
            _dataQuerySortBy = string.Empty;
            _dataQueryFields = string.Empty;
            _dataByColumnTableName = string.Empty;
            _dataByColumnName = string.Empty;
            _dataByColumnValue = string.Empty;
            _selectedRoutineItem = new object();
            SelectRoutineCommand = new RelayCommand(param => { }, param => true);

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
            CallProcedureCommand = new RelayCommand(async param => await ExecuteCallRoutineAsync("процедура"),
                                                    param => !string.IsNullOrWhiteSpace(SelectedRoutineName));
            CallFunctionCommand = new RelayCommand(async param => await ExecuteCallRoutineAsync("функция"),
                                                   param => !string.IsNullOrWhiteSpace(SelectedRoutineName));
            ReadDataCommand = new RelayCommand(async param => await ExecuteReadDataAsync(),
                                               param => !string.IsNullOrWhiteSpace(DataQueryTableName));
            ReadDataByColumnCommand = new RelayCommand(async param => await ExecuteReadDataByColumnAsync(),
                                                       param => !string.IsNullOrWhiteSpace(DataByColumnTableName) &&
                                                                !string.IsNullOrWhiteSpace(DataByColumnName) &&
                                                                DataByColumnValue != null);
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

            var argDefinitions = new List<Tuple<string, string>>();
            var regex = new Regex(@"(?:(IN|OUT|INOUT)\s+)?([a-zA-Z0-9_]+)\s+([^,]+(?:\[\])?)(?:,|$)", RegexOptions.IgnoreCase);
            MatchCollection matches = regex.Matches(argumentsSignature);

            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 4)
                {
                    string direction = match.Groups[1].Value.ToUpperInvariant();
                    string name = match.Groups[2].Value;
                    string type = match.Groups[3].Value.Trim();

                    if (direction != "OUT")
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
                object exampleValue = "строковое_значение";

                if (paramType.Contains("int") || paramType.Contains("serial") || paramType.Contains("smallint") || paramType.Contains("bigint"))
                    exampleValue = 0;
                else if (paramType.Contains("numeric") || paramType.Contains("decimal") || paramType.Contains("real") || paramType.Contains("double"))
                    exampleValue = 0.0;
                else if (paramType.Contains("bool"))
                    exampleValue = false;
                else if (paramType.Contains("date"))
                    exampleValue = DateTime.Now.ToString("yyyy-MM-dd");
                else if (paramType.Contains("timestamp") || paramType.Contains("time"))
                    exampleValue = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                else if (paramType.Contains("uuid"))
                    exampleValue = Guid.NewGuid().ToString();
                else if (paramType.Contains("json"))
                    exampleValue = new JsonObject();
                else if (paramType.Contains("bytea"))
                    exampleValue = "base64_кодированные_байты";
                else if (paramType.EndsWith("[]"))
                    exampleValue = new List<object>();


                if (paramName.Contains("name"))
                {
                    if (paramName.Contains("first")) exampleValue = "Иван";
                    else if (paramName.Contains("last")) exampleValue = "Иванов";
                    else if (paramName.Contains("middle")) exampleValue = "И";
                    else if (paramName.Contains("user")) exampleValue = "имя_пользователя";
                    else exampleValue = "Имя по умолчанию";
                }
                else if (paramName.Contains("email"))
                    exampleValue = "email@example.com";
                else if (paramName.Contains("phone"))
                    exampleValue = "+79123456789";
                else if (paramName.Contains("password"))
                    exampleValue = "Пароль123!";
                else if (paramName.Contains("date"))
                {
                    if (paramName.Contains("birth")) exampleValue = new DateTime(1990, 1, 1).ToString("yyyy-MM-dd");
                    else if (paramName.Contains("start")) exampleValue = DateTime.Now.ToString("yyyy-MM-dd");
                    else if (paramName.Contains("end")) exampleValue = DateTime.Now.AddDays(7).ToString("yyyy-MM-dd");
                    else if (paramName.Contains("hire")) exampleValue = DateTime.Now.ToString("yyyy-MM-dd");
                    else if (paramType == "date") exampleValue = DateTime.Now.ToString("yyyy-MM-dd");
                    else exampleValue = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                }
                else if (paramName.Contains("id"))
                {
                    if (paramType.Contains("int") || paramType.Contains("serial")) exampleValue = 1;
                    else if (paramType.Contains("uuid")) exampleValue = Guid.NewGuid().ToString();
                    else exampleValue = "id_значение";
                }
                else if (paramName.Contains("status"))
                    exampleValue = "активный";
                else if (paramName.Contains("passport"))
                    exampleValue = "1234 567890";
                else if (paramName.Contains("p_plain_password"))
                    exampleValue = "НадежныйПароль123!";
                else if (paramName.EndsWith("_str") && paramType.Contains("char"))
                {
                    if (paramName.Contains("date")) exampleValue = DateTime.Now.ToString("yyyy-MM-dd");
                }

                exampleValues.Add(exampleValue);
            }

            try
            {
                RoutineArgumentsInput = JsonSerializer.Serialize(exampleValues,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });
            }
            catch
            {
                RoutineArgumentsInput = "[]";
            }
        }

        private async Task ExecuteCallSelectedRoutineAsync()
        {
            if (SelectedRoutineItem == null || string.IsNullOrWhiteSpace(SelectedRoutineName))
            {
                ApiStatusMessage = "Выберите подпрограмму.";
                RoutineCallResult = "Подпрограмма не выбрана.";
                RoutineCallDataTableResult = null;
                return;
            }

            string routineType = SelectedRoutineItem is ProcedureInfo ? "процедура" : "функция";
            ApiStatusMessage = $"Вызов {routineType} {SelectedRoutineName}...";
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
                ApiStatusMessage = $"Предупреждение: Не удалось разобрать аргументы как JSON-массив: {jsonEx.Message}. Отправка пустых аргументов.";
                RoutineCallResult = $"Ошибка разбора аргументов: {jsonEx.Message}";
            }
            catch (Exception ex)
            {
                ApiStatusMessage = $"Ошибка разбора аргументов: {ex.Message}";
                RoutineCallResult = $"Ошибка разбора аргументов: {ex.Message}";
                return;
            }

            var (response, errorMessage) = await _apiService.CallRoutineAsync(routineType, SelectedRoutineName, payload);

            if (response != null)
            {
                ApiStatusMessage = $"{routineType} '{SelectedRoutineName}' успешно вызвана.";
                if (response.Result.ValueKind != JsonValueKind.Undefined && response.Result.ValueKind != JsonValueKind.Null)
                {
                    if (SelectedRoutineItem is FunctionInfo funcInfo &&
                        (funcInfo.PreciseReturnType?.ToUpperInvariant().StartsWith("TABLE") == true ||
                         funcInfo.PreciseReturnType?.ToUpperInvariant().Contains("SETOF RECORD") == true ||
                         funcInfo.PreciseReturnType?.ToUpperInvariant().Contains("RECORD[]") == true))
                    {
                        if (response.Result.ValueKind == JsonValueKind.Array)
                        {
                            try
                            {
                                var elements = JsonSerializer.Deserialize<List<JsonElement>>(response.Result.GetRawText());
                                RoutineCallDataTableResult = ConvertJsonElementsToDataTable(elements);
                                RoutineCallResult = $"Табличный результат показан ниже. ({elements?.Count ?? 0} строк)";
                            }
                            catch (Exception ex)
                            {
                                RoutineCallResult = $"Ошибка преобразования табличного результата: {ex.Message}\nИсходный JSON:\n{JsonSerializer.Serialize(response.Result, new JsonSerializerOptions { WriteIndented = true })}";
                                RoutineCallDataTableResult = null;
                            }
                        }
                        else
                        {
                            RoutineCallResult = $"Ожидался массив для табличного результата, но получен {response.Result.ValueKind}.\nИсходный JSON:\n{JsonSerializer.Serialize(response.Result, new JsonSerializerOptions { WriteIndented = true })}";
                            RoutineCallDataTableResult = null;
                        }
                    }
                    else
                    {
                        RoutineCallResult = JsonSerializer.Serialize(response.Result, new JsonSerializerOptions { WriteIndented = true });
                        RoutineCallDataTableResult = null;
                    }
                }
                else if (!string.IsNullOrEmpty(response.Status))
                {
                    RoutineCallResult = $"Статус: {response.Status}";
                    if (response.ArgsUsed != null && response.ArgsUsed.Any())
                    {
                        RoutineCallResult += $"\nИспользованные аргументы: {JsonSerializer.Serialize(response.ArgsUsed, new JsonSerializerOptions { WriteIndented = true })}";
                    }
                    RoutineCallDataTableResult = null;
                }
                else
                {
                    RoutineCallResult = "Подпрограмма выполнена. Данных или статуса не возвращено.";
                    RoutineCallDataTableResult = null;
                }
            }
            else
            {
                ApiStatusMessage = $"Ошибка вызова {routineType} {SelectedRoutineName}: {errorMessage}";
                RoutineCallResult = $"Ошибка: {errorMessage}";
                RoutineCallDataTableResult = null;
            }
        }

        private async Task ExecuteCheckHealthAsync()
        {
            ApiStatusMessage = "Проверка соединения...";
            var (healthInfo, latency, errorMessage) = await _apiService.CheckHealthAsyncWithLatency();

            string latencyString = $" (Задержка: {latency.TotalMilliseconds:F0} мс)";

            if (healthInfo != null)
            {
                HealthStatus = $"Статус API: {healthInfo.Status}, соединение с БД: {healthInfo.DatabaseConnection}{latencyString}";
                ApiStatusMessage = "Проверка соединения успешна.";
            }
            else
            {
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
            ApiStatusMessage = "Загрузка таблиц...";
            Tables.Clear();
            var (tables, errorMessage) = await _apiService.GetTablesAsync();
            if (tables != null)
            {
                foreach (var table in tables.OrderBy(t => t)) Tables.Add(table);
                ApiStatusMessage = Tables.Any() ? $"Загружено {Tables.Count} таблиц." : "Таблицы не найдены.";
            }
            else
            {
                ApiStatusMessage = $"Ошибка загрузки таблиц: {errorMessage}";
            }
        }

        private async Task ExecuteLoadViewsAsync()
        {
            ApiStatusMessage = "Загрузка представлений...";
            Views.Clear();
            var (views, errorMessage) = await _apiService.GetViewsAsync();
            if (views != null)
            {
                foreach (var view in views.OrderBy(v => v)) Views.Add(view);
                ApiStatusMessage = Views.Any() ? $"Загружено {Views.Count} представлений." : "Представления не найдены.";
            }
            else
            {
                ApiStatusMessage = $"Ошибка загрузки представлений: {errorMessage}";
            }
        }

        private async Task ExecuteLoadTableSchemaAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedTableNameForSchema))
            {
                ApiStatusMessage = "Пожалуйста, выберите или введите имя таблицы/представления.";
                SelectedTableSchema = null;
                return;
            }
            ApiStatusMessage = $"Загрузка схемы для {SelectedTableNameForSchema}...";
            SelectedTableSchema = null;
            var (schema, errorMessage) = await _apiService.GetTableOrViewSchemaAsync(SelectedTableNameForSchema);
            if (schema != null)
            {
                SelectedTableSchema = schema;
                ApiStatusMessage = $"Схема загружена для {SelectedTableNameForSchema}.";
            }
            else
            {
                ApiStatusMessage = $"Ошибка загрузки схемы для {SelectedTableNameForSchema}: {errorMessage}";
                MessageBox.Show($"Не удалось загрузить схему: {errorMessage}", "Ошибка схемы", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteLoadProceduresAsync()
        {
            ApiStatusMessage = "Загрузка процедур...";
            Procedures.Clear();
            var (procedures, errorMessage) = await _apiService.GetProceduresAsync();
            if (procedures != null)
            {
                foreach (var proc in procedures.OrderBy(p => p.Name)) Procedures.Add(proc);
                ApiStatusMessage = Procedures.Any() ? $"Загружено {Procedures.Count} процедур." : "Процедуры не найдены.";
            }
            else
            {
                ApiStatusMessage = $"Ошибка загрузки процедур: {errorMessage}";
            }
        }

        private async Task ExecuteLoadFunctionsAsync()
        {
            ApiStatusMessage = "Загрузка функций...";
            Functions.Clear();
            var (functions, errorMessage) = await _apiService.GetFunctionsAsync();
            if (functions != null)
            {
                foreach (var func in functions.OrderBy(f => f.Name)) Functions.Add(func);
                ApiStatusMessage = Functions.Any() ? $"Загружено {Functions.Count} функций." : "Функции не найдены.";
            }
            else
            {
                ApiStatusMessage = $"Ошибка загрузки функций: {errorMessage}";
            }
        }

        private async Task ExecuteCallRoutineAsync(string routineType)
        {
            if (string.IsNullOrWhiteSpace(SelectedRoutineName))
            {
                ApiStatusMessage = "Пожалуйста, выберите имя подпрограммы.";
                RoutineCallResult = "Подпрограмма не выбрана.";
                return;
            }

            ApiStatusMessage = $"Вызов {routineType} {SelectedRoutineName}...";
            RoutineCallResult = "";

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
                ApiStatusMessage = $"Предупреждение: Не удалось разобрать аргументы как JSON-массив: {jsonEx.Message}. Отправка пустых аргументов.";
                RoutineCallResult = $"Ошибка разбора аргументов: {jsonEx.Message}";
            }
            catch (Exception ex)
            {
                ApiStatusMessage = $"Ошибка разбора аргументов: {ex.Message}";
                RoutineCallResult = $"Ошибка разбора аргументов: {ex.Message}";
                return;
            }

            var (response, errorMessage) = await _apiService.CallRoutineAsync(routineType, SelectedRoutineName, payload);

            if (response != null)
            {
                ApiStatusMessage = $"{routineType} '{SelectedRoutineName}' успешно вызвана.";
                if (response.Result.ValueKind != JsonValueKind.Undefined && response.Result.ValueKind != JsonValueKind.Null)
                {
                    RoutineCallResult = JsonSerializer.Serialize(response.Result, new JsonSerializerOptions { WriteIndented = true });
                }
                else if (!string.IsNullOrEmpty(response.Status))
                {
                    RoutineCallResult = $"Статус: {response.Status}";
                    if (response.ArgsUsed != null && response.ArgsUsed.Any())
                    {
                        RoutineCallResult += $"\nИспользованные аргументы: {JsonSerializer.Serialize(response.ArgsUsed, new JsonSerializerOptions { WriteIndented = true })}";
                    }
                }
                else
                {
                    RoutineCallResult = "Подпрограмма выполнена. Специфичных данных или статуса в ожидаемом формате не возвращено.";
                }
            }
            else
            {
                ApiStatusMessage = $"Ошибка вызова {routineType} {SelectedRoutineName}: {errorMessage}";
                RoutineCallResult = $"Ошибка: {errorMessage}";
            }
        }

        private async Task ExecuteReadDataAsync()
        {
            if (string.IsNullOrWhiteSpace(DataQueryTableName))
            {
                ApiStatusMessage = "Пожалуйста, введите имя таблицы/представления для запроса данных.";
                QueriedDataTable = null;
                DataQueryTotalCount = 0;
                UpdatePaginationCommands();
                return;
            }

            ApiStatusMessage = $"Запрос данных из {DataQueryTableName}...";
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
                DataQueryOffset = response.Offset;
                DataQueryLimit = response.Limit;
                QueriedDataTable = ConvertJsonElementsToDataTable(response.Data);
                ApiStatusMessage = $"Данные загружены из {DataQueryTableName}. Показано {response.Data.Count} из {response.TotalCount} элементов. (Смещение: {response.Offset}, Лимит: {response.Limit})";
            }
            else
            {
                DataQueryTotalCount = 0;
                ApiStatusMessage = $"Ошибка запроса данных из {DataQueryTableName}: {errorMessage}";
                MessageBox.Show($"Не удалось запросить данные: {errorMessage}", "Ошибка запроса данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            UpdatePaginationCommands();
        }

        private async Task ExecuteReadDataByColumnAsync()
        {
            if (string.IsNullOrWhiteSpace(DataByColumnTableName) ||
               string.IsNullOrWhiteSpace(DataByColumnName) ||
               DataByColumnValue == null)
            {
                ApiStatusMessage = "Пожалуйста, заполните все обязательные поля для 'Чтения по значению столбца'.";
                QueriedByColumnDataTable = null;
                return;
            }
            ApiStatusMessage = $"Запрос данных из {DataByColumnTableName} по {DataByColumnName}='{DataByColumnValue}'...";
            QueriedByColumnDataTable = null;

            var (items, errorMessage) = await _apiService.ReadItemsByColumnValueAsync(
                DataByColumnTableName,
                DataByColumnName,
                DataByColumnValue
            );

            if (items != null)
            {
                QueriedByColumnDataTable = ConvertJsonElementsToDataTable(items);
                ApiStatusMessage = $"Данные загружены из {DataByColumnTableName}, где {DataByColumnName} = '{DataByColumnValue}'. Найдено {items.Count} элементов.";
            }
            else
            {
                ApiStatusMessage = $"Ошибка запроса данных по столбцу: {errorMessage}";
                MessageBox.Show($"Не удалось запросить данные по столбцу: {errorMessage}", "Ошибка запроса данных", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    Type columnType = typeof(object);
                    dataTable.Columns.Add(property.Name, columnType);
                }
            }
            else
            {
                dataTable.Columns.Add("Значение", typeof(string));
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
                    if (dataTable.Columns.Contains("Значение"))
                    {
                        row["Значение"] = GetValueFromJsonElement(element);
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
                    break;
                case nameof(SelectedRoutineItem):
                case nameof(SelectedRoutineName):
                    (CallSelectedRoutineCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    Debug.WriteLine($"SelectedRoutineName изменено на: {SelectedRoutineName}");
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