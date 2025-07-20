using Ship.Ses.Extractor.Domain.Entities.Extractor;

namespace Ship.Ses.Extractor.Domain.Entities.Observation
{
    public class ObservationSyncRecord : FhirSyncRecord
    {
        public override string CollectionName => "transformed_pool_observations";

        public ObservationSyncRecord()
        {
            ResourceType = "Observation";
        }
    }
}