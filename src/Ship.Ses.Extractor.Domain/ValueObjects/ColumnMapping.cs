using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Domain.ValueObjects
{
    public class ColumnMapping
    {
        public string EmrTable { get; }
        public string EmrColumn { get; }
        public string FhirPath { get; }
        public string TransformationExpression { get; }

        public ColumnMapping(string emrTable, string emrColumn, string fhirPath, string transformationExpression = null)
        {
            EmrTable = emrTable ?? throw new ArgumentNullException(nameof(emrTable));
            EmrColumn = emrColumn ?? throw new ArgumentNullException(nameof(emrColumn));
            FhirPath = fhirPath ?? throw new ArgumentNullException(nameof(fhirPath));
            TransformationExpression = transformationExpression;
        }
    }
}
