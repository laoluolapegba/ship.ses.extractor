using Microsoft.AspNetCore.Mvc;
using Ship.Ses.Extractor.Application.DTOs;
using Ship.Ses.Extractor.Domain.Repositories.DataMapping;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Ship.Ses.Extractor.Presentation.Api.Controllers.v1
{


    [ApiController]
    [Route("api/emr")]
    public class EmrController : ControllerBase
    {
        private readonly IEmrDatabaseService _emrDatabaseService;
        private readonly ILogger<EmrController> _logger;

        public EmrController(IEmrDatabaseService emrDatabaseService, ILogger<EmrController> logger)
        {
            _emrDatabaseService = emrDatabaseService;
            _logger = logger;
        }

        [HttpGet("tables")]
        public async Task<ActionResult<IEnumerable<EmrTableDto>>> GetTables()
        {
            try
            {
                var tables = await _emrDatabaseService.GetAllTablesSchemaAsync();

                var tableDtos = tables.Select(t => new EmrTableDto
                {
                    Name = t.TableName,
                    Columns = t.Columns.Select(c => new EmrColumnDto
                    {
                        Name = c.Name,
                        DataType = c.DataType,
                        IsNullable = c.IsNullable,
                        IsPrimaryKey = c.IsPrimaryKey
                    }).ToList()
                }).ToList();

                return Ok(tableDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving EMR database tables");
                return StatusCode(500, "Error retrieving database schema information");
            }
        }

        [HttpGet("tables/{tableName}")]
        public async Task<ActionResult<EmrTableDto>> GetTableSchema(string tableName)
        {
            try
            {
                var table = await _emrDatabaseService.GetTableSchemaAsync(tableName);

                var tableDto = new EmrTableDto
                {
                    Name = table.TableName,
                    Columns = table.Columns.Select(c => new EmrColumnDto
                    {
                        Name = c.Name,
                        DataType = c.DataType,
                        IsNullable = c.IsNullable,
                        IsPrimaryKey = c.IsPrimaryKey
                    }).ToList()
                };

                return Ok(tableDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schema for table {TableName}", tableName);
                return StatusCode(500, $"Error retrieving schema for table {tableName}");
            }
        }

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                await _emrDatabaseService.TestConnectionAsync();
                return Ok(new { message = "Connection successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to EMR database");
                return StatusCode(500, "Error connecting to EMR database");
            }
        }
    }
}
