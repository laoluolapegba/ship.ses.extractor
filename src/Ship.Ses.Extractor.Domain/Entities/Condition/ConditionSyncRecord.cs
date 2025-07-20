using Ship.Ses.Extractor.Domain.Entities.Extractor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Domain.Entities.Condition
{
    public class ConditionSyncRecord : FhirSyncRecord
    {
        public override string CollectionName => "transformed_pool_conditions";

        public ConditionSyncRecord()
        {
            ResourceType = "Condition";
        }
    }
}
