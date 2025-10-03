using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOP.Entities;
using SOP.Repositories;

namespace SOP.Controllers
{
    //create the route for our angular to call
    [Route("api/[controller]")]
    [ApiController]
    public class ComputerPartController : ControllerBase
    {
        // Injecting the IRoomRepository interface and storing it in a private readonly variable
        // This allows access to the room repository methods throughout the class
        private readonly IComputerPartRepository _computerPartRepository;

        // Initializes the controller with the address repository
        public ComputerPartController(IComputerPartRepository computerPartRepository)
        {
            // Assigning the repository to the private variable
            _computerPartRepository = computerPartRepository;
        }

        private static string? SafeDecrypt(string? v)
        {
            if (string.IsNullOrWhiteSpace(v)) return v;
            try { return EncryptionHelper.Decrypt(v); } catch { return v; }
        }

        [Authorize("Admin", "Instruktør")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                var computerParts = await _computerPartRepository.GetAllAsync();

                List<ComputerPartResponse> computerPartResponses = computerParts
                    .Select(cp => MapComputerPartToComputerPartResponse(cp))
                    .ToList();

                return Ok(computerPartResponses);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] ComputerPartRequest computerPartRequest)
        {
            try
            {
                var newComputerPart = MapComputerPartRequestToComputerPart(computerPartRequest);

                // Encrypt sensitive strings before save
                newComputerPart.SerialNumber = EncryptionHelper.Encrypt(newComputerPart.SerialNumber);
                newComputerPart.ModelNumber = EncryptionHelper.Encrypt(newComputerPart.ModelNumber);

                var computerPart = await _computerPartRepository.CreateAsync(newComputerPart);

                var computerPartResponse = MapComputerPartToComputerPartResponse(computerPart);
                return Ok(computerPartResponse);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør")]
        [HttpGet]
        [Route("{computerPartId}")]
        public async Task<IActionResult> FindByIdAsync([FromRoute] int computerPartId)
        {
            try
            {
                var computerPart = await _computerPartRepository.FindByIdAsync(computerPartId);
                if (computerPart == null)
                {
                    return NotFound();
                }

                return Ok(MapComputerPartToComputerPartResponse(computerPart));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør")]
        [HttpPut]
        [Route("{computerPartId}")]
        public async Task<IActionResult> UpdateByIdAsync([FromRoute] int computerPartId, [FromBody] ComputerPartRequest computerPartRequest)
        {
            try
            {
                var updateComputerPart = MapComputerPartRequestToComputerPart(computerPartRequest);

                // Encrypt sensitive strings before save
                updateComputerPart.SerialNumber = EncryptionHelper.Encrypt(updateComputerPart.SerialNumber);
                updateComputerPart.ModelNumber = EncryptionHelper.Encrypt(updateComputerPart.ModelNumber);

                var computerPart = await _computerPartRepository.UpdateByIdAsync(computerPartId, updateComputerPart);

                if (computerPart == null)
                {
                    return NotFound();
                }

                return Ok(MapComputerPartToComputerPartResponse(computerPart));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin")]
        [HttpDelete]
        [Route("{computerPartId}")]
        public async Task<IActionResult> DeleteByIdAsync([FromRoute] int computerPartId)
        {
            try
            {
                // Repository now returns a DeleteResult<ComputerPart> (NotFound / InUse / Deleted)
                var result = await _computerPartRepository.DeleteByIdAsync(computerPartId);

                switch (result.Status)
                {
                    case DeleteStatus.NotFound:
                        return NotFound();

                    case DeleteStatus.InUse:
                        // Blocked because this part is attached via Computer_ComputerPart
                        return Conflict(new
                        {
                            message = "Cannot delete ComputerPart because it is in use by a Computer.",
                            code = "IN_USE",
                            blockedBy = "Computer_ComputerPart"
                        });

                    case DeleteStatus.Deleted:
                        return Ok(MapComputerPartToComputerPartResponse(result.Entity));

                    default:
                        return Problem("Unknown delete status.");
                }
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        private ComputerPartResponse MapComputerPartToComputerPartResponse(ComputerPart computerPart)
        {
            var response = new ComputerPartResponse
            {
                Id = computerPart.Id,
                PartGroupId = computerPart.PartGroupId,
                SerialNumber = SafeDecrypt(computerPart.SerialNumber),
                ModelNumber = SafeDecrypt(computerPart.ModelNumber),
            };

            if (computerPart.PartGroup != null)
            {
                response.Group = new ComputerPartPartGroupResponse
                {
                    Id = computerPart.PartGroup.Id,
                    PartName = SafeDecrypt(computerPart.PartGroup.PartName),
                    Price = computerPart.PartGroup.Price,
                    Manufacturer = SafeDecrypt(computerPart.PartGroup.Manufacturer),
                    WarrantyPeriod = SafeDecrypt(computerPart.PartGroup.WarrantyPeriod),
                    ReleaseDate = computerPart.PartGroup.ReleaseDate,
                    PartTypeId = computerPart.PartGroup.PartTypeId,
                };

                if (computerPart.PartGroup.PartType != null)
                {
                    response.Group.PartType = new ComputerPartPartGroupPartTypeResponse
                    {
                        Id = computerPart.PartGroup.PartType.Id,
                        PartTypeName = computerPart.PartGroup.PartType.PartTypeName,
                    };
                }
            }

            if (computerPart.Computer_ComputerPart != null)
            {
                response.Computer_ComputerPart = new ComputerPartComputer_ComputerPartResponse
                {
                    Id = computerPart.Computer_ComputerPart.Id,
                    ComputerId = computerPart.Computer_ComputerPart.ComputerId
                };
            }

            return response;
        }

        private ComputerPart MapComputerPartRequestToComputerPart(ComputerPartRequest computerPartRequest)
        {
            return new ComputerPart
            {
                PartGroupId = computerPartRequest.PartGroupId,
                SerialNumber = computerPartRequest.SerialNumber,
                ModelNumber = computerPartRequest.ModelNumber,
            };
        }
    }
}
