using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Ship.Ses.Extractor.Application.DTOs;
using Ship.Ses.Extractor.Application.Interfaces;
using Ship.Ses.Extractor.Domain.Repositories.DataMapping;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace Ship.Ses.Extractor.WebApi.Controllers
{


    [ApiController]
    [Route("api/mappings")]
    public class MappingsController : ControllerBase
    {
        private readonly IMappingService _mappingService;
        private readonly IFhirResourceService _fhirResourceService;
        private readonly ILogger<MappingsController> _logger;

        public MappingsController(
            IMappingService mappingService,
            IFhirResourceService fhirResourceService,
            ILogger<MappingsController> logger)
        {
            _mappingService = mappingService;
            _fhirResourceService = fhirResourceService;
            _logger = logger;
        }

        [HttpGet("resource-types")]
        public async Task<ActionResult<IEnumerable<FhirResourceTypeDto>>> GetResourceTypes()
        {
            try
            {
                var resourceTypes = await _fhirResourceService.GetAllResourceTypesAsync();

                var resourceTypeDtos = resourceTypes.Select(rt => new FhirResourceTypeDto
                {
                    Id = rt.Id,
                    Name = rt.Name,
                    Structure = rt.Structure
                }).ToList();

                return Ok(resourceTypeDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving FHIR resource types");
                return StatusCode(500, "Error retrieving FHIR resource types");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MappingDefinitionDto>>> GetMappings()
        {
            try
            {
                var mappings = await _mappingService.GetAllMappingsAsync();
                return Ok(mappings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mappings");
                return StatusCode(500, "Error retrieving mappings");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MappingDefinitionDto>> GetMapping(Guid id)
        {
            try
            {
                var mapping = await _mappingService.GetMappingByIdAsync(id);

                if (mapping == null)
                {
                    return NotFound();
                }

                return Ok(mapping);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mapping with ID {MappingId}", id);
                return StatusCode(500, $"Error retrieving mapping with ID {id}");
            }
        }

        [HttpGet("resource-type/{resourceTypeId}")]
        public async Task<ActionResult<IEnumerable<MappingDefinitionDto>>> GetMappingsByResourceType(int resourceTypeId)
        {
            try
            {
                var mappings = await _mappingService.GetMappingsByResourceTypeAsync(resourceTypeId);
                return Ok(mappings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mappings for resource type {ResourceTypeId}", resourceTypeId);
                return StatusCode(500, $"Error retrieving mappings for resource type {resourceTypeId}");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> CreateMapping(MappingDefinitionDto mappingDto)
        {
            try
            {
                var id = await _mappingService.CreateMappingAsync(mappingDto);
                return CreatedAtAction(nameof(GetMapping), new { id }, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating mapping");
                return StatusCode(500, "Error creating mapping");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMapping(Guid id, MappingDefinitionDto mappingDto)
        {
            if (id != mappingDto.Id)
            {
                return BadRequest("ID mismatch");
            }

            try
            {
                await _mappingService.UpdateMappingAsync(mappingDto);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating mapping with ID {MappingId}", id);
                return StatusCode(500, $"Error updating mapping with ID {id}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMapping(Guid id)
        {
            try
            {
                await _mappingService.DeleteMappingAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting mapping with ID {MappingId}", id);
                return StatusCode(500, $"Error deleting mapping with ID {id}");
            }
        }
    }
}
