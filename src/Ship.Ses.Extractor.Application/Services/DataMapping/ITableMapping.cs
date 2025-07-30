using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Application.Services.DataMapping
{
    public interface ITableMapping
    {
        string ResourceType { get; }
        string TableName { get; }
        IList FieldsAsList { get; }
    }
    //public class TableMapping<TField> : ITableMapping
    //{
    //    [JsonPropertyName("resourceType")]
    //    public string ResourceType { get; set; } = default!;

    //    [JsonPropertyName("tableName")]
    //    public string TableName { get; set; } = default!;

    //    [JsonPropertyName("fields")]
    //    public List<TField> Fields { get; set; } = new();

    //    [JsonPropertyName("constants")]
    //    public Dictionary<string, JsonNode> Constants { get; set; } = new();

    //    public IList FieldsAsList => Fields!;
}

