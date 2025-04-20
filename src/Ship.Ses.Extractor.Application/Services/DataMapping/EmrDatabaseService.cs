using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Application.Services.DataMapping
{
    using Ship.Ses.Extractor.Domain.Repositories.DataMapping;
    using Ship.Ses.Extractor.Domain.ValueObjects;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class EmrDatabaseService : IEmrDatabaseService
    {
        private readonly IEmrDatabaseReader _databaseReader;

        public EmrDatabaseService(IEmrDatabaseReader databaseReader)
        {
            _databaseReader = databaseReader;
        }

        public Task<IEnumerable<string>> GetTableNamesAsync()
        {
            return _databaseReader.GetTableNamesAsync();
        }

        public Task<TableSchema> GetTableSchemaAsync(string tableName)
        {
            return _databaseReader.GetTableSchemaAsync(tableName);
        }

        public async Task<IEnumerable<TableSchema>> GetAllTablesSchemaAsync()
        {
            var tableNames = await _databaseReader.GetTableNamesAsync();
            var tables = new List<TableSchema>();

            foreach (var tableName in tableNames)
            {
                var schema = await _databaseReader.GetTableSchemaAsync(tableName);
                tables.Add(schema);
            }

            return tables;
        }

        public Task TestConnectionAsync()
        {
            return _databaseReader.TestConnectionAsync();
        }
    }

    // Internal interface used by EmrDatabaseService
    public interface IEmrDatabaseReader
    {
        Task<IEnumerable<string>> GetTableNamesAsync();
        Task<TableSchema> GetTableSchemaAsync(string tableName);
        Task TestConnectionAsync();
    }
}
