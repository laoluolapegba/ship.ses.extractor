namespace Ship.Ses.Extractor.UI.BlazorWeb.Models.UI
{
    public class MappingNode
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string Type { get; set; } // EMR or FHIR
        public string ParentId { get; set; }
        public bool IsSelected { get; set; }
        public bool IsExpanded { get; set; }
        public string DataType { get; set; }

        // EMR-specific properties
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public bool IsPrimaryKey { get; set; }

        // FHIR-specific properties
        public string Path { get; set; }
        public bool IsRequired { get; set; }
    }
}
