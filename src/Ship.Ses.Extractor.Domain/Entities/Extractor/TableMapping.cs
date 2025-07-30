using Ship.Ses.Extractor.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Ship.Ses.Extractor.Domain.Models.Extractor
{
    //public class TableMapping
    //{
    //    public string ResourceType { get; set; } = default!;
    //    public string TableName { get; set; } = default!;
    //    public Dictionary<string, string> FieldMappings { get; set; } = new(); // e.g. EMR_column -> FHIR_property
    //}
    public class TableMapping ///this is the version that was commented out in the original code
    {
        [JsonPropertyName("resourceType")]
        public string ResourceType { get; set; } = default!;
    [JsonPropertyName("tableName")]
    public string TableName { get; set; } = default!;
    [JsonPropertyName("fields")]
    public List<FieldMapping> Fields { get; set; } = new();

    [JsonPropertyName("constants")]
    public Dictionary<string, JsonNode> Constants { get; set; } = new();
}


    // This is the abstract class for TableMapping, which can be used with different field mapping types
    //public abstract class TableMapping
    //{
    //    [JsonPropertyName("resourceType")]
    //    public string ResourceType { get; set; } = default!;

    //    [JsonPropertyName("tableName")]
    //    public string TableName { get; set; } = default!;

    //    [JsonPropertyName("constants")]
    //    public Dictionary<string, JsonNode> Constants { get; set; } = new();

    //    [JsonIgnore]
    //    public abstract IList<FieldMapping> FieldsUntyped { get; }
    //}
    //public class TableMapping<TFieldMapping> : TableMapping where TFieldMapping : FieldMapping
    //{
    //    [JsonPropertyName("fields")]
    //    public List<TFieldMapping> Fields { get; set; } = new();

    //    [JsonIgnore]
    //    public override IList<FieldMapping> FieldsUntyped => Fields.Cast<FieldMapping>().ToList();
    //}

   
}
