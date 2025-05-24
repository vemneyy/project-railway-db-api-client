// ApiService.cs
using ApiManagerApp.Classes;
using Microsoft.AspNetCore.WebUtilities;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ApiManagerApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://api.desperatio.com";
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        private const string DbRoleHeaderName = "X-Database-Role";

        public ApiService(string? baseUrl = null)
        {
            if (!string.IsNullOrEmpty(baseUrl))
            {
                _baseUrl = baseUrl;
            }

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                // Для корректной сериализации русских символов в JSON ответах (если нужно)
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        }

        public void SetCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
                Debug.WriteLine("Учетные данные API сброшены.");
            }
            else
            {
                var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                Debug.WriteLine($"Установлены учетные данные API для пользователя: {username}");
            }
        }

        public void ClearCredentials()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            Debug.WriteLine("Учетные данные API очищены.");
        }

        public void SetDatabaseRoleHeader(string? dbRole)
        {
            _httpClient.DefaultRequestHeaders.Remove(DbRoleHeaderName); // Удаляем старый, если был
            if (!string.IsNullOrWhiteSpace(dbRole))
            {
                _httpClient.DefaultRequestHeaders.Add(DbRoleHeaderName, dbRole);
                Debug.WriteLine($"Установлен заголовок {DbRoleHeaderName}: {dbRole}");
            }
            else
            {
                Debug.WriteLine($"Заголовок {DbRoleHeaderName} очищен.");
            }
        }

        private string FormatError(HttpResponseMessage response, string responseString)
        {
            string baseError = $"Ошибка: {response.StatusCode} - {response.ReasonPhrase}.";
            if (string.IsNullOrWhiteSpace(responseString))
            {
                return $"{baseError} (Пустое тело ответа)";
            }

            try
            {
                var validationErrorWrapper = JsonSerializer.Deserialize<FastApiValidationErrorWrapper>(responseString, _jsonSerializerOptions);
                if (validationErrorWrapper?.Detail != null && validationErrorWrapper.Detail.Any())
                {
                    var errorMessages = validationErrorWrapper.Detail.Select(d =>
                    {
                        string loc = d.Loc != null && d.Loc.Any() ? string.Join(" -> ", d.Loc.Select(o => o.ToString())) : "Н/Д";
                        return $"Поле: {loc}, Сообщение: {d.Msg} (Тип: {d.Type})";
                    });
                    return $"{baseError}\nДетали валидации:\n{string.Join("\n", errorMessages)}";
                }

                // Попытка десериализовать как стандартный ответ с ошибкой FastAPI (словарь с ключом "detail")
                var errorDetailDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseString, _jsonSerializerOptions);
                if (errorDetailDict != null && errorDetailDict.TryGetValue("detail", out var detailElement))
                {
                    if (detailElement.ValueKind == JsonValueKind.String)
                    {
                        return $"{baseError}\nДетали: {detailElement.GetString()}";
                    }
                    // Если "detail" не строка, а, например, объект или массив (хотя обычно строка)
                    return $"{baseError}\nДетали (JSON):\n{JsonSerializer.Serialize(detailElement, _jsonSerializerOptions)}";
                }

                // Если не удалось распознать структуру ошибки, но это валидный JSON
                using (JsonDocument.Parse(responseString)) // Проверка, что это валидный JSON
                {
                    // Используем _jsonSerializerOptions, который уже включает Encoder
                    return $"{baseError}\nОтвет сервера (JSON):\n{JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(responseString), _jsonSerializerOptions)}";
                }
            }
            catch (JsonException) // Если это не JSON
            {
                const int maxRawTextLength = 500;
                string rawText = responseString.Length > maxRawTextLength
                    ? $"{responseString.Substring(0, maxRawTextLength)}..."
                    : responseString;
                return $"{baseError}\nОтвет сервера (не JSON):\n{rawText}";
            }
        }

        private async Task<string> FormatErrorAsync(HttpResponseMessage response, string responseString)
        {
            return await Task.FromResult(FormatError(response, responseString));
        }

        public async Task<(List<string>? Tables, string? ErrorMessage)> GetTablesAsync()
        {
            return await GetAsync<List<string>>("/schema/tables");
        }

        public async Task<(List<string>? Views, string? ErrorMessage)> GetViewsAsync()
        {
            return await GetAsync<List<string>>("/schema/views");
        }

        public async Task<(TableSchemaDetail? Schema, string ErrorMessage)> GetTableOrViewSchemaAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return (null, "Имя таблицы/представления не может быть пустым.");

            var (responseBody, errorMessage) = await GetAsync<TableSchemaDetail>($"/schema/table/{Uri.EscapeDataString(name)}");
            return (responseBody, errorMessage ?? string.Empty);
        }

        public async Task<(List<ProcedureInfo>? Procedures, string? ErrorMessage)> GetProceduresAsync()
        {
            return await GetAsync<List<ProcedureInfo>>("/schema/procedures");
        }

        public async Task<(List<FunctionInfo>? Functions, string? ErrorMessage)> GetFunctionsAsync()
        {
            return await GetAsync<List<FunctionInfo>>("/schema/functions");
        }

        private async Task<(T? ResponseBody, string? ErrorMessage)> GetAsync<T>(string requestUri)
        {
            try
            {
                Debug.WriteLine($"GET запрос: {_httpClient.BaseAddress}{requestUri}");
                HttpResponseMessage response = await _httpClient.GetAsync(requestUri);
                var responseString = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"GET статус ответа: {response.StatusCode}");
                Debug.WriteLine($"GET тело ответа: {responseString}");

                if (response.IsSuccessStatusCode)
                {
                    var responseObject = JsonSerializer.Deserialize<T>(responseString, _jsonSerializerOptions);
                    return (responseObject, null);
                }
                else
                {
                    return (default, FormatError(response, responseString));
                }
            }
            catch (HttpRequestException httpEx)
            {
                Debug.WriteLine($"HTTP исключение запроса для {requestUri}: {httpEx.Message}");
                return (default, $"Сетевая ошибка: {httpEx.Message} (СтатусКод: {httpEx.StatusCode})");
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine($"Запрос отменен/тайм-аут для {requestUri}: {ex.Message}");
                return (default, ex.InnerException is TimeoutException ? "Тайм-аут запроса." : $"Запрос отменен: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Общее исключение для {requestUri}: {ex.Message}");
                return (default, $"Произошла непредвиденная ошибка: {ex.Message}");
            }
        }

        private async Task<(T? ResponseBody, string? ErrorMessage)> PostAsync<T>(string requestUri, object payload)
        {
            try
            {
                var jsonPayload = JsonSerializer.Serialize(payload, _jsonSerializerOptions);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                Debug.WriteLine($"POST запрос: {_httpClient.BaseAddress}{requestUri}");
                Debug.WriteLine($"POST полезная нагрузка: {jsonPayload}");

                HttpResponseMessage response = await _httpClient.PostAsync(requestUri, content);
                var responseString = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"POST статус ответа: {response.StatusCode}");
                Debug.WriteLine($"POST тело ответа: {responseString}");

                if (response.IsSuccessStatusCode)
                {
                    var responseObject = JsonSerializer.Deserialize<T>(responseString, _jsonSerializerOptions);
                    return (responseObject, null);
                }
                else
                {
                    return (default, FormatError(response, responseString));
                }
            }
            catch (HttpRequestException httpEx)
            {
                Debug.WriteLine($"HTTP исключение запроса для {requestUri}: {httpEx.Message}");
                return (default, $"Сетевая ошибка: {httpEx.Message} (СтатусКод: {httpEx.StatusCode})");
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine($"Запрос отменен/тайм-аут для {requestUri}: {ex.Message}");
                return (default, ex.InnerException is TimeoutException ? "Тайм-аут запроса." : $"Запрос отменен: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Общее исключение для {requestUri}: {ex.Message}");
                return (default, $"Произошла непредвиденная ошибка: {ex.Message}");
            }
        }

        public async Task<(HealthCheckResponse? HealthInfo, TimeSpan Latency, string? ErrorMessage)> CheckHealthAsyncWithLatency()
        {
            var stopwatch = new Stopwatch();
            try
            {
                Debug.WriteLine($"GET запрос: {_httpClient.BaseAddress}/health");
                stopwatch.Start();
                HttpResponseMessage response = await _httpClient.GetAsync("/health");
                stopwatch.Stop();

                var responseString = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"GET /health статус ответа: {response.StatusCode}");
                Debug.WriteLine($"GET /health тело ответа: {responseString}");
                Debug.WriteLine($"GET /health задержка: {stopwatch.ElapsedMilliseconds} мс");

                if (response.IsSuccessStatusCode)
                {
                    var healthInfo = JsonSerializer.Deserialize<HealthCheckResponse>(responseString, _jsonSerializerOptions);
                    return (healthInfo, stopwatch.Elapsed, null);
                }
                else
                {
                    return (null, stopwatch.Elapsed, await FormatErrorAsync(response, responseString));
                }
            }
            catch (HttpRequestException httpEx)
            {
                stopwatch.Stop();
                Debug.WriteLine($"Health Check HTTP исключение запроса: {httpEx.Message}");
                return (null, stopwatch.Elapsed, $"Сетевая ошибка: {httpEx.Message} (СтатусКод: {httpEx.StatusCode})");
            }
            catch (TaskCanceledException ex)
            {
                stopwatch.Stop();
                Debug.WriteLine($"Health Check запрос отменен/тайм-аут: {ex.Message}");
                return (null, stopwatch.Elapsed, ex.InnerException is TimeoutException ? $"Тайм-аут запроса после {stopwatch.ElapsedMilliseconds} мс." : $"Запрос отменен: {ex.Message}");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.WriteLine($"Health Check общее исключение: {ex.Message}");
                return (null, stopwatch.Elapsed, $"Произошла непредвиденная ошибка: {ex.Message}");
            }
        }

        public async Task<(RoutineCallResponse Response, string ErrorMessage)> CallRoutineAsync(string routineType, string routineName, RoutineCallArgs payload)
        {
            if (string.IsNullOrWhiteSpace(routineName))
                return (new RoutineCallResponse(), "Имя рутины не может быть пустым.");
            payload ??= new RoutineCallArgs();

            string endpointPathSegment;
            if (routineType.Equals("процедура", StringComparison.OrdinalIgnoreCase) ||
                routineType.Equals("procedure", StringComparison.OrdinalIgnoreCase))
            {
                endpointPathSegment = "procedure";
            }
            else if (routineType.Equals("функция", StringComparison.OrdinalIgnoreCase) ||
                     routineType.Equals("function", StringComparison.OrdinalIgnoreCase))
            {
                endpointPathSegment = "function";
            }
            else
            {
                return (new RoutineCallResponse(), $"Неизвестный тип рутины: {routineType}");
            }

            string endpoint = $"/routines/{endpointPathSegment}/{Uri.EscapeDataString(routineName)}";

            var (response, errorMessage) = await PostAsync<RoutineCallResponse>(endpoint, payload);
            return (response ?? new RoutineCallResponse(), errorMessage ?? string.Empty);
        }

        public async Task<(PaginatedDataResponse<JsonElement>? Response, string? ErrorMessage)> ReadItemsAsync(
            string tableOrViewName,
            int limit = 100,
            int offset = 0,
            string? sortBy = null,
            string? fields = null,
            Dictionary<string, string>? filters = null)
        {
            if (string.IsNullOrWhiteSpace(tableOrViewName))
                return (null, "Имя таблицы/представления не может быть пустым.");

            var queryParams = new Dictionary<string, string?>()
            {
                { "limit", limit.ToString() },
                { "offset", offset.ToString() }
            };

            if (!string.IsNullOrWhiteSpace(sortBy)) queryParams["sort_by"] = sortBy;
            if (!string.IsNullOrWhiteSpace(fields)) queryParams["fields"] = fields;

            if (filters != null)
            {
                foreach (var filter in filters)
                {
                    if (!string.IsNullOrWhiteSpace(filter.Key) && filter.Value != null)
                    {
                        queryParams[filter.Key] = filter.Value;
                    }
                }
            }
            string requestUri = QueryHelpers.AddQueryString($"/{Uri.EscapeDataString(tableOrViewName)}", queryParams);

            return await GetAsync<PaginatedDataResponse<JsonElement>>(requestUri);
        }

        public async Task<(List<JsonElement> Items, string ErrorMessage)> ReadItemsByColumnValueAsync(
           string tableOrViewName,
           string columnName,
           string columnValue)
        {
            if (string.IsNullOrWhiteSpace(tableOrViewName))
                return (new List<JsonElement>(), "Имя таблицы/представления не может быть пустым.");
            if (string.IsNullOrWhiteSpace(columnName))
                return (new List<JsonElement>(), "Имя колонки не может быть пустым.");
            columnValue ??= string.Empty;

            string requestUri = $"/{Uri.EscapeDataString(tableOrViewName)}/column/{Uri.EscapeDataString(columnName)}/{Uri.EscapeDataString(columnValue)}";
            var (items, errorMessage) = await GetAsync<List<JsonElement>>(requestUri);
            return (items ?? new List<JsonElement>(), errorMessage ?? string.Empty);
        }
    }
}