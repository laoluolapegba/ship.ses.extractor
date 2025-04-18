using Microsoft.Extensions.Configuration;
using Ship.Ses.Extractor.Application.Services;
using Ship.Ses.Extractor.Domain.Models.Extractor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Infrastructure.Configuration
{
    public class JsonTableMappingService : ITableMappingService
    {
        private readonly string _rootPath;

        public JsonTableMappingService(IConfiguration config)
        {
            _rootPath = config["TableMappings:RootPath"]
                ?? throw new InvalidOperationException("Mapping root path not configured");
        }

        public async Task<TableMapping> GetMappingForResourceAsync(string resourceType, CancellationToken cancellationToken = default)
        {
            var filePath = Path.Combine(_rootPath, $"{resourceType.ToLowerInvariant()}.mapping.json");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Mapping file not found: {filePath}");

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            return JsonSerializer.Deserialize<TableMapping>(json)
                   ?? throw new InvalidOperationException($"Invalid mapping JSON: {filePath}");
        }
    }


}
