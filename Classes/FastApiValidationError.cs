using System.Text.Json.Serialization;

namespace ApiManagerApp.Classes
{
    public class FastApiValidationError
    {
        [JsonPropertyName("loc")]
        public List<object>? Loc { get; set; }
        [JsonPropertyName("msg")]
        public string? Msg { get; set; }
        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }
}
