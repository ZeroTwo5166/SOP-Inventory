using Microsoft.AspNetCore.Mvc;
using SOP.Entities;
using SOP.Repositories;

namespace SOP.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class StatusHistoryController : ControllerBase
    {
        private readonly IStatusHistoryRepository _statusHistoryRepository;

        public StatusHistoryController(IStatusHistoryRepository statusHistoryRepository)
        {
            _statusHistoryRepository = statusHistoryRepository;
        }

        private static string? SafeDecrypt(string? v)
        {
            if (string.IsNullOrWhiteSpace(v)) return v;
            try { return EncryptionHelper.Decrypt(v); }
            catch { return v; }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                var statusHistories = await _statusHistoryRepository.GetAllAsync();

                List<StatusHistoryResponse> statusHistoryResponses = statusHistories.Select(
                    statusHistory => MapStatusHistoryToStatusHistoryResponse(statusHistory)).ToList();

                return Ok(statusHistoryResponses);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] StatusHistoryRequest statusHistoryRequest)
        {
            try
            {
                StatusHistory newStatusHistory = MapStatusHistoryRequestToStatusHistory(statusHistoryRequest);

                // Encrypt note before save
                newStatusHistory.Note = EncryptionHelper.Encrypt(newStatusHistory.Note);

                var statusHistory = await _statusHistoryRepository.CreateAsync(newStatusHistory);

                StatusHistoryResponse statusHistoryResponse = MapStatusHistoryToStatusHistoryResponse(statusHistory);

                return Ok(statusHistoryResponse);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet]
        [Route("{Id}")]
        public async Task<IActionResult> FindByIdAsync([FromRoute] int Id)
        {
            try
            {
                var statusHistory = await _statusHistoryRepository.FindByIdAsync(Id);
                if (statusHistory == null)
                {
                    return NotFound();
                }

                return Ok(MapStatusHistoryToStatusHistoryResponse(statusHistory));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPut]
        [Route("{Id}")]
        public async Task<IActionResult> UpdateByIdAsync([FromRoute] int Id, [FromBody] StatusHistoryRequest statusHistoryRequest)
        {
            try
            {
                var updateStatusHistory = MapStatusHistoryRequestToStatusHistory(statusHistoryRequest);

                //Encrypt before saving
                updateStatusHistory.Note = EncryptionHelper.Encrypt(updateStatusHistory.Note);

                var statusHistory = await _statusHistoryRepository.UpdateByIdAsync(Id, updateStatusHistory);

                if (statusHistory == null)
                {
                    return NotFound();
                }

                return Ok(MapStatusHistoryToStatusHistoryResponse(statusHistory));
            }
            catch (Exception ex)
            {

                return Problem(ex.Message);
            }
        }

        private StatusHistoryResponse MapStatusHistoryToStatusHistoryResponse(StatusHistory statusHistory)
        {
            var response = new StatusHistoryResponse
            {
                Id = statusHistory.Id,
                ItemId = statusHistory.ItemId,
                StatusId = statusHistory.StatusId,
                StatusUpdateDate = statusHistory.StatusUpdateDate,
                // decrypt the free-text note
                Note = SafeDecrypt(statusHistory.Note)
            };

            if (statusHistory.Status != null)
            {
                response.Status = new StatusHistoryStatusResponse
                {
                    Id = statusHistory.Status.Id,
                    // if you later encrypt Status.Name, switch to SafeDecrypt here too
                    Name = statusHistory.Status.Name
                };
            }

            if (statusHistory.Item != null)
            {
                response.Item = new StatusItemResponse
                {
                    Id = statusHistory.Item.Id,
                    RoomId = statusHistory.Item.RoomId,
                    ItemGroupId = statusHistory.Item.ItemGroupId,
                    // Item.SerialNumber is encrypted in ItemController, so decrypt here
                    SerialNumber = SafeDecrypt(statusHistory.Item.SerialNumber),
                };
            }

            return response;
        }

        private StatusHistory MapStatusHistoryRequestToStatusHistory(StatusHistoryRequest statusHistoryRequest)
        {
            return new StatusHistory
            {
                ItemId = statusHistoryRequest.ItemId,
                StatusId = statusHistoryRequest.StatusId,
                StatusUpdateDate = statusHistoryRequest.StatusUpdateDate,
                // keep plaintext here; encrypt on write in Create/Update actions
                Note = statusHistoryRequest.Note
            };
        }

    }
}
