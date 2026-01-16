using Microsoft.AspNetCore.Mvc;
using SOP.Entities;
using SOP.Repositories;
using SOP.Encryption;

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
            StatusHistoryResponse response = new StatusHistoryResponse
            {
                Id = statusHistory.Id,
                ItemId = statusHistory.ItemId,
                StatusId = statusHistory.StatusId,
                StatusUpdateDate = statusHistory.StatusUpdateDate,
                Note = EncryptionHelper.Decrypt(statusHistory.Note)
            };
            if(statusHistory.Status != null)
            {
                response.Status = new StatusHistoryStatusResponse
                {
                    Id = statusHistory.Status.Id,
                    Name = statusHistory.Status.Name
                };
            }
            if(statusHistory.Item != null)
            {
                response.Item = new StatusItemResponse
                {
                    Id = statusHistory.Item.Id,
                    RoomId = statusHistory.Item.RoomId,
                    ItemGroupId = statusHistory.Item.ItemGroupId,
                    SerialNumber = EncryptionHelper.Decrypt(statusHistory.Item.SerialNumber),
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
                StatusUpdateDate= statusHistoryRequest.StatusUpdateDate,
                Note= EncryptionHelper.Encrypt(statusHistoryRequest.Note)
            };
        }
    }
}
