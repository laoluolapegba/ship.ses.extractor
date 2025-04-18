using MongoDB.Bson;
using Ship.Ses.Extractor.Application.Services;
using Ship.Ses.Extractor.Domain.Entities.Patients;
using Ship.Ses.Extractor.Domain.Repositories.Extractor;
using Ship.Ses.Extractor.Domain.Repositories.Transformer;
using Ship.Ses.Extractor.Domain.Repositories.Validator;
using Ship.Ses.Extractor.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Application.Services.Extractors
{
    public class PatientResourceExtractor
    {
        private readonly ITableMappingService _mappingService;
        private readonly IDataExtractorService _dataExtractor;
        private readonly IResourceTransformer<JsonObject> _transformer;
        private readonly IFhirValidator _validator;
        private readonly IFhirSyncRepository<PatientSyncRecord> _repository;

        public PatientResourceExtractor(
            ITableMappingService mappingService,
            IDataExtractorService dataExtractor,
            IResourceTransformer<JsonObject> transformer,
            IFhirValidator validator,
            IFhirSyncRepository<PatientSyncRecord> repository)
        {
            _mappingService = mappingService;
            _dataExtractor = dataExtractor;
            _transformer = transformer;
            _validator = validator;
            _repository = repository;
        }

        public async Task ExtractAndPersistAsync(CancellationToken cancellationToken = default)
        {
            var mapping = await _mappingService.GetMappingForResourceAsync("Patient", cancellationToken);
            var rawRows = await _dataExtractor.ExtractAsync(mapping, cancellationToken);

            foreach (var row in rawRows)
            {
                var json = _transformer.Transform(row, mapping);
                var resourceId = json["id"]?.GetValue<string>() ?? Guid.NewGuid().ToString();

                var record = new PatientSyncRecord
                {
                    ResourceType = mapping.ResourceType,
                    ResourceId = resourceId,
                    FhirJson = BsonDocument.Parse(json.ToJsonString()),
                    CreatedDate = DateTime.UtcNow,
                    Status = "Pending",
                    RetryCount = 0
                };

                if (await _validator.IsValidAsync(json, cancellationToken))
                {
                    await _repository.InsertAsync(record, cancellationToken);
                }
                else
                {
                    record.Status = "Failed";
                    record.ErrorMessage = "Validation failed"; // Expand later
                    await _repository.InsertAsync(record, cancellationToken);
                }
            }
        }
    }



}
