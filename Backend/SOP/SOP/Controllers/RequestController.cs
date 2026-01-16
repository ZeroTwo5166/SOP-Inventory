using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using SOP.Archive.DTOs;
using SOP.DTOs;
using SOP.Encryption;
using SOP.Entities;
using SOP.Repositories;

namespace SOP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestController : ControllerBase
    {
        private readonly IRequestRepository _requestRepository;

        public RequestController(IRequestRepository requestRepository)
        {
            _requestRepository = requestRepository;
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                List<Request> request = await _requestRepository.GetAllAsync();

                List<RequestResponse> requestResponses = request.Select(
                    request => MapRequestToRequestResponse(request)).ToList();
                return Ok(requestResponses);
            }
            catch (Exception ex)
            {

                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Elev", "Drift")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] RequestRequest requestRequest)
        {
            try
            {
                Request newRequest = MapRequestRequestToRequest(requestRequest);

                var request = await _requestRepository.CreateAsync(newRequest);

                RequestResponse requestResponse = MapRequestToRequestResponse(request);

                return Ok(requestResponse);
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
                var request = await _requestRepository.FindByIdAsync(Id);
                if (request == null)
                {
                    return NotFound(); 
                }

                return Ok(MapRequestToRequestResponse(request));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPut]
        [Route("{Id}")]
        public async Task<IActionResult> UpdateByIdAsync([FromRoute] int Id, [FromBody] RequestRequest requestRequest)
        {
            try
            {
                var updateRequest = MapRequestRequestToRequest(requestRequest);

                var request = await _requestRepository.UpdateByIdAsync(Id, updateRequest);

                if (request == null)
                {
                    return NotFound(); 
                }

                return Ok(MapRequestToRequestResponse(request));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpDelete]
        [Route("{Id}")]
        public async Task<IActionResult> ArchiveByIdAsync([FromRoute] int Id, [FromBody] ArchiveNoteRequest archiveNoteRequest)
        {
            try
            {
                string archiveNote = archiveNoteRequest.ArchiveNote;
                var request = await _requestRepository.ArchiveByIdAsync(Id, archiveNote);
                if (request == null)
                {
                    return NotFound();
                }

                Archive_RequestResponse response = new Archive_RequestResponse
                {
                    Id = request.Id,
                    Date = request.Date,
                    Item = request.Item,
                    Message = request.Message,
                    UserId = request.UserId,
                    Status = request.Status,
                    RecipientEmail = request.RecipientEmail,
                    ArchiveNote = request.ArchiveNote,
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        private static RequestResponse MapRequestToRequestResponse(Request Request)
        {
            RequestResponse response = new RequestResponse
            {
                Id = Request.Id,
                Date = Request.Date,
                Item = EncryptionHelper.Decrypt(Request.Item),
                Message = EncryptionHelper.Decrypt(Request.Message),
                UserId = Request.UserId,
                Status = Request.Status,
                RecipientEmail = Request.RecipientEmail,
            };

            if (Request.User != null)
            {
                response.RequestUser = new RequestUserResponse
                {
                    Id = Request.User.Id,
                    Email = EncryptionHelper.Decrypt(Request.User.Email),
                    Name = EncryptionHelper.Decrypt(Request.User.Name),
                    RoleId = Request.User.RoleId,
                    TwoFactorAuthentication = Request.User.TwoFactorAuthentication,
                };
            }

            return response;
        }

        private static Request MapRequestRequestToRequest(RequestRequest requestRequest)
        {
            return new Request
            {
                Date = DateTime.UtcNow, // Er ikke sikker på den her
                Message = EncryptionHelper.Encrypt(requestRequest.Message),
                UserId = requestRequest.UserId,
                Item = EncryptionHelper.Encrypt(requestRequest.Item),
                Status = requestRequest.Status,
                RecipientEmail = requestRequest.RecipientEmail,
            };
        }
    }
}
