using Ship.Ses.Extractor.Domain.Entities.Extractor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Domain.Entities.Generic
{
    public class GenericResourceSyncRecord : FhirSyncRecord
    {
        public override string CollectionName => "transformed_pool_resources";
    }
}
