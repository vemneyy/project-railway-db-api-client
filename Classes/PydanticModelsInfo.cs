using System.Text.Json.Serialization;

namespace ApiManagerApp.Classes
{
    public class PydanticModelsInfo
    {
        [JsonPropertyName("read")]
        public string? Read { get; set; }

        [JsonPropertyName("create")]
        public string? Create { get; set; }

        [JsonPropertyName("update")]
        public string? Update { get; set; }
    }
}
