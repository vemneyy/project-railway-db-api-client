using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiManagerApp.Classes
{
    public class RoutineCallResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("procedure")]
        public string? Procedure { get; set; }

        [JsonPropertyName("function")]
        public string? Function { get; set; }

        [JsonPropertyName("args_used")]
        public List<object>? ArgsUsed { get; set; }

        [JsonPropertyName("result")]
        public JsonElement Result { get; set; }
    }
}
