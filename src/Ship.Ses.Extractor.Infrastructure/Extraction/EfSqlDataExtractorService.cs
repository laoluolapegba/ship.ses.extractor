using Microsoft.EntityFrameworkCore;
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

        public EfSqlDataExtractorService(ExtractorDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<IDictionary<string, object>>> ExtractAsync(TableMapping mapping, CancellationToken cancellationToken = default)
        {
            var results = new List<IDictionary<string, object>>();
            var sql = $"SELECT * FROM \"{mapping.TableName}\""; // Escape table name for PostgreSQL

            await using var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = await reader.IsDBNullAsync(i, cancellationToken) ? null : reader.GetValue(i);
                }
                results.Add(row);
            }

            return results;
        }
    }

}
