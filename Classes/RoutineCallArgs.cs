using System.Text.Json.Serialization;

namespace ApiManagerApp.Classes
{
    public class RoutineCallArgs
    {
        [JsonPropertyName("args")]
        public List<object> Args { get; set; } = new List<object>();

        [JsonPropertyName("kwargs")]
        public Dictionary<string, object> Kwargs { get; set; } = new Dictionary<string, object>();
    }
}
