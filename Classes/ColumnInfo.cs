using System.Text.Json.Serialization;

namespace ApiManagerApp.Classes
{
    public class ColumnInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("python_type")]
        public string? PythonType { get; set; }

        [JsonPropertyName("nullable")]
        public bool Nullable { get; set; }

        [JsonPropertyName("primary_key")]
        public bool PrimaryKey { get; set; }

        [JsonPropertyName("default")]
        public string? Default { get; set; }

        [JsonPropertyName("server_default")]
        public string? ServerDefault { get; set; }
    }
}
