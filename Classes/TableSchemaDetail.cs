using System.Text.Json.Serialization;

namespace ApiManagerApp.Classes
{
    public class TableSchemaDetail
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("db_table_name")]
        public string? DbTableName { get; set; }

        [JsonPropertyName("db_schema")]
        public string? DbSchema { get; set; }

        [JsonPropertyName("is_view")]
        public bool IsView { get; set; }

        [JsonPropertyName("columns")]
        public List<ColumnInfo>? Columns { get; set; }

        [JsonPropertyName("primary_keys")]
        public List<string>? PrimaryKeys { get; set; }

        [JsonPropertyName("foreign_keys")]
        public List<ForeignKeyInfo>? ForeignKeys { get; set; }

        [JsonPropertyName("pydantic_models")]
        public PydanticModelsInfo? PydanticModels { get; set; }
    }
}
