namespace Ship.Ses.Extractor.UI.BlazorWeb.Models.UI
{
    public class MappingConnection
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public MappingNodeReference Source { get; set; }
        public MappingNodeReference Target { get; set; }
        public string TransformationExpression { get; set; }

        public MappingConnection()
        {
        }

        public MappingConnection(MappingNodeReference source, MappingNodeReference target, string transformationExpression = null)
        {
            Source = source;
            Target = target;
            TransformationExpression = transformationExpression;
        }
    }

    public class MappingNodeReference
    {
        public string NodeId { get; set; }
        public string NodeType { get; set; } // EMR or FHIR
        public string Table { get; set; } // For EMR
        public string Column { get; set; } // For EMR
        public string Path { get; set; } // For FHIR

        public MappingNodeReference()
        {
        }

        public MappingNodeReference(string nodeId, string nodeType)
        {
            NodeId = nodeId;
            NodeType = nodeType;
        }
    }
}
