using System.Text.Json.Serialization;

namespace ApiManagerApp.Classes
{
    public class PaginatedDataResponse<T>
    {
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("data")]
        public List<T>? Data { get; set; }
    }
}
