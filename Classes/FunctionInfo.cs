using System.Text.Json.Serialization;

namespace ApiManagerApp.Classes
{
    public class FunctionInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("return_type")]
        public string? ReturnType { get; set; }

        [JsonPropertyName("arguments_signature")]
        public string? ArgumentsSignature { get; set; }

        [JsonPropertyName("precise_return_type")]
        public string? PreciseReturnType { get; set; }
    }
}
