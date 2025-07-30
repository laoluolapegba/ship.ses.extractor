using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
namespace Ship.Ses.Extractor.Domain.Entities.Extractor
{
    using System.Text.Json.Nodes;
    

    public class BaseFieldMapping
    {
        [JsonPropertyName("fhirPath")]
        public string FhirPath { get; set; } = default!;

        [JsonPropertyName("emrField")]
        public string? EmrField { get; set; }

        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("dataType")]
        public string? DataType { get; set; }

        [JsonPropertyName("format")]
        public string? Format { get; set; }

        [JsonPropertyName("default")]
        public string? Default { get; set; }

        [JsonPropertyName("defaults")]
        public Dictionary<string, object>? Defaults { get; set; }

        [JsonPropertyName("required")]
        public bool Required { get; set; } = false;
    }

}
