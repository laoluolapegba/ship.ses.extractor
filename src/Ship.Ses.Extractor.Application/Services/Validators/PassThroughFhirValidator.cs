using Ship.Ses.Extractor.Domain.Repositories.Validator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Application.Services.Validators
{
    public class PassThroughFhirValidator : IFhirValidator
    {
        public Task<bool> IsValidAsync(JsonObject fhirResource, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true); // Stub: always valid
        }
    }

}
