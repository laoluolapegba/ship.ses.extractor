using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ship.Ses.Extractor.Application.Services;
using Ship.Ses.Extractor.Domain.Models.Extractor;
using Ship.Ses.Extractor.Infrastructure.Persistance.Contexts;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Infrastructure.Extraction
{
    public class EfSqlDataExtractorService : IDataExtractorService
    {
        private readonly ExtractorDbContext _context;
        private readonly ILogger<EfSqlDataExtractorService> _logger;

        public EfSqlDataExtractorService(ExtractorDbContext context, ILogger<EfSqlDataExtractorService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<IDictionary<string, object>>> ExtractAsync(TableMapping mapping, CancellationToken cancellationToken = default)
        {
            var results = new List<IDictionary<string, object>>();
            var sql = $"SELECT * FROM {mapping.TableName}";

            try
            {
                _logger.LogInformation("📥 Starting extraction from table '{TableName}' for resource '{ResourceType}'", mapping.TableName, mapping.ResourceType);

                await using var connection = _context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync(cancellationToken);
                    _logger.LogDebug("Opened database connection to {DataSource}", connection.DataSource);
                }

                await using var command = connection.CreateCommand();
                command.CommandText = sql;

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var row = new Dictionary<string, object>();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);

                        try
                        {
                            var value = await reader.IsDBNullAsync(i, cancellationToken)
                                ? null
                                : reader.GetValue(i); // This is where the crash happens

                            row[columnName] = value;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "❌ Error reading column '{Column}' at index {Index} in table '{TableName}'. Value skipped.",
                                columnName, i, mapping.TableName);

                            row[columnName] = null; // Optionally: use "InvalidDate" as a string placeholder
                        }
                    }

                    results.Add(row);
                }

                _logger.LogInformation("📦 Extracted {Count} rows from '{TableName}'", results.Count, mapping.TableName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to extract from table '{TableName}'", mapping.TableName);
                throw; // Re-throw to let higher-level logic handle it (e.g., retry or sync tracking)
            }

            return results;
        }
    }

}
