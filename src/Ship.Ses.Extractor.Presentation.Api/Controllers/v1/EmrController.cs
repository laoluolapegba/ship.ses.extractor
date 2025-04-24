using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Ship.Ses.Extractor.Application.DTOs;
using Ship.Ses.Extractor.Domain.Repositories.DataMapping;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Ship.Ses.Extractor.Presentation.Api.Controllers.v1
{


    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/emr")]
    public class EmrController : ControllerBase
    {
        private readonly IEmrDatabaseService _emrDatabaseService;
        private readonly ILogger<EmrController> _logger;
        private readonly IEmrConnectionRepository _connectionRepository;

        public EmrController(
            IEmrDatabaseService emrDatabaseService,
            IEmrConnectionRepository connectionRepository,
            ILogger<EmrController> logger)
        {
            _emrDatabaseService = emrDatabaseService;
            _connectionRepository = connectionRepository;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a list of EMR tables.
        /// </summary>
        /// <returns>A list of EMR tables.</returns>
        [HttpGet("tables")]
        [ProducesResponseType(typeof(IEnumerable<EmrTableDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTables()
        {
            try
            {
                var tableNames = await _emrDatabaseService.GetTableNamesAsync();
                var tableDtos = tableNames.Select(t => new EmrTableDto
                {
                    Name = t,
                    Columns = new List<EmrColumnDto>()
                });
                return Ok(tableDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving EMR database tables.");
                return StatusCode(500, "Error retrieving database schema information.");
            }
        }

        /// <summary>
        /// Retrieves the schema for a specific EMR table.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <returns>The schema of the specified table.</returns>
        [HttpGet("tables/{tableName}")]
        [ProducesResponseType(typeof(EmrTableDto), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTableSchema(string tableName)
        {
            try
            {
                var schema = await _emrDatabaseService.GetTableSchemaAsync(tableName);
                var columnDtos = schema.Columns.Select(c => new EmrColumnDto
                {
                    Name = c.Name,
                    DataType = c.DataType,
                    IsNullable = c.IsNullable,
                    IsPrimaryKey = c.IsPrimaryKey
                }).ToList();

                var tableDto = new EmrTableDto
                {
                    Name = schema.TableName,
                    Columns = columnDtos
                };

                return Ok(tableDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schema for table {TableName}", tableName);
                return StatusCode(500, $"Error retrieving schema for table {tableName}");
            }
        }

        /// <summary>
        /// Tests the EMR database connection.
        /// </summary>
        /// <returns>Result of the connection test.</returns>
        [HttpGet("test-connection")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                await _emrDatabaseService.TestConnectionAsync();
                return Ok(new { message = "Connection successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to EMR database.");
                return StatusCode(500, "Error connecting to EMR database.");
            }
        }

        /// <summary>
        /// Retrieves a list of active EMR connections.
        /// </summary>
        /// <returns>A list of EMR connections.</returns>
        [HttpGet("connections")]
        [ProducesResponseType(typeof(IEnumerable<EmrConnectionDto>), 200)]
        public async Task<IActionResult> GetConnections()
        {
            var connections = await _connectionRepository.GetActiveAsync();
            var connectionDtos = connections.Select(c => new EmrConnectionDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                DatabaseType = c.DatabaseType,
                Server = c.Server,
                Port = c.Port,
                DatabaseName = c.DatabaseName,
                Username = c.Username
                // Note: Password is intentionally not returned for security
            });

            return Ok(connectionDtos);
        }

        /// <summary>
        /// Selects a specific EMR connection by ID.
        /// </summary>
        /// <param name="id">The ID of the EMR connection.</param>
        /// <returns>Result of the selection operation.</returns>
        [HttpPost("connections/select/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SelectConnection(int id)
        {
            try
            {
                await _emrDatabaseService.SelectConnectionAsync(id);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "EMR connection with ID {Id} not found.", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting EMR connection with ID {Id}.", id);
                return StatusCode(500, $"Error selecting EMR connection with ID {id}.");
            }
        }

        /// <summary>
        /// Tests a specific EMR connection by ID.
        /// </summary>
        /// <param name="id">The ID of the EMR connection.</param>
        /// <returns>Result of the connection test.</returns>
        [HttpPost("test-connection/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> TestConnectionById(int id)
        {
            try
            {
                await _emrDatabaseService.SelectConnectionAsync(id);
                await _emrDatabaseService.TestConnectionAsync();
                return Ok(new { message = "Connection successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing EMR connection with ID {Id}.", id);
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
