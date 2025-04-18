using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Domain.Repositories.Validator
{
    public interface IFhirValidator
    {
        Task<bool> IsValidAsync(JsonObject fhirResource, CancellationToken cancellationToken = default);
    }

}
