using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Domain.Models.Extractor
{
    public class FieldMapping
    {
        public string EmrField { get; set; } = default!;
        public string FhirPath { get; set; } = default!;
        public string? DataType { get; set; } // "string", "date", etc.
        public string? Format { get; set; }   // e.g., "yyyy-MM-dd"
        public string? Default { get; set; }
    }
}
