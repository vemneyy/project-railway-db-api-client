using System.Text.Json.Serialization;

namespace ApiManagerApp.Classes
{
    public class ForeignKeyInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("constrained_columns")]
        public List<string>? ConstrainedColumns { get; set; }

        [JsonPropertyName("referred_schema")]
        public string? ReferredSchema { get; set; }

        [JsonPropertyName("referred_table")]
        public string? ReferredTable { get; set; }

        [JsonPropertyName("referred_columns")]
        public List<string>? ReferredColumns { get; set; }
    }
}
