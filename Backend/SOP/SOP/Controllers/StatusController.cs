using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOP.Entities;
using SOP.Repositories;

namespace SOP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        private readonly IStatusRepository _statusRepository;

        public StatusController(IStatusRepository statusRepository)
        {
            _statusRepository = statusRepository;
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet]
        public async Task<IActionResult> GetAllASync()
        {
            try
            {
                var statuses = await _statusRepository.GetAllAsync();
                var statusResponses = statuses.Select(MapStatusToStatusResponse).ToList();
                return Ok(statusResponses);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] StatusRequest statusRequest)
        {
            try
            {
                var newStatus = MapStatusRequestToStatus(statusRequest);
                var status = await _statusRepository.CreateAsync(newStatus);
                return Ok(MapStatusToStatusResponse(status));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        // Quick helper endpoint to check if a status is referenced in StatusHistory
        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet("{id}/has-history")]
        public async Task<IActionResult> HasHistory([FromRoute] int id)
        {
            try
            {
                var exists = await _statusRepository.HasStatusHistoryAsync(id);
                return Ok(new { hasHistory = exists });
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        // DELETE with "in-use" guard -> return 409 if referenced by any StatusHistory row
        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync([FromRoute] int id)
        {
            try
            {
                var result = await _statusRepository.DeleteByIdAsync(id);

                return result.Status switch
                {
                    DeleteStatus.NotFound => NotFound(new { message = $"Status med ID {id} blev ikke fundet." }),
                    DeleteStatus.InUse => Conflict(new { message = "Status kan ikke slettes, fordi den bruges i StatusHistory." }),
                    DeleteStatus.Deleted => NoContent(), // 204
                    _ => Problem("Ukendt sletningsstatus.")
                };
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }


        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet("{Id}")]
        public async Task<IActionResult> FindByIdAsync([FromRoute] int Id)
        {
            try
            {
                var status = await _statusRepository.FindByIdAsync(Id);
                if (status == null) return NotFound();

                return Ok(MapStatusToStatusResponse(status));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        private static StatusResponse MapStatusToStatusResponse(Status status) => new StatusResponse
        {
            Id = status.Id,
            Name = status.Name
        };

        private static Status MapStatusRequestToStatus(StatusRequest statusRequest) => new Status
        {
            Name = statusRequest.Name,
        };
    }
}
