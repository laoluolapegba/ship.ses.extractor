using Ship.Ses.Extractor.Domain.Shared;
using System.Text.Json.Serialization;
namespace Ship.Ses.Extractor.Domain.Entities.Observation
{


    public class ObservationFieldMapping : FieldMapping
    {
        //[JsonPropertyName("emrFieldMap")]
        //public Dictionary<string, string>? EmrFieldMap { get; set; }

        //[JsonPropertyName("valueSet")]
        //public Dictionary<string, object>? ValueSet { get; set; }

        // Extend with condition-specific metadata if needed
    }

}
