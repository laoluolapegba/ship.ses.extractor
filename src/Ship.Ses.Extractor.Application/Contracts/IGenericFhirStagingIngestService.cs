using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Application.Contracts
{
    public interface IGenericFhirStagingIngestService
    {
        Task<int> IngestPatientsAsync(CancellationToken ct);
        Task<int> IngestNonPatientsAsync(CancellationToken ct);
    }

}
