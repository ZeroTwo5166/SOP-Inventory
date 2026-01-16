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

        [Authorize("Admin", "Instruktør")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            //We use a try methode to get an answer if anything goes wrong,
            //we can print a message and not let the user completle blind over the problem
            try
            {
                //We are using the GetAllAsync methode from the Interface and set it into a var
                var computerParts = await _computerPartRepository.GetAllAsync();

                //We are selecting and mapping the statusHistories we got from the database and making it into a list of computerPart responses
                List<ComputerPartResponse> computerPartResponses = computerParts.Select(
                    computerPart => MapComputerPartToComputerPartResponse(computerPart)).ToList();

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
                ComputerPart newComputerPart = MapComputerPartRequestToComputerPart(computerPartRequest);

                var computerPart = await _computerPartRepository.CreateAsync(newComputerPart);

                ComputerPartResponse computerPartResponse = MapComputerPartToComputerPartResponse(computerPart);

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
                    return NotFound(); //Status Code 404
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

                var computerPart = await _computerPartRepository.UpdateByIdAsync(computerPartId, updateComputerPart);

                if (computerPart == null)
                {
                    return NotFound(); //Status Code 404
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
                var computerPart = await _computerPartRepository.DeleteByIdAsync(computerPartId);
                if (computerPart == null)
                {
                    return NotFound(); //Status Code 404
                }

                return Ok(MapComputerPartToComputerPartResponse(computerPart));
            }
            catch (Exception ex)
            {

                return Problem(ex.Message);
            }
        }

        private static ComputerPartResponse MapComputerPartToComputerPartResponse(ComputerPart computerPart)
        {
            ComputerPartResponse response = new ComputerPartResponse
            {
                Id = computerPart.Id,
                PartGroupId = computerPart.PartGroupId,
                SerialNumber = EncryptionHelper.Decrypt(computerPart.SerialNumber),
                ModelNumber = EncryptionHelper.Decrypt(computerPart.ModelNumber),
            };
            
            if(computerPart.PartGroup != null)
            {
                response.Group = new ComputerPartPartGroupResponse
                {
                    Id = computerPart.PartGroup.Id,
                    PartName = EncryptionHelper.Decrypt(computerPart.PartGroup.PartName),
                    Price = computerPart.PartGroup.Price,
                    Manufacturer = EncryptionHelper.Decrypt(computerPart.PartGroup.Manufacturer),
                    WarrantyPeriod = computerPart.PartGroup.WarrantyPeriod,
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
            
            if(computerPart.Computer_ComputerPart != null)
            {
                response.Computer_ComputerPart = new ComputerPartComputer_ComputerPartResponse
                {
                    Id = computerPart.Computer_ComputerPart.Id,
                    ComputerId = computerPart.Computer_ComputerPart.ComputerId
                };
            }

            return response;
        }

        // MODIFIED: Add encryption
        private static ComputerPart MapComputerPartRequestToComputerPart(ComputerPartRequest computerPartRequest)
        {
            return new ComputerPart
            {
                PartGroupId = computerPartRequest.PartGroupId,
                SerialNumber = EncryptionHelper.Encrypt(computerPartRequest.SerialNumber),
                ModelNumber = EncryptionHelper.Encrypt(computerPartRequest.ModelNumber),
            };
        }
    }
}
    

