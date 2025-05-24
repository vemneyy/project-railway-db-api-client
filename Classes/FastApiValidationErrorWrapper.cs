using System.Text.Json.Serialization;

namespace ApiManagerApp.Classes
{
    public class FastApiValidationErrorWrapper
    {
        [JsonPropertyName("detail")]
        public List<FastApiValidationError>? Detail { get; set; }
    }
}
