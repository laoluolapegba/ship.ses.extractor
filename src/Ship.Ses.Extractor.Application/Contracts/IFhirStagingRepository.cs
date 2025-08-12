using Ship.Ses.Extractor.Domain.Entities.Extractor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Application.Contracts
{
    public interface IFhirStagingRepository
    {
        // Locks a batch for processing (so multiple workers won't double-process)
        Task<IReadOnlyList<FhirStagingRecord>> DequeueBatchAsync(int batchSize, CancellationToken ct);

        Task MarkProcessedAsync(long id, CancellationToken ct);
        Task MarkFailedAsync(long id, string reason, CancellationToken ct); 
    }
}
