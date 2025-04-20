using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Infrastructure.Services
{
    using Microsoft.Extensions.Logging;
    using MySql.Data.MySqlClient;
    using Ship.Ses.Extractor.Application.Services;
    using Ship.Ses.Extractor.Application.Services.DataMapping;
    using Ship.Ses.Extractor.Domain.ValueObjects;
    using Ship.Ses.Extractor.Infrastructure.Persistance.Repositories;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Linq;
    using System.Threading.Tasks;

    public class EmrDatabaseReader : IEmrDatabaseReader
    {
        private readonly EmrDbContextFactory _dbContextFactory;
        private readonly ILogger<EmrDatabaseReader> _logger;

        public EmrDatabaseReader(EmrDbContextFactory dbContextFactory, ILogger<EmrDatabaseReader> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<string>> GetTableNamesAsync()
        {
            var tables = new List<string>();

            try
            {
                using var connection = _dbContextFactory.CreateConnection();
                await connection.OpenAsync();

                tables = await GetTableNamesForConnectionType(connection);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving database tables");
                throw;
            }

            return tables;
        }

        public async Task<TableSchema> GetTableSchemaAsync(string tableName)
        {
            try
            {
                using var connection = _dbContextFactory.CreateConnection();
                await connection.OpenAsync();

                var columns = await GetColumnsForTable(connection, tableName);
                return new TableSchema(tableName, columns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schema for table {TableName}", tableName);
                throw;
            }
        }

        public async Task TestConnectionAsync()
        {
            try
            {
                using var connection = _dbContextFactory.CreateConnection();
                await connection.OpenAsync();
                _logger.LogInformation("Successfully connected to EMR database");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to EMR database");
                throw;
            }
        }

        private async Task<List<string>> GetTableNamesForConnectionType(DbConnection connection)
        {
            var tables = new List<string>();

            if (connection is MySqlConnection)
            {
                var schema = connection.Database;
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_schema = @schema AND table_type = 'BASE TABLE'";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@schema";
                parameter.Value = schema;
                command.Parameters.Add(parameter);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }
            }
            else if (connection is Npgsql.NpgsqlConnection)
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }
            }
            else if (connection is Microsoft.Data.SqlClient.SqlConnection)
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_type = 'BASE TABLE'";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }
            }

            return tables;
        }

        private async Task<List<ColumnSchema>> GetColumnsForTable(DbConnection connection, string tableName)
        {
            var columns = new List<ColumnSchema>();

            if (connection is MySqlConnection)
            {
                var schema = connection.Database;
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT 
                        column_name, 
                        data_type,
                        is_nullable,
                        CASE WHEN column_key = 'PRI' THEN 1 ELSE 0 END as is_primary_key
                    FROM 
                        information_schema.columns 
                    WHERE 
                        table_schema = @schema 
                        AND table_name = @tableName
                    ORDER BY 
                        ordinal_position";

                var schemaParam = command.CreateParameter();
                schemaParam.ParameterName = "@schema";
                schemaParam.Value = schema;
                command.Parameters.Add(schemaParam);

                var tableParam = command.CreateParameter();
                tableParam.ParameterName = "@tableName";
                tableParam.Value = tableName;
                command.Parameters.Add(tableParam);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    columns.Add(new ColumnSchema(
                        reader.GetString(0),
                        reader.GetString(1),
                        reader.GetString(2).Equals("YES", StringComparison.OrdinalIgnoreCase),
                        reader.GetInt32(3) == 1));
                }
            }
            else if (connection is Npgsql.NpgsqlConnection)
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT 
                        column_name, 
                        data_type,
                        is_nullable,
                        CASE WHEN pk.column_name IS NOT NULL THEN true ELSE false END as is_primary_key
                    FROM 
                        information_schema.columns c
                    LEFT JOIN (
                        SELECT kcu.column_name 
                        FROM information_schema.table_constraints tc
                        JOIN information_schema.key_column_usage kcu 
                        ON tc.constraint_name = kcu.constraint_name
                        WHERE tc.constraint_type = 'PRIMARY KEY' 
                        AND tc.table_name = @tableName
                    ) pk ON c.column_name = pk.column_name
                    WHERE 
                        c.table_schema = 'public' 
                        AND c.table_name = @tableName
                    ORDER BY 
                        c.ordinal_position";

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@tableName";
                parameter.Value = tableName;
                command.Parameters.Add(parameter);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    columns.Add(new ColumnSchema(
                        reader.GetString(0),
                        reader.GetString(1),
                        reader.GetString(2).Equals("YES", StringComparison.OrdinalIgnoreCase),
                        reader.GetBoolean(3)));
                }
            }
            else if (connection is Microsoft.Data.SqlClient.SqlConnection)
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT 
                        c.COLUMN_NAME, 
                        c.DATA_TYPE,
                        c.IS_NULLABLE,
                        CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as is_primary_key
                    FROM 
                        INFORMATION_SCHEMA.COLUMNS c
                    LEFT JOIN (
                        SELECT ku.COLUMN_NAME
                        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS tc
                        INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS ku
                        ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                        WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY' 
                        AND ku.TABLE_NAME = @tableName
                    ) pk ON c.COLUMN_NAME = pk.COLUMN_NAME
                    WHERE 
                        c.TABLE_NAME = @tableName
                    ORDER BY 
                        c.ORDINAL_POSITION";

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@tableName";
                parameter.Value = tableName;
                command.Parameters.Add(parameter);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    columns.Add(new ColumnSchema(
                        reader.GetString(0),
                        reader.GetString(1),
                        reader.GetString(2).Equals("YES", StringComparison.OrdinalIgnoreCase),
                        reader.GetInt32(3) == 1));
                }
            }

            return columns;
        }
    }
}
