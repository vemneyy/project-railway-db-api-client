using System.Text.Json.Serialization;

namespace ApiManagerApp.Classes
{
    public class ProcedureInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("arguments_signature")]
        public string? ArgumentsSignature { get; set; }
    }
}
