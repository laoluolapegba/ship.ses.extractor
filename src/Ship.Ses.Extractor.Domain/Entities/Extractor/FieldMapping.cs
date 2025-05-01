using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Domain.Models.Extractor
{
    public class FieldMapping
    {
        [JsonPropertyName("emrField")]
        public string EmrField { get; set; } = default!;
        [JsonPropertyName("fhirPath")]
        public string FhirPath { get; set; } = default!;
        [JsonPropertyName("dataType")]
        public string? DataType { get; set; } // "string", "date", etc.
        [JsonPropertyName("format")]
        public string? Format { get; set; }   // e.g., "yyyy-MM-dd"
        [JsonPropertyName("default")]
        public string? Default { get; set; }
        [JsonPropertyName("required")]
        public bool Required { get; set; } = false; // Default to false if not explicitly set
        [JsonPropertyName("template")]
        public string? Template { get; set; }

        [JsonPropertyName("emrFieldMap")]
        public Dictionary<string, string>? EmrFieldMap { get; set; }

        [JsonPropertyName("defaults")]
        public Dictionary<string, object>? Defaults { get; set; }

        [JsonPropertyName("valueSet")]
        public Dictionary<string, object>? ValueSet { get; set; }

    }
}
