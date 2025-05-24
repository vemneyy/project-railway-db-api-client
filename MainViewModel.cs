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

        private string _apiUsername = "api_user_cashier"; // Значение по умолчанию
        public string ApiUsername
        {
            get => _apiUsername;
            set { SetProperty(ref _apiUsername, value); }
        }

        private string _apiPassword = "api_cashier_pass"; // Значение по умолчанию
        public string ApiPassword // Это свойство будет связано с PasswordBox через Behavior или Attached Property
        {
            get => _apiPassword;
            set { SetProperty(ref _apiPassword, value); }
        }

        private string? _selectedDbRole;
        public string? SelectedDbRole
        {
            get => _selectedDbRole;
            set
            {
                if (SetProperty(ref _selectedDbRole, value))
                {
                    _apiService.SetDatabaseRoleHeader(value); // Устанавливаем заголовок при изменении
                    ApiStatusMessage = string.IsNullOrWhiteSpace(value)
                        ? "Роль БД не выбрана (будет использована роль по умолчанию, если возможно)."
                        : $"Выбрана роль БД: {value}";
                }
            }
        }

        public ObservableCollection<string> AvailableDbRoles { get; } = new ObservableCollection<string>
        {
            "cashier", "dispatcher", "analyst", "managment"
        };

        public ICommand ApplyApiCredentialsCommand { get; }
        public ICommand ClearApiCredentialsCommand { get; }

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

            _apiService.SetCredentials(_apiUsername, _apiPassword);

            ApplyApiCredentialsCommand = new RelayCommand(
                param => ExecuteApplyApiCredentials(),
                param => !string.IsNullOrWhiteSpace(ApiUsername) // Пароль проверяется при выполнении
            );
            ClearApiCredentialsCommand = new RelayCommand(param => ExecuteClearApiCredentials());

            CheckHealthCommand = new RelayCommand(async param => await ExecuteCheckHealthAsync(), param => true);
            LoadTablesCommand = new RelayCommand(async param => await ExecuteLoadTablesAsync(), param => true);
            LoadViewsCommand = new RelayCommand(async param => await ExecuteLoadViewsAsync(), param => true);
            LoadTableSchemaCommand = new RelayCommand(async param => await ExecuteLoadTableSchemaAsync(),
                                                      param => !string.IsNullOrWhiteSpace(SelectedTableNameForSchema));
            LoadProceduresCommand = new RelayCommand(async param => await ExecuteLoadProceduresAsync(), param => true);
            LoadFunctionsCommand = new RelayCommand(async param => await ExecuteLoadFunctionsAsync(), param => true);

            // Point CallProcedureCommand and CallFunctionCommand to ExecuteCallSelectedRoutineAsync
            // if SelectedRoutineName is managed by SelectedRoutineItem. Otherwise, ensure ExecuteCallRoutineAsync is updated.
            // For simplicity, we assume CallSelectedRoutineCommand is the primary path for new calls.
            // If CallProcedureCommand/CallFunctionCommand are still independently used, ExecuteCallRoutineAsync needs the same fix.
            CallProcedureCommand = new RelayCommand(async param => await ExecuteCallRoutineAsync("процедура"), //This still calls ExecuteCallRoutineAsync
                                                    param => !string.IsNullOrWhiteSpace(SelectedRoutineName));
            CallFunctionCommand = new RelayCommand(async param => await ExecuteCallRoutineAsync("функция"),    //This still calls ExecuteCallRoutineAsync
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

        private void ExecuteApplyApiCredentials()
        {
            // В реальном приложении пароль нужно получать из PasswordBox более безопасным способом
            if (string.IsNullOrWhiteSpace(ApiPassword))
            {
                ApiStatusMessage = "Пароль API не может быть пустым.";
                MessageBox.Show("Пароль API не может быть пустым.", "Ошибка учетных данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _apiService.SetCredentials(ApiUsername, ApiPassword); // ApiPassword здесь из свойства
            ApiStatusMessage = $"Учетные данные API установлены для пользователя '{ApiUsername}'.";
        }

        private void ExecuteClearApiCredentials()
        {
            _apiService.ClearCredentials();
            ApiUsername = string.Empty; // Очищаем поля в UI
            ApiPassword = string.Empty; // Очищаем поля в UI (для PasswordBox это будет сложнее)
            ApiStatusMessage = "Учетные данные API очищены.";
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
            RoutineCallResult = ""; // Clear previous result
            RoutineCallDataTableResult = null; // Clear previous table result

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
                // Do not return here, proceed with empty or default payload if desired, or handle as error
            }
            catch (Exception ex) // Catch other argument parsing errors
            {
                ApiStatusMessage = $"Ошибка подготовки аргументов: {ex.Message}";
                RoutineCallResult = $"Ошибка подготовки аргументов: {ex.Message}";
                return; // Return if payload preparation fails critically
            }

            var (response, errorMessage) = await _apiService.CallRoutineAsync(routineType, SelectedRoutineName, payload);

            if (!string.IsNullOrEmpty(errorMessage)) // Prioritize error message from ApiService
            {
                ApiStatusMessage = $"Ошибка вызова {routineType} {SelectedRoutineName}.";
                RoutineCallResult = errorMessage; // This contains the detailed error from ApiService
                RoutineCallDataTableResult = null;
            }
            else // No direct API call error, process the response content (response object is guaranteed non-null by ApiService)
            {
                ApiStatusMessage = $"{routineType} '{SelectedRoutineName}' вызвана, ответ от API получен.";

                bool isFunctionReturningTable = SelectedRoutineItem is FunctionInfo funcInfo &&
                                                (funcInfo.PreciseReturnType?.ToUpperInvariant().StartsWith("TABLE") == true ||
                                                 funcInfo.PreciseReturnType?.ToUpperInvariant().Contains("SETOF RECORD") == true ||
                                                 funcInfo.PreciseReturnType?.ToUpperInvariant().Contains("RECORD[]") == true);

                if (isFunctionReturningTable && response.Result.ValueKind == JsonValueKind.Array)
                {
                    try
                    {
                        var elements = JsonSerializer.Deserialize<List<JsonElement>>(response.Result.GetRawText());
                        RoutineCallDataTableResult = ConvertJsonElementsToDataTable(elements);
                        RoutineCallResult = $"Табличный результат показан ниже. ({elements?.Count ?? 0} строк)";
                    }
                    catch (Exception ex)
                    {
                        RoutineCallResult = $"Ошибка преобразования табличного результата: {ex.Message}\nИсходный JSON:\n{JsonSerializer.Serialize(response.Result, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping })}";
                        RoutineCallDataTableResult = null;
                    }
                }
                else if (response.Result.ValueKind != JsonValueKind.Undefined && response.Result.ValueKind != JsonValueKind.Null)
                {
                    RoutineCallResult = JsonSerializer.Serialize(response.Result, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                    RoutineCallDataTableResult = null;
                    if (isFunctionReturningTable) // It was expected to be a table but wasn't an array
                    {
                        RoutineCallResult = $"Ожидался массив для табличного результата, но получен {response.Result.ValueKind}.\nРезультат:\n{RoutineCallResult}";
                    }
                }
                else if (!string.IsNullOrEmpty(response.Status))
                {
                    RoutineCallResult = $"Статус: {response.Status}";
                    if (response.ArgsUsed != null && response.ArgsUsed.Any())
                    {
                        RoutineCallResult += $"\nИспользованные аргументы: {JsonSerializer.Serialize(response.ArgsUsed, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping })}";
                    }
                    RoutineCallDataTableResult = null;
                }
                else
                {
                    RoutineCallResult = "Подпрограмма выполнена. Данных или статуса не возвращено в теле ответа.";
                    RoutineCallDataTableResult = null;
                }
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
                if (latency > TimeSpan.Zero || (errorMessage?.Contains("timed out") ?? false) || (errorMessage?.Contains("Тайм-аут") ?? false)) // Added "Тайм-аут" for localized timeout message
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

        // This method also needs to be updated similarly to ExecuteCallSelectedRoutineAsync
        private async Task ExecuteCallRoutineAsync(string routineType)
        {
            if (string.IsNullOrWhiteSpace(SelectedRoutineName))
            {
                ApiStatusMessage = "Пожалуйста, выберите имя подпрограммы.";
                RoutineCallResult = "Подпрограмма не выбрана.";
                return;
            }

            ApiStatusMessage = $"Вызов {routineType} {SelectedRoutineName}...";
            RoutineCallResult = ""; // Clear previous result

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
                // Consider if we should proceed or return
            }
            catch (Exception ex) // Catch other argument parsing errors
            {
                ApiStatusMessage = $"Ошибка подготовки аргументов: {ex.Message}";
                RoutineCallResult = $"Ошибка подготовки аргументов: {ex.Message}";
                return;
            }

            var (response, errorMessage) = await _apiService.CallRoutineAsync(routineType, SelectedRoutineName, payload);

            if (!string.IsNullOrEmpty(errorMessage)) // Prioritize error message from ApiService
            {
                ApiStatusMessage = $"Ошибка вызова {routineType} {SelectedRoutineName}.";
                RoutineCallResult = errorMessage; // This contains the detailed error from ApiService
            }
            else // No direct API call error, process the response content (response object is guaranteed non-null)
            {
                ApiStatusMessage = $"{routineType} '{SelectedRoutineName}' вызвана, ответ от API получен.";

                if (response.Result.ValueKind != JsonValueKind.Undefined && response.Result.ValueKind != JsonValueKind.Null)
                {
                    RoutineCallResult = JsonSerializer.Serialize(response.Result, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                }
                else if (!string.IsNullOrEmpty(response.Status))
                {
                    RoutineCallResult = $"Статус: {response.Status}";
                    if (response.ArgsUsed != null && response.ArgsUsed.Any())
                    {
                        RoutineCallResult += $"\nИспользованные аргументы: {JsonSerializer.Serialize(response.ArgsUsed, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping })}";
                    }
                }
                else
                {
                    // This is for procedures that complete without returning a specific result or status in the JSON body
                    RoutineCallResult = "Подпрограмма выполнена. Данных или статуса не возвращено в теле ответа.";
                }
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

            if (response != null && response.Data != null) // Check response and response.Data
            {
                DataQueryTotalCount = response.TotalCount;
                DataQueryOffset = response.Offset;
                DataQueryLimit = response.Limit;
                QueriedDataTable = ConvertJsonElementsToDataTable(response.Data);
                ApiStatusMessage = $"Данные загружены из {DataQueryTableName}. Показано {response.Data.Count} из {response.TotalCount} элементов. (Смещение: {response.Offset}, Лимит: {response.Limit})";
            }
            else // Handle error or empty response
            {
                DataQueryTotalCount = 0;
                // If errorMessage is present, use it. Otherwise, a generic message.
                ApiStatusMessage = !string.IsNullOrEmpty(errorMessage)
                    ? $"Ошибка запроса данных из {DataQueryTableName}: {errorMessage}"
                    : $"Данные из {DataQueryTableName} не получены или ответ пуст.";

                if (!string.IsNullOrEmpty(errorMessage)) // Show MessageBox only on actual error
                {
                    MessageBox.Show($"Не удалось запросить данные: {errorMessage}", "Ошибка запроса данных", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            UpdatePaginationCommands();
        }

        private async Task ExecuteReadDataByColumnAsync()
        {
            if (string.IsNullOrWhiteSpace(DataByColumnTableName) ||
               string.IsNullOrWhiteSpace(DataByColumnName) ||
               DataByColumnValue == null) // DataByColumnValue can be an empty string, so null check is important
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

            // items can be an empty list on success, or null on error (though ApiService tries to return empty list on error too)
            if (!string.IsNullOrEmpty(errorMessage))
            {
                ApiStatusMessage = $"Ошибка запроса данных по столбцу: {errorMessage}";
                MessageBox.Show($"Не удалось запросить данные по столбцу: {errorMessage}", "Ошибка запроса данных", MessageBoxButton.OK, MessageBoxImage.Error);
                QueriedByColumnDataTable = null; // Ensure table is cleared
            }
            else if (items != null) // items is not null and no error message
            {
                QueriedByColumnDataTable = ConvertJsonElementsToDataTable(items);
                ApiStatusMessage = $"Данные загружены из {DataByColumnTableName}, где {DataByColumnName} = '{DataByColumnValue}'. Найдено {items.Count} элементов.";
            }
            else // Should not happen if ApiService always returns non-null items or an error message
            {
                ApiStatusMessage = "Неожиданный ответ от API при запросе данных по столбцу.";
                QueriedByColumnDataTable = null;
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
                    // Determine column type based on the first row's data for more specific typing if possible
                    // For simplicity, keeping as object, or inferring basic types
                    Type columnType = GetTypeFromJsonElement(property.Value);
                    try
                    {
                        dataTable.Columns.Add(property.Name, columnType);
                    }
                    catch (DuplicateNameException)
                    {
                        // Handle duplicate column names if necessary, though rare from structured JSON
                        // For now, we assume unique property names in the first object
                    }
                }
            }
            else // If elements are not objects (e.g., list of strings, numbers)
            {
                dataTable.Columns.Add("Значение", GetTypeFromJsonElement(firstElement));
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
                            row[property.Name] = GetValueFromJsonElement(property.Value) ?? DBNull.Value;
                        }
                    }
                }
                else // For non-object elements
                {
                    if (dataTable.Columns.Contains("Значение"))
                    {
                        row["Значение"] = GetValueFromJsonElement(element) ?? DBNull.Value;
                    }
                }
                dataTable.Rows.Add(row);
            }
            return dataTable;
        }

        private Type GetTypeFromJsonElement(JsonElement jsonElement)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.String:
                    return typeof(string);
                case JsonValueKind.Number:
                    if (jsonElement.TryGetInt64(out _)) return typeof(long);
                    if (jsonElement.TryGetDouble(out _)) return typeof(double);
                    return typeof(string); // Fallback for numbers that don't fit
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return typeof(bool);
                case JsonValueKind.Null:
                    return typeof(object); // Or handle as nullable of a default type
                default:
                    return typeof(string); // For arrays, objects, undefined - represent as string
            }
        }


        private object? GetValueFromJsonElement(JsonElement jsonElement) // Return type changed to object?
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.String:
                    return jsonElement.GetString(); // Nullable if JSON string is null, though ValueKind.Null is separate
                case JsonValueKind.Number:
                    if (jsonElement.TryGetInt64(out long l)) return l;
                    if (jsonElement.TryGetDouble(out double d)) return d;
                    // If it's a number but can't be parsed into long/double (e.g. very large decimal), return as string
                    return jsonElement.GetRawText();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return DBNull.Value; // For DataTable compatibility
                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    // Serialize complex types back to string for display in a simple DataTable cell
                    return jsonElement.ToString();
                default: // JsonValueKind.Undefined or any other
                    return jsonElement.ToString(); // Fallback
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
                    (CallProcedureCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (CallFunctionCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
                case nameof(ApiUsername):
                    (ApplyApiCredentialsCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    break;
            }
        }
    }
}