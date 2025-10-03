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

        private static string? SafeDecrypt(string? v) 
        {
            if (string.IsNullOrEmpty(v)) return v;
            try { return EncryptionHelper.Decrypt(v); }
            catch { return v; }
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

                // Encrypt before save
                newRequest.Item = EncryptionHelper.Encrypt(newRequest.Item);
                newRequest.Message = EncryptionHelper.Encrypt(newRequest.Message);
                newRequest.Status = EncryptionHelper.Encrypt(newRequest.Status);
                newRequest.RecipientEmail = EncryptionHelper.Encrypt(newRequest.RecipientEmail);

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

                // Encrypt before save
                updateRequest.Item = EncryptionHelper.Encrypt(updateRequest.Item);
                updateRequest.Message = EncryptionHelper.Encrypt(updateRequest.Message);
                updateRequest.Status = EncryptionHelper.Encrypt(updateRequest.Status);
                updateRequest.RecipientEmail = EncryptionHelper.Encrypt(updateRequest.RecipientEmail);

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
                // Encrypt archive note BEFORE save
                var encryptedNote = EncryptionHelper.Encrypt(archiveNoteRequest.ArchiveNote);

                var request = await _requestRepository.ArchiveByIdAsync(Id, encryptedNote);
                if (request == null) return NotFound();

                var response = new Archive_RequestResponse
                {
                    Id = request.Id,
                    Date = request.Date,
                    Item = SafeDecrypt(request.Item),
                    Message = SafeDecrypt(request.Message),
                    UserId = request.UserId,
                    Status = SafeDecrypt(request.Status),
                    RecipientEmail = SafeDecrypt(request.RecipientEmail),
                    ArchiveNote = SafeDecrypt(request.ArchiveNote),
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        private static RequestResponse MapRequestToRequestResponse(Request r)
        {
            var response = new RequestResponse
            {
                Id = r.Id,
                Date = r.Date,
                Item = SafeDecrypt(r.Item),
                Message = SafeDecrypt(r.Message),
                UserId = r.UserId,
                Status = SafeDecrypt(r.Status),
                RecipientEmail = SafeDecrypt(r.RecipientEmail),
            };

            if (r.User != null)
            {
                response.RequestUser = new RequestUserResponse
                {
                    Id = r.User.Id,
                    Email = SafeDecrypt(r.User.Email),
                    Name = SafeDecrypt(r.User.Name),
                    RoleId = r.User.RoleId,
                    TwoFactorAuthentication = r.User.TwoFactorAuthentication,
                };
            }

            return response;
        }

        private static Request MapRequestRequestToRequest(RequestRequest requestRequest)
        {
            return new Request
            {
                Date = DateTime.UtcNow, // Er ikke sikker på den her
                Message = requestRequest.Message,
                UserId = requestRequest.UserId,
                Item = requestRequest.Item,
                Status = requestRequest.Status,
                RecipientEmail = requestRequest.RecipientEmail,
            };
        }
    }
}
