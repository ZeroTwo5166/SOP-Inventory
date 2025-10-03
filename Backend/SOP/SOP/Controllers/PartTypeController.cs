using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOP.Entities;
using SOP.Repositories;

namespace SOP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PartTypeController : ControllerBase
    {
        private readonly IPartTypeRepository _partTypeRepository;

        public PartTypeController(IPartTypeRepository partTypeRepository)
        {
            _partTypeRepository = partTypeRepository;
        }

        [Authorize("Admin", "Instruktør")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                var partTypes = await _partTypeRepository.GetAllAsync();

                var partTypeResponses = partTypes
                    .Select(partType => MapPartTypeToPartTypeResponse(partType))
                    .ToList();

                return Ok(partTypeResponses);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] PartTypeRequest partTypeRequest)
        {
            try
            {
                var newPartType = MapPartTypeRequestToPartType(partTypeRequest);

                var partType = await _partTypeRepository.CreateAsync(newPartType);

                var partTypeResponse = MapPartTypeToPartTypeResponse(partType);

                return Ok(partTypeResponse);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør")]
        [HttpGet]
        [Route("{partTypeId}")]
        public async Task<IActionResult> FindByIdAsync([FromRoute] int partTypeId)
        {
            try
            {
                var partType = await _partTypeRepository.FindByIdAsync(partTypeId);
                if (partType == null)
                {
                    return NotFound();
                }

                return Ok(MapPartTypeToPartTypeResponse(partType));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør")]
        [HttpPut]
        [Route("{partTypeId}")]
        public async Task<IActionResult> UpdateByIdAsync([FromRoute] int partTypeId, [FromBody] PartTypeRequest partTypeRequest)
        {
            try
            {
                var updatePartType = MapPartTypeRequestToPartType(partTypeRequest);

                var partType = await _partTypeRepository.UpdateByIdAsync(partTypeId, updatePartType);

                if (partType == null)
                {
                    return NotFound();
                }

                return Ok(MapPartTypeToPartTypeResponse(partType));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        // NEW: Guarded delete that maps repo result to 404/409/200
        [Authorize("Admin")]
        [HttpDelete]
        [Route("{partTypeId}")]
        public async Task<IActionResult> DeleteByIdAsync([FromRoute] int partTypeId)
        {
            try
            {
                var result = await _partTypeRepository.DeleteByIdAsync(partTypeId);

                switch (result.Status)
                {
                    case DeleteStatus.NotFound:
                        return NotFound();

                    case DeleteStatus.InUse:
                        return Conflict(new
                        {
                            message = "Cannot delete PartType because it is referenced by one or more PartGroups.",
                            code = "IN_USE",
                            blockedBy = new[] { "PartGroup" }
                        });

                    case DeleteStatus.Deleted:
                        // Return the deleted entity details (optional)
                        return Ok(MapPartTypeToPartTypeResponse(result.Entity!));

                    default:
                        return Problem("Unknown delete status.");
                }
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        private static PartTypeResponse MapPartTypeToPartTypeResponse(PartType partType)
        {
            return new PartTypeResponse
            {
                Id = partType.Id,
                PartTypeName = partType.PartTypeName,
            };
        }

        private static PartType MapPartTypeRequestToPartType(PartTypeRequest partTypeRequest)
        {
            return new PartType
            {
                PartTypeName = partTypeRequest.PartTypeName,
            };
        }
    }
}
