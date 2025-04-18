using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Domain.Models.Extractor
{
    //public class TableMapping
    //{
    //    public string ResourceType { get; set; } = default!;
    //    public string TableName { get; set; } = default!;
    //    public Dictionary<string, string> FieldMappings { get; set; } = new(); // e.g. EMR_column -> FHIR_property
    //}
    public class TableMapping
    {
        public string ResourceType { get; set; } = default!;
        public string TableName { get; set; } = default!;
        public List<FieldMapping> Fields { get; set; } = new();
    }
}
