using System.Text.Json.Serialization;

namespace ApiManagerApp.Classes
{
    public class HealthCheckResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("database_connection")]
        public string? DatabaseConnection { get; set; }
    }
}
