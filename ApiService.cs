// ApiService.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text; // For StringContent
using System.Text.Json;
using System.Text.Json.Serialization; // For JsonPropertyName
using System.Threading.Tasks;
using System.Diagnostics;
// Для .NET Core / .NET 5+ используйте Microsoft.AspNetCore.WebUtilities
// Установите NuGet пакет: Microsoft.AspNetCore.WebUtilities
using Microsoft.AspNetCore.WebUtilities;
// Если вы используете .NET Framework, раскомментируйте System.Web и закомментируйте Microsoft.AspNetCore.WebUtilities
// using System.Web; 

namespace ApiManagerApp.Services
{
    // DTO for Health Check Response
    public class HealthCheckResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("database_connection")]
        public string Database_Connection { get; set; }
    }

    // DTOs для схемы таблицы/представления
    public class ColumnInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("python_type")]
        public string PythonType { get; set; }

        [JsonPropertyName("nullable")]
        public bool Nullable { get; set; }

        [JsonPropertyName("primary_key")]
        public bool PrimaryKey { get; set; }

        [JsonPropertyName("default")]
        public string Default { get; set; }

        [JsonPropertyName("server_default")]
        public string ServerDefault { get; set; }
    }

    public class ForeignKeyInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("constrained_columns")]
        public List<string> ConstrainedColumns { get; set; }

        [JsonPropertyName("referred_schema")]
        public string ReferredSchema { get; set; }

        [JsonPropertyName("referred_table")]
        public string ReferredTable { get; set; }

        [JsonPropertyName("referred_columns")]
        public List<string> ReferredColumns { get; set; }
    }

    public class PydanticModelsInfo
    {
        [JsonPropertyName("read")]
        public string Read { get; set; }

        [JsonPropertyName("create")]
        public string Create { get; set; }

        [JsonPropertyName("update")]
        public string Update { get; set; }
    }

    public class TableSchemaDetail
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("db_table_name")]
        public string DbTableName { get; set; }

        [JsonPropertyName("db_schema")]
        public string DbSchema { get; set; }

        [JsonPropertyName("is_view")]
        public bool IsView { get; set; }

        [JsonPropertyName("columns")]
        public List<ColumnInfo> Columns { get; set; }

        [JsonPropertyName("primary_keys")]
        public List<string> PrimaryKeys { get; set; }

        [JsonPropertyName("foreign_keys")]
        public List<ForeignKeyInfo> ForeignKeys { get; set; }

        [JsonPropertyName("pydantic_models")]
        public PydanticModelsInfo PydanticModels { get; set; }
    }

    // DTO для процедур
    public class ProcedureInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("arguments_signature")]
        public string ArgumentsSignature { get; set; }
    }

    // DTO для функций
    public class FunctionInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("return_type")]
        public string ReturnType { get; set; }

        [JsonPropertyName("arguments_signature")]
        public string ArgumentsSignature { get; set; }

        [JsonPropertyName("precise_return_type")]
        public string PreciseReturnType { get; set; }
    }

    // DTO для вызова рутин
    public class RoutineCallArgs
    {
        [JsonPropertyName("args")]
        public List<object> Args { get; set; } = new List<object>();

        [JsonPropertyName("kwargs")]
        public Dictionary<string, object> Kwargs { get; set; } = new Dictionary<string, object>();
    }

    public class RoutineCallResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("procedure")]
        public string Procedure { get; set; }

        [JsonPropertyName("function")]
        public string Function { get; set; }

        [JsonPropertyName("args_used")]
        public List<object> ArgsUsed { get; set; }

        [JsonPropertyName("result")]
        public JsonElement Result { get; set; }
    }

    // DTO для ответа при чтении элементов (с пагинацией)
    public class PaginatedDataResponse<T>
    {
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("data")]
        public List<T> Data { get; set; }
    }

    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://api.desperatio.com"; // Замените на ваш URL, если нужно
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public ApiService(string baseUrl = null)
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
            // Устанавливаем таймаут, чтобы приложение не зависало бесконечно
            _httpClient.Timeout = TimeSpan.FromSeconds(30);


            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private async Task<(T ResponseBody, string ErrorMessage)> GetAsync<T>(string requestUri)
        {
            try
            {
                Debug.WriteLine($"GET request: {_httpClient.BaseAddress}{requestUri}");
                HttpResponseMessage response = await _httpClient.GetAsync(requestUri);
                var responseString = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"GET response status: {response.StatusCode}");
                Debug.WriteLine($"GET response body: {responseString}");

                if (response.IsSuccessStatusCode)
                {
                    var responseObject = JsonSerializer.Deserialize<T>(responseString, _jsonSerializerOptions);
                    return (responseObject, null);
                }
                else
                {
                    return (default, await FormatErrorAsync(response, responseString));
                }
            }
            catch (HttpRequestException httpEx)
            {
                Debug.WriteLine($"HTTP Request Exception for {requestUri}: {httpEx.Message}");
                return (default, $"Network error: {httpEx.Message} (StatusCode: {httpEx.StatusCode})");
            }
            catch (TaskCanceledException ex) // Таймаут или отмена
            {
                Debug.WriteLine($"Request Canceled/Timeout for {requestUri}: {ex.Message}");
                return (default, ex.InnerException is TimeoutException ? "Request timed out." : $"Request canceled: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Generic Exception for {requestUri}: {ex.Message}");
                return (default, $"An unexpected error occurred: {ex.Message}");
            }
        }

        private async Task<(T ResponseBody, string ErrorMessage)> PostAsync<T>(string requestUri, object payload)
        {
            try
            {
                var jsonPayload = JsonSerializer.Serialize(payload, _jsonSerializerOptions);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                Debug.WriteLine($"POST request: {_httpClient.BaseAddress}{requestUri}");
                Debug.WriteLine($"POST payload: {jsonPayload}");

                HttpResponseMessage response = await _httpClient.PostAsync(requestUri, content);
                var responseString = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"POST response status: {response.StatusCode}");
                Debug.WriteLine($"POST response body: {responseString}");

                if (response.IsSuccessStatusCode)
                {
                    var responseObject = JsonSerializer.Deserialize<T>(responseString, _jsonSerializerOptions);
                    return (responseObject, null);
                }
                else
                {
                    return (default, await FormatErrorAsync(response, responseString));
                }
            }
            catch (HttpRequestException httpEx)
            {
                Debug.WriteLine($"HTTP Request Exception for {requestUri}: {httpEx.Message}");
                return (default, $"Network error: {httpEx.Message} (StatusCode: {httpEx.StatusCode})");
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine($"Request Canceled/Timeout for {requestUri}: {ex.Message}");
                return (default, ex.InnerException is TimeoutException ? "Request timed out." : $"Request canceled: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Generic Exception for {requestUri}: {ex.Message}");
                return (default, $"An unexpected error occurred: {ex.Message}");
            }
        }

        private async Task<string> FormatErrorAsync(HttpResponseMessage response, string responseString)
        {
            // Попытка десериализовать ошибку, если API возвращает JSON с 'detail'
            try
            {
                // Иногда FastAPI возвращает массив ошибок валидации
                var validationErrors = JsonSerializer.Deserialize<FastApiValidationErrorWrapper>(responseString, _jsonSerializerOptions);
                if (validationErrors?.Detail != null && validationErrors.Detail.Any())
                {
                    var errorMessages = validationErrors.Detail.Select(d =>
                    {
                        string loc = d.Loc != null && d.Loc.Any() ? string.Join(" -> ", d.Loc.Select(o => o.ToString())) : "N/A";
                        return $"Field: {loc}, Message: {d.Msg} (Type: {d.Type})";
                    });
                    return $"Validation Error(s): {response.StatusCode} - {string.Join("; ", errorMessages)}";
                }

                var errorDetail = JsonSerializer.Deserialize<Dictionary<string, string>>(responseString, _jsonSerializerOptions);
                if (errorDetail != null && errorDetail.TryGetValue("detail", out var detailMessage))
                {
                    return $"Error: {response.StatusCode} - {detailMessage}";
                }
            }
            catch (JsonException) { /* Игнорируем ошибку десериализации, используем полный ответ */ }

            return $"Error: {response.StatusCode} - {response.ReasonPhrase}. Response: {responseString.Substring(0, Math.Min(responseString.Length, 200))}"; // Обрезаем длинный ответ
        }


        // DTO для ошибок валидации FastAPI
        public class FastApiValidationError
        {
            [JsonPropertyName("loc")]
            public List<object> Loc { get; set; } // Может содержать строки или числа
            [JsonPropertyName("msg")]
            public string Msg { get; set; }
            [JsonPropertyName("type")]
            public string Type { get; set; }
        }
        public class FastApiValidationErrorWrapper
        {
            [JsonPropertyName("detail")]
            public List<FastApiValidationError> Detail { get; set; }
        }


        public async Task<(HealthCheckResponse HealthInfo, string ErrorMessage)> CheckHealthAsync()
        {
            return await GetAsync<HealthCheckResponse>("/health");
        }

        public async Task<(List<string> Tables, string ErrorMessage)> GetTablesAsync()
        {
            return await GetAsync<List<string>>("/schema/tables");
        }

        public async Task<(List<string> Views, string ErrorMessage)> GetViewsAsync()
        {
            return await GetAsync<List<string>>("/schema/views");
        }

        public async Task<(TableSchemaDetail Schema, string ErrorMessage)> GetTableOrViewSchemaAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return (null, "Table/View name cannot be empty.");
            return await GetAsync<TableSchemaDetail>($"/schema/table/{Uri.EscapeDataString(name)}");
        }

        public async Task<(List<ProcedureInfo> Procedures, string ErrorMessage)> GetProceduresAsync()
        {
            return await GetAsync<List<ProcedureInfo>>("/schema/procedures");
        }

        public async Task<(List<FunctionInfo> Functions, string ErrorMessage)> GetFunctionsAsync()
        {
            return await GetAsync<List<FunctionInfo>>("/schema/functions");
        }

        public async Task<(RoutineCallResponse Response, string ErrorMessage)> CallRoutineAsync(string routineType, string routineName, RoutineCallArgs payload)
        {
            if (string.IsNullOrWhiteSpace(routineName)) return (null, "Routine name cannot be empty.");
            payload ??= new RoutineCallArgs();

            string endpoint = routineType.ToLower() == "procedure"
                ? $"/routines/procedure/{Uri.EscapeDataString(routineName)}"
                : $"/routines/function/{Uri.EscapeDataString(routineName)}";

            return await PostAsync<RoutineCallResponse>(endpoint, payload);
        }

        public async Task<(PaginatedDataResponse<JsonElement> Response, string ErrorMessage)> ReadItemsAsync(
            string tableOrViewName,
            int limit = 100,
            int offset = 0,
            string sortBy = null,
            string fields = null,
            Dictionary<string, string> filters = null)
        {
            if (string.IsNullOrWhiteSpace(tableOrViewName)) return (null, "Table/View name cannot be empty.");

            var queryParams = new Dictionary<string, string>()
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
                    if (!string.IsNullOrWhiteSpace(filter.Key) && filter.Value != null) // Разрешаем пустую строку для filter.Value
                    {
                        queryParams[filter.Key] = filter.Value;
                    }
                }
            }
            // Используем Microsoft.AspNetCore.WebUtilities для построения строки запроса
            string requestUri = QueryHelpers.AddQueryString($"/{Uri.EscapeDataString(tableOrViewName)}", queryParams);

            // Для .NET Framework с System.Web:
            // var queryBuilder = HttpUtility.ParseQueryString(string.Empty);
            // queryBuilder["limit"] = limit.ToString();
            // queryBuilder["offset"] = offset.ToString();
            // if (!string.IsNullOrWhiteSpace(sortBy)) queryBuilder["sort_by"] = sortBy;
            // if (!string.IsNullOrWhiteSpace(fields)) queryBuilder["fields"] = fields;
            // if (filters != null) { foreach (var filter in filters) { if (!string.IsNullOrWhiteSpace(filter.Key) && filter.Value != null) queryBuilder[filter.Key] = filter.Value; } }
            // string queryString = queryBuilder.ToString();
            // string requestUri = $"/{Uri.EscapeDataString(tableOrViewName)}";
            // if (!string.IsNullOrEmpty(queryString)) requestUri += $"?{queryString}";

            return await GetAsync<PaginatedDataResponse<JsonElement>>(requestUri);
        }

        public async Task<(List<JsonElement> Items, string ErrorMessage)> ReadItemsByColumnValueAsync(
            string tableOrViewName,
            string columnName,
            string columnValue)
        {
            if (string.IsNullOrWhiteSpace(tableOrViewName)) return (null, "Table/View name cannot be empty.");
            if (string.IsNullOrWhiteSpace(columnName)) return (null, "Column name cannot be empty.");
            // columnValue может быть null или пустой строкой, это валидно для запроса
            columnValue ??= string.Empty; // Если null, преобразуем в пустую строку для Uri.EscapeDataString

            string requestUri = $"/{Uri.EscapeDataString(tableOrViewName)}/column/{Uri.EscapeDataString(columnName)}/{Uri.EscapeDataString(columnValue)}";
            return await GetAsync<List<JsonElement>>(requestUri);
        }
    }
}